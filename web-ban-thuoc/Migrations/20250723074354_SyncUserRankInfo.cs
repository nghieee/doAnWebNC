using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace web_ban_thuoc.Migrations
{
    /// <inheritdoc />
    public partial class SyncUserRankInfo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastRankMailSent",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "LastRankNotiSent",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "UserRank",
                table: "AspNetUsers");

            // UserRankInfos đã được tạo ở migration AddChatMessageTable
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserRankInfos");

            migrationBuilder.AddColumn<DateTime>(
                name: "LastRankMailSent",
                table: "AspNetUsers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastRankNotiSent",
                table: "AspNetUsers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UserRank",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
