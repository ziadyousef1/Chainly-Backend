using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Chainly.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSessionIdToReports : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Reports_ProductionLines_productionLineId",
                table: "Reports");

            migrationBuilder.RenameColumn(
                name: "productionLineId",
                table: "Reports",
                newName: "ProductionLineId");

            migrationBuilder.RenameIndex(
                name: "IX_Reports_productionLineId",
                table: "Reports",
                newName: "IX_Reports_ProductionLineId");

            migrationBuilder.AddColumn<string>(
                name: "SessionId",
                table: "Reports",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "ProductId",
                table: "DefectiveProducts",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddForeignKey(
                name: "FK_Reports_ProductionLines_ProductionLineId",
                table: "Reports",
                column: "ProductionLineId",
                principalTable: "ProductionLines",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Reports_ProductionLines_ProductionLineId",
                table: "Reports");

            migrationBuilder.DropColumn(
                name: "SessionId",
                table: "Reports");

            migrationBuilder.RenameColumn(
                name: "ProductionLineId",
                table: "Reports",
                newName: "productionLineId");

            migrationBuilder.RenameIndex(
                name: "IX_Reports_ProductionLineId",
                table: "Reports",
                newName: "IX_Reports_productionLineId");

            migrationBuilder.AlterColumn<int>(
                name: "ProductId",
                table: "DefectiveProducts",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddForeignKey(
                name: "FK_Reports_ProductionLines_productionLineId",
                table: "Reports",
                column: "productionLineId",
                principalTable: "ProductionLines",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
