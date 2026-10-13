# VC22 — Test local frontend người dùng & phân quyền

Frontend: **`/admin/users`**. Thứ tự bàn giao: backend → test backend local → frontend → test local toàn luồng → production.

Trang sử dụng `AdminUserStateService` → `AuthorizedApiService` → các endpoint Admin VC22. Đây là Blazor Server: các lệnh HTTP đến API chạy ở Web server; tab Network của trình duyệt chủ yếu thấy kết nối Blazor, không nhất thiết thấy từng request API.

## 1. Build và tests

Chạy PowerShell tại thư mục gốc repository:

```powershell
dotnet build .\ClinicManagementSystem.slnx -c Release

dotnet test .\tests\ClinicManagementSystem.UnitTests\ClinicManagementSystem.UnitTests.csproj -c Release --no-build --no-restore

dotnet test .\tests\ClinicManagementSystem.IntegrationTests\ClinicManagementSystem.IntegrationTests.csproj -c Release --no-build --no-restore --filter "FullyQualifiedName~AuthorizationTests"
```

Kết quả đã chạy khi triển khai frontend: **416 unit tests pass**, bao gồm 18 cases mới cho `AdminUserStateService`; **220 HTTP authorization tests pass**. Build thành công với 7 warnings đã có ở các trang/dịch vụ Doctor và Receptionist.

HTTP authorization tests dùng test host/repository trong bộ nhớ. Chưa chạy các integration tests cần SQL Server container vì Docker engine chưa hoạt động. Kiểm tra trình duyệt đã dùng dữ liệu/API giả lập trong bộ nhớ, không thao tác database local hoặc Azure. Các bước dưới đây cần bạn thực hiện với database local thật.

## 2. Khởi động API và Web local

Dừng tiến trình API/Web cũ bằng `Ctrl+C` để chạy bản mới. Kiểm tra API đang trỏ tới database local bạn đã test backend; không cần đổi schema hoặc seed lại nếu đã có tài khoản test.

Trong `src/ClinicManagement.Web/appsettings.Development.json`, giá trị cần dùng:

```json
"ApiSettings": {
  "BaseUrl": "http://localhost:5238"
}
```

Terminal 1 — API:

```powershell
dotnet run --project .\src\ClinicManagementSystem.Api\ClinicManagementSystem.Api.csproj --launch-profile http
```

Terminal 2 — Web:

```powershell
dotnet run --project .\src\ClinicManagement.Web\ClinicManagement.Web.csproj --launch-profile http
```

