using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhoAmThuc.Helpers;
using PhoAmThuc.Models;

namespace PhoAmThuc.Controllers
{
    // API bài đăng (trả JSON):
    //   GET    api/post/labels        danh sách nhãn cho trình soạn thảo
    //   GET    api/post/list          danh sách bài công khai (page, kind)
    //   GET    api/post/mine          bài đăng của tôi
    //   GET    api/post/{id}          chi tiết một bài
    //   POST   api/post/{id}/view     ghi một lượt xem
    //   POST   api/post               tạo bài
    //   PUT    api/post/{id}          sửa bài
    //   DELETE api/post/{id}          xóa bài (xóa mềm)
    //   POST   api/post/{id}/status   đổi trạng thái nhanh
    [Route("api/post")]
    public class PostApiController : ControllerBase
    {
        private const int pageSize = 10;

        private readonly AppDbContext db;
        private readonly CurrentUser currentUser;
        private readonly FeatureCheck featureCheck;
        private readonly HtmlCleaner htmlCleaner;
        private readonly PostCards postCards;

        public PostApiController(AppDbContext db, CurrentUser currentUser, FeatureCheck featureCheck, HtmlCleaner htmlCleaner, PostCards postCards)
        {
            this.db = db;
            this.currentUser = currentUser;
            this.featureCheck = featureCheck;
            this.htmlCleaner = htmlCleaner;
            this.postCards = postCards;
        }

        // ===================== ĐỌC DỮ LIỆU =====================

        [HttpGet("labels")]
        public IActionResult GetLabels()
        {
            var labels = db.Labels
                .Where(l => l.IsActive)
                .OrderBy(l => l.SystemKey == null ? 1 : 0)
                .ThenBy(l => l.Name)
                .Select(l => new { id = l.Id, name = l.Name, description = l.Description, systemKey = l.SystemKey })
                .ToList();

            return ApiResult.Success(new
            {
                labels = labels,
                canRecipe = currentUser.IsChef || currentUser.IsAdmin,
                canPromo = currentUser.IsProvider || currentUser.IsAdmin,
                postFeatureOn = featureCheck.IsOn(Features.Post),
                promoFeatureOn = featureCheck.IsOn(Features.Promo)
            });
        }

        // Danh sách bài công khai, có tìm kiếm, lọc, sắp xếp và phân trang:
        //   q: từ khóa trong tiêu đề hoặc mô tả   kind: recipe, tip, experience, review, promo
        //   labelId: một nhãn bất kỳ              author: một phần tên người đăng      authorId: mã người đăng
        //   minRating: điểm trung bình tối thiểu (1 đến 10)
        //   sort: newest (mặc định), updated, rating, ratingcount
        [HttpGet("list")]
        public IActionResult GetList(int page = 1, string? kind = null, string? q = null, int? labelId = null,
            string? author = null, int? authorId = null, int? minRating = null, string? sort = null)
        {
            IQueryable<Post> query = db.Posts.Where(p => p.Status == PostStatus.Public);
            query = FilterPosts(query, kind, q, labelId, author, authorId);
            return ApiResult.Success(BuildFilteredPage(query, page, minRating, sort));
        }

        // Bài của tôi (mọi trạng thái trừ đã xóa). kind=promo thì chỉ hiện bài quảng bá...
        [HttpGet("mine")]
        public IActionResult GetMine(int page = 1, string? kind = null)
        {
            if (!currentUser.IsLoggedIn)
            {
                return ApiResult.NeedLogin();
            }

            int myId = currentUser.Id;
            IQueryable<Post> query = db.Posts.Where(p => p.AccountId == myId && p.Status != PostStatus.Deleted);

            string? systemKey = ConvertKindToLabelKey(kind);
            if (systemKey != null)
            {
                query = query.Where(p => p.PostLabels.Any(pl => pl.Label.SystemKey == systemKey));
            }

            query = query.OrderByDescending(p => p.UpdatedAt ?? p.CreatedAt).ThenByDescending(p => p.Id);
            return ApiResult.Success(BuildPostPage(query, page));
        }

