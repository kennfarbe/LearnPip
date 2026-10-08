using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LearnPip.Data.Migrations
{
    /// <inheritdoc />
    public partial class InstanceCatalogPackagesAndHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "QuestionVersionIdsJson",
                table: "CatalogPackageImports",
                type: "jsonb",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "CatalogPackageImportRevisions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    PackageId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Archive = table.Column<byte[]>(type: "bytea", nullable: false),
                    QuestionIdsJson = table.Column<string>(type: "jsonb", nullable: false),
                    QuestionVersionIdsJson = table.Column<string>(type: "jsonb", nullable: false),
                    ArchivedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CatalogPackageImportRevisions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CatalogPackageImportRevisions_Accounts_OwnerAccountId",
                        column: x => x.OwnerAccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InstanceCatalogPackages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PackageId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    CatalogVersion = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ArchiveSha256 = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Archive = table.Column<byte[]>(type: "bytea", nullable: false),
                    Available = table.Column<bool>(type: "boolean", nullable: false),
                    InstalledAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InstanceCatalogPackages", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CatalogPackageImportRevisions_OwnerAccountId",
                table: "CatalogPackageImportRevisions",
                column: "OwnerAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_InstanceCatalogPackages_PackageId_CatalogVersion",
                table: "InstanceCatalogPackages",
                columns: new[] { "PackageId", "CatalogVersion" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CatalogPackageImportRevisions");

            migrationBuilder.DropTable(
                name: "InstanceCatalogPackages");

            migrationBuilder.DropColumn(
                name: "QuestionVersionIdsJson",
                table: "CatalogPackageImports");
        }
    }
}
