using System;
using LearnPip.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LearnPip.Data.Migrations;

/// <summary>
/// Enthält die Schemaänderungen der Datenbankmigration ExamProfiles.
/// </summary>
[DbContext(typeof(LearnPipDbContext))]
[Migration("20260929180000_ExamProfiles")]
public sealed class ExamProfiles : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable("OfficialCatalogEditions", columns: table => new
        {
            Id = table.Column<Guid>(type: "uuid", nullable: false),
            Code = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
            Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
            Revision = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
            SourceUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
            License = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
            Attribution = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
            ChangedOn = table.Column<DateOnly>(type: "date", nullable: false),
            ImportedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
            QuestionsJson = table.Column<string>(type: "text", nullable: false)
        }, constraints: table => table.PrimaryKey("PK_OfficialCatalogEditions", x => x.Id));
        migrationBuilder.CreateIndex("IX_OfficialCatalogEditions_Code_Revision", "OfficialCatalogEditions",
            new[] { "Code", "Revision" }, unique: true);

        migrationBuilder.CreateTable("ExamProfileVersions", columns: table => new
        {
            Id = table.Column<Guid>(type: "uuid", nullable: false),
            Code = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
            Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
            AmateurClass = table.Column<string>(type: "character varying(1)", maxLength: 1, nullable: false),
            Version = table.Column<int>(type: "integer", nullable: false),
            CatalogEditionId = table.Column<Guid>(type: "uuid", nullable: false),
            PartsJson = table.Column<string>(type: "text", nullable: false),
            CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
        }, constraints: table =>
        {
            table.PrimaryKey("PK_ExamProfileVersions", x => x.Id);
            table.ForeignKey("FK_ExamProfileVersions_OfficialCatalogEditions_CatalogEditionId",
                x => x.CatalogEditionId, "OfficialCatalogEditions", "Id", onDelete: ReferentialAction.Restrict);
        });
        migrationBuilder.CreateIndex("IX_ExamProfileVersions_CatalogEditionId", "ExamProfileVersions",
            "CatalogEditionId");
        migrationBuilder.CreateIndex("IX_ExamProfileVersions_Code_Version", "ExamProfileVersions",
            new[] { "Code", "Version" }, unique: true);

        migrationBuilder.CreateTable("ExamSimulations", columns: table => new
        {
            Id = table.Column<Guid>(type: "uuid", nullable: false),
            AccountId = table.Column<Guid>(type: "uuid", nullable: false),
            ProfileVersionId = table.Column<Guid>(type: "uuid", nullable: false),
            SnapshotJson = table.Column<string>(type: "text", nullable: false),
            AnswersJson = table.Column<string>(type: "text", nullable: false),
            CurrentPartIndex = table.Column<int>(type: "integer", nullable: false),
            StartedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
            PartStartedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
            CompletedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
            ResultJson = table.Column<string>(type: "text", nullable: true)
        }, constraints: table =>
        {
            table.PrimaryKey("PK_ExamSimulations", x => x.Id);
            table.ForeignKey("FK_ExamSimulations_Accounts_AccountId", x => x.AccountId,
                "Accounts", "Id", onDelete: ReferentialAction.Restrict);
            table.ForeignKey("FK_ExamSimulations_ExamProfileVersions_ProfileVersionId",
                x => x.ProfileVersionId, "ExamProfileVersions", "Id", onDelete: ReferentialAction.Restrict);
        });
        migrationBuilder.CreateIndex("IX_ExamSimulations_AccountId_StartedAtUtc", "ExamSimulations",
            new[] { "AccountId", "StartedAtUtc" });
        migrationBuilder.CreateIndex("IX_ExamSimulations_ProfileVersionId", "ExamSimulations", "ProfileVersionId");

        migrationBuilder.CreateTable("AccountExamCredits", columns: table => new
        {
            AccountId = table.Column<Guid>(type: "uuid", nullable: false),
            Code = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
            ReportedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
        }, constraints: table =>
        {
            table.PrimaryKey("PK_AccountExamCredits", x => new { x.AccountId, x.Code });
            table.ForeignKey("FK_AccountExamCredits_Accounts_AccountId", x => x.AccountId,
                "Accounts", "Id", onDelete: ReferentialAction.Cascade);
        });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("AccountExamCredits");
        migrationBuilder.DropTable("ExamSimulations");
        migrationBuilder.DropTable("ExamProfileVersions");
        migrationBuilder.DropTable("OfficialCatalogEditions");
    }
}
