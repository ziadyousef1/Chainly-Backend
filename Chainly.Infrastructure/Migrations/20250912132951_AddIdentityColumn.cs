using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Chainly.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddIdentityColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TempId",
                table: "OrderTracking",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql("UPDATE OrderTracking SET TempId = Id");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "OrderTracking");

            migrationBuilder.AddColumn<int>(
                    name: "Id",
                    table: "OrderTracking",
                    type: "int",
                    nullable: false)
                .Annotation("SqlServer:Identity", "1, 1");

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Id",
                table: "OrderTracking");

            migrationBuilder.AddColumn<int>(
                name: "Id",
                table: "OrderTracking",
                type: "int",
                nullable: false);

            migrationBuilder.DropColumn(
                name: "TempId",
                table: "OrderTracking");

        }
    }
}
