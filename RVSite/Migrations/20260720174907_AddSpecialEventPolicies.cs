using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RVSite.Migrations
{
    /// <inheritdoc />
    public partial class AddSpecialEventPolicies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ReservationPolicies",
                columns: table => new
                {
                    ReservationPolicyID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaximumAdvanceBookingDays = table.Column<int>(type: "int", nullable: false),
                    PeakSeasonMaximumStayNights = table.Column<int>(type: "int", nullable: false),
                    RequiredDaysAwayBeforeReturn = table.Column<int>(type: "int", nullable: false),
                    LateCancellationWindowDays = table.Column<int>(type: "int", nullable: false),
                    GeneralPolicyNotes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    LastUpdated = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReservationPolicies", x => x.ReservationPolicyID);
                });

            migrationBuilder.CreateTable(
                name: "SpecialEventPolicies",
                columns: table => new
                {
                    SpecialEventPolicyID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EventName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SiteTypeID = table.Column<int>(type: "int", nullable: true),
                    MaximumStayNights = table.Column<int>(type: "int", nullable: true),
                    CancellationWindowDays = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpecialEventPolicies", x => x.SpecialEventPolicyID);
                    table.ForeignKey(
                        name: "FK_SpecialEventPolicies_SiteTypes_SiteTypeID",
                        column: x => x.SiteTypeID,
                        principalTable: "SiteTypes",
                        principalColumn: "SiteTypeID");
                });

            migrationBuilder.CreateIndex(
                name: "IX_SpecialEventPolicies_SiteTypeID",
                table: "SpecialEventPolicies",
                column: "SiteTypeID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReservationPolicies");

            migrationBuilder.DropTable(
                name: "SpecialEventPolicies");
        }
    }
}
