using Fillsquir.Visuals;

namespace Fillsquir;

public partial class SettingsPage : ContentPage
{
    private readonly VisualSettingsState visualSettingsState;
    private readonly IReadOnlyList<SkinDefinition> skins;
    private bool isHydrating;

    public SettingsPage()
    {
        InitializeComponent();
        Shell.SetNavBarIsVisible(this, false);

        IServiceProvider? services = App.Services;
        visualSettingsState = services?.GetService(typeof(VisualSettingsState)) as VisualSettingsState
            ?? new VisualSettingsState(new VisualSettingsStore());

        skins = SkinCatalog.All;

        skinPicker.ItemsSource = skins.Select(static skin => skin.DisplayName).ToList();
        qualityPicker.ItemsSource = Enum.GetNames<GraphicsQualityTier>().ToList();
        mappingPicker.ItemsSource = new List<string>
        {
            "World-Locked",
            "Piece-Local"
        };
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        isHydrating = true;
        try
        {
            VisualSettings settings = await visualSettingsState.LoadAsync();
            HydrateControls(settings);
        }
        finally
        {
            isHydrating = false;
        }
    }

    private void HydrateControls(VisualSettings settings)
    {
        VisualSettings normalized = settings.Normalize();

        string selectedSkinId = SkinCatalog.Resolve(normalized.SelectedSkinId).Id;
        skinPicker.SelectedIndex = skins
            .Select((skin, index) => new { skin.Id, Index = index })
            .FirstOrDefault(entry => string.Equals(entry.Id, selectedSkinId, StringComparison.OrdinalIgnoreCase))?.Index
            ?? 0;

        qualityPicker.SelectedIndex = (int)normalized.QualityTier;
        mappingPicker.SelectedIndex = normalized.MappingMode == TextureMappingMode.WorldLocked ? 0 : 1;
        outlineSwitch.IsToggled = normalized.ShowStrongOutlines;
        depthSlider.Value = normalized.DepthIntensity;
        stripOpacitySlider.Value = normalized.StripOpacity;
        stripFrostSlider.Value = normalized.StripFrostAmount;

        UpdateSliderLabels();
    }

    private async void AnySettingChanged(object sender, EventArgs e)
    {
        if (isHydrating)
        {
            return;
        }

        await PersistSettingsAsync();
    }

    private async void AnySliderChanged(object sender, ValueChangedEventArgs e)
    {
        UpdateSliderLabels();

        if (isHydrating)
        {
            return;
        }

        await PersistSettingsAsync();
    }

    private async Task PersistSettingsAsync()
    {
        int skinIndex = Math.Clamp(skinPicker.SelectedIndex, 0, skins.Count - 1);
        int qualityIndex = Math.Clamp(qualityPicker.SelectedIndex, 0, Enum.GetValues<GraphicsQualityTier>().Length - 1);

        TextureMappingMode mappingMode = mappingPicker.SelectedIndex == 1
            ? TextureMappingMode.PieceLocal
            : TextureMappingMode.WorldLocked;

        VisualSettings updated = new()
        {
            SelectedSkinId = skins[skinIndex].Id,
            QualityTier = (GraphicsQualityTier)qualityIndex,
            MappingMode = mappingMode,
            ShowStrongOutlines = outlineSwitch.IsToggled,
            DepthIntensity = (float)depthSlider.Value,
            StripOpacity = (float)stripOpacitySlider.Value,
            StripFrostAmount = (float)stripFrostSlider.Value
        };

        await visualSettingsState.UpdateAsync(updated);
    }

    private void UpdateSliderLabels()
    {
        depthValueLabel.Text = $"Depth Intensity: {depthSlider.Value:0.00}";
        stripOpacityValueLabel.Text = $"Strip Opacity: {stripOpacitySlider.Value:0.00}";
        stripFrostValueLabel.Text = $"Strip Frost: {stripFrostSlider.Value:0.00}";
    }

    private async void BackButton_Clicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }
}
