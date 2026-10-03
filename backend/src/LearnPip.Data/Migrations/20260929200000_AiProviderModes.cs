using LearnPip.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace LearnPip.Data.Migrations;

/// <summary>
/// Enthält die Schemaänderungen der Datenbankmigration AiProviderModes.
/// </summary>
[DbContext(typeof(LearnPipDbContext))]
[Migration("20260929200000_AiProviderModes")]
public sealed class AiProviderModes : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable("UserAiCredentials", columns: table => new
        {
            AccountId = table.Column<Guid>(type: "uuid", nullable: false),
            Ciphertext = table.Column<string>(type: "text", nullable: false),
            UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
        }, constraints: table =>
        {
            table.PrimaryKey("PK_UserAiCredentials", x => x.AccountId);
            table.ForeignKey("FK_UserAiCredentials_Accounts_AccountId", x => x.AccountId,
                "Accounts", "Id", onDelete: ReferentialAction.Cascade);
        });
        migrationBuilder.CreateTable("AiDailyUsages", columns: table => new
        {
            AccountId = table.Column<Guid>(type: "uuid", nullable: false),
            Day = table.Column<DateOnly>(type: "date", nullable: false),
            Mode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
            UsedRequests = table.Column<int>(type: "integer", nullable: false)
        }, constraints: table =>
        {
            table.PrimaryKey("PK_AiDailyUsages", x => new { x.AccountId, x.Day, x.Mode });
            table.ForeignKey("FK_AiDailyUsages_Accounts_AccountId", x => x.AccountId,
                "Accounts", "Id", onDelete: ReferentialAction.Cascade);
        });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("AiDailyUsages");
        migrationBuilder.DropTable("UserAiCredentials");
    }
}
