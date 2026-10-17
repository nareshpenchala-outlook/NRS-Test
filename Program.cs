using System.Diagnostics;
using Microsoft.ApplicationInsights.AspNetCore.Extensions;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddApplicationInsightsTelemetry();
builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();
var startupLogger = app.Logger;
startupLogger.LogInformation("SRE demo service starting with {EnvironmentName}", app.Environment.EnvironmentName);

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUi(settings => settings.DocumentPath = "/openapi/v1.json");
}

app.MapGet("/health/live", () => Results.Ok(new { status = "healthy" }))
    .WithName("GetLiveHealth")
    .WithSummary("Check liveness")
    .WithDescription("Returns healthy when the application process is running.");
app.MapGet("/health/ready", () => Results.Ok(new { status = "ready" }))
    .WithName("GetReadyHealth")
    .WithSummary("Check readiness")
    .WithDescription("Returns ready when the application can accept requests.");

app.MapGet("/demo/{scenario}", (string scenario, HttpContext context, ILogger<Program> logger) =>
{
    var traceId = Activity.Current?.TraceId.ToString() ?? context.TraceIdentifier;
    var scope = new Dictionary<string, object>
    {
        ["Scenario"] = scenario,
        ["TraceId"] = traceId,
        ["ServiceName"] = "sre-agent-demo"
    };

    using (logger.BeginScope(scope))
    {
        switch (scenario.ToLowerInvariant())
        {
            case "info":
                logger.LogInformation("Synthetic request completed for {DependencyName} in {DurationMs} ms", "catalog", 42);
                return Results.Ok(new { scenario, traceId, outcome = "success" });

            case "warning":
                logger.LogWarning("Synthetic dependency latency elevated for {DependencyName}; observed {DurationMs} ms", "payments", 1850);
                return Results.Ok(new { scenario, traceId, outcome = "degraded" });

            case "error":
                var exception = new TimeoutException("Synthetic timeout from the inventory dependency.");
                logger.LogError(exception, "Synthetic dependency failure for {DependencyName} with {StatusCode}", "inventory", 503);
                return Results.Problem(
                    title: "Synthetic dependency failure",
                    detail: "This response is intentional for SRE-agent testing.",
                    statusCode: StatusCodes.Status503ServiceUnavailable,
                    extensions: new Dictionary<string, object?> { ["traceId"] = traceId });

            default:
                logger.LogInformation("Unknown diagnostic scenario requested: {Scenario}", scenario);
                return Results.NotFound(new { error = "Use info, warning, or error.", traceId });
        }
    }
})
    .WithName("RunDiagnosticScenario")
    .WithSummary("Run a diagnostic scenario")
    .WithDescription("Emits telemetry for the info, warning, or error scenario; unknown scenarios return 404.");

app.MapControllers();

app.Run();
