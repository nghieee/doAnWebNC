using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace web_ban_thuoc.Migrations
{
    /// <inheritdoc />
    public partial class Phase3_MarketingAndLoyalty : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "MinOrderAmount",
                table: "Vouchers",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RequiredRank",
                table: "Vouchers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LoyaltyPoints",
                table: "UserRankInfos",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "LoyaltyPointTransactions",
                columns: table => new
                {
                    LoyaltyPointTransactionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Points = table.Column<int>(type: "int", nullable: false),
                    TransactionType = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    OrderId = table.Column<int>(type: "int", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoyaltyPointTransactions", x => x.LoyaltyPointTransactionId);
                    table.ForeignKey(
                        name: "FK_LoyaltyPointTransactions_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "OrderId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "VoucherRedemptions",
                columns: table => new
                {
                    VoucherRedemptionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VoucherId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OrderId = table.Column<int>(type: "int", nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    RedeemedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsReverted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VoucherRedemptions", x => x.VoucherRedemptionId);
                    table.ForeignKey(
                        name: "FK_VoucherRedemptions_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "OrderId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VoucherRedemptions_Vouchers_VoucherId",
                        column: x => x.VoucherId,
                        principalTable: "Vouchers",
                        principalColumn: "VoucherId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Vouchers_CategoryId",
                table: "Vouchers",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_LoyaltyPointTransactions_OrderId",
                table: "LoyaltyPointTransactions",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_LoyaltyPointTransactions_UserId_OrderId_TransactionType",
                table: "LoyaltyPointTransactions",
                columns: new[] { "UserId", "OrderId", "TransactionType" },
                filter: "[OrderId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_VoucherRedemptions_OrderId",
                table: "VoucherRedemptions",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_VoucherRedemptions_VoucherId_OrderId",
                table: "VoucherRedemptions",
                columns: new[] { "VoucherId", "OrderId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Vouchers_Categories_CategoryId",
                table: "Vouchers",
                column: "CategoryId",
                principalTable: "Categories",
                principalColumn: "CategoryId",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Vouchers_Categories_CategoryId",
                table: "Vouchers");

            migrationBuilder.DropTable(
                name: "LoyaltyPointTransactions");

            migrationBuilder.DropTable(
                name: "VoucherRedemptions");

            migrationBuilder.DropIndex(
                name: "IX_Vouchers_CategoryId",
                table: "Vouchers");

            migrationBuilder.DropColumn(
                name: "MinOrderAmount",
                table: "Vouchers");

            migrationBuilder.DropColumn(
                name: "RequiredRank",
                table: "Vouchers");

            migrationBuilder.DropColumn(
                name: "LoyaltyPoints",
                table: "UserRankInfos");
        }
    }
}
