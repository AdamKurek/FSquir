namespace Fillsquir.Visuals;

public readonly record struct MaterialEffectFlags(
    bool UseGradient,
    bool UseTexture,
    bool UseShadow,
    bool UseAmbientOcclusion,
    bool UseBevel,
    bool UseRimHighlight,
    bool UseGlintOverlay);
