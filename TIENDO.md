# TIENDO.md - Sổ tay dự án "Phố Ẩm Thực"

> File này do Claude ghi **cho chính Claude đọc** ở các session sau (Claude không nhớ gì giữa các session).
> Đầu mỗi session: đọc file này trước, rồi đọc `Website ẩm thực.docx` (đặc tả) phần liên quan đến giai đoạn đang làm.
> Cuối mỗi task: cập nhật mục "Nhật ký" và bảng tiến độ bên dưới.

## 1. Bối cảnh

- Người dùng là lập trình viên mới học web (khoảng 1 tháng). Code phải đơn giản, dễ đọc, dễ sửa. Không viết vội để tiết kiệm token, làm cẩn thận từng giai đoạn.
- Project: ASP.NET Core MVC (.NET 10), tên project `PhoAmThuc`, dùng Visual Studio, SQL Server Express (`.\SQLEXPRESS`) với Windows Authentication.
- Không dùng Node.js. Người dùng tự biên dịch/chạy trong Visual Studio rồi báo lỗi cho Claude.
- Sandbox của Claude: có Node (chỉ để `node --check` kiểm tra cú pháp JS), **không có .NET SDK**, mạng thường bị tắt. Vì vậy C# chưa từng được biên dịch bởi Claude: mọi lỗi biên dịch sẽ do người dùng báo.
- Sự cố đã gặp (GĐ1): chạy báo "layout view '_Layout' could not be located" dù file có đủ; người dùng tạo project mới rồi copy file vào thì chạy bình thường (nguyên nhân nhiều khả năng là bản build/Hot Reload cũ). Nếu lặp lại: Stop app, xóa `bin` và `obj`, Rebuild Solution, chạy lại (không dùng Hot Reload).
- GĐ1 đã chạy được trang chủ. Đăng nhập/đăng ký và kết nối SQL **chưa được người dùng xác nhận**, hỏi lại nếu có lỗi liên quan DB.
- UI toàn bộ tiếng Việt. Code (tên biến, hàm, class, id, class CSS) tiếng Anh. Comment tiếng Việt.
- Người dùng yêu cầu: **không băm mật khẩu** (lưu văn bản thường, cột `Password`), không cần bảo mật cao, "làm sao tiện là được". Đặc tả gốc có yêu cầu băm nhưng người dùng đã bỏ yêu cầu này.

## 2. Kiến trúc đã chốt (phương án A)

