using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RVSite.Migrations
{
    /// <inheritdoc />
    public partial class AddSiteTypeModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SiteType",
                table: "Sites");

            migrationBuilder.AddColumn<int>(
                name: "SiteTypeID",
                table: "Sites",
                type: "INTEGER",
                maxLength: 50,
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "SiteTypes",
                columns: table => new
                {
                    SiteTypeID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SiteTypes", x => x.SiteTypeID);
                });

            migrationBuilder.CreateTable(
                name: "SiteTypePrices",
                columns: table => new
                {
                    SiteTypePriceID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SiteTypeID = table.Column<int>(type: "INTEGER", nullable: false),
                    StartDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EndDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Price = table.Column<decimal>(type: "decimal(10,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SiteTypePrices", x => x.SiteTypePriceID);
                    table.ForeignKey(
                        name: "FK_SiteTypePrices_SiteTypes_SiteTypeID",
                        column: x => x.SiteTypeID,
                        principalTable: "SiteTypes",
                        principalColumn: "SiteTypeID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Sites_SiteTypeID",
                table: "Sites",
                column: "SiteTypeID");

            migrationBuilder.CreateIndex(
                name: "IX_SiteTypePrices_SiteTypeID",
                table: "SiteTypePrices",
                column: "SiteTypeID");

            migrationBuilder.AddForeignKey(
                name: "FK_Sites_SiteTypes_SiteTypeID",
                table: "Sites",
                column: "SiteTypeID",
                principalTable: "SiteTypes",
                principalColumn: "SiteTypeID",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Sites_SiteTypes_SiteTypeID",
                table: "Sites");

            migrationBuilder.DropTable(
                name: "SiteTypePrices");

            migrationBuilder.DropTable(
                name: "SiteTypes");

            migrationBuilder.DropIndex(
                name: "IX_Sites_SiteTypeID",
                table: "Sites");

            migrationBuilder.DropColumn(
                name: "SiteTypeID",
                table: "Sites");

            migrationBuilder.AddColumn<string>(
                name: "SiteType",
                table: "Sites",
                type: "TEXT",
                maxLength: 50,
                nullable: false,
                defaultValue: "");
        }
    }
}
