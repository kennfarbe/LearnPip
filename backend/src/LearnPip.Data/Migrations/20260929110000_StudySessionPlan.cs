using LearnPip.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LearnPip.Data.Migrations;

[DbContext(typeof(LearnPipDbContext))]
[Migration("20260929110000_StudySessionPlan")]
public sealed class StudySessionPlan : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) =>
        migrationBuilder.AddColumn<string>("PlanJson", "StudySessions", type: "character varying(8192)",
            maxLength: 8192, nullable: true);

    protected override void Down(MigrationBuilder migrationBuilder) =>
        migrationBuilder.DropColumn("PlanJson", "StudySessions");
}
