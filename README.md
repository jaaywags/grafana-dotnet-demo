# Overview

This is a demo repo to show you how to integrate dotnet APIs into Grafana.

In this project, we send logs (using serilog) to Loki and OpenTelemetry metrics to Promethus. Between our API and Prometheus, we have something called a collector. It is in charge of receiving the metrics and traces, deciding how to process them and where to send them.

## Folders

- **Dashboards** - Holds all of the dashboards for Grafana.
- **Grafana** - Holds all of the configurations and docker stuff for standing up Grafana with Loki and Prometheus.
- **TodoApi** - This is our sample API.

## Running the project

You will need dotnet SDK 8 or higher. You will also need Docker installed and Docker Desktop running. I use VS Code to view this project and run the API. I recommend you use the same.

1. Run the Docker Compose file for Grafana

```bash
cd grafana
docker compose up
```

2. Run the API

```bash
cd TodoApi
dotnet build
dotnet run
```

Alternatively you can use the run & debug menu in VSCode.

## Creating and Viewing Logs and Metrics

1. Follow the steps in the previous section
2. [Go to the Grafana page](http://localhost:3000/)
3. Enter your default credentials

username: admin
password: admin

4. You will be prompted to update your password. Do so.
5. After the API starts up, [go to the swagger page](http://localhost:5000/swagger).
6. Run a few endpoints
7. Go back to Grafana and [go to the explore tab](http://localhost:3000/explore).
8. Click on the labels dropdown and select any of the suggested labels.
9. Do the same for the value dropdown.
10. Click the blue Run Query button and you will see your logs!
11. [Now go to the dashboards tab](http://localhost:3000/dashboards)
12. Click the blue `Create Dashboard` button
13. Click `Import a dashboard`
- If prompted that you have unsaved changes, click Discard

14. Paste the json from this file, `./dashboards/ASPNETCoreDashboard.json`
15. Click Load, then Import.
16. There are your first metrics!
17. [Go back to the dashboards tab](http://localhost:3000/dashboards)
18. Click the new dropdown and click import
19. Add the other dashboard json, `./dashboards/ASPNETCoreEndpointsDashboard.json`
20. Click Load then Import
21. [Go back to the dashboards tab](http://localhost:3000/dashboards)
23. Open up the dashboard, `ASP.NET Core`
24. Now any endpoints that show up in the bottom 3 tables, you can click and you will be redirected to the dashboard we set up in step 19.
25. To stop the API, just press `ctrl + c`
26. To stop the docker containers, just press `ctrl + c`

## Notes

### Source Control

I purposely left a few folders out of source control.
- `./grafana/grafana/`
- `./grafana/prometheus/`
- `./grafana/loki/`

You may want to include these so that your grafana config, metrics, and logs all are saved into source control. I did not need to do that for this demo project.

### Running the Docker Containers

In the steps above, I ran the docker containers using `docker compose up`. You may choose to run the in detached mode by using `docker compose up -d`. If you do so, in order to stop the containers, just run `docker compose down`.

### Setting up a Remote Server

If you want to put this somewhere like on a VPS or Droplet, you definitely can! Here are a few things to keep in mind though.

- This is an unsecured logging server. So anyone with the loki or prometheus endpoints can flood your servers. Very unlikely but you never know.
- The way I am sending logs to Loki is through an http endpoint. So all you need to do is set up a domain with ProxyPass to `http://127.0.0.1:3100`. I also turn off all CORS policies.
- The way metrics and traces are sent are through gRPC, not HTTP. So you don't need a virtual host. Instead, you should just open up a port for `4317`.

## Other Dashboards

Grafana provides so many dashboards. You can [find more here](https://grafana.com/grafana/dashboards/). The one I used in this demo project is a modified version of [this dashboard](https://grafana.com/grafana/dashboards/19924-asp-net-core/).
