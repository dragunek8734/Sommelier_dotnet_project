using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace dotnetprojekt.Migrations
{
    /// <inheritdoc />
    public partial class AddWineReviewEnhancements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Description",
                schema: "public",
                table: "Ratings",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ImagePath",
                schema: "public",
                table: "Ratings",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Description",
                schema: "public",
                table: "Ratings");

            migrationBuilder.DropColumn(
                name: "ImagePath",
                schema: "public",
                table: "Ratings");
        }
    }
}
