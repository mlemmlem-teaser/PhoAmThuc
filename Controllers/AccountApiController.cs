using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using PhoAmThuc.Helpers;
using PhoAmThuc.Models;

namespace PhoAmThuc.Controllers
{
    // API tài khoản: trả về JSON cho JavaScript gọi bằng fetch
    //   POST api/account/register  - đăng ký
    //   POST api/account/login     - đăng nhập
    //   POST api/account/logout    - đăng xuất
    //   GET  api/account/me        - thông tin tài khoản đang đăng nhập (cho menu)
    [Route("api/account")]
    public class AccountApiController : ControllerBase
    {
        private readonly AppDbContext db;
        private readonly CurrentUser currentUser;
        private readonly FeatureCheck featureCheck;

        public AccountApiController(AppDbContext db, CurrentUser currentUser, FeatureCheck featureCheck)
        {
            this.db = db;
            this.currentUser = currentUser;
            this.featureCheck = featureCheck;
        }

        [HttpPost("register")]
        public IActionResult Register([FromBody] RegisterRequest? request)
        {
            if (request == null)
            {
                return ApiResult.Fail("Dữ liệu gửi lên không hợp lệ.");
            }
            if (!featureCheck.IsOn(Features.Register))
            {
                return ApiResult.Fail("Chức năng đăng ký đang tạm đóng.");
            }

            string username = CleanText(request.Username);
            string email = CleanText(request.Email);
            string password = request.Password ?? "";
            string displayName = CleanText(request.DisplayName);

            string? error = CheckRegisterInput(username, email, password, displayName);
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
                Role = Roles.User
            };
            db.Accounts.Add(account);
            db.SaveChanges();

            currentUser.Login(account);
            return ApiResult.Success(null, "Đăng ký thành công.");
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginRequest? request)
        {
            if (request == null)
            {
                return ApiResult.Fail("Dữ liệu gửi lên không hợp lệ.");
            }

            string loginName = CleanText(request.Username);
            string password = request.Password ?? "";

            Account? account = db.Accounts.FirstOrDefault(a => a.Username == loginName || a.Email == loginName);
            if (account == null || account.Password != password)
            {
                return ApiResult.Fail("Sai tên đăng nhập hoặc mật khẩu.");
            }
            if (account.IsLocked)
            {
                return ApiResult.Fail("Tài khoản này đã bị khóa.");
            }

            currentUser.Login(account);
            return ApiResult.Success(null, "Đăng nhập thành công.");
        }

        [HttpPost("logout")]
        public IActionResult Logout()
        {
            currentUser.Logout();
            return ApiResult.Success();
        }

        [HttpGet("me")]
        public IActionResult Me()
        {
            if (!currentUser.IsLoggedIn)
            {
                return ApiResult.NeedLogin();
            }

            int accountId = currentUser.Id;
            RatingInfo rating = RatingHelper.GetProfileRating(db, accountId);
            int postCount = db.Posts.Count(p => p.AccountId == accountId && p.Status != PostStatus.Deleted);

            return ApiResult.Success(new
            {
                id = accountId,
                displayName = currentUser.Account!.DisplayName,
                role = currentUser.Role,
                roleName = Roles.GetDisplayName(currentUser.Role),
                ratingAverage = rating.Average,
                ratingCount = rating.Count,
                postCount = postCount
            });
        }

        // Bỏ khoảng trắng thừa ở hai đầu, chuỗi null thành chuỗi rỗng
        private string CleanText(string? text)
        {
            return (text ?? "").Trim();
        }

        // Trả về câu báo lỗi nếu dữ liệu sai, trả về null nếu hợp lệ
        private string? CheckRegisterInput(string username, string email, string password, string displayName)
        {
            if (username.Length < 3 || username.Length > 50)
            {
                return "Tên đăng nhập phải từ 3 đến 50 ký tự.";
            }
            if (!Regex.IsMatch(username, "^[A-Za-z0-9_.]+$"))
            {
                return "Tên đăng nhập chỉ gồm chữ không dấu, số, dấu chấm và dấu gạch dưới.";
            }
            if (email.Length > 254 || !email.Contains('@') || !email.Contains('.'))
            {
                return "Email không hợp lệ.";
            }
            if (password.Length < 4 || password.Length > 100)
            {
                return "Mật khẩu phải từ 4 đến 100 ký tự.";
            }
            if (displayName.Length > 100)
            {
                return "Tên hiển thị tối đa 100 ký tự.";
            }
            return null;
        }
    }
}
