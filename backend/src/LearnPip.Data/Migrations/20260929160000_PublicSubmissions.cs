using System;
using LearnPip.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LearnPip.Data.Migrations;

/// <summary>
/// Enthält die Schemaänderungen der Datenbankmigration PublicSubmissions.
/// </summary>
[DbContext(typeof(LearnPipDbContext))]
[Migration("20260929160000_PublicSubmissions")]
public sealed class PublicSubmissions : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>("AuthorAttribution", "QuestionVersions",
            type: "character varying(120)", maxLength: 120, nullable: false, defaultValue: "");
        // The older direct-public flow is closed before submissions are accepted.
        migrationBuilder.Sql("UPDATE \"QuestionVersions\" SET \"Visibility\" = 'private' WHERE \"Visibility\" = 'public'");
        migrationBuilder.CreateTable("PublicSubmissions", columns: table => new
        {
            QuestionVersionId = table.Column<Guid>(type: "uuid", nullable: false),
            AccountId = table.Column<Guid>(type: "uuid", nullable: false),
            Status = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
            LicenseChoice = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
            AuthorAttribution = table.Column<string>(type: "character varying(120)", maxLength: 120,
                nullable: false),
            AgeDeclaration = table.Column<string>(type: "character varying(16)", maxLength: 16,
                nullable: false),
            RightsConfirmed = table.Column<bool>(type: "boolean", nullable: false),
            ImageRightsConfirmed = table.Column<bool>(type: "boolean", nullable: false),
            SubmittedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
            ReviewedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
            ReviewedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
            ReviewNote = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
        }, constraints: table =>
        {
            table.PrimaryKey("PK_PublicSubmissions", x => x.QuestionVersionId);
            table.ForeignKey("FK_PublicSubmissions_QuestionVersions_QuestionVersionId", x => x.QuestionVersionId,
                "QuestionVersions", "Id", onDelete: ReferentialAction.Cascade);
            table.ForeignKey("FK_PublicSubmissions_Accounts_AccountId", x => x.AccountId,
                "Accounts", "Id", onDelete: ReferentialAction.Restrict);
            table.ForeignKey("FK_PublicSubmissions_Accounts_ReviewedByAccountId", x => x.ReviewedByAccountId,
                "Accounts", "Id", onDelete: ReferentialAction.Restrict);
        });
        migrationBuilder.CreateIndex("IX_PublicSubmissions_AccountId", "PublicSubmissions", "AccountId");
        migrationBuilder.CreateIndex("IX_PublicSubmissions_ReviewedByAccountId", "PublicSubmissions",
            "ReviewedByAccountId");
        migrationBuilder.CreateIndex("IX_PublicSubmissions_Status_SubmittedAtUtc", "PublicSubmissions",
            new[] { "Status", "SubmittedAtUtc" });

        migrationBuilder.CreateTable("PublicSubmissionPreviews", columns: table => new
        {
            QuestionVersionId = table.Column<Guid>(type: "uuid", nullable: false),
            AccountId = table.Column<Guid>(type: "uuid", nullable: false),
            TokenHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
            ExpiresAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
        }, constraints: table =>
        {
            table.PrimaryKey("PK_PublicSubmissionPreviews", x => x.QuestionVersionId);
            table.ForeignKey("FK_PublicSubmissionPreviews_QuestionVersions_QuestionVersionId", x => x.QuestionVersionId,
                "QuestionVersions", "Id", onDelete: ReferentialAction.Cascade);
            table.ForeignKey("FK_PublicSubmissionPreviews_Accounts_AccountId", x => x.AccountId,
                "Accounts", "Id", onDelete: ReferentialAction.Restrict);
        });
        migrationBuilder.CreateIndex("IX_PublicSubmissionPreviews_AccountId", "PublicSubmissionPreviews", "AccountId");

        migrationBuilder.CreateTable("PublicSubmissionReviews", columns: table => new
        {
            Id = table.Column<Guid>(type: "uuid", nullable: false),
            QuestionVersionId = table.Column<Guid>(type: "uuid", nullable: false),
            ModeratorAccountId = table.Column<Guid>(type: "uuid", nullable: false),
            Decision = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
            CorrectnessChecked = table.Column<bool>(type: "boolean", nullable: false),
            ImageRightsChecked = table.Column<bool>(type: "boolean", nullable: false),
            PersonalDataChecked = table.Column<bool>(type: "boolean", nullable: false),
            DuplicateChecked = table.Column<bool>(type: "boolean", nullable: false),
            Note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
            CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
        }, constraints: table =>
        {
            table.PrimaryKey("PK_PublicSubmissionReviews", x => x.Id);
            table.ForeignKey("FK_PublicSubmissionReviews_QuestionVersions_QuestionVersionId", x => x.QuestionVersionId,
                "QuestionVersions", "Id", onDelete: ReferentialAction.Cascade);
            table.ForeignKey("FK_PublicSubmissionReviews_Accounts_ModeratorAccountId", x => x.ModeratorAccountId,
                "Accounts", "Id", onDelete: ReferentialAction.Restrict);
        });
        migrationBuilder.CreateIndex("IX_PublicSubmissionReviews_QuestionVersionId", "PublicSubmissionReviews",
            "QuestionVersionId");
        migrationBuilder.CreateIndex("IX_PublicSubmissionReviews_ModeratorAccountId", "PublicSubmissionReviews",
            "ModeratorAccountId");
    }

    /// <inheritdoc />

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("PublicSubmissionReviews");
        migrationBuilder.DropTable("PublicSubmissionPreviews");
        migrationBuilder.DropTable("PublicSubmissions");
        migrationBuilder.DropColumn("AuthorAttribution", "QuestionVersions");
    }
}
