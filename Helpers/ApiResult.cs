using Microsoft.AspNetCore.Mvc;

namespace PhoAmThuc.Helpers
{
    // Mọi API trả về JSON cùng một dạng: { ok: true/false, message: "...", data: ... }
    // Nhờ vậy code JavaScript chỉ cần kiểm tra result.ok
    public static class ApiResult
    {
        public static IActionResult Success(object? data = null, string message = "")
        {
            return new JsonResult(new { ok = true, message = message, data = data });
        }

        // Dữ liệu gửi lên không hợp lệ
        public static IActionResult Fail(string message)
        {
            return new JsonResult(new { ok = false, message = message }) { StatusCode = 400 };
        }

        // Chưa đăng nhập
        public static IActionResult NeedLogin()
        {
            return new JsonResult(new { ok = false, message = "Bạn cần đăng nhập để thực hiện thao tác này." }) { StatusCode = 401 };
        }

        // Đã đăng nhập nhưng không đủ quyền
        public static IActionResult Forbidden(string message = "Bạn không có quyền thực hiện thao tác này.")
        {
            return new JsonResult(new { ok = false, message = message }) { StatusCode = 403 };
        }

        // Không tìm thấy dữ liệu
        public static IActionResult NotFound(string message = "Không tìm thấy dữ liệu.")
        {
            return new JsonResult(new { ok = false, message = message }) { StatusCode = 404 };
        }
    }
}