        [HttpGet("{id:int}")]
        public IActionResult GetPost(int id)
        {
            Post? post = db.Posts
                .Include(p => p.Author)
                .Include(p => p.PostLabels).ThenInclude(pl => pl.Label)
                .FirstOrDefault(p => p.Id == id);
            if (post == null || !PostAccess.CanView(post, currentUser))
            {
                return ApiResult.NotFound("Không tìm thấy bài đăng.");
            }

            List<int> ids = new List<int> { post.Id };
            RatingInfo rating = RatingHelper.GetPostRating(db, post.Id);
            int viewCount = FindViewCount(VisitHelper.GetPostViewCounts(db, ids), post.Id);

            List<object> labels = post.PostLabels
                .Select(pl => (object)new { id = pl.Label.Id, name = pl.Label.Name, systemKey = pl.Label.SystemKey })
                .ToList();
            List<int> labelIds = post.PostLabels.Select(pl => pl.LabelId).ToList();
            bool isPromo = post.PostLabels.Any(pl => pl.Label.SystemKey == LabelKey.Promo);

            return ApiResult.Success(new
            {
                id = post.Id,
                title = post.Title,
                summary = post.Summary,
                content = post.Content,
                contentHtml = htmlCleaner.ShowVideos(post.Content),
                cover = post.Cover,
                status = post.Status,
                createdAt = post.CreatedAt,
                updatedAt = post.UpdatedAt,
                publishedAt = post.PublishedAt,
                viewCount = viewCount,
                ratingAverage = rating.Average,
                ratingCount = rating.Count,
                labels = labels,
                labelIds = labelIds,
                author = new
                {
                    id = post.Author.Id,
                    displayName = post.Author.DisplayName,
                    roleName = Roles.GetDisplayName(post.Author.Role),
                    avatar = post.Author.Avatar
                },
                recipe = BuildRecipeData(post.Id),
                provider = isPromo ? BuildProviderData(post.AccountId) : null,
                canEdit = PostAccess.CanEdit(post, currentUser)
            });
        }

        [HttpPost("{id:int}/view")]
        public IActionResult AddView(int id)
        {
            Post? post = db.Posts.FirstOrDefault(p => p.Id == id && p.Status == PostStatus.Public);
            if (post == null)
            {
                return ApiResult.NotFound("Không tìm thấy bài đăng.");
            }

            Visit visit = new Visit
            {
                AccountId = currentUser.IsLoggedIn ? currentUser.Id : null,
                Kind = VisitKind.Post,
                PostId = id,
                Path = "/Post/Detail/" + id
            };
            db.Visits.Add(visit);
            db.SaveChanges();
            return ApiResult.Success();
        }

        // ===================== GHI DỮ LIỆU =====================

        [HttpPost("")]
        public IActionResult Create([FromBody] PostSaveRequest? request)
        {
            if (!currentUser.IsLoggedIn)
            {
                return ApiResult.NeedLogin();
            }
            if (request == null)
            {
                return ApiResult.Fail("Dữ liệu gửi lên không hợp lệ.");
            }
            if (!featureCheck.IsOn(Features.Post))
            {
                return ApiResult.Fail("Chức năng đăng bài đang tạm đóng.");
            }

            Post post = new Post { AccountId = currentUser.Id };
            return SavePost(post, request, true);
        }

        [HttpPut("{id:int}")]
        public IActionResult Update(int id, [FromBody] PostSaveRequest? request)
        {
            if (!currentUser.IsLoggedIn)
            {
                return ApiResult.NeedLogin();
            }
            if (request == null)
            {
                return ApiResult.Fail("Dữ liệu gửi lên không hợp lệ.");
            }

            Post? post = db.Posts.FirstOrDefault(p => p.Id == id && p.Status != PostStatus.Deleted);
            if (post == null)
            {
                return ApiResult.NotFound("Không tìm thấy bài đăng.");
            }
            if (!PostAccess.CanEdit(post, currentUser))
            {
                return ApiResult.Forbidden("Bạn chỉ được sửa bài đăng của mình.");
            }
            return SavePost(post, request, false);
        }

        [HttpDelete("{id:int}")]
        public IActionResult Delete(int id)
        {
            if (!currentUser.IsLoggedIn)
            {
                return ApiResult.NeedLogin();
            }

            Post? post = db.Posts.FirstOrDefault(p => p.Id == id && p.Status != PostStatus.Deleted);
            if (post == null)
            {
                return ApiResult.NotFound("Không tìm thấy bài đăng.");
            }
            if (!PostAccess.CanEdit(post, currentUser))
            {
                return ApiResult.Forbidden("Bạn chỉ được xóa bài đăng của mình.");
            }

            post.Status = PostStatus.Deleted;
            post.UpdatedAt = DateTime.Now;
            db.SaveChanges();
            return ApiResult.Success(null, "Đã xóa bài đăng.");
        }

