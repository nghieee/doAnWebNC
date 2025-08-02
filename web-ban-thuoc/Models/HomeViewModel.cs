namespace web_ban_thuoc.Models;

public class HomeViewModel
{
    public IEnumerable<Banner> Banners { get; set; } = new List<Banner>();
    public IEnumerable<Category> FeaturedCategories { get; set; }
    public IEnumerable<Product> FeaturedProducts { get; set; }
}