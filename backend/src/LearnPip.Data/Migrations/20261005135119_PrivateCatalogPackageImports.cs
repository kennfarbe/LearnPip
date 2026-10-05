using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LearnPip.Data.Migrations
{
    /// <inheritdoc />
    public partial class PrivateCatalogPackageImports : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CatalogPackageImports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    PrivateCatalogId = table.Column<Guid>(type: "uuid", nullable: true),
                    PackageId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    CatalogVersion = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Fingerprint = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Archive = table.Column<byte[]>(type: "bytea", nullable: false),
                    QuestionIdsJson = table.Column<string>(type: "jsonb", nullable: false),
                    ImportedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CatalogPackageImports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CatalogPackageImports_Accounts_OwnerAccountId",
                        column: x => x.OwnerAccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CatalogPackageImports_PrivateCatalogs_PrivateCatalogId",
                        column: x => x.PrivateCatalogId,
                        principalTable: "PrivateCatalogs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CatalogPackageImports_OwnerAccountId_PackageId",
                table: "CatalogPackageImports",
                columns: new[] { "OwnerAccountId", "PackageId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CatalogPackageImports_PrivateCatalogId",
                table: "CatalogPackageImports",
                column: "PrivateCatalogId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CatalogPackageImports");
        }
    }
}
