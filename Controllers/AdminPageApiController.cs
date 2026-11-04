using Microsoft.AspNetCore.Mvc;
using PhoAmThuc.Helpers;
using PhoAmThuc.Models;

namespace PhoAmThuc.Controllers
{
    // API sửa nội dung các trang thông tin: Giới thiệu, Liên hệ, Trợ giúp (chỉ quản trị viên):
    //   GET api/admin/pages         danh sách trang
    //   GET api/admin/pages/{id}    nội dung một trang
    //   PUT api/admin/pages/{id}    lưu nội dung (HTML được làm sạch giống bài đăng)
    // Chỉ sửa các trang đã có, không thêm hoặc xóa trang vì menu của website cố định ba trang này.
    [Route("api/admin/pages")]
    public class AdminPageApiController : AdminApiBase
    {
        private readonly AppDbContext db;
        private readonly HtmlCleaner htmlCleaner;

        public AdminPageApiController(AppDbContext db, CurrentUser currentUser, HtmlCleaner htmlCleaner) : base(currentUser)
        {
            this.db = db;
            this.htmlCleaner = htmlCleaner;
        }

        [HttpGet("")]
        public IActionResult GetList()
        {
            IActionResult? denied = CheckAdmin();
            if (denied != null)
            {
                return denied;
            }

            var rows = db.InfoPages
                .OrderBy(p => p.Id)
                .Select(p => new
                {
                    id = p.Id,
                    slug = p.Slug,
                    title = p.Title,
                    isPublic = p.IsPublic,
                    updatedAt = p.UpdatedAt
                })
                .ToList();

            List<object> items = rows.Select(r => (object)r).ToList();
            return ApiResult.Success(new { items = items });
        }

        [HttpGet("{id:int}")]
        public IActionResult GetOne(int id)
        {
            IActionResult? denied = CheckAdmin();
            if (denied != null)
            {
                return denied;
            }

            InfoPage? page = db.InfoPages.FirstOrDefault(p => p.Id == id);
            if (page == null)
            {
                return ApiResult.NotFound("Không tìm thấy trang.");
            }
            return ApiResult.Success(new
            {
                id = page.Id,
                slug = page.Slug,
                title = page.Title,
                content = page.Content,
                isPublic = page.IsPublic
            });
        }

        [HttpPut("{id:int}")]
        public IActionResult Save(int id, [FromBody] InfoPageSaveRequest? request)
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

            InfoPage? page = db.InfoPages.FirstOrDefault(p => p.Id == id);
            if (page == null)
            {
                return ApiResult.NotFound("Không tìm thấy trang.");
            }

            string title = (request.Title ?? "").Trim();
            string content = htmlCleaner.Clean(request.Content);
            if (title.Length == 0 || title.Length > 200)
            {
                return ApiResult.Fail("Tiêu đề không được để trống và tối đa 200 ký tự.");
            }
            if (!htmlCleaner.HasContent(content))
            {
                return ApiResult.Fail("Nội dung trang không được để trống.");
            }
            if (content.Length > 200000)
            {
                return ApiResult.Fail("Nội dung trang quá dài.");
            }

            page.Title = title;
            page.Content = content;
            page.IsPublic = request.IsPublic;
            page.UpdatedByAccountId = currentUser.Id;
            page.UpdatedAt = DateTime.Now;
            db.SaveChanges();
            return ApiResult.Success(null, "Đã lưu trang.");
        }
    }
}
