using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace web_ban_thuoc.Migrations
{
    /// <inheritdoc />
    public partial class AddGhnFieldsToShipment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "GhnOrderCode",
                table: "Shipments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GhnPrintFormat",
                table: "Shipments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GhnPrintToken",
                table: "Shipments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "GhnPrintTokenExpiredAt",
                table: "Shipments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GhnRawResponse",
                table: "Shipments",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GhnOrderCode",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "GhnPrintFormat",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "GhnPrintToken",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "GhnPrintTokenExpiredAt",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "GhnRawResponse",
                table: "Shipments");
        }
    }
}
