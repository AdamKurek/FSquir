using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Fillsquir.Domain;

namespace Fillsquir.Services;

public sealed class HttpLeaderboardClient : ILeaderboardClient
{
    private readonly HttpClient httpClient;

    public HttpLeaderboardClient(HttpClient httpClient)
    {
        this.httpClient = httpClient;
    }

    public async Task<RecordSnapshot?> GetRecordAsync(PuzzleKey puzzleKey, string installId, CancellationToken cancellationToken = default)
    {
        using HttpRequestMessage request = new(
            HttpMethod.Get,
            $"api/v1/records/{puzzleKey.Level}/{puzzleKey.Seed}/{puzzleKey.RulesVersion}");
        request.Headers.TryAddWithoutValidation("X-Install-Id", installId);

        using HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        RecordResponseDto? dto = await response.Content.ReadFromJsonAsync<RecordResponseDto>(cancellationToken: cancellationToken);
        if (dto is null)
        {
            return null;
        }

        return new RecordSnapshot
        {
            WorldRecordCoveragePercent = dto.WorldRecordCoveragePercent,
            WorldRecordHolderInstallId = dto.WorldRecordHolderInstallId,
            PlayerBestCoveragePercent = dto.PlayerBestCoveragePercent,
            UpdatedAtUtc = dto.UpdatedAtUtc
        };
    }

    public async Task<SubmitScoreResult?> SubmitScoreAsync(ScoreSubmission submission, CancellationToken cancellationToken = default)
    {
        SubmitScoreRequestDto payload = new()
        {
            Level = submission.PuzzleKey.Level,
            Seed = submission.PuzzleKey.Seed,
            RulesVersion = submission.PuzzleKey.RulesVersion,
            InstallId = submission.InstallId,
            CoveragePercent = submission.CoveragePercent,
            AchievedAtUtc = submission.AchievedAtUtc,
            ClientAttemptId = submission.ClientAttemptId
        };

        using HttpResponseMessage response = await httpClient.PostAsJsonAsync("api/v1/scores", payload, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        SubmitScoreResponseDto? dto = await response.Content.ReadFromJsonAsync<SubmitScoreResponseDto>(cancellationToken: cancellationToken);
        if (dto is null)
        {
            return null;
        }

        return new SubmitScoreResult
        {
            IsNewWorldRecord = dto.IsNewWorldRecord,
            IsNewPersonalBest = dto.IsNewPersonalBest,
            WorldRecordCoveragePercent = dto.WorldRecordCoveragePercent,
            WorldRecordHolderInstallId = dto.WorldRecordHolderInstallId,
            PlayerBestCoveragePercent = dto.PlayerBestCoveragePercent,
            UpdatedAtUtc = dto.UpdatedAtUtc
        };
    }

    private sealed class RecordResponseDto
    {
        [JsonPropertyName("worldRecordCoveragePercent")]
        public decimal? WorldRecordCoveragePercent { get; set; }

        [JsonPropertyName("worldRecordHolderInstallId")]
        public string? WorldRecordHolderInstallId { get; set; }

        [JsonPropertyName("playerBestCoveragePercent")]
        public decimal? PlayerBestCoveragePercent { get; set; }

        [JsonPropertyName("updatedAtUtc")]
        public DateTimeOffset? UpdatedAtUtc { get; set; }
    }

    private sealed class SubmitScoreRequestDto
    {
        [JsonPropertyName("level")]
        public int Level { get; set; }

        [JsonPropertyName("seed")]
        public int Seed { get; set; }

        [JsonPropertyName("rulesVersion")]
        public string RulesVersion { get; set; } = string.Empty;

        [JsonPropertyName("installId")]
        public string InstallId { get; set; } = string.Empty;

        [JsonPropertyName("coveragePercent")]
        public decimal CoveragePercent { get; set; }

        [JsonPropertyName("achievedAtUtc")]
        public DateTimeOffset AchievedAtUtc { get; set; }

        [JsonPropertyName("clientAttemptId")]
        public Guid ClientAttemptId { get; set; }
    }

    private sealed class SubmitScoreResponseDto
    {
        [JsonPropertyName("isNewWorldRecord")]
        public bool IsNewWorldRecord { get; set; }

        [JsonPropertyName("isNewPersonalBest")]
        public bool IsNewPersonalBest { get; set; }

        [JsonPropertyName("worldRecordCoveragePercent")]
        public decimal? WorldRecordCoveragePercent { get; set; }

        [JsonPropertyName("worldRecordHolderInstallId")]
        public string? WorldRecordHolderInstallId { get; set; }

        [JsonPropertyName("playerBestCoveragePercent")]
        public decimal? PlayerBestCoveragePercent { get; set; }

        [JsonPropertyName("updatedAtUtc")]
        public DateTimeOffset? UpdatedAtUtc { get; set; }
    }
}
