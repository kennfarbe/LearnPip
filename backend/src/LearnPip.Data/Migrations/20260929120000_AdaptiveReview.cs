using System;
using LearnPip.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LearnPip.Data.Migrations;

/// <summary>
/// Enthält die Schemaänderungen der Datenbankmigration AdaptiveReview.
/// </summary>
[DbContext(typeof(LearnPipDbContext))]
[Migration("20260929120000_AdaptiveReview")]
public sealed class AdaptiveReview : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable("LearningContents", columns: table => new
        {
            Id = table.Column<Guid>(type: "uuid", nullable: false),
            OwnerAccountId = table.Column<Guid>(type: "uuid", nullable: false),
            Title = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false)
        }, constraints: table =>
        {
            table.PrimaryKey("PK_LearningContents", x => x.Id);
            table.ForeignKey("FK_LearningContents_Accounts_OwnerAccountId", x => x.OwnerAccountId,
                "Accounts", "Id", onDelete: ReferentialAction.Restrict);
        });
        migrationBuilder.CreateIndex("IX_LearningContents_OwnerAccountId", "LearningContents", "OwnerAccountId");

        migrationBuilder.AddColumn<Guid>("LearningContentId", "Questions", type: "uuid", nullable: true);
        migrationBuilder.AddColumn<bool>("WasGuessed", "StudyAttempts", type: "boolean", nullable: false,
            defaultValue: false);
        migrationBuilder.AddColumn<DateTimeOffset>("ExplanationViewedAtUtc", "StudyAttempts",
            type: "timestamp with time zone", nullable: true);

        migrationBuilder.Sql("""
            INSERT INTO "LearningContents" ("Id", "OwnerAccountId", "Title")
            SELECT q."Id", q."OwnerAccountId",
                COALESCE((SELECT v."Topic" FROM "QuestionVersions" v
                    WHERE v."QuestionId" = q."Id" ORDER BY v."VersionNumber" DESC LIMIT 1), 'Lerninhalt')
            FROM "Questions" q;
            UPDATE "Questions" SET "LearningContentId" = "Id";
            """);
        migrationBuilder.CreateIndex("IX_Questions_LearningContentId", "Questions", "LearningContentId");
        migrationBuilder.AddForeignKey("FK_Questions_LearningContents_LearningContentId", "Questions",
            "LearningContentId", "LearningContents", principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.CreateTable("FrequentLearningContents", columns: table => new
        {
            AccountId = table.Column<Guid>(type: "uuid", nullable: false),
            LearningContentId = table.Column<Guid>(type: "uuid", nullable: false),
            CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
        }, constraints: table =>
        {
            table.PrimaryKey("PK_FrequentLearningContents", x => new { x.AccountId, x.LearningContentId });
            table.ForeignKey("FK_FrequentLearningContents_Accounts_AccountId", x => x.AccountId,
                "Accounts", "Id", onDelete: ReferentialAction.Cascade);
            table.ForeignKey("FK_FrequentLearningContents_LearningContents_LearningContentId",
                x => x.LearningContentId, "LearningContents", "Id", onDelete: ReferentialAction.Cascade);
        });
        migrationBuilder.CreateIndex("IX_FrequentLearningContents_LearningContentId",
            "FrequentLearningContents", "LearningContentId");
    }

    /// <inheritdoc />

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("FrequentLearningContents");
        migrationBuilder.DropForeignKey("FK_Questions_LearningContents_LearningContentId", "Questions");
        migrationBuilder.DropIndex("IX_Questions_LearningContentId", "Questions");
        migrationBuilder.DropColumn("LearningContentId", "Questions");
        migrationBuilder.DropColumn("WasGuessed", "StudyAttempts");
        migrationBuilder.DropColumn("ExplanationViewedAtUtc", "StudyAttempts");
        migrationBuilder.DropTable("LearningContents");
    }
}
