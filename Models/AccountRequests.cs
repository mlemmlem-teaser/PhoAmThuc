namespace PhoAmThuc.Models
{
    // Dữ liệu mà trình duyệt gửi lên khi đăng ký
    public class RegisterRequest
    {
        public string? Username { get; set; }
        public string? Email { get; set; }
        public string? Password { get; set; }
        public string? DisplayName { get; set; }
    }

    // Dữ liệu mà trình duyệt gửi lên khi đăng nhập (Username có thể là tên đăng nhập hoặc email)
    public class LoginRequest
    {
        public string? Username { get; set; }
        public string? Password { get; set; }
    }
}
