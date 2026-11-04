namespace PhoAmThuc.Models
{
    // Một lượt xem. Kind là Page, Post hoặc Profile (xem VisitKind trong Helpers/Constants.cs)
    public class Visit
    {
        public int Id { get; set; }
        public int? AccountId { get; set; }
        public string Kind { get; set; } = "Page";
        public int? PostId { get; set; }
        public int? ProfileAccountId { get; set; }
        public string Path { get; set; } = "";
        public DateTime VisitedAt { get; set; } = DateTime.Now;
    }
}