        [HttpPost("{id:int}/status")]
        public IActionResult ChangeStatus(int id, [FromBody] StatusRequest? request)
        {
            if (!currentUser.IsLoggedIn)
            {
                return ApiResult.NeedLogin();
            }

            Post? post = db.Posts.FirstOrDefault(p => p.Id == id && p.Status != PostStatus.Deleted);
            if (post == null)
            {
                return ApiResult.NotFound("Không tìm thấy bài đăng.");
            }
            if (!PostAccess.CanEdit(post, currentUser))
            {
                return ApiResult.Forbidden("Bạn chỉ được đổi trạng thái bài đăng của mình.");
            }

            string wanted = request?.Status ?? "";
            bool allowed = wanted == PostStatus.Draft
                || wanted == PostStatus.Public
                || (wanted == PostStatus.Hidden && currentUser.IsAdmin);
            if (!allowed)
            {
                return ApiResult.Fail("Trạng thái không hợp lệ.");
            }
            if (post.Status == PostStatus.Hidden && !currentUser.IsAdmin)
            {
                return ApiResult.Forbidden("Bài đăng đang bị quản trị viên ẩn.");
            }

            post.Status = wanted;
            post.UpdatedAt = DateTime.Now;
            if (wanted == PostStatus.Public && post.PublishedAt == null)
            {
                post.PublishedAt = DateTime.Now;
            }
            db.SaveChanges();
            return ApiResult.Success(new { status = post.Status }, "Đã đổi trạng thái bài đăng.");
        }

        // ===================== LƯU BÀI ĐĂNG =====================

        // Kiểm tra dữ liệu, rồi lưu bài + nhãn + công thức trong một transaction (lỗi giữa chừng thì hủy hết)
        private IActionResult SavePost(Post post, PostSaveRequest request, bool isNew)
        {
            string title = (request.Title ?? "").Trim();
            string summary = (request.Summary ?? "").Trim();
            string content = htmlCleaner.Clean(request.Content);
            string? cover = string.IsNullOrWhiteSpace(request.Cover) ? null : request.Cover.Trim();
            RecipeRequest? recipe = request.Recipe;
            bool isRecipe = recipe != null;

            string? error = CheckPostFields(title, summary, content, cover, post.Cover, isRecipe);
            if (error != null)
            {
                return ApiResult.Fail(error);
            }

            if (recipe != null)
            {
                if (!currentUser.IsChef && !currentUser.IsAdmin)
                {
                    return ApiResult.Forbidden("Chỉ đầu bếp mới được đăng bài công thức nấu ăn.");
                }
                error = CheckRecipe(recipe);
                if (error != null)
                {
                    return ApiResult.Fail(error);
                }
            }

            List<int>? labelIds = ChooseLabelIds(request.LabelIds, isRecipe, post, isNew, out error);
            if (labelIds == null)
            {
                return ApiResult.Fail(error ?? "Nhãn không hợp lệ.");
            }

            using (var transaction = db.Database.BeginTransaction())
            {
                ApplyPostFields(post, request, title, summary, content, cover, isNew);
                if (isNew)
                {
                    db.Posts.Add(post);
                }
                db.SaveChanges();

                SavePostLabels(post.Id, labelIds);
                SaveRecipe(post.Id, recipe);
                transaction.Commit();
            }

            return ApiResult.Success(new { id = post.Id, status = post.Status }, "Đã lưu bài đăng.");
        }

        // Trả về câu báo lỗi nếu sai, null nếu hợp lệ
        // currentCover là ảnh bìa đang có của bài (null nếu bài mới): được giữ nguyên dù không phải ảnh tải lên,
        // ví dụ bài mẫu dùng ảnh từ Internet. Ảnh bìa mới đưa vào thì bắt buộc phải là ảnh đã tải lên website
        private string? CheckPostFields(string title, string summary, string content, string? cover, string? currentCover, bool isRecipe)
        {
            if (title.Length == 0)
            {
                return "Tiêu đề không được để trống.";
            }
            if (title.Length > 200)
            {
                return "Tiêu đề tối đa 200 ký tự.";
            }
            if (summary.Length > 500)
            {
                return "Mô tả ngắn tối đa 500 ký tự.";
            }
            if (content.Length > 200000)
            {
                return "Nội dung bài đăng quá dài.";
            }
            // Bài công thức được phép không có phần nội dung chính (đã có nguyên liệu và các bước)
            if (!isRecipe && !htmlCleaner.HasContent(content))
            {
                return "Nội dung bài đăng không được để trống.";
            }
            if (cover != null && cover != currentCover && !ImageUploader.IsUploadedImage(cover))
            {
                return "Ảnh đại diện không hợp lệ.";
            }
            return null;
        }

