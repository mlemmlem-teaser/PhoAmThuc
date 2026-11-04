using Microsoft.AspNetCore.Mvc;
using PhoAmThuc.Helpers;
using PhoAmThuc.Models;

namespace PhoAmThuc.Controllers
{
    // API cho các khu đề xuất ở trang chủ: GET api/home/summary
    //   topPosts   bài công khai có nhiều lượt xem nhất
    //   monthPosts bài công khai có nhiều lượt xem nhất trong tháng hiện tại
    //   profiles   hồ sơ nổi bật (điểm cao nhất, bằng điểm thì nhiều lượt đánh giá hơn, chưa có đánh giá xếp sau)
    //   recipes    công thức nấu ăn mới nhất
    // Khu nào chưa đủ dữ liệu thì được bù bằng bài mới nhất để trang chủ không bị trống
    [Route("api/home")]
    public class HomeApiController : ControllerBase
    {
        private const int sectionSize = 6;

        private readonly AppDbContext db;
        private readonly PostCards postCards;

        public HomeApiController(AppDbContext db, PostCards postCards)
        {
            this.db = db;
            this.postCards = postCards;
        }

        [HttpGet("summary")]
        public IActionResult GetSummary()
        {
            DateTime startOfMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);

            return ApiResult.Success(new
            {
                topPosts = BuildPostCards(FindMostViewedPostIds(null)),
                monthPosts = BuildPostCards(FindMostViewedPostIds(startOfMonth)),
                profiles = FindFeaturedProfiles(),
                recipes = BuildPostCards(FindLatestRecipeIds())
            });
        }

        private List<object> BuildPostCards(List<int> ids)
        {
            List<Post> posts = postCards.LoadPosts(ids);
            return postCards.BuildCards(posts);
        }

        // Mã các bài công khai nhiều lượt xem nhất (từ mốc thời gian since, null là từ trước đến nay)
        private List<int> FindMostViewedPostIds(DateTime? since)
        {
            IQueryable<Visit> visits = db.Visits.Where(v => v.Kind == VisitKind.Post && v.PostId != null);
            if (since != null)
            {
                DateTime start = since.Value;
                visits = visits.Where(v => v.VisitedAt >= start);
            }

            List<int?> viewedIds = visits
                .GroupBy(v => v.PostId)
                .OrderByDescending(g => g.Count())
                .Take(50)
                .Select(g => g.Key)
                .ToList();
            List<int> candidates = viewedIds.Where(id => id != null).Select(id => id!.Value).ToList();

            // Giữ đúng thứ tự nhiều lượt xem, bỏ các bài không còn công khai
            List<int> publicIds = db.Posts
                .Where(p => p.Status == PostStatus.Public && candidates.Contains(p.Id))
                .Select(p => p.Id)
                .ToList();
            List<int> chosen = candidates.Where(id => publicIds.Contains(id)).Take(sectionSize).ToList();

            // Chưa đủ thì bù bằng bài mới nhất
            int missing = sectionSize - chosen.Count;
            if (missing > 0)
            {
                List<int> newest = db.Posts
                    .Where(p => p.Status == PostStatus.Public && !chosen.Contains(p.Id))
                    .OrderByDescending(p => p.PublishedAt)
                    .ThenByDescending(p => p.Id)
                    .Take(missing)
                    .Select(p => p.Id)
                    .ToList();
                chosen.AddRange(newest);
            }
            return chosen;
        }

        private List<int> FindLatestRecipeIds()
        {
            return db.Posts
                .Where(p => p.Status == PostStatus.Public && p.PostLabels.Any(pl => pl.Label.SystemKey == LabelKey.Recipe))
                .OrderByDescending(p => p.PublishedAt)
                .ThenByDescending(p => p.Id)
                .Take(sectionSize)
                .Select(p => p.Id)
                .ToList();
        }

        // Hồ sơ nổi bật. Tài khoản quản trị và tài khoản bị khóa không được đưa vào
        private List<object> FindFeaturedProfiles()
        {
            Dictionary<int, RatingInfo> ratings = RatingHelper.GetAllProfileRatings(db);

            List<int> ratedIds = ratings
                .OrderByDescending(x => x.Value.Average)
                .ThenByDescending(x => x.Value.Count)
                .Select(x => x.Key)
                .ToList();

            List<Account> ratedAccounts = db.Accounts
                .Where(a => !a.IsLocked && a.Role != Roles.Admin && ratedIds.Contains(a.Id))
                .ToList();

            List<Account> chosen = new List<Account>();
            foreach (int id in ratedIds)
            {
                Account? account = ratedAccounts.FirstOrDefault(a => a.Id == id);
                if (account != null && chosen.Count < sectionSize)
                {
                    chosen.Add(account);
                }
            }

            // Chưa đủ thì thêm các hồ sơ chưa có đánh giá (xếp sau, không có điểm số)
            int missing = sectionSize - chosen.Count;
            if (missing > 0)
            {
                List<int> chosenIds = chosen.Select(a => a.Id).ToList();
                List<Account> unrated = db.Accounts
                    .Where(a => !a.IsLocked && a.Role != Roles.Admin && !chosenIds.Contains(a.Id))
                    .OrderBy(a => a.DisplayName)
                    .Take(missing)
                    .ToList();
                chosen.AddRange(unrated);
            }

            return chosen.Select(a => BuildProfileCard(a, ratings)).ToList();
        }

        private object BuildProfileCard(Account account, Dictionary<int, RatingInfo> ratings)
        {
            ratings.TryGetValue(account.Id, out RatingInfo? rating);
            return new
            {
                id = account.Id,
                displayName = account.DisplayName,
                avatar = account.Avatar,
                roleName = Roles.GetDisplayName(account.Role),
                ratingAverage = rating?.Average,
                ratingCount = rating?.Count ?? 0
            };
        }
    }
}
