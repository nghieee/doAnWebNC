using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace web_ban_thuoc.Migrations
{
    /// <inheritdoc />
    public partial class LinkPromotionsToBanners : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Banners_PromotionCampaigns_PromotionCampaignId",
                table: "Banners");

            migrationBuilder.DropIndex(
                name: "IX_Banners_PromotionCampaignId",
                table: "Banners");

            migrationBuilder.AddColumn<int>(
                name: "BannerId",
                table: "PromotionCampaigns",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BannerId",
                table: "Products",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PromotionCampaigns_BannerId",
                table: "PromotionCampaigns",
                column: "BannerId",
                unique: true,
                filter: "[BannerId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Products_BannerId",
                table: "Products",
                column: "BannerId");

            migrationBuilder.AddForeignKey(
                name: "FK_Products_Banners_BannerId",
                table: "Products",
                column: "BannerId",
                principalTable: "Banners",
                principalColumn: "BannerId");

            migrationBuilder.AddForeignKey(
                name: "FK_PromotionCampaigns_Banners_BannerId",
                table: "PromotionCampaigns",
                column: "BannerId",
                principalTable: "Banners",
                principalColumn: "BannerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Products_Banners_BannerId",
                table: "Products");

            migrationBuilder.DropForeignKey(
                name: "FK_PromotionCampaigns_Banners_BannerId",
                table: "PromotionCampaigns");

            migrationBuilder.DropIndex(
                name: "IX_PromotionCampaigns_BannerId",
                table: "PromotionCampaigns");

            migrationBuilder.DropIndex(
                name: "IX_Products_BannerId",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "BannerId",
                table: "PromotionCampaigns");

            migrationBuilder.DropColumn(
                name: "BannerId",
                table: "Products");

            migrationBuilder.CreateIndex(
                name: "IX_Banners_PromotionCampaignId",
                table: "Banners",
                column: "PromotionCampaignId");

            migrationBuilder.AddForeignKey(
                name: "FK_Banners_PromotionCampaigns_PromotionCampaignId",
                table: "Banners",
                column: "PromotionCampaignId",
                principalTable: "PromotionCampaigns",
                principalColumn: "PromotionCampaignId");
        }
    }
}
