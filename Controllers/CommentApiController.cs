using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhoAmThuc.Helpers;
using PhoAmThuc.Models;

namespace PhoAmThuc.Controllers
{
    // API bình luận, trả lời và đánh giá (dùng chung cho bài đăng và hồ sơ):
    //   GET    api/comment/list?postId=1 hoặc ?profileId=2   danh sách + điểm trung bình + quyền của người xem
    //   POST   api/comment                                    tạo bình luận / đánh giá / trả lời
    //   PUT    api/comment/{id}                               sửa bình luận của mình
    //   DELETE api/comment/{id}                               xóa (tác giả hoặc quản trị viên)
    [Route("api/comment")]
    public class CommentApiController : ControllerBase
    {
        private const int pageSize = 10;
        private const int maxCommentLength = 2000;
        private const int maxRatingLength = 500;

        private readonly AppDbContext db;
        private readonly CurrentUser currentUser;
        private readonly FeatureCheck featureCheck;

        public CommentApiController(AppDbContext db, CurrentUser currentUser, FeatureCheck featureCheck)
        {
            this.db = db;
            this.currentUser = currentUser;
            this.featureCheck = featureCheck;
        }

        // ===================== ĐỌC DỮ LIỆU =====================

        [HttpGet("list")]
        public IActionResult GetList(int? postId, int? profileId, int page = 1)
        {
            if ((postId == null) == (profileId == null))
            {
                return ApiResult.Fail("Cần chọn một bài đăng hoặc một hồ sơ.");
            }

            IQueryable<Comment> query = db.Comments.Where(c => c.ParentId == null && c.Status == CommentStatus.Visible);
            Post? post = null;
            RatingInfo rating;

            if (postId != null)
            {
                int postKey = postId.Value;
                post = db.Posts.FirstOrDefault(p => p.Id == postKey);
                if (post == null || !PostAccess.CanView(post, currentUser))
                {
                    return ApiResult.NotFound("Không tìm thấy bài đăng.");
                }
                query = query.Where(c => c.PostId == postKey);
                rating = RatingHelper.GetPostRating(db, postKey);
            }
            else
            {
                int profileKey = profileId ?? 0;
                if (!db.Accounts.Any(a => a.Id == profileKey && !a.IsLocked))
                {
                    return ApiResult.NotFound("Không tìm thấy hồ sơ.");
                }
                query = query.Where(c => c.TargetAccountId == profileKey);
                rating = RatingHelper.GetProfileRating(db, profileKey);
            }

            int totalCount = query.Count();
            int totalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)pageSize));
            if (page < 1)
            {
                page = 1;
            }
            if (page > totalPages)
            {
                page = totalPages;
            }

            List<Comment> topLevel = query
                .Include(c => c.Author)
                .OrderByDescending(c => c.CreatedAt)
                .ThenByDescending(c => c.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            List<object> items = BuildItemsWithReplies(topLevel);

            return ApiResult.Success(new
            {
                items = items,
                page = page,
                totalPages = totalPages,
                totalCount = totalCount,
                ratingAverage = rating.Average,
                ratingCount = rating.Count,
                permissions = BuildPermissions(post, profileId)
            });
        }

        // ===================== GHI DỮ LIỆU =====================

        [HttpPost("")]
        public IActionResult Create([FromBody] CommentSaveRequest? request)
        {
            if (!currentUser.IsLoggedIn)
            {
                return ApiResult.NeedLogin();
            }
            if (request == null)
            {
                return ApiResult.Fail("Dữ liệu gửi lên không hợp lệ.");
            }

            string content = (request.Content ?? "").Trim();
            string? error = CheckContentAndRating(content, request.Rating, request.Rating != null);
            if (error != null)
            {
                return ApiResult.Fail(error);
            }

            Comment comment = new Comment { AccountId = currentUser.Id, Content = content, Rating = request.Rating };
            if (request.ParentId != null)
            {
                error = PrepareReply(comment, request);
            }
            else
            {
                error = PrepareTopLevel(comment, request);
            }
            if (error != null)
            {
                return ApiResult.Fail(error);
            }

            db.Comments.Add(comment);
            try
            {
                db.SaveChanges();
            }
            catch (DbUpdateException)
            {
                // Thường do vi phạm ràng buộc "mỗi người một đánh giá" khi gửi hai lần liên tiếp
                return ApiResult.Fail("Không lưu được. Có thể bạn đã đánh giá đối tượng này rồi.");
            }
            return ApiResult.Success(new { id = comment.Id }, "Đã gửi thành công.");
        }

        [HttpPut("{id:int}")]
        public IActionResult Update(int id, [FromBody] CommentEditRequest? request)
        {
            if (!currentUser.IsLoggedIn)
            {
                return ApiResult.NeedLogin();
            }
            if (request == null)
            {
                return ApiResult.Fail("Dữ liệu gửi lên không hợp lệ.");
            }

            Comment? comment = db.Comments.FirstOrDefault(c => c.Id == id && c.Status == CommentStatus.Visible);
            if (comment == null)
            {
                return ApiResult.NotFound("Không tìm thấy bình luận.");
            }
            // Chỉ người viết được sửa. Quản trị viên chỉ được ẩn/xóa, không sửa chữ của người khác
            if (comment.AccountId != currentUser.Id)
            {
                return ApiResult.Forbidden("Bạn chỉ được sửa bình luận của mình.");
            }

            bool isRating = comment.Rating != null;
            string content = (request.Content ?? "").Trim();
            string? error = CheckContentAndRating(content, request.Rating, isRating);
            if (error != null)
            {
                return ApiResult.Fail(error);
            }

            comment.Content = content;
            if (isRating)
            {
                comment.Rating = request.Rating;
            }
            comment.UpdatedAt = DateTime.Now;
            db.SaveChanges();
            return ApiResult.Success(null, "Đã lưu thay đổi.");
        }

        [HttpDelete("{id:int}")]
        public IActionResult Delete(int id)
        {
            if (!currentUser.IsLoggedIn)
            {
                return ApiResult.NeedLogin();
            }

            Comment? comment = db.Comments.FirstOrDefault(c => c.Id == id && c.Status != CommentStatus.Deleted);
            if (comment == null)
            {
                return ApiResult.NotFound("Không tìm thấy bình luận.");
            }
            if (comment.AccountId != currentUser.Id && !currentUser.IsAdmin)
            {
                return ApiResult.Forbidden("Bạn chỉ được xóa bình luận của mình.");
            }

            comment.Status = CommentStatus.Deleted;
            comment.UpdatedAt = DateTime.Now;

            // Xóa bình luận gốc thì các câu trả lời cũng không còn hiển thị nữa
            List<Comment> replies = db.Comments.Where(c => c.ParentId == id && c.Status != CommentStatus.Deleted).ToList();
            foreach (Comment reply in replies)
            {
                reply.Status = CommentStatus.Deleted;
                reply.UpdatedAt = DateTime.Now;
            }
            db.SaveChanges();
            return ApiResult.Success(null, "Đã xóa bình luận.");
        }

        // ===================== KIỂM TRA KHI TẠO =====================

        // Trả về câu báo lỗi nếu sai, null nếu hợp lệ. Bình luận đánh giá bắt buộc có điểm và nhận xét ngắn hơn
        private string? CheckContentAndRating(string content, int? rating, bool isRating)
        {
            if (content.Length == 0)
            {
                return "Nội dung không được để trống.";
            }
            if (isRating)
            {
                if (rating == null || rating < 1 || rating > 10)
                {
                    return "Điểm đánh giá phải từ 1 đến 10.";
                }
                if (content.Length > maxRatingLength)
                {
                    return "Nhận xét đánh giá tối đa " + maxRatingLength + " ký tự.";
                }
                return null;
            }
            if (content.Length > maxCommentLength)
            {
                return "Bình luận tối đa " + maxCommentLength + " ký tự.";
            }
            return null;
        }

        // Chuẩn bị câu trả lời: chỉ trả lời bình luận gốc, không kèm điểm, đích lấy từ bình luận gốc
        private string? PrepareReply(Comment comment, CommentSaveRequest request)
        {
            if (request.Rating != null)
            {
                return "Câu trả lời không thể kèm điểm đánh giá.";
            }
            if (!featureCheck.IsOn(Features.Comment))
            {
                return "Chức năng bình luận đang tạm đóng.";
            }

            int parentId = request.ParentId ?? 0;
            Comment? parent = db.Comments.FirstOrDefault(c => c.Id == parentId && c.Status == CommentStatus.Visible);
            if (parent == null)
            {
                return "Bình luận bạn muốn trả lời không còn tồn tại.";
            }
            if (parent.ParentId != null)
            {
                return "Chỉ được trả lời bình luận gốc, không trả lời một câu trả lời.";
            }

            if (parent.PostId != null)
            {
                int postKey = parent.PostId.Value;
                if (!db.Posts.Any(p => p.Id == postKey && p.Status == PostStatus.Public))
                {
                    return "Bài đăng này hiện không nhận bình luận.";
                }
            }

            comment.ParentId = parent.Id;
            comment.PostId = parent.PostId;
            comment.TargetAccountId = parent.TargetAccountId;
            return null;
        }

        // Chuẩn bị bình luận gốc hoặc đánh giá cho một bài đăng hoặc một hồ sơ
        private string? PrepareTopLevel(Comment comment, CommentSaveRequest request)
        {
            if ((request.PostId == null) == (request.TargetAccountId == null))
            {
                return "Cần chọn một bài đăng hoặc một hồ sơ.";
            }

            bool isRating = request.Rating != null;
            if (isRating && !featureCheck.IsOn(Features.Rating))
            {
                return "Chức năng đánh giá đang tạm đóng.";
            }
            if (!isRating && !featureCheck.IsOn(Features.Comment))
            {
                return "Chức năng bình luận đang tạm đóng.";
            }

            if (request.PostId != null)
            {
                return PreparePostComment(comment, request.PostId.Value, isRating);
            }
            return PrepareProfileRating(comment, request.TargetAccountId ?? 0, isRating);
        }

        private string? PreparePostComment(Comment comment, int postId, bool isRating)
        {
            if (!db.Posts.Any(p => p.Id == postId && p.Status == PostStatus.Public))
            {
                return "Bài đăng này hiện không nhận bình luận.";
            }
            if (isRating && HasRated(postId, null))
            {
                return "Bạn đã đánh giá bài đăng này rồi. Hãy sửa đánh giá cũ của bạn.";
            }

            comment.PostId = postId;
            return null;
        }

        // Hồ sơ chỉ nhận đánh giá có điểm, và không được tự đánh giá hồ sơ của mình
        private string? PrepareProfileRating(Comment comment, int profileId, bool isRating)
        {
            if (!isRating)
            {
                return "Hồ sơ chỉ nhận đánh giá kèm điểm.";
            }
            if (profileId == currentUser.Id)
            {
                return "Bạn không thể đánh giá hồ sơ của chính mình.";
            }
            if (!db.Accounts.Any(a => a.Id == profileId && !a.IsLocked))
            {
                return "Không tìm thấy hồ sơ.";
            }
            if (HasRated(null, profileId))
            {
                return "Bạn đã đánh giá hồ sơ này rồi. Hãy sửa đánh giá cũ của bạn.";
            }

            comment.TargetAccountId = profileId;
            return null;
        }

        // ===================== HÀM PHỤ =====================

        // Người đang đăng nhập đã có đánh giá còn hiển thị cho đối tượng này chưa
        private bool HasRated(int? postId, int? profileId)
        {
            int myId = currentUser.Id;
            if (postId != null)
            {
                int postKey = postId.Value;
                return db.Comments.Any(c => c.AccountId == myId && c.PostId == postKey
                    && c.Rating != null && c.Status == CommentStatus.Visible);
            }

            int profileKey = profileId ?? 0;
            return db.Comments.Any(c => c.AccountId == myId && c.TargetAccountId == profileKey
                && c.Rating != null && c.Status == CommentStatus.Visible);
        }

        // Cho giao diện biết người xem được làm gì (để ẩn/hiện biểu mẫu). Máy chủ vẫn kiểm tra lại khi gửi
        private object BuildPermissions(Post? post, int? profileId)
        {
            bool isLoggedIn = currentUser.IsLoggedIn;
            bool targetOpen = post == null || post.Status == PostStatus.Public;

            bool canReply = isLoggedIn && targetOpen && featureCheck.IsOn(Features.Comment);
            bool canComment = canReply && post != null;

            string? blockedReason = null;
            if (isLoggedIn && targetOpen)
            {
                if (!featureCheck.IsOn(Features.Rating))
                {
                    blockedReason = "Chức năng đánh giá đang tạm đóng.";
                }
                else if (profileId != null && profileId.Value == currentUser.Id)
                {
                    blockedReason = "Bạn không thể đánh giá hồ sơ của chính mình.";
                }
                else if (HasRated(post?.Id, profileId))
                {
                    blockedReason = "Bạn đã đánh giá rồi. Hãy sửa đánh giá của bạn bên dưới nếu muốn thay đổi.";
                }
            }
            bool canRate = isLoggedIn && targetOpen && blockedReason == null;

            return new
            {
                isLoggedIn = isLoggedIn,
                canComment = canComment,
                canReply = canReply,
                canRate = canRate,
                rateBlockedReason = blockedReason
            };
        }

        // Ghép các bình luận gốc với câu trả lời của chúng
        private List<object> BuildItemsWithReplies(List<Comment> topLevel)
        {
            List<int> parentIds = topLevel.Select(c => c.Id).ToList();
            List<Comment> replies = db.Comments
                .Include(c => c.Author)
                .Where(c => c.ParentId != null && parentIds.Contains(c.ParentId.Value) && c.Status == CommentStatus.Visible)
                .OrderBy(c => c.CreatedAt)
                .ThenBy(c => c.Id)
                .ToList();

            List<object> items = new List<object>();
            foreach (Comment comment in topLevel)
            {
                List<object> replyItems = replies
                    .Where(r => r.ParentId == comment.Id)
                    .Select(r => BuildItem(r, new List<object>()))
                    .ToList();
                items.Add(BuildItem(comment, replyItems));
            }
            return items;
        }

        private object BuildItem(Comment comment, List<object> replies)
        {
            bool isMine = currentUser.IsLoggedIn && comment.AccountId == currentUser.Id;
            return new
            {
                id = comment.Id,
                authorId = comment.AccountId,
                authorName = comment.Author.DisplayName,
                authorRole = Roles.GetDisplayName(comment.Author.Role),
                authorAvatar = comment.Author.Avatar,
                content = comment.Content,
                rating = comment.Rating,
                createdAt = comment.CreatedAt,
                updatedAt = comment.UpdatedAt,
                canEdit = isMine,
                canDelete = isMine || currentUser.IsAdmin,
                replies = replies
            };
        }
    }
}
