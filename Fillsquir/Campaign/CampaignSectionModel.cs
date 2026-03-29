namespace Fillsquir.Campaign;

internal sealed record CampaignSectionModel(
    int SectionIndex,
    int StartLevel,
    int EndLevel,
    string Title,
    string Subtitle,
    CampaignSectionTheme Theme,
    IReadOnlyList<CampaignLevelCard> Levels);
