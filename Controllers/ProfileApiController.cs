using Microsoft.AspNetCore.Mvc;
using PhoAmThuc.Helpers;
using PhoAmThuc.Models;

namespace PhoAmThuc.Controllers
{
    // API hồ sơ:
    //   GET  api/profile/{id}        thông tin công khai của một hồ sơ (không có email, mật khẩu)
    //   POST api/profile/{id}/view   ghi một lượt xem hồ sơ
    //   PUT  api/profile/me          sửa hồ sơ của chính mình
    [Route("api/profile")]
    public class ProfileApiController : ControllerBase
    {
        private readonly AppDbContext db;
        private readonly CurrentUser currentUser;

        public ProfileApiController(AppDbContext db, CurrentUser currentUser)
        {
            this.db = db;
            this.currentUser = currentUser;
        }

        [HttpGet("{id:int}")]
        public IActionResult GetProfile(int id)
        {
            Account? account = db.Accounts.FirstOrDefault(a => a.Id == id && !a.IsLocked);
            if (account == null)
            {
                return ApiResult.NotFound("Không tìm thấy hồ sơ.");
            }

            RatingInfo rating = RatingHelper.GetProfileRating(db, id);
            int postCount = db.Posts.Count(p => p.AccountId == id && p.Status == PostStatus.Public);
            int viewCount = db.Visits.Count(v => v.Kind == VisitKind.Profile && v.ProfileAccountId == id);

            return ApiResult.Success(new
            {
                id = account.Id,
                displayName = account.DisplayName,
                avatar = account.Avatar,
                bio = account.Bio,
                role = account.Role,
                roleName = Roles.GetDisplayName(account.Role),
                joinedAt = account.CreatedAt,
                ratingAverage = rating.Average,
                ratingCount = rating.Count,
                postCount = postCount,
                viewCount = viewCount,
                isMine = currentUser.IsLoggedIn && currentUser.Id == id,
                provider = BuildProviderData(account)
            });
        }

        // Mỗi lần mở trang hồ sơ tính một lượt xem
        [HttpPost("{id:int}/view")]
        public IActionResult AddView(int id)
        {
            if (!db.Accounts.Any(a => a.Id == id && !a.IsLocked))
            {
                return ApiResult.NotFound("Không tìm thấy hồ sơ.");
            }

            Visit visit = new Visit
            {
                AccountId = currentUser.IsLoggedIn ? currentUser.Id : null,
                Kind = VisitKind.Profile,
                ProfileAccountId = id,
                Path = "/Profile/Detail/" + id
            };
            db.Visits.Add(visit);
            db.SaveChanges();
            return ApiResult.Success();
        }

        [HttpPut("me")]
        public IActionResult UpdateMine([FromBody] ProfileSaveRequest? request)
        {
            if (!currentUser.IsLoggedIn)
            {
                return ApiResult.NeedLogin();
            }
            if (request == null)
            {
                return ApiResult.Fail("Dữ liệu gửi lên không hợp lệ.");
            }

            string displayName = (request.DisplayName ?? "").Trim();
            string bio = (request.Bio ?? "").Trim();
            string? avatar = string.IsNullOrWhiteSpace(request.Avatar) ? null : request.Avatar.Trim();

            if (displayName.Length == 0 || displayName.Length > 100)
            {
                return ApiResult.Fail("Tên hiển thị không được để trống và tối đa 100 ký tự.");
            }
            if (bio.Length > 1000)
            {
                return ApiResult.Fail("Phần giới thiệu tối đa 1000 ký tự.");
            }
            if (avatar != null && !ImageUploader.IsUploadedImage(avatar))
            {
                return ApiResult.Fail("Ảnh đại diện không hợp lệ.");
            }

            // currentUser.Account đã được theo dõi bởi database nên sửa trực tiếp rồi lưu
            Account account = currentUser.Account!;
            account.DisplayName = displayName;
            account.Bio = bio.Length == 0 ? null : bio;
            account.Avatar = avatar;
            account.UpdatedAt = DateTime.Now;
            db.SaveChanges();

            return ApiResult.Success(new { id = account.Id }, "Đã lưu hồ sơ.");
        }

        // Hồ sơ doanh nghiệp công khai của tài khoản nhà cung cấp (null nếu không có)
        private object? BuildProviderData(Account account)
        {
            if (account.Role != Roles.Provider)
            {
                return null;
            }

            ProviderProfile? profile = db.ProviderProfiles.FirstOrDefault(p => p.AccountId == account.Id && p.IsPublic);
            if (profile == null)
            {
                return null;
            }

            return new
            {
                accountId = profile.AccountId,
                businessName = profile.BusinessName,
                description = profile.Description,
                logo = profile.Logo,
                address = profile.Address,
                phone = profile.Phone,
                contactEmail = profile.ContactEmail,
                website = profile.Website,
                socialLink = profile.SocialLink
            };
        }
    }
}
