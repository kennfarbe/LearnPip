using System;
using LearnPip.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LearnPip.Data.Migrations;

/// <summary>
/// Enthält die Schemaänderungen der Datenbankmigration PrivateMedia.
/// </summary>
[DbContext(typeof(LearnPipDbContext))]
[Migration("20260929083000_PrivateMedia")]
public sealed class PrivateMedia : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "AltText", table: "MediaAssets", type: "character varying(300)",
            maxLength: 300, nullable: false, defaultValue: "");
        migrationBuilder.CreateTable(
            name: "MediaBlobs",
            columns: table => new
            {
                MediaAssetId = table.Column<Guid>(type: "uuid", nullable: false),
                Data = table.Column<byte[]>(type: "bytea", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_MediaBlobs", x => x.MediaAssetId);
                table.ForeignKey("FK_MediaBlobs_MediaAssets_MediaAssetId", x => x.MediaAssetId,
                    "MediaAssets", "Id", onDelete: ReferentialAction.Cascade);
            });
    }

    /// <inheritdoc />

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("MediaBlobs");
        migrationBuilder.DropColumn("AltText", "MediaAssets");
    }
}
