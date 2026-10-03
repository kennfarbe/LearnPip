using System;
using LearnPip.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LearnPip.Data.Migrations;

/// <summary>
/// Enthält die Schemaänderungen der Datenbankmigration PrivateCatalogs.
/// </summary>
[DbContext(typeof(LearnPipDbContext))]
[Migration("20260929100000_PrivateCatalogs")]
public sealed class PrivateCatalogs : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable("PrivateCatalogs", columns: table => new
        {
            Id = table.Column<Guid>(type: "uuid", nullable: false),
            OwnerAccountId = table.Column<Guid>(type: "uuid", nullable: false),
            Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
            CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
        }, constraints: table =>
        {
            table.PrimaryKey("PK_PrivateCatalogs", x => x.Id);
            table.ForeignKey("FK_PrivateCatalogs_Accounts_OwnerAccountId", x => x.OwnerAccountId,
                "Accounts", "Id", onDelete: ReferentialAction.Restrict);
        });
        migrationBuilder.CreateIndex("IX_PrivateCatalogs_OwnerAccountId_Name", "PrivateCatalogs",
            new[] { "OwnerAccountId", "Name" }, unique: true);
        migrationBuilder.AddColumn<Guid>("PrivateCatalogId", "Questions", type: "uuid", nullable: true);
        migrationBuilder.CreateIndex("IX_Questions_PrivateCatalogId", "Questions", "PrivateCatalogId");
        migrationBuilder.AddForeignKey("FK_Questions_PrivateCatalogs_PrivateCatalogId", "Questions",
            "PrivateCatalogId", "PrivateCatalogs", principalColumn: "Id", onDelete: ReferentialAction.SetNull);
        migrationBuilder.CreateTable("QuestionDrafts", columns: table => new
        {
            QuestionId = table.Column<Guid>(type: "uuid", nullable: false),
            PayloadJson = table.Column<string>(type: "character varying(65536)", maxLength: 65536,
                nullable: false),
            UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
        }, constraints: table =>
        {
            table.PrimaryKey("PK_QuestionDrafts", x => x.QuestionId);
            table.ForeignKey("FK_QuestionDrafts_Questions_QuestionId", x => x.QuestionId,
                "Questions", "Id", onDelete: ReferentialAction.Cascade);
        });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("QuestionDrafts");
        migrationBuilder.DropForeignKey("FK_Questions_PrivateCatalogs_PrivateCatalogId", "Questions");
        migrationBuilder.DropTable("PrivateCatalogs");
        migrationBuilder.DropIndex("IX_Questions_PrivateCatalogId", "Questions");
        migrationBuilder.DropColumn("PrivateCatalogId", "Questions");
    }
}
