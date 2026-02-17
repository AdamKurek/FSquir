namespace FSquir.Api.Data;

public sealed class ScoreSubmissionLog
{
    public Guid ClientAttemptId { get; set; }
    public string InstallId { get; set; } = string.Empty;
    public int Level { get; set; }
    public int Seed { get; set; }
    public string RulesVersion { get; set; } = string.Empty;
    public decimal CoveragePercent { get; set; }
    public DateTimeOffset AchievedAtUtc { get; set; }
    public DateTimeOffset ProcessedAtUtc { get; set; }

    public bool IsNewWorldRecord { get; set; }
    public bool IsNewPersonalBest { get; set; }
    public decimal? WorldRecordCoveragePercent { get; set; }
    public string? WorldRecordHolderInstallId { get; set; }
    public decimal? PlayerBestCoveragePercent { get; set; }
}
