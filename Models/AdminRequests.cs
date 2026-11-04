namespace PhoAmThuc.Models
{
    // Dữ liệu quản trị viên gửi lên khi thêm tài khoản mới
    public class AdminAccountCreateRequest
    {
        public string? Username { get; set; }
        public string? Email { get; set; }
        public string? Password { get; set; }
        public string? DisplayName { get; set; }
        public string? Role { get; set; }
    }

    // Dữ liệu khi sửa một tài khoản. Password để trống nghĩa là không đổi mật khẩu
    public class AdminAccountUpdateRequest
    {
        public string? Email { get; set; }
        public string? DisplayName { get; set; }
        public string? Role { get; set; }
        public bool IsLocked { get; set; }
        public string? Password { get; set; }
    }

    // Dữ liệu khi thêm hoặc sửa một nhãn
    public class LabelSaveRequest
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public bool IsActive { get; set; }
    }

    // Dữ liệu khi sửa một trang thông tin (giới thiệu, liên hệ, trợ giúp)
    public class InfoPageSaveRequest
    {
        public string? Title { get; set; }
        public string? Content { get; set; }
        public bool IsPublic { get; set; }
    }

    // Dữ liệu khi bật hoặc tắt một chức năng
    public class SettingSaveRequest
    {
        public bool Enabled { get; set; }
    }
}
