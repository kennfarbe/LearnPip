using LearnPip.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace LearnPip.Data.Migrations;

/// <summary>
/// Enthält die Schemaänderungen der Datenbankmigration QuestionTranslations.
/// </summary>
[DbContext(typeof(LearnPipDbContext))]
[Migration("20260929210000_QuestionTranslations")]
public sealed class QuestionTranslations : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable("QuestionTranslations", columns: table => new
        {
            Id = table.Column<Guid>(type: "uuid", nullable: false),
            QuestionVersionId = table.Column<Guid>(type: "uuid", nullable: false),
            Language = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
            Revision = table.Column<int>(type: "integer", nullable: false),
            Status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
            PayloadJson = table.Column<string>(type: "character varying(65536)", maxLength: 65536, nullable: false),
            Source = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
            License = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
            Provenance = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
            CreatedByAccountId = table.Column<Guid>(type: "uuid", nullable: false),
            CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
            ApprovedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
        }, constraints: table =>
        {
            table.PrimaryKey("PK_QuestionTranslations", x => x.Id);
            table.ForeignKey("FK_QuestionTranslations_QuestionVersions_QuestionVersionId", x => x.QuestionVersionId,
                "QuestionVersions", "Id", onDelete: ReferentialAction.Cascade);
            table.ForeignKey("FK_QuestionTranslations_Accounts_CreatedByAccountId", x => x.CreatedByAccountId,
                "Accounts", "Id", onDelete: ReferentialAction.Restrict);
        });
        migrationBuilder.CreateIndex("IX_QuestionTranslations_QuestionVersionId_Language_Revision",
            "QuestionTranslations", new[] { "QuestionVersionId", "Language", "Revision" }, unique: true);
        migrationBuilder.CreateIndex("IX_QuestionTranslations_CreatedByAccountId",
            "QuestionTranslations", "CreatedByAccountId");

        migrationBuilder.CreateTable("TranslationReports", columns: table => new
        {
            Id = table.Column<Guid>(type: "uuid", nullable: false),
            QuestionTranslationId = table.Column<Guid>(type: "uuid", nullable: false),
            AccountId = table.Column<Guid>(type: "uuid", nullable: false),
            Details = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
            CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
        }, constraints: table =>
        {
            table.PrimaryKey("PK_TranslationReports", x => x.Id);
            table.ForeignKey("FK_TranslationReports_QuestionTranslations_QuestionTranslationId",
                x => x.QuestionTranslationId, "QuestionTranslations", "Id", onDelete: ReferentialAction.Cascade);
            table.ForeignKey("FK_TranslationReports_Accounts_AccountId", x => x.AccountId,
                "Accounts", "Id", onDelete: ReferentialAction.Restrict);
        });
        migrationBuilder.CreateIndex("IX_TranslationReports_QuestionTranslationId_CreatedAtUtc",
            "TranslationReports", new[] { "QuestionTranslationId", "CreatedAtUtc" });
        migrationBuilder.CreateIndex("IX_TranslationReports_AccountId", "TranslationReports", "AccountId");
    }

    /// <inheritdoc />

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("TranslationReports");
        migrationBuilder.DropTable("QuestionTranslations");
    }
}
