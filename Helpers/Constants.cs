namespace PhoAmThuc.Helpers
{
    // Các chuỗi cố định dùng nhiều nơi. Giá trị phải khớp với file WebsiteAmThuc2.sql
    // Dùng hằng số thay vì gõ chuỗi trực tiếp để tránh gõ sai chính tả.

    // Vai trò của tài khoản
    public static class Roles
    {
        public const string User = "User";
        public const string Chef = "Chef";
        public const string Provider = "Provider";
        public const string Admin = "Admin";

        // Tên vai trò hiển thị cho người dùng
        public static string GetDisplayName(string role)
        {
            switch (role)
            {
                case Chef: return "Đầu bếp";
                case Provider: return "Nhà cung cấp";
                case Admin: return "Quản trị viên";
                default: return "Người dùng";
            }
        }
    }

    // Trạng thái bài đăng
    public static class PostStatus
    {
        public const string Draft = "Draft";       // bản nháp
        public const string Public = "Public";     // công khai
        public const string Hidden = "Hidden";     // bị ẩn
        public const string Deleted = "Deleted";   // đã xóa
    }

    // Trạng thái bình luận
    public static class CommentStatus
    {
        public const string Visible = "Visible";
        public const string Hidden = "Hidden";
        public const string Deleted = "Deleted";
    }

    // Loại lượt xem
    public static class VisitKind
    {
        public const string Page = "Page";
        public const string Post = "Post";
        public const string Profile = "Profile";
    }

    // Mã của 5 nhãn hệ thống (cột SystemKey trong bảng Labels)
    public static class LabelKey
    {
        public const string Recipe = "Recipe";
        public const string Tip = "Tip";
        public const string Experience = "Experience";
        public const string Review = "Review";
        public const string Promo = "Promo";
    }

    // Tên các chức năng có thể bật/tắt (cột Name trong bảng Settings)
    public static class Features
    {
        public const string Register = "Register";
        public const string Post = "Post";
        public const string Comment = "Comment";
        public const string Rating = "Rating";
        public const string Promo = "Promo";
    }
}
