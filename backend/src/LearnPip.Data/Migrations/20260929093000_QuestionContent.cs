using System;
using LearnPip.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LearnPip.Data.Migrations;

/// <summary>
/// Enthält die Schemaänderungen der Datenbankmigration QuestionContent.
/// </summary>
[DbContext(typeof(LearnPipDbContext))]
[Migration("20260929093000_QuestionContent")]
public sealed class QuestionContent : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>("SelectionMode", "QuestionVersions", type: "character varying(16)",
            maxLength: 16, nullable: false, defaultValue: "single");
        migrationBuilder.AddColumn<string>("Subject", "QuestionVersions", type: "character varying(120)",
            maxLength: 120, nullable: false, defaultValue: "");
        migrationBuilder.AddColumn<string>("Topic", "QuestionVersions", type: "character varying(120)",
            maxLength: 120, nullable: false, defaultValue: "");
        migrationBuilder.AddColumn<string>("Language", "QuestionVersions", type: "character varying(35)",
            maxLength: 35, nullable: false, defaultValue: "de");
        migrationBuilder.AddColumn<string>("Source", "QuestionVersions", type: "character varying(500)",
            maxLength: 500, nullable: false, defaultValue: "");
        migrationBuilder.AddColumn<string>("License", "QuestionVersions", type: "character varying(120)",
            maxLength: 120, nullable: false, defaultValue: "");
        migrationBuilder.AddColumn<DateTimeOffset>("PublishedAtUtc", "QuestionVersions",
            type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP");

        migrationBuilder.CreateTable(
            name: "QuestionContentBlocks",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                QuestionVersionId = table.Column<Guid>(type: "uuid", nullable: true),
                AnswerOptionId = table.Column<Guid>(type: "uuid", nullable: true),
                Section = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                SortOrder = table.Column<int>(type: "integer", nullable: false),
                Kind = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                Text = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                MediaAssetId = table.Column<Guid>(type: "uuid", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_QuestionContentBlocks", x => x.Id);
                table.CheckConstraint("CK_QuestionContentBlocks_Owner",
                    "(\"QuestionVersionId\" IS NOT NULL AND \"AnswerOptionId\" IS NULL) OR " +
                    "(\"QuestionVersionId\" IS NULL AND \"AnswerOptionId\" IS NOT NULL)");
                table.ForeignKey("FK_QuestionContentBlocks_QuestionVersions_QuestionVersionId",
                    x => x.QuestionVersionId, "QuestionVersions", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_QuestionContentBlocks_AnswerOptions_AnswerOptionId",
                    x => x.AnswerOptionId, "AnswerOptions", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_QuestionContentBlocks_MediaAssets_MediaAssetId",
                    x => x.MediaAssetId, "MediaAssets", "Id", onDelete: ReferentialAction.Restrict);
            });
        migrationBuilder.CreateIndex("IX_QuestionContentBlocks_QuestionVersionId_Section_SortOrder",
            "QuestionContentBlocks", new[] { "QuestionVersionId", "Section", "SortOrder" },
            unique: true, filter: "\"QuestionVersionId\" IS NOT NULL");
        migrationBuilder.CreateIndex("IX_QuestionContentBlocks_AnswerOptionId_SortOrder",
            "QuestionContentBlocks", new[] { "AnswerOptionId", "SortOrder" },
            unique: true, filter: "\"AnswerOptionId\" IS NOT NULL");
        migrationBuilder.CreateIndex("IX_QuestionContentBlocks_MediaAssetId", "QuestionContentBlocks", "MediaAssetId");

        migrationBuilder.CreateTable(
            name: "StudyAttemptSelections",
            columns: table => new
            {
                StudyAttemptId = table.Column<Guid>(type: "uuid", nullable: false),
                AnswerOptionId = table.Column<Guid>(type: "uuid", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_StudyAttemptSelections", x => new { x.StudyAttemptId, x.AnswerOptionId });
                table.ForeignKey("FK_StudyAttemptSelections_StudyAttempts_StudyAttemptId",
                    x => x.StudyAttemptId, "StudyAttempts", "Id", onDelete: ReferentialAction.Cascade);
                table.ForeignKey("FK_StudyAttemptSelections_AnswerOptions_AnswerOptionId",
                    x => x.AnswerOptionId, "AnswerOptions", "Id", onDelete: ReferentialAction.Restrict);
            });
        migrationBuilder.CreateIndex("IX_StudyAttemptSelections_AnswerOptionId",
            "StudyAttemptSelections", "AnswerOptionId");
    }

    /// <inheritdoc />

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("QuestionContentBlocks");
        migrationBuilder.DropTable("StudyAttemptSelections");
        foreach (var column in new[] { "SelectionMode", "Subject", "Topic", "Language",
            "Source", "License", "PublishedAtUtc" })
            migrationBuilder.DropColumn(column, "QuestionVersions");
    }
}
