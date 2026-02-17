using System.Text.Json;
using Fillsquir.Domain;
using Microsoft.Maui.Storage;

namespace Fillsquir.Services;

public sealed class JsonFileProgressStore : IProgressStore
{
    private const string InstallIdFileName = "install_id.txt";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly SemaphoreSlim gate = new(1, 1);
    private readonly string baseDirectory;
    private readonly string snapshotsDirectory;

    public JsonFileProgressStore()
    {
        baseDirectory = Path.Combine(FileSystem.AppDataDirectory, "progress");
        snapshotsDirectory = Path.Combine(baseDirectory, "snapshots");
    }

    public async Task<string> GetOrCreateInstallIdAsync(CancellationToken cancellationToken = default)
    {
        await gate.WaitAsync(cancellationToken);
        try
        {
            Directory.CreateDirectory(baseDirectory);
            string path = Path.Combine(baseDirectory, InstallIdFileName);
            if (File.Exists(path))
            {
                string existing = (await File.ReadAllTextAsync(path, cancellationToken)).Trim();
                if (!string.IsNullOrWhiteSpace(existing))
                {
                    return existing;
                }
            }

            string installId = Guid.NewGuid().ToString("D");
            await File.WriteAllTextAsync(path, installId, cancellationToken);
            return installId;
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task<LevelProgress> LoadLevelProgressAsync(PuzzleKey puzzleKey, CancellationToken cancellationToken = default)
    {
        await gate.WaitAsync(cancellationToken);
        try
        {
            Directory.CreateDirectory(baseDirectory);
            string path = ProgressFilePath(puzzleKey);
            if (!File.Exists(path))
            {
                return new LevelProgress
                {
                    PuzzleKey = puzzleKey,
                    BestCoveragePercent = 0m
                };
            }

            string json = await File.ReadAllTextAsync(path, cancellationToken);
            LevelProgress? loaded = JsonSerializer.Deserialize<LevelProgress>(json, JsonOptions);
            if (loaded is null)
            {
                return new LevelProgress
                {
                    PuzzleKey = puzzleKey
                };
            }

            loaded.PuzzleKey = puzzleKey;
            return loaded;
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task SaveLevelProgressAsync(LevelProgress progress, CancellationToken cancellationToken = default)
    {
        await gate.WaitAsync(cancellationToken);
        try
        {
            Directory.CreateDirectory(baseDirectory);
            string json = JsonSerializer.Serialize(progress, JsonOptions);
            await File.WriteAllTextAsync(ProgressFilePath(progress.PuzzleKey), json, cancellationToken);
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task<LevelSnapshot?> LoadSnapshotAsync(PuzzleKey puzzleKey, CancellationToken cancellationToken = default)
    {
        await gate.WaitAsync(cancellationToken);
        try
        {
            Directory.CreateDirectory(snapshotsDirectory);
            string path = SnapshotFilePath(puzzleKey);
            if (!File.Exists(path))
            {
                return null;
            }

            string json = await File.ReadAllTextAsync(path, cancellationToken);
            LevelSnapshot? snapshot = JsonSerializer.Deserialize<LevelSnapshot>(json, JsonOptions);
            if (snapshot is null)
            {
                return null;
            }

            snapshot.PuzzleKey = puzzleKey;
            return snapshot;
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task SaveSnapshotAsync(LevelSnapshot snapshot, CancellationToken cancellationToken = default)
    {
        await gate.WaitAsync(cancellationToken);
        try
        {
            Directory.CreateDirectory(snapshotsDirectory);
            string json = JsonSerializer.Serialize(snapshot, JsonOptions);
            await File.WriteAllTextAsync(SnapshotFilePath(snapshot.PuzzleKey), json, cancellationToken);
        }
        finally
        {
            gate.Release();
        }
    }

    private string ProgressFilePath(PuzzleKey puzzleKey)
    {
        return Path.Combine(baseDirectory, $"{Sanitize(PuzzleKey.BuildStableId(puzzleKey))}.json");
    }

    private string SnapshotFilePath(PuzzleKey puzzleKey)
    {
        return Path.Combine(snapshotsDirectory, $"{Sanitize(PuzzleKey.BuildStableId(puzzleKey))}.json");
    }

    private static string Sanitize(string value)
    {
        foreach (char invalid in Path.GetInvalidFileNameChars())
        {
            value = value.Replace(invalid, '_');
        }

        return value;
    }
}
