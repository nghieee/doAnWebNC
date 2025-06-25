using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using web_ban_thuoc.Models;

public class NavbarViewComponent : ViewComponent
{
    public IViewComponentResult Invoke()
    {
        // Lấy dữ liệu từ HttpContext.Items do NavbarFilter gán
        var categories = HttpContext.Items["NavbarCategories"] as List<CategoryMenuViewModel> ?? new List<CategoryMenuViewModel>();
        return View(categories);
    }
}