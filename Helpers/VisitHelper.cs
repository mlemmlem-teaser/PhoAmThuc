using PhoAmThuc.Models;

namespace PhoAmThuc.Helpers
{
    // Đếm lượt xem từ bảng Visits
    public static class VisitHelper
    {
        // Số lượt xem của nhiều bài đăng cùng lúc: key là mã bài đăng, value là số lượt
        public static Dictionary<int, int> GetPostViewCounts(AppDbContext db, List<int> postIds)
        {
            var groups = db.Visits
                .Where(v => v.Kind == VisitKind.Post && v.PostId != null && postIds.Contains(v.PostId.Value))
                .GroupBy(v => v.PostId)
                .Select(g => new { PostId = g.Key, Count = g.Count() })
                .ToList();

            Dictionary<int, int> result = new Dictionary<int, int>();
            foreach (var group in groups)
            {
                result[group.PostId!.Value] = group.Count;
            }
            return result;
        }
    }
}
