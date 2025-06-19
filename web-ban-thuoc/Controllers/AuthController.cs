using Microsoft.AspNetCore.Mvc;

public class AuthController : Controller
{
    public IActionResult Index()
    {
        ViewData["Title"] = "Tài khoản - Nhà Thuốc Long Châu";
        return View("~/Views/Auth/Index.cshtml");
    }
}