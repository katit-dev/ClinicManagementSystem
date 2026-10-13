# VC21 — Test frontend danh mục dịch vụ

Thứ tự: **backend → frontend → test local toàn luồng → production**. Trang quản trị: `/admin/services`.

## 1. Các file frontend đã thay đổi

| File | Thay đổi |
|---|---|
| `src/ClinicManagement.Web/Pages/Admin/Services.razor` | Trang danh mục, lọc/phân trang, thêm/sửa và xác nhận ngừng sử dụng; yêu cầu role Admin |
| `src/ClinicManagement.Web/Services/AdminServiceStateService.cs` | Gọi API có JWT, tải chuyên khoa qua tất cả các trang, quản lý trạng thái tải/lưu và lỗi |
| `src/ClinicManagement.Web/wwwroot/css/admin-services.css` | CSS riêng cho trang, form và bảng |
| `src/ClinicManagement.Web/wwwroot/js/admin-services.js` | Đưa focus và cuộn đến form/thông báo, trả focus khi hủy |
| `src/ClinicManagement.Web/Pages/_Host.cshtml` | Link CSS và JavaScript mới |
| `src/ClinicManagement.Web/Program.cs` | Đăng ký StateService scoped |
| `src/ClinicManagement.Web/Shared/HomePageMaster.razor` | Thêm menu Danh mục dịch vụ cho Admin |
| `src/ClinicManagement.Web/Pages/Admin/Index.razor` | Thêm thẻ truy cập danh mục trên dashboard Admin |
| `tests/ClinicManagementSystem.UnitTests/Web/AdminServiceStateServiceTests.cs` | 17 test cho StateService: request, phân trang chuyên khoa, lỗi, retry và chống gửi đồng thời |
| `docs/VC21_FRONTEND_TEST.md` | Hướng dẫn này |

Không cần migration hoặc seed lại database cho VC21. Backend đã triển khai ở giai đoạn trước; xem [hướng dẫn backend](VC21_BACKEND_TEST.md).

## 2. Chạy local

Chạy từ thư mục gốc dự án. Nếu API/Web cũ đang chạy, dừng bằng `Ctrl+C` trong terminal tương ứng để nạp code mới.

Terminal API:

```powershell
dotnet run --project .\src\ClinicManagementSystem.Api\ClinicManagementSystem.Api.csproj --launch-profile http
```

Terminal Web:

```powershell
dotnet run --project .\src\ClinicManagement.Web\ClinicManagement.Web.csproj --launch-profile http
```

