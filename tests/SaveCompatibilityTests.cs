using System.Text.Json;
using Fillsquir.Domain;

namespace tests;

[TestClass]
public sealed class SaveCompatibilityTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    [TestMethod]
    public void NormalizeProgress_UpgradesLegacyProgressWithoutSchemaVersion()
    {
        PuzzleKey key = new(4, 0, "v2");
        const string legacyJson = """
            {
              "puzzleKey": { "level": 4, "seed": 0, "rulesVersion": "v2" },
              "bestCoveragePercent": 42.5,
              "bestSnapshot": {
                "puzzleKey": { "level": 4, "seed": 0, "rulesVersion": "v2" },
                "coveragePercent": 42.5,
                "placedFragments": [
                  { "fragmentIndex": 0, "positionXWorld": 100, "positionYWorld": 200, "wasTouched": true }
                ]
              }
            }
            """;

        LevelProgress? legacy = JsonSerializer.Deserialize<LevelProgress>(legacyJson, JsonOptions);
        LevelProgress normalized = SaveCompatibility.NormalizeProgress(legacy, key);

        Assert.AreEqual(SaveSchema.CurrentVersion, normalized.SaveSchemaVersion);
        Assert.AreEqual(key, normalized.PuzzleKey);
        Assert.IsNotNull(normalized.BestSnapshot);
        Assert.AreEqual(SaveSchema.CurrentVersion, normalized.BestSnapshot.SaveSchemaVersion);
        Assert.AreEqual(key, normalized.BestSnapshot.PuzzleKey);
        Assert.AreEqual(1, normalized.BestSnapshot.PlacedFragments.Count);
    }

    [TestMethod]
    public void NormalizeProgress_CreatesEmptyProgressForMissingFile()
    {
        PuzzleKey key = new(8, 0, "v2");

        LevelProgress normalized = SaveCompatibility.NormalizeProgress(null, key);

        Assert.AreEqual(SaveSchema.CurrentVersion, normalized.SaveSchemaVersion);
        Assert.AreEqual(key, normalized.PuzzleKey);
        Assert.AreEqual(0m, normalized.BestCoveragePercent);
    }
}
