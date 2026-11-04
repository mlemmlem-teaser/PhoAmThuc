using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using PhoAmThuc.Helpers;
using PhoAmThuc.Models;

namespace PhoAmThuc.Controllers
{
    // API quản lý tài khoản (chỉ quản trị viên):
    //   GET    api/admin/accounts?page=&q=&role=   danh sách
    //   POST   api/admin/accounts                   thêm tài khoản
    //   PUT    api/admin/accounts/{id}              sửa thông tin, vai trò, khóa/mở khóa, đặt lại mật khẩu
    //   DELETE api/admin/accounts/{id}              xóa (chỉ khi tài khoản chưa có dữ liệu liên quan)
    [Route("api/admin/accounts")]
    public class AdminAccountApiController : AdminApiBase
    {
        private const int pageSize = 15;

        private readonly AppDbContext db;

        public AdminAccountApiController(AppDbContext db, CurrentUser currentUser) : base(currentUser)
        {
            this.db = db;
        }

        [HttpGet("")]
        public IActionResult GetList(int page = 1, string? q = null, string? role = null)
        {
            IActionResult? denied = CheckAdmin();
            if (denied != null)
            {
                return denied;
            }

            IQueryable<Account> query = db.Accounts;
            if (!string.IsNullOrWhiteSpace(q))
            {
                string keyword = q.Trim();
                query = query.Where(a => a.Username.Contains(keyword) || a.Email.Contains(keyword) || a.DisplayName.Contains(keyword));
            }
            if (IsValidRole(role))
            {
                query = query.Where(a => a.Role == role);
            }

            int totalCount = query.Count();
            int totalPages = CountPages(totalCount, pageSize);
            page = ClampPage(page, totalPages);

            List<Account> accounts = query
                .OrderBy(a => a.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            List<int> ids = accounts.Select(a => a.Id).ToList();
            var postGroups = db.Posts
                .Where(p => ids.Contains(p.AccountId) && p.Status != PostStatus.Deleted)
                .GroupBy(p => p.AccountId)
                .Select(g => new { AccountId = g.Key, Count = g.Count() })
                .ToList();

            List<object> items = new List<object>();
            foreach (Account account in accounts)
            {
                var found = postGroups.FirstOrDefault(g => g.AccountId == account.Id);
                items.Add(new
                {
                    id = account.Id,
                    username = account.Username,
                    email = account.Email,
                    displayName = account.DisplayName,
                    role = account.Role,
                    roleName = Roles.GetDisplayName(account.Role),
                    isLocked = account.IsLocked,
                    createdAt = account.CreatedAt,
                    postCount = found == null ? 0 : found.Count,
                    isMe = account.Id == currentUser.Id
                });
            }
            return ApiResult.Success(new { items = items, page = page, totalPages = totalPages, totalCount = totalCount });
        }

        [HttpPost("")]
        public IActionResult Create([FromBody] AdminAccountCreateRequest? request)
        {
            IActionResult? denied = CheckAdmin();
            if (denied != null)
            {
                return denied;
            }
            if (request == null)
            {
                return ApiResult.Fail("Dữ liệu gửi lên không hợp lệ.");
            }

            string username = (request.Username ?? "").Trim();
            string email = (request.Email ?? "").Trim();
            string password = request.Password ?? "";
            string displayName = (request.DisplayName ?? "").Trim();
            string role = request.Role ?? "";

            string? error = CheckUsername(username) ?? CheckEmail(email) ?? CheckPassword(password, true)
                ?? CheckDisplayName(displayName) ?? CheckRole(role);
            if (error != null)
            {
                return ApiResult.Fail(error);
            }
            if (db.Accounts.Any(a => a.Username == username))
            {
                return ApiResult.Fail("Tên đăng nhập này đã có người dùng.");
            }
            if (db.Accounts.Any(a => a.Email == email))
            {
                return ApiResult.Fail("Email này đã được đăng ký.");
            }

            Account account = new Account
            {
                Username = username,
                Email = email,
                Password = password,
                DisplayName = displayName == "" ? username : displayName,
                Role = role
            };
            db.Accounts.Add(account);
            db.SaveChanges();
            return ApiResult.Success(new { id = account.Id }, "Đã thêm tài khoản.");
        }

        [HttpPut("{id:int}")]
        public IActionResult Update(int id, [FromBody] AdminAccountUpdateRequest? request)
        {
            IActionResult? denied = CheckAdmin();
            if (denied != null)
            {
                return denied;
            }
            if (request == null)
            {
                return ApiResult.Fail("Dữ liệu gửi lên không hợp lệ.");
            }

            Account? account = db.Accounts.FirstOrDefault(a => a.Id == id);
            if (account == null)
            {
                return ApiResult.NotFound("Không tìm thấy tài khoản.");
            }

            string email = (request.Email ?? "").Trim();
            string displayName = (request.DisplayName ?? "").Trim();
            string role = request.Role ?? "";
            string password = request.Password ?? "";

            string? error = CheckEmail(email) ?? CheckDisplayName(displayName) ?? CheckRole(role);
            if (error == null && password.Length > 0)
            {
                error = CheckPassword(password, false);
            }
            if (error == null && displayName.Length == 0)
            {
                error = "Tên hiển thị không được để trống.";
            }
            if (error != null)
            {
                return ApiResult.Fail(error);
            }
            if (db.Accounts.Any(a => a.Email == email && a.Id != id))
            {
                return ApiResult.Fail("Email này đã được tài khoản khác sử dụng.");
            }

            error = CheckRoleAndLockRules(account, role, request.IsLocked);
            if (error != null)
            {
                return ApiResult.Fail(error);
            }

            account.Email = email;
            account.DisplayName = displayName;
            account.Role = role;
            account.IsLocked = request.IsLocked;
            if (password.Length > 0)
            {
                account.Password = password;
            }
            account.UpdatedAt = DateTime.Now;
            db.SaveChanges();
            return ApiResult.Success(null, "Đã lưu tài khoản.");
        }

        [HttpDelete("{id:int}")]
        public IActionResult Delete(int id)
        {
            IActionResult? denied = CheckAdmin();
            if (denied != null)
            {
                return denied;
            }

            Account? account = db.Accounts.FirstOrDefault(a => a.Id == id);
            if (account == null)
            {
                return ApiResult.NotFound("Không tìm thấy tài khoản.");
            }
            if (id == currentUser.Id)
            {
                return ApiResult.Fail("Bạn không thể xóa chính tài khoản đang đăng nhập.");
            }
            if (HasRelatedData(id))
            {
                return ApiResult.Fail("Tài khoản đã có dữ liệu liên quan (bài đăng, bình luận, lượt xem...) nên không thể xóa. Hãy khóa tài khoản thay vì xóa.");
            }

            db.Accounts.Remove(account);
            db.SaveChanges();
            return ApiResult.Success(null, "Đã xóa tài khoản.");
        }

        // ===================== QUY TẮC VÀ KIỂM TRA =====================

        private bool IsValidRole(string? role)
        {
            return role == Roles.User || role == Roles.Chef || role == Roles.Provider || role == Roles.Admin;
        }

        // Không cho quản trị viên tự hạ quyền hoặc tự khóa mình, và luôn phải còn ít nhất một quản trị viên đang hoạt động
        private string? CheckRoleAndLockRules(Account account, string newRole, bool newLocked)
        {
            if (account.Id == currentUser.Id && (newRole != Roles.Admin || newLocked))
            {
                return "Bạn không thể tự đổi vai trò hoặc tự khóa tài khoản của mình.";
            }

            bool losesAdmin = account.Role == Roles.Admin && (newRole != Roles.Admin || newLocked);
            if (losesAdmin)
            {
                int otherAdmins = db.Accounts.Count(a => a.Role == Roles.Admin && !a.IsLocked && a.Id != account.Id);
                if (otherAdmins == 0)
                {
                    return "Website phải luôn còn ít nhất một quản trị viên đang hoạt động.";
                }
            }
            return null;
        }

        // Tài khoản đã gắn với dữ liệu khác thì xóa sẽ làm hỏng liên kết và thống kê
        private bool HasRelatedData(int accountId)
        {
            return db.Posts.Any(p => p.AccountId == accountId)
                || db.Comments.Any(c => c.AccountId == accountId || c.TargetAccountId == accountId)
                || db.ProviderProfiles.Any(p => p.AccountId == accountId)
                || db.Visits.Any(v => v.AccountId == accountId || v.ProfileAccountId == accountId)
                || db.InfoPages.Any(p => p.UpdatedByAccountId == accountId)
                || db.Settings.Any(s => s.UpdatedByAccountId == accountId);
        }

        private string? CheckUsername(string username)
        {
            if (username.Length < 3 || username.Length > 50)
            {
                return "Tên đăng nhập phải từ 3 đến 50 ký tự.";
            }
            if (!Regex.IsMatch(username, "^[A-Za-z0-9_.]+$"))
            {
                return "Tên đăng nhập chỉ gồm chữ không dấu, số, dấu chấm và dấu gạch dưới.";
            }
            return null;
        }

        private string? CheckEmail(string email)
        {
            if (email.Length > 254 || !email.Contains('@') || !email.Contains('.'))
            {
                return "Email không hợp lệ.";
            }
            return null;
        }

        // mustHave = true: bắt buộc phải nhập (khi thêm mới)
        private string? CheckPassword(string password, bool mustHave)
        {
            if (password.Length == 0 && !mustHave)
            {
                return null;
            }
            if (password.Length < 4 || password.Length > 100)
            {
                return "Mật khẩu phải từ 4 đến 100 ký tự.";
            }
            return null;
        }

        private string? CheckDisplayName(string displayName)
        {
            if (displayName.Length > 100)
            {
                return "Tên hiển thị tối đa 100 ký tự.";
            }
            return null;
        }

        private string? CheckRole(string role)
        {
            return IsValidRole(role) ? null : "Vai trò không hợp lệ.";
        }
    }
}
