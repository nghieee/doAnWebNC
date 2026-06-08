using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace web_ban_thuoc.Migrations
{
    /// <inheritdoc />
    public partial class MergeVoucherAndInventoryWarehouse : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InventoryTransactions_Products_ProductId",
                table: "InventoryTransactions");

            // --- Voucher: thêm cột trước khi migrate dữ liệu ---
            migrationBuilder.AddColumn<bool>(
                name: "IsNew",
                table: "Vouchers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsUsed",
                table: "Vouchers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "UsedDate",
                table: "Vouchers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "Vouchers",
                type: "nvarchar(450)",
                nullable: true);

            // Voucher chỉ có 1 user gán: cập nhật trực tiếp
            migrationBuilder.Sql(@"
UPDATE v
SET v.UserId = uv.UserId,
    v.IsUsed = uv.IsUsed,
    v.UsedDate = uv.UsedDate,
    v.IsNew = uv.IsNew
FROM Vouchers v
INNER JOIN UserVouchers uv ON uv.VoucherId = v.VoucherId
WHERE v.VoucherId IN (
    SELECT VoucherId FROM UserVouchers GROUP BY VoucherId HAVING COUNT(*) = 1
);");

            // Voucher gán cho nhiều user: clone bản ghi cho từng user
            migrationBuilder.Sql(@"
INSERT INTO Vouchers (
    Code, Description, ExpiryDate, DiscountAmount, IsActive, DiscountType,
    PercentValue, CategoryId, CategoryName, Detail, MaxUsage, UsedCount,
    UserId, IsUsed, UsedDate, IsNew
)
SELECT
    v.Code, v.Description, v.ExpiryDate, v.DiscountAmount, v.IsActive, v.DiscountType,
    v.PercentValue, v.CategoryId, v.CategoryName, v.Detail, v.MaxUsage, v.UsedCount,
    uv.UserId, uv.IsUsed, uv.UsedDate, uv.IsNew
FROM UserVouchers uv
INNER JOIN Vouchers v ON v.VoucherId = uv.VoucherId
WHERE v.VoucherId IN (
    SELECT VoucherId FROM UserVouchers GROUP BY VoucherId HAVING COUNT(*) > 1
);");

            migrationBuilder.Sql(@"
DELETE v
FROM Vouchers v
WHERE v.VoucherId IN (
    SELECT VoucherId FROM UserVouchers GROUP BY VoucherId HAVING COUNT(*) > 1
)
AND v.UserId IS NULL;");

            migrationBuilder.DropTable(name: "UserVouchers");

            migrationBuilder.AlterColumn<string>(
                name: "Code",
                table: "Vouchers",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateIndex(
                name: "IX_Vouchers_Code_UserId",
                table: "Vouchers",
                columns: new[] { "Code", "UserId" });

            migrationBuilder.CreateIndex(
                name: "IX_Vouchers_UserId",
                table: "Vouchers",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Vouchers_AspNetUsers_UserId",
                table: "Vouchers",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            // --- Kho ---
            migrationBuilder.CreateTable(
                name: "Warehouses",
                columns: table => new
                {
                    WarehouseId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Address = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Warehouses", x => x.WarehouseId);
                });

            migrationBuilder.Sql(@"
INSERT INTO Warehouses (Name, Address, IsDefault, IsActive, CreatedAt)
VALUES (N'Kho chính', N'Kho trung tâm', 1, 1, GETDATE());");

            // --- InventoryTransactions: mở rộng cấu trúc ---
            migrationBuilder.Sql("DELETE FROM InventoryTransactions WHERE ProductId IS NULL;");

            migrationBuilder.AddColumn<int>(
                name: "OrderId",
                table: "InventoryTransactions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedByUserId",
                table: "InventoryTransactions",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Note",
                table: "InventoryTransactions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Quantity",
                table: "InventoryTransactions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "QuantityAfter",
                table: "InventoryTransactions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "QuantityBefore",
                table: "InventoryTransactions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "SupplierName",
                table: "InventoryTransactions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "UnitCost",
                table: "InventoryTransactions",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WarehouseId",
                table: "InventoryTransactions",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.Sql(@"
UPDATE InventoryTransactions
SET Quantity = ABS(ISNULL(QuantityChange, 0)),
    TransactionType = ISNULL(TransactionType, 'Adjustment'),
    TransactionDate = ISNULL(TransactionDate, GETDATE());");

            migrationBuilder.DropColumn(
                name: "QuantityChange",
                table: "InventoryTransactions");

            migrationBuilder.AlterColumn<string>(
                name: "TransactionType",
                table: "InventoryTransactions",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "TransactionDate",
                table: "InventoryTransactions",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "ProductId",
                table: "InventoryTransactions",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransactions_CreatedByUserId",
                table: "InventoryTransactions",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransactions_OrderId",
                table: "InventoryTransactions",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransactions_WarehouseId",
                table: "InventoryTransactions",
                column: "WarehouseId");

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryTransactions_AspNetUsers_CreatedByUserId",
                table: "InventoryTransactions",
                column: "CreatedByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryTransactions_Orders_OrderId",
                table: "InventoryTransactions",
                column: "OrderId",
                principalTable: "Orders",
                principalColumn: "OrderId",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryTransactions_Products_ProductId",
                table: "InventoryTransactions",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "ProductId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryTransactions_Warehouses_WarehouseId",
                table: "InventoryTransactions",
                column: "WarehouseId",
                principalTable: "Warehouses",
                principalColumn: "WarehouseId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InventoryTransactions_AspNetUsers_CreatedByUserId",
                table: "InventoryTransactions");

            migrationBuilder.DropForeignKey(
                name: "FK_InventoryTransactions_Orders_OrderId",
                table: "InventoryTransactions");

            migrationBuilder.DropForeignKey(
                name: "FK_InventoryTransactions_Products_ProductId",
                table: "InventoryTransactions");

            migrationBuilder.DropForeignKey(
                name: "FK_InventoryTransactions_Warehouses_WarehouseId",
                table: "InventoryTransactions");

            migrationBuilder.DropForeignKey(
                name: "FK_Vouchers_AspNetUsers_UserId",
                table: "Vouchers");

            migrationBuilder.DropTable(name: "Warehouses");

            migrationBuilder.DropIndex(name: "IX_Vouchers_Code_UserId", table: "Vouchers");
            migrationBuilder.DropIndex(name: "IX_Vouchers_UserId", table: "Vouchers");
            migrationBuilder.DropIndex(name: "IX_InventoryTransactions_CreatedByUserId", table: "InventoryTransactions");
            migrationBuilder.DropIndex(name: "IX_InventoryTransactions_OrderId", table: "InventoryTransactions");
            migrationBuilder.DropIndex(name: "IX_InventoryTransactions_WarehouseId", table: "InventoryTransactions");

            migrationBuilder.DropColumn(name: "IsNew", table: "Vouchers");
            migrationBuilder.DropColumn(name: "IsUsed", table: "Vouchers");
            migrationBuilder.DropColumn(name: "UsedDate", table: "Vouchers");
            migrationBuilder.DropColumn(name: "UserId", table: "Vouchers");

            migrationBuilder.AddColumn<int>(
                name: "QuantityChange",
                table: "InventoryTransactions",
                type: "int",
                nullable: true);

            migrationBuilder.Sql(@"
UPDATE InventoryTransactions
SET QuantityChange = Quantity;");

            migrationBuilder.DropColumn(name: "OrderId", table: "InventoryTransactions");
            migrationBuilder.DropColumn(name: "CreatedByUserId", table: "InventoryTransactions");
            migrationBuilder.DropColumn(name: "Note", table: "InventoryTransactions");
            migrationBuilder.DropColumn(name: "Quantity", table: "InventoryTransactions");
            migrationBuilder.DropColumn(name: "QuantityAfter", table: "InventoryTransactions");
            migrationBuilder.DropColumn(name: "QuantityBefore", table: "InventoryTransactions");
            migrationBuilder.DropColumn(name: "SupplierName", table: "InventoryTransactions");
            migrationBuilder.DropColumn(name: "UnitCost", table: "InventoryTransactions");
            migrationBuilder.DropColumn(name: "WarehouseId", table: "InventoryTransactions");

            migrationBuilder.AlterColumn<string>(
                name: "Code",
                table: "Vouchers",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "TransactionType",
                table: "InventoryTransactions",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<DateTime>(
                name: "TransactionDate",
                table: "InventoryTransactions",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<int>(
                name: "ProductId",
                table: "InventoryTransactions",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.CreateTable(
                name: "UserVouchers",
                columns: table => new
                {
                    UserVoucherId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VoucherId = table.Column<int>(type: "int", nullable: false),
                    IsNew = table.Column<bool>(type: "bit", nullable: false),
                    IsUsed = table.Column<bool>(type: "bit", nullable: false),
                    UsedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserVouchers", x => x.UserVoucherId);
                    table.ForeignKey(
                        name: "FK_UserVouchers_Vouchers_VoucherId",
                        column: x => x.VoucherId,
                        principalTable: "Vouchers",
                        principalColumn: "VoucherId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserVouchers_VoucherId",
                table: "UserVouchers",
                column: "VoucherId");

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryTransactions_Products_ProductId",
                table: "InventoryTransactions",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "ProductId");
        }
    }
}
