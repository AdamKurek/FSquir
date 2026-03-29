namespace Fillsquir.Campaign;

internal enum CampaignLevelState
{
    Locked = 0,
    Current = 1,
    Completed = 2
}

internal sealed record CampaignLevelCard(
    int Level,
    CampaignLevelState State,
    bool IsPlayable,
    int Stars,
    decimal BestCoveragePercent,
    decimal? WorldRecordCoveragePercent,
    string StatusLabel,
    string DetailLabel,
    string FooterLabel);
