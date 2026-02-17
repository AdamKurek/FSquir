namespace Fillsquir.Domain;

public sealed class QueuedSubmission
{
    public ScoreSubmission Submission { get; set; } = new();
    public DateTimeOffset NextAttemptAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public int AttemptCount { get; set; }
    public string? LastError { get; set; }
}
