using FSquir.Api.Contracts;
using FSquir.Api.Data;
using FSquir.Api.Services;
using Fillsquir.Controls;
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

    private static SubmitScoreRequest CreateRequest(
        int level,
        string installId,
        IReadOnlyList<ScoreProofFragment> proof,
        Guid? clientAttemptId = null)
    {
        bool computed = ScoreProofVerifier.TryComputeCoveragePercent(
            level,
            seed: 0,
            proof,
            out decimal coveragePercent,
            out string? failureReason);
        Assert.IsTrue(computed, failureReason);

        return new SubmitScoreRequest
        {
            Level = level,
            Seed = 0,
            RulesVersion = "v2",
            InstallId = installId,
            CoveragePercent = coveragePercent,
            AchievedAtUtc = DateTimeOffset.UtcNow,
            ClientAttemptId = clientAttemptId ?? Guid.NewGuid(),
            PlacedFragments = proof.Select(static fragment => new PlacedFragmentRequest
            {
                FragmentIndex = fragment.FragmentIndex,
                PositionXWorld = fragment.PositionXWorld,
                PositionYWorld = fragment.PositionYWorld,
                WasTouched = fragment.WasTouched
            }).ToList()
        };
    }

    private static IReadOnlyList<ScoreProofFragment> EmptyProof()
    {
        return Array.Empty<ScoreProofFragment>();
    }

    private static IReadOnlyList<ScoreProofFragment> CenteredSingleFragmentProof()
    {
        return
        [
            new ScoreProofFragment
            {
                FragmentIndex = 0,
                PositionXWorld = 500f,
                PositionYWorld = 500f,
                WasTouched = true
            }
        ];
    }

    [TestMethod]
    public async Task SubmitScoreAsync_UpsertsPlayerBest()
    {
        await using var db = CreateContext();
        RecordService service = new(db);

        SubmitScoreRequest first = CreateRequest(10, "playerA", EmptyProof());
        SubmitScoreRequest second = CreateRequest(10, "playerA", CenteredSingleFragmentProof());
        Assert.AreEqual(1, decimal.Compare(second.CoveragePercent, first.CoveragePercent));

        _ = await service.SubmitScoreAsync(first, CancellationToken.None);
        _ = await service.SubmitScoreAsync(second, CancellationToken.None);

        PlayerBestScore? stored = await db.PlayerBestScores.SingleAsync();
        Assert.AreEqual(second.CoveragePercent, stored.CoveragePercent);
    }

    [TestMethod]
    public async Task SubmitScoreAsync_UpdatesWorldRecordOnlyWhenHigher()
    {
        await using var db = CreateContext();
        RecordService service = new(db);

        SubmitScoreRequest high = CreateRequest(11, "playerA", CenteredSingleFragmentProof());
        SubmitScoreRequest lower = CreateRequest(11, "playerB", EmptyProof());
        Assert.AreEqual(1, decimal.Compare(high.CoveragePercent, lower.CoveragePercent));

        _ = await service.SubmitScoreAsync(high, CancellationToken.None);
        _ = await service.SubmitScoreAsync(lower, CancellationToken.None);

        WorldRecord? world = await db.WorldRecords.SingleAsync();
        Assert.AreEqual(high.CoveragePercent, world.CoveragePercent);
        Assert.AreEqual("playerA", world.HolderInstallId);
    }

    [TestMethod]
    public async Task SubmitScoreAsync_IsIdempotentPerClientAttemptId()
    {
        await using var db = CreateContext();
        RecordService service = new(db);

        Guid attemptId = Guid.NewGuid();
        SubmitScoreRequest request = CreateRequest(12, "playerA", CenteredSingleFragmentProof(), attemptId);

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

    [TestMethod]
    public async Task SubmitScoreAsync_RejectsTamperedCoverage()
    {
        await using var db = CreateContext();
        RecordService service = new(db);

        SubmitScoreRequest request = CreateRequest(13, "playerA", CenteredSingleFragmentProof());
        request.CoveragePercent += 10m;

        SubmitScoreResponse? result = await service.SubmitScoreAsync(request, CancellationToken.None);

        Assert.IsNull(result);
        Assert.AreEqual(0, await db.ScoreSubmissionLogs.CountAsync());
    }
}
