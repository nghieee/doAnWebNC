using System;
using System.Collections.Generic;

namespace web_ban_thuoc.Models;

public partial class Category
{
    public int CategoryId { get; set; }

    public string CategoryName { get; set; } = null!;

    public string? Description { get; set; }

    public string? ImageUrl { get; set; }

    public int? ParentCategoryId { get; set; }

    public bool IsFeature { get; set; }

    public string? CategoryLevel { get; set; }

    public int ProductCount { get; set; }

    public virtual ICollection<Category> InverseParentCategory { get; set; } = new List<Category>();

    public virtual Category? ParentCategory { get; set; }

    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
}