- **C# (controller + Entity Framework Core)** là lớp kiểm soát: đăng nhập, kiểm tra quyền, kiểm tra công tắc chức năng (`FeatureCheck`), kiểm tra dữ liệu đầu vào, truy vấn DB (lọc, sắp xếp, phân trang, tính điểm trung bình), trả JSON. Mọi quy tắc nghiệp vụ nằm ở đây vì đặc tả yêu cầu kiểm tra ở server.
- **JavaScript (chạy ở trình duyệt)** lo hiển thị: gọi `fetch` tới API, dựng item từ JSON, lọc/sắp xếp UI, form, trình soạn thảo, biểu đồ.
- Trang (`Views/*.cshtml`) do `XxxController` trả về; API JSON do `XxxApiController` (route `api/xxx/...`, kế thừa `ControllerBase`, **không** dùng `[ApiController]`).
- HTML cố định (xuất hiện 1 lần) để thẳng trong `.cshtml`. HTML của item lặp lại (thẻ bài đăng, bình luận...) đặt trong thẻ `<template>` trong `.cshtml`, JS nhân bản và điền dữ liệu. Không nối chuỗi HTML trong JS.
- API luôn trả `{ ok: true/false, message: "...", data: ... }` qua `ApiResult` (Helpers). Mã HTTP: 200, 400 (sai dữ liệu), 401 (chưa đăng nhập), 403 (không đủ quyền), 404.
- Đăng nhập bằng Session (không dùng Identity). `CurrentUser` đọc lại tài khoản từ DB mỗi request nên đổi vai trò có hiệu lực ngay. Tài khoản bị khóa coi như khách.
- Thời gian lưu theo giờ máy chủ (`SYSDATETIME()` / `DateTime.Now`), không đổi múi giờ. JS hiển thị bằng `Format.formatDate`.
- Mỗi lần xem tính 1 lượt (người dùng chốt: ai xem cũng tính, kể cả tác giả, không chống trùng).
- Quyền tạo bài (người dùng đã đồng ý): trình tạo công thức (nguyên liệu, các bước) chỉ đầu bếp + admin; bài quảng bá (nhãn Promo) chỉ nhà cung cấp + admin và phải bật công tắc `Promo`; các nhãn còn lại ai đăng nhập cũng gắn được. Mỗi bài cần ít nhất 1 nhãn. Người dùng đã nói không cần cơ chế hạn chế nhãn phức tạp hơn thế.
- Soạn bài (GĐ2): trình soạn thảo tự viết (`richeditor.js`, `contenteditable` + `document.execCommand`), nội dung lưu HTML đã làm sạch bằng NuGet `HtmlSanitizer` (`Helpers/HtmlCleaner.cs`, namespace `Ganss.Xss`). Video YouTube lưu dạng `<div data-youtube="MÃ11KÝTỰ"></div>` (không lưu iframe); khi trả cho trang xem, `HtmlCleaner.ShowVideos` đổi thành iframe `youtube.com/embed/...`. Trường `content` (thô) dùng cho trang sửa, `contentHtml` (đã đổi video) dùng cho trang xem.
- Nhãn "Công thức nấu ăn" (`SystemKey = Recipe`) không cho chọn tay: server tự gắn khi bài có phần công thức (`recipe != null`) và tự bỏ khi không có. Promo chỉ Provider/Admin; công tắc `Promo` chỉ chặn khi *thêm mới* nhãn Promo vào bài (bài đã là quảng bá vẫn sửa được). Công tắc `Post` chỉ chặn *tạo* bài mới, không chặn sửa/xóa.
- Quyền xem bài (`PostAccess.CanView`): `Deleted` không ai xem; `Public` ai cũng xem; `Draft`/`Hidden` chỉ tác giả + admin. Tác giả chỉ chọn được Draft/Public; bài bị admin ẩn (`Hidden`) thì tác giả không tự bỏ ẩn được (admin thì được).
- Lưu bài trong 1 transaction (`db.Database.BeginTransaction`). Nhãn: chỉ thêm/bỏ phần chênh lệch. Công thức: xóa nguyên liệu/bước cũ, `SaveChanges`, rồi thêm lại (tránh vi phạm UNIQUE (RecipeId, Position)).
- Bình luận/đánh giá (GĐ3): `CommentApiController` dùng chung cho bài đăng (`postId`) và hồ sơ (`profileId`/`targetAccountId`). Đánh giá = bình luận có `Rating` 1..10, nhận xét bắt buộc, tối đa 500 ký tự (bình luận thường 2000). Hồ sơ chỉ nhận đánh giá kèm điểm ở cấp gốc (không có bình luận thường trên hồ sơ), nhưng có thể trả lời đánh giá. Trả lời chỉ 1 cấp, không kèm điểm, `PostId/TargetAccountId` copy từ bình luận gốc. Mỗi người 1 đánh giá `Visible` cho mỗi đích (kiểm tra trong code + filtered unique index; xóa đánh giá thì được đánh giá lại). Không được tự đánh giá hồ sơ của mình; **được** đánh giá bài của chính mình (đặc tả chỉ cấm hồ sơ; đổi dễ nếu người dùng muốn cấm, sửa `PreparePostComment`). Chỉ tác giả được sửa; tác giả hoặc admin được xóa (xóa mềm, xóa gốc thì xóa luôn các trả lời). Công tắc `Comment` chặn bình luận thường và trả lời; công tắc `Rating` chặn tạo đánh giá mới; sửa/xóa không bị chặn. Chỉ bài `Public` nhận bình luận.
- `GET api/comment/list` trả `items` (gốc + `replies`), `page/totalPages/totalCount`, `ratingAverage/ratingCount` và `permissions` {isLoggedIn, canComment, canReply, canRate, rateBlockedReason} để JS ẩn/hiện biểu mẫu (server vẫn kiểm tra lại khi ghi).
- Danh sách bài (GĐ4): `GET api/post/list` nhận `page, kind, q, labelId, author, authorId, minRating, sort`. `kind` = recipe/tip/experience/review/promo (ánh xạ sang `Labels.SystemKey`); `q` tìm trong tiêu đề + mô tả (không tìm trong nội dung); `author` là một phần tên hiển thị, `authorId` là mã (dùng cho trang hồ sơ); `minRating` là điểm trung bình >= n; `sort` = newest (mặc định) / updated / rating / ratingcount. Sắp xếp và lọc theo điểm làm trong bộ nhớ trên danh sách mã bài (`RatingHelper.GetAllPostRatings`), bài chưa có đánh giá luôn xếp sau. Trang `Post/Index` là form GET: mọi lựa chọn nằm trên URL, `postlist.js` điền lại form và chuyển nguyên query cho API.
- Dựng thẻ bài đăng dùng chung ở `Helpers/PostCards.cs` (`LoadPosts` theo thứ tự mã + `BuildCards`). Mọi nơi hiển thị thẻ bài (danh sách, trang chủ, hồ sơ, quản lý bài) đều qua đây.
- Trang chủ (`GET api/home/summary`, `home.js`): 4 khu, mỗi khu 6 mục: nổi nhất (nhiều lượt xem nhất mọi thời gian), nổi bật trong tháng (nhiều lượt xem từ ngày 1 tháng hiện tại), hồ sơ nổi bật (điểm giảm dần, bằng điểm thì nhiều lượt đánh giá hơn, chưa có đánh giá xếp sau; loại tài khoản Admin và tài khoản bị khóa), công thức mới nhất. Khu bài nổi nhất/trong tháng thiếu dữ liệu thì **bù bằng bài mới nhất** để trang không trống; khu nào rỗng hoàn toàn thì ẩn. Quyết định này là của Claude, đổi ở `HomeApiController.FindMostViewedPostIds` nếu muốn nghiêm ngặt.
- Hồ sơ: `/Profile/Detail/{id}` (công khai, không lộ email/mật khẩu; hiển thị tên, ảnh, giới thiệu, vai trò, điểm, số bài công khai, lượt xem hồ sơ, hồ sơ doanh nghiệp nếu là Provider có `IsPublic`, danh sách bài, khu đánh giá `CommentSection("profile", ...)`). Mỗi lần mở trang ghi 1 `Visit` Kind=Profile. `/Profile/Edit` cho người dùng sửa tên hiển thị, giới thiệu, ảnh đại diện (chưa có đổi mật khẩu/email, đặc tả không yêu cầu). Tên tác giả ở thẻ bài, trang bài, bình luận đều là link sang hồ sơ.
- Danh sách nhà cung cấp (`/Provider`, `GET api/provider/list`): chỉ tài khoản `Role = Provider`, chưa khóa, có `ProviderProfile.IsPublic`; xếp theo điểm giảm dần; có tìm theo tên doanh nghiệp, 12 mục/trang.
- Khu vực nhà cung cấp (GĐ5): trang `/Provider/Manage` (chỉ Provider; người khác bị chuyển về trang chủ) có 5 thẻ: Hồ sơ doanh nghiệp, Bài quảng bá, Thống kê của tôi, Phản hồi, Thị trường. Tên thẻ nhớ trên URL (`#stats`...). `providermanage.js` điều khiển thẻ; mỗi thẻ là một class có `activate()` chỉ tải dữ liệu lần đầu mở. API trong `ProviderApiController` (`mine` GET/PUT, `stats`, `feedback`, `market`), đều qua `CheckProvider()` (401 nếu chưa đăng nhập, 403 nếu không phải Provider; Admin cũng không dùng được).
- Hồ sơ doanh nghiệp: tạo mới nếu chưa có dòng `ProviderProfiles`, kiểm tra: tên bắt buộc, điện thoại chỉ gồm số và `+ ( ) . -`, email có @, website/liên kết MXH bắt buộc bắt đầu http(s)://, logo phải là ảnh đã tải lên.
- Thống kê: tổng lượt xem hồ sơ, tổng lượt xem các bài của tôi (không tính bài đã xóa), điểm + số lượt đánh giá hồ sơ, bình luận (không điểm) và đánh giá (có điểm) người KHÁC để lại trên bài của tôi. Biểu đồ: lượt xem theo ngày (7/30/90 ngày, ngày không có lượt vẫn là 0), phân bố điểm hồ sơ 1..10, 8 bài nhiều lượt xem nhất.
- Phản hồi: bình luận gốc + đánh giá của người khác trên bài (chưa xóa) của tôi và đánh giá trên hồ sơ của tôi; không gồm câu trả lời; có lọc tất cả/chỉ đánh giá/chỉ bình luận; 10 mục/trang.
- Thị trường: số bài công khai theo 5 loại (nhãn hệ thống) và 10 nhãn thường xuất hiện nhiều nhất. Luôn hiện danh sách chữ, biểu đồ chỉ vẽ khi có Chart.js.
- Chart.js: mã đã viết như thể đã cài. Trang nạp `~/lib/chartjs/chart.umd.js` (đặt file Chart.js bản 4.x UMD vào `wwwroot/lib/chartjs/` với đúng tên này). Thiếu file thì trang vẫn chạy, hiện lời nhắc "Chưa cài thư viện Chart.js" và bỏ qua biểu đồ. `wwwroot/js/chartview.js` (class `ChartView`) gói lệnh vẽ đường/cột, **GĐ6 dashboard quản trị dùng lại**. Đã test `ChartView` bằng Chart giả trong Node. Người dùng đã cài Chart.js và xác nhận biểu đồ chạy ổn.
- Vào `/Post/Create?kind=promo` thì chọn sẵn nhãn Quảng bá; `/Post/Mine?kind=promo` chỉ hiện bài quảng bá của tôi (`api/post/mine` có tham số `kind`).
- Quản trị (GĐ6): trang `/Admin` (chỉ Admin; chưa đăng nhập thì chuyển sang đăng nhập, không phải Admin thì về trang chủ) có 8 thẻ: Tổng quan, Tài khoản, Bài đăng, Bình luận, Nhà cung cấp, Nhãn, Trang thông tin, Cấu hình. `TabControl` (tabcontrol.js) điều khiển thẻ; mỗi thẻ là class `Admin...Tab` với `activate()`. Mọi API quản trị nằm dưới `api/admin/...`, mỗi controller kế thừa `AdminApiBase` và gọi `CheckAdmin()` đầu tiên (401/403). Mẫu giao diện chung: thanh lọc `adminbar`, bảng `datatable` trong `tablebox`, biểu mẫu `formpanel` hiện/ẩn ngay trên bảng (không dùng modal), thông báo qua `Notice.show(element, text, isGood)` (site.js).
- Dashboard: tổng tài khoản, bài công khai, bình luận (không điểm, Visible), đánh giá (có điểm, Visible); lượt xem theo ngày 3 đường (trang/bài/hồ sơ); số bài công khai theo 5 loại; 10 bài công khai nhiều lượt xem nhất. Lượt xem trang (`Visits.Kind = Page`) được ghi bởi thuộc tính `[RecordVisit]` (Helpers/RecordVisitAttribute.cs) gắn lên Home Index/About/Contact/Help, Post Index, Provider Index; lỗi ghi bị nuốt để không làm hỏng trang. Lượt xem = số lần mở trang, không phải số người.
- Tài khoản: thêm (mật khẩu bắt buộc), sửa (email, tên hiển thị, vai trò, khóa/mở khóa, đặt lại mật khẩu), xóa. Admin không tự đổi vai trò/khóa/xóa mình; luôn phải còn >= 1 admin đang hoạt động. Xóa chỉ được khi tài khoản chưa có bài, bình luận, hồ sơ doanh nghiệp, lượt xem, lần sửa trang/cấu hình (thực tế gần như chỉ xóa được tài khoản mới tạo chưa đụng gì) - còn lại khuyên khóa.
- Bài đăng: admin xem mọi bài mọi trạng thái; đổi trạng thái tự do giữa Draft/Public/Hidden/Deleted (kể cả khôi phục bài đã xóa); sửa nội dung dùng lại `/Post/Edit/{id}` (CanEdit cho admin, trừ bài Deleted). Không có xóa cứng.
- Bình luận: admin ẩn/hiện/xóa mềm (`Visible/Hidden/Deleted`), cascade sang câu trả lời (ẩn/xóa gốc thì các trả lời đang hiện cũng ẩn/xóa; hiện lại gốc thì hiện lại các trả lời đang ẩn). Hiện lại một đánh giá bị chặn nếu người đó đã có đánh giá khác đang hiển thị cùng đối tượng; hiện lại câu trả lời bị chặn nếu gốc chưa hiển thị. Admin không sửa chữ của người khác.
- Hồ sơ nhà cung cấp: admin tạo/sửa/xóa hồ sơ của tài khoản có Role = Provider (kể cả hồ sơ không công khai); chỉ bỏ được logo, không tải logo hộ. Quy tắc kiểm tra dùng chung với nhà cung cấp tự sửa ở `Helpers/ProviderProfileRules.cs`.
- Nhãn: thêm nhãn thường, sửa tên/mô tả/bật-tắt; 5 nhãn hệ thống chỉ đổi tên/mô tả (không tắt, không xóa); nhãn đã gắn cho bài (kể cả bài đã xóa mềm) không xóa được, chỉ tắt.
- Trang thông tin: chỉ sửa 3 trang có sẵn (tiêu đề, nội dung bằng `RichEditor`, bật/tắt hiển thị), không thêm/xóa trang. Nội dung được làm sạch bằng `HtmlCleaner` khi lưu, video YouTube hiển thị qua `ShowVideos` (HomeController đặt `ViewBag.ContentHtml`). Thanh công cụ soạn thảo tách thành `Views/Shared/_RichEditor.cshtml` dùng chung với trang soạn bài (thêm 2 nút "Tiêu đề"/"Đoạn thường" qua `data-command="formatBlock" data-value="h2|p"`). Nội dung 3 trang seed trong SQL không qua bộ lọc nhưng do chính chúng ta viết.
- Cấu hình: 5 công tắc Register/Post/Comment/Rating/Promo; chưa có dòng trong `Settings` thì coi là bật và tạo dòng khi admin gạt lần đầu. Việc chặn thao tác nằm ở các API nghiệp vụ qua `FeatureCheck` (đã kiểm tra từ GĐ1-3).
- Chỉ admin đổi vai trò. Không có chức năng xin cấp quyền. Mỗi tài khoản đúng 1 vai trò: `User`, `Chef`, `Provider`, `Admin`.
- Ảnh: chỉ jpg/png/webp, tối đa 2MB, lưu `wwwroot/uploads/`. Video chỉ là iframe YouTube (bộ lọc HTML chỉ cho phép `youtube.com/embed` và `youtube-nocookie.com`). Dùng video mẫu không bản quyền hoặc placeholder.
- Chart.js: người dùng sẽ tự tải file về `wwwroot/lib/chartjs/` (hoặc nhờ Claude tải nếu mạng bật). Soạn thảo bài đăng: tự viết bằng `contenteditable`, lọc HTML phía server bằng NuGet `HtmlSanitizer` (giai đoạn 2). **Cuối dự án phải nhắc người dùng cách cài các gói NuGet và cách dùng chúng.**

## 3. Cơ sở dữ liệu

- Dùng `WebsiteAmThuc2.sql` (schema, **xóa sạch bảng cũ khi chạy**) rồi `WebsiteAmThucSeed.sql` (dữ liệu nền, UTF-8 có BOM). `WebsiteAmThuc.sql` là file cũ tiếng Việt, **không dùng nữa**, giữ lại để tham khảo.
- Tên bảng/cột tiếng Anh để khớp 1-1 với entity C# (không cần ánh xạ tên). Mọi cột số là `INT` (tránh lỗi kiểu TINYINT/SMALLINT với EF).
- Bảng: `Accounts`, `ProviderProfiles`, `Posts`, `Labels`, `PostLabels`, `Recipes`, `Ingredients`, `RecipeSteps`, `Comments`, `Visits`, `InfoPages`, `Settings`.
- Quy ước người dùng: mỗi bảng chỉ có khóa chính, cột, khóa ngoại, ràng buộc (không trigger, procedure, view). Có 2 filtered unique index cho đánh giá và 1 cho `Labels.SystemKey` (đây là ràng buộc duy nhất, không phải index hiệu năng).
- Phân loại bài đăng: không có cột loại. Dùng 5 nhãn hệ thống (`Labels.SystemKey`: `Recipe`, `Tip`, `Experience`, `Review`, `Promo`) + nhãn thường (SystemKey = NULL). Bài công thức = có dòng trong `Recipes`.
- `Comments`: `Rating` NULL là bình luận thường, 1..10 là đánh giá. Đích là `PostId` hoặc `TargetAccountId` (đúng 1 trong 2). Trả lời = có `ParentId`, **chỉ 1 cấp (kiểm tra trong code)**, đánh giá không được là trả lời, không tự đánh giá hồ sơ mình. Mỗi tài khoản 1 đánh giá `Visible` cho mỗi đích. Xóa mềm bằng `Status` (`Visible/Hidden/Deleted`).
- `Visits.Kind`: `Page` (lượt xem trang, cho biểu đồ lưu lượng), `Post` (PostId), `Profile` (ProfileAccountId).
- `Settings.Name`: `Register`, `Post`, `Comment`, `Rating`, `Promo`. Không có dòng = coi như bật.
- Tài khoản mẫu: `admin/admin123`, `daubep/123456`, `nhacungcap/123456` (có hồ sơ doanh nghiệp), `nguoidung/123456`.
- Chuỗi kết nối: `appsettings.json` -> `ConnectionStrings:Default`.

## 4. Quy ước đặt tên (do người dùng quy định, bắt buộc)

- Class HTML: chữ thường, không dấu, cố gắng 1 từ, nhiều từ thì nối bằng `-` (ví dụ `topbar-inner`, `notice-error`). Tránh trùng tên class Bootstrap (`card`, `btn`, `nav`, `container`...).
- Id HTML: viết hoa chữ cái đầu mỗi từ, không dấu, không gạch (`LoginForm`, `UsernameInput`).
- Biến JS: viết liền, chữ thường từ đầu, từ sau viết hoa (`anApple`).
- Hàm JS: hành động + đối tượng (`eatApple`, `loadStats`).
- Class JS: PascalCase (`LoginPage`, `NavBar`).
- C#: theo chuẩn C# (PascalCase cho class/method/property, camelCase cho biến/tham số), dùng từ trực tiếp, đơn giản, **không có ký tự đặc biệt (không dấu gạch dưới)**.
- Không dùng top-level code: C# có `class Program` + `Main`. JS: mọi logic trong class/hàm; cuối file chỉ có đúng 1 dòng khởi động dạng `new XxxPage().start();`.
- CSS: ưu tiên class/id tự đặt; Bootstrap chỉ dùng cho thành phần ít quan trọng (hiện chỉ nạp `bootstrap.bundle.min.js` + css, chưa dùng class Bootstrap nào). Màu nằm trong biến `:root` đầu `site.css`.
- Màu chủ đạo: cam đất `#c8651b` trên nền kem `#fffaf3`.

## 5. Cấu trúc thư mục

```
PhoAmThuc/
  Program.cs                 class Program, hàm AddServices + SetupPipeline
  appsettings.json           chuỗi kết nối
  WebsiteAmThuc2.sql         schema mới (dùng)
  WebsiteAmThucSeed.sql      dữ liệu nền (dùng)
  Controllers/               HomeController, AccountController, PostController, ProfileController, ProviderController (trang)
                             AccountApiController, PostApiController, UploadApiController, CommentApiController,
                             ProfileApiController, ProviderApiController (list + khu nhà cung cấp), HomeApiController (JSON)
                             AdminApiBase (lớp cha, CheckAdmin) + AdminDashboard/Account/Post/Comment/Provider/Label/Page/
                             SettingApiController (JSON, route api/admin/...), AdminController (trang /Admin)
  Models/                    entity (1 file/bảng), AppDbContext, AccountRequests, PostRequests, CommentRequests, ProfileRequests, AdminRequests
  Helpers/                   Constants (Roles, PostStatus, CommentStatus, VisitKind, LabelKey, Features),
                             CurrentUser, FeatureCheck, RatingHelper, VisitHelper, ApiResult,
                             HtmlCleaner, PostAccess, ImageUploader, PostCards, ProviderProfileRules, RecordVisitAttribute
  Views/                     Home, Account, Post (Index, Detail, Editor, Mine), Profile (Detail, Edit), Provider (Index, Manage), Admin (Index + 8 partial _Dashboard/_Accounts/_Posts/_Comments/_Providers/_Labels/_Pages/_Settings),
                             Shared (_Layout, _PostTemplates, _CommentSection, _RichEditor, Error, Soon)
  wwwroot/css/site.css       toàn bộ CSS tự viết
  wwwroot/js/                site.js (Api, Notice, UrlParam, Template, Format, Avatar, NavBar, AccountMenu, SiteScript)
                             login.js, register.js
                             postcard.js (PostCard), pager.js (Pager), commentsection.js (CommentSection)  <- file thư viện: chỉ định nghĩa class
                             richeditor.js (RichEditor), recipeform.js (RecipeForm)  <- file thư viện: chỉ định nghĩa class
                             postlist.js, postdetail.js, postmine.js, posteditor.js, home.js,
                             profiledetail.js, profileedit.js, providerlist.js, providermanage.js  <- file trang: có 1 dòng khởi động cuối file
                             tabcontrol.js (TabControl), admin*.js (AdminDashboardTab, AdminAccountsTab, AdminPostsTab, AdminCommentsTab,
                             AdminProvidersTab, AdminLabelsTab, AdminInfoTab, AdminSettingsTab), adminpage.js (trang, 1 dòng khởi động)
                             chartview.js (ChartView), providerstats.js (ProviderStatsTab, ProviderMarketTab),
                             providerfeedback.js (ProviderFeedbackTab)  <- file thư viện: chỉ định nghĩa class
  wwwroot/lib/chartjs/       chart.umd.js (Chart.js 4.4.7, người dùng đã thêm và xác nhận chạy)
  wwwroot/uploads/           ảnh người dùng tải lên (tạo tự động lúc chạy)
```
Người dùng đã đồng ý thêm `Helpers/`. Không tạo `Routes/` vì ASP.NET dùng attribute routing.

