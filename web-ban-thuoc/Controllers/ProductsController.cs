using Microsoft.AspNetCore.Mvc;

public class ProductsController : Controller
{
    public IActionResult Index()
    {
        ViewData["Title"] = "Chi tiết sản phẩm - Nhà Thuốc Long Châu";
        return View("~/Views/Products/Index.cshtml");
    }
}