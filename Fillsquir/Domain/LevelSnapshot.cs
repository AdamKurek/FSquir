namespace Fillsquir.Domain;

public sealed class LevelSnapshot
{
    public PuzzleKey PuzzleKey { get; set; }
    public decimal CoveragePercent { get; set; }
    public List<PlacedFragmentState> PlacedFragments { get; set; } = new();
}
