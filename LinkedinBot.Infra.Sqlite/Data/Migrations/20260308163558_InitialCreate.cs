using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LinkedinBot.Infra.Sqlite.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "job_history",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    JobUrl = table.Column<string>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", nullable: false),
                    Company = table.Column<string>(type: "TEXT", nullable: false),
                    Location = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    Reason = table.Column<string>(type: "TEXT", nullable: true),
                    ConfidenceScore = table.Column<double>(type: "REAL", nullable: true),
                    AiMessage = table.Column<string>(type: "TEXT", nullable: true),
                    KeyMatchingSkills = table.Column<string>(type: "TEXT", nullable: true),
                    MissingRequirements = table.Column<string>(type: "TEXT", nullable: true),
                    AnalyzedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    AppliedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_job_history", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_job_history_JobUrl",
                table: "job_history",
                column: "JobUrl",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "job_history");
        }
    }
}
