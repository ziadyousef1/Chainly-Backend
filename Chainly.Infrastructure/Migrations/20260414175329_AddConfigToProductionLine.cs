using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Chainly.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddConfigToProductionLine : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "MaximumCurrent",
                table: "ProductionLines",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "MaximumSpeed",
                table: "ProductionLines",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "MaximumTemperature",
                table: "ProductionLines",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "RatedPower",
                table: "ProductionLines",
                type: "float",
                nullable: false,
                defaultValue: 0.0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MaximumCurrent",
                table: "ProductionLines");

            migrationBuilder.DropColumn(
                name: "MaximumSpeed",
                table: "ProductionLines");

            migrationBuilder.DropColumn(
                name: "MaximumTemperature",
                table: "ProductionLines");

            migrationBuilder.DropColumn(
                name: "RatedPower",
                table: "ProductionLines");
        }
    }
}
