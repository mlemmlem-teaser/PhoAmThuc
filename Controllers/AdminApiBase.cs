using Microsoft.AspNetCore.Mvc;
using PhoAmThuc.Helpers;

namespace PhoAmThuc.Controllers
{
    // Lớp cha của mọi API quản trị. Mỗi API quản trị phải gọi CheckAdmin() đầu tiên:
    // kiểm tra quyền ở máy chủ, không dựa vào việc giao diện ẩn nút
    public abstract class AdminApiBase : ControllerBase
    {
        protected readonly CurrentUser currentUser;

        protected AdminApiBase(CurrentUser currentUser)
        {
            this.currentUser = currentUser;
        }

        // Trả về lỗi nếu người gọi không phải quản trị viên đang đăng nhập, null nếu hợp lệ
        protected IActionResult? CheckAdmin()
        {
            if (!currentUser.IsLoggedIn)
            {
                return ApiResult.NeedLogin();
            }
            if (!currentUser.IsAdmin)
            {
                return ApiResult.Forbidden("Chỉ quản trị viên mới dùng được chức năng này.");
            }
            return null;
        }

        protected int CountPages(int totalCount, int pageSize)
        {
            return Math.Max(1, (int)Math.Ceiling(totalCount / (double)pageSize));
        }

        protected int ClampPage(int page, int totalPages)
        {
            if (page < 1)
            {
                return 1;
            }
            if (page > totalPages)
            {
                return totalPages;
            }
            return page;
        }
    }
}
