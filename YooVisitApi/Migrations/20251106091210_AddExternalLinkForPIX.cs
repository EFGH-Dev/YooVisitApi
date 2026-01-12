using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace YooVisitApi.Migrations
{
    /// <inheritdoc />
    public partial class AddExternalLinkForPIX : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HorairesOuverture",
                table: "Pastilles");

            migrationBuilder.DropColumn(
                name: "PeriodeConstruction",
                table: "Pastilles");

            migrationBuilder.RenameColumn(
                name: "StyleArchitectural",
                table: "Pastilles",
                newName: "ExternalLink");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ExternalLink",
                table: "Pastilles",
                newName: "StyleArchitectural");

            migrationBuilder.AddColumn<string>(
                name: "HorairesOuverture",
                table: "Pastilles",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PeriodeConstruction",
                table: "Pastilles",
                type: "text",
                nullable: true);
        }
    }
}
