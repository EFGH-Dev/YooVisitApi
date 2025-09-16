using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace YooVisitApi.Migrations
{
    /// <inheritdoc />
    public partial class AddExplanationToQuiz : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Explanation",
                table: "Quizzes",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Explanation",
                table: "Quizzes");
        }
    }
}
