using System.Collections.ObjectModel;

namespace Fillsquir.Campaign;

internal sealed class CampaignCatalogState
{
    public CampaignCatalogState(
        IReadOnlyDictionary<int, CampaignProgressEntry> progressByLevel,
        IReadOnlySet<int> clearedLevels,
        IReadOnlySet<int> legacyPlayableLevels,
        int currentLevel)
    {
        ProgressByLevel = progressByLevel;
        ClearedLevels = clearedLevels;
        LegacyPlayableLevels = legacyPlayableLevels;
        CurrentLevel = Math.Max(1, currentLevel);
    }

    public IReadOnlyDictionary<int, CampaignProgressEntry> ProgressByLevel { get; }

    public IReadOnlySet<int> ClearedLevels { get; }

    public IReadOnlySet<int> LegacyPlayableLevels { get; }

    public int CurrentLevel { get; }

    public int CompletedLevelCount => ClearedLevels.Count;

    public IReadOnlyDictionary<int, CampaignProgressEntry> AsReadOnlyProgressMap()
    {
        return ProgressByLevel is ReadOnlyDictionary<int, CampaignProgressEntry>
            ? ProgressByLevel
            : new ReadOnlyDictionary<int, CampaignProgressEntry>(new Dictionary<int, CampaignProgressEntry>(ProgressByLevel));
    }
}
