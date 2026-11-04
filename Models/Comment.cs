namespace PhoAmThuc.Models
{
    // Bình luận. Nếu Rating khác null thì đây là bình luận đánh giá (1 đến 10).
    // Đích của bình luận là PostId (bài đăng) hoặc TargetAccountId (hồ sơ), đúng một trong hai.
    public class Comment
    {
        public int Id { get; set; }
        public int AccountId { get; set; }
        public int? PostId { get; set; }
        public int? TargetAccountId { get; set; }
        public int? ParentId { get; set; }
        public string Content { get; set; } = "";
        public int? Rating { get; set; }
        public string Status { get; set; } = "Visible";
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }

        public Account Author { get; set; } = null!;
        public Post? Post { get; set; }
        public Account? TargetAccount { get; set; }
        public Comment? Parent { get; set; }
        public List<Comment> Replies { get; set; } = new();
    }
}
