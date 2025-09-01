using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Chainly.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCompanyIdToRoles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Order_Companies_CompanyId",
                table: "Order");

            migrationBuilder.DropForeignKey(
                name: "FK_OrderTracking_Order_OrderId_CompanyId",
                table: "OrderTracking");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Order",
                table: "Order");

            migrationBuilder.RenameTable(
                name: "Order",
                newName: "Orders");

            migrationBuilder.RenameIndex(
                name: "IX_Order_CompanyId",
                table: "Orders",
                newName: "IX_Orders_CompanyId");

            migrationBuilder.AddColumn<int>(
                name: "CompanyId",
                table: "AspNetRoles",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Orders",
                table: "Orders",
                columns: new[] { "OrderId", "CompanyId" });

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_Companies_CompanyId",
                table: "Orders",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_OrderTracking_Orders_OrderId_CompanyId",
                table: "OrderTracking",
                columns: new[] { "OrderId", "CompanyId" },
                principalTable: "Orders",
                principalColumns: new[] { "OrderId", "CompanyId" },
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_Companies_CompanyId",
                table: "Orders");

            migrationBuilder.DropForeignKey(
                name: "FK_OrderTracking_Orders_OrderId_CompanyId",
                table: "OrderTracking");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Orders",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "AspNetRoles");

            migrationBuilder.RenameTable(
                name: "Orders",
                newName: "Order");

            migrationBuilder.RenameIndex(
                name: "IX_Orders_CompanyId",
                table: "Order",
                newName: "IX_Order_CompanyId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Order",
                table: "Order",
                columns: new[] { "OrderId", "CompanyId" });

            migrationBuilder.AddForeignKey(
                name: "FK_Order_Companies_CompanyId",
                table: "Order",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_OrderTracking_Order_OrderId_CompanyId",
                table: "OrderTracking",
                columns: new[] { "OrderId", "CompanyId" },
                principalTable: "Order",
                principalColumns: new[] { "OrderId", "CompanyId" },
                onDelete: ReferentialAction.Cascade);
        }
    }
}
