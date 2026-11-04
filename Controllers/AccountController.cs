using Microsoft.AspNetCore.Mvc;
using PhoAmThuc.Helpers;

namespace PhoAmThuc.Controllers
{
    // Các trang đăng nhập và đăng ký. Việc xử lý dữ liệu nằm ở AccountApiController
    public class AccountController : Controller
    {
        private readonly CurrentUser currentUser;
        private readonly FeatureCheck featureCheck;

        public AccountController(CurrentUser currentUser, FeatureCheck featureCheck)
        {
            this.currentUser = currentUser;
            this.featureCheck = featureCheck;
        }

        public IActionResult Login()
        {
            if (currentUser.IsLoggedIn)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        public IActionResult Register()
        {
            if (currentUser.IsLoggedIn)
            {
                return RedirectToAction("Index", "Home");
            }
            ViewBag.IsOpen = featureCheck.IsOn(Features.Register);
            return View();
        }
    }
}
