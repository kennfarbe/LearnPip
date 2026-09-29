using System;
using LearnPip.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LearnPip.Data.Migrations;

[DbContext(typeof(LearnPipDbContext))]
[Migration("20260929150000_VersionVisibility")]
public sealed class VersionVisibility : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>("Visibility", "QuestionVersions",
            type: "character varying(16)", maxLength: 16, nullable: false, defaultValue: "private");
        migrationBuilder.CreateTable("GroupVersionShares", columns: table => new
        {
            StudyGroupId = table.Column<Guid>(type: "uuid", nullable: false),
            QuestionVersionId = table.Column<Guid>(type: "uuid", nullable: false),
            PrivateCatalogId = table.Column<Guid>(type: "uuid", nullable: false),
            SharedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
        }, constraints: table =>
        {
            table.PrimaryKey("PK_GroupVersionShares", x => new { x.StudyGroupId, x.QuestionVersionId });
            table.ForeignKey("FK_GroupVersionShares_StudyGroups_StudyGroupId", x => x.StudyGroupId,
                "StudyGroups", "Id", onDelete: ReferentialAction.Cascade);
            table.ForeignKey("FK_GroupVersionShares_QuestionVersions_QuestionVersionId", x => x.QuestionVersionId,
                "QuestionVersions", "Id", onDelete: ReferentialAction.Cascade);
            table.ForeignKey("FK_GroupVersionShares_PrivateCatalogs_PrivateCatalogId", x => x.PrivateCatalogId,
                "PrivateCatalogs", "Id", onDelete: ReferentialAction.Cascade);
        });
        migrationBuilder.CreateIndex("IX_GroupVersionShares_PrivateCatalogId", "GroupVersionShares",
            "PrivateCatalogId");
        migrationBuilder.CreateIndex("IX_GroupVersionShares_QuestionVersionId", "GroupVersionShares",
            "QuestionVersionId");
        // Freeze currently shared catalogs at their latest published version per question.
        migrationBuilder.Sql("""
            INSERT INTO "GroupVersionShares" ("StudyGroupId", "QuestionVersionId", "PrivateCatalogId", "SharedAtUtc")
            SELECT DISTINCT ON (s."StudyGroupId", q."Id") s."StudyGroupId", v."Id", s."PrivateCatalogId", CURRENT_TIMESTAMP
            FROM "GroupCatalogShares" s
            JOIN "Questions" q ON q."PrivateCatalogId" = s."PrivateCatalogId" AND q."DeletedAtUtc" IS NULL
            JOIN "QuestionVersions" v ON v."QuestionId" = q."Id"
            WHERE q."OwnerAccountId" = (SELECT c."OwnerAccountId" FROM "PrivateCatalogs" c WHERE c."Id" = s."PrivateCatalogId")
            ORDER BY s."StudyGroupId", q."Id", v."VersionNumber" DESC;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("GroupVersionShares");
        migrationBuilder.DropColumn("Visibility", "QuestionVersions");
    }
}
