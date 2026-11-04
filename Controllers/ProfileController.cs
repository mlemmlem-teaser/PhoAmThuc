using Microsoft.AspNetCore.Mvc;
using PhoAmThuc.Helpers;
using PhoAmThuc.Models;

namespace PhoAmThuc.Controllers
{
    // Các trang hồ sơ. Dữ liệu do JavaScript lấy qua ProfileApiController
    public class ProfileController : Controller
    {
        private readonly AppDbContext db;
        private readonly CurrentUser currentUser;

        public ProfileController(AppDbContext db, CurrentUser currentUser)
        {
            this.db = db;
            this.currentUser = currentUser;
        }

        // Trang hồ sơ công khai của một tài khoản
        public IActionResult Detail(int id)
        {
            Account? account = db.Accounts.FirstOrDefault(a => a.Id == id && !a.IsLocked);
            if (account == null)
            {
                return NotFound();
            }

            ViewBag.ProfileId = id;
            ViewData["Title"] = account.DisplayName;
            return View();
        }

        // Trang sửa hồ sơ của chính mình
        public IActionResult Edit()
        {
            if (!currentUser.IsLoggedIn)
            {
                string returnUrl = Request.Path.Value + Request.QueryString.Value;
                return RedirectToAction("Login", "Account", new { returnUrl = returnUrl });
            }

            ViewBag.ProfileId = currentUser.Id;
            return View();
        }
    }
}
