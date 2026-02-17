namespace FSquir.Api.Contracts;

public sealed class SubmitScoreRequest
{
    public int Level { get; set; }
    public int Seed { get; set; }
    public string RulesVersion { get; set; } = string.Empty;
    public string InstallId { get; set; } = string.Empty;
    public decimal CoveragePercent { get; set; }
    public DateTimeOffset AchievedAtUtc { get; set; }
    public Guid ClientAttemptId { get; set; }
}

public sealed class SubmitScoreResponse
{
    public bool IsNewWorldRecord { get; set; }
    public bool IsNewPersonalBest { get; set; }
    public decimal? WorldRecordCoveragePercent { get; set; }
    public string? WorldRecordHolderInstallId { get; set; }
    public decimal? PlayerBestCoveragePercent { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}

public sealed class RecordResponse
{
    public decimal? WorldRecordCoveragePercent { get; set; }
    public string? WorldRecordHolderInstallId { get; set; }
    public decimal? PlayerBestCoveragePercent { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }
}
