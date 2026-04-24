using System.Threading.RateLimiting;
using FSquir.Api.Contracts;
using FSquir.Api.Data;
using FSquir.Api.Services;
using FSquir.Api.Validation;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

string connectionString =
    builder.Configuration.GetConnectionString("RecordsDb")
    ?? Environment.GetEnvironmentVariable("FSQUIR_RECORDS_DB")
    ?? "Host=localhost;Port=5432;Database=fsquir_records;Username=postgres;Password=postgres";

builder.Services.AddDbContext<RecordsDbContext>(options =>
{
    options.UseNpgsql(connectionString);
});

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
    {
        string key = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: key,
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 60,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                AutoReplenishment = true
            });
    });
});

builder.Services.AddProblemDetails();
builder.Services.AddHttpLogging(options =>
{
    options.LoggingFields = HttpLoggingFields.RequestMethod
        | HttpLoggingFields.RequestPath
        | HttpLoggingFields.ResponseStatusCode
        | HttpLoggingFields.Duration;
});
builder.Services.AddScoped<RecordService>();

var app = builder.Build();

app.UseExceptionHandler();
app.UseHttpLogging();
app.UseRateLimiter();

if (app.Configuration.GetValue("ApplyMigrationsOnStartup", true))
{
    using IServiceScope scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<RecordsDbContext>();
    ILogger<Program> logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        db.Database.Migrate();
        logger.LogInformation("Records database migrations applied.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Records database migration failed. Readiness checks and record endpoints may fail until the database is fixed.");
    }
}

app.MapGet("/api/v1/records/{level:int}/{seed:int}/{rulesVersion}",
    async (int level, int seed, string rulesVersion, HttpRequest httpRequest, RecordService recordService, CancellationToken cancellationToken) =>
    {
        string? installId = httpRequest.Headers["X-Install-Id"].FirstOrDefault();
        RecordResponse? response = await recordService.GetRecordAsync(level, seed, rulesVersion, installId, cancellationToken);
        return response is null ? Results.BadRequest("Invalid puzzle key.") : Results.Ok(response);
    });

app.MapPost("/api/v1/scores",
    async (SubmitScoreRequest request, RecordService recordService, CancellationToken cancellationToken) =>
    {
        SubmitScoreResponse? response = await recordService.SubmitScoreAsync(request, cancellationToken);
        return response is null
            ? Results.BadRequest("Invalid score submission payload.")
            : Results.Ok(response);
    });

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapGet("/health/live", () => Results.Ok(new { status = "ok" }));
app.MapGet("/health/ready",
    async (RecordsDbContext db, CancellationToken cancellationToken) =>
    {
        try
        {
            bool canConnect = await db.Database.CanConnectAsync(cancellationToken);
            return canConnect
                ? Results.Ok(new { status = "ready" })
                : Results.Problem("Database is not reachable.", statusCode: StatusCodes.Status503ServiceUnavailable);
        }
        catch (Exception ex)
        {
            return Results.Problem(
                ex.Message,
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "Database readiness check failed.");
        }
    });

app.Run();
