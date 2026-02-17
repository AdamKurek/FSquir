using Fillsquir.Domain;

namespace Fillsquir.Services;

public interface IProgressStore
{
    Task<string> GetOrCreateInstallIdAsync(CancellationToken cancellationToken = default);
    Task<LevelProgress> LoadLevelProgressAsync(PuzzleKey puzzleKey, CancellationToken cancellationToken = default);
    Task SaveLevelProgressAsync(LevelProgress progress, CancellationToken cancellationToken = default);
    Task<LevelSnapshot?> LoadSnapshotAsync(PuzzleKey puzzleKey, CancellationToken cancellationToken = default);
    Task SaveSnapshotAsync(LevelSnapshot snapshot, CancellationToken cancellationToken = default);
}
