using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RVSite.Migrations
{
    /// <inheritdoc />
    public partial class AddMilitaryFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Base",
                table: "Users",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "MilitaryID",
                table: "Users",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Rank",
                table: "Users",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Base",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "MilitaryID",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Rank",
                table: "Users");
        }
    }
}
