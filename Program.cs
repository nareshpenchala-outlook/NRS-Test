using System.Diagnostics;
using Microsoft.ApplicationInsights.AspNetCore.Extensions;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddApplicationInsightsTelemetry();
builder.Services.AddControllers();

var app = builder.Build();
var startupLogger = app.Logger;
startupLogger.LogInformation("SRE demo service starting with {EnvironmentName}", app.Environment.EnvironmentName);

app.MapGet("/health/live", () => Results.Ok(new { status = "healthy" }));
app.MapGet("/health/ready", () => Results.Ok(new { status = "ready" }));

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
});

app.MapControllers();

app.Run();
