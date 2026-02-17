using System.Text.Json;
using Fillsquir.Domain;
using Microsoft.Maui.Storage;

namespace Fillsquir.Services;

public sealed class JsonFileSyncQueue : ISyncQueue
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly SemaphoreSlim gate = new(1, 1);
    private readonly string queueFilePath;

    public JsonFileSyncQueue()
    {
        string baseDirectory = Path.Combine(FileSystem.AppDataDirectory, "progress");
        queueFilePath = Path.Combine(baseDirectory, "sync_queue.json");
        Directory.CreateDirectory(baseDirectory);
    }

    public async Task EnqueueAsync(ScoreSubmission submission, CancellationToken cancellationToken = default)
    {
        await gate.WaitAsync(cancellationToken);
        try
        {
            List<QueuedSubmission> queue = await LoadAsync(cancellationToken);
            bool exists = queue.Any(x => x.Submission.ClientAttemptId == submission.ClientAttemptId);
            if (exists)
            {
                return;
            }

            queue.Add(new QueuedSubmission
            {
                Submission = submission,
                NextAttemptAtUtc = DateTimeOffset.UtcNow
            });
            await SaveAsync(queue, cancellationToken);
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task<IReadOnlyList<QueuedSubmission>> PeekDueAsync(DateTimeOffset utcNow, int maxItems, CancellationToken cancellationToken = default)
    {
        await gate.WaitAsync(cancellationToken);
        try
        {
            List<QueuedSubmission> queue = await LoadAsync(cancellationToken);
            return queue
                .Where(x => x.NextAttemptAtUtc <= utcNow)
                .OrderBy(x => x.NextAttemptAtUtc)
                .ThenBy(x => x.AttemptCount)
                .Take(Math.Max(1, maxItems))
                .ToList();
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task MarkSuccessAsync(Guid clientAttemptId, CancellationToken cancellationToken = default)
    {
        await gate.WaitAsync(cancellationToken);
        try
        {
            List<QueuedSubmission> queue = await LoadAsync(cancellationToken);
            queue.RemoveAll(x => x.Submission.ClientAttemptId == clientAttemptId);
            await SaveAsync(queue, cancellationToken);
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task RescheduleFailureAsync(
        Guid clientAttemptId,
        DateTimeOffset nextAttemptAtUtc,
        int attemptCount,
        string? lastError,
        CancellationToken cancellationToken = default)
    {
        await gate.WaitAsync(cancellationToken);
        try
        {
            List<QueuedSubmission> queue = await LoadAsync(cancellationToken);
            QueuedSubmission? item = queue.FirstOrDefault(x => x.Submission.ClientAttemptId == clientAttemptId);
            if (item is null)
            {
                return;
            }

            item.NextAttemptAtUtc = nextAttemptAtUtc;
            item.AttemptCount = attemptCount;
            item.LastError = lastError;
            await SaveAsync(queue, cancellationToken);
        }
        finally
        {
            gate.Release();
        }
    }

    private async Task<List<QueuedSubmission>> LoadAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(queueFilePath))
        {
            return new List<QueuedSubmission>();
        }

        string json = await File.ReadAllTextAsync(queueFilePath, cancellationToken);
        return JsonSerializer.Deserialize<List<QueuedSubmission>>(json, JsonOptions) ?? new List<QueuedSubmission>();
    }

    private async Task SaveAsync(List<QueuedSubmission> queue, CancellationToken cancellationToken)
    {
        string json = JsonSerializer.Serialize(queue, JsonOptions);
        await File.WriteAllTextAsync(queueFilePath, json, cancellationToken);
    }
}
