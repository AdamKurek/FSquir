namespace Fillsquir.Visuals;

public sealed class VisualSettingsState
{
    private readonly IVisualSettingsStore store;
    private readonly SemaphoreSlim gate = new(1, 1);

    private bool isLoaded;
    private VisualSettings current = new();

    public VisualSettingsState(IVisualSettingsStore store)
    {
        this.store = store;
    }

    public VisualSettings Current => current.Clone();

    public event EventHandler<VisualSettings>? Changed;

    public async Task<VisualSettings> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (isLoaded)
        {
            return current.Clone();
        }

        await gate.WaitAsync(cancellationToken);
        try
        {
            if (!isLoaded)
            {
                current = (await store.LoadAsync(cancellationToken)).Normalize();
                isLoaded = true;
            }

            return current.Clone();
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task UpdateAsync(VisualSettings settings, CancellationToken cancellationToken = default)
    {
        VisualSettings normalized = settings.Normalize();

        await gate.WaitAsync(cancellationToken);
        try
        {
            current = normalized;
            isLoaded = true;
            await store.SaveAsync(current, cancellationToken);
        }
        finally
        {
            gate.Release();
        }

        Changed?.Invoke(this, current.Clone());
    }
}
