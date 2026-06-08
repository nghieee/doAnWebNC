using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace web_ban_thuoc.Migrations
{
    /// <inheritdoc />
    public partial class AddLoyaltyRewards : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "LoyaltyRewardId",
                table: "LoyaltyPointTransactions",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "LoyaltyRewards",
                columns: table => new
                {
                    LoyaltyRewardId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PointsCost = table.Column<int>(type: "int", nullable: false),
                    RewardType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PercentValue = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    DiscountAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    ExpiryDays = table.Column<int>(type: "int", nullable: false),
                    MinOrderAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    RequiredRank = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StockRemaining = table.Column<int>(type: "int", nullable: true),
                    MaxPerUser = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoyaltyRewards", x => x.LoyaltyRewardId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LoyaltyPointTransactions_LoyaltyRewardId",
                table: "LoyaltyPointTransactions",
                column: "LoyaltyRewardId");

            migrationBuilder.AddForeignKey(
                name: "FK_LoyaltyPointTransactions_LoyaltyRewards_LoyaltyRewardId",
                table: "LoyaltyPointTransactions",
                column: "LoyaltyRewardId",
                principalTable: "LoyaltyRewards",
                principalColumn: "LoyaltyRewardId",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LoyaltyPointTransactions_LoyaltyRewards_LoyaltyRewardId",
                table: "LoyaltyPointTransactions");

            migrationBuilder.DropTable(
                name: "LoyaltyRewards");

            migrationBuilder.DropIndex(
                name: "IX_LoyaltyPointTransactions_LoyaltyRewardId",
                table: "LoyaltyPointTransactions");

            migrationBuilder.DropColumn(
                name: "LoyaltyRewardId",
                table: "LoyaltyPointTransactions");
        }
    }
}
