namespace PhoAmThuc.Models
{
    // Dữ liệu trình duyệt gửi lên khi tạo bình luận, đánh giá hoặc câu trả lời.
    //   - Bình luận dưới bài đăng: gửi PostId.
    //   - Đánh giá hồ sơ: gửi TargetAccountId và Rating.
    //   - Trả lời: gửi ParentId (bài đăng hoặc hồ sơ được lấy từ bình luận gốc).
    //   - Có Rating nghĩa là bình luận đánh giá (1 đến 10).
    public class CommentSaveRequest
    {
        public int? PostId { get; set; }
        public int? TargetAccountId { get; set; }
        public int? ParentId { get; set; }
        public string? Content { get; set; }
        public int? Rating { get; set; }
    }

    // Dữ liệu khi sửa bình luận của mình. Rating chỉ có tác dụng nếu đây là bình luận đánh giá
    public class CommentEditRequest
    {
        public string? Content { get; set; }
        public int? Rating { get; set; }
    }
}
