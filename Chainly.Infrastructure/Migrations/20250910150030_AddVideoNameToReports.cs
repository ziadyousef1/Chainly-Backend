using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Chainly.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddVideoNameToReports : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "VideoName",
                table: "Reports",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "VideoName",
                table: "Reports");
        }
    }
}
