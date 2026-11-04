using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhoAmThuc.Helpers;
using PhoAmThuc.Models;

namespace PhoAmThuc.Controllers
{
    // API nhà cung cấp:
    //   GET api/provider/list       danh sách nhà cung cấp công khai (ai cũng xem được)
    //   GET api/provider/mine       hồ sơ doanh nghiệp của tôi   |  PUT api/provider/mine  tạo hoặc sửa
    //   GET api/provider/stats      số liệu của tôi (lượt xem, điểm, bình luận) để vẽ biểu đồ
    //   GET api/provider/feedback   bình luận và đánh giá người khác để lại cho tôi
    //   GET api/provider/market     số liệu tổng hợp thị trường từ nội dung công khai
    // Các API từ "mine" trở xuống chỉ dành cho tài khoản có vai trò nhà cung cấp
    [Route("api/provider")]
    public class ProviderApiController : ControllerBase
    {
        private const int pageSize = 12;
        private const int feedbackPageSize = 10;

        private readonly AppDbContext db;
        private readonly CurrentUser currentUser;

        public ProviderApiController(AppDbContext db, CurrentUser currentUser)
        {
            this.db = db;
            this.currentUser = currentUser;
        }

        // ===================== DANH SÁCH CÔNG KHAI =====================

        [HttpGet("list")]
        public IActionResult GetList(int page = 1, string? q = null)
        {
            IQueryable<ProviderProfile> query = db.ProviderProfiles
                .Include(p => p.Account)
                .Where(p => p.IsPublic && !p.Account.IsLocked && p.Account.Role == Roles.Provider);

            if (!string.IsNullOrWhiteSpace(q))
            {
                string keyword = q.Trim();
                query = query.Where(p => p.BusinessName.Contains(keyword));
            }

            // Số nhà cung cấp không lớn nên lấy hết rồi sắp xếp và cắt trang trong bộ nhớ
            List<ProviderProfile> profiles = query.ToList();
            Dictionary<int, RatingInfo> ratings = RatingHelper.GetAllProfileRatings(db);

            List<ProviderProfile> sorted = profiles
                .OrderByDescending(p => FindAverage(ratings, p.AccountId))
                .ThenByDescending(p => FindCount(ratings, p.AccountId))
                .ThenBy(p => p.BusinessName)
                .ToList();

            int totalCount = sorted.Count;
            int totalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)pageSize));
            if (page < 1)
            {
                page = 1;
            }
            if (page > totalPages)
            {
                page = totalPages;
            }

            List<object> items = sorted
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => BuildListItem(p, ratings))
                .ToList();

            return ApiResult.Success(new { items = items, page = page, totalPages = totalPages, totalCount = totalCount });
        }

        // ===================== HỒ SƠ DOANH NGHIỆP CỦA TÔI =====================

        [HttpGet("mine")]
        public IActionResult GetMine()
        {
            IActionResult? denied = CheckProvider();
            if (denied != null)
            {
                return denied;
            }

            int myId = currentUser.Id;
            ProviderProfile? profile = db.ProviderProfiles.FirstOrDefault(p => p.AccountId == myId);
            if (profile == null)
            {
                return ApiResult.Success(null);
            }

            return ApiResult.Success(new
            {
                businessName = profile.BusinessName,
                description = profile.Description,
                logo = profile.Logo,
                address = profile.Address,
                phone = profile.Phone,
                contactEmail = profile.ContactEmail,
                website = profile.Website,
                socialLink = profile.SocialLink,
                isPublic = profile.IsPublic
            });
        }

        [HttpPut("mine")]
        public IActionResult SaveMine([FromBody] ProviderProfileSaveRequest? request)
        {
            IActionResult? denied = CheckProvider();
            if (denied != null)
            {
                return denied;
            }
            if (request == null)
            {
                return ApiResult.Fail("Dữ liệu gửi lên không hợp lệ.");
            }

            string? error = ProviderProfileRules.CheckInput(request);
            if (error != null)
            {
                return ApiResult.Fail(error);
            }

            int myId = currentUser.Id;
            ProviderProfile? profile = db.ProviderProfiles.FirstOrDefault(p => p.AccountId == myId);
            bool isNew = profile == null;
            if (profile == null)
            {
                profile = new ProviderProfile { AccountId = myId };
                db.ProviderProfiles.Add(profile);
            }

            ProviderProfileRules.Apply(profile, request);
            if (!isNew)
            {
                profile.UpdatedAt = DateTime.Now;
            }

            db.SaveChanges();
            return ApiResult.Success(new { accountId = myId }, "Đã lưu hồ sơ doanh nghiệp.");
        }

        // ===================== THỐNG KÊ CỦA TÔI =====================

        // days: số ngày gần nhất để vẽ biểu đồ lượt xem theo ngày (7 đến 90)
        [HttpGet("stats")]
        public IActionResult GetStats(int days = 30)
        {
            IActionResult? denied = CheckProvider();
            if (denied != null)
            {
                return denied;
            }
            if (days < 7)
            {
                days = 7;
            }
            if (days > 90)
            {
                days = 90;
            }

            int myId = currentUser.Id;
            List<int> postIds = db.Posts
                .Where(p => p.AccountId == myId && p.Status != PostStatus.Deleted)
                .Select(p => p.Id)
                .ToList();
            Dictionary<int, int> postViews = VisitHelper.GetPostViewCounts(db, postIds);
            RatingInfo profileRating = RatingHelper.GetProfileRating(db, myId);

            return ApiResult.Success(new
            {
                totals = new
                {
                    profileViews = db.Visits.Count(v => v.Kind == VisitKind.Profile && v.ProfileAccountId == myId),
                    postViews = postViews.Values.Sum(),
                    ratingAverage = profileRating.Average,
                    ratingCount = profileRating.Count,
                    postComments = CountPostFeedback(postIds, myId, false),
                    postRatings = CountPostFeedback(postIds, myId, true)
                },
                days = BuildDailyViews(myId, postIds, days),
                ratingDistribution = BuildRatingDistribution(myId),
                topPosts = BuildTopPosts(postIds, postViews)
            });
        }

        // ===================== PHẢN HỒI NHẬN ĐƯỢC =====================

        // filter: all (mặc định), rating (chỉ đánh giá), comment (chỉ bình luận thường)
        [HttpGet("feedback")]
        public IActionResult GetFeedback(int page = 1, string? filter = null)
        {
            IActionResult? denied = CheckProvider();
            if (denied != null)
            {
                return denied;
            }

            int myId = currentUser.Id;
            IQueryable<Comment> query = db.Comments
                .Where(c => c.Status == CommentStatus.Visible && c.ParentId == null && c.AccountId != myId
                    && ((c.Post != null && c.Post.AccountId == myId && c.Post.Status != PostStatus.Deleted)
                        || c.TargetAccountId == myId));

            if (filter == "rating")
            {
                query = query.Where(c => c.Rating != null);
            }
            else if (filter == "comment")
            {
                query = query.Where(c => c.Rating == null);
            }

            int totalCount = query.Count();
            int totalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)feedbackPageSize));
            if (page < 1)
            {
                page = 1;
            }
            if (page > totalPages)
            {
                page = totalPages;
            }

            List<Comment> comments = query
                .Include(c => c.Author)
                .Include(c => c.Post)
                .OrderByDescending(c => c.CreatedAt)
                .ThenByDescending(c => c.Id)
                .Skip((page - 1) * feedbackPageSize)
                .Take(feedbackPageSize)
                .ToList();

            List<object> items = comments.Select(c => BuildFeedbackItem(c)).ToList();
            return ApiResult.Success(new { items = items, page = page, totalPages = totalPages, totalCount = totalCount });
        }

        // ===================== THÔNG TIN THỊ TRƯỜNG =====================

        // Chỉ là số liệu tổng hợp từ bài công khai: số bài theo loại và các nhãn xuất hiện nhiều
        [HttpGet("market")]
        public IActionResult GetMarket()
        {
            IActionResult? denied = CheckProvider();
            if (denied != null)
            {
                return denied;
            }

            List<Label> typeLabels = db.Labels.Where(l => l.SystemKey != null).ToList();
            List<object> types = typeLabels
                .Select(l => new
                {
                    name = l.Name,
                    systemKey = l.SystemKey,
                    count = db.PostLabels.Count(pl => pl.LabelId == l.Id && pl.Post.Status == PostStatus.Public)
                })
                .OrderByDescending(x => x.count)
                .Select(x => (object)x)
                .ToList();

            var topGroups = db.PostLabels
                .Where(pl => pl.Post.Status == PostStatus.Public && pl.Label.SystemKey == null && pl.Label.IsActive)
                .GroupBy(pl => pl.LabelId)
                .OrderByDescending(g => g.Count())
                .Take(10)
                .Select(g => new { LabelId = g.Key, Count = g.Count() })
                .ToList();

            List<int> labelIds = topGroups.Select(g => g.LabelId).ToList();
            Dictionary<int, string> names = db.Labels
                .Where(l => labelIds.Contains(l.Id))
                .ToDictionary(l => l.Id, l => l.Name);

            List<object> topLabels = topGroups
                .Select(g => (object)new { name = names.ContainsKey(g.LabelId) ? names[g.LabelId] : "", count = g.Count })
                .ToList();

            return ApiResult.Success(new
            {
                totalPosts = db.Posts.Count(p => p.Status == PostStatus.Public),
                types = types,
                topLabels = topLabels
            });
        }

        // ===================== HÀM PHỤ =====================

        // Trả về lỗi nếu người dùng không phải nhà cung cấp đang đăng nhập, null nếu hợp lệ
        private IActionResult? CheckProvider()
        {
            if (!currentUser.IsLoggedIn)
            {
                return ApiResult.NeedLogin();
            }
            if (!currentUser.IsProvider)
            {
                return ApiResult.Forbidden("Chỉ tài khoản nhà cung cấp mới dùng được chức năng này.");
            }
            return null;
        }

        // Số bình luận (withRating = false) hoặc đánh giá (true) người khác để lại trên các bài của tôi
        private int CountPostFeedback(List<int> postIds, int myId, bool withRating)
        {
            if (postIds.Count == 0)
            {
                return 0;
            }
            IQueryable<Comment> query = db.Comments.Where(c => c.PostId != null && postIds.Contains(c.PostId.Value)
                && c.Status == CommentStatus.Visible && c.AccountId != myId);

            if (withRating)
            {
                return query.Count(c => c.Rating != null);
            }
            return query.Count(c => c.Rating == null);
        }

        // Lượt xem hồ sơ và lượt xem bài mỗi ngày trong khoảng thời gian gần nhất (ngày không có lượt xem vẫn có số 0)
        private List<object> BuildDailyViews(int myId, List<int> postIds, int days)
        {
            DateTime today = DateTime.Now.Date;
            DateTime start = today.AddDays(-(days - 1));

            var visits = db.Visits
                .Where(v => v.VisitedAt >= start
                    && ((v.Kind == VisitKind.Profile && v.ProfileAccountId == myId)
                        || (v.Kind == VisitKind.Post && v.PostId != null && postIds.Contains(v.PostId.Value))))
                .Select(v => new { v.VisitedAt, v.Kind })
                .ToList();

            List<object> result = new List<object>();
            for (int i = 0; i < days; i++)
            {
                DateTime day = start.AddDays(i);
                int profileViews = visits.Count(v => v.VisitedAt.Date == day && v.Kind == VisitKind.Profile);
                int postViews = visits.Count(v => v.VisitedAt.Date == day && v.Kind == VisitKind.Post);
                result.Add(new { date = day.ToString("yyyy-MM-dd"), profileViews = profileViews, postViews = postViews });
            }
            return result;
        }

        // Mảng 10 phần tử: số lượt đánh giá hồ sơ cho điểm 1, 2, ... 10
        private List<int> BuildRatingDistribution(int myId)
        {
            var groups = db.Comments
                .Where(c => c.TargetAccountId == myId && c.Rating != null && c.Status == CommentStatus.Visible)
                .GroupBy(c => c.Rating)
                .Select(g => new { Rating = g.Key, Count = g.Count() })
                .ToList();

            List<int> result = new List<int>();
            for (int score = 1; score <= 10; score++)
            {
                var found = groups.FirstOrDefault(g => g.Rating == score);
                result.Add(found == null ? 0 : found.Count);
            }
            return result;
        }

        // Tối đa 8 bài của tôi có nhiều lượt xem nhất
        private List<object> BuildTopPosts(List<int> postIds, Dictionary<int, int> postViews)
        {
            List<int> topIds = postViews
                .OrderByDescending(x => x.Value)
                .Take(8)
                .Select(x => x.Key)
                .ToList();

            Dictionary<int, string> titles = db.Posts
                .Where(p => topIds.Contains(p.Id))
                .ToDictionary(p => p.Id, p => p.Title);
            Dictionary<int, RatingInfo> ratings = RatingHelper.GetPostRatings(db, topIds);

            List<object> result = new List<object>();
            foreach (int id in topIds)
            {
                ratings.TryGetValue(id, out RatingInfo? rating);
                result.Add(new
                {
                    id = id,
                    title = titles.ContainsKey(id) ? titles[id] : "",
                    viewCount = postViews[id],
                    ratingAverage = rating?.Average,
                    ratingCount = rating?.Count ?? 0
                });
            }
            return result;
        }

        private object BuildFeedbackItem(Comment comment)
        {
            return new
            {
                id = comment.Id,
                kind = comment.PostId != null ? "Post" : "Profile",
                postId = comment.PostId,
                postTitle = comment.Post?.Title,
                authorId = comment.AccountId,
                authorName = comment.Author.DisplayName,
                content = comment.Content,
                rating = comment.Rating,
                createdAt = comment.CreatedAt
            };
        }

        private object BuildListItem(ProviderProfile profile, Dictionary<int, RatingInfo> ratings)
        {
            ratings.TryGetValue(profile.AccountId, out RatingInfo? rating);
            string description = profile.Description ?? "";
            if (description.Length > 160)
            {
                description = description.Substring(0, 160).TrimEnd() + "...";
            }

            return new
            {
                accountId = profile.AccountId,
                businessName = profile.BusinessName,
                description = description,
                logo = profile.Logo,
                address = profile.Address,
                ratingAverage = rating?.Average,
                ratingCount = rating?.Count ?? 0
            };
        }

        // Chưa có đánh giá thì trả về -1 để xếp sau cùng
        private double FindAverage(Dictionary<int, RatingInfo> ratings, int accountId)
        {
            ratings.TryGetValue(accountId, out RatingInfo? found);
            return found?.Average ?? -1;
        }

        private int FindCount(Dictionary<int, RatingInfo> ratings, int accountId)
        {
            ratings.TryGetValue(accountId, out RatingInfo? found);
            return found?.Count ?? 0;
        }
    }
}
