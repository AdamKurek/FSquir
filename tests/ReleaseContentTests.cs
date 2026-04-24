using System.Text.Json;
using Fillsquir.Campaign;
using Fillsquir.Controls;

namespace tests;

[TestClass]
public sealed class ReleaseContentTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    [TestMethod]
    public void CampaignProfiles_AreValidForRelease()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "level_profiles.json");
        string json = File.ReadAllText(path);
        LevelProfiles? profiles = JsonSerializer.Deserialize<LevelProfiles>(json, JsonOptions);

        Assert.IsNotNull(profiles);
        Assert.AreEqual(1, profiles.Version);
        Assert.AreEqual(profiles.Profiles.Count, profiles.Profiles.Select(static profile => profile.Level).Distinct().Count());

        foreach (LevelProfile profile in profiles.Profiles)
        {
            Assert.IsTrue(profile.Level is >= 1 and <= 100, $"Invalid profile level {profile.Level}.");
            if (profile.Fragments.HasValue)
            {
                Assert.IsTrue(profile.Fragments.Value is >= 1 and <= 100, $"Invalid fragment count for level {profile.Level}.");
            }

            if (profile.SnapMultiplier.HasValue)
            {
                Assert.IsTrue(profile.SnapMultiplier.Value > 0d, $"Invalid snap multiplier for level {profile.Level}.");
            }

            Assert.IsFalse(profile.Notes?.Contains("â", StringComparison.Ordinal) ?? false, $"Profile {profile.Level} has mojibake text.");
        }
    }

    [DataTestMethod]
    [DataRow(1)]
    [DataRow(10)]
    [DataRow(16)]
    [DataRow(17)]
    [DataRow(35)]
    [DataRow(51)]
    [DataRow(52)]
    [DataRow(75)]
    [DataRow(91)]
    [DataRow(100)]
    public void ScoreProofVerifier_AcceptsCampaignMilestoneProofs(int level)
    {
        ScoreProofFragment[] proof = FindCoveringProof(level, out decimal coverage);

        ScoreProofVerificationResult verification = ScoreProofVerifier.VerifyCoverage(level, seed: 0, coverage, proof);
        Assert.IsTrue(verification.IsValid, verification.FailureReason);
    }

    private static ScoreProofFragment[] FindCoveringProof(int level, out decimal coverage)
    {
        foreach ((float x, float y) in CandidatePositions())
        {
            ScoreProofFragment[] proof =
            [
                new()
                {
                    FragmentIndex = 0,
                    PositionXWorld = x,
                    PositionYWorld = y,
                    WasTouched = true
                }
            ];

            bool computed = ScoreProofVerifier.TryComputeCoveragePercent(level, seed: 0, proof, out coverage, out string? failureReason);
            Assert.IsTrue(computed, failureReason);
            if (decimal.Compare(coverage, 0m) > 0)
            {
                return proof;
            }
        }

        coverage = 0m;
        Assert.Fail($"Level {level} did not produce a simple nonzero proof at any candidate position.");
        return [];
    }

    private static IEnumerable<(float X, float Y)> CandidatePositions()
    {
        yield return (500f, 500f);
        yield return (260f, 260f);
        yield return (500f, 260f);
        yield return (260f, 500f);
        yield return (740f, 500f);
        yield return (500f, 740f);
        yield return (740f, 740f);
        yield return (160f, 740f);
        yield return (740f, 160f);
    }
}
