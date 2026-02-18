using Microsoft.Maui.Storage;

namespace Fillsquir.Visuals;

public sealed class VisualSettingsStore : IVisualSettingsStore
{
    private const string SkinIdKey = "visual.skin";
    private const string QualityTierKey = "visual.quality";
    private const string MappingModeKey = "visual.mapping";
    private const string ShowStrongOutlinesKey = "visual.outlines";
    private const string DepthIntensityKey = "visual.depth_intensity";
    private const string StripOpacityKey = "visual.strip_opacity";
    private const string StripFrostAmountKey = "visual.strip_frost";

    private readonly IPreferences preferences;

    public VisualSettingsStore()
        : this(Preferences.Default)
    {
    }

    public VisualSettingsStore(IPreferences preferences)
    {
        this.preferences = preferences;
    }

    public Task<VisualSettings> LoadAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        string skinId = preferences.Get(SkinIdKey, SkinCatalog.DefaultSkinId);
        string qualityValue = preferences.Get(QualityTierKey, GraphicsQualityTier.Medium.ToString());
        string mappingValue = preferences.Get(MappingModeKey, TextureMappingMode.WorldLocked.ToString());
        bool showStrongOutlines = preferences.Get(ShowStrongOutlinesKey, true);
        float depthIntensity = preferences.Get(DepthIntensityKey, VisualSettings.DefaultDepthIntensity);
        float stripOpacity = preferences.Get(StripOpacityKey, VisualSettings.DefaultStripOpacity);
        float stripFrostAmount = preferences.Get(StripFrostAmountKey, VisualSettings.DefaultStripFrostAmount);

        if (!Enum.TryParse(qualityValue, ignoreCase: true, out GraphicsQualityTier qualityTier))
        {
            qualityTier = GraphicsQualityTier.Medium;
        }

        if (!Enum.TryParse(mappingValue, ignoreCase: true, out TextureMappingMode mappingMode))
        {
            mappingMode = TextureMappingMode.WorldLocked;
        }

        VisualSettings settings = new()
        {
            SelectedSkinId = skinId,
            QualityTier = qualityTier,
            MappingMode = mappingMode,
            ShowStrongOutlines = showStrongOutlines,
            DepthIntensity = depthIntensity,
            StripOpacity = stripOpacity,
            StripFrostAmount = stripFrostAmount
        };

        return Task.FromResult(settings.Normalize());
    }

    public Task SaveAsync(VisualSettings settings, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        VisualSettings normalized = settings.Normalize();

        preferences.Set(SkinIdKey, normalized.SelectedSkinId);
        preferences.Set(QualityTierKey, normalized.QualityTier.ToString());
        preferences.Set(MappingModeKey, normalized.MappingMode.ToString());
        preferences.Set(ShowStrongOutlinesKey, normalized.ShowStrongOutlines);
        preferences.Set(DepthIntensityKey, normalized.DepthIntensity);
        preferences.Set(StripOpacityKey, normalized.StripOpacity);
        preferences.Set(StripFrostAmountKey, normalized.StripFrostAmount);

        return Task.CompletedTask;
    }
}
