namespace PhoAmThuc.Models
{
    // Một tài khoản người dùng. Mỗi tài khoản có đúng một vai trò (xem Helpers/Constants.cs)
    public class Account
    {
        public int Id { get; set; }
        public string Username { get; set; } = "";
        public string Email { get; set; } = "";
        public string Password { get; set; } = "";
        public string DisplayName { get; set; } = "";
        public string? Avatar { get; set; }
        public string? Bio { get; set; }
        public string Role { get; set; } = "User";
        public bool IsLocked { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }
    }
}
