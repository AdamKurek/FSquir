namespace Fillsquir.Controls;

internal sealed class GameHudController
{
    private readonly Label levelStatusLabel;
    private readonly Label coverageStatusLabel;
    private readonly Label recordStatusLabel;
    private readonly Label syncStatusLabel;
    private readonly ProgressBar coverageProgressBar;
    private readonly Border statusToast;
    private readonly Label statusToastLabel;

    private CancellationTokenSource? toastCts;

    internal GameHudController(
        Label levelStatusLabel,
        Label coverageStatusLabel,
        Label recordStatusLabel,
        Label syncStatusLabel,
        ProgressBar coverageProgressBar,
        Border statusToast,
        Label statusToastLabel)
    {
        this.levelStatusLabel = levelStatusLabel;
        this.coverageStatusLabel = coverageStatusLabel;
        this.recordStatusLabel = recordStatusLabel;
        this.syncStatusLabel = syncStatusLabel;
        this.coverageProgressBar = coverageProgressBar;
        this.statusToast = statusToast;
        this.statusToastLabel = statusToastLabel;
    }

    internal void Update(
        int level,
        decimal coveragePercent,
        decimal bestCoveragePercent,
        decimal? worldRecordCoveragePercent,
        int stars)
    {
        string world = worldRecordCoveragePercent.HasValue
            ? $"{worldRecordCoveragePercent.Value:F2}%"
            : "--";

        levelStatusLabel.Text = $"Level {level:00}";
        coverageStatusLabel.Text = $"Coverage {coveragePercent:F2}%";
        coverageProgressBar.Progress = Math.Clamp((double)coveragePercent / 100d, 0d, 1d);
        recordStatusLabel.Text =
            $"Best {bestCoveragePercent:F2}% | World {world} | Stars {stars}/3";
    }

    internal void SetSyncStatus(string status)
    {
        syncStatusLabel.Text = status;
        syncStatusLabel.TextColor = status switch
        {
            "Synced" => Microsoft.Maui.Graphics.Color.FromArgb("#8FE8FF"),
            "Syncing" or "Loading" => Microsoft.Maui.Graphics.Color.FromArgb("#FFD36A"),
            _ => Microsoft.Maui.Graphics.Color.FromArgb("#9FB0C4")
        };
    }

    internal Task ShowToastAsync(string message)
    {
        toastCts?.Cancel();
        toastCts = new CancellationTokenSource();
        CancellationToken token = toastCts.Token;

        return MainThread.InvokeOnMainThreadAsync(async () =>
        {
            statusToastLabel.Text = message;
            statusToast.IsVisible = true;
            await statusToast.FadeToAsync(1, 120, Easing.CubicOut);

            try
            {
                await Task.Delay(1800, token);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            if (token.IsCancellationRequested)
            {
                return;
            }

            await statusToast.FadeToAsync(0, 180, Easing.CubicIn);
            if (!token.IsCancellationRequested)
            {
                statusToast.IsVisible = false;
            }
        });
    }

    internal void CancelToast()
    {
        toastCts?.Cancel();
    }
}
