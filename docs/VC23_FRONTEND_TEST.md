# VC23 — Test frontend báo cáo doanh thu & lượt khám

Trang: `/admin/reports`, chỉ dành cho **Admin**. Frontend dùng `AuthorizedApiService` hiện có để gọi backend bằng JWT và refresh token.

Tài liệu backend và cách đối chiếu dữ liệu: [VC23_BACKEND_TEST.md](VC23_BACKEND_TEST.md).

## 1. Chuẩn bị và chạy local

Mở hai terminal PowerShell ở thư mục gốc dự án `D:\_KhanhVy\ClinicManagementSystem`.

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
- Kiểm tra cấu hình Web Development: `ApiSettings:BaseUrl` trỏ đến `http://localhost:5238`, API dùng database local bạn đã chuẩn bị.
- Đăng nhập bằng Admin local, mở **Báo cáo doanh thu & lượt khám** trong menu hoặc thẻ ở trang Admin.
- Link trực tiếp: [http://localhost:5050/admin/reports](http://localhost:5050/admin/reports).
- Khi vừa mở trang, báo cáo tự chạy từ đầu tháng đến ngày hiện tại theo GMT+7, cho toàn phòng khám.

Không cần thay đổi schema hoặc seed riêng cho frontend. Nếu database chưa có lịch Completed/NoShow hoặc thanh toán trong kỳ, báo cáo có thể hợp lệ và toàn số 0. Chọn khoảng ngày có dữ liệu để kiểm tra số liệu.

## 2. Kiểm tra bộ lọc

| Thao tác | Kết quả mong đợi |
| --- | --- |
| Chọn khoảng ngày hợp lệ rồi Chạy báo cáo | Kết quả ghi rõ khoảng ngày đã chạy, tính cả ngày kết thúc |
| Chọn Tất cả bác sĩ | Tổng hợp toàn phòng khám; có cả bác sĩ đã ngừng hoạt động và hóa đơn chưa gắn bác sĩ nếu có dữ liệu |
| Chọn một bác sĩ | Bảng và chỉ số chỉ thuộc bác sĩ đã chọn |
| Chọn bác sĩ đã ngừng hoạt động | Vẫn xem được dữ liệu lịch sử; lựa chọn có nhãn trạng thái |
| Đổi ngày/bác sĩ nhưng chưa chạy lại | Có nhắc Bộ lọc đã thay đổi; nút Excel bị vô hiệu hóa; tiêu đề kết quả vẫn ghi bộ lọc đã chạy trước đó |
| Chạy lại sau khi đổi bộ lọc | Kết quả cập nhật, bỏ thông báo thay đổi, nút Excel bật lại |
| Bấm Tháng này | Đặt đầu tháng–hôm nay, bỏ chọn bác sĩ và chạy lại báo cáo toàn phòng khám |
| Xóa một ngày, hoặc Đến ngày trước Từ ngày | Có thông báo validation và focus vào lỗi; không gửi request báo cáo mới |
| Chọn 366 ngày tính cả hai đầu | Hợp lệ |
| Chọn 367 ngày trở lên | Bị chặn và báo lỗi khoảng ngày |

Ví dụ khoảng 366 ngày: `01/01/2026`–`01/01/2027`. Khoảng `01/01/2026`–`02/01/2027` vượt giới hạn.

Sau khi chạy thành công, focus đến **Kết quả báo cáo**. Khi request thất bại, focus đến thông báo lỗi và bỏ kết quả cũ. Thao tác không cần cuộn lên để tìm thông báo.

## 3. Đối chiếu số liệu với backend

Trong Swagger, dùng JWT Admin local và gọi cùng bộ lọc:

```http
GET /api/admin/reports?from=2026-10-01&to=2026-10-13
GET /api/admin/reports?from=2026-10-01&to=2026-10-13&doctorId=<ID_BAC_SI>
```

So sánh `content` trong JSON với trang:

| Trên Web | Trường JSON / quy tắc |
| --- | --- |
| Lượt khám | `totalVisits`: lịch Completed theo ngày khám |
| Doanh thu thực thu | `totalRevenue = collectedAmount - refundedAmount`, theo ngày thu/hoàn tiền |
| Công nợ trong kỳ | `outstandingDebt` của hóa đơn **phát hành trong kỳ**, tính đến hết ngày kết thúc |
| Tỷ lệ không đến | `noShowRate`, đã là số phần trăm; ví dụ `33.33` hiển thị `33,33 %` |
| Mẫu số tỷ lệ | `concludedAppointments = Completed + NoShow`; không tính Cancelled hoặc lịch chưa kết thúc |
| Cơ cấu doanh thu | `consultationRevenue`, `serviceRevenue`, `medicineRevenue`, `otherRevenue` |
| Theo bác sĩ | `doctors`; tổng cuối bảng lấy chỉ số toàn báo cáo, không cộng các tỷ lệ phần trăm |
| Chi tiết dịch vụ & thuốc | `revenueItems`: mô tả đã lưu trên hóa đơn, doanh thu tiền thực thu được phân bổ |

Ví dụ 2 Completed + 1 NoShow: lượt khám = 2, tỷ lệ không đến = 33,33 %. Không có Completed/NoShow: tỷ lệ = 0,00 %, không hiển thị NaN.

Công nợ trong kỳ **không phải toàn bộ nợ lũy kế** trước kỳ. Thanh toán trong kỳ cho hóa đơn cũ vẫn được tính vào doanh thu. Nếu kỳ chỉ có hoàn tiền, doanh thu có thể âm. Tiền hiển thị theo định dạng Việt Nam và đơn vị ₫.

Kiểm tra thêm trường hợp thu một phần, có hoàn tiền, dữ liệu bác sĩ đã ngừng hoạt động và hóa đơn chưa gắn bác sĩ theo dữ liệu local sẵn có. Không sửa dữ liệu production để tạo ca test.

## 4. Test xuất Excel

1. Chạy báo cáo thành công với một bộ lọc cụ thể.
2. Bấm **Xuất Excel**. Trong lúc xử lý, các nút bị khóa để tránh tải trùng.
3. Kiểm tra mục Downloads của trình duyệt. File đúng đuôi `.xlsx`, tên có khoảng ngày và phạm vi `all-doctors` hoặc `doctor-ID`.
4. Mở file bằng Excel hoặc LibreOffice: có 3 sheet **Tổng quan**, **Theo bác sĩ**, **Dịch vụ & thuốc**; không có thông báo file hỏng.
5. Đối chiếu bộ lọc và số liệu với Web. Backend tổng hợp lại lúc xuất; tránh phát sinh thanh toán/lịch mới giữa hai lần nếu muốn so sánh chính xác.
6. Đổi bộ lọc nhưng chưa chạy lại: nút Excel phải bị khóa. Chạy lại rồi xuất để kiểm tra bộ lọc mới.
7. Kỳ không có dữ liệu vẫn xuất được file hợp lệ với số 0 và bảng rỗng.

Thông báo “Đã gửi file Excel đến trình duyệt” xác nhận đã gửi yêu cầu tải, không xác nhận người dùng đã lưu file vào ổ đĩa. Nếu trình duyệt chặn download, kiểm tra quyền tải hoặc thực hiện thao tác trong trình duyệt local của bạn.

Web Blazor Server gọi API ở phía server rồi chuyển file qua `DotNetStreamReference`. Không đưa JWT vào URL tải, không cần mở API công khai để xuất file. Lỗi API/JSON không được lưu giả thành file Excel.

## 5. Lỗi, quyền truy cập và giao diện

- Đăng xuất rồi mở `/admin/reports`: đi qua cơ chế yêu cầu đăng nhập hiện có.
- Đăng nhập Patient/Doctor/Receptionist: không có menu báo cáo, không được vào trang Admin này; API vẫn kiểm tra quyền Admin độc lập.
- Token bị thu hồi/hết hiệu lực và refresh không thành công: luồng hiện có yêu cầu đăng nhập lại. `403`: thông báo không có quyền và xóa dữ liệu báo cáo đang hiển thị.
- Khi đã xem kết quả thành công, tắt **API local của bạn**, bấm Chạy báo cáo: phải hiện lỗi kết nối/quá thời gian chờ, bỏ số liệu cũ và nút Excel. Chạy lại API và retry: trang phục hồi.
- Nếu riêng danh sách bác sĩ tải lỗi: có thông báo và nút Thử lại; vẫn có thể xem toàn phòng khám khi API báo cáo hoạt động. Bộ chọn bác sĩ bị khóa trong lúc danh sách lỗi.
- Kiểm tra ở màn hình desktop và cửa sổ nhỏ: bộ lọc/chỉ số xếp lại; bảng nhiều cột cuộn ngang bên trong bảng.
- Dùng Tab/Enter: input có label, nút focus được, bảng cuộn có thể nhận focus và vùng lỗi/kết quả được focus sau thao tác.

## 6. Kiểm tra tự động

Từ thư mục gốc:

```powershell
dotnet build .\ClinicManagementSystem.slnx -c Release --no-restore

dotnet test .\tests\ClinicManagementSystem.UnitTests\ClinicManagementSystem.UnitTests.csproj -c Release --no-build --no-restore

dotnet test .\tests\ClinicManagementSystem.IntegrationTests\ClinicManagementSystem.IntegrationTests.csproj -c Release --no-build --no-restore --filter "FullyQualifiedName~AuthorizationTests"
```

Kết quả thực chạy khi hoàn thành frontend:

- Build Release thành công. Lần build Web đầy đủ có 7 warnings đã có từ trước ở Doctor/Reception; lần incremental cuối không có warnings mới.
- **493 unit tests pass**, gồm **35 tests frontend VC23** cho StateService và render trang Razor thật.
- **250 tests HTTP/phân quyền pass**.
- Trình duyệt với dữ liệu mẫu: kiểm tra đổi bác sĩ, chặn export khi bộ lọc thay đổi, chạy lại, focus kết quả/lỗi, validation ngày, dữ liệu rỗng, lỗi API xóa kết quả cũ và bố cục màn hình nhỏ.
- Download thực tế trong phiên trình duyệt tự động bị từ chối quyền; cần bạn xác nhận tải/mở Excel trong bước local bên trên. Tests xử lý phản hồi XLSX, metadata, lỗi và stream callback đã pass.
- Các tests SQL Server container cần Docker engine. Engine chưa hoạt động trong phiên triển khai nên chưa chạy nhóm này; không gọi kết quả trên là đã pass toàn bộ integration database.
- Chưa test với tài khoản/database local của bạn, chưa commit/push/deploy production ở bước frontend.

## 7. Các file thêm/sửa ở bước frontend

| File | Vai trò |
| --- | --- |
| [Reports.razor](../src/ClinicManagement.Web/Pages/Admin/Reports.razor) | Trang `/admin/reports`: bộ lọc, chỉ số, bảng, export, validation và focus |
| [AdminReportStateService.cs](../src/ClinicManagement.Web/Services/AdminReportStateService.cs) | Gọi API qua JWT/refresh hiện có, lưu bộ lọc đã chạy, xử lý lỗi và phản hồi XLSX |
| [admin-reports.css](../src/ClinicManagement.Web/wwwroot/css/admin-reports.css) | CSS riêng của trang |
| [admin-reports.js](../src/ClinicManagement.Web/wwwroot/js/admin-reports.js) | Focus và tải stream với MIME Excel |
| [Program.cs](../src/ClinicManagement.Web/Program.cs) | Đăng ký DI StateService |
| [_Host.cshtml](../src/ClinicManagement.Web/Pages/_Host.cshtml) | Link CSS và JavaScript |
| [HomePageMaster.razor](../src/ClinicManagement.Web/Shared/HomePageMaster.razor) | Menu báo cáo cho Admin |
| [Index.razor](../src/ClinicManagement.Web/Pages/Admin/Index.razor) | Thẻ báo cáo trên dashboard Admin |
| [AdminReportStateServiceTests.cs](../tests/ClinicManagementSystem.UnitTests/Web/AdminReportStateServiceTests.cs) | Tests bộ lọc, quyền, request, export, lỗi và chống thao tác trùng |
| [AdminReportsRenderingTests.cs](../tests/ClinicManagementSystem.UnitTests/Web/AdminReportsRenderingTests.cs) | Tests render Razor, dữ liệu rỗng/lỗi, bộ lọc thay đổi và quyền route |
| [VC23_FRONTEND_TEST.md](VC23_FRONTEND_TEST.md) | Hướng dẫn test và danh sách file |

Backend VC23 từ bước trước vẫn nằm trong working tree. Danh sách trên chỉ mô tả thay đổi frontend của bước này.

## 8. Sau khi local pass

Đối chiếu số liệu và xác nhận mở Excel thành công, sau đó chuyển sang commit/push, chờ CI + CD API/Web và test production theo cùng bộ lọc. Việc test UI bằng dữ liệu mẫu không thay thế bước này.
