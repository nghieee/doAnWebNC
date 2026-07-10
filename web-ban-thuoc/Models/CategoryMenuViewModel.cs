using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace web_ban_thuoc.Models
{
    public class CategoryMenuViewModel
    {
        [JsonPropertyName("categoryId")]
        public int CategoryId { get; set; }

        [JsonPropertyName("categoryName")]
        public string CategoryName { get; set; } = string.Empty;

        [JsonPropertyName("imageUrl")]
        public string? ImageUrl { get; set; }

        [JsonPropertyName("children")]
        public List<CategoryMenuViewModel> Children { get; set; } = new();
    }
}
