namespace Fillsquir;

public partial class MainPage : ContentPage
{
	public MainPage()
	{
        InitializeComponent();
        Shell.SetNavBarIsVisible(this, false);

    }

    private async void Button_Clicked(object sender, EventArgs e)
    {
		await Shell.Current.GoToAsync("//GameSelectionPage",true);
    }
}