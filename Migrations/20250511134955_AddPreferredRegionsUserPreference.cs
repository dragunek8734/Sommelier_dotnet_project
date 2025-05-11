using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace dotnetprojekt.Migrations
{
    /// <inheritdoc />
    public partial class AddPreferredRegionsUserPreference : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int[]>(
                name: "PrefferedRegions",
                table: "UserPreferences",
                type: "integer[]",
                nullable: false,
                defaultValue: new int[0]);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PrefferedRegions",
                table: "UserPreferences");
        }
    }
}
