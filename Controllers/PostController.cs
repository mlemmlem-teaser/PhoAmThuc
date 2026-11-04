using Microsoft.AspNetCore.Mvc;
using PhoAmThuc.Helpers;
using PhoAmThuc.Models;

namespace PhoAmThuc.Controllers
{
    // Các trang liên quan đến bài đăng. Dữ liệu do JavaScript lấy qua PostApiController
    public class PostController : Controller
    {
        private readonly AppDbContext db;
        private readonly CurrentUser currentUser;

        public PostController(AppDbContext db, CurrentUser currentUser)
        {
            this.db = db;
            this.currentUser = currentUser;
        }

        // Danh sách bài đăng. Các bộ lọc (kind, q, labelId...) do JavaScript đọc từ đường dẫn rồi gọi API
        [RecordVisit]
        public IActionResult Index(string? kind)
        {
            ViewBag.PageTitle = GetKindTitle(kind);
            return View();
        }

        // Trang xem một bài đăng
        public IActionResult Detail(int id)
        {
            Post? post = db.Posts.FirstOrDefault(p => p.Id == id);
            if (post == null || !PostAccess.CanView(post, currentUser))
            {
                return NotFound();
            }

            ViewBag.PostId = id;
            ViewData["Title"] = post.Title;
            return View();
        }

        // Trang viết bài mới
        public IActionResult Create()
        {
            if (!currentUser.IsLoggedIn)
            {
                return RedirectToLogin();
            }
            ViewBag.PostId = 0;
            return View("Editor");
        }

        // Trang sửa bài
        public IActionResult Edit(int id)
        {
            if (!currentUser.IsLoggedIn)
            {
                return RedirectToLogin();
            }

            Post? post = db.Posts.FirstOrDefault(p => p.Id == id);
            if (post == null || !PostAccess.CanEdit(post, currentUser))
            {
                return NotFound();
            }

            ViewBag.PostId = id;
            return View("Editor");
        }

        // Trang quản lý các bài đăng của tôi. kind=promo thì chỉ hiện bài quảng bá
        public IActionResult Mine(string? kind)
        {
            if (!currentUser.IsLoggedIn)
            {
                return RedirectToLogin();
            }

            ViewBag.Kind = kind;
            ViewBag.PageTitle = kind == "promo" ? "Bài quảng bá của tôi" : "Quản lý bài đăng của tôi";
            ViewData["Title"] = kind == "promo" ? "Bài quảng bá của tôi" : "Quản lý bài đăng";
            return View();
        }

        // Tiêu đề trang theo loại bài đang xem
        private string GetKindTitle(string? kind)
        {
            switch (kind)
            {
                case "recipe": return "Công thức nấu ăn";
                case "tip": return "Kinh nghiệm nấu ăn";
                case "experience": return "Trải nghiệm ẩm thực";
                case "review": return "Bài viết đánh giá";
                case "promo": return "Bài đăng quảng bá";
                default: return "Bài đăng";
            }
        }

        // Chuyển sang trang đăng nhập, đăng nhập xong sẽ quay lại đúng trang này
        private IActionResult RedirectToLogin()
        {
            string returnUrl = Request.Path.Value + Request.QueryString.Value;
            return RedirectToAction("Login", "Account", new { returnUrl = returnUrl });
        }
    }
}
