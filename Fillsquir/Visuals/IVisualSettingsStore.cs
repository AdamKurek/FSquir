namespace Fillsquir.Visuals;

public interface IVisualSettingsStore
{
    Task<VisualSettings> LoadAsync(CancellationToken cancellationToken = default);
    Task SaveAsync(VisualSettings settings, CancellationToken cancellationToken = default);
}
