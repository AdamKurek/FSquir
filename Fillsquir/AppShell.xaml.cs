namespace Fillsquir;

public partial class AppShell : Shell
{
	public AppShell()
	{
        //Routing.RegisterRoute("GamePage{levelNumber}", typeof(int));
        InitializeComponent();
        Routing.RegisterRoute(nameof(SettingsPage), typeof(SettingsPage));
	}
}
