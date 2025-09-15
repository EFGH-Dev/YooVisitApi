using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace YooVisitApi.Migrations
{
    /// <inheritdoc />
    public partial class AddQuizTypeToQuiz : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Type",
                table: "Quizzes",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Type",
                table: "Quizzes");
        }
    }
}
