using Fillsquir.Campaign;
using Fillsquir.Services;
using System.Linq;

namespace tests;

[TestClass]
public class CampaignProgressionTests
{
    private readonly CampaignProgressionService progressionService = new(new ScoreEvaluator());

    [TestMethod]
    public void NoProgress_OnlyLevelOneIsPlayable()
    {
        CampaignCatalogState catalog = progressionService.BuildCatalog(Array.Empty<CampaignProgressEntry>());
        CampaignSectionModel section = progressionService.BuildSection(sectionIndex: 0, pageSize: 10, catalog);

        Assert.AreEqual(1, catalog.CurrentLevel);
        Assert.AreEqual(CampaignLevelState.Current, section.Levels[0].State);
        Assert.IsTrue(section.Levels[0].IsPlayable);
        Assert.AreEqual(CampaignLevelState.Locked, section.Levels[1].State);
        Assert.IsFalse(section.Levels[1].IsPlayable);
    }

    [TestMethod]
    public void OneStarClear_UnlocksExactlyTheNextLevel()
    {
        CampaignCatalogState catalog = progressionService.BuildCatalog(
        [
            new CampaignProgressEntry(Level: 1, BestCoveragePercent: 92m, WorldRecordCoveragePercent: 100m, HasSavedSnapshot: false)
        ]);

        CampaignSectionModel section = progressionService.BuildSection(sectionIndex: 0, pageSize: 10, catalog);

        Assert.AreEqual(2, catalog.CurrentLevel);
        Assert.AreEqual(CampaignLevelState.Completed, section.Levels[0].State);
        Assert.AreEqual(1, section.Levels[0].Stars);
        Assert.AreEqual(CampaignLevelState.Current, section.Levels[1].State);
        Assert.IsTrue(section.Levels[1].IsPlayable);
        Assert.AreEqual(CampaignLevelState.Locked, section.Levels[2].State);
        Assert.IsFalse(section.Levels[2].IsPlayable);
    }

    [TestMethod]
    public void CompletedLevels_RemainReplayable()
    {
        CampaignCatalogState catalog = progressionService.BuildCatalog(
        [
            new CampaignProgressEntry(Level: 1, BestCoveragePercent: 98m, WorldRecordCoveragePercent: 100m, HasSavedSnapshot: true),
            new CampaignProgressEntry(Level: 2, BestCoveragePercent: 96m, WorldRecordCoveragePercent: 100m, HasSavedSnapshot: true)
        ]);

        CampaignSectionModel section = progressionService.BuildSection(sectionIndex: 0, pageSize: 10, catalog);

        Assert.AreEqual(3, catalog.CurrentLevel);
        Assert.IsTrue(section.Levels[0].IsPlayable);
        Assert.IsTrue(section.Levels[1].IsPlayable);
        Assert.AreEqual(CampaignLevelState.Completed, section.Levels[0].State);
        Assert.AreEqual(CampaignLevelState.Completed, section.Levels[1].State);
        Assert.AreEqual(CampaignLevelState.Current, section.Levels[2].State);
    }

    [TestMethod]
    public void FutureLevels_RemainLockedBeyondCurrentFrontier()
    {
        CampaignCatalogState catalog = progressionService.BuildCatalog(
        [
            new CampaignProgressEntry(Level: 1, BestCoveragePercent: 98m, WorldRecordCoveragePercent: 100m, HasSavedSnapshot: true),
            new CampaignProgressEntry(Level: 2, BestCoveragePercent: 96m, WorldRecordCoveragePercent: 100m, HasSavedSnapshot: true)
        ]);

        CampaignSectionModel section = progressionService.BuildSection(sectionIndex: 0, pageSize: 10, catalog);

        Assert.AreEqual(CampaignLevelState.Locked, section.Levels[3].State);
        Assert.IsFalse(section.Levels[3].IsPlayable);
        Assert.AreEqual("Clear L03 first", section.Levels[3].DetailLabel);
    }

    [TestMethod]
    public void LegacyProgressOnLaterLevel_StaysSelectable()
    {
        CampaignCatalogState catalog = progressionService.BuildCatalog(
        [
            new CampaignProgressEntry(Level: 1, BestCoveragePercent: 92m, WorldRecordCoveragePercent: 100m, HasSavedSnapshot: true),
            new CampaignProgressEntry(Level: 7, BestCoveragePercent: 95m, WorldRecordCoveragePercent: 100m, HasSavedSnapshot: true)
        ]);

        CampaignSectionModel section = progressionService.BuildSection(sectionIndex: 0, pageSize: 10, catalog);
        CampaignLevelCard laterLevel = section.Levels.Single(static card => card.Level == 7);

        Assert.AreEqual(2, catalog.CurrentLevel);
        Assert.IsTrue(laterLevel.IsPlayable);
        Assert.AreEqual(CampaignLevelState.Completed, laterLevel.State);
        Assert.AreEqual(2, laterLevel.Stars);
    }

