using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace YooVisitApi.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    IdUtilisateur = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: false),
                    HashedPassword = table.Column<string>(type: "text", nullable: false),
                    DateInscription = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Experience = table.Column<int>(type: "integer", nullable: false),
                    Nom = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Biographie = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ProfilePictureFileName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.IdUtilisateur);
                });

            migrationBuilder.CreateTable(
                name: "Zones",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    CoordinatesJson = table.Column<string>(type: "text", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Zones", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Pastilles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Latitude = table.Column<double>(type: "double precision", nullable: false),
                    Longitude = table.Column<double>(type: "double precision", nullable: false),
                    Altitude = table.Column<double>(type: "double precision", nullable: true),
                    StyleArchitectural = table.Column<string>(type: "text", nullable: true),
                    PeriodeConstruction = table.Column<string>(type: "text", nullable: true),
                    HorairesOuverture = table.Column<string>(type: "text", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pastilles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Pastilles_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "IdUtilisateur",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PastilleRatings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PastilleId = table.Column<Guid>(type: "uuid", nullable: false),
                    RaterUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RatingValue = table.Column<int>(type: "integer", nullable: false),
                    RatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PastilleRatings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PastilleRatings_Pastilles_PastilleId",
                        column: x => x.PastilleId,
                        principalTable: "Pastilles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Photos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FileName = table.Column<string>(type: "text", nullable: false),
                    FilePath = table.Column<string>(type: "text", nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PastilleId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Photos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Photos_Pastilles_PastilleId",
                        column: x => x.PastilleId,
                        principalTable: "Pastilles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PastilleRatings_PastilleId_RaterUserId",
                table: "PastilleRatings",
                columns: new[] { "PastilleId", "RaterUserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Pastilles_CreatedByUserId",
                table: "Pastilles",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Photos_PastilleId",
                table: "Photos",
                column: "PastilleId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PastilleRatings");

            migrationBuilder.DropTable(
                name: "Photos");

            migrationBuilder.DropTable(
                name: "Zones");

            migrationBuilder.DropTable(
                name: "Pastilles");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
