using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace web_ban_thuoc.Migrations
{
    /// <inheritdoc />
    public partial class AddTotalSpent6MonthsAndLastRankResetToUserRankInfo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastRankReset",
                table: "UserRankInfos",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalSpent6Months",
                table: "UserRankInfos",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastRankReset",
                table: "UserRankInfos");

            migrationBuilder.DropColumn(
                name: "TotalSpent6Months",
                table: "UserRankInfos");
        }
    }
}
