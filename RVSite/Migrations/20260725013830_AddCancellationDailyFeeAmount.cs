using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RVSite.Migrations
{
    /// <inheritdoc />
    public partial class AddCancellationDailyFeeAmount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "CancellationDailyFeeAmount",
                table: "ReservationPolicies",
                type: "decimal(10,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CancellationDailyFeeAmount",
                table: "ReservationPolicies");
        }
    }
}
