using System.Text.RegularExpressions;
using PhoAmThuc.Models;

namespace PhoAmThuc.Helpers
{
    // Quy tắc kiểm tra và ghi hồ sơ doanh nghiệp, dùng chung cho nhà cung cấp tự sửa (ProviderApiController)
    // và quản trị viên sửa giúp (AdminProviderApiController)
    public static class ProviderProfileRules
    {
        // Trả về câu báo lỗi nếu dữ liệu sai, null nếu hợp lệ
        public static string? CheckInput(ProviderProfileSaveRequest request)
        {
            string businessName = (request.BusinessName ?? "").Trim();
            if (businessName.Length == 0 || businessName.Length > 200)
            {
                return "Tên doanh nghiệp không được để trống và tối đa 200 ký tự.";
            }
            if ((request.Description ?? "").Trim().Length > 2000)
            {
                return "Mô tả tối đa 2000 ký tự.";
            }
            if ((request.Address ?? "").Trim().Length > 300)
            {
                return "Địa chỉ tối đa 300 ký tự.";
            }

            string phone = (request.Phone ?? "").Trim();
            if (phone.Length > 30 || (phone.Length > 0 && !Regex.IsMatch(phone, "^[0-9 +().-]+$")))
            {
                return "Số điện thoại không hợp lệ.";
            }

            string email = (request.ContactEmail ?? "").Trim();
            if (email.Length > 254 || (email.Length > 0 && (!email.Contains('@') || !email.Contains('.'))))
            {
                return "Email liên hệ không hợp lệ.";
            }

            if (!IsEmptyOrHttpUrl(request.Website))
            {
                return "Website phải bắt đầu bằng http:// hoặc https://.";
            }
            if (!IsEmptyOrHttpUrl(request.SocialLink))
            {
                return "Liên kết mạng xã hội phải bắt đầu bằng http:// hoặc https://.";
            }

            string logo = (request.Logo ?? "").Trim();
            if (logo.Length > 0 && !ImageUploader.IsUploadedImage(logo))
            {
                return "Logo không hợp lệ.";
            }
            return null;
        }

        // Chép dữ liệu đã kiểm tra vào hồ sơ (chưa lưu xuống database)
        public static void Apply(ProviderProfile profile, ProviderProfileSaveRequest request)
        {
            profile.BusinessName = (request.BusinessName ?? "").Trim();
            profile.Description = CleanOptionalText(request.Description);
            profile.Logo = CleanOptionalText(request.Logo);
            profile.Address = CleanOptionalText(request.Address);
            profile.Phone = CleanOptionalText(request.Phone);
            profile.ContactEmail = CleanOptionalText(request.ContactEmail);
            profile.Website = CleanOptionalText(request.Website);
            profile.SocialLink = CleanOptionalText(request.SocialLink);
            profile.IsPublic = request.IsPublic;
        }

        private static bool IsEmptyOrHttpUrl(string? text)
        {
            string url = (text ?? "").Trim();
            if (url.Length == 0)
            {
                return true;
            }
            return url.Length <= 500 && Regex.IsMatch(url, "^https?://\\S+$", RegexOptions.IgnoreCase);
        }

        private static string? CleanOptionalText(string? text)
        {
            string cleaned = (text ?? "").Trim();
            return cleaned.Length == 0 ? null : cleaned;
        }
    }
}
