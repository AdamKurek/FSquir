using Fillsquir.Services;

namespace Fillsquir;

public partial class App : Application
{
    internal static IServiceProvider? Services { get; private set; }
    private readonly CancellationTokenSource syncCts = new();
    private Page? rootPage;

    public App(IServiceProvider services, IRecordSyncService recordSyncService)
    {
        InitializeComponent();

        Services = services;
        rootPage = new AppShell();

        _ = Task.Run(async () =>
        {
            try
            {
                await recordSyncService.TriggerSyncAsync();

                using PeriodicTimer timer = new(TimeSpan.FromSeconds(20));
                while (await timer.WaitForNextTickAsync(syncCts.Token))
                {
                    await recordSyncService.TriggerSyncAsync(syncCts.Token);
                }
            }
            catch
            {
                // Startup sync is best effort only.
            }
        });
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var window = new Window(rootPage ?? new AppShell());
        window.Destroying += (_, _) => syncCts.Cancel();
        return window;
    }
}
