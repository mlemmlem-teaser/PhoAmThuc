namespace PhoAmThuc.Models
{
    // Một bước thực hiện của công thức
    public class RecipeStep
    {
        public int Id { get; set; }
        public int RecipeId { get; set; }
        public int Position { get; set; }
        public string Content { get; set; } = "";
        public string? Image { get; set; }

        public Recipe Recipe { get; set; } = null!;
    }
}
