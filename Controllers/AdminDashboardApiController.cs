using Microsoft.AspNetCore.Mvc;
using PhoAmThuc.Helpers;
using PhoAmThuc.Models;

namespace PhoAmThuc.Controllers
{
    // API dashboard quản trị: GET api/admin/dashboard?days=30
    // Lượt xem được đếm theo từng lần mở trang, không phải số người dùng khác nhau
    [Route("api/admin/dashboard")]
    public class AdminDashboardApiController : AdminApiBase
    {
        private readonly AppDbContext db;

        public AdminDashboardApiController(AppDbContext db, CurrentUser currentUser) : base(currentUser)
        {
            this.db = db;
        }

        [HttpGet("")]
        public IActionResult GetDashboard(int days = 30)
        {
            IActionResult? denied = CheckAdmin();
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

            return ApiResult.Success(new
            {
                totals = new
                {
                    accounts = db.Accounts.Count(),
                    publicPosts = db.Posts.Count(p => p.Status == PostStatus.Public),
                    comments = db.Comments.Count(c => c.Status == CommentStatus.Visible && c.Rating == null),
                    ratings = db.Comments.Count(c => c.Status == CommentStatus.Visible && c.Rating != null)
                },
                days = BuildDailyViews(days),
                postTypes = BuildPostTypes(),
                topPosts = BuildTopPosts()
            });
        }

        // Lượt xem mỗi ngày chia theo loại (trang, bài đăng, hồ sơ). Ngày không có lượt xem vẫn có số 0
        private List<object> BuildDailyViews(int days)
        {
            DateTime start = DateTime.Now.Date.AddDays(-(days - 1));

            var groups = db.Visits
                .Where(v => v.VisitedAt >= start)
                .GroupBy(v => new { Day = v.VisitedAt.Date, v.Kind })
                .Select(g => new { Day = g.Key.Day, Kind = g.Key.Kind, Count = g.Count() })
                .ToList();

            List<(DateTime Day, string Kind, int Count)> counts = groups.Select(g => (g.Day, g.Kind, g.Count)).ToList();

            List<object> result = new List<object>();
            for (int i = 0; i < days; i++)
            {
                DateTime day = start.AddDays(i);
                result.Add(new
                {
                    date = day.ToString("yyyy-MM-dd"),
                    pageViews = SumViews(counts, day, VisitKind.Page),
                    postViews = SumViews(counts, day, VisitKind.Post),
                    profileViews = SumViews(counts, day, VisitKind.Profile)
                });
            }
            return result;
        }

        private int SumViews(List<(DateTime Day, string Kind, int Count)> counts, DateTime day, string kind)
        {
            return counts.Where(c => c.Day == day && c.Kind == kind).Sum(c => c.Count);
        }

        // Số bài công khai theo từng loại (nhãn hệ thống)
        private List<object> BuildPostTypes()
        {
            List<Label> typeLabels = db.Labels.Where(l => l.SystemKey != null).ToList();
            return typeLabels
                .Select(l => new
                {
                    name = l.Name,
                    count = db.PostLabels.Count(pl => pl.LabelId == l.Id && pl.Post.Status == PostStatus.Public)
                })
                .OrderByDescending(x => x.count)
                .Select(x => (object)x)
                .ToList();
        }

        // 10 bài công khai có nhiều lượt xem nhất
        private List<object> BuildTopPosts()
        {
            var groups = db.Visits
                .Where(v => v.Kind == VisitKind.Post && v.PostId != null)
                .GroupBy(v => v.PostId)
                .OrderByDescending(g => g.Count())
                .Take(30)
                .Select(g => new { PostId = g.Key, Count = g.Count() })
                .ToList();

            List<int> ids = groups.Where(g => g.PostId != null).Select(g => g.PostId!.Value).ToList();
            Dictionary<int, string> titles = db.Posts
                .Where(p => p.Status == PostStatus.Public && ids.Contains(p.Id))
                .ToDictionary(p => p.Id, p => p.Title);

            List<object> result = new List<object>();
            foreach (var group in groups)
            {
                int id = group.PostId ?? 0;
                if (titles.ContainsKey(id) && result.Count < 10)
                {
                    result.Add(new { id = id, title = titles[id], viewCount = group.Count });
                }
            }
            return result;
        }
    }
}
