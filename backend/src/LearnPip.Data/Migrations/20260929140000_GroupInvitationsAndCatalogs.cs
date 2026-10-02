using System;
using LearnPip.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LearnPip.Data.Migrations;

/// <summary>
/// Enthält die Schemaänderungen der Datenbankmigration GroupInvitationsAndCatalogs.
/// </summary>
[DbContext(typeof(LearnPipDbContext))]
[Migration("20260929140000_GroupInvitationsAndCatalogs")]
public sealed class GroupInvitationsAndCatalogs : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable("GroupInvitations", columns: table => new
        {
            Id = table.Column<Guid>(type: "uuid", nullable: false),
            StudyGroupId = table.Column<Guid>(type: "uuid", nullable: false),
            CodeHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
            CreatedByAccountId = table.Column<Guid>(type: "uuid", nullable: false),
            CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
            ExpiresAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
            MaxUses = table.Column<int>(type: "integer", nullable: false),
            UsedCount = table.Column<int>(type: "integer", nullable: false),
            RevokedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
        }, constraints: table =>
        {
            table.PrimaryKey("PK_GroupInvitations", x => x.Id);
            table.ForeignKey("FK_GroupInvitations_StudyGroups_StudyGroupId", x => x.StudyGroupId,
                "StudyGroups", "Id", onDelete: ReferentialAction.Cascade);
            table.ForeignKey("FK_GroupInvitations_Accounts_CreatedByAccountId", x => x.CreatedByAccountId,
                "Accounts", "Id", onDelete: ReferentialAction.Restrict);
        });
        migrationBuilder.CreateIndex("IX_GroupInvitations_CodeHash", "GroupInvitations", "CodeHash", unique: true);
        migrationBuilder.CreateIndex("IX_GroupInvitations_CreatedByAccountId", "GroupInvitations", "CreatedByAccountId");
        migrationBuilder.CreateIndex("IX_GroupInvitations_StudyGroupId", "GroupInvitations", "StudyGroupId");

        migrationBuilder.CreateTable("GroupCatalogShares", columns: table => new
        {
            StudyGroupId = table.Column<Guid>(type: "uuid", nullable: false),
            PrivateCatalogId = table.Column<Guid>(type: "uuid", nullable: false),
            SharedByAccountId = table.Column<Guid>(type: "uuid", nullable: false),
            SharedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
        }, constraints: table =>
        {
            table.PrimaryKey("PK_GroupCatalogShares", x => new { x.StudyGroupId, x.PrivateCatalogId });
            table.ForeignKey("FK_GroupCatalogShares_StudyGroups_StudyGroupId", x => x.StudyGroupId,
                "StudyGroups", "Id", onDelete: ReferentialAction.Cascade);
            table.ForeignKey("FK_GroupCatalogShares_PrivateCatalogs_PrivateCatalogId", x => x.PrivateCatalogId,
                "PrivateCatalogs", "Id", onDelete: ReferentialAction.Cascade);
            table.ForeignKey("FK_GroupCatalogShares_Accounts_SharedByAccountId", x => x.SharedByAccountId,
                "Accounts", "Id", onDelete: ReferentialAction.Restrict);
        });
        migrationBuilder.CreateIndex("IX_GroupCatalogShares_PrivateCatalogId", "GroupCatalogShares", "PrivateCatalogId");
        migrationBuilder.CreateIndex("IX_GroupCatalogShares_SharedByAccountId", "GroupCatalogShares", "SharedByAccountId");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("GroupCatalogShares");
        migrationBuilder.DropTable("GroupInvitations");
    }
}
