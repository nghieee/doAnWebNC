using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace web_ban_thuoc.Controllers.Admin
{
    [Authorize(Roles = "Admin")]
    public class AdminHomeController : Controller
    {
        public IActionResult Index()
        {
            return View("~/Views/Admin/Index.cshtml");
        }
    }
} 