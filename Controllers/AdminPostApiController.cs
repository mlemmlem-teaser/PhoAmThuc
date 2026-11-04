using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhoAmThuc.Helpers;
using PhoAmThuc.Models;

namespace PhoAmThuc.Controllers
{
    // API quản lý bài đăng (chỉ quản trị viên):
    //   GET api/admin/posts?page=&q=&status=            danh sách bài của mọi tài khoản, mọi trạng thái
    //   PUT api/admin/posts/{id}/status                 đổi trạng thái (Draft, Public, Hidden, Deleted), kể cả khôi phục bài đã xóa
    // Sửa nội dung bài dùng lại trang soạn bài /Post/Edit/{id} (quản trị viên được phép sửa mọi bài chưa xóa).
    // Bài đăng chỉ bị xóa mềm (Deleted) để không làm hỏng bình luận, lượt xem và thống kê liên quan.
    [Route("api/admin/posts")]
    public class AdminPostApiController : AdminApiBase
    {
        private const int pageSize = 15;

        private readonly AppDbContext db;

        public AdminPostApiController(AppDbContext db, CurrentUser currentUser) : base(currentUser)
        {
            this.db = db;
        }

        [HttpGet("")]
        public IActionResult GetList(int page = 1, string? q = null, string? status = null)
        {
            IActionResult? denied = CheckAdmin();
            if (denied != null)
            {
                return denied;
            }

            IQueryable<Post> query = db.Posts;
            if (!string.IsNullOrWhiteSpace(q))
            {
                string keyword = q.Trim();
                query = query.Where(p => p.Title.Contains(keyword) || p.Author.DisplayName.Contains(keyword));
            }
            if (IsValidStatus(status))
            {
                query = query.Where(p => p.Status == status);
            }

            int totalCount = query.Count();
            int totalPages = CountPages(totalCount, pageSize);
            page = ClampPage(page, totalPages);

            List<Post> posts = query
                .Include(p => p.Author)
                .OrderByDescending(p => p.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            List<int> ids = posts.Select(p => p.Id).ToList();
            Dictionary<int, int> views = VisitHelper.GetPostViewCounts(db, ids);

            List<object> items = new List<object>();
            foreach (Post post in posts)
            {
                views.TryGetValue(post.Id, out int viewCount);
                items.Add(new
                {
                    id = post.Id,
                    title = post.Title,
                    authorId = post.AccountId,
                    authorName = post.Author.DisplayName,
                    status = post.Status,
                    createdAt = post.CreatedAt,
                    updatedAt = post.UpdatedAt,
                    viewCount = viewCount
                });
            }
            return ApiResult.Success(new { items = items, page = page, totalPages = totalPages, totalCount = totalCount });
        }

        [HttpPut("{id:int}/status")]
        public IActionResult ChangeStatus(int id, [FromBody] StatusRequest? request)
        {
            IActionResult? denied = CheckAdmin();
            if (denied != null)
            {
                return denied;
            }

            string wanted = request?.Status ?? "";
            if (!IsValidStatus(wanted))
            {
                return ApiResult.Fail("Trạng thái không hợp lệ.");
            }

            Post? post = db.Posts.FirstOrDefault(p => p.Id == id);
            if (post == null)
            {
                return ApiResult.NotFound("Không tìm thấy bài đăng.");
            }

            post.Status = wanted;
            post.UpdatedAt = DateTime.Now;
            if (wanted == PostStatus.Public && post.PublishedAt == null)
            {
                post.PublishedAt = DateTime.Now;
            }
            db.SaveChanges();
            return ApiResult.Success(new { status = post.Status }, "Đã đổi trạng thái bài đăng.");
        }

        private bool IsValidStatus(string? status)
        {
            return status == PostStatus.Draft || status == PostStatus.Public
                || status == PostStatus.Hidden || status == PostStatus.Deleted;
        }
    }
}
