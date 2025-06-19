using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using web_ban_thuoc.Models;

namespace web_ban_thuoc.Controllers;

public class CategoriesController : Controller
{
    private readonly ILogger<CategoriesController> _logger;

    public CategoriesController(ILogger<CategoriesController> logger)
    {
        _logger = logger;
    }

    public IActionResult Index()
    {
        ViewData["Title"] = "Danh sách sản phẩm - Nhà Thuốc Long Châu";
        return View("~/Views/Categories/index.cshtml");
    }
}
