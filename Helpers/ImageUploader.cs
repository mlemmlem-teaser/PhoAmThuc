using System.Text.RegularExpressions;

namespace PhoAmThuc.Helpers
{
    // Kết quả lưu ảnh: thành công thì có Url, thất bại thì có Error
    public class UploadResult
    {
        public string? Url { get; set; }
        public string? Error { get; set; }
    }

    // Lưu ảnh người dùng tải lên vào wwwroot/uploads. Chỉ nhận jpg, png, webp, tối đa 2MB
    public class ImageUploader
    {
        private const int maxBytes = 2 * 1024 * 1024;

        private readonly IWebHostEnvironment environment;

        public ImageUploader(IWebHostEnvironment environment)
        {
            this.environment = environment;
        }

        public UploadResult SaveImage(IFormFile file)
        {
            if (file.Length == 0)
            {
                return new UploadResult { Error = "File ảnh đang trống." };
            }
            if (file.Length > maxBytes)
            {
                return new UploadResult { Error = "Ảnh quá lớn, tối đa 2MB." };
            }

            string extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (extension != ".jpg" && extension != ".jpeg" && extension != ".png" && extension != ".webp")
            {
                return new UploadResult { Error = "Chỉ nhận ảnh định dạng jpg, png hoặc webp." };
            }
            if (!HasCorrectSignature(file, extension))
            {
                return new UploadResult { Error = "File không phải ảnh hợp lệ." };
            }

            string folder = Path.Combine(environment.WebRootPath, "uploads");
            Directory.CreateDirectory(folder);

            string fileName = Guid.NewGuid().ToString("N") + extension;
            using (FileStream output = File.Create(Path.Combine(folder, fileName)))
            {
                file.CopyTo(output);
            }
            return new UploadResult { Url = "/uploads/" + fileName };
        }

        // Kiểm tra đường dẫn ảnh gửi lên có đúng là ảnh do website lưu hay không (chống đường dẫn lạ)
        public static bool IsUploadedImage(string? path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return false;
            }
            return Regex.IsMatch(path, "^/uploads/[A-Za-z0-9]+\\.(jpg|jpeg|png|webp)$");
        }

        // Đọc vài byte đầu file để chắc chắn đúng là ảnh, không chỉ đổi đuôi file
        private bool HasCorrectSignature(IFormFile file, string extension)
        {
            byte[] head = new byte[12];
            int count;
            using (Stream stream = file.OpenReadStream())
            {
                count = stream.Read(head, 0, head.Length);
            }
            if (count < 12)
            {
                return false;
            }

            if (extension == ".jpg" || extension == ".jpeg")
            {
                return head[0] == 0xFF && head[1] == 0xD8 && head[2] == 0xFF;
            }
            if (extension == ".png")
            {
                return head[0] == 0x89 && head[1] == 0x50 && head[2] == 0x4E && head[3] == 0x47;
            }
            // webp: "RIFF" ở đầu và "WEBP" ở byte 8 đến 11
            return head[0] == 0x52 && head[1] == 0x49 && head[2] == 0x46 && head[3] == 0x46
                && head[8] == 0x57 && head[9] == 0x45 && head[10] == 0x42 && head[11] == 0x50;
        }
    }
}
