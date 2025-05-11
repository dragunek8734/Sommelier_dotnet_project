using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace dotnetprojekt.Migrations
{
    /// <inheritdoc />
    public partial class AddExperienceLevelRealationDb : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ExperienceLevelId",
                table: "Users",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ExperienceLvlId",
                table: "Users",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ReviewCount",
                table: "Users",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Occasion",
                table: "UserPreferences",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "ExperienceLevels",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExperienceLevels", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Users_ExperienceLevelId",
                table: "Users",
                column: "ExperienceLevelId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_ExperienceLvlId",
                table: "Users",
                column: "ExperienceLvlId");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_ExperienceLevels_ExperienceLevelId",
                table: "Users",
                column: "ExperienceLevelId",
                principalTable: "ExperienceLevels",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_ExperienceLevels_ExperienceLvlId",
                table: "Users",
                column: "ExperienceLvlId",
                principalTable: "ExperienceLevels",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_ExperienceLevels_ExperienceLevelId",
                table: "Users");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_ExperienceLevels_ExperienceLvlId",
                table: "Users");

            migrationBuilder.DropTable(
                name: "ExperienceLevels");

            migrationBuilder.DropIndex(
                name: "IX_Users_ExperienceLevelId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_ExperienceLvlId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ExperienceLevelId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ExperienceLvlId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ReviewCount",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Occasion",
                table: "UserPreferences");
        }
    }
}
