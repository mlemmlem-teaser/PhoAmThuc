using Microsoft.AspNetCore.Mvc;
using PhoAmThuc.Helpers;
using PhoAmThuc.Models;
using System.Diagnostics;

namespace PhoAmThuc.Controllers
{
    // Các trang chung: trang chủ và các trang thông tin (giới thiệu, liên hệ, trợ giúp)
    public class HomeController : Controller
    {
        private readonly AppDbContext db;
        private readonly HtmlCleaner htmlCleaner;

        public HomeController(AppDbContext db, HtmlCleaner htmlCleaner)
        {
            this.db = db;
            this.htmlCleaner = htmlCleaner;
        }

        [RecordVisit]
        public IActionResult Index()
        {
            return View();
        }

        [RecordVisit]
        public IActionResult About()
        {
            return ShowInfoPage("about");
        }

        [RecordVisit]
        public IActionResult Contact()
        {
            return ShowInfoPage("contact");
        }

        [RecordVisit]
        public IActionResult Help()
        {
            return ShowInfoPage("help");
        }

        // Đọc nội dung trang thông tin từ bảng InfoPages theo mã trang (slug)
        private IActionResult ShowInfoPage(string slug)
        {
            InfoPage? page = db.InfoPages.FirstOrDefault(p => p.Slug == slug && p.IsPublic);
            if (page == null)
            {
                return NotFound();
            }
            // Nội dung đã được làm sạch lúc lưu; đổi chỗ đánh dấu video thành iframe YouTube khi hiển thị
            ViewBag.ContentHtml = htmlCleaner.ShowVideos(page.Content);
            return View("Info", page);
        }

        // Trang hiển thị khi gặp mã lỗi như 404 (xem UseStatusCodePagesWithReExecute trong Program.cs)
        public IActionResult StatusPage(int code)
        {
            ViewBag.Code = code;
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
