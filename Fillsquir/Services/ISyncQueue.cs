using Fillsquir.Domain;

namespace Fillsquir.Services;

public interface ISyncQueue
{
    Task EnqueueAsync(ScoreSubmission submission, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<QueuedSubmission>> PeekDueAsync(DateTimeOffset utcNow, int maxItems, CancellationToken cancellationToken = default);
    Task MarkSuccessAsync(Guid clientAttemptId, CancellationToken cancellationToken = default);
    Task RescheduleFailureAsync(
        Guid clientAttemptId,
        DateTimeOffset nextAttemptAtUtc,
        int attemptCount,
        string? lastError,
        CancellationToken cancellationToken = default);
}
