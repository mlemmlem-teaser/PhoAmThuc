using PhoAmThuc.Models;

namespace PhoAmThuc.Helpers
{
    // Kết quả tính điểm: Average là null khi chưa có đánh giá nào (hiển thị "Chưa có đánh giá", không hiển thị 0)
    public class RatingInfo
    {
        public double? Average { get; set; }
        public int Count { get; set; }
    }

    // Tính điểm trung bình từ các bình luận đánh giá còn hiển thị
    public static class RatingHelper
    {
        // Điểm của một hồ sơ (tính độc lập với điểm bài đăng)
        public static RatingInfo GetProfileRating(AppDbContext db, int accountId)
        {
            return Calculate(db.Comments.Where(c => c.TargetAccountId == accountId));
        }

        // Điểm của một bài đăng
        public static RatingInfo GetPostRating(AppDbContext db, int postId)
        {
            return Calculate(db.Comments.Where(c => c.PostId == postId));
        }

        // Điểm của nhiều bài đăng cùng lúc (dùng cho danh sách). Bài chưa có đánh giá sẽ không có trong kết quả
        public static Dictionary<int, RatingInfo> GetPostRatings(AppDbContext db, List<int> postIds)
        {
            var groups = db.Comments
                .Where(c => c.PostId != null && postIds.Contains(c.PostId.Value) && c.Rating != null && c.Status == CommentStatus.Visible)
                .GroupBy(c => c.PostId)
                .Select(g => new { PostId = g.Key, Count = g.Count(), Total = g.Sum(c => c.Rating ?? 0) })
                .ToList();

            Dictionary<int, RatingInfo> result = new Dictionary<int, RatingInfo>();
            foreach (var group in groups)
            {
                double average = Math.Round((double)group.Total / group.Count, 1);
                result[group.PostId!.Value] = new RatingInfo { Average = average, Count = group.Count };
            }
            return result;
        }

        // Điểm của tất cả bài đăng đã có đánh giá (dùng khi lọc hoặc sắp xếp theo điểm)
        public static Dictionary<int, RatingInfo> GetAllPostRatings(AppDbContext db)
        {
            var groups = db.Comments
                .Where(c => c.PostId != null && c.Rating != null && c.Status == CommentStatus.Visible)
                .GroupBy(c => c.PostId)
                .Select(g => new { PostId = g.Key, Count = g.Count(), Total = g.Sum(c => c.Rating ?? 0) })
                .ToList();

            Dictionary<int, RatingInfo> result = new Dictionary<int, RatingInfo>();
            foreach (var group in groups)
            {
                double average = Math.Round((double)group.Total / group.Count, 1);
                result[group.PostId!.Value] = new RatingInfo { Average = average, Count = group.Count };
            }
            return result;
        }

        // Điểm của tất cả hồ sơ đã có đánh giá (dùng cho hồ sơ nổi bật)
        public static Dictionary<int, RatingInfo> GetAllProfileRatings(AppDbContext db)
        {
            var groups = db.Comments
                .Where(c => c.TargetAccountId != null && c.Rating != null && c.Status == CommentStatus.Visible)
                .GroupBy(c => c.TargetAccountId)
                .Select(g => new { AccountId = g.Key, Count = g.Count(), Total = g.Sum(c => c.Rating ?? 0) })
                .ToList();

            Dictionary<int, RatingInfo> result = new Dictionary<int, RatingInfo>();
            foreach (var group in groups)
            {
                double average = Math.Round((double)group.Total / group.Count, 1);
                result[group.AccountId!.Value] = new RatingInfo { Average = average, Count = group.Count };
            }
            return result;
        }

        private static RatingInfo Calculate(IQueryable<Comment> comments)
        {
            IQueryable<Comment> ratings = comments.Where(c => c.Rating != null && c.Status == CommentStatus.Visible);

            int count = ratings.Count();
            if (count == 0)
            {
                return new RatingInfo { Average = null, Count = 0 };
            }

            double average = ratings.Average(c => c.Rating!.Value);
            return new RatingInfo { Average = Math.Round(average, 1), Count = count };
        }
    }
}
