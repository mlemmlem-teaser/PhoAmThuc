using Microsoft.AspNetCore.Mvc.Filters;
using PhoAmThuc.Models;

namespace PhoAmThuc.Helpers
{
    // Gắn [RecordVisit] lên một hành động trả về trang thì mỗi lần mở trang đó được ghi 1 lượt xem (Kind = Page).
    // Số liệu này dùng cho biểu đồ lượt xem theo ngày ở dashboard quản trị.
    // Các trang chi tiết bài đăng và hồ sơ có cách ghi riêng (Kind = Post, Profile) nên không gắn thuộc tính này.
    public class RecordVisitAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            try
            {
                HttpContext http = context.HttpContext;
                if (http.Request.Method == "GET")
                {
                    SaveVisit(http);
                }
            }
            catch (Exception)
            {
                // Ghi lượt xem bị lỗi thì bỏ qua, không được làm hỏng việc hiển thị trang
            }
            base.OnActionExecuting(context);
        }

        private void SaveVisit(HttpContext http)
        {
            AppDbContext db = http.RequestServices.GetRequiredService<AppDbContext>();
            CurrentUser currentUser = http.RequestServices.GetRequiredService<CurrentUser>();

            string path = (http.Request.Path.Value ?? "/") + http.Request.QueryString.Value;
            if (path.Length > 500)
            {
                path = path.Substring(0, 500);
            }

            Visit visit = new Visit
            {
                AccountId = currentUser.IsLoggedIn ? currentUser.Id : null,
                Kind = VisitKind.Page,
                Path = path
            };
            db.Visits.Add(visit);
            db.SaveChanges();
        }
    }
}
