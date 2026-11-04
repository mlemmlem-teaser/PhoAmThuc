using System.Net;
using System.Text.RegularExpressions;
using Ganss.Xss;

namespace PhoAmThuc.Helpers
{
    // Làm sạch nội dung HTML của bài đăng bằng thư viện HtmlSanitizer (gói NuGet "HtmlSanitizer").
    // Chỉ giữ lại các thẻ an toàn: chữ đậm/nghiêng/gạch chân, màu, cỡ chữ, danh sách, ảnh, đường dẫn.
    // Video YouTube được lưu dạng <div data-youtube="MÃ_VIDEO"></div>, khi hiển thị mới đổi thành iframe (hàm ShowVideos).
    public class HtmlCleaner
    {
        public string Clean(string? html)
        {
            return CreateSanitizer().Sanitize(html ?? "");
        }

        // Có chữ, ảnh hoặc video thì coi là có nội dung
        public bool HasContent(string cleanHtml)
        {
            if (cleanHtml.Contains("<img") || cleanHtml.Contains("data-youtube"))
            {
                return true;
            }
            return GetPlainText(cleanHtml).Length > 0;
        }

        // Tạo mô tả ngắn từ nội dung (khi người viết không tự nhập)
        public string MakeSummary(string cleanHtml, int maxLength)
        {
            string text = GetPlainText(cleanHtml);
            if (text.Length <= maxLength)
            {
                return text;
            }
            return text.Substring(0, maxLength).TrimEnd() + "...";
        }

        // Đổi chỗ đánh dấu video thành iframe YouTube. Mã video chỉ gồm 11 ký tự chữ, số, gạch nên an toàn
        public string ShowVideos(string cleanHtml)
        {
            string pattern = "<div[^>]*data-youtube=\"([A-Za-z0-9_-]{11})\"[^>]*>\\s*</div>";
            return Regex.Replace(cleanHtml, pattern, match =>
                "<div class=\"videobox\"><iframe src=\"https://www.youtube.com/embed/" + match.Groups[1].Value +
                "\" title=\"Video YouTube\" loading=\"lazy\" allowfullscreen></iframe></div>");
        }

        private HtmlSanitizer CreateSanitizer()
        {
            HtmlSanitizer sanitizer = new HtmlSanitizer();

            sanitizer.AllowedTags.Add("font");
            sanitizer.AllowedTags.Add("span");
            sanitizer.AllowedTags.Add("div");

            sanitizer.AllowedAttributes.Add("style");
            sanitizer.AllowedAttributes.Add("class");
            sanitizer.AllowedAttributes.Add("color");
            sanitizer.AllowedAttributes.Add("size");
            sanitizer.AllowedAttributes.Add("data-youtube");

            sanitizer.AllowedCssProperties.Add("color");
            sanitizer.AllowedCssProperties.Add("font-size");
            sanitizer.AllowedCssProperties.Add("font-weight");
            sanitizer.AllowedCssProperties.Add("font-style");
            sanitizer.AllowedCssProperties.Add("text-decoration");
            sanitizer.AllowedCssProperties.Add("text-align");

            return sanitizer;
        }

        // Bỏ hết thẻ HTML, chỉ giữ chữ
        private string GetPlainText(string html)
        {
            string text = Regex.Replace(html, "<[^>]+>", " ");
            text = WebUtility.HtmlDecode(text);
            return Regex.Replace(text, "\\s+", " ").Trim();
        }
    }
}