- API: `http://localhost:5238`.
- Web: `http://localhost:5050`.
- Web Development cần `ApiSettings:BaseUrl = http://localhost:5238`. Không đổi sang API Azure khi đang test toàn luồng local.
- API phải dùng connection string database local của bạn. Không chạy các bước tạo/sửa dữ liệu bên dưới nếu API đang trỏ database production.
- Đăng nhập bằng Admin local đã có, dùng mật khẩu thật; không dùng password hash làm mật khẩu đăng nhập.
- Mở [Danh mục dịch vụ local](http://localhost:5050/admin/services), hoặc chọn menu/thẻ **Danh mục dịch vụ** trong Admin.

## 3. Test quyền truy cập và tải danh mục

1. Chưa đăng nhập, mở `/admin/services`: không được xem trang quản trị.
2. Đăng nhập Doctor/Patient/Receptionist/Pharmacist: không có quyền mở trang này. Backend vẫn kiểm tra role Admin; ẩn menu không thay thế phân quyền API.
3. Đăng nhập Admin: thấy bảng Mã, Tên dịch vụ, Chuyên khoa, Giá, Thời lượng, Đang dùng và Thao tác.
4. Trạng thái mặc định là **Tất cả**. Dịch vụ đã ngừng vẫn xuất hiện nếu chưa lọc.
5. Dữ liệu cũ thiếu mã hoặc thời lượng hiện **Chưa khai báo**, không tự gán giá trị giả.

Nếu không tải được danh mục, trang báo lỗi và có nút **Thử lại**. Nếu không tải được chuyên khoa, thêm/sửa bị khóa để tránh dùng danh mục chưa tải đủ; có nút thử tải chuyên khoa lại.

## 4. Test thêm dịch vụ

Bấm **Thêm dịch vụ**, nhập:

| Trường | Dữ liệu test |
|---|---|
| Mã | `vc21_local_ui_001` — đổi hậu tố nếu đã dùng |
| Tên | `VC21 local - Xét nghiệm kiểm thử` |
| Chuyên khoa | Chọn chuyên khoa đang hoạt động hoặc để Không thuộc chuyên khoa |
| Giá | `250000` |
| Thời lượng | `15` |
| Mô tả | `Dữ liệu kiểm thử VC21 trên local` |

1. Nhập giá/thời lượng rồi chuyển ô: giá trị phải được giữ lại.
2. Mã được trim và chuyển thành `VC21_LOCAL_UI_001` sau khi rời ô.
3. Bấm **Lưu dịch vụ**: thấy thông báo thành công, form đóng, dịch vụ mới đang sử dụng.
4. Bấm **Đặt lại** bộ lọc rồi tìm theo mã nếu không thấy bản ghi; bộ lọc trước đó có thể loại dịch vụ mới.
5. Tìm theo mã để xác nhận giá `250.000 ₫`, thời lượng `15 phút`, đúng chuyên khoa/mô tả.

Khi đang gửi, các nút bị khóa và nút lưu hiện **Đang lưu...** để tránh gửi nhiều yêu cầu cùng lúc.

## 5. Test validation và lỗi API

Mỗi case dùng form mới hoặc sửa lại dữ liệu trước khi test case kế tiếp.

| Case | Kỳ vọng |
|---|---|
| Bỏ trống mã, tên hoặc giá | Báo bắt buộc, không lưu |
| Mã có khoảng trắng ở giữa hoặc dấu/ký tự đặc biệt | Báo mã không hợp lệ |
| Mã trùng bản ghi đang dùng hoặc đã ngừng | API 409; báo lỗi và giữ dữ liệu form |
| Giá âm hoặc vượt `9999999999.99` | Không lưu |
| Giá có hơn 2 chữ số thập phân | Không lưu |
| Giá `0` | Hợp lệ, dịch vụ miễn phí |
| Thời lượng trống, `0`, âm hoặc số lẻ | Không lưu; phải là số nguyên dương |
| Chuyên khoa trống | Hợp lệ cho dịch vụ dùng chung |

Lỗi validation đưa focus đến danh sách lỗi. Lỗi API đưa focus đến thông báo lỗi. Không hiển thị thông báo thành công khi request thất bại.

## 6. Test sửa dịch vụ

1. Tìm dịch vụ test, bấm **Sửa**. Trang tải chi tiết mới nhất từ API trước khi mở form.
2. Form được cuộn vào tầm nhìn; không cần kéo lên tìm.
3. Đổi giá thành `300000`, thời lượng `20`, mô tả tùy chọn rồi lưu.
4. Danh sách phải hiện `300.000 ₫` và `20 phút`. Kiểm tra reload trang vẫn giữ dữ liệu.
5. Mã trùng dịch vụ khác phải bị từ chối; dữ liệu form không mất.
6. Với bản ghi cũ có mã/thời lượng null, form giữ trống; bổ sung giá trị hợp lệ trước khi lưu.
7. Chuyên khoa đã ngừng của bản ghi cũ vẫn hiển thị để hiểu dữ liệu hiện tại. Danh sách chọn mới chỉ cho chuyên khoa đang hoạt động; nếu backend yêu cầu đổi chuyên khoa, chọn chuyên khoa hợp lệ hoặc bỏ trống.

## 7. Test tìm kiếm, bộ lọc và phân trang

1. Tìm theo một phần mã hoặc tên rồi bấm **Lọc**: chỉ hiện kết quả phù hợp.
2. Lọc chuyên khoa: chỉ hiện dịch vụ thuộc chuyên khoa đó.
3. Lọc trạng thái **Đang sử dụng**, **Ngừng sử dụng**, **Tất cả**: đúng trạng thái tương ứng.
4. Kết hợp từ khóa + chuyên khoa + trạng thái: phải thỏa cả ba.
5. **Đặt lại**: xóa từ khóa/chuyên khoa/trạng thái, về trang 1.
6. Có trên 10 bản ghi: bấm **Sau/Trước**, kiểm tra chỉ số trang và dữ liệu; nút bị khóa ở trang đầu/cuối.
7. Sửa nội dung bộ lọc nhưng chưa bấm Lọc: phân trang vẫn dùng bộ lọc đã áp dụng gần nhất.
8. Không có kết quả: hiện thông báo không tìm thấy dịch vụ phù hợp.

## 8. Test ngừng sử dụng và dữ liệu lịch sử

Chỉ ngừng dịch vụ test của bạn, tránh ảnh hưởng dữ liệu thực đang dùng.

1. Bấm **Ngừng sử dụng** ở dòng cuối bảng: panel xác nhận xuất hiện và được đưa vào tầm nhìn.
2. Bấm **Hủy**: trạng thái không thay đổi, focus trả về nút trên dòng đã chọn.
3. Bấm lại và **Xác nhận ngừng sử dụng**: thông báo thành công; trạng thái thành **Đã ngừng**.
4. Lọc Đang sử dụng: không còn dịch vụ này; lọc Ngừng sử dụng: thấy dịch vụ.
5. Dòng đã ngừng chỉ còn Sửa; sửa thông tin không tự bật lại. VC21 hiện chưa có nút kích hoạt lại.
6. Đăng nhập Doctor: danh mục để tạo chỉ định mới không còn dịch vụ đã ngừng. Gửi trực tiếp ID đã ngừng vào API tạo chỉ định phải bị từ chối; xem hướng dẫn backend cho request cụ thể.
7. Nếu có chỉ định/hóa đơn test trước khi sửa giá hoặc ngừng: mở lại và xác nhận dữ liệu lịch sử được giữ. Hóa đơn đã lập dùng snapshot mô tả/đơn giá; không được đổi giá theo danh mục mới.

## 9. Test lỗi kết nối và hiển thị

- Đang mở form, dừng API local rồi bấm lưu: báo không kết nối, dữ liệu form còn nguyên. Bật API lại và thử lưu.
- Dừng API rồi tải danh sách: báo lỗi, không trình bày bảng cũ như dữ liệu mới. Bật API, bấm Thử lại.
- Thu nhỏ cửa sổ: form xếp lại; bảng nhiều cột cuộn ngang trong khu vực bảng.
- Sửa, hủy, ngừng và validation tại cuối trang: focus/cuộn đúng vị trí, không phải tự kéo lên tìm thông báo.
- Sau khi cập nhật frontend, nếu CSS/JS còn cũ, dùng `Ctrl+F5`.

## 10. Automated tests

```powershell
dotnet test .\tests\ClinicManagementSystem.UnitTests\ClinicManagementSystem.UnitTests.csproj -c Release
dotnet test .\tests\ClinicManagementSystem.IntegrationTests\ClinicManagementSystem.IntegrationTests.csproj -c Release --filter "FullyQualifiedName~AuthorizationTests"
```

Đã chạy sau khi hoàn thành frontend: **361 unit tests pass**, **168 HTTP authorization tests pass**. Web Release build thành công; còn các warning có sẵn từ trước.

HTTP authorization tests dùng TestServer và mocks, không kiểm tra SQL/Azure thật. Kiểm tra trình duyệt của người triển khai dùng API giả lập trong bộ nhớ: thêm/sửa/ngừng, hủy xác nhận, lọc, phân trang, mã trùng, validation, giữ giá thập phân và focus đều được kiểm tra. Các bước dùng database local ở trên vẫn cần bạn thực hiện.

## 11. Sau khi local pass: production

1. Commit cả backend và frontend VC21 sau khi đã kiểm tra diff.
2. Push nhánh `develop`: các workflow hiện có `CD - API` và `CD - Web` sẽ triển khai theo paths tương ứng. VC21 gồm thay đổi cả hai phía nên cần kiểm tra cả hai run của commit này.
3. Đợi cả API và Web deploy thành công. Workflow deploy hiện tách riêng khỏi CI; cũng kiểm tra run CI tương ứng đã pass.
4. Trên Azure Web, `ApiSettings__BaseUrl` phải trỏ API Azure. Production không dùng `appsettings.Development.json` local; không đổi URL local sang Azure để test production.
5. Đăng nhập Admin production đã tạo, mở `/admin/services`. Kiểm tra danh sách, phân quyền, thêm/sửa một dịch vụ test có mã riêng, rồi ngừng dịch vụ test đó.
6. Kiểm tra danh mục Doctor và dữ liệu lịch sử liên quan. Không ngừng dịch vụ thực đang sử dụng chỉ để test.

Chưa commit, push hoặc triển khai production trong bước làm frontend này.
