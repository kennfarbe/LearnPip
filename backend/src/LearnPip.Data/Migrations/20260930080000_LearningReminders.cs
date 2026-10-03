using LearnPip.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace LearnPip.Data.Migrations;

/// <summary>
/// Enthält die Schemaänderungen der Datenbankmigration LearningReminders.
/// </summary>
[DbContext(typeof(LearnPipDbContext))]
[Migration("20260930080000_LearningReminders")]
public sealed class LearningReminders : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable("ReminderPreferences", columns: table => new
        {
            AccountId = table.Column<Guid>(type: "uuid", nullable: false),
            Enabled = table.Column<bool>(type: "boolean", nullable: false),
            IntervalDays = table.Column<int>(type: "integer", nullable: false),
            QuietStartMinute = table.Column<int>(type: "integer", nullable: false),
            QuietEndMinute = table.Column<int>(type: "integer", nullable: false),
            TimeZoneId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
            LastNotifiedActivityAtUtc = table.Column<DateTimeOffset?>(type: "timestamp with time zone", nullable: true),
            LastSentAtUtc = table.Column<DateTimeOffset?>(type: "timestamp with time zone", nullable: true)
        }, constraints: table =>
        {
            table.PrimaryKey("PK_ReminderPreferences", x => x.AccountId);
            table.ForeignKey("FK_ReminderPreferences_Accounts_AccountId", x => x.AccountId,
                "Accounts", "Id", onDelete: ReferentialAction.Cascade);
        });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder) =>
        migrationBuilder.DropTable("ReminderPreferences");
}
