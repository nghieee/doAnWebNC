using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace web_ban_thuoc.Migrations
{
    /// <inheritdoc />
    public partial class FixProductImagesTableName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // DB cũ tạo bảng "ProductImage" (InitialCreate) nhưng EF model dùng "ProductImages"
            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[dbo].[ProductImage]', N'U') IS NOT NULL
   AND OBJECT_ID(N'[dbo].[ProductImages]', N'U') IS NULL
BEGIN
    EXEC sp_rename N'[dbo].[ProductImage]', N'ProductImages';
END
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[dbo].[ProductImages]', N'U') IS NOT NULL
   AND OBJECT_ID(N'[dbo].[ProductImage]', N'U') IS NULL
BEGIN
    EXEC sp_rename N'[dbo].[ProductImages]', N'ProductImage';
END
");
        }
    }
}
