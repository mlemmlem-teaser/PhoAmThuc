namespace PhoAmThuc.Models
{
    // Một bài đăng. Loại bài (công thức, quảng bá...) được xác định bằng nhãn, không có cột loại riêng
    public class Post
    {
        public int Id { get; set; }
        public int AccountId { get; set; }
        public string Title { get; set; } = "";
        public string? Summary { get; set; }
        public string Content { get; set; } = "";
        public string? Cover { get; set; }
        public string Status { get; set; } = "Draft";
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }
        public DateTime? PublishedAt { get; set; }

        public Account Author { get; set; } = null!;
        public List<PostLabel> PostLabels { get; set; } = new();
        public Recipe? Recipe { get; set; }
        public List<Comment> Comments { get; set; } = new();
    }
}