        private string? CheckRecipe(RecipeRequest recipe)
        {
            List<IngredientRequest> ingredients = recipe.Ingredients ?? new List<IngredientRequest>();
            List<StepRequest> steps = recipe.Steps ?? new List<StepRequest>();

            if (ingredients.Count == 0)
            {
                return "Công thức cần ít nhất một nguyên liệu.";
            }
            if (steps.Count == 0)
            {
                return "Công thức cần ít nhất một bước thực hiện.";
            }
            if (ingredients.Count > 100 || steps.Count > 100)
            {
                return "Công thức có quá nhiều nguyên liệu hoặc quá nhiều bước.";
            }

            foreach (IngredientRequest ingredient in ingredients)
            {
                string name = (ingredient.Name ?? "").Trim();
                if (name.Length == 0 || name.Length > 200)
                {
                    return "Tên nguyên liệu không được để trống và tối đa 200 ký tự.";
                }
                if (ingredient.Amount != null && (ingredient.Amount <= 0 || ingredient.Amount > 1000000))
                {
                    return "Định lượng nguyên liệu phải lớn hơn 0.";
                }
                if ((ingredient.Unit ?? "").Trim().Length > 50)
                {
                    return "Đơn vị đo tối đa 50 ký tự.";
                }
                if ((ingredient.Note ?? "").Trim().Length > 250)
                {
                    return "Ghi chú nguyên liệu tối đa 250 ký tự.";
                }
            }

            foreach (StepRequest step in steps)
            {
                string content = (step.Content ?? "").Trim();
                if (content.Length == 0 || content.Length > 5000)
                {
                    return "Nội dung mỗi bước không được để trống và tối đa 5000 ký tự.";
                }
                if (!string.IsNullOrEmpty(step.Image) && !ImageUploader.IsUploadedImage(step.Image))
                {
                    return "Ảnh của một bước thực hiện không hợp lệ.";
                }
            }

            if (recipe.PrepMinutes != null && (recipe.PrepMinutes < 0 || recipe.PrepMinutes > 10000))
            {
                return "Thời gian chuẩn bị không hợp lệ.";
            }
            if (recipe.CookMinutes != null && (recipe.CookMinutes < 0 || recipe.CookMinutes > 10000))
            {
                return "Thời gian nấu không hợp lệ.";
            }
            if (recipe.Servings != null && (recipe.Servings < 1 || recipe.Servings > 1000))
            {
                return "Số khẩu phần phải từ 1 đến 1000.";
            }
            string difficulty = recipe.Difficulty ?? "";
            if (difficulty != "" && difficulty != "Easy" && difficulty != "Medium" && difficulty != "Hard")
            {
                return "Độ khó không hợp lệ.";
            }
            if ((recipe.Note ?? "").Trim().Length > 1000)
            {
                return "Ghi chú công thức tối đa 1000 ký tự.";
            }
            return null;
        }

