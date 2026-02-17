namespace FSquir.Api.Data;

public sealed class PlayerBestScore
{
    public long Id { get; set; }
    public string InstallId { get; set; } = string.Empty;
    public int Level { get; set; }
    public int Seed { get; set; }
    public string RulesVersion { get; set; } = string.Empty;
    public decimal CoveragePercent { get; set; }
    public DateTimeOffset AchievedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}
