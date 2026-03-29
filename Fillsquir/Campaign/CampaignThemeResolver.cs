namespace Fillsquir.Campaign;

internal static class CampaignThemeResolver
{
    private static readonly ThemePalette[] Palettes =
    {
        new("#09111F", "#16243A", "#5DE2FF", "#1A6780", "#48AEE8", "#2B5671"),
        new("#190E28", "#2C1740", "#FFB44F", "#8D4E14", "#F47D36", "#70402E"),
        new("#101A28", "#182C46", "#78FFCA", "#1C6B55", "#43E3C6", "#356A67"),
        new("#220B22", "#3F1A3B", "#FF7B9C", "#86354E", "#FF4B73", "#7A3354"),
        new("#11161E", "#21314F", "#9DAEFF", "#4457A4", "#7C86FF", "#425486"),
        new("#180E16", "#35202B", "#FF8E6D", "#8D4A38", "#FFA35E", "#7A4A36")
    };

    private static readonly string[] Prefixes =
    {
        "Silent",
        "Shifting",
        "Pressure",
        "Bright",
        "Cracked",
        "Midnight",
        "Magnetic",
        "Burning"
    };

    private static readonly string[] Nouns =
    {
        "Orbit",
        "Wake",
        "Spire",
        "Circuit",
        "Cavern",
        "Signal",
        "Rift",
        "Vault"
    };

    public static CampaignSectionTheme Resolve(int sectionIndex)
    {
        int safeSectionIndex = Math.Max(0, sectionIndex);
        ThemePalette palette = Palettes[safeSectionIndex % Palettes.Length];

        double drama = Math.Min(1d, 0.28d + safeSectionIndex * 0.08d);
        double overlayIntensity = Math.Min(0.8d, 0.2d + safeSectionIndex * 0.035d);
        double decorativeDensity = Math.Min(1d, 0.24d + safeSectionIndex * 0.06d);
        string subtitle = $"{Prefixes[safeSectionIndex % Prefixes.Length]} {Nouns[(safeSectionIndex * 3 + 2) % Nouns.Length]}";

        return new CampaignSectionTheme(
            SurfaceStartHex: palette.SurfaceStartHex,
            SurfaceEndHex: palette.SurfaceEndHex,
            AccentHex: palette.AccentHex,
            AccentMutedHex: palette.AccentMutedHex,
            GlowHex: palette.GlowHex,
            StrokeHex: palette.StrokeHex,
            Subtitle: subtitle,
            OverlayIntensity: overlayIntensity,
            DecorativeDensity: decorativeDensity,
            Drama: drama);
    }

    private sealed record ThemePalette(
        string SurfaceStartHex,
        string SurfaceEndHex,
        string AccentHex,
        string AccentMutedHex,
        string GlowHex,
        string StrokeHex);
}