## 6. Tiến độ các giai đoạn

| GĐ | Nội dung | Trạng thái |
|----|----------|-----------|
| 1 | Nền tảng: 2 file SQL, entity, DbContext, Program, Helpers, layout + menu tài khoản, đăng ký/đăng nhập/đăng xuất, trang Giới thiệu/Liên hệ/Trợ giúp (đọc từ DB), trang chủ khung | Trang chủ đã chạy; phần DB chưa xác nhận |
| 2 | Bài đăng: tạo/sửa/xóa (soft delete), trình soạn thảo, upload ảnh, nhãn, trình tạo công thức, trang chi tiết, trang quản lý bài của tôi, ghi lượt xem bài, danh sách cơ bản (phân trang, `kind=recipe`) | **Đã xong, người dùng xác nhận chạy tốt** (ảnh, video, công thức, nhãn, quản lý bài) |
| 3 | Bình luận, trả lời (1 cấp), sửa/xóa; đánh giá bài đăng và hồ sơ (1-10, 1 đánh giá/đích, không tự đánh giá hồ sơ). Khu bình luận đã gắn vào trang xem bài. API + `_CommentSection` + `commentsection.js` dùng chung, **trang hồ sơ sẽ gắn ở GĐ4** | **Đã xong, người dùng xác nhận chạy tốt** (bình luận, trả lời, đánh giá, công tắc Comment/Rating) |
| 4 | Danh sách bài có tìm kiếm/lọc/sắp xếp/phân trang; trang chủ 4 khu đề xuất; hồ sơ công khai + sửa hồ sơ cá nhân (tên, giới thiệu, ảnh); danh sách nhà cung cấp; ghi lượt xem hồ sơ; tên tác giả thành link | **Đã xong, người dùng xác nhận chạy ổn** |
| 5 | Khu vực nhà cung cấp `/Provider/Manage`: tạo/sửa hồ sơ doanh nghiệp, lối tắt bài quảng bá (`?kind=promo`), thống kê của tôi + biểu đồ (Chart.js), xem phản hồi nhận được, thông tin thị trường | **Đã xong, người dùng xác nhận chạy ổn** (đã cài Chart.js) |
| 6 | Quản trị `/Admin`: dashboard (Chart.js), quản lý tài khoản/vai trò, bài đăng, bình luận-đánh giá, hồ sơ nhà cung cấp, nhãn, trang thông tin, công tắc chức năng; ghi lượt xem trang bằng `[RecordVisit]` | **Đã viết, chờ người dùng biên dịch và chạy thử** |
| 7 | Hoàn thiện: trang 404, kiểm tra lại quyền/công tắc ở mọi API, dữ liệu mẫu (bài, bình luận, video YouTube), responsive, hướng dẫn cài NuGet/Chart.js/HtmlSanitizer cho người dùng | Chưa làm |

## 7. Việc phải làm khi sang các giai đoạn sau

