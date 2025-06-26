using System.Collections.Generic;

namespace web_ban_thuoc.Models
{
    public class CategoryMenuViewModel
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; }
        public string? ImageUrl { get; set; }
        public List<CategoryMenuViewModel> Children { get; set; } = new();
    }
}
