using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace web_ban_thuoc.Migrations
{
    /// <inheritdoc />
    public partial class AddBannerCampaignRelation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PromotionCampaignId",
                table: "Banners",
                type: "int",
                nullable: true);

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Banners_PromotionCampaigns_PromotionCampaignId",
                table: "Banners");

            migrationBuilder.DropIndex(
                name: "IX_Banners_PromotionCampaignId",
                table: "Banners");

            migrationBuilder.DropColumn(
                name: "PromotionCampaignId",
                table: "Banners");
        }
    }
}