- (GĐ5 đã thêm "Khu vực nhà cung cấp" cho Provider; còn lại mục Quản trị cho Admin ở GĐ6.) Mỗi giai đoạn thêm mục tương ứng vào `accountpanel` trong `Views/Shared/_Layout.cshtml` (đang có chỗ ghi chú): quản lý bài đăng (GĐ2), hồ sơ của tôi (GĐ4), khu vực nhà cung cấp (GĐ5, chỉ `Provider`), quản trị (GĐ6, chỉ `Admin`). Mục nào chỉ hiện cho vai trò đúng, nhưng **API vẫn phải tự kiểm tra quyền**.
- Link "Công thức nấu ăn" trong menu là `/Post?kind=recipe`. Nav chỉ tô sáng khi URL khớp đúng (có thêm bộ lọc thì không tô).
- `CommentSection("profile", ...)` đã được gắn vào trang hồ sơ. `PostApiController.GetPost` trả `ratingAverage/ratingCount`. Provider box ở trang bài có link sang hồ sơ nhà cung cấp.
- Admin chưa có trang riêng để xem/ẩn/xóa bài của người khác; API `status` đã cho admin đặt `Hidden` (GĐ6 làm giao diện). `GetMine` chỉ trả bài của chính người đăng nhập.
- Ảnh tải lên không bao giờ bị xóa khỏi `wwwroot/uploads/` (kể cả khi bài bị xóa). Chấp nhận được cho MVP.
- `HtmlSanitizer`: cấu hình ở `HtmlCleaner.CreateSanitizer`. Nếu người dùng báo định dạng (màu, cỡ chữ) bị mất sau khi lưu, kiểm tra danh sách `AllowedCssProperties/AllowedAttributes` ở đó.
- Chưa có chống tấn công CSRF/XSS ngoài việc lọc HTML bài đăng (người dùng không yêu cầu bảo mật cao). Nội dung `InfoPages.Content` hiển thị bằng `Html.Raw`, chỉ admin sửa được; GĐ6 nên lọc bằng HtmlSanitizer khi lưu.
- Kiểm tra `Visits`: chỉ ghi lượt xem trang (`Kind = Page`) khi làm dashboard (GĐ6), dùng một filter hoặc middleware nhỏ.
- Admin chưa có giao diện ẩn (`Hidden`) bình luận; API xóa đã cho admin xóa bình luận của người khác (GĐ6 làm giao diện quản lý, nên thêm API ẩn/hiện).
- Nhà cung cấp xem phản hồi (GĐ5): dùng lại `GET api/comment/list?profileId=` (đã có UI trên trang hồ sơ) cho đánh giá hồ sơ, và truy vấn thêm bình luận/đánh giá trên các bài của họ.

## 8. Cách người dùng chạy thử giai đoạn 1

1. Cài NuGet `Microsoft.EntityFrameworkCore.SqlServer` (bản 10.x, đúng với .NET 10) và `HtmlSanitizer` (GĐ2) cho project `PhoAmThuc`.
2. Mở SSMS, kết nối `.\SQLEXPRESS`, chạy `WebsiteAmThuc2.sql` rồi `WebsiteAmThucSeed.sql`.
3. Chạy project (F5). Thử: trang chủ, các link menu, đăng ký tài khoản mới, đăng nhập `admin/admin123`, xem menu tài khoản (đánh giá "Chưa có đánh giá", số bài 0), đăng xuất, trang Giới thiệu/Liên hệ/Trợ giúp, thu nhỏ cửa sổ để xem menu điện thoại.
4. Thử GĐ2: đăng nhập `daubep/123456` -> Viết bài mới (chọn nhãn, thử in đậm/màu/cỡ chữ, chèn ảnh, chèn video YouTube, tích "Đây là bài công thức" rồi nhập nguyên liệu + bước + ảnh bước) -> Lưu nháp -> Quản lý bài đăng -> Đăng bài -> xem bài (video phải hiện iframe) -> sửa -> xóa. Đăng nhập `nhacungcap/123456` thử nhãn Quảng bá (hồ sơ nhà cung cấp hiện trong trang xem bài). Đăng nhập `nguoidung/123456` kiểm tra không thấy mục công thức và không chọn được nhãn Quảng bá. Mở trang bài nhiều lần để thấy số lượt xem tăng.
5. Thử GĐ3 (mở một bài công khai, đăng xuất rồi lần lượt đăng nhập các tài khoản): khách chỉ thấy lời nhắc đăng nhập; `nguoidung` viết bình luận thường, gửi đánh giá kèm điểm (lần 2 phải bị chặn và có lời nhắc), sửa điểm/nhận xét, trả lời bình luận (không có nút trả lời trên câu trả lời), xóa; `daubep` thử trả lời và xóa bình luận của mình; `admin` xóa được bình luận người khác; điểm trung bình trên đầu bài và menu "Đánh giá" cập nhật. Tắt công tắc `Comment` hoặc `Rating` trong bảng `Settings` (Enabled = 0) để kiểm tra bị chặn.
6. Thử GĐ4: trang chủ (4 khu, mở vài bài để lượt xem tăng rồi tải lại, tạo vài đánh giá hồ sơ để khu "Hồ sơ nổi bật" đổi thứ tự); trang `/Post` thử từng ô lọc (từ khóa, loại, nhãn, tên người đăng, mức điểm, sắp xếp theo điểm/lượt đánh giá), bấm phân trang và kiểm tra bộ lọc được giữ nguyên; bấm tên tác giả để sang hồ sơ; hồ sơ `nhacungcap` thấy khung doanh nghiệp, danh sách bài, đánh giá hồ sơ (không đánh giá được hồ sơ của chính mình); menu "Hồ sơ của tôi" và "Sửa hồ sơ" (đổi tên, giới thiệu, ảnh đại diện); trang `/Provider` (tìm theo tên).
7. Thử GĐ5 (đăng nhập `nhacungcap/123456`, menu "Khu vực nhà cung cấp"): thẻ Hồ sơ doanh nghiệp (sửa thông tin, đổi logo, bỏ tick "hiển thị công khai" rồi kiểm tra `/Provider` không còn thấy, tick lại); thẻ Bài quảng bá (hai nút, nhãn Quảng bá được chọn sẵn); thẻ Thống kê (số liệu thay đổi sau khi xem bài/hồ sơ, đổi 7/30/90 ngày); thẻ Phản hồi (dùng tài khoản khác bình luận/đánh giá bài và hồ sơ của nhà cung cấp rồi kiểm tra hiện đủ, thử bộ lọc); thẻ Thị trường. Đăng nhập tài khoản không phải nhà cung cấp mở `/Provider/Manage` phải bị chuyển về trang chủ. Chưa có Chart.js: thấy lời nhắc và các con số vẫn đúng; sau khi cài Chart.js: biểu đồ hiện.
8. Thử GĐ6 (đăng nhập `admin/admin123`, menu "Quản trị website"): thẻ Tổng quan (mở vài trang công khai rồi xem biểu đồ lượt xem trang/bài/hồ sơ tăng, đổi 7/30/90 ngày); Tài khoản (thêm, sửa vai trò, khóa rồi thử đăng nhập tài khoản bị khóa, đặt lại mật khẩu, thử tự hạ quyền/tự khóa mình và xóa tài khoản đã có dữ liệu - phải bị chặn); Bài đăng (đổi sang Hidden rồi xem trang công khai không thấy, Deleted rồi khôi phục); Bình luận (ẩn một đánh giá và xem điểm trung bình đổi); Nhà cung cấp (sửa hồ sơ, tạo hồ sơ cho nhà cung cấp mới, xóa); Nhãn (thêm, sửa, tắt, thử xóa nhãn đang dùng); Trang thông tin (sửa nội dung trang Giới thiệu bằng trình soạn thảo, xem kết quả ở trang công khai); Cấu hình (gạt công tắc rồi kiểm tra chức năng bị chặn). Đăng nhập tài khoản không phải Admin mở `/Admin` phải bị chuyển về trang chủ, và gọi trực tiếp `/api/admin/...` phải nhận 403.
9. Lỗi biên dịch hoặc lỗi chạy: dán nguyên văn thông báo lỗi cho Claude.

