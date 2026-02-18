using SkiaSharp;

namespace Fillsquir.Visuals;

public sealed class SkinDefinition
{
    public SkinDefinition(
        string id,
        string displayName,
        SKColor boardColor,
        SKColor pieceBaseColor,
        SKColor outlineColor,
        SKColor hoverColor,
        SKColor stripColor,
        SKColor textureLowColor,
        SKColor textureHighColor,
        SKColor keyLightColor,
        SKColor fillLightColor,
        SKColor shadowColor,
        float noiseScale,
        float contrast,
        float vignetteIntensity,
        float accentIntensity,
        float bevelStrength,
        float shadowStrength)
    {
        Id = id;
        DisplayName = displayName;
        BoardColor = boardColor;
        PieceBaseColor = pieceBaseColor;
        OutlineColor = outlineColor;
        HoverColor = hoverColor;
        StripColor = stripColor;
        TextureLowColor = textureLowColor;
        TextureHighColor = textureHighColor;
        KeyLightColor = keyLightColor;
        FillLightColor = fillLightColor;
        ShadowColor = shadowColor;
        NoiseScale = noiseScale;
        Contrast = contrast;
        VignetteIntensity = vignetteIntensity;
        AccentIntensity = accentIntensity;
        BevelStrength = bevelStrength;
        ShadowStrength = shadowStrength;
    }

    public string Id { get; }
    public string DisplayName { get; }

    public SKColor BoardColor { get; }
    public SKColor PieceBaseColor { get; }
    public SKColor OutlineColor { get; }
    public SKColor HoverColor { get; }
    public SKColor StripColor { get; }

    public SKColor TextureLowColor { get; }
    public SKColor TextureHighColor { get; }
    public SKColor KeyLightColor { get; }
    public SKColor FillLightColor { get; }
    public SKColor ShadowColor { get; }

    public float NoiseScale { get; }
    public float Contrast { get; }
    public float VignetteIntensity { get; }
    public float AccentIntensity { get; }
    public float BevelStrength { get; }
    public float ShadowStrength { get; }
}
