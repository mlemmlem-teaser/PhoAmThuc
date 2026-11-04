using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhoAmThuc.Helpers;
using PhoAmThuc.Models;

namespace PhoAmThuc.Controllers
{
    // API quản lý bình luận và đánh giá (chỉ quản trị viên):
    //   GET api/admin/comments?page=&q=&type=&status=    danh sách (type: rating, comment, reply; status: Visible, Hidden, Deleted)
    //   PUT api/admin/comments/{id}/status               ẩn, hiện lại hoặc xóa một bình luận
    // Bình luận chỉ bị ẩn hoặc xóa mềm. Điểm trung bình luôn chỉ tính các đánh giá đang hiển thị.
    [Route("api/admin/comments")]
    public class AdminCommentApiController : AdminApiBase
    {
        private const int pageSize = 15;

        private readonly AppDbContext db;

        public AdminCommentApiController(AppDbContext db, CurrentUser currentUser) : base(currentUser)
        {
            this.db = db;
        }

        [HttpGet("")]
        public IActionResult GetList(int page = 1, string? q = null, string? type = null, string? status = null)
        {
            IActionResult? denied = CheckAdmin();
            if (denied != null)
            {
                return denied;
            }

            IQueryable<Comment> query = db.Comments;
            if (!string.IsNullOrWhiteSpace(q))
            {
                string keyword = q.Trim();
                query = query.Where(c => c.Content.Contains(keyword) || c.Author.DisplayName.Contains(keyword));
            }
            if (type == "rating")
            {
                query = query.Where(c => c.Rating != null);
            }
            else if (type == "comment")
            {
                query = query.Where(c => c.Rating == null && c.ParentId == null);
            }
            else if (type == "reply")
            {
                query = query.Where(c => c.ParentId != null);
            }
            if (IsValidStatus(status))
            {
                query = query.Where(c => c.Status == status);
            }

            int totalCount = query.Count();
            int totalPages = CountPages(totalCount, pageSize);
            page = ClampPage(page, totalPages);

            List<Comment> comments = query
                .Include(c => c.Author)
                .Include(c => c.Post)
                .Include(c => c.TargetAccount)
                .OrderByDescending(c => c.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            List<object> items = comments.Select(c => BuildItem(c)).ToList();
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

            Comment? comment = db.Comments.FirstOrDefault(c => c.Id == id);
            if (comment == null)
            {
                return ApiResult.NotFound("Không tìm thấy bình luận.");
            }
            if (comment.Status == wanted)
            {
                return ApiResult.Success(new { status = wanted }, "Trạng thái không thay đổi.");
            }

            string? error = wanted == CommentStatus.Visible ? CheckCanShow(comment) : null;
            if (error != null)
            {
                return ApiResult.Fail(error);
            }

            comment.Status = wanted;
            comment.UpdatedAt = DateTime.Now;
            ChangeReplyStatus(comment, wanted);

            try
            {
                db.SaveChanges();
            }
            catch (DbUpdateException)
            {
                return ApiResult.Fail("Không đổi được trạng thái. Có thể người viết đã có một đánh giá khác đang hiển thị.");
            }
            return ApiResult.Success(new { status = wanted }, "Đã đổi trạng thái bình luận.");
        }

        // Hiện lại một bình luận chỉ được khi bình luận gốc đang hiển thị và không tạo ra hai đánh giá hiển thị của cùng một người
        private string? CheckCanShow(Comment comment)
        {
            if (comment.ParentId != null)
            {
                int parentId = comment.ParentId.Value;
                if (!db.Comments.Any(c => c.Id == parentId && c.Status == CommentStatus.Visible))
                {
                    return "Cần hiển thị bình luận gốc trước khi hiển thị câu trả lời.";
                }
            }

            if (comment.Rating != null)
            {
                bool hasOther = db.Comments.Any(c => c.Id != comment.Id && c.AccountId == comment.AccountId
                    && c.Rating != null && c.Status == CommentStatus.Visible
                    && c.PostId == comment.PostId && c.TargetAccountId == comment.TargetAccountId);
                if (hasOther)
                {
                    return "Người này đã có một đánh giá khác đang hiển thị cho cùng đối tượng.";
                }
            }
            return null;
        }

        // Ẩn hoặc xóa bình luận gốc thì các câu trả lời cũng ẩn hoặc xóa theo. Hiện lại gốc thì hiện lại các câu trả lời đang ẩn
        private void ChangeReplyStatus(Comment comment, string wanted)
        {
            if (comment.ParentId != null)
            {
                return;
            }

            List<Comment> replies = db.Comments.Where(c => c.ParentId == comment.Id).ToList();
            foreach (Comment reply in replies)
            {
                bool hide = wanted != CommentStatus.Visible && reply.Status == CommentStatus.Visible;
                bool show = wanted == CommentStatus.Visible && reply.Status == CommentStatus.Hidden;
                if (hide || show)
                {
                    reply.Status = wanted;
                    reply.UpdatedAt = DateTime.Now;
                }
            }
        }

        private object BuildItem(Comment comment)
        {
            string targetKind = comment.PostId != null ? "Post" : "Profile";
            return new
            {
                id = comment.Id,
                authorId = comment.AccountId,
                authorName = comment.Author.DisplayName,
                content = comment.Content,
                rating = comment.Rating,
                isReply = comment.ParentId != null,
                status = comment.Status,
                createdAt = comment.CreatedAt,
                targetKind = targetKind,
                postId = comment.PostId,
                postTitle = comment.Post?.Title,
                targetAccountId = comment.TargetAccountId,
                targetName = comment.TargetAccount?.DisplayName
            };
        }

        private bool IsValidStatus(string? status)
        {
            return status == CommentStatus.Visible || status == CommentStatus.Hidden || status == CommentStatus.Deleted;
        }
    }
}
