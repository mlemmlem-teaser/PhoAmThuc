using PhoAmThuc.Models;

namespace PhoAmThuc.Helpers
{
    // Quy tắc ai được xem, ai được sửa một bài đăng. Dùng chung cho trang và API
    public static class PostAccess
    {
        // Bài đã xóa: không ai xem được. Bài công khai: ai cũng xem được.
        // Bài nháp hoặc bị ẩn: chỉ tác giả và quản trị viên xem được.
        public static bool CanView(Post post, CurrentUser user)
        {
            if (post.Status == PostStatus.Deleted)
            {
                return false;
            }
            if (post.Status == PostStatus.Public)
            {
                return true;
            }
            return user.IsLoggedIn && (user.IsAdmin || post.AccountId == user.Id);
        }

        // Tác giả và quản trị viên được sửa, xóa bài (chưa bị xóa)
        public static bool CanEdit(Post post, CurrentUser user)
        {
            if (post.Status == PostStatus.Deleted)
            {
                return false;
            }
            return user.IsLoggedIn && (user.IsAdmin || post.AccountId == user.Id);
        }
    }
}
