namespace Fillsquir.Domain;

public sealed class ScoreSubmission
{
    public PuzzleKey PuzzleKey { get; set; }
    public string InstallId { get; set; } = string.Empty;
    public decimal CoveragePercent { get; set; }
    public DateTimeOffset AchievedAtUtc { get; set; }
    public Guid ClientAttemptId { get; set; } = Guid.NewGuid();
}
