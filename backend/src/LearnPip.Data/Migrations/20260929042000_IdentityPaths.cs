using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LearnPip.Data.Migrations;

public partial class IdentityPaths : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "RecoveryCredentials",
            columns: table => new
            {
                AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                SecretHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_RecoveryCredentials", x => x.AccountId);
                table.ForeignKey(
                    name: "FK_RecoveryCredentials_Accounts_AccountId",
                    column: x => x.AccountId,
                    principalTable: "Accounts",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "AccountSessions",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                TokenHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                ExpiresAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                RevokedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AccountSessions", x => x.Id);
                table.ForeignKey(
                    name: "FK_AccountSessions_Accounts_AccountId",
                    column: x => x.AccountId,
                    principalTable: "Accounts",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "EmailLoginCodes",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                Purpose = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                AccountId = table.Column<Guid>(type: "uuid", nullable: true),
                InitiatingSessionId = table.Column<Guid>(type: "uuid", nullable: true),
                CodeHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                ExpiresAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                ConsumedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                FailedAttempts = table.Column<int>(type: "integer", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_EmailLoginCodes", x => x.Id);
                table.ForeignKey(
                    name: "FK_EmailLoginCodes_Accounts_AccountId",
                    column: x => x.AccountId,
                    principalTable: "Accounts",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_EmailLoginCodes_AccountSessions_InitiatingSessionId",
                    column: x => x.InitiatingSessionId,
                    principalTable: "AccountSessions",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_RecoveryCredentials_SecretHash",
            table: "RecoveryCredentials",
            column: "SecretHash",
            unique: true);
        migrationBuilder.CreateIndex(
            name: "IX_AccountSessions_AccountId_ExpiresAtUtc",
            table: "AccountSessions",
            columns: new[] { "AccountId", "ExpiresAtUtc" });
        migrationBuilder.CreateIndex(
            name: "IX_AccountSessions_TokenHash",
            table: "AccountSessions",
            column: "TokenHash",
            unique: true);
        migrationBuilder.CreateIndex(
            name: "IX_EmailLoginCodes_AccountId",
            table: "EmailLoginCodes",
            column: "AccountId");
        migrationBuilder.CreateIndex(
            name: "IX_EmailLoginCodes_InitiatingSessionId",
            table: "EmailLoginCodes",
            column: "InitiatingSessionId");
        migrationBuilder.CreateIndex(
            name: "IX_EmailLoginCodes_Email_Purpose_CreatedAtUtc",
            table: "EmailLoginCodes",
            columns: new[] { "Email", "Purpose", "CreatedAtUtc" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "EmailLoginCodes");
        migrationBuilder.DropTable(name: "RecoveryCredentials");
        migrationBuilder.DropTable(name: "AccountSessions");
    }
}
