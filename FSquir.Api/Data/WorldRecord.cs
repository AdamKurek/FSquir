namespace FSquir.Api.Data;

public sealed class WorldRecord
{
    public long Id { get; set; }
    public int Level { get; set; }
    public int Seed { get; set; }
    public string RulesVersion { get; set; } = string.Empty;
    public decimal CoveragePercent { get; set; }
    public string HolderInstallId { get; set; } = string.Empty;
    public DateTimeOffset AchievedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}
