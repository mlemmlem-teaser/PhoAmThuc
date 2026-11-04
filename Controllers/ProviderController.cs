using Microsoft.AspNetCore.Mvc;
using PhoAmThuc.Helpers;

namespace PhoAmThuc.Controllers
{
    // Trang danh sách nhà cung cấp (ai cũng xem) và khu vực quản lý dành riêng cho nhà cung cấp.
    // Dữ liệu do JavaScript lấy qua ProviderApiController
    public class ProviderController : Controller
    {
        private readonly CurrentUser currentUser;

        public ProviderController(CurrentUser currentUser)
        {
            this.currentUser = currentUser;
        }

        [RecordVisit]
        public IActionResult Index()
        {
            return View();
        }

        // Khu vực nhà cung cấp: hồ sơ doanh nghiệp, thống kê, phản hồi, thị trường
        public IActionResult Manage()
        {
            if (!currentUser.IsLoggedIn)
            {
                string returnUrl = Request.Path.Value + Request.QueryString.Value;
                return RedirectToAction("Login", "Account", new { returnUrl = returnUrl });
            }
            if (!currentUser.IsProvider)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }
    }
}
