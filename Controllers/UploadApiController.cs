using Microsoft.AspNetCore.Mvc;
using PhoAmThuc.Helpers;

namespace PhoAmThuc.Controllers
{
    // API tải ảnh lên: POST api/upload/image (gửi file qua form, tên trường là "file")
    [Route("api/upload")]
    public class UploadApiController : ControllerBase
    {
        private readonly CurrentUser currentUser;
        private readonly ImageUploader imageUploader;

        public UploadApiController(CurrentUser currentUser, ImageUploader imageUploader)
        {
            this.currentUser = currentUser;
            this.imageUploader = imageUploader;
        }

        [HttpPost("image")]
        public IActionResult UploadImage([FromForm] IFormFile? file)
        {
            if (!currentUser.IsLoggedIn)
            {
                return ApiResult.NeedLogin();
            }
            if (file == null)
            {
                return ApiResult.Fail("Bạn chưa chọn ảnh.");
            }

            UploadResult result = imageUploader.SaveImage(file);
            if (result.Error != null)
            {
                return ApiResult.Fail(result.Error);
            }
            return ApiResult.Success(new { url = result.Url });
        }
    }
}
