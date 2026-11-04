using PhoAmThuc.Models;

namespace PhoAmThuc.Helpers
{
    // Kiểm tra một chức năng đang bật hay tắt (theo bảng Settings).
    // Controller phải gọi hàm này trước khi xử lý, không chỉ ẩn nút trên giao diện.
    public class FeatureCheck
    {
        private readonly AppDbContext db;

        public FeatureCheck(AppDbContext db)
        {
            this.db = db;
        }

        // Chưa có dòng cấu hình thì coi như đang bật
        public bool IsOn(string featureName)
        {
            Setting? setting = db.Settings.FirstOrDefault(s => s.Name == featureName);
            return setting == null || setting.Enabled;
        }
    }
}
