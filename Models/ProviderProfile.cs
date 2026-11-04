namespace PhoAmThuc.Models
{
    // Hồ sơ doanh nghiệp của tài khoản nhà cung cấp
    public class ProviderProfile
    {
        public int Id { get; set; }
        public int AccountId { get; set; }
        public string BusinessName { get; set; } = "";
        public string? Description { get; set; }
        public string? Logo { get; set; }
        public string? Address { get; set; }
        public string? Phone { get; set; }
        public string? ContactEmail { get; set; }
        public string? Website { get; set; }
        public string? SocialLink { get; set; }
        public bool IsPublic { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }

        public Account Account { get; set; } = null!;
    }
}
