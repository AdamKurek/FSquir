using Fillsquir.Domain;

namespace Fillsquir.Services;

public sealed class RecordSyncService : IRecordSyncService
{
    private static readonly TimeSpan MaxBackoff = TimeSpan.FromMinutes(30);
    private readonly SemaphoreSlim syncGate = new(1, 1);

    private readonly ILeaderboardClient leaderboardClient;
    private readonly IProgressStore progressStore;
    private readonly ISyncQueue syncQueue;

    public RecordSyncService(
        ILeaderboardClient leaderboardClient,
        IProgressStore progressStore,
        ISyncQueue syncQueue)
    {
        this.leaderboardClient = leaderboardClient;
        this.progressStore = progressStore;
        this.syncQueue = syncQueue;
    }

    public async Task EnqueueBestScoreAsync(ScoreSubmission submission, CancellationToken cancellationToken = default)
    {
        await syncQueue.EnqueueAsync(submission, cancellationToken);
        await TriggerSyncAsync(cancellationToken);
    }

    public async Task TriggerSyncAsync(CancellationToken cancellationToken = default)
    {
        if (!await syncGate.WaitAsync(0, cancellationToken))
        {
            return;
        }

        try
        {
            IReadOnlyList<QueuedSubmission> due = await syncQueue.PeekDueAsync(DateTimeOffset.UtcNow, maxItems: 8, cancellationToken);
            foreach (QueuedSubmission queued in due)
            {
                try
                {
                    SubmitScoreResult? result = await leaderboardClient.SubmitScoreAsync(queued.Submission, cancellationToken);
                    if (result is null)
                    {
                        throw new InvalidOperationException("Leaderboard submission failed.");
                    }

                    await syncQueue.MarkSuccessAsync(queued.Submission.ClientAttemptId, cancellationToken);
                    await UpdateLocalProgressFromServerAsync(queued.Submission.PuzzleKey, result, cancellationToken);
                }
                catch (Exception ex)
                {
                    int nextAttemptCount = queued.AttemptCount + 1;
                    double backoffSeconds = Math.Pow(2d, Math.Min(queued.AttemptCount, 8)) * 5d;
                    TimeSpan delay = TimeSpan.FromSeconds(Math.Min(backoffSeconds, MaxBackoff.TotalSeconds));
                    await syncQueue.RescheduleFailureAsync(
                        queued.Submission.ClientAttemptId,
                        DateTimeOffset.UtcNow + delay,
                        nextAttemptCount,
                        ex.Message,
                        cancellationToken);
                }
            }
        }
        finally
        {
            syncGate.Release();
        }
    }

    private async Task UpdateLocalProgressFromServerAsync(
        PuzzleKey puzzleKey,
        SubmitScoreResult result,
        CancellationToken cancellationToken)
    {
        LevelProgress progress = await progressStore.LoadLevelProgressAsync(puzzleKey, cancellationToken);
        progress.WorldRecordCoveragePercent = result.WorldRecordCoveragePercent;
        progress.WorldRecordHolderInstallId = result.WorldRecordHolderInstallId;
        progress.LastSyncedAtUtc = result.UpdatedAtUtc ?? DateTimeOffset.UtcNow;

        if (result.PlayerBestCoveragePercent.HasValue
            && result.PlayerBestCoveragePercent.Value > progress.BestCoveragePercent)
        {
            progress.BestCoveragePercent = result.PlayerBestCoveragePercent.Value;
        }

        await progressStore.SaveLevelProgressAsync(progress, cancellationToken);
    }
}
