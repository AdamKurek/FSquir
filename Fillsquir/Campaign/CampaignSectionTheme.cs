namespace Fillsquir.Campaign;

internal sealed record CampaignSectionTheme(
    string SurfaceStartHex,
    string SurfaceEndHex,
    string AccentHex,
    string AccentMutedHex,
    string GlowHex,
    string StrokeHex,
    string Subtitle,
    double OverlayIntensity,
    double DecorativeDensity,
    double Drama);
