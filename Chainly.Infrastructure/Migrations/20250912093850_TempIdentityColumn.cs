using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Chainly.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class TempIdentityColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
             
            migrationBuilder.DropForeignKey(
                name: "FK_OrderTracking_Orders_OrderId_CompanyId",
                table: "OrderTracking");

            migrationBuilder.DropIndex(
                name: "IX_OrderTracking_OrderId_CompanyId",
                table: "OrderTracking");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Orders",
                table: "Orders");

            migrationBuilder.AddColumn<int>(
                name: "Id",
                table: "Orders",
                type: "int",
                nullable: false)
                .Annotation("SqlServer:Identity", "1, 1");

            migrationBuilder.RenameColumn(
                name: "OrderId",
                table: "Orders",
                newName: "CompanyOrderId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Orders",
                table: "Orders",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_CompanyOrderId_CompanyId",
                table: "Orders",
                columns: new[] { "CompanyOrderId", "CompanyId" },
                unique: true);

            migrationBuilder.Sql(@"
                SELECT * 
                INTO #TempOrderTracking 
                FROM OrderTracking");

            migrationBuilder.DropTable(name: "OrderTracking");

            migrationBuilder.CreateTable(
                name: "OrderTracking",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false), // FK to Orders.Id (no IDENTITY)
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                 
                },
                constraints: table =>
                {
                    table.ForeignKey(
                        name: "FK_OrderTracking_Orders_Id",
                        column: x => x.Id,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OrderTracking_Users_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.Sql(@"
                INSERT INTO OrderTracking (Id, UpdatedBy)
                SELECT 
                    o.Id, 
                    temp.UpdatedBy
                FROM #TempOrderTracking temp
                INNER JOIN Orders o ON temp.Id = o.Id 
                DROP TABLE #TempOrderTracking");

            migrationBuilder.CreateIndex(
                name: "IX_OrderTracking_UpdatedBy",
                table: "OrderTracking",
                column: "UpdatedBy");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrderTracking_Orders_Id",
                table: "OrderTracking");

            migrationBuilder.DropForeignKey(
                name: "FK_OrderTracking_Users_UpdatedBy",
                table: "OrderTracking");

            migrationBuilder.DropIndex(
                name: "IX_OrderTracking_UpdatedBy",
                table: "OrderTracking");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Orders",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_CompanyOrderId_CompanyId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "Orders");

            migrationBuilder.RenameColumn(
                name: "CompanyOrderId",
                table: "Orders",
                newName: "OrderId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Orders",
                table: "Orders",
                columns: new[] { "OrderId", "CompanyId" });

            migrationBuilder.Sql(@"
                SELECT * 
                INTO #TempOrderTracking 
                FROM OrderTracking");

            migrationBuilder.DropTable(name: "OrderTracking");

            migrationBuilder.CreateTable(
                name: "OrderTracking",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderId = table.Column<int>(type: "int", nullable: false),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderTracking", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderTracking_Orders_OrderId_CompanyId",
                        columns: x => new { x.OrderId, x.CompanyId },
                        principalTable: "Orders",
                        principalColumns: new[] { "OrderId", "CompanyId" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OrderTracking_Users_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.Sql(@"
                INSERT INTO OrderTracking (OrderId, CompanyId, UpdatedBy )
                SELECT 
                    o.OrderId,
                    o.CompanyId,
                    temp.UpdatedBy
           
                FROM #TempOrderTracking temp
                INNER JOIN Orders o ON temp.Id = o.Id 
                
                DROP TABLE #TempOrderTracking");

            migrationBuilder.CreateIndex(
                name: "IX_OrderTracking_OrderId_CompanyId",
                table: "OrderTracking",
                columns: new[] { "OrderId", "CompanyId" });

            migrationBuilder.CreateIndex(
                name: "IX_OrderTracking_UpdatedBy",
                table: "OrderTracking",
                column: "UpdatedBy");
        }
    }
}
