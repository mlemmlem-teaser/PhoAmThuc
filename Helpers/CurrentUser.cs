using PhoAmThuc.Models;

namespace PhoAmThuc.Helpers
{
    // Cho biết ai đang đăng nhập. Mã tài khoản được lưu trong Session (cookie của trình duyệt),
    // thông tin tài khoản luôn đọc lại từ database nên khi quản trị viên đổi vai trò thì có hiệu lực ngay.
    public class CurrentUser
    {
        private const string sessionKey = "AccountId";

        private readonly IHttpContextAccessor contextAccessor;
        private readonly AppDbContext db;
        private Account? loadedAccount;
        private bool isLoaded;

        public CurrentUser(IHttpContextAccessor contextAccessor, AppDbContext db)
        {
            this.contextAccessor = contextAccessor;
            this.db = db;
        }

        // Tài khoản đang đăng nhập, null nếu là khách (hoặc tài khoản đang bị khóa)
        public Account? Account
        {
            get
            {
                LoadAccount();
                return loadedAccount;
            }
        }

        public bool IsLoggedIn => Account != null;
        public int Id => Account?.Id ?? 0;
        public string Role => Account?.Role ?? "";
        public bool IsAdmin => Role == Roles.Admin;
        public bool IsChef => Role == Roles.Chef;
        public bool IsProvider => Role == Roles.Provider;

        // Ghi nhớ đã đăng nhập
        public void Login(Account account)
        {
            GetSession().SetInt32(sessionKey, account.Id);
            loadedAccount = account;
            isLoaded = true;
        }

        // Xóa trạng thái đăng nhập
        public void Logout()
        {
            GetSession().Remove(sessionKey);
            loadedAccount = null;
            isLoaded = true;
        }

        private ISession GetSession()
        {
            return contextAccessor.HttpContext!.Session;
        }

        // Chỉ đọc database một lần cho mỗi request
        private void LoadAccount()
        {
            if (isLoaded)
            {
                return;
            }
            isLoaded = true;

            int? accountId = GetSession().GetInt32(sessionKey);
            if (accountId == null)
            {
                return;
            }

            Account? found = db.Accounts.FirstOrDefault(a => a.Id == accountId.Value);
            if (found == null || found.IsLocked)
            {
                GetSession().Remove(sessionKey);
                return;
            }
            loadedAccount = found;
        }
    }
}
