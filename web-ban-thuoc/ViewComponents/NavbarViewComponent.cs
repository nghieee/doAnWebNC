using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using web_ban_thuoc.Models;

public class NavbarViewComponent : ViewComponent
{
    private readonly LongChauDbContext _context;

    public NavbarViewComponent(LongChauDbContext context)
    {
        _context = context;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var parentCategories = await _context.Categories
            .Where(c => c.ParentCategoryId == null)
            .Include(c => c.InverseParentCategory)
                .ThenInclude(child => child.InverseParentCategory)
            .AsNoTracking()
            .ToListAsync();

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

        return View(menuModel);
    }
}