        // Quyết định danh sách nhãn cuối cùng của bài. Trả về null (kèm câu báo lỗi) nếu không hợp lệ
        private List<int>? ChooseLabelIds(List<int>? requested, bool isRecipe, Post post, bool isNew, out string? error)
        {
            error = null;
            List<int> ids = (requested ?? new List<int>()).Distinct().ToList();
            List<Label> labels = db.Labels.Where(l => ids.Contains(l.Id) && l.IsActive).ToList();
            if (labels.Count != ids.Count)
            {
                error = "Có nhãn không hợp lệ hoặc đã bị tắt.";
                return null;
            }

            // Nhãn Công thức nấu ăn do hệ thống tự gắn khi bài có công thức, không cho chọn tay
            labels = labels.Where(l => l.SystemKey != LabelKey.Recipe).ToList();

            Label? promo = labels.FirstOrDefault(l => l.SystemKey == LabelKey.Promo);
            if (promo != null)
            {
                if (!currentUser.IsProvider && !currentUser.IsAdmin)
                {
                    error = "Chỉ nhà cung cấp mới được đăng bài quảng bá.";
                    return null;
                }
                // Bài đã là bài quảng bá từ trước thì được sửa kể cả khi chức năng đang tắt
                bool alreadyPromo = !isNew && db.PostLabels.Any(pl => pl.PostId == post.Id && pl.LabelId == promo.Id);
                if (!alreadyPromo && !featureCheck.IsOn(Features.Promo))
                {
                    error = "Chức năng đăng bài quảng bá đang tạm đóng.";
                    return null;
                }
            }

            List<int> result = labels.Select(l => l.Id).ToList();
            if (isRecipe)
            {
                Label? recipeLabel = db.Labels.FirstOrDefault(l => l.SystemKey == LabelKey.Recipe);
                if (recipeLabel == null)
                {
                    error = "Thiếu nhãn Công thức nấu ăn trong cơ sở dữ liệu.";
                    return null;
                }
                result.Add(recipeLabel.Id);
            }

            if (result.Count == 0)
            {
                error = "Bài đăng cần ít nhất một nhãn.";
                return null;
            }
            return result;
        }

        // Ghi các trường cơ bản vào đối tượng Post (chưa lưu xuống database)
        private void ApplyPostFields(Post post, PostSaveRequest request, string title, string summary, string content, string? cover, bool isNew)
        {
            post.Title = title;
            post.Content = content;
            post.Cover = cover;

            if (summary.Length > 0)
            {
                post.Summary = summary;
            }
            else
            {
                string autoSummary = htmlCleaner.MakeSummary(content, 160);
                post.Summary = autoSummary.Length > 0 ? autoSummary : null;
            }

            // Tác giả chỉ chọn được Draft hoặc Public. Bài bị quản trị viên ẩn thì tác giả không tự bỏ ẩn được
            string status = request.Status == PostStatus.Public ? PostStatus.Public : PostStatus.Draft;
            if (!isNew && post.Status == PostStatus.Hidden && !currentUser.IsAdmin)
            {
                status = PostStatus.Hidden;
            }
            post.Status = status;

            if (status == PostStatus.Public && post.PublishedAt == null)
            {
                post.PublishedAt = DateTime.Now;
            }
            if (!isNew)
            {
                post.UpdatedAt = DateTime.Now;
            }
        }

        // Chỉ thêm nhãn mới và bỏ nhãn không còn chọn (không xóa rồi thêm lại để tránh trùng khóa)
        private void SavePostLabels(int postId, List<int> labelIds)
        {
            List<PostLabel> existing = db.PostLabels.Where(pl => pl.PostId == postId).ToList();

            List<PostLabel> toRemove = existing.Where(pl => !labelIds.Contains(pl.LabelId)).ToList();
            db.PostLabels.RemoveRange(toRemove);

            foreach (int labelId in labelIds)
            {
                bool hasLabel = existing.Any(pl => pl.LabelId == labelId);
                if (!hasLabel)
                {
                    db.PostLabels.Add(new PostLabel { PostId = postId, LabelId = labelId });
                }
            }
            db.SaveChanges();
        }

