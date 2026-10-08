using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LearnPip.Data.Migrations
{
    /// <inheritdoc />
    public partial class CatalogMultipleMemberships : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "PrivateCatalogs",
                type: "character varying(2048)",
                maxLength: 2048,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "QuestionCatalogMemberships",
                columns: table => new
                {
                    CatalogId = table.Column<Guid>(type: "uuid", nullable: false),
                    QuestionId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuestionCatalogMemberships", x => new { x.CatalogId, x.QuestionId });
                    table.ForeignKey(
                        name: "FK_QuestionCatalogMemberships_PrivateCatalogs_CatalogId",
                        column: x => x.CatalogId,
                        principalTable: "PrivateCatalogs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_QuestionCatalogMemberships_Questions_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "Questions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_QuestionCatalogMemberships_QuestionId",
                table: "QuestionCatalogMemberships",
                column: "QuestionId");

            // Bestehende Einzelzuordnungen erhalten eine gleichwertige Mitgliedschaft.
            migrationBuilder.Sql(
                "INSERT INTO \"QuestionCatalogMemberships\" (\"CatalogId\", \"QuestionId\") " +
                "SELECT \"PrivateCatalogId\", \"Id\" FROM \"Questions\" WHERE \"PrivateCatalogId\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "QuestionCatalogMemberships");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "PrivateCatalogs");
        }
    }
}
