using Microsoft.AspNetCore.Mvc;
using PhoAmThuc.Helpers;
using PhoAmThuc.Models;

namespace PhoAmThuc.Controllers
{
    // API bật/tắt các chức năng của website (chỉ quản trị viên):
    //   GET api/admin/settings          danh sách 5 chức năng và trạng thái hiện tại
    //   PUT api/admin/settings/{name}   bật hoặc tắt một chức năng (name: Register, Post, Comment, Rating, Promo)
    // Việc từ chối thao tác khi chức năng bị tắt nằm ở các API tương ứng (qua FeatureCheck), không nằm ở đây
    [Route("api/admin/settings")]
    public class AdminSettingApiController : AdminApiBase
    {
        private readonly AppDbContext db;

        public AdminSettingApiController(AppDbContext db, CurrentUser currentUser) : base(currentUser)
        {
            this.db = db;
        }

        [HttpGet("")]
        public IActionResult GetList()
        {
            IActionResult? denied = CheckAdmin();
            if (denied != null)
            {
                return denied;
            }

            List<Setting> rows = db.Settings.ToList();
            List<object> items = new List<object>();
            foreach (string name in GetFeatureNames())
            {
                Setting? row = rows.FirstOrDefault(s => s.Name == name);
                items.Add(new
                {
                    name = name,
                    title = GetFeatureTitle(name),
                    // Chưa có dòng cấu hình thì coi như đang bật (giống FeatureCheck)
                    enabled = row == null || row.Enabled,
                    updatedAt = row?.UpdatedAt
                });
            }
            return ApiResult.Success(new { items = items });
        }

        [HttpPut("{name}")]
        public IActionResult Save(string name, [FromBody] SettingSaveRequest? request)
        {
            IActionResult? denied = CheckAdmin();
            if (denied != null)
            {
                return denied;
            }
            if (request == null)
            {
                return ApiResult.Fail("Dữ liệu gửi lên không hợp lệ.");
            }
            if (!GetFeatureNames().Contains(name))
            {
                return ApiResult.Fail("Chức năng không hợp lệ.");
            }

            Setting? row = db.Settings.FirstOrDefault(s => s.Name == name);
            if (row == null)
            {
                row = new Setting { Name = name, Description = GetFeatureTitle(name) };
                db.Settings.Add(row);
            }

            row.Enabled = request.Enabled;
            row.UpdatedByAccountId = currentUser.Id;
            row.UpdatedAt = DateTime.Now;
            db.SaveChanges();

            string message = request.Enabled ? "Đã bật chức năng." : "Đã tắt chức năng.";
            return ApiResult.Success(new { enabled = row.Enabled }, message);
        }

        private List<string> GetFeatureNames()
        {
            return new List<string> { Features.Register, Features.Post, Features.Comment, Features.Rating, Features.Promo };
        }

        private string GetFeatureTitle(string name)
        {
            switch (name)
            {
                case Features.Register: return "Cho phép đăng ký tài khoản mới";
                case Features.Post: return "Cho phép tạo bài đăng";
                case Features.Comment: return "Cho phép bình luận và trả lời";
                case Features.Rating: return "Cho phép đánh giá bài đăng và hồ sơ";
                default: return "Cho phép đăng bài quảng bá";
            }
        }
    }
}
