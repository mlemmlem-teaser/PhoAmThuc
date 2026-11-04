namespace PhoAmThuc.Models
{
    // Dữ liệu khi người dùng sửa hồ sơ của mình
    public class ProfileSaveRequest
    {
        public string? DisplayName { get; set; }
        public string? Bio { get; set; }
        public string? Avatar { get; set; }   // đường dẫn ảnh đã tải lên, null nghĩa là bỏ ảnh
    }

    // Dữ liệu khi nhà cung cấp tạo hoặc sửa hồ sơ doanh nghiệp của mình
    public class ProviderProfileSaveRequest
    {
        public string? BusinessName { get; set; }
        public string? Description { get; set; }
        public string? Logo { get; set; }          // đường dẫn ảnh đã tải lên, null nghĩa là bỏ logo
        public string? Address { get; set; }
        public string? Phone { get; set; }
        public string? ContactEmail { get; set; }
        public string? Website { get; set; }
        public string? SocialLink { get; set; }
        public bool IsPublic { get; set; }
    }
}
