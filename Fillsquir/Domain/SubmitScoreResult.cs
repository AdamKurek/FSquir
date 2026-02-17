namespace Fillsquir.Domain;

public sealed class SubmitScoreResult
{
    public bool IsNewWorldRecord { get; set; }
    public bool IsNewPersonalBest { get; set; }
    public decimal? WorldRecordCoveragePercent { get; set; }
    public string? WorldRecordHolderInstallId { get; set; }
    public decimal? PlayerBestCoveragePercent { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }
}
