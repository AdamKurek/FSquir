using FSquir.Api.Contracts;
using FSquir.Api.Data;
using FSquir.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace FSquir.Api.Tests;

[TestClass]
public sealed class RecordServiceTests
{
    private static RecordsDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<RecordsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new RecordsDbContext(options);
    }

    [TestMethod]
    public async Task SubmitScoreAsync_UpsertsPlayerBest()
    {
        await using var db = CreateContext();
        RecordService service = new(db);

        SubmitScoreRequest first = new()
        {
            Level = 10,
            Seed = 0,
            RulesVersion = "v2",
            InstallId = "playerA",
            CoveragePercent = 71.1m,
            AchievedAtUtc = DateTimeOffset.UtcNow,
            ClientAttemptId = Guid.NewGuid()
        };

        SubmitScoreRequest second = new()
        {
            Level = 10,
            Seed = 0,
            RulesVersion = "v2",
            InstallId = "playerA",
            CoveragePercent = 82.2m,
            AchievedAtUtc = DateTimeOffset.UtcNow,
            ClientAttemptId = Guid.NewGuid()
        };

        _ = await service.SubmitScoreAsync(first, CancellationToken.None);
        _ = await service.SubmitScoreAsync(second, CancellationToken.None);

        PlayerBestScore? stored = await db.PlayerBestScores.SingleAsync();
        Assert.AreEqual(82.2m, stored.CoveragePercent);
    }

    [TestMethod]
    public async Task SubmitScoreAsync_UpdatesWorldRecordOnlyWhenHigher()
    {
        await using var db = CreateContext();
        RecordService service = new(db);

        SubmitScoreRequest high = new()
        {
            Level = 11,
            Seed = 0,
            RulesVersion = "v2",
            InstallId = "playerA",
            CoveragePercent = 93.5m,
            AchievedAtUtc = DateTimeOffset.UtcNow,
            ClientAttemptId = Guid.NewGuid()
        };

        SubmitScoreRequest lower = new()
        {
            Level = 11,
            Seed = 0,
            RulesVersion = "v2",
            InstallId = "playerB",
            CoveragePercent = 91.4m,
            AchievedAtUtc = DateTimeOffset.UtcNow,
            ClientAttemptId = Guid.NewGuid()
        };

        _ = await service.SubmitScoreAsync(high, CancellationToken.None);
        _ = await service.SubmitScoreAsync(lower, CancellationToken.None);

        WorldRecord? world = await db.WorldRecords.SingleAsync();
        Assert.AreEqual(93.5m, world.CoveragePercent);
        Assert.AreEqual("playerA", world.HolderInstallId);
    }

    [TestMethod]
    public async Task SubmitScoreAsync_IsIdempotentPerClientAttemptId()
    {
        await using var db = CreateContext();
        RecordService service = new(db);

        Guid attemptId = Guid.NewGuid();
        SubmitScoreRequest request = new()
        {
            Level = 12,
            Seed = 0,
            RulesVersion = "v2",
            InstallId = "playerA",
            CoveragePercent = 88.8m,
            AchievedAtUtc = DateTimeOffset.UtcNow,
            ClientAttemptId = attemptId
        };

        SubmitScoreResponse? first = await service.SubmitScoreAsync(request, CancellationToken.None);
        SubmitScoreResponse? second = await service.SubmitScoreAsync(request, CancellationToken.None);

        Assert.IsNotNull(first);
        Assert.IsNotNull(second);
        Assert.AreEqual(first.WorldRecordCoveragePercent, second.WorldRecordCoveragePercent);
        Assert.AreEqual(1, await db.ScoreSubmissionLogs.CountAsync());
        Assert.AreEqual(1, await db.PlayerBestScores.CountAsync());
    }

    [TestMethod]
    public async Task SubmitScoreAsync_RejectsInvalidCoverage()
    {
        await using var db = CreateContext();
        RecordService service = new(db);

        SubmitScoreRequest invalid = new()
        {
            Level = 1,
            Seed = 0,
            RulesVersion = "v2",
            InstallId = "playerA",
            CoveragePercent = 101m,
            AchievedAtUtc = DateTimeOffset.UtcNow,
            ClientAttemptId = Guid.NewGuid()
        };

        SubmitScoreResponse? result = await service.SubmitScoreAsync(invalid, CancellationToken.None);
        Assert.IsNull(result);
    }
}
