using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace web_ban_thuoc.Migrations
{
    /// <inheritdoc />
    public partial class UpdateVoucherForPercentAndCategory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CategoryId",
                table: "Vouchers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CategoryName",
                table: "Vouchers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Detail",
                table: "Vouchers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DiscountType",
                table: "Vouchers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "PercentValue",
                table: "Vouchers",
                type: "decimal(18,2)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CategoryId",
                table: "Vouchers");

            migrationBuilder.DropColumn(
                name: "CategoryName",
                table: "Vouchers");

            migrationBuilder.DropColumn(
                name: "Detail",
                table: "Vouchers");

            migrationBuilder.DropColumn(
                name: "DiscountType",
                table: "Vouchers");

            migrationBuilder.DropColumn(
                name: "PercentValue",
                table: "Vouchers");
        }
    }
}
