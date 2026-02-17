using System.Threading.RateLimiting;
using FSquir.Api.Contracts;
using FSquir.Api.Data;
using FSquir.Api.Services;
using FSquir.Api.Validation;
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
builder.Services.AddScoped<RecordService>();

var app = builder.Build();

app.UseRateLimiter();

using (IServiceScope scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<RecordsDbContext>();
    try
    {
        db.Database.Migrate();
    }
    catch
    {
        // API can still start without migration success; caller will receive DB errors until resolved.
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

app.Run();
