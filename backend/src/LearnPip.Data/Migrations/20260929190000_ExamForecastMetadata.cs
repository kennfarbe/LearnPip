using LearnPip.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace LearnPip.Data.Migrations;

[DbContext(typeof(LearnPipDbContext))]
[Migration("20260929190000_ExamForecastMetadata")]
public sealed class ExamForecastMetadata : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>("ScheduleJson", "ExamProfileVersions", "text",
            nullable: false, defaultValue: "[]");
        migrationBuilder.AddColumn<string>("RulesSourceUrl", "ExamProfileVersions", "text",
            nullable: true);
        migrationBuilder.AddColumn<DateOnly>("RulesCheckedOn", "ExamProfileVersions", "date",
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn("ScheduleJson", "ExamProfileVersions");
        migrationBuilder.DropColumn("RulesSourceUrl", "ExamProfileVersions");
        migrationBuilder.DropColumn("RulesCheckedOn", "ExamProfileVersions");
    }
}
