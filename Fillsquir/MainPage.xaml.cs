namespace Fillsquir;

public partial class MainPage : ContentPage
{
    private bool hasAnimatedIn;

    public MainPage()
    {
        InitializeComponent();
        Shell.SetNavBarIsVisible(this, false);

        PrimeForIntro(heroPanel, translateY: 22);
        PrimeForIntro(metricsPanel, translateY: 16);
        PrimeForIntro(runLaunchPanel, translateY: 22);
        PrimeForIntro(multiplayerPanel, translateY: 28);
        PrimeForIntro(settingsPanel, translateY: 34);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (hasAnimatedIn)
        {
            return;
        }

        hasAnimatedIn = true;
        await AnimateIntroAsync();
    }

    private async void SinglePlayerButton_Clicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//GameSelectionPage", true);
    }

    private async void SettingsButton_Clicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(SettingsPage));
    }

    private async void MultiplayerButton_Clicked(object sender, EventArgs e)
    {
        await DisplayAlertAsync("Multiplayer", "Coming soon.", "OK");
    }

    private static void PrimeForIntro(VisualElement element, double translateY)
    {
        element.Opacity = 0;
        element.TranslationY = translateY;
        element.Scale = 0.985;
    }

    private async Task AnimateIntroAsync()
    {
        await heroPanel.FadeToAsync(1, 260, Easing.CubicOut);
        await Task.WhenAll(
            heroPanel.TranslateToAsync(0, 0, 320, Easing.CubicOut),
            heroPanel.ScaleToAsync(1, 320, Easing.CubicOut),
            metricsPanel.FadeToAsync(1, 260, Easing.CubicOut),
            metricsPanel.TranslateToAsync(0, 0, 300, Easing.CubicOut),
            metricsPanel.ScaleToAsync(1, 300, Easing.CubicOut));

        await AnimateCardAsync(runLaunchPanel, 0);
        await AnimateCardAsync(multiplayerPanel, 70);
        await AnimateCardAsync(settingsPanel, 140);
    }

    private static async Task AnimateCardAsync(VisualElement element, uint delayMs)
    {
        if (delayMs > 0)
        {
            await Task.Delay((int)delayMs);
        }

        await Task.WhenAll(
            element.FadeToAsync(1, 240, Easing.CubicOut),
            element.TranslateToAsync(0, 0, 300, Easing.CubicOut),
            element.ScaleToAsync(1, 300, Easing.CubicOut));
    }
}
