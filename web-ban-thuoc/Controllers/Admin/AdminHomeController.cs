using Microsoft.AspNetCore.Mvc;

namespace web_ban_thuoc.Controllers.Admin
{
    public class AdminHomeController : Controller
    {
        public IActionResult Index()
        {
            return View("~/Views/Admin/Index.cshtml");
        }
    }
} 