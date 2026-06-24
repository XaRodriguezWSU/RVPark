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
                    SiteID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SiteNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    SiteType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SiteStatus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    MaxRVLength = table.Column<int>(type: "int", nullable: false),
                    BaseRate = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
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
