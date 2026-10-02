using System;
using LearnPip.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LearnPip.Data.Migrations;

/// <summary>
/// Enthält die Schemaänderungen der Datenbankmigration Administration.
/// </summary>
[DbContext(typeof(LearnPipDbContext))]
[Migration("20260929070000_Administration")]
public sealed class Administration : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "SystemSettings",
            columns: table => new
            {
                Key = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                Value = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_SystemSettings", x => x.Key));
        migrationBuilder.CreateTable(
            name: "AdministrationAuditEvents",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ActorAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                Action = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                Target = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                PreviousValue = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                NewValue = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AdministrationAuditEvents", x => x.Id);
                table.ForeignKey("FK_AdministrationAuditEvents_Accounts_ActorAccountId", x => x.ActorAccountId,
                    "Accounts", "Id", onDelete: ReferentialAction.Restrict);
            });
        migrationBuilder.CreateIndex("IX_AdministrationAuditEvents_ActorAccountId", "AdministrationAuditEvents", "ActorAccountId");
        migrationBuilder.CreateIndex("IX_AdministrationAuditEvents_CreatedAtUtc", "AdministrationAuditEvents", "CreatedAtUtc");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("AdministrationAuditEvents");
        migrationBuilder.DropTable("SystemSettings");
    }
}
