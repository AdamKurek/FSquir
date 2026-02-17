using Fillsquir.Domain;

namespace Fillsquir.Services;

public interface IRecordSyncService
{
    Task EnqueueBestScoreAsync(ScoreSubmission submission, CancellationToken cancellationToken = default);
    Task TriggerSyncAsync(CancellationToken cancellationToken = default);
}
