namespace PhoAmThuc.Models
{
    // Dữ liệu riêng của bài đăng công thức nấu ăn
    public class Recipe
    {
        public int Id { get; set; }
        public int PostId { get; set; }
        public int? PrepMinutes { get; set; }
        public int? CookMinutes { get; set; }
        public int? Servings { get; set; }
        public string? Difficulty { get; set; }   // Easy, Medium, Hard hoặc để trống
        public string? Note { get; set; }

        public Post Post { get; set; } = null!;
        public List<Ingredient> Ingredients { get; set; } = new();
        public List<RecipeStep> Steps { get; set; } = new();
    }
}