        // Lưu công thức: xóa nguyên liệu và bước cũ rồi thêm lại (SaveChanges hai lần để xóa xong mới thêm)
        private void SaveRecipe(int postId, RecipeRequest? request)
        {
            Recipe? recipe = db.Recipes
                .Include(r => r.Ingredients)
                .Include(r => r.Steps)
                .FirstOrDefault(r => r.PostId == postId);

            if (request == null)
            {
                // Bài không còn là công thức thì xóa dữ liệu công thức
                if (recipe != null)
                {
                    db.Ingredients.RemoveRange(recipe.Ingredients);
                    db.RecipeSteps.RemoveRange(recipe.Steps);
                    db.SaveChanges();
                    db.Recipes.Remove(recipe);
                    db.SaveChanges();
                }
                return;
            }

            if (recipe == null)
            {
                recipe = new Recipe { PostId = postId };
                db.Recipes.Add(recipe);
            }
            else
            {
                db.Ingredients.RemoveRange(recipe.Ingredients);
                db.RecipeSteps.RemoveRange(recipe.Steps);
                db.SaveChanges();
            }

            recipe.PrepMinutes = request.PrepMinutes;
            recipe.CookMinutes = request.CookMinutes;
            recipe.Servings = request.Servings;
            recipe.Difficulty = string.IsNullOrEmpty(request.Difficulty) ? null : request.Difficulty;
            recipe.Note = CleanOptionalText(request.Note);
            db.SaveChanges();

            List<IngredientRequest> ingredients = request.Ingredients ?? new List<IngredientRequest>();
            for (int i = 0; i < ingredients.Count; i++)
            {
                db.Ingredients.Add(new Ingredient
                {
                    RecipeId = recipe.Id,
                    Position = i + 1,
                    Name = (ingredients[i].Name ?? "").Trim(),
                    Amount = ingredients[i].Amount,
                    Unit = CleanOptionalText(ingredients[i].Unit),
                    Note = CleanOptionalText(ingredients[i].Note)
                });
            }

            List<StepRequest> steps = request.Steps ?? new List<StepRequest>();
            for (int i = 0; i < steps.Count; i++)
            {
                db.RecipeSteps.Add(new RecipeStep
                {
                    RecipeId = recipe.Id,
                    Position = i + 1,
                    Content = (steps[i].Content ?? "").Trim(),
                    Image = string.IsNullOrEmpty(steps[i].Image) ? null : steps[i].Image
                });
            }
            db.SaveChanges();
        }

        // ===================== HÀM PHỤ =====================

        // Áp các bộ lọc không liên quan đến điểm đánh giá
        private IQueryable<Post> FilterPosts(IQueryable<Post> query, string? kind, string? q, int? labelId, string? author, int? authorId)
        {
            string? systemKey = ConvertKindToLabelKey(kind);
            if (systemKey != null)
            {
                query = query.Where(p => p.PostLabels.Any(pl => pl.Label.SystemKey == systemKey));
            }

            if (!string.IsNullOrWhiteSpace(q))
            {
                string keyword = q.Trim();
                query = query.Where(p => p.Title.Contains(keyword) || (p.Summary != null && p.Summary.Contains(keyword)));
            }

            if (labelId != null)
            {
                int labelKey = labelId.Value;
                query = query.Where(p => p.PostLabels.Any(pl => pl.LabelId == labelKey));
            }

            if (!string.IsNullOrWhiteSpace(author))
            {
                string authorName = author.Trim();
                query = query.Where(p => p.Author.DisplayName.Contains(authorName));
            }

            if (authorId != null)
            {
                int authorKey = authorId.Value;
                query = query.Where(p => p.AccountId == authorKey);
            }
            return query;
        }

        // Đổi tên loại ở đường dẫn (recipe...) thành mã nhãn hệ thống trong database
        private string? ConvertKindToLabelKey(string? kind)
        {
            switch (kind)
            {
                case "recipe": return LabelKey.Recipe;
                case "tip": return LabelKey.Tip;
                case "experience": return LabelKey.Experience;
                case "review": return LabelKey.Review;
                case "promo": return LabelKey.Promo;
                default: return null;
            }
        }

        // Lọc theo điểm, sắp xếp và cắt trang. Sắp xếp theo điểm làm trong bộ nhớ (chỉ với danh sách mã bài, rất nhẹ)
        private object BuildFilteredPage(IQueryable<Post> query, int page, int? minRating, string? sort)
        {
            bool sortByRating = sort == "rating" || sort == "ratingcount";
            Dictionary<int, RatingInfo> allRatings = new Dictionary<int, RatingInfo>();
            if (minRating != null || sortByRating)
            {
                allRatings = RatingHelper.GetAllPostRatings(db);
            }

            if (minRating != null)
            {
                double minimum = minRating.Value;
                List<int> passedIds = allRatings
                    .Where(x => x.Value.Average != null && x.Value.Average >= minimum)
                    .Select(x => x.Key)
                    .ToList();
                query = query.Where(p => passedIds.Contains(p.Id));
            }

            int totalCount = query.Count();
            int totalPages = CountPages(totalCount);
            page = ClampPage(page, totalPages);

