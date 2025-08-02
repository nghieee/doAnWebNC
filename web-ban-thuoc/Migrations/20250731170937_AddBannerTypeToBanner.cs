using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace web_ban_thuoc.Migrations
{
    /// <inheritdoc />
    public partial class AddBannerTypeToBanner : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BannerType",
                table: "Banners",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BannerType",
                table: "Banners");
        }
    }
}
