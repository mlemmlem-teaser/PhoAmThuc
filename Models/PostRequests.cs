namespace PhoAmThuc.Models
{
    // Dữ liệu trình duyệt gửi lên khi lưu một bài đăng (tạo mới hoặc sửa)
    public class PostSaveRequest
    {
        public string? Title { get; set; }
        public string? Summary { get; set; }
        public string? Content { get; set; }
        public string? Cover { get; set; }
        public string? Status { get; set; }          // chỉ nhận Draft hoặc Public
        public List<int>? LabelIds { get; set; }
        public RecipeRequest? Recipe { get; set; }   // null nghĩa là không phải bài công thức
    }

    // Phần công thức của bài đăng
    public class RecipeRequest
    {
        public int? PrepMinutes { get; set; }
        public int? CookMinutes { get; set; }
        public int? Servings { get; set; }
        public string? Difficulty { get; set; }      // Easy, Medium, Hard hoặc để trống
        public string? Note { get; set; }
        public List<IngredientRequest>? Ingredients { get; set; }
        public List<StepRequest>? Steps { get; set; }
    }

    public class IngredientRequest
    {
        public string? Name { get; set; }
        public decimal? Amount { get; set; }
        public string? Unit { get; set; }
        public string? Note { get; set; }
    }

    public class StepRequest
    {
        public string? Content { get; set; }
        public string? Image { get; set; }
    }

    // Đổi trạng thái nhanh một bài đăng (Draft, Public, Hidden)
    public class StatusRequest
    {
        public string? Status { get; set; }
    }
}
