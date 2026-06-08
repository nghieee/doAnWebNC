using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace web_ban_thuoc.Migrations
{
    /// <inheritdoc />
    public partial class Phase1_OrderWorkflowAndCart : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Carts",
                columns: table => new
                {
                    CartId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    VoucherCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    VoucherDiscount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Carts", x => x.CartId);
                });

            migrationBuilder.CreateTable(
                name: "OrderStatusHistories",
                columns: table => new
                {
                    OrderStatusHistoryId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderId = table.Column<int>(type: "int", nullable: false),
                    FromStatus = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ToStatus = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ChangedByUserId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Note = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ChangedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderStatusHistories", x => x.OrderStatusHistoryId);
                    table.ForeignKey(
                        name: "FK_OrderStatusHistories_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "OrderId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CartItems",
                columns: table => new
                {
                    CartItemId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CartId = table.Column<int>(type: "int", nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CartItems", x => x.CartItemId);
                    table.ForeignKey(
                        name: "FK_CartItems_Carts_CartId",
                        column: x => x.CartId,
                        principalTable: "Carts",
                        principalColumn: "CartId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CartItems_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "ProductId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CartItems_CartId",
                table: "CartItems",
                column: "CartId");

            migrationBuilder.CreateIndex(
                name: "IX_CartItems_ProductId",
                table: "CartItems",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_Carts_UserId",
                table: "Carts",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrderStatusHistories_OrderId",
                table: "OrderStatusHistories",
                column: "OrderId");

            // Chuyển giỏ hàng cũ (Orders.Status = 'Cart') sang bảng Carts/CartItems
            migrationBuilder.Sql(@"
                INSERT INTO Carts (UserId, VoucherCode, VoucherDiscount, UpdatedAt)
                SELECT UserId, VoucherCode, ISNULL(VoucherDiscount, 0), ISNULL(OrderDate, GETDATE())
                FROM (
                    SELECT UserId, VoucherCode, VoucherDiscount, OrderDate,
                           ROW_NUMBER() OVER (PARTITION BY UserId ORDER BY OrderDate DESC) AS rn
                    FROM Orders
                    WHERE Status = N'Cart' AND UserId IS NOT NULL
                ) latest
                WHERE rn = 1;

                INSERT INTO CartItems (CartId, ProductId, Quantity, UnitPrice)
                SELECT c.CartId, oi.ProductId, oi.Quantity, oi.Price
                FROM OrderItems oi
                INNER JOIN Orders o ON oi.OrderId = o.OrderId
                INNER JOIN (
                    SELECT OrderId, UserId
                    FROM (
                        SELECT OrderId, UserId,
                               ROW_NUMBER() OVER (PARTITION BY UserId ORDER BY OrderDate DESC) AS rn
                        FROM Orders
                        WHERE Status = N'Cart' AND UserId IS NOT NULL
                    ) x
                    WHERE rn = 1
                ) latest ON o.OrderId = latest.OrderId
                INNER JOIN Carts c ON c.UserId = latest.UserId;

                DELETE oi
                FROM OrderItems oi
                INNER JOIN Orders o ON oi.OrderId = o.OrderId
                WHERE o.Status = N'Cart';

                DELETE FROM Orders WHERE Status = N'Cart';
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CartItems");

            migrationBuilder.DropTable(
                name: "OrderStatusHistories");

            migrationBuilder.DropTable(
                name: "Carts");
        }
    }
}
