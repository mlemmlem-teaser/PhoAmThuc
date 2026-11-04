using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhoAmThuc.Helpers;
using PhoAmThuc.Models;

namespace PhoAmThuc.Controllers
{
    // API quản lý hồ sơ nhà cung cấp (chỉ quản trị viên):
    //   GET    api/admin/providers?page=&q=     danh sách hồ sơ (cả hồ sơ không công khai) và các nhà cung cấp chưa có hồ sơ
    //   GET    api/admin/providers/{accountId}  chi tiết một hồ sơ
    //   PUT    api/admin/providers/{accountId}  tạo hoặc sửa hồ sơ của một tài khoản nhà cung cấp
    //   DELETE api/admin/providers/{accountId}  xóa hồ sơ (tài khoản và bài đăng vẫn giữ nguyên)
    [Route("api/admin/providers")]
    public class AdminProviderApiController : AdminApiBase
    {
        private const int pageSize = 15;

        private readonly AppDbContext db;

        public AdminProviderApiController(AppDbContext db, CurrentUser currentUser) : base(currentUser)
        {
            this.db = db;
        }

        [HttpGet("")]
        public IActionResult GetList(int page = 1, string? q = null)
        {
            IActionResult? denied = CheckAdmin();
            if (denied != null)
            {
                return denied;
            }

            IQueryable<ProviderProfile> query = db.ProviderProfiles.Include(p => p.Account);
            if (!string.IsNullOrWhiteSpace(q))
            {
                string keyword = q.Trim();
                query = query.Where(p => p.BusinessName.Contains(keyword) || p.Account.DisplayName.Contains(keyword));
            }

            int totalCount = query.Count();
            int totalPages = CountPages(totalCount, pageSize);
            page = ClampPage(page, totalPages);

            List<object> items = query
                .OrderBy(p => p.BusinessName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList()
                .Select(p => (object)new
                {
                    accountId = p.AccountId,
                    businessName = p.BusinessName,
                    accountName = p.Account.DisplayName,
                    isProvider = p.Account.Role == Roles.Provider,
                    isPublic = p.IsPublic
                })
                .ToList();

            // Các tài khoản nhà cung cấp chưa có hồ sơ, để quản trị viên tạo hồ sơ giúp
            var missingRows = db.Accounts
                .Where(a => a.Role == Roles.Provider && !db.ProviderProfiles.Any(p => p.AccountId == a.Id))
                .OrderBy(a => a.DisplayName)
                .Select(a => new { accountId = a.Id, displayName = a.DisplayName })
                .ToList();
            List<object> withoutProfile = missingRows.Select(r => (object)r).ToList();

            return ApiResult.Success(new
            {
                items = items,
                withoutProfile = withoutProfile,
                page = page,
                totalPages = totalPages,
                totalCount = totalCount
            });
        }

        [HttpGet("{accountId:int}")]
        public IActionResult GetOne(int accountId)
        {
            IActionResult? denied = CheckAdmin();
            if (denied != null)
            {
                return denied;
            }

            ProviderProfile? profile = db.ProviderProfiles.FirstOrDefault(p => p.AccountId == accountId);
            if (profile == null)
            {
                return ApiResult.NotFound("Không tìm thấy hồ sơ.");
            }

            return ApiResult.Success(new
            {
                accountId = profile.AccountId,
                businessName = profile.BusinessName,
                description = profile.Description,
                logo = profile.Logo,
                address = profile.Address,
                phone = profile.Phone,
                contactEmail = profile.ContactEmail,
                website = profile.Website,
                socialLink = profile.SocialLink,
                isPublic = profile.IsPublic
            });
        }

        [HttpPut("{accountId:int}")]
        public IActionResult Save(int accountId, [FromBody] ProviderProfileSaveRequest? request)
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

            Account? account = db.Accounts.FirstOrDefault(a => a.Id == accountId);
            if (account == null)
            {
                return ApiResult.NotFound("Không tìm thấy tài khoản.");
            }
            if (account.Role != Roles.Provider)
            {
                return ApiResult.Fail("Chỉ tạo hoặc sửa hồ sơ doanh nghiệp cho tài khoản có vai trò nhà cung cấp.");
            }

            string? error = ProviderProfileRules.CheckInput(request);
            if (error != null)
            {
                return ApiResult.Fail(error);
            }

            ProviderProfile? profile = db.ProviderProfiles.FirstOrDefault(p => p.AccountId == accountId);
            bool isNew = profile == null;
            if (profile == null)
            {
                profile = new ProviderProfile { AccountId = accountId };
                db.ProviderProfiles.Add(profile);
            }

            ProviderProfileRules.Apply(profile, request);
            if (!isNew)
            {
                profile.UpdatedAt = DateTime.Now;
            }
            db.SaveChanges();
            return ApiResult.Success(null, "Đã lưu hồ sơ nhà cung cấp.");
        }

        [HttpDelete("{accountId:int}")]
        public IActionResult Delete(int accountId)
        {
            IActionResult? denied = CheckAdmin();
            if (denied != null)
            {
                return denied;
            }

            ProviderProfile? profile = db.ProviderProfiles.FirstOrDefault(p => p.AccountId == accountId);
            if (profile == null)
            {
                return ApiResult.NotFound("Không tìm thấy hồ sơ.");
            }

            db.ProviderProfiles.Remove(profile);
            db.SaveChanges();
            return ApiResult.Success(null, "Đã xóa hồ sơ nhà cung cấp.");
        }
    }
}
