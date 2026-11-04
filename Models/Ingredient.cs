namespace PhoAmThuc.Models
{
    // Một nguyên liệu của công thức
    public class Ingredient
    {
        public int Id { get; set; }
        public int RecipeId { get; set; }
        public int Position { get; set; }
        public string Name { get; set; } = "";
        public decimal? Amount { get; set; }
        public string? Unit { get; set; }
        public string? Note { get; set; }

        public Recipe Recipe { get; set; } = null!;
    }
}
