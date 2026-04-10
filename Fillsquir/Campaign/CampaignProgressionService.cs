using Fillsquir.Services;

namespace Fillsquir.Campaign;

internal sealed class CampaignProgressionService
{
    private readonly IScoreEvaluator scoreEvaluator;

    public CampaignProgressionService(IScoreEvaluator scoreEvaluator)
    {
        this.scoreEvaluator = scoreEvaluator;
    }

    public CampaignCatalogState BuildCatalog(IEnumerable<CampaignProgressEntry> progressEntries)
    {
        Dictionary<int, CampaignProgressEntry> progressByLevel = progressEntries
            .Where(static entry => entry.Level > 0)
            .GroupBy(static entry => entry.Level)
            .ToDictionary(
                static group => group.Key,
                static group => group
                    .OrderByDescending(static entry => entry.BestCoveragePercent)
                    .ThenByDescending(static entry => entry.WorldRecordCoveragePercent ?? 0m)
                    .ThenByDescending(static entry => entry.HasSavedSnapshot)
                    .First());

        HashSet<int> clearedLevels = new();
        HashSet<int> legacyPlayableLevels = new();

        foreach ((int level, CampaignProgressEntry entry) in progressByLevel)
        {
            int stars = ComputeStars(entry);
            if (stars >= 1)
            {
                clearedLevels.Add(level);
            }

            if (entry.BestCoveragePercent > 0m || entry.HasSavedSnapshot)
            {
                legacyPlayableLevels.Add(level);
            }
        }

        int currentLevel = 1;
        while (clearedLevels.Contains(currentLevel))
        {
            currentLevel++;
        }

        return new CampaignCatalogState(progressByLevel, clearedLevels, legacyPlayableLevels, currentLevel);
    }

    public (int SectionIndex, int Level) ResolveCurrentTarget(CampaignCatalogState catalog, int pageSize)
    {
        int safePageSize = Math.Max(1, pageSize);
        int currentLevel = Math.Max(1, catalog.CurrentLevel);
        int sectionIndex = (currentLevel - 1) / safePageSize;
        return (sectionIndex, currentLevel);
    }

    public CampaignSectionModel BuildSection(int sectionIndex, int pageSize, CampaignCatalogState catalog)
    {
        int safeSectionIndex = Math.Max(0, sectionIndex);
        int safePageSize = Math.Max(1, pageSize);
        int startLevel = safeSectionIndex * safePageSize + 1;
        int endLevel = startLevel + safePageSize - 1;
        CampaignSectionTheme theme = CampaignThemeResolver.Resolve(safeSectionIndex);

        List<CampaignLevelCard> cards = new(safePageSize);
        for (int level = startLevel; level <= endLevel; level++)
        {
            catalog.ProgressByLevel.TryGetValue(level, out CampaignProgressEntry? progress);
            int stars = ComputeStars(progress);
            bool isCompleted = catalog.ClearedLevels.Contains(level);
            bool isCurrent = level == catalog.CurrentLevel;
            bool isLegacyPlayable = catalog.LegacyPlayableLevels.Contains(level);
            bool isPlayable = isCompleted || isCurrent || isLegacyPlayable;

            CampaignLevelState state = isCompleted
                ? CampaignLevelState.Completed
                : isCurrent
                    ? CampaignLevelState.Current
                    : CampaignLevelState.Locked;

            string statusLabel = state switch
            {
                CampaignLevelState.Completed => "CLEARED",
                CampaignLevelState.Current => "NEXT TARGET",
                _ => "LOCKED"
            };

            string detailLabel = state switch
            {
                CampaignLevelState.Completed => $"Best {progress?.BestCoveragePercent ?? 0m:F2}%",
                CampaignLevelState.Current => $"Break into Sector {safeSectionIndex + 1:00}",
                _ => $"Clear L{Math.Max(1, level - 1):00} first"
            };

            string footerLabel = state switch
            {
                CampaignLevelState.Completed => progress?.WorldRecordCoveragePercent is decimal world
                    ? $"Stars {stars}/3  |  World {world:F2}%"
                    : $"Stars {stars}/3  |  World --",
                CampaignLevelState.Current => progress?.WorldRecordCoveragePercent is decimal world
                    ? $"World target {world:F2}%"
                    : "Fresh route. No target cached.",
                _ => isPlayable
                    ? "Legacy progress kept open"
                    : "Sequential campaign unlock"
            };

            cards.Add(new CampaignLevelCard(
                Level: level,
                State: state,
                IsPlayable: isPlayable,
                Stars: stars,
                BestCoveragePercent: progress?.BestCoveragePercent ?? 0m,
                WorldRecordCoveragePercent: progress?.WorldRecordCoveragePercent,
                StatusLabel: statusLabel,
                DetailLabel: detailLabel,
                FooterLabel: footerLabel));
        }

        return new CampaignSectionModel(
            SectionIndex: safeSectionIndex,
            StartLevel: startLevel,
            EndLevel: endLevel,
            Title: $"Sector {safeSectionIndex + 1:00}",
            Subtitle: theme.Subtitle,
            Theme: theme,
            Levels: cards);
    }

    private int ComputeStars(CampaignProgressEntry? progress)
    {
        if (progress is null)
        {
            return 0;
        }

        decimal bestCoverage = Math.Max(0m, progress.BestCoveragePercent);
        decimal? localReference = bestCoverage > 0m ? bestCoverage : null;
        return scoreEvaluator.ComputeStars(bestCoverage, progress.WorldRecordCoveragePercent, localReference);
    }
}
