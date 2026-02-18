using SkiaSharp;

namespace Fillsquir.Visuals;

public static class SkinCatalog
{
    public const string DefaultSkinId = "paper";

    private static readonly IReadOnlyList<SkinDefinition> skins =
    [
        new SkinDefinition(
            id: "paper",
            displayName: "Paper",
            boardColor: new SKColor(229, 219, 196),
            pieceBaseColor: new SKColor(214, 193, 153),
            outlineColor: new SKColor(72, 58, 36),
            hoverColor: new SKColor(208, 140, 78),
            stripColor: new SKColor(190, 171, 136),
            textureLowColor: new SKColor(245, 239, 221),
            textureHighColor: new SKColor(198, 171, 120),
            keyLightColor: new SKColor(255, 248, 231),
            fillLightColor: new SKColor(238, 222, 189),
            shadowColor: new SKColor(58, 46, 29),
            noiseScale: 9.5f,
            contrast: 1.15f,
            vignetteIntensity: 0.24f,
            accentIntensity: 0.10f,
            bevelStrength: 0.95f,
            shadowStrength: 0.82f),
        new SkinDefinition(
            id: "neon",
            displayName: "Neon",
            boardColor: new SKColor(18, 22, 35),
            pieceBaseColor: new SKColor(35, 44, 73),
            outlineColor: new SKColor(70, 230, 220),
            hoverColor: new SKColor(255, 90, 130),
            stripColor: new SKColor(24, 30, 53),
            textureLowColor: new SKColor(25, 40, 78),
            textureHighColor: new SKColor(81, 187, 255),
            keyLightColor: new SKColor(130, 255, 238),
            fillLightColor: new SKColor(93, 170, 255),
            shadowColor: new SKColor(6, 10, 20),
            noiseScale: 12.5f,
            contrast: 1.35f,
            vignetteIntensity: 0.12f,
            accentIntensity: 0.42f,
            bevelStrength: 1.15f,
            shadowStrength: 0.92f),
        new SkinDefinition(
            id: "nature",
            displayName: "Nature",
            boardColor: new SKColor(170, 190, 144),
            pieceBaseColor: new SKColor(123, 159, 102),
            outlineColor: new SKColor(45, 75, 38),
            hoverColor: new SKColor(212, 166, 93),
            stripColor: new SKColor(147, 171, 117),
            textureLowColor: new SKColor(120, 149, 94),
            textureHighColor: new SKColor(207, 223, 177),
            keyLightColor: new SKColor(232, 247, 208),
            fillLightColor: new SKColor(177, 209, 142),
            shadowColor: new SKColor(39, 58, 32),
            noiseScale: 8.0f,
            contrast: 1.05f,
            vignetteIntensity: 0.28f,
            accentIntensity: 0.17f,
            bevelStrength: 0.9f,
            shadowStrength: 0.78f)
    ];

    private static readonly Dictionary<string, SkinDefinition> byId =
        skins.ToDictionary(static skin => skin.Id, StringComparer.OrdinalIgnoreCase);

    public static IReadOnlyList<SkinDefinition> All => skins;

    public static bool TryGet(string? skinId, out SkinDefinition skin)
    {
        if (string.IsNullOrWhiteSpace(skinId))
        {
            skin = Resolve(DefaultSkinId);
            return false;
        }

        return byId.TryGetValue(skinId, out skin!);
    }

    public static SkinDefinition Resolve(string? skinId)
    {
        if (TryGet(skinId, out SkinDefinition? resolved))
        {
            return resolved;
        }

        return byId[DefaultSkinId];
    }
}
