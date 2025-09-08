using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Chainly.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSalesDataTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SalesData",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CityId = table.Column<long>(type: "bigint", nullable: false),
                    StoreId = table.Column<long>(type: "bigint", nullable: false),
                    ManagementGroupId = table.Column<long>(type: "bigint", nullable: false),
                    FirstCategoryId = table.Column<long>(type: "bigint", nullable: false),
                    SecondCategoryId = table.Column<long>(type: "bigint", nullable: false),
                    ThirdCategoryId = table.Column<long>(type: "bigint", nullable: false),
                    ProductId = table.Column<long>(type: "bigint", nullable: false),
                    Dt = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SaleAmount = table.Column<double>(type: "float(18)", precision: 18, scale: 4, nullable: false),
                    HoursSale = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StockHour6_22Cnt = table.Column<int>(type: "int", nullable: false),
                    HoursStockStatus = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Discount = table.Column<double>(type: "float(18)", precision: 18, scale: 4, nullable: false),
                    HolidayFlag = table.Column<int>(type: "int", nullable: false),
                    ActivityFlag = table.Column<int>(type: "int", nullable: false),
                    Precpt = table.Column<double>(type: "float(18)", precision: 18, scale: 4, nullable: false),
                    AvgTemperature = table.Column<double>(type: "float(18)", precision: 18, scale: 4, nullable: false),
                    AvgHumidity = table.Column<double>(type: "float(18)", precision: 18, scale: 4, nullable: false),
                    AvgWindLevel = table.Column<double>(type: "float(18)", precision: 18, scale: 4, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompanyId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesData", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SalesData_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_SalesData_CityId_StoreId_ProductId_Dt",
                table: "SalesData",
                columns: new[] { "CityId", "StoreId", "ProductId", "Dt" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SalesData_CompanyId",
                table: "SalesData",
                column: "CompanyId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SalesData");
        }
    }
}
