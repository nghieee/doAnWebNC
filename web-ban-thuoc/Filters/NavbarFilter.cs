using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using web_ban_thuoc.Models;

public class NavbarFilter : IActionFilter
{
    private readonly LongChauDbContext _context;

    public NavbarFilter(LongChauDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public void OnActionExecuting(ActionExecutingContext context)
    {
        try
        {
            var parentCategories = _context.Categories
                .Where(c => c.ParentCategoryId == null)
                .Include(c => c.InverseParentCategory)
                    .ThenInclude(child => child.InverseParentCategory)
                .ToList();

            // Map sang view model phẳng để tránh vòng lặp
            var menuModel = parentCategories.Select(parent => new CategoryMenuViewModel
            {
                CategoryId = parent.CategoryId,
                CategoryName = parent.CategoryName,
                ImageUrl = parent.ImageUrl,
                Children = parent.InverseParentCategory.Select(child => new CategoryMenuViewModel
                {
                    CategoryId = child.CategoryId,
                    CategoryName = child.CategoryName,
                    ImageUrl = child.ImageUrl,
                    Children = child.InverseParentCategory.Select(grandchild => new CategoryMenuViewModel
                    {
                        CategoryId = grandchild.CategoryId,
                        CategoryName = grandchild.CategoryName,
                        ImageUrl = grandchild.ImageUrl
                    }).ToList()
                }).ToList()
            }).ToList();

            context.HttpContext.Items["NavbarCategories"] = menuModel;
        }
        catch (Exception ex)
        {
            context.HttpContext.Items["NavbarCategories"] = new List<CategoryMenuViewModel>();
            System.Console.WriteLine($"Lỗi khi lấy dữ liệu: {ex.Message}");
        }
    }

    public void OnActionExecuted(ActionExecutedContext context) { }
}