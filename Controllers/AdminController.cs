using Microsoft.AspNetCore.Mvc;
using PhoAmThuc.Helpers;

namespace PhoAmThuc.Controllers
{
    // Trang quản trị (chỉ quản trị viên). Dữ liệu do JavaScript lấy qua các AdminXxxApiController
    public class AdminController : Controller
    {
        private readonly CurrentUser currentUser;

        public AdminController(CurrentUser currentUser)
        {
            this.currentUser = currentUser;
        }

        public IActionResult Index()
        {
            if (!currentUser.IsLoggedIn)
            {
                string returnUrl = Request.Path.Value + Request.QueryString.Value;
                return RedirectToAction("Login", "Account", new { returnUrl = returnUrl });
            }
            if (!currentUser.IsAdmin)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }
    }
}
