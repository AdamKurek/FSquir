namespace Fillsquir.Campaign;

internal sealed record CampaignProgressEntry(
    int Level,
    decimal BestCoveragePercent,
    decimal? WorldRecordCoveragePercent,
    bool HasSavedSnapshot);