            List<int> pageIds;
            if (sortByRating)
            {
                pageIds = SortIdsByRating(query, allRatings, sort == "ratingcount", page);
            }
            else
            {
                query = sort == "updated"
                    ? query.OrderByDescending(p => p.UpdatedAt ?? p.PublishedAt).ThenByDescending(p => p.Id)
                    : query.OrderByDescending(p => p.PublishedAt).ThenByDescending(p => p.Id);
                pageIds = query.Skip((page - 1) * pageSize).Take(pageSize).Select(p => p.Id).ToList();
            }
            return MakePageResult(pageIds, page, totalPages, totalCount);
        }

        // Bài chưa có đánh giá luôn xếp sau các bài đã có đánh giá
        private List<int> SortIdsByRating(IQueryable<Post> query, Dictionary<int, RatingInfo> ratings, bool byCount, int page)
        {
            List<int> ids = query.Select(p => p.Id).ToList();

            IOrderedEnumerable<int> ordered;
            if (byCount)
            {
                ordered = ids.OrderByDescending(id => FindRatingCount(ratings, id)).ThenByDescending(id => FindRatingAverage(ratings, id));
            }
            else
            {
                ordered = ids.OrderByDescending(id => FindRatingAverage(ratings, id)).ThenByDescending(id => FindRatingCount(ratings, id));
            }
            return ordered.ThenByDescending(id => id).Skip((page - 1) * pageSize).Take(pageSize).ToList();
        }

        // Chưa có đánh giá thì trả về -1 để xếp sau cùng
        private double FindRatingAverage(Dictionary<int, RatingInfo> ratings, int postId)
        {
            ratings.TryGetValue(postId, out RatingInfo? found);
            return found?.Average ?? -1;
        }

        private int FindRatingCount(Dictionary<int, RatingInfo> ratings, int postId)
        {
            ratings.TryGetValue(postId, out RatingInfo? found);
            return found?.Count ?? 0;
        }

        // Cắt trang cho một truy vấn đã có thứ tự sắp xếp sẵn
        private object BuildPostPage(IQueryable<Post> query, int page)
        {
            int totalCount = query.Count();
            int totalPages = CountPages(totalCount);
            page = ClampPage(page, totalPages);

            List<int> ids = query.Skip((page - 1) * pageSize).Take(pageSize).Select(p => p.Id).ToList();
            return MakePageResult(ids, page, totalPages, totalCount);
        }

        private object MakePageResult(List<int> pageIds, int page, int totalPages, int totalCount)
        {
            List<Post> posts = postCards.LoadPosts(pageIds);
            List<object> items = postCards.BuildCards(posts);
            return new { items = items, page = page, totalPages = totalPages, totalCount = totalCount };
        }

        private int CountPages(int totalCount)
        {
            return Math.Max(1, (int)Math.Ceiling(totalCount / (double)pageSize));
        }

        private int ClampPage(int page, int totalPages)
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

        private int FindViewCount(Dictionary<int, int> views, int postId)
        {
            views.TryGetValue(postId, out int count);
            return count;
        }

        // Dữ liệu công thức của một bài (null nếu bài không phải công thức)
        private object? BuildRecipeData(int postId)
        {
            Recipe? recipe = db.Recipes
                .Include(r => r.Ingredients)
                .Include(r => r.Steps)
                .FirstOrDefault(r => r.PostId == postId);
            if (recipe == null)
            {
                return null;
            }

            return new
            {
                prepMinutes = recipe.PrepMinutes,
                cookMinutes = recipe.CookMinutes,
                servings = recipe.Servings,
                difficulty = recipe.Difficulty,
                note = recipe.Note,
                ingredients = recipe.Ingredients
                    .OrderBy(i => i.Position)
                    .Select(i => new { name = i.Name, amount = i.Amount, unit = i.Unit, note = i.Note })
                    .ToList(),
                steps = recipe.Steps
                    .OrderBy(s => s.Position)
                    .Select(s => new { content = s.Content, image = s.Image })
                    .ToList()
            };
        }

        // Thông tin nhà cung cấp hiển thị kèm bài quảng bá (null nếu chưa có hồ sơ công khai)
        private object? BuildProviderData(int accountId)
        {
            ProviderProfile? profile = db.ProviderProfiles.FirstOrDefault(p => p.AccountId == accountId && p.IsPublic);
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
                website = profile.Website
            };
        }

        private string? CleanOptionalText(string? text)
        {
            string cleaned = (text ?? "").Trim();
            return cleaned.Length == 0 ? null : cleaned;
        }
    }
}
