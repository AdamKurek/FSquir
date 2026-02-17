namespace Fillsquir.Domain;

public sealed class GameSessionState
{
    public bool SnapEnabled { get; set; } = true;
    public decimal CoveragePercent { get; set; }
    public List<PlacedFragmentState> CurrentPlacements { get; set; } = new();
}
