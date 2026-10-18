# SRE Agent Demo (.NET 10)

A small ASP.NET Core API that emits structured logs, exceptions, and request telemetry for SRE-agent development. `appsettings.json` contains a nonfunctional dummy Application Insights connection string. Set `APPLICATIONINSIGHTS_CONNECTION_STRING` to a real connection string to send telemetry; the environment variable overrides the dummy value.

## Requirements

- .NET 10 SDK
- An Azure Application Insights resource (optional for local-only console testing)

## Run

```sh
export APPLICATIONINSIGHTS_CONNECTION_STRING='InstrumentationKey=<key>;IngestionEndpoint=<endpoint>'
dotnet restore
dotnet run --urls http://localhost:5080
```

With the dummy value, the API still runs and logs to the console, but telemetry is not sent to a real Application Insights resource. Requests are sent to Application Insights when a valid connection string is configured. Allow a few minutes for telemetry ingestion.

Open [http://localhost:5080/swagger](http://localhost:5080/swagger) for the interactive Swagger UI or [http://localhost:5080/openapi/v1.json](http://localhost:5080/openapi/v1.json) for the OpenAPI document. Swagger UI is enabled in **all environments** (including Production) since this is a demo/test service. Navigating to the root URL (`/`) redirects to `/swagger`.

In non-Development environments a global exception handler catches unhandled exceptions, logs them to Application Insights, and returns a JSON error response with a `traceId` (no stack-trace leakage).

## Exercise the service

```sh
curl http://localhost:5080/health/live
curl http://localhost:5080/health/ready
curl http://localhost:5080/api/sample
curl http://localhost:5080/demo/info
curl http://localhost:5080/demo/warning
curl -i http://localhost:5080/demo/error
```

The diagnostic routes add `Scenario`, `TraceId`, and `ServiceName` scope properties. The error route logs a synthetic `TimeoutException` and returns HTTP 503. Application Insights request telemetry includes the incoming request and its trace operation ID.

## Example KQL

Recent diagnostic traces:

```kusto
traces
| where timestamp > ago(1h)
| where customDimensions.Scenario in ("info", "warning", "error")
| project timestamp, severityLevel, message, operation_Id, customDimensions
| order by timestamp desc
```

Failed requests:

```kusto
requests
| where timestamp > ago(1h)
| where success == false
| project timestamp, name, resultCode, operation_Id, url
| order by timestamp desc
```

Exceptions:

```kusto
exceptions
| where timestamp > ago(1h)
| project timestamp, type, outerMessage, operation_Id, customDimensions
| order by timestamp desc
```
