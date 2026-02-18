namespace Fillsquir.Visuals;

public sealed class VisualSettings
{
    public const float DefaultDepthIntensity = 0.65f;
    public const float DefaultStripOpacity = 0.58f;
    public const float DefaultStripFrostAmount = 0.35f;

    public string SelectedSkinId { get; set; } = SkinCatalog.DefaultSkinId;
    public GraphicsQualityTier QualityTier { get; set; } = GraphicsQualityTier.Medium;
    public TextureMappingMode MappingMode { get; set; } = TextureMappingMode.WorldLocked;
    public bool ShowStrongOutlines { get; set; } = true;
    public float DepthIntensity { get; set; } = DefaultDepthIntensity;
    public float StripOpacity { get; set; } = DefaultStripOpacity;
    public float StripFrostAmount { get; set; } = DefaultStripFrostAmount;

    public VisualSettings Clone()
    {
        return new VisualSettings
        {
            SelectedSkinId = SelectedSkinId,
            QualityTier = QualityTier,
            MappingMode = MappingMode,
            ShowStrongOutlines = ShowStrongOutlines,
            DepthIntensity = DepthIntensity,
            StripOpacity = StripOpacity,
            StripFrostAmount = StripFrostAmount
        };
    }

    public VisualSettings Normalize()
    {
        var normalized = Clone();
        normalized.SelectedSkinId = SkinCatalog.Resolve(SelectedSkinId).Id;
        normalized.DepthIntensity = Math.Clamp(DepthIntensity, 0.2f, 1.0f);
        normalized.StripOpacity = Math.Clamp(StripOpacity, 0.25f, 0.9f);
        normalized.StripFrostAmount = Math.Clamp(StripFrostAmount, 0f, 1f);
        return normalized;
    }
}
