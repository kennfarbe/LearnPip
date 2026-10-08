using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LearnPip.Data.Migrations
{
    /// <inheritdoc />
    public partial class IndividualRightsHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "HistoryJson",
                table: "QuestionRights",
                type: "jsonb",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.AddColumn<string>(
                name: "RightsJson",
                table: "PublicSubmissions",
                type: "jsonb",
                nullable: false,
                defaultValue: "{}");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HistoryJson",
                table: "QuestionRights");

            migrationBuilder.DropColumn(
                name: "RightsJson",
                table: "PublicSubmissions");
        }
    }
}
