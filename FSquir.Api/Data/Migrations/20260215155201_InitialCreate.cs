using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace FSquir.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "player_best_scores",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    InstallId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    Seed = table.Column<int>(type: "integer", nullable: false),
                    RulesVersion = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CoveragePercent = table.Column<decimal>(type: "numeric(6,4)", precision: 6, scale: 4, nullable: false),
                    AchievedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_player_best_scores", x => x.Id);
                    table.CheckConstraint("ck_player_best_scores_coverage", "\"CoveragePercent\" >= 0 AND \"CoveragePercent\" <= 100");
                });

            migrationBuilder.CreateTable(
                name: "score_submission_logs",
                columns: table => new
                {
                    ClientAttemptId = table.Column<Guid>(type: "uuid", nullable: false),
                    InstallId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    Seed = table.Column<int>(type: "integer", nullable: false),
                    RulesVersion = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CoveragePercent = table.Column<decimal>(type: "numeric(6,4)", precision: 6, scale: 4, nullable: false),
                    AchievedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ProcessedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IsNewWorldRecord = table.Column<bool>(type: "boolean", nullable: false),
                    IsNewPersonalBest = table.Column<bool>(type: "boolean", nullable: false),
                    WorldRecordCoveragePercent = table.Column<decimal>(type: "numeric", nullable: true),
                    WorldRecordHolderInstallId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    PlayerBestCoveragePercent = table.Column<decimal>(type: "numeric", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_score_submission_logs", x => x.ClientAttemptId);
                    table.CheckConstraint("ck_score_submission_logs_coverage", "\"CoveragePercent\" >= 0 AND \"CoveragePercent\" <= 100");
                });

            migrationBuilder.CreateTable(
                name: "world_records",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    Seed = table.Column<int>(type: "integer", nullable: false),
                    RulesVersion = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CoveragePercent = table.Column<decimal>(type: "numeric(6,4)", precision: 6, scale: 4, nullable: false),
                    HolderInstallId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    AchievedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_world_records", x => x.Id);
                    table.CheckConstraint("ck_world_records_coverage", "\"CoveragePercent\" >= 0 AND \"CoveragePercent\" <= 100");
                });

            migrationBuilder.CreateIndex(
                name: "IX_player_best_scores_InstallId",
                table: "player_best_scores",
                column: "InstallId");

            migrationBuilder.CreateIndex(
                name: "IX_player_best_scores_InstallId_Level_Seed_RulesVersion",
                table: "player_best_scores",
                columns: new[] { "InstallId", "Level", "Seed", "RulesVersion" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_score_submission_logs_InstallId",
                table: "score_submission_logs",
                column: "InstallId");

            migrationBuilder.CreateIndex(
                name: "IX_score_submission_logs_Level_Seed_RulesVersion",
                table: "score_submission_logs",
                columns: new[] { "Level", "Seed", "RulesVersion" });

            migrationBuilder.CreateIndex(
                name: "IX_world_records_Level_Seed_RulesVersion",
                table: "world_records",
                columns: new[] { "Level", "Seed", "RulesVersion" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "player_best_scores");

            migrationBuilder.DropTable(
                name: "score_submission_logs");

            migrationBuilder.DropTable(
                name: "world_records");
        }
    }
}
