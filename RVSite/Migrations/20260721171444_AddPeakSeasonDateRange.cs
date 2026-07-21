using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RVSite.Migrations
{
    /// <inheritdoc />
    public partial class AddPeakSeasonDateRange : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PeakSeasonEndDay",
                table: "ReservationPolicies",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PeakSeasonEndMonth",
                table: "ReservationPolicies",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PeakSeasonStartDay",
                table: "ReservationPolicies",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PeakSeasonStartMonth",
                table: "ReservationPolicies",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PeakSeasonEndDay",
                table: "ReservationPolicies");

            migrationBuilder.DropColumn(
                name: "PeakSeasonEndMonth",
                table: "ReservationPolicies");

            migrationBuilder.DropColumn(
                name: "PeakSeasonStartDay",
                table: "ReservationPolicies");

            migrationBuilder.DropColumn(
                name: "PeakSeasonStartMonth",
                table: "ReservationPolicies");
        }
    }
}
