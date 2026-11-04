# Phố Ẩm Thực - Hướng dẫn

Website chia sẻ công thức nấu ăn, kinh nghiệm, trải nghiệm và đánh giá ẩm thực, viết bằng ASP.NET Core MVC (.NET 10), cơ sở dữ liệu SQL Server. C# xử lý quyền, dữ liệu và trả JSON; JavaScript trong trình duyệt hiển thị và xử lý tương tác.

Mục lục: [1. Cài đặt lần đầu](#1-cài-đặt-lần-đầu) · [2. Tài khoản mặc định](#2-tài-khoản-mặc-định) · [3. Cách dùng website](#3-cách-dùng-website) · [4. Khu vực quản trị](#4-khu-vực-quản-trị) · [5. Dữ liệu mẫu, ảnh và video](#5-dữ-liệu-mẫu-ảnh-và-video) · [6. Cấu trúc dự án](#6-cấu-trúc-dự-án-dành-cho-người-sửa-code) · [7. Quy ước đặt tên](#7-quy-ước-đặt-tên) · [8. Thêm tính năng mới](#8-thêm-tính-năng-mới) · [9. Sự cố thường gặp](#9-sự-cố-thường-gặp) · [10. Giới hạn đã biết](#10-giới-hạn-đã-biết-và-bảo-mật) · [11. Dọn file thừa](#11-dọn-file-không-cần-thiết)

---

## 1. Cài đặt lần đầu

### 1.1 Cần có
- Visual Studio với workload "ASP.NET and web development" và **.NET 10 SDK**.
- SQL Server (bản Express là đủ) và SQL Server Management Studio (SSMS).

### 1.2 Cài hai gói NuGet
Website cần đúng hai gói. Cài một lần cho project `PhoAmThuc`:

| Gói | Dùng để |
|---|---|
| `Microsoft.EntityFrameworkCore.SqlServer` (chọn bản **10.x**) | Kết nối và truy vấn SQL Server từ C# (qua `AppDbContext`) |
| `HtmlSanitizer` | Làm sạch nội dung HTML của bài đăng và trang thông tin, chỉ giữ thẻ an toàn (class `HtmlCleaner`) |

**Cách cài bằng giao diện:** chuột phải project, chọn *Manage NuGet Packages*, thẻ *Browse*, tìm tên gói, chọn *Install*.

**Hoặc bằng lệnh:** *Tools, NuGet Package Manager, Package Manager Console*, rồi chạy:
```
Install-Package Microsoft.EntityFrameworkCore.SqlServer
Install-Package HtmlSanitizer
```
Nếu Visual Studio hỏi phiên bản, chọn bản ổn định (không phải preview) khớp .NET 10 (EF Core 10.x).

### 1.3 Thêm thư viện biểu đồ Chart.js
Biểu đồ ở khu nhà cung cấp và trang quản trị dùng Chart.js. Thiếu file này website vẫn chạy, chỉ không vẽ biểu đồ (hiện lời nhắc "Chưa cài thư viện Chart.js").

1. Tải file **Chart.js bản 4.x dạng UMD** (đường dẫn kết thúc bằng `chart.umd.js`), ví dụ từ jsDelivr: `https://cdn.jsdelivr.net/npm/chart.js@4.4.7/dist/chart.umd.js`.
2. Đặt vào `wwwroot/lib/chartjs/` với **đúng tên** `chart.umd.js`.
3. Chạy lại, nhấn Ctrl+F5 để trình duyệt tải file mới. Kiểm tra: mở F12, tab Console, gõ `typeof Chart` phải ra `"function"`.

### 1.4 Tạo cơ sở dữ liệu
Mở SSMS, kết nối vào SQL Server, mở từng file trong thư mục project rồi bấm *Execute* theo thứ tự:

**Trường hợp A: máy chưa có CSDL (cài mới)**
1. `WebsiteAmThuc2.sql`: tạo CSDL `WebsiteAmThuc` và các bảng. **Chạy file này sẽ xóa sạch bảng cũ**.
2. `WebsiteAmThucSeed.sql`: 3 tài khoản, nhãn, công tắc chức năng, 3 trang thông tin.
3. `WebsiteAmThucSample.sql` (không bắt buộc): 6 bài đăng mẫu kèm bình luận, đánh giá và lượt xem.

**Trường hợp B: CSDL đã có dữ liệu thử nghiệm, muốn làm sạch**
1. `WebsiteAmThucReset.sql`: xóa hết dữ liệu, chỉ giữ 3 tài khoản `admin`, `nhacungcap`, `nguoidung` (và hồ sơ doanh nghiệp của `nhacungcap`), bật lại mọi công tắc.
2. `WebsiteAmThucSample.sql` (không bắt buộc).
3. Xóa tay các ảnh người dùng đã tải lên trong `wwwroot/uploads/`.

Các file `.sql` lưu UTF-8 có BOM để SSMS hiển thị đúng tiếng Việt. Nếu thấy chữ lạ, mở file bằng *File, Open, File* rồi chọn đúng bảng mã UTF-8.

### 1.5 Chuỗi kết nối
Trong `appsettings.json`:
```json
"ConnectionStrings": {
  "Default": "Server=.\\SQLEXPRESS;Database=WebsiteAmThuc;Trusted_Connection=True;TrustServerCertificate=True;"
}
```
Đổi `.\\SQLEXPRESS` thành tên máy chủ SQL của bạn (tên hiện trong hộp thoại kết nối của SSMS). Đăng nhập bằng Windows Authentication nên không cần mật khẩu.

### 1.6 Chạy
Nhấn F5 trong Visual Studio. Lần đầu nên chọn Build, Rebuild Solution.

---

## 2. Tài khoản mặc định

| Tên đăng nhập | Mật khẩu | Vai trò |
|---|---|---|
| `admin` | `admin123` | Quản trị viên |
| `nhacungcap` | `123456` | Nhà cung cấp (có sẵn hồ sơ "Nhà hàng Hương Việt") |
| `nguoidung` | `123456` | Người dùng thường |

Hãy **đổi mật khẩu admin** (Quản trị, Tài khoản, Sửa, nhập mật khẩu mới) trước khi đưa website cho người khác dùng.

Chưa có tài khoản **Đầu bếp**: đăng nhập `admin`, vào *Quản trị, Tài khoản*, sửa một tài khoản và đổi vai trò sang Đầu bếp (hoặc tạo tài khoản mới rồi đặt vai trò này).

---

## 3. Cách dùng website

### 3.1 Khách (chưa đăng nhập)
Xem trang chủ, danh sách bài đăng (có tìm kiếm, lọc, sắp xếp), công thức, nhà cung cấp, hồ sơ công khai và các trang Giới thiệu, Liên hệ, Trợ giúp. Muốn bình luận, đánh giá hay đăng bài cần đăng ký tài khoản (nút *Đăng ký* góc trên bên phải; tài khoản mới có vai trò Người dùng).

### 3.2 Người dùng đã đăng nhập
Bấm tên của bạn ở góc phải để mở menu tài khoản (vai trò, điểm đánh giá, số bài đăng, hồ sơ, viết bài, quản lý bài, đăng xuất).

**Viết bài:** menu, *Viết bài mới*.
- Bắt buộc có tiêu đề, nội dung và **ít nhất một nhãn**. Mô tả ngắn có thể để trống, hệ thống tự lấy từ phần đầu nội dung.
- Thanh công cụ soạn thảo: in đậm, nghiêng, gạch chân, tiêu đề, cỡ chữ, màu chữ, danh sách, chèn ảnh, chèn video.
- **Chèn ảnh:** bấm *Chèn ảnh*, chọn file jpg, png hoặc webp, tối đa 2MB. Ảnh đại diện (ảnh bìa) chọn riêng ở ô *Ảnh đại diện*.
- **Chèn video:** bấm *Chèn video YouTube* rồi dán đường dẫn video (dạng `youtube.com/watch?v=...`, `youtu.be/...`, `.../embed/...` hoặc `.../shorts/...`). Chỉ nhận YouTube. Trong khung soạn thảo video hiện như một ô giữ chỗ, khi xem bài mới thành video thật.
- Bấm *Lưu nháp* (chỉ bạn thấy) hoặc *Đăng bài* (công khai). Quản lý, sửa, chuyển về nháp, xóa tại menu, *Quản lý bài đăng*.
- Bạn chỉ sửa và xóa được bài của chính mình. Bài bị quản trị viên ẩn thì bạn không tự công khai lại được.

**Bình luận và đánh giá** (cuối trang bài đăng hoặc trang hồ sơ):
- Có thể bình luận, trả lời bình luận (chỉ một cấp), sửa và xóa bình luận của mình.
- **Đánh giá** là bình luận kèm điểm từ 1 đến 10 và nhận xét ngắn (tối đa 500 ký tự). Mỗi người chỉ có **một** đánh giá cho mỗi bài hoặc mỗi hồ sơ. Muốn đổi thì sửa đánh giá cũ, hoặc xóa rồi đánh giá lại.
- Không tự đánh giá hồ sơ của chính mình. Hồ sơ chỉ nhận đánh giá có điểm (và câu trả lời cho đánh giá đó).
- Điểm trung bình hiển thị "Chưa có đánh giá" khi chưa có ai chấm (không hiển thị 0).

**Hồ sơ cá nhân:** menu, *Sửa hồ sơ* để đổi tên hiển thị, giới thiệu và ảnh đại diện. Bấm tên tác giả ở bất kỳ đâu để xem hồ sơ công khai của họ.

### 3.3 Đầu bếp
Như người dùng, thêm: khi viết bài có ô **"Đây là bài công thức nấu ăn"**. Tick vào sẽ hiện phần nhập thời gian chuẩn bị, thời gian nấu, số khẩu phần, độ khó, nguyên liệu (tên, số lượng, đơn vị, ghi chú) và các bước thực hiện (mỗi bước có thể kèm ảnh, đổi thứ tự bằng nút mũi tên). Bài công thức tự được gắn nhãn *Công thức nấu ăn*.

### 3.4 Nhà cung cấp
Menu, *Khu vực nhà cung cấp* có 5 thẻ:
- **Hồ sơ doanh nghiệp:** tên, mô tả, logo, địa chỉ, điện thoại, email, website, mạng xã hội. Tick *Hiển thị công khai* thì mới xuất hiện trong danh sách nhà cung cấp.
- **Bài quảng bá:** viết bài có nhãn *Quảng bá* (chỉ nhà cung cấp và quản trị viên được dùng nhãn này). Bài hiện kèm khung thông tin doanh nghiệp.
- **Thống kê của tôi:** lượt xem hồ sơ và bài, điểm đánh giá, số bình luận và đánh giá nhận được, biểu đồ lượt xem theo ngày (7, 30, 90 ngày).
- **Phản hồi:** các bình luận và đánh giá người khác để lại trên bài và hồ sơ của bạn.
- **Thị trường:** số bài công khai theo loại và các nhãn xuất hiện nhiều.

---

## 4. Khu vực quản trị

Đăng nhập `admin`, menu, *Quản trị website* (`/Admin`). Tám thẻ:

| Thẻ | Làm được gì |
|---|---|
| Tổng quan | Số tài khoản, bài công khai, bình luận, đánh giá; biểu đồ lượt xem theo ngày (trang, bài đăng, hồ sơ); bài nhiều lượt xem nhất |
| Tài khoản | Thêm, sửa (email, tên, vai trò, khóa/mở khóa, đặt lại mật khẩu), xóa |
| Bài đăng | Xem mọi bài mọi trạng thái, đổi trạng thái (Bản nháp, Công khai, Bị ẩn, Đã xóa; chọn lại để khôi phục), sửa nội dung |
| Bình luận | Ẩn, hiện lại, xóa bình luận và đánh giá |
| Nhà cung cấp | Tạo, sửa, xóa hồ sơ doanh nghiệp (kể cả hồ sơ không công khai) |
| Nhãn | Thêm, sửa, bật/tắt, xóa nhãn |
| Trang thông tin | Sửa nội dung Giới thiệu, Liên hệ, Trợ giúp |
| Cấu hình | Bật/tắt 5 chức năng: đăng ký, đăng bài, bình luận, đánh giá, bài quảng bá |

**Các quy tắc bảo vệ dữ liệu:**
- Không tự đổi vai trò, tự khóa hay tự xóa tài khoản của chính mình; luôn phải còn ít nhất một quản trị viên đang hoạt động.
- Tài khoản đã có bài đăng, bình luận, lượt xem... thì **không xóa được**, hãy **khóa** tài khoản (không đăng nhập được nữa).
- Bài đăng và bình luận chỉ **xóa mềm** (đổi trạng thái), không mất dữ liệu liên quan. Ẩn hoặc xóa bình luận gốc thì các câu trả lời cũng ẩn hoặc xóa theo.
- 5 nhãn hệ thống (Công thức nấu ăn, Kinh nghiệm nấu ăn, Trải nghiệm ẩm thực, Đánh giá, Quảng bá) chỉ đổi được tên và mô tả, không tắt hay xóa. Nhãn đã gắn cho bài thì chỉ tắt được, không xóa.
- Khi một chức năng bị tắt, **máy chủ** từ chối thao tác đó (không chỉ ẩn nút). Tắt đăng bài chỉ chặn bài mới, vẫn sửa và xóa được bài cũ; tắt đánh giá chỉ chặn đánh giá mới.

---

## 5. Dữ liệu mẫu, ảnh và video

`WebsiteAmThucSample.sql` thêm 6 bài mẫu (2 công thức, 1 mẹo, 1 đánh giá, 1 trải nghiệm, 1 quảng bá), vài bình luận, đánh giá, câu trả lời và lượt xem rải đều 14 ngày để trang chủ và dashboard có số liệu. Nội dung mẫu do website tạo ra, **không phải đánh giá của quán ăn có thật**. Xóa tất cả bằng `WebsiteAmThucReset.sql`.

**Ảnh** lấy trực tiếp từ Wikimedia Commons (cần Internet khi xem, không lưu trong project). Giấy phép yêu cầu ghi nguồn, mỗi bài mẫu đã có dòng ghi công ở cuối nội dung:

| Ảnh | Tác giả | Giấy phép |
|---|---|---|
| `Pho-Beef-Noodles-2008.jpg` | Kham Tran | CC BY 3.0 |
| `Com-Tam-2008.jpg` | Kham Tran | CC BY-SA 3.0 |
| `Bánh mì.jpg` | Stu Spivack | CC BY-SA 2.0 |
| `Phở bò (39425047901).jpg` | Vinnie Cartabiano | CC BY 2.0 |

**Video** nhúng bằng trình phát YouTube chính thức, từ kênh Điện máy XANH: `uMZrHDdPwSo` (cách nấu phở gà Hà Nội) và `HEdZlCQVbEM` (cách làm bánh mì Việt Nam). Đây **không phải video giấy phép mở**: chúng chỉ được nhúng (không sao chép), và có thể bị chủ kênh gỡ hay tắt cho phép nhúng bất cứ lúc nào. Nếu muốn dùng video khác, sửa bài và thay đường dẫn bằng nút *Chèn video YouTube*.

**Khi tự thêm nội dung thật:** dùng ảnh của chính bạn hoặc ảnh có giấy phép cho phép dùng lại, và ghi nguồn khi giấy phép yêu cầu. Ảnh bạn tải lên website được lưu trong `wwwroot/uploads/` (nhớ sao lưu thư mục này cùng CSDL).

Lưu ý: bài mẫu dùng ảnh bìa từ Internet nên khi sửa bài mẫu vẫn giữ được ảnh đó. Nhưng muốn **thay** ảnh bìa mới thì phải là ảnh tải lên từ máy bạn.

---

## 6. Cấu trúc dự án (dành cho người sửa code)

```
PhoAmThuc/
  Program.cs             Khởi động, đăng ký dịch vụ, thứ tự xử lý request
  appsettings.json       Chuỗi kết nối CSDL
  WebsiteAmThuc2.sql     Tạo CSDL (xóa bảng cũ)       WebsiteAmThucSeed.sql   Dữ liệu nền
  WebsiteAmThucReset.sql Làm sạch dữ liệu             WebsiteAmThucSample.sql Dữ liệu mẫu
  Controllers/           XxxController = trả về trang; XxxApiController = trả về JSON (route api/...)
  Models/                Mỗi bảng một class (Account, Post, Comment...), AppDbContext, các class nhận dữ liệu gửi lên
  Helpers/               Constants, CurrentUser, FeatureCheck, RatingHelper, PostCards, HtmlCleaner, ...
  Views/                 .cshtml: khung trang, các khuôn <template> cho phần tử lặp lại
  wwwroot/css/site.css   Toàn bộ CSS tự viết (màu ở biến :root đầu file)
  wwwroot/js/            JavaScript, mỗi trang một file
  wwwroot/lib/           Bootstrap, Chart.js
  wwwroot/uploads/       Ảnh người dùng tải lên (tạo tự động)
```

**Kiến trúc:**
- **C#** quyết định mọi quy tắc: ai được làm gì, chức năng bật hay tắt, kiểm tra dữ liệu, truy vấn, phân trang, tính điểm. Mọi API ghi dữ liệu đều tự kiểm tra quyền (`CurrentUser`) và công tắc (`FeatureCheck`), không tin giao diện.
- **JavaScript** gọi API bằng `fetch`, dựng các phần tử từ JSON dựa trên thẻ `<template>` trong `.cshtml` (không nối chuỗi HTML trong JS).
- API luôn trả `{ ok, message, data }` (xem `Helpers/ApiResult.cs`) kèm mã 200, 400, 401 (chưa đăng nhập), 403 (không đủ quyền) hoặc 404.
- Đăng nhập bằng Session (cookie), tối đa 8 giờ không hoạt động. `CurrentUser` đọc lại tài khoản từ CSDL mỗi request nên đổi vai trò hay khóa tài khoản có hiệu lực ngay.
- Thời gian lưu theo giờ máy chủ.

**Các class JS dùng chung (`wwwroot/js/`):** `site.js` (`Api`, `Notice`, `UrlParam`, `Template`, `Format`, `Avatar`), `postcard.js`, `pager.js`, `commentsection.js`, `richeditor.js`, `recipeform.js`, `chartview.js`, `tabcontrol.js`. File "trang" (ví dụ `postlist.js`, `adminpage.js`) kết thúc bằng đúng một dòng `new XxxPage().start();`.

**Bảng trong CSDL:** `Accounts`, `ProviderProfiles`, `Posts`, `Labels`, `PostLabels`, `Recipes`, `Ingredients`, `RecipeSteps`, `Comments`, `Visits`, `InfoPages`, `Settings`. Ý nghĩa cần nhớ:
- Loại bài đăng không có cột riêng mà xác định bằng nhãn: 5 nhãn hệ thống có `Labels.SystemKey` (`Recipe`, `Tip`, `Experience`, `Review`, `Promo`).
- Đánh giá là một dòng `Comments` có `Rating` khác rỗng. Dòng `Comments` nhắm vào bài (`PostId`) hoặc hồ sơ (`TargetAccountId`), đúng một trong hai.
- Điểm trung bình luôn tính từ các đánh giá `Status = Visible`.
- `Visits` ghi mỗi lần xem, `Kind` là `Page`, `Post` hoặc `Profile`.
- Vai trò lưu trong `Accounts.Role`: `User`, `Chef`, `Provider`, `Admin`.

---

## 7. Quy ước đặt tên

- **Class HTML:** chữ thường, không dấu, ưu tiên một từ, nhiều từ nối bằng `-` (ví dụ `postcard`, `topbar-inner`).
- **Id HTML:** viết hoa chữ cái đầu mỗi từ, không dấu, không gạch (ví dụ `LoginForm`).
- **Biến JS:** viết liền, từ đầu chữ thường, các từ sau viết hoa (ví dụ `anApple`).
- **Hàm JS:** hành động rồi đối tượng (ví dụ `loadPosts`, `showCover`).
- **Class JS:** viết hoa chữ cái đầu mỗi từ (ví dụ `PostListPage`).
- **C#:** theo chuẩn C# (class, hàm, thuộc tính viết hoa chữ cái đầu; biến và tham số viết thường chữ đầu), dùng từ trực tiếp và không dùng ký tự gạch dưới.
- Không dùng "top level code": C# có `class Program` với `Main`; JS đặt trong class và hàm.
- Code bằng tiếng Anh, chú thích bằng tiếng Việt, giao diện hiển thị tiếng Việt.

---

## 8. Thêm tính năng mới

Ví dụ thêm một trang "Sự kiện": 
1. Thêm bảng vào `WebsiteAmThuc2.sql`, thêm class trong `Models/` và một dòng `DbSet` trong `AppDbContext`.
2. Tạo `EventApiController` (route `api/event`) trả JSON qua `ApiResult`. Kiểm tra đăng nhập và quyền ở đầu mỗi API ghi.
3. Tạo `EventController` trả trang, `Views/Event/Index.cshtml` (khung và thẻ `<template>`), và `wwwroot/js/eventlist.js` (một class `EventListPage` với hàm `start()`, một dòng khởi động ở cuối).
4. Thêm CSS vào `site.css`, thêm link vào menu trong `Views/Shared/_Layout.cshtml`.
5. Muốn ghi lượt xem trang: gắn `[RecordVisit]` lên hành động trả về trang.

Muốn thêm một công tắc bật/tắt chức năng: thêm tên vào `Helpers/Constants.cs` (class `Features`) và `AdminSettingApiController`, rồi gọi `featureCheck.IsOn(Features.TenMoi)` ở API tương ứng.

---

## 9. Sự cố thường gặp

| Hiện tượng | Cách xử lý |
|---|---|
| Lỗi "layout view '_Layout' could not be located" dù file có đủ | Stop ứng dụng, xóa hai thư mục `bin` và `obj`, chọn Rebuild Solution, chạy lại (không dùng Hot Reload sau khi sao chép nhiều file) |
| Không biên dịch được, báo thiếu `UseSqlServer` hoặc `Ganss.Xss` | Chưa cài hai gói NuGet ở mục 1.2 |
| Trang báo lỗi kết nối cơ sở dữ liệu | Kiểm tra tên máy chủ trong `appsettings.json`, SQL Server đang chạy và đã chạy các file `.sql` (mục 1.4) |
| Biểu đồ không hiện, có lời nhắc "Chưa cài thư viện Chart.js" | Làm theo mục 1.3, nhấn Ctrl+F5 |
| Ảnh bìa bài mẫu không hiện | Cần có Internet (ảnh lấy từ Wikimedia Commons) |
| Chữ tiếng Việt trong file `.sql` bị lỗi phông | Mở file với bảng mã UTF-8 |
| Đăng nhập báo "Tài khoản này đã bị khóa" | Đăng nhập admin, vào Quản trị, Tài khoản, bỏ tick khóa |
| Quên mật khẩu admin | Trong SSMS chạy: `UPDATE dbo.Accounts SET Password = N'admin123' WHERE Username = N'admin';` |
| Tắt nhầm chức năng và không đăng nhập được admin | Chạy `UPDATE dbo.Settings SET Enabled = 1;` trong SSMS |

---

## 10. Giới hạn đã biết và bảo mật

Website được làm cho mục đích học tập và dùng nội bộ, nên **chưa phù hợp để đưa lên Internet** khi chưa cải thiện các điểm sau:
- **Mật khẩu lưu dạng chữ thường** trong cột `Password` (theo yêu cầu của dự án). Nên băm mật khẩu (ví dụ BCrypt) trước khi dùng thật.
- **Chưa chống CSRF**: API không kiểm tra token chống giả mạo yêu cầu. Nên thêm trước khi triển khai thật.
- Nội dung bài đăng và trang thông tin được làm sạch bằng HtmlSanitizer, nhưng nên giữ gói này cập nhật.
- Chưa có chức năng quên mật khẩu, đổi mật khẩu cho người dùng thường (chỉ admin đặt lại), xác thực email, gửi thông báo.
- Ảnh tải lên không tự bị xóa khi bài đăng bị xóa.
- Sắp xếp theo điểm đánh giá và danh sách nhà cung cấp xử lý trong bộ nhớ: phù hợp quy mô vừa và nhỏ.
- Giao diện có thích ứng điện thoại nhưng chưa được kiểm thử trên nhiều thiết bị.

---

## 11. Dọn file không cần thiết

Các file sau không cần để chạy website, có thể xóa khỏi thư mục project (chạy trong PowerShell tại thư mục chứa `Program.cs`):
```powershell
Remove-Item "*.docx" -ErrorAction SilentlyContinue                          # file đặc tả ban đầu
Remove-Item "WebsiteAmThuc.sql", "TIENDO.md" -ErrorAction SilentlyContinue  # SQL bản cũ tiếng Việt, ghi chú làm việc
Remove-Item "Views\Shared\_ValidationScriptsPartial.cshtml", "Views\Shared\Soon.cshtml", "Views\Shared\_Layout.cshtml.css", "Views\Home\Privacy.cshtml" -ErrorAction SilentlyContinue
Remove-Item "wwwroot\lib\jquery", "wwwroot\lib\jquery-validation", "wwwroot\lib\jquery-validation-unobtrusive" -Recurse -ErrorAction SilentlyContinue
Remove-Item "wwwroot\uploads\*" -ErrorAction SilentlyContinue               # ảnh tải lên lúc thử nghiệm
```
Thư mục `bin`, `obj` và `.vs` do Visual Studio tạo ra, xóa được an toàn (sẽ tự tạo lại khi build). **Giữ lại** `wwwroot/lib/bootstrap` và `wwwroot/lib/chartjs`: giao diện vẫn dựa vào Bootstrap và biểu đồ cần Chart.js.

Trước khi nộp hay chuyển project, sao lưu: toàn bộ thư mục project, `wwwroot/uploads/` và bản sao lưu CSDL (SSMS: chuột phải CSDL `WebsiteAmThuc`, *Tasks*, *Back Up...*).