- Web: [http://localhost:5050/](http://localhost:5050/).
- Trang VC22: [http://localhost:5050/admin/users](http://localhost:5050/admin/users).
- Swagger API: [http://localhost:5238/](http://localhost:5238/).

Đăng xuất rồi đăng nhập lại bằng Admin local để lấy JWT mới có dấu kiểm tra phiên VC22. Chuẩn bị thêm một tài khoản bệnh nhân **chỉ dùng để test**, có mật khẩu bạn biết và hồ sơ Patient đã tồn tại. Lưu lại vai trò ban đầu để khôi phục cuối buổi test. Các thao tác reset/khóa nên thực hiện trên tài khoản test đó.

## 3. Mở trang và kiểm tra quyền

1. Đăng nhập Admin. Sidebar có **Người dùng & phân quyền**; trang `/admin` có thẻ dẫn tới VC22.
2. Mở VC22: danh sách có tài khoản, họ tên/liên hệ, vai trò, đăng nhập cuối và trạng thái.
3. Tài khoản hiện tại có nhãn **Tài khoản của bạn**. Nút tự khóa bị vô hiệu hóa.
4. `lastLoginAt` null hiển thị **Chưa đăng nhập**; thời gian có giá trị hiển thị GMT+7.
5. Mở cửa sổ ẩn danh, chưa đăng nhập rồi vào `/admin/users`: không truy cập được nội dung Admin. Đăng nhập Patient và thử lại: vẫn không có quyền.

Phân quyền API là lớp kiểm tra bắt buộc: gọi trực tiếp API không có JWT trả 401; JWT hợp lệ không có Admin trả 403. Xem thêm [hướng dẫn backend VC22](VC22_BACKEND_TEST.md).

## 4. Tìm kiếm, lọc và phân trang

| Thao tác | Kết quả mong đợi |
|---|---|
| Nhập một phần username/họ tên/email/số điện thoại, bấm **Lọc** | Chỉ hiện tài khoản phù hợp |
| Chọn role Patient, bấm **Lọc** | Các dòng đều có Patient trong tập vai trò |
| Chọn **Hoạt động** hoặc **Đã khóa** | Chỉ hiện đúng trạng thái |
| Chọn 10/20/50 dòng rồi bấm **Lọc** | Số dòng mỗi trang phù hợp |
| Bấm **Sau/Trước** khi có nhiều trang | Số trang và dữ liệu thay đổi; nút ở đầu/cuối bị khóa |
| Đổi bộ lọc sau khi ở trang 2 | Sau khi bấm Lọc trở về trang 1 |
| Tìm chuỗi không tồn tại | Hiện “Không có tài khoản phù hợp với bộ lọc” |
| Bấm **Đặt lại** | Xóa bộ lọc, trở về 10 dòng/trang và trang 1 |

Vai trò được lấy từ `GET /api/admin/users/roles` bằng ID/tên thật trong database. Frontend không hardcode ID role và không tự thêm role còn thiếu vào DB.

## 5. Gán và gỡ nhiều vai trò

1. Tìm tài khoản test. Bấm **Vai trò** ở dòng đó.
2. Trang tự cuộn và focus vào form **Sửa vai trò**. Username/họ tên phải đúng người đã chọn; các role hiện tại đã được tick.
3. Chưa thay đổi gì: nút **Lưu vai trò** bị khóa.
4. Giữ Patient, tick thêm Receptionist và Pharmacist nếu có trong DB. Bấm Lưu: có thông báo thành công, form đóng, dòng tài khoản hiện đủ role đã chọn.
5. Mở lại và bỏ một role, lưu: chỉ gỡ role vừa bỏ. Request gửi **toàn bộ tập role muốn giữ**, không phải riêng role vừa tick.
6. Bỏ tất cả role: hiện cảnh báo; chưa tick xác nhận thì không lưu được. Tick **Tôi xác nhận gỡ toàn bộ vai trò...**, lưu: dòng tài khoản hiện **Chưa có vai trò**.
7. Khôi phục Patient và tập role ban đầu.
8. Thử gán Doctor cho tài khoản không có hồ sơ bác sĩ: backend trả 409; trang hiện lý do, giữ form để sửa, không cập nhật giả danh sách. Không tự tạo hồ sơ bác sĩ.
9. Mở form vai trò của chính Admin đang test: checkbox Admin đã tick và bị khóa. Không thể tự gỡ Admin.

Nếu thay đổi vai trò của chính mình thành công, Web sẽ đăng xuất và yêu cầu đăng nhập lại. Nếu muốn thử case này, chỉ thêm/gỡ role phụ phù hợp; Admin luôn được giữ lại. Backend cũng bảo vệ Admin hoạt động cuối cùng khi người khác cố gỡ Admin hoặc khóa tài khoản đó.

## 6. Reset mật khẩu

1. Bấm **Reset mật khẩu** trên tài khoản test. Form mở đúng tài khoản và tự cuộn/focus vào tiêu đề.
2. Để trống rồi xác nhận: hiển thị lỗi required và focus tới phần validation.
3. Thử dưới 8 ký tự hoặc xác nhận khác mật khẩu: hiển thị lỗi; chưa gửi thao tác hợp lệ tới API.
4. Nhập mật khẩu mới và xác nhận trùng nhau: 8–128 ký tự, tối đa **72 bytes UTF-8**. Ký tự tiếng Việt có thể chiếm nhiều hơn 1 byte.
5. Bấm **Xác nhận reset mật khẩu**: hiện thành công, form đóng. Đăng nhập tài khoản test bằng mật khẩu cũ thất bại; mật khẩu mới thành công.
6. Trong cửa sổ khác đã đăng nhập tài khoản test, thử thao tác cần API: phiên cũ bị vô hiệu hóa; phải đăng nhập lại.
7. Mở lại form: hai ô password trống. Bấm Hủy rồi mở lại: vẫn trống. Sau một lần gửi HTTP thất bại, password cũng được xóa; cần nhập lại trước khi retry.
8. Nếu tài khoản đang bị khóa, reset giữ nguyên trạng thái khóa. Cần mở khóa riêng mới đăng nhập được.

Nếu reset chính Admin đang dùng, trang cảnh báo trước và đăng xuất sau khi thành công. Chỉ test khi bạn biết mật khẩu mới đã đặt. Frontend không lưu password vào StateService/local storage hoặc hiển thị password trong danh sách.

## 7. Khóa và mở khóa

1. Bấm **Khóa** trên tài khoản test đang hoạt động. Form hiện đúng người và mô tả tác động.
2. Để trống lý do: có lỗi validation. Nhập lý do tối đa 500 ký tự rồi bấm **Xác nhận khóa tài khoản**.
3. Thành công: dòng chuyển **Đã khóa**, nút đổi thành **Mở khóa**. Tài khoản test không đăng nhập được; phiên cũ cũng bị thu hồi.
4. Bấm **Mở khóa**, nhập lý do, xác nhận: dòng trở về **Hoạt động**. Đăng nhập lại tài khoản test thành công.
5. Mở khóa không khôi phục JWT/refresh token cũ đã thu hồi.
6. Khi lọc chỉ Hoạt động, khóa tài khoản xong dòng đó biến mất. Khi lọc Đã khóa, mở khóa xong dòng biến mất. Nếu trang cuối hết dòng, giao diện tự chuyển về trang còn dữ liệu.

Các thao tác khóa/role/reset không xóa dữ liệu tài khoản. Việc ghi audit và thu hồi token do backend thực hiện.

## 8. Focus, hủy và lỗi kết nối

- Cuộn xuống cuối bảng rồi bấm một thao tác: form ở phía trên được đưa vào vùng nhìn thấy, không cần tự kéo lên.
- Bấm **Hủy**: đóng form và trả focus về nút đã mở form; nếu dòng không còn thì về tiêu đề danh sách.
- Validation/lỗi backend/thông báo thành công tự được cuộn và focus để dễ đọc.
- Trong lúc tải/gửi, các thao tác cạnh tranh bị khóa để tránh gửi trùng.
- Dừng API local bằng Ctrl+C, thử lọc/mở form: hiện lỗi kết nối thay vì báo thành công. Chạy lại API và bấm thử lại.
- Nếu lỗi tải danh mục role: có nút retry và chặn sửa role cho tới khi tải được danh mục. Không gửi tập role thiếu do lookup lỗi.
- API trả 401 sau refresh thất bại: Web đăng xuất. API trả 403: báo không có quyền, đóng form và chặn thao tác quản trị.
- Thu hẹp trình duyệt: bộ lọc/form xếp lại; bảng có thể cuộn ngang, các ô nhập và nút vẫn sử dụng được.

## 9. File frontend đã thay đổi

Đường dẫn tính từ thư mục gốc repository. Các file backend VC22 ở bước trước được liệt kê riêng trong `VC22_BACKEND_TEST.md`.

| File | Thay đổi |
|---|---|
| `src/ClinicManagement.Web/Pages/Admin/Users.razor` | Mới: danh sách, lọc/phân trang, form role/password/lock, validation và xử lý phiên |
| `src/ClinicManagement.Web/Services/AdminUserStateService.cs` | Mới: gọi API VC22, quản lý loading/kết quả/lỗi, chặn gửi trùng |
| `src/ClinicManagement.Web/wwwroot/css/admin-users.css` | Mới: CSS riêng cho VC22, desktop/mobile |
| `src/ClinicManagement.Web/wwwroot/js/admin-users.js` | Mới: helper cuộn và focus form/thông báo/nút |
| `src/ClinicManagement.Web/Program.cs` | Đăng ký DI cho AdminUserStateService |
| `src/ClinicManagement.Web/Pages/_Host.cshtml` | Link CSS và script riêng VC22 |
| `src/ClinicManagement.Web/Shared/HomePageMaster.razor` | Thêm menu Admin “Người dùng & phân quyền” |
| `src/ClinicManagement.Web/Pages/Admin/Index.razor` | Thêm thẻ mở trang VC22 |
| `tests/ClinicManagementSystem.UnitTests/Web/AdminUserStateServiceTests.cs` | Mới: 18 test cases cho StateService |
| `docs/VC22_FRONTEND_TEST.md` | Mới: hướng dẫn này |

## 10. Sau khi local pass

Khôi phục role/trạng thái của tài khoản test về trạng thái mong muốn. Bàn giao kết quả local trước khi commit/push và chạy CI/CD. Kiểm tra production thực hiện sau khi cả API và Web triển khai cùng bản VC22; dùng tài khoản test riêng trên production.
