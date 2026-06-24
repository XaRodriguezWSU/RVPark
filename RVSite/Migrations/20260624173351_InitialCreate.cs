using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RVSite.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Sites",
                columns: table => new
                {
                    SiteID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SiteNumber = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    SiteType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    SiteStatus = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    MaxRVLength = table.Column<int>(type: "INTEGER", nullable: false),
                    BaseRate = table.Column<decimal>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sites", x => x.SiteID);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Sites");
        }
    }
}
