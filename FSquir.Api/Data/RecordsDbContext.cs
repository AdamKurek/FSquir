using Microsoft.EntityFrameworkCore;

namespace FSquir.Api.Data;

public sealed class RecordsDbContext : DbContext
{
    public RecordsDbContext(DbContextOptions<RecordsDbContext> options)
        : base(options)
    {
    }

    public DbSet<PlayerBestScore> PlayerBestScores => Set<PlayerBestScore>();
    public DbSet<WorldRecord> WorldRecords => Set<WorldRecord>();
    public DbSet<ScoreSubmissionLog> ScoreSubmissionLogs => Set<ScoreSubmissionLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PlayerBestScore>(entity =>
        {
            entity.ToTable("player_best_scores");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.InstallId).IsRequired().HasMaxLength(128);
            entity.Property(x => x.RulesVersion).IsRequired().HasMaxLength(32);
            entity.Property(x => x.CoveragePercent).HasPrecision(6, 4);
            entity.HasIndex(x => new { x.InstallId, x.Level, x.Seed, x.RulesVersion }).IsUnique();
            entity.HasCheckConstraint("ck_player_best_scores_coverage", "\"CoveragePercent\" >= 0 AND \"CoveragePercent\" <= 100");
            entity.HasIndex(x => x.InstallId);
        });

        modelBuilder.Entity<WorldRecord>(entity =>
        {
            entity.ToTable("world_records");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.RulesVersion).IsRequired().HasMaxLength(32);
            entity.Property(x => x.HolderInstallId).IsRequired().HasMaxLength(128);
            entity.Property(x => x.CoveragePercent).HasPrecision(6, 4);
            entity.HasIndex(x => new { x.Level, x.Seed, x.RulesVersion }).IsUnique();
            entity.HasCheckConstraint("ck_world_records_coverage", "\"CoveragePercent\" >= 0 AND \"CoveragePercent\" <= 100");
            entity.HasIndex(x => new { x.Level, x.Seed, x.RulesVersion });
        });

        modelBuilder.Entity<ScoreSubmissionLog>(entity =>
        {
            entity.ToTable("score_submission_logs");
            entity.HasKey(x => x.ClientAttemptId);
            entity.Property(x => x.InstallId).IsRequired().HasMaxLength(128);
            entity.Property(x => x.RulesVersion).IsRequired().HasMaxLength(32);
            entity.Property(x => x.CoveragePercent).HasPrecision(6, 4);
            entity.Property(x => x.WorldRecordHolderInstallId).HasMaxLength(128);
            entity.HasCheckConstraint("ck_score_submission_logs_coverage", "\"CoveragePercent\" >= 0 AND \"CoveragePercent\" <= 100");
            entity.HasIndex(x => new { x.Level, x.Seed, x.RulesVersion });
            entity.HasIndex(x => x.InstallId);
        });
    }
}
