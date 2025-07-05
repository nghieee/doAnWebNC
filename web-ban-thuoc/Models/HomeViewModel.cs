namespace web_ban_thuoc.Models;

public class HomeViewModel
{
    public IEnumerable<Category> FeaturedCategories { get; set; }
    public IEnumerable<Product> FeaturedProducts { get; set; }
}