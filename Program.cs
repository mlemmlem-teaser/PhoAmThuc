using Microsoft.EntityFrameworkCore;
using PhoAmThuc.Helpers;
using PhoAmThuc.Models;

namespace PhoAmThuc
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            AddServices(builder);

            var app = builder.Build();

            SetupPipeline(app);

            app.Run();
        }

        // Đăng ký các dịch vụ mà website cần dùng
        private static void AddServices(WebApplicationBuilder builder)
        {
            builder.Services.AddControllersWithViews();

            // Kết nối SQL Server. Chuỗi kết nối nằm trong appsettings.json, mục ConnectionStrings:Default
            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

            // Session dùng để nhớ ai đang đăng nhập
            builder.Services.AddDistributedMemoryCache();
            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromHours(8);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
            });
            builder.Services.AddHttpContextAccessor();

            // Các lớp hỗ trợ, mỗi request tạo một đối tượng mới
            builder.Services.AddScoped<CurrentUser>();
            builder.Services.AddScoped<FeatureCheck>();
            builder.Services.AddScoped<HtmlCleaner>();
            builder.Services.AddScoped<ImageUploader>();
            builder.Services.AddScoped<PostCards>();
        }

        // Thiết lập thứ tự xử lý của mỗi request
        private static void SetupPipeline(WebApplication app)
        {
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            // Các lỗi không có nội dung (ví dụ 404) được chuyển sang trang thân thiện. API trả JSON thì không bị ảnh hưởng
            app.UseStatusCodePagesWithReExecute("/Home/StatusPage", "?code={0}");

            app.UseHttpsRedirection();
            app.UseStaticFiles();   // cần cho các file người dùng tải lên lúc website đang chạy
            app.UseRouting();
            app.UseSession();
            app.UseAuthorization();

            app.MapStaticAssets();
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}")
                .WithStaticAssets();
        }
    }
}
