namespace PhoAmThuc.Models
{
    // Công tắc bật/tắt một chức năng của website. Tên chức năng nằm trong Features (Helpers/Constants.cs)
    public class Setting
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public bool Enabled { get; set; } = true;
        public string? Description { get; set; }
        public int? UpdatedByAccountId { get; set; }
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}
