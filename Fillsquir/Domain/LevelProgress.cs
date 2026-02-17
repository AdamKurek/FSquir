namespace Fillsquir.Domain;

public sealed class LevelProgress
{
    public PuzzleKey PuzzleKey { get; set; }
    public decimal BestCoveragePercent { get; set; }
    public LevelSnapshot? BestSnapshot { get; set; }
    public decimal? WorldRecordCoveragePercent { get; set; }
    public string? WorldRecordHolderInstallId { get; set; }
    public DateTimeOffset? LastSyncedAtUtc { get; set; }
}
