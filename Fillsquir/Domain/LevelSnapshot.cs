namespace Fillsquir.Domain;

public sealed class LevelSnapshot
{
    public int SaveSchemaVersion { get; set; } = SaveSchema.CurrentVersion;
    public PuzzleKey PuzzleKey { get; set; }
    public decimal CoveragePercent { get; set; }
    public List<PlacedFragmentState> PlacedFragments { get; set; } = new();
}
