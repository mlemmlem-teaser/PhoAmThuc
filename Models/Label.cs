namespace PhoAmThuc.Models
{
    // Nhãn phân loại bài đăng. SystemKey khác null nghĩa là nhãn hệ thống (Recipe, Tip, Experience, Review, Promo)
    public class Label
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string? Description { get; set; }
        public string? SystemKey { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public List<PostLabel> PostLabels { get; set; } = new();
    }
}
