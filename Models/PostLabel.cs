namespace PhoAmThuc.Models
{
    // Bảng nối giữa bài đăng và nhãn (nhiều - nhiều)
    public class PostLabel
    {
        public int PostId { get; set; }
        public int LabelId { get; set; }

        public Post Post { get; set; } = null!;
        public Label Label { get; set; } = null!;
    }
}
