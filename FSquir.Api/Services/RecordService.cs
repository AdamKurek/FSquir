using FSquir.Api.Contracts;
using FSquir.Api.Data;
using FSquir.Api.Validation;
using Microsoft.EntityFrameworkCore;

namespace FSquir.Api.Services;

public sealed class RecordService
{
    private readonly RecordsDbContext db;

    public RecordService(RecordsDbContext db)
    {
        this.db = db;
    }

    public async Task<RecordResponse?> GetRecordAsync(
        int level,
        int seed,
        string rulesVersion,
        string? installId,
        CancellationToken cancellationToken)
    {
        if (!RequestValidation.IsValidPuzzleKey(level, seed, rulesVersion))
        {
            return null;
        }

        WorldRecord? worldRecord = await db.WorldRecords
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.Level == level && x.Seed == seed && x.RulesVersion == rulesVersion,
                cancellationToken);

        PlayerBestScore? playerBest = null;
        if (RequestValidation.IsValidInstallId(installId ?? string.Empty))
        {
            playerBest = await db.PlayerBestScores
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    x => x.InstallId == installId
                        && x.Level == level
                        && x.Seed == seed
                        && x.RulesVersion == rulesVersion,
                    cancellationToken);
        }

        return new RecordResponse
        {
            WorldRecordCoveragePercent = worldRecord?.CoveragePercent,
            WorldRecordHolderInstallId = worldRecord?.HolderInstallId,
            PlayerBestCoveragePercent = playerBest?.CoveragePercent,
            UpdatedAtUtc = worldRecord?.UpdatedAtUtc ?? playerBest?.UpdatedAtUtc
        };
    }

    public async Task<SubmitScoreResponse?> SubmitScoreAsync(SubmitScoreRequest request, CancellationToken cancellationToken)
    {
        if (!RequestValidation.IsValidSubmission(request))
        {
            return null;
        }

        ScoreSubmissionLog? existingLog = await db.ScoreSubmissionLogs
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.ClientAttemptId == request.ClientAttemptId, cancellationToken);

        if (existingLog is not null)
        {
            return new SubmitScoreResponse
            {
                IsNewWorldRecord = existingLog.IsNewWorldRecord,
                IsNewPersonalBest = existingLog.IsNewPersonalBest,
                WorldRecordCoveragePercent = existingLog.WorldRecordCoveragePercent,
                WorldRecordHolderInstallId = existingLog.WorldRecordHolderInstallId,
                PlayerBestCoveragePercent = existingLog.PlayerBestCoveragePercent,
                UpdatedAtUtc = existingLog.ProcessedAtUtc
            };
        }

        DateTimeOffset now = DateTimeOffset.UtcNow;

        PlayerBestScore? playerBest = await db.PlayerBestScores
            .SingleOrDefaultAsync(
                x => x.InstallId == request.InstallId
                    && x.Level == request.Level
                    && x.Seed == request.Seed
                    && x.RulesVersion == request.RulesVersion,
                cancellationToken);

        bool isNewPersonalBest = false;
        if (playerBest is null)
        {
            playerBest = new PlayerBestScore
            {
                InstallId = request.InstallId,
                Level = request.Level,
                Seed = request.Seed,
                RulesVersion = request.RulesVersion,
                CoveragePercent = request.CoveragePercent,
                AchievedAtUtc = request.AchievedAtUtc,
                UpdatedAtUtc = now
            };
            db.PlayerBestScores.Add(playerBest);
            isNewPersonalBest = true;
        }
        else if (request.CoveragePercent > playerBest.CoveragePercent)
        {
            playerBest.CoveragePercent = request.CoveragePercent;
            playerBest.AchievedAtUtc = request.AchievedAtUtc;
            playerBest.UpdatedAtUtc = now;
            isNewPersonalBest = true;
        }

        WorldRecord? worldRecord = await db.WorldRecords
            .SingleOrDefaultAsync(
                x => x.Level == request.Level
                    && x.Seed == request.Seed
                    && x.RulesVersion == request.RulesVersion,
                cancellationToken);

        bool isNewWorldRecord = false;
        if (worldRecord is null)
        {
            worldRecord = new WorldRecord
            {
                Level = request.Level,
                Seed = request.Seed,
                RulesVersion = request.RulesVersion,
                CoveragePercent = request.CoveragePercent,
                HolderInstallId = request.InstallId,
                AchievedAtUtc = request.AchievedAtUtc,
                UpdatedAtUtc = now
            };
            db.WorldRecords.Add(worldRecord);
            isNewWorldRecord = true;
        }
        else if (request.CoveragePercent > worldRecord.CoveragePercent)
        {
            worldRecord.CoveragePercent = request.CoveragePercent;
            worldRecord.HolderInstallId = request.InstallId;
            worldRecord.AchievedAtUtc = request.AchievedAtUtc;
            worldRecord.UpdatedAtUtc = now;
            isNewWorldRecord = true;
        }

        SubmitScoreResponse response = new()
        {
            IsNewWorldRecord = isNewWorldRecord,
            IsNewPersonalBest = isNewPersonalBest,
            WorldRecordCoveragePercent = worldRecord.CoveragePercent,
            WorldRecordHolderInstallId = worldRecord.HolderInstallId,
            PlayerBestCoveragePercent = playerBest.CoveragePercent,
            UpdatedAtUtc = now
        };

        db.ScoreSubmissionLogs.Add(new ScoreSubmissionLog
        {
            ClientAttemptId = request.ClientAttemptId,
            InstallId = request.InstallId,
            Level = request.Level,
            Seed = request.Seed,
            RulesVersion = request.RulesVersion,
            CoveragePercent = request.CoveragePercent,
            AchievedAtUtc = request.AchievedAtUtc,
            ProcessedAtUtc = now,
            IsNewWorldRecord = response.IsNewWorldRecord,
            IsNewPersonalBest = response.IsNewPersonalBest,
            WorldRecordCoveragePercent = response.WorldRecordCoveragePercent,
            WorldRecordHolderInstallId = response.WorldRecordHolderInstallId,
            PlayerBestCoveragePercent = response.PlayerBestCoveragePercent
        });

        await db.SaveChangesAsync(cancellationToken);
        return response;
    }
}