    [TestMethod]
    public void ResolveCurrentTarget_NoProgress_PointsToFirstLevelInFirstSection()
    {
        CampaignCatalogState catalog = progressionService.BuildCatalog(Array.Empty<CampaignProgressEntry>());

        (int sectionIndex, int level) = progressionService.ResolveCurrentTarget(catalog, pageSize: 10);

        Assert.AreEqual(0, sectionIndex);
        Assert.AreEqual(1, level);
    }

    [TestMethod]
    public void ResolveCurrentTarget_MultiSectionProgress_PointsToCurrentLevelSection()
    {
        CampaignCatalogState catalog = progressionService.BuildCatalog(
        [
            new CampaignProgressEntry(Level: 1, BestCoveragePercent: 92m, WorldRecordCoveragePercent: 100m, HasSavedSnapshot: true),
            new CampaignProgressEntry(Level: 2, BestCoveragePercent: 92m, WorldRecordCoveragePercent: 100m, HasSavedSnapshot: true),
            new CampaignProgressEntry(Level: 3, BestCoveragePercent: 92m, WorldRecordCoveragePercent: 100m, HasSavedSnapshot: true),
            new CampaignProgressEntry(Level: 4, BestCoveragePercent: 92m, WorldRecordCoveragePercent: 100m, HasSavedSnapshot: true),
            new CampaignProgressEntry(Level: 5, BestCoveragePercent: 92m, WorldRecordCoveragePercent: 100m, HasSavedSnapshot: true),
            new CampaignProgressEntry(Level: 6, BestCoveragePercent: 92m, WorldRecordCoveragePercent: 100m, HasSavedSnapshot: true),
            new CampaignProgressEntry(Level: 7, BestCoveragePercent: 92m, WorldRecordCoveragePercent: 100m, HasSavedSnapshot: true),
            new CampaignProgressEntry(Level: 8, BestCoveragePercent: 92m, WorldRecordCoveragePercent: 100m, HasSavedSnapshot: true),
            new CampaignProgressEntry(Level: 9, BestCoveragePercent: 92m, WorldRecordCoveragePercent: 100m, HasSavedSnapshot: true),
            new CampaignProgressEntry(Level: 10, BestCoveragePercent: 92m, WorldRecordCoveragePercent: 100m, HasSavedSnapshot: true),
            new CampaignProgressEntry(Level: 11, BestCoveragePercent: 92m, WorldRecordCoveragePercent: 100m, HasSavedSnapshot: true),
            new CampaignProgressEntry(Level: 12, BestCoveragePercent: 92m, WorldRecordCoveragePercent: 100m, HasSavedSnapshot: true),
            new CampaignProgressEntry(Level: 13, BestCoveragePercent: 92m, WorldRecordCoveragePercent: 100m, HasSavedSnapshot: true),
            new CampaignProgressEntry(Level: 14, BestCoveragePercent: 92m, WorldRecordCoveragePercent: 100m, HasSavedSnapshot: true)
        ]);

        (int sectionIndex, int level) = progressionService.ResolveCurrentTarget(catalog, pageSize: 10);

        Assert.AreEqual(1, sectionIndex);
        Assert.AreEqual(15, level);
    }

    [TestMethod]
    public void ResolveCurrentTarget_IgnoresLaterLegacyProgressBeyondTheFrontier()
    {
        CampaignCatalogState catalog = progressionService.BuildCatalog(
        [
            new CampaignProgressEntry(Level: 1, BestCoveragePercent: 92m, WorldRecordCoveragePercent: 100m, HasSavedSnapshot: true),
            new CampaignProgressEntry(Level: 19, BestCoveragePercent: 40m, WorldRecordCoveragePercent: 100m, HasSavedSnapshot: true)
        ]);

        (int sectionIndex, int level) = progressionService.ResolveCurrentTarget(catalog, pageSize: 10);

        Assert.AreEqual(0, sectionIndex);
        Assert.AreEqual(2, level);
    }

    
}
