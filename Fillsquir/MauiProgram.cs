using Fillsquir.Services;
using SkiaSharp.Views.Maui.Controls.Hosting;

namespace Fillsquir;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseSkiaSharp()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        builder.Services.AddSingleton<IProgressStore, JsonFileProgressStore>();
        builder.Services.AddSingleton<ISyncQueue, JsonFileSyncQueue>();
        builder.Services.AddSingleton<IScoreEvaluator, ScoreEvaluator>();
        builder.Services.AddSingleton<ICoordinateTransformer, CoordinateTransformer>();
        builder.Services.AddSingleton<IRecordSyncService, RecordSyncService>();

        builder.Services.AddSingleton<ILeaderboardClient>(_ =>
            new HttpLeaderboardClient(new HttpClient
            {
                BaseAddress = new Uri("http://localhost:5180/"),
                Timeout = TimeSpan.FromSeconds(2)
            }));

        return builder.Build();
    }
}
