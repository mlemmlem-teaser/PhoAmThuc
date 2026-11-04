using Microsoft.AspNetCore.Mvc;
using PhoAmThuc.Helpers;
using PhoAmThuc.Models;

namespace PhoAmThuc.Controllers
{
    // API quản lý nhãn (chỉ quản trị viên):
    //   GET    api/admin/labels        tất cả nhãn kèm số bài đang dùng
    //   POST   api/admin/labels        thêm nhãn thường
    //   PUT    api/admin/labels/{id}   sửa tên, mô tả, bật/tắt
    //   DELETE api/admin/labels/{id}   xóa nhãn thường chưa gắn cho bài nào
    // 5 nhãn hệ thống (loại bài) chỉ được đổi tên và mô tả, không tắt hay xóa được vì trang web dựa vào chúng.
    // Nhãn đang dùng không xóa được, chỉ tắt (tắt rồi thì không chọn được cho bài mới, bài cũ vẫn giữ).
    [Route("api/admin/labels")]
    public class AdminLabelApiController : AdminApiBase
    {
        private readonly AppDbContext db;

        public AdminLabelApiController(AppDbContext db, CurrentUser currentUser) : base(currentUser)
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

            List<Label> labels = db.Labels
                .OrderBy(l => l.SystemKey == null ? 1 : 0)
                .ThenBy(l => l.Name)
                .ToList();

            var usage = db.PostLabels
                .Where(pl => pl.Post.Status != PostStatus.Deleted)
                .GroupBy(pl => pl.LabelId)
                .Select(g => new { LabelId = g.Key, Count = g.Count() })
                .ToList();

            List<object> items = new List<object>();
            foreach (Label label in labels)
            {
                var found = usage.FirstOrDefault(u => u.LabelId == label.Id);
                items.Add(new
                {
                    id = label.Id,
                    name = label.Name,
                    description = label.Description,
                    systemKey = label.SystemKey,
                    isActive = label.IsActive,
                    postCount = found == null ? 0 : found.Count
                });
            }
            return ApiResult.Success(new { items = items });
        }

        [HttpPost("")]
        public IActionResult Create([FromBody] LabelSaveRequest? request)
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

            string name = (request.Name ?? "").Trim();
            string? description = CleanOptionalText(request.Description);
            string? error = CheckInput(name, description);
            if (error != null)
            {
                return ApiResult.Fail(error);
            }
            if (db.Labels.Any(l => l.Name == name))
            {
                return ApiResult.Fail("Đã có một nhãn trùng tên.");
            }

            Label label = new Label { Name = name, Description = description, IsActive = request.IsActive };
            db.Labels.Add(label);
            db.SaveChanges();
            return ApiResult.Success(new { id = label.Id }, "Đã thêm nhãn.");
        }

        [HttpPut("{id:int}")]
        public IActionResult Update(int id, [FromBody] LabelSaveRequest? request)
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

            Label? label = db.Labels.FirstOrDefault(l => l.Id == id);
            if (label == null)
            {
                return ApiResult.NotFound("Không tìm thấy nhãn.");
            }

            string name = (request.Name ?? "").Trim();
            string? description = CleanOptionalText(request.Description);
            string? error = CheckInput(name, description);
            if (error != null)
            {
                return ApiResult.Fail(error);
            }
            if (db.Labels.Any(l => l.Name == name && l.Id != id))
            {
                return ApiResult.Fail("Đã có một nhãn trùng tên.");
            }
            if (label.SystemKey != null && !request.IsActive)
            {
                return ApiResult.Fail("Nhãn hệ thống không thể tắt.");
            }

            label.Name = name;
            label.Description = description;
            label.IsActive = request.IsActive;
            db.SaveChanges();
            return ApiResult.Success(null, "Đã lưu nhãn.");
        }

        [HttpDelete("{id:int}")]
        public IActionResult Delete(int id)
        {
            IActionResult? denied = CheckAdmin();
            if (denied != null)
            {
                return denied;
            }

            Label? label = db.Labels.FirstOrDefault(l => l.Id == id);
            if (label == null)
            {
                return ApiResult.NotFound("Không tìm thấy nhãn.");
            }
            if (label.SystemKey != null)
            {
                return ApiResult.Fail("Nhãn hệ thống không thể xóa.");
            }
            if (db.PostLabels.Any(pl => pl.LabelId == id))
            {
                return ApiResult.Fail("Nhãn này đã được gắn cho bài đăng nên không thể xóa. Hãy tắt nhãn thay vì xóa.");
            }

            db.Labels.Remove(label);
            db.SaveChanges();
            return ApiResult.Success(null, "Đã xóa nhãn.");
        }

        private string? CheckInput(string name, string? description)
        {
            if (name.Length == 0 || name.Length > 80)
            {
                return "Tên nhãn không được để trống và tối đa 80 ký tự.";
            }
            if (description != null && description.Length > 300)
            {
                return "Mô tả nhãn tối đa 300 ký tự.";
            }
            return null;
        }

        private string? CleanOptionalText(string? text)
        {
            string cleaned = (text ?? "").Trim();
            return cleaned.Length == 0 ? null : cleaned;
        }
    }
}
