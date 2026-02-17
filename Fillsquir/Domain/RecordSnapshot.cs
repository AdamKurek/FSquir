namespace Fillsquir.Domain;

public sealed class RecordSnapshot
{
    public decimal? WorldRecordCoveragePercent { get; set; }
    public string? WorldRecordHolderInstallId { get; set; }
    public decimal? PlayerBestCoveragePercent { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }
}
