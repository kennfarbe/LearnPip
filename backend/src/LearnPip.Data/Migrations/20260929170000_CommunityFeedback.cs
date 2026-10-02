using System;
using LearnPip.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LearnPip.Data.Migrations;

/// <summary>
/// Enthält die Schemaänderungen der Datenbankmigration CommunityFeedback.
/// </summary>
[DbContext(typeof(LearnPipDbContext))]
[Migration("20260929170000_CommunityFeedback")]
public sealed class CommunityFeedback : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable("QuestionReports", columns: table => new
        {
            Id = table.Column<Guid>(type: "uuid", nullable: false),
            QuestionVersionId = table.Column<Guid>(type: "uuid", nullable: false),
            AccountId = table.Column<Guid>(type: "uuid", nullable: false),
            Reason = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
            Details = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
            Status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
            CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
            ClosedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
        }, constraints: table =>
        {
            table.PrimaryKey("PK_QuestionReports", x => x.Id);
            table.ForeignKey("FK_QuestionReports_QuestionVersions_QuestionVersionId", x => x.QuestionVersionId,
                "QuestionVersions", "Id", onDelete: ReferentialAction.Restrict);
            table.ForeignKey("FK_QuestionReports_Accounts_AccountId", x => x.AccountId,
                "Accounts", "Id", onDelete: ReferentialAction.Restrict);
        });
        migrationBuilder.CreateIndex("IX_QuestionReports_Status_QuestionVersionId", "QuestionReports",
            new[] { "Status", "QuestionVersionId" });
        migrationBuilder.CreateIndex("IX_QuestionReports_QuestionVersionId", "QuestionReports", "QuestionVersionId");
        migrationBuilder.CreateIndex("IX_QuestionReports_AccountId", "QuestionReports", "AccountId");

        migrationBuilder.CreateTable("QuestionComments", columns: table => new
        {
            Id = table.Column<Guid>(type: "uuid", nullable: false),
            QuestionVersionId = table.Column<Guid>(type: "uuid", nullable: false),
            AccountId = table.Column<Guid>(type: "uuid", nullable: false),
            Text = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
            CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
            RemovedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
        }, constraints: table =>
        {
            table.PrimaryKey("PK_QuestionComments", x => x.Id);
            table.ForeignKey("FK_QuestionComments_QuestionVersions_QuestionVersionId", x => x.QuestionVersionId,
                "QuestionVersions", "Id", onDelete: ReferentialAction.Restrict);
            table.ForeignKey("FK_QuestionComments_Accounts_AccountId", x => x.AccountId,
                "Accounts", "Id", onDelete: ReferentialAction.Restrict);
        });
        migrationBuilder.CreateIndex("IX_QuestionComments_QuestionVersionId_CreatedAtUtc", "QuestionComments",
            new[] { "QuestionVersionId", "CreatedAtUtc" });
        migrationBuilder.CreateIndex("IX_QuestionComments_AccountId", "QuestionComments", "AccountId");

        migrationBuilder.CreateTable("QuestionHelpfulVotes", columns: table => new
        {
            QuestionVersionId = table.Column<Guid>(type: "uuid", nullable: false),
            AccountId = table.Column<Guid>(type: "uuid", nullable: false),
            Helpful = table.Column<bool>(type: "boolean", nullable: false),
            UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
        }, constraints: table =>
        {
            table.PrimaryKey("PK_QuestionHelpfulVotes", x => new { x.QuestionVersionId, x.AccountId });
            table.ForeignKey("FK_QuestionHelpfulVotes_QuestionVersions_QuestionVersionId", x => x.QuestionVersionId,
                "QuestionVersions", "Id", onDelete: ReferentialAction.Restrict);
            table.ForeignKey("FK_QuestionHelpfulVotes_Accounts_AccountId", x => x.AccountId,
                "Accounts", "Id", onDelete: ReferentialAction.Restrict);
        });
        migrationBuilder.CreateIndex("IX_QuestionHelpfulVotes_AccountId", "QuestionHelpfulVotes", "AccountId");

        migrationBuilder.CreateTable("QuestionModerationEvents", columns: table => new
        {
            Id = table.Column<Guid>(type: "uuid", nullable: false),
            QuestionVersionId = table.Column<Guid>(type: "uuid", nullable: false),
            ModeratorAccountId = table.Column<Guid>(type: "uuid", nullable: false),
            Action = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
            Note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
            ReplacementVersionId = table.Column<Guid>(type: "uuid", nullable: true),
            CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
        }, constraints: table =>
        {
            table.PrimaryKey("PK_QuestionModerationEvents", x => x.Id);
            table.ForeignKey("FK_QuestionModerationEvents_QuestionVersions_QuestionVersionId", x => x.QuestionVersionId,
                "QuestionVersions", "Id", onDelete: ReferentialAction.Restrict);
            table.ForeignKey("FK_QuestionModerationEvents_Accounts_ModeratorAccountId", x => x.ModeratorAccountId,
                "Accounts", "Id", onDelete: ReferentialAction.Restrict);
        });
        migrationBuilder.CreateIndex("IX_QuestionModerationEvents_QuestionVersionId_CreatedAtUtc",
            "QuestionModerationEvents", new[] { "QuestionVersionId", "CreatedAtUtc" });
        migrationBuilder.CreateIndex("IX_QuestionModerationEvents_ModeratorAccountId",
            "QuestionModerationEvents", "ModeratorAccountId");
    }

    /// <inheritdoc />

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("QuestionModerationEvents");
        migrationBuilder.DropTable("QuestionHelpfulVotes");
        migrationBuilder.DropTable("QuestionComments");
        migrationBuilder.DropTable("QuestionReports");
    }
}