## 9. Nhật ký

- Session 1: đọc đặc tả, chốt kiến trúc và quy ước, viết toàn bộ giai đoạn 1 (chưa biên dịch được vì sandbox không có .NET SDK). Đã `node --check` các file JS: không lỗi cú pháp.
- Session 1 (tiếp): người dùng xác nhận trang chủ chạy (sau khi tạo project mới và copy file). Viết toàn bộ GĐ2 (bài đăng). `node --check` mọi file JS đạt; đã test bằng Node các hàm `Format` và `RichEditor.findYoutubeId`. C# vẫn chưa được biên dịch.
- Session 1 (tiếp 2): người dùng gửi ảnh chụp xác nhận GĐ2 chạy tốt (danh sách bài, trang xem bài có ảnh/video/công thức/khung nhà cung cấp, quản lý bài, trang soạn bài). Viết GĐ3: `CommentApiController`, `_CommentSection`, `commentsection.js`, nâng `Pager` (tham số `onClick`), gắn vào trang xem bài. `node --check` đạt; C# chưa biên dịch.
- Session 1 (tiếp 3): người dùng xác nhận GĐ3 chạy tốt (kể cả bật/tắt công tắc Comment, Register, Post bằng UPDATE bảng Settings). Viết GĐ4: tách `PostCards`, nâng `PostApiController.GetList`, thêm `ProfileController/ProfileApiController`, `ProviderApiController`, `HomeApiController` + view/JS/CSS tương ứng. `node --check` đạt; C# chưa biên dịch.
- Session 1 (tiếp 4): người dùng xác nhận GĐ4 chạy ổn; yêu cầu viết biểu đồ như thể đã cài Chart.js và nhắc cài sau. Viết GĐ5: `ProviderApiController` (mine/stats/feedback/market), `ProviderController.Manage`, `Provider/Manage.cshtml`, `providermanage.js`, `providerstats.js`, `providerfeedback.js`, `chartview.js`, CSS; thêm `kind` cho `api/post/mine`, `Post/Mine?kind=promo`, `Post/Create?kind=promo`, link menu cho Provider. `node --check` đạt, `ChartView` đã test với Chart giả; C# chưa biên dịch.
- Session 1 (tiếp 5): người dùng xác nhận GĐ5 chạy ổn và đã cài Chart.js. Viết GĐ6: 8 controller `Admin...ApiController` + `AdminApiBase` + `AdminController`, `RecordVisitAttribute`, `ProviderProfileRules` (tái cấu trúc `ProviderApiController`), `Views/Admin/*`, `_RichEditor` (tách từ Editor.cshtml), JS `tabcontrol/admin*`, CSS, menu admin. `node --check` đạt; đã test `TabControl` bằng DOM giả; C# chưa biên dịch.
