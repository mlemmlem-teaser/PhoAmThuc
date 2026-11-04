namespace PhoAmThuc.Models
{
    // Trang thông tin tĩnh (giới thiệu, liên hệ, trợ giúp). Slug là mã trang, ví dụ "about"
    public class InfoPage
    {
        public int Id { get; set; }
        public string Slug { get; set; } = "";
        public string Title { get; set; } = "";
        public string Content { get; set; } = "";
        public bool IsPublic { get; set; } = true;
        public int? UpdatedByAccountId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }
    }
}
