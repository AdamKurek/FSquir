using Fillsquir.Domain;

namespace Fillsquir.Services;

public interface ILeaderboardClient
{
    Task<RecordSnapshot?> GetRecordAsync(PuzzleKey puzzleKey, string installId, CancellationToken cancellationToken = default);
    Task<SubmitScoreResult?> SubmitScoreAsync(ScoreSubmission submission, CancellationToken cancellationToken = default);
}
