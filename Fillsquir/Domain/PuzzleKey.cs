namespace Fillsquir.Domain;

public readonly record struct PuzzleKey(int Level, int Seed, string RulesVersion)
{
    public static string BuildStableId(PuzzleKey key)
    {
        return $"{key.Level}_{key.Seed}_{key.RulesVersion}";
    }
}
