using System;
using LearnPip.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LearnPip.Data.Migrations;

[DbContext(typeof(LearnPipDbContext))]
[Migration("20260929130000_AccountInactivity")]
public sealed class AccountInactivity : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTimeOffset>("LastActivityAtUtc", "Accounts",
            type: "timestamp with time zone", nullable: false,
            defaultValueSql: "CURRENT_TIMESTAMP");
        migrationBuilder.AddColumn<DateTimeOffset>("DisabledAtUtc", "Accounts",
            type: "timestamp with time zone", nullable: true);
        migrationBuilder.CreateIndex("IX_Accounts_LastActivityAtUtc", "Accounts", "LastActivityAtUtc");
        migrationBuilder.CreateTable("AccountInactivityWarnings", columns: table => new
        {
            AccountId = table.Column<Guid>(type: "uuid", nullable: false),
            PhaseDays = table.Column<int>(type: "integer", nullable: false),
            ActivityAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
            ClaimedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
            SentAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
            DeliveryStatus = table.Column<string>(type: "character varying(16)", maxLength: 16,
                nullable: false)
        }, constraints: table =>
        {
            table.PrimaryKey("PK_AccountInactivityWarnings", x =>
                new { x.AccountId, x.PhaseDays, x.ActivityAtUtc });
            table.ForeignKey("FK_AccountInactivityWarnings_Accounts_AccountId", x => x.AccountId,
                "Accounts", "Id", onDelete: ReferentialAction.Cascade);
        });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("AccountInactivityWarnings");
        migrationBuilder.DropIndex("IX_Accounts_LastActivityAtUtc", "Accounts");
        migrationBuilder.DropColumn("LastActivityAtUtc", "Accounts");
        migrationBuilder.DropColumn("DisabledAtUtc", "Accounts");
    }
}
