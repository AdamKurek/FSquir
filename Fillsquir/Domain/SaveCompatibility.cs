namespace Fillsquir.Domain;

public static class SaveCompatibility
{
    public static LevelProgress NormalizeProgress(LevelProgress? progress, PuzzleKey puzzleKey)
    {
        progress ??= new LevelProgress();
        progress.PuzzleKey = puzzleKey;
        progress.SaveSchemaVersion = NormalizeVersion(progress.SaveSchemaVersion);

        if (progress.BestSnapshot is not null)
        {
            progress.BestSnapshot = NormalizeSnapshot(progress.BestSnapshot, puzzleKey);
        }

        return progress;
    }

    public static LevelSnapshot NormalizeSnapshot(LevelSnapshot snapshot, PuzzleKey puzzleKey)
    {
        snapshot.PuzzleKey = puzzleKey;
        snapshot.SaveSchemaVersion = NormalizeVersion(snapshot.SaveSchemaVersion);
        snapshot.PlacedFragments ??= new List<PlacedFragmentState>();
        return snapshot;
    }

    private static int NormalizeVersion(int version)
    {
        return version <= 0 ? SaveSchema.CurrentVersion : version;
    }
}
