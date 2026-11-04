using Microsoft.EntityFrameworkCore;
using PhoAmThuc.Models;

namespace PhoAmThuc.Helpers
{
    // Dựng "thẻ bài đăng" (dữ liệu ngắn gọn hiển thị trong danh sách) cho mọi nơi cần:
    // danh sách bài, trang chủ, trang hồ sơ, trang quản lý bài của tôi
    public class PostCards
    {
        private readonly AppDbContext db;

        public PostCards(AppDbContext db)
        {
            this.db = db;
        }

        // Nạp các bài đăng (kèm tác giả và nhãn) theo đúng thứ tự mã đã cho
        public List<Post> LoadPosts(List<int> orderedIds)
        {
            List<Post> loaded = db.Posts
                .Include(p => p.Author)
                .Include(p => p.PostLabels).ThenInclude(pl => pl.Label)
                .Where(p => orderedIds.Contains(p.Id))
                .ToList();

            List<Post> result = new List<Post>();
            foreach (int id in orderedIds)
            {
                Post? found = loaded.FirstOrDefault(p => p.Id == id);
                if (found != null)
                {
                    result.Add(found);
                }
            }
            return result;
        }

        // Dựng thẻ cho một danh sách bài (bài phải được nạp sẵn tác giả và nhãn, xem LoadPosts)
        public List<object> BuildCards(List<Post> posts)
        {
            List<int> ids = posts.Select(p => p.Id).ToList();
            Dictionary<int, RatingInfo> ratings = RatingHelper.GetPostRatings(db, ids);
            Dictionary<int, int> views = VisitHelper.GetPostViewCounts(db, ids);

            return posts.Select(p => BuildCard(p, ratings, views)).ToList();
        }

        private object BuildCard(Post post, Dictionary<int, RatingInfo> ratings, Dictionary<int, int> views)
        {
            RatingInfo rating = new RatingInfo();
            ratings.TryGetValue(post.Id, out RatingInfo? found);
            if (found != null)
            {
                rating = found;
            }

            views.TryGetValue(post.Id, out int viewCount);

            return new
            {
                id = post.Id,
                title = post.Title,
                summary = post.Summary,
                cover = post.Cover,
                status = post.Status,
                authorId = post.AccountId,
                authorName = post.Author.DisplayName,
                labels = post.PostLabels
                    .Select(pl => new { id = pl.Label.Id, name = pl.Label.Name, systemKey = pl.Label.SystemKey })
                    .ToList(),
                ratingAverage = rating.Average,
                ratingCount = rating.Count,
                viewCount = viewCount,
                createdAt = post.CreatedAt,
                updatedAt = post.UpdatedAt,
                publishedAt = post.PublishedAt
            };
        }
    }
}
