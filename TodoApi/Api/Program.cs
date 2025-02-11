using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Exceptions;
using Serilog.Sinks.Grafana.Loki;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var env = "Local";
var appId = "1234";
var appName = "Todo Sample API";

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<TodoDb>(opt => opt.UseInMemoryDatabase("TodoList"));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApiDocument(config =>
{
    config.DocumentName = "TodoAPI";
    config.Title = "TodoAPI v1";
    config.Version = "v1";
});

// Setup Logging
builder.Host.UseSerilog((context, config) =>
{
  config.Enrich.FromLogContext();
  config.Enrich.WithExceptionDetails();
  config.Enrich.With(new SerilogEnricher(
    appId,
    appName,
    env
  ));

  config
    .WriteTo.GrafanaLoki
    (
      "http://localhost:3100",
      new List<LokiLabel> { new() { Key = "appId", Value = appId }, new() { Key = "appName", Value = appName }, new() { Key = "env", Value = env } }
    )
    .WriteTo.Console
    (
      outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} level=[{Level:u3}] appId={ApplicationId} appName={ApplicationName} env={Environment} {Message:lj} {NewLine}{Exception}"
    );
  config.ReadFrom.Configuration(context.Configuration);
});

// #######################
// STARTING GRAFANA SETUP
// #######################

// configure metrics for grafana
var otel = builder.Services.AddOpenTelemetry();

// Configure OpenTelemetry Resources with the application name
otel.ConfigureResource(resource =>
  {
    resource.AddService(serviceName: $"{appName}");
    var globalOpenTelemetryAttributes = new List<KeyValuePair<string, object>>();
    globalOpenTelemetryAttributes.Add(new KeyValuePair<string, object>("env", env));
    globalOpenTelemetryAttributes.Add(new KeyValuePair<string, object>("appId", appId));
    globalOpenTelemetryAttributes.Add(new KeyValuePair<string, object>("appName", appName));
    resource.AddAttributes(globalOpenTelemetryAttributes);
  });

// Add Metrics for ASP.NET Core and our custom metrics and export to Prometheus
otel.WithMetrics(metrics => metrics
    .AddOtlpExporter(otlpOptions =>
    {
      otlpOptions.Endpoint = new Uri("http://localhost:4317");
    })
    // Metrics provider from OpenTelemetry
    .AddAspNetCoreInstrumentation()
    .AddMeter(appName)
    // Metrics provides by ASP.NET Core in .NET 8
    .AddMeter("Microsoft.AspNetCore.Hosting")
    .AddMeter("Microsoft.AspNetCore.Server.Kestrel")
    .AddPrometheusExporter());

// Add Tracing for ASP.NET Core and our custom ActivitySource and export to Jaeger
otel.WithTracing(tracing =>
{
  tracing.AddAspNetCoreInstrumentation();
  tracing.AddHttpClientInstrumentation();
  tracing.AddSource(appName);
  tracing.AddOtlpExporter(otlpOptions =>
  {
    otlpOptions.Endpoint = new Uri("http://localhost:4317");
  });
  // tracing.AddConsoleExporter();
});

// #######################
// ENDING GRAFANA SETUP
// #######################

var app = builder.Build();

// more configuring metrics for grafana
app.UseOpenTelemetryPrometheusScrapingEndpoint();

if (app.Environment.IsDevelopment())
{
    app.UseOpenApi();
    app.UseSwaggerUi(config =>
    {
        config.DocumentTitle = "TodoAPI";
        config.Path = "/swagger";
        config.DocumentPath = "/swagger/{documentName}/swagger.json";
        config.DocExpansion = "list";
    });
}

var logger = app.Services.GetRequiredService<Serilog.ILogger>();

app.MapGet("/todoitems", async (TodoDb db) =>
{
    logger.Information("Get all todo items");
    return await db.Todos.ToListAsync();
});

app.MapGet("/todoitems/complete", async (TodoDb db) =>
{
    logger.Information("Complete an todo item.");
    return await db.Todos.Where(t => t.IsComplete).ToListAsync();
});

// <snippet_getCustom>
app.MapGet("/todoitems/{id}", async (int id, TodoDb db) =>
    await db.Todos.FindAsync(id)
        is Todo todo
            ? Results.Ok(todo)
            : Results.NotFound());
// </snippet_getCustom>
// </snippet_get>

// <snippet_post>
app.MapPost("/todoitems", async (Todo todo, TodoDb db) =>
{
    logger.Information("Add a todo item.");
    db.Todos.Add(todo);
    await db.SaveChangesAsync();

    return Results.Created($"/todoitems/{todo.Id}", todo);
});
// </snippet_post>

// <snippet_put>
app.MapPut("/todoitems/{id}", async (int id, Todo inputTodo, TodoDb db) =>
{
    logger.Information("Update a todo item.");
    var todo = await db.Todos.FindAsync(id);

    if (todo is null) return Results.NotFound();

    todo.Name = inputTodo.Name;
    todo.IsComplete = inputTodo.IsComplete;

    await db.SaveChangesAsync();

    return Results.NoContent();
});
// </snippet_put>

// <snippet_delete>
app.MapDelete("/todoitems/{id}", async (int id, TodoDb db) =>
{
    logger.Information("Get a todo item by ID.");
    if (await db.Todos.FindAsync(id) is Todo todo)
    {
        db.Todos.Remove(todo);
        await db.SaveChangesAsync();
        return Results.NoContent();
    }

    return Results.NotFound();
});

app.Run();