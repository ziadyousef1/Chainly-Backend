using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Chainly.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SimplifyReports : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DefectiveProducts");

            migrationBuilder.DropTable(
                name: "NonDefectiveProducts");

            migrationBuilder.DropColumn(
                name: "SessionId",
                table: "Reports");

            migrationBuilder.RenameColumn(
                name: "ReportDate",
                table: "Reports",
                newName: "StartedAt");

            migrationBuilder.AddColumn<int>(
                name: "DefectiveProducts",
                table: "Reports",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "EndedAt",
                table: "Reports",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DefectiveProducts",
                table: "Reports");

            migrationBuilder.DropColumn(
                name: "EndedAt",
                table: "Reports");

            migrationBuilder.RenameColumn(
                name: "StartedAt",
                table: "Reports",
                newName: "ReportDate");

            migrationBuilder.AddColumn<string>(
                name: "SessionId",
                table: "Reports",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "DefectiveProducts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReportId = table.Column<int>(type: "int", nullable: false),
                    ProductId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Time = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DefectiveProducts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DefectiveProducts_Reports_ReportId",
                        column: x => x.ReportId,
                        principalTable: "Reports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NonDefectiveProducts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReportId = table.Column<int>(type: "int", nullable: false),
                    ProductId = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NonDefectiveProducts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NonDefectiveProducts_Reports_ReportId",
                        column: x => x.ReportId,
                        principalTable: "Reports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DefectiveProducts_ReportId",
                table: "DefectiveProducts",
                column: "ReportId");

            migrationBuilder.CreateIndex(
                name: "IX_NonDefectiveProducts_ReportId",
                table: "NonDefectiveProducts",
                column: "ReportId");
        }
    }
}
