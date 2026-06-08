using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace web_ban_thuoc.Migrations
{
    /// <inheritdoc />
    public partial class RestoreUserVouchersTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsPublic",
                table: "Vouchers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "UserVouchers",
                columns: table => new
                {
                    UserVoucherId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    VoucherId = table.Column<int>(type: "int", nullable: false),
                    IsUsed = table.Column<bool>(type: "bit", nullable: false),
                    UsedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsNew = table.Column<bool>(type: "bit", nullable: false)
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

            migrationBuilder.Sql(@"
INSERT INTO UserVouchers (UserId, VoucherId, IsUsed, UsedDate, IsNew)
SELECT UserId, VoucherId, IsUsed, UsedDate, IsNew
FROM Vouchers
WHERE UserId IS NOT NULL;");

            migrationBuilder.Sql(@"
UPDATE Vouchers SET IsPublic = 1 WHERE UserId IS NULL;");

            migrationBuilder.Sql(@"
;WITH DupCode AS (
    SELECT Code FROM Vouchers WHERE UserId IS NOT NULL GROUP BY Code HAVING COUNT(*) > 1
),
Master AS (
    SELECT v.Code, MIN(v.VoucherId) AS MasterVoucherId
    FROM Vouchers v
    INNER JOIN DupCode d ON v.Code = d.Code
    WHERE v.UserId IS NOT NULL
    GROUP BY v.Code
)
UPDATE uv
SET uv.VoucherId = m.MasterVoucherId
FROM UserVouchers uv
INNER JOIN Vouchers v ON v.VoucherId = uv.VoucherId
INNER JOIN Master m ON v.Code = m.Code
WHERE v.VoucherId <> m.MasterVoucherId;");

            migrationBuilder.Sql(@"
;WITH DupCode AS (
    SELECT Code FROM Vouchers WHERE UserId IS NOT NULL GROUP BY Code HAVING COUNT(*) > 1
),
Master AS (
    SELECT v.Code, MIN(v.VoucherId) AS MasterVoucherId
    FROM Vouchers v
    INNER JOIN DupCode d ON v.Code = d.Code
    WHERE v.UserId IS NOT NULL
    GROUP BY v.Code
)
DELETE v
FROM Vouchers v
INNER JOIN Master m ON v.Code = m.Code
WHERE v.VoucherId <> m.MasterVoucherId AND v.UserId IS NOT NULL;");

            migrationBuilder.DropForeignKey(
                name: "FK_Vouchers_AspNetUsers_UserId",
                table: "Vouchers");

            migrationBuilder.DropIndex(
                name: "IX_Vouchers_Code_UserId",
                table: "Vouchers");

            migrationBuilder.DropIndex(
                name: "IX_Vouchers_UserId",
                table: "Vouchers");

            migrationBuilder.DropColumn(name: "IsNew", table: "Vouchers");
            migrationBuilder.DropColumn(name: "UsedDate", table: "Vouchers");
            migrationBuilder.DropColumn(name: "UserId", table: "Vouchers");
            migrationBuilder.DropColumn(name: "IsUsed", table: "Vouchers");

            migrationBuilder.CreateIndex(
                name: "IX_UserVouchers_UserId_VoucherId",
                table: "UserVouchers",
                columns: new[] { "UserId", "VoucherId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserVouchers_VoucherId",
                table: "UserVouchers",
                column: "VoucherId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "UserVouchers");

            migrationBuilder.AddColumn<bool>(
                name: "IsUsed",
                table: "Vouchers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsNew",
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

            migrationBuilder.DropColumn(name: "IsPublic", table: "Vouchers");

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
        }
    }
}
