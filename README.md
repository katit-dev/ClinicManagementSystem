# Clinic Management System

Hệ thống quản lý phòng khám dịch vụ, từ đặt lịch, tiếp nhận và khám đến thanh toán và cấp thuốc. Bệnh nhân có thể xem lại bệnh án đã chốt, hóa đơn, tải tệp của mình và trao đổi với lễ tân. Quản trị viên quản lý lịch làm việc, danh mục, phân quyền, báo cáo và nhật ký truy cập bệnh án.

![Trang giới thiệu hệ thống quản lý phòng khám](docs/screenshots/Screenshot%202026-11-08%20154246.png)

**Nội dung** · [Chức năng](#1-chức-năng) · [Demo](#15-kịch-bản-demo) · [Kiến trúc](#3-kiến-trúc-và-flow) · [Cài đặt local](#6-cài-đặt-local) · [Database](#7-database) · [API](#9-api-chính) · [Kiểm thử](#10-kiểm-thử) · [Azure và CI/CD](#12-azure-và-cicd)

| Môi trường | Địa chỉ |
|---|---|
| Web production | [Mở Web](https://clinicmanagement-web-2026-aubeawcgfda8bmdx.southeastasia-01.azurewebsites.net) |
| API production | [Danh mục chuyên khoa](https://clinicmanagement-api-2026-aubbg7gbecdnchbu.southeastasia-01.azurewebsites.net/api/specialties) |
| API local | [Swagger](http://localhost:5238/) |
| Web local | [Mở Web](http://localhost:5050/) |

> Các URL production là địa chỉ triển khai của dự án; bảng này không xác nhận trạng thái chạy hiện tại. Swagger chỉ bật trong Development. API hiện chưa khai báo `/api/health`; có thể kiểm tra một endpoint công khai như `/api/specialties` để xác nhận API phản hồi. Root `/` trên production trả 404 không đồng nghĩa API đã dừng.

## 1. Chức năng

| Vai trò | Trang Web tiêu biểu | Chức năng chính |
|---|---|---|
| Bệnh nhân (`Patient`) | `/patient/booking`, `/patient/appointments`, `/patient/records`, `/patient/invoices` | Đăng ký, đặt/đổi/hủy lịch, nhận thông báo, xem bệnh án đã chốt và đơn thuốc, tải tệp đính kèm, xem hóa đơn, chat hỗ trợ |
| Lễ tân (`Receptionist`) | `/reception`, `/reception/queue`, `/reception/patients`, `/reception/support` | Tra cứu/tạo hồ sơ, đặt lịch tại quầy, check-in, xử lý lịch bị ảnh hưởng, thu/hoàn tiền, in hóa đơn PDF và trả lời chat |
| Bác sĩ (`Doctor`) | `/doctor/queue`, `/doctor/medical-records`, `/doctor/schedule` | Khám theo hàng chờ, lưu nháp/chốt bệnh án, chỉ định và ghi kết quả dịch vụ, đính kèm tệp, kê đơn, xem ca làm và lịch nghỉ |
| Dược sĩ (`Pharmacist`) | `/pharmacy`, `/pharmacy/inventory`, `/pharmacy/receipt` | Xử lý đơn, cấp theo lô, báo thiếu thuốc, nhập/điều chỉnh kho, xem lịch sử và cảnh báo tồn thấp/gần hết hạn |
| Quản trị viên (`Admin`) | `/admin`, `/admin/support` | Quản lý chuyên khoa, lịch làm/nghỉ, dịch vụ, tài khoản/vai trò, báo cáo, audit và điều phối yêu cầu hỗ trợ |

### 1.1. Các trang Admin

Tất cả trang dưới đây yêu cầu đăng nhập bằng tài khoản có role `Admin`. Menu và thẻ chức năng nằm trong layout `HomePageMaster` và trang chủ Admin.

| Module | Trang Web | Chức năng đã triển khai |
|---|---|---|
| Trang chủ | `/admin`, `/admin/dashboard` | Điều hướng đến các module quản trị |
| Chuyên khoa | `/admin/specialties` | Tìm kiếm, lọc, phân trang, thêm/sửa, bật/ngừng hoạt động |
| VC20 — Lịch làm việc & nghỉ phép | `/admin/schedules` | Khai báo ca theo thứ, giờ nghỉ, thời lượng slot và giai đoạn hiệu lực; đăng ký/xóa khoảng nghỉ; xác nhận các lịch hẹn bị ảnh hưởng |
| VC21 — Danh mục dịch vụ | `/admin/services` | Tìm kiếm, lọc theo chuyên khoa/trạng thái, phân trang, thêm/sửa mã, tên, giá, thời lượng và ngừng sử dụng |
| VC22 — Người dùng & phân quyền | `/admin/users` | Tìm/lọc tài khoản, xem chi tiết, gán nhiều vai trò, reset mật khẩu, khóa/mở khóa |
| VC23 — Báo cáo doanh thu & lượt khám | `/admin/reports` | Lọc khoảng ngày/bác sĩ; tổng hợp lượt khám, doanh thu, công nợ, không đến; xuất Excel `.xlsx` |
| VC24 — Nhật ký truy cập bệnh án | `/admin/audit-logs` | Lọc đối tượng, mã đối tượng, người dùng, ngày; phân trang theo lượt truy cập; xem đầy đủ các log gốc trong một lượt |
| Hỗ trợ trực tuyến | `/admin/support` | Xem thông tin yêu cầu và nhật ký xử lý, giao/chuyển lễ tân, trả về hàng chờ hoặc đóng yêu cầu |

### 1.2. Quy tắc nghiệp vụ Admin

**Chuyên khoa và dịch vụ**

- Mã chuyên khoa/dịch vụ được chuẩn hóa thành chữ hoa và kiểm tra trùng. Chuyên khoa có bác sĩ hoặc dịch vụ đang hoạt động không thể ngừng hoạt động ngay.
- Dịch vụ có chuyên khoa tùy chọn, giá không âm (tối đa 2 chữ số thập phân) và thời lượng là số phút nguyên lớn hơn 0. Mã của dịch vụ đã ngừng sử dụng vẫn không được tái dùng.
- Ngừng sử dụng dịch vụ là đổi trạng thái, không xóa bản ghi. Sửa danh mục không viết lại mô tả/đơn giá snapshot trên hóa đơn hoặc chỉ định cũ.

**Lịch làm việc và nghỉ phép — VC20**

- Mỗi ca áp dụng cho một bác sĩ và một thứ trong tuần (`dayOfWeek`: 0 = Chủ nhật, 1 = Thứ 2, …, 6 = Thứ 7). `slotMinutes` từ 5 đến 120; ngày kết thúc hiệu lực có thể để trống.
- Không cho phép các ca trùng giờ trong cùng giai đoạn hiệu lực. Khi sửa với ngày bắt đầu hiệu lực mới, hệ thống có thể tạo phiên bản ca mới và chốt ngày kết thúc phiên bản cũ.
- `doctorId = null` trong lịch nghỉ nghĩa là **toàn phòng khám nghỉ**. Slot đặt khám loại trừ giờ nghỉ giữa ca, khoảng nghỉ của bác sĩ/phòng khám, lịch hẹn đã chiếm chỗ và giờ đã qua.
- Nếu thay đổi ca hoặc đăng ký nghỉ ảnh hưởng lịch hẹn, API trả `409` kèm `requiresConfirmation` và `affectedAppointments`. Admin xem danh sách rồi xác nhận bằng `confirmAffectedAppointments = true`.
- Sau xác nhận, **lịch hẹn đã đặt vẫn giữ nguyên**, không tự hủy/đổi giờ. Hệ thống tạo thông báo; lễ tân xử lý tại `/reception/affected-appointments` bằng luồng đổi/hủy lịch riêng.

**Tài khoản và phân quyền — VC22**

- Các role hiện có trong code: `Admin`, `Doctor`, `Receptionist`, `Patient`, `Pharmacist`. Tên hằng số không tự tạo dữ liệu trong `auth.roles`; cần seed role tương ứng trong database.
- `roleIds` là **toàn bộ tập vai trò mong muốn**, không chỉ các vai trò thêm mới. Vai trò Doctor/Patient cần hồ sơ bác sĩ/bệnh nhân đã liên kết.
- Không được tự khóa tài khoản, tự gỡ role Admin hoặc làm mất tài khoản Admin hoạt động cuối cùng.
- Đổi vai trò, reset mật khẩu hoặc khóa/mở khóa thu hồi refresh token và token reset mật khẩu. JWT có `clinic_access_stamp`; API đối chiếu trạng thái tài khoản và quyền hiện tại để từ chối token cũ. Tài khoản bị tác động cần đăng nhập lại.
- Module này quản lý các tài khoản đã tồn tại; chưa có chức năng tạo tài khoản nhân viên/hồ sơ bác sĩ từ trang Admin Users.

**Báo cáo — VC23**

| Chỉ số | Cách tính |
|---|---|
| Lượt khám | Lịch hẹn `Completed`, theo thời gian bắt đầu lịch hẹn trong kỳ |
| Tỷ lệ không đến | `NoShow / (Completed + NoShow) × 100`; mẫu số bằng 0 trả 0; không tính lịch hủy/chưa kết thúc |
| Doanh thu | Tiền đã thu trừ tiền hoàn trong kỳ, theo `payments`; có thể âm nếu kỳ chỉ có hoàn tiền |
| Công nợ | Phần chưa thu của hóa đơn phát hành trong kỳ, tính thanh toán và trạng thái hủy đến cuối kỳ |
| Phân tích doanh thu | Theo bác sĩ và nhóm khám/dịch vụ/thuốc/khoản khác; phân bổ tiền thực thu theo các thành phần hóa đơn |

`from` và `to` bắt buộc, tính cả ngày kết thúc, tối đa 366 ngày. Báo cáo hiện lọc theo timestamp đang lưu trong database, không tự quy đổi toàn bộ dữ liệu lịch sử về GMT+7. Khi đối chiếu local/Azure, cần kiểm tra thời gian lưu thực tế.

**Nhật ký truy cập — VC24**

- Ghi người thao tác, hành động, đối tượng, thời gian, IP và kết quả của các endpoint y tế được khai báo trong `MedicalAuditEndpoint`. Middleware/result filter không thay thế kiểm tra role và quyền sở hữu dữ liệu.
- Một request đọc 4 bệnh án lưu 4 log gốc nhưng Web hiển thị **một lượt — 4 bệnh án**. Chi tiết liệt kê từng bệnh án và mã log; các request khác nhau vẫn là lượt riêng. Log cũ không có metadata gộp được giữ thành từng dòng riêng.
- Giao diện nghiệp vụ hiển thị thời gian, người dùng, thao tác, đối tượng, kết quả và IP. Phương thức HTTP, mã yêu cầu, mã HTTP và JSON kỹ thuật vẫn có ở backend nhưng không hiển thị trên giao diện.
- Log mới lưu UTC và hiển thị GMT+7. Log cũ giữ nguyên giờ lưu, có nhãn chưa xác định múi giờ; bộ lọc ngày quy đổi GMT+7 sang UTC nên cần bỏ lọc ngày khi tra cứu log cũ gần ranh giới ngày.
- Nhật ký chỉ có API đọc. `SaveChanges` từ chối sửa/xóa `AuditLog` được EF theo dõi; cơ chế này không chặn DBA hoặc truy vấn raw SQL/bulk update.
- Tệp bệnh án tải qua API được kiểm tra quyền. Đường trực tiếp `/uploads/medical-records/...` bị chặn `404` và ghi log; không dùng URL tệp tĩnh để bỏ qua API.

### 1.3. Luồng khám và thanh toán

~~~text
Bệnh nhân đặt lịch online hoặc lễ tân đặt tại quầy
  → Lễ tân check-in, cấp số thứ tự
  → Bác sĩ mở bệnh án nháp, khám, ghi kết quả và kê thuốc nếu cần
  → Bác sĩ chốt bệnh án, hệ thống hoàn tất lượt khám và lập hóa đơn
  → Lễ tân ghi nhận thanh toán, dược sĩ xử lý đơn có thuốc
  → Bệnh nhân xem lại bệnh án, tệp đính kèm và hóa đơn
~~~

- **Bệnh án:** Bác sĩ xem bệnh án mình phụ trách trong **Bệnh án của tôi**. Bệnh án đã chốt chỉ xem. Dịch vụ chưa hoàn tất và chưa hủy sẽ chặn thao tác chốt.
- **Hóa đơn:** Tại `/reception`, mở chi tiết lượt khám có hóa đơn để dùng **Hóa đơn / Thu tiền** hoặc **Xem hóa đơn**. Nút mở `/reception/invoices/{invoiceId}`. Mã hóa đơn lấy từ lịch hẹn, tách biệt với mã lịch hẹn và mã bệnh nhân. Thu tiền cập nhật trạng thái chưa thu, thu một phần hoặc đã thu đủ. Hệ thống ghi nhận giao dịch do lễ tân nhập.
- **Kho thuốc:** Khi cấp thuốc, hệ thống ưu tiên lô còn hạn có ngày hết hạn gần nhất (FEFO). Dược sĩ có thể báo thiếu nếu không đủ hàng và kiểm tra giao dịch nhập/xuất trong lịch sử kho.
- **Tệp bệnh án:** Bác sĩ phụ trách tải lên khi bệnh án còn nháp. Bệnh nhân có quyền tải tệp qua API. SQL lưu khóa tệp, còn nội dung nằm trong thư mục riêng ở local hoặc Azure Blob container private. Xem [cấu hình và kiểm thử tệp đính kèm](docs/attachment-storage.md).

### 1.4. Hỗ trợ trực tuyến

Bệnh nhân gửi yêu cầu trong khung chat, nhận một tin xác nhận tự động cho mỗi yêu cầu mới và xem lại lịch sử. Lễ tân tiếp nhận, trả lời và kết thúc yêu cầu tại `/reception/support`. Admin điều phối tại `/admin/support`, với quyền xem thông tin yêu cầu và nhật ký xử lý. Nội dung tin nhắn chỉ dành cho các bên có quyền truy cập hội thoại. Cấu hình mặc định hiển thị hotline `0389479884` và giờ hành chính hằng ngày, gồm Thứ Bảy và Chủ Nhật.

Tin nhắn lưu vào SQL qua REST API. SignalR Hub `/hubs/support-chat` báo cho các phiên liên quan tải dữ liệu mới sau khi lưu. Module có cấu hình bật/tắt riêng và dùng trực tiếp Hub trên API trong mô hình triển khai một API instance. Xem [cài đặt và kiểm thử chat](docs/SUPPORT_CHAT_SETUP.md).

### 1.5. Kịch bản demo

1. Chuẩn bị tài khoản thử cho từng vai trò, ca làm có slot trống, dịch vụ và thuốc còn tồn. Dùng dữ liệu giả và phiên trình duyệt riêng khi cần mở nhiều tài khoản cùng lúc.
2. Dùng Patient đặt lịch trong ngày. Lễ tân check-in; bác sĩ mở lượt khám từ hàng chờ, nhập bệnh án, hoàn tất dịch vụ nếu đã chỉ định và chốt.
3. Lễ tân mở hóa đơn từ lượt khám, ghi nhận thanh toán. Dược sĩ mở đơn và cấp thuốc; bệnh nhân xem lại hồ sơ và hóa đơn.
4. Nếu đã bật chat, mở hai phiên Patient và Receptionist để thử gửi yêu cầu, tiếp nhận và trả lời. Dùng Admin kiểm tra báo cáo hoặc nhật ký của lượt vừa thao tác.

![Bệnh nhân chọn ngày và khung giờ khám](docs/screenshots/Screenshot%202026-11-08%20154722.png)

![Bàn tiếp đón và check-in lịch hẹn](docs/screenshots/Screenshot%202026-11-08%20155041.png)

<details>
<summary>Xem thêm giao diện</summary>

![Lịch làm việc và nghỉ phép của bác sĩ](docs/screenshots/Screenshot%202026-11-09%20001041.png)

![Kho thuốc và cảnh báo theo lô](docs/screenshots/Screenshot%202026-11-08%20154955.png)

![Báo cáo doanh thu và lượt khám](docs/screenshots/Screenshot%202026-11-08%20154805.png)

![Khung chat hỗ trợ trên cổng bệnh nhân](docs/screenshots/Screenshot%202026-11-08%20154649.png)

![Màn hình đăng nhập](docs/screenshots/Screenshot%202026-11-08%20154408.png)

</details>

## 2. Công nghệ

| Thành phần | Công nghệ |
|---|---|
| API | ASP.NET Core 10, C# 14, REST Controllers |
| Frontend | Blazor Server, Razor Components, Server-side SignalR |
| Data access | Entity Framework Core 10, SQL Server |
| Authentication | JWT Bearer, refresh token, role-based authorization, BCrypt, account access stamp |
| Chat và tệp đính kèm | ASP.NET Core SignalR Hub, Azure Blob Storage hoặc thư mục private local |
| API documentation | Swashbuckle / OpenAPI |
| Logging | Serilog, Application Insights trên Azure |
| PDF và email | QuestPDF, MailKit |
| Excel | Xuất `.xlsx` bằng `ClinicReportExcelWriter` |
| Test | xUnit, Moq, MockQueryable, WebApplicationFactory, Testcontainers |
| Hosting | Azure App Service Linux B1, GitHub Actions |
| Container | Dockerfile cho API |

Phiên bản package cụ thể và project references nằm trong các file `.csproj` của từng project.

## 3. Kiến trúc và flow

~~~text
Browser
   │
   ▼
ClinicManagement.Web (Blazor Server)
   │  HttpClient / JWT
   ▼
ClinicManagementSystem.Api (ASP.NET Core)
   │
   ▼
Application services
   │
   ▼
Infrastructure / EF Core / Repositories
   │
   ▼
SQL Server
~~~

### Luồng backend

~~~text
HTTP request → Middleware → Controller → Application service
             → Repository / UnitOfWork → EF Core → SQL Server
             → HTTP response
~~~

JWT được kiểm tra bởi authentication middleware và `AccountAccessValidationService`. Authorization kiểm tra role trên từng controller hoặc action. Endpoint `/api/admin/...` yêu cầu role Admin; chỉ ẩn menu/nút ở Web không bảo vệ được API.

Với endpoint y tế được theo dõi, `MedicalAuditMiddleware` ghi các lần bị từ chối/lỗi và `MedicalAuditResultFilter` lưu audit trước khi trả JSON/tệp. `MedicalAuditWriter` dùng scope riêng để không vô tình lưu các entity nghiệp vụ còn pending.

### Luồng frontend

~~~text
Razor page/component → State service → HttpClient service
                     → API → State update → Blazor re-render
~~~

Web lưu access token và refresh token trong `localStorage`. Cấu hình mặc định của API cấp access token 60 phút và refresh token 7 ngày kể từ khi cấp. `AuthorizedApiService` gắn access token vào header `Authorization: Bearer`. Khi API trả `401`, service thử refresh; nếu thành công, Web lưu cặp token mới và gửi lại request một lần. Refresh token cũ bị thu hồi và cơ sở dữ liệu lưu hash của refresh token. Quên mật khẩu dùng OTP riêng, mặc định 5 phút, gửi qua SMTP.

Web là **Blazor Server**: `HttpClient` chạy ở server Web để gọi API; trình duyệt kết nối với Web qua SignalR. `ApiSettings:BaseUrl` quyết định API được gọi, không phụ thuộc máy người dùng có đang chạy API local hay không.

### Cấu trúc một module Admin

~~~text
Pages/Admin/*.razor
    → Services/Admin*StateService.cs
    → AuthorizedApiService
    → Api/Controllers/Admin*Controller.cs
    → Application/Services/Admin*Service.cs
    → Infrastructure/Repositories + UnitOfWork
    → SQL Server
~~~

DTO/validation nằm ở Application. Razor quản lý form và hiển thị state; CSS riêng nằm trong `wwwroot/css`, JS hỗ trợ focus/tải file nằm trong `wwwroot/js` và được link trong `Pages/_Host.cshtml`. State service xử lý gọi API, lỗi và tải lại dữ liệu.

## 4. Cấu trúc thư mục

~~~text
ClinicManagementSystem/
├── src/
│   ├── ClinicManagement.Web/                   # Blazor Server frontend
│   │   ├── Pages/Admin/                       # Razor cho các module Admin
│   │   ├── Services/                          # State service và HTTP client
│   │   └── wwwroot/css/, wwwroot/js/           # CSS/JS từng trang
│   ├── ClinicManagementSystem.Api/             # ASP.NET Core REST API
│   │   ├── Controllers/Admin*Controller.cs    # Endpoint Admin
│   │   └── Middleware/                        # Theo dõi truy cập y tế VC24
│   ├── ClinicManagementSystem.Application/     # DTO, service, business rules
│   └── ClinicManagementSystem.Infrastructure/  # EF Core, DbContext, repository
├── database/                                   # Script schema và seed
├── tests/
│   ├── ClinicManagementSystem.UnitTests/
│   └── ClinicManagementSystem.IntegrationTests/
├── docs/                                       # Hướng dẫn test backend/frontend từng VC
├── .github/workflows/                          # CI/CD GitHub Actions
├── Dockerfile
└── ClinicManagementSystem.slnx
~~~

## 5. Yêu cầu môi trường

- .NET SDK 10.0
- SQL Server 2019 trở lên hoặc SQL Server Developer/Express
- SSMS hoặc công cụ tương đương để chạy script SQL
- IDE hỗ trợ SDK .NET 10 hoặc VS Code và .NET CLI
- Git
- Docker Desktop nếu chạy integration test dùng Testcontainers

~~~powershell
dotnet --version
~~~

## 6. Cài đặt local

~~~powershell
git clone https://github.com/katit-dev/ClinicManagementSystem.git
cd ClinicManagementSystem
dotnet restore .\ClinicManagementSystem.slnx
dotnet build .\ClinicManagementSystem.slnx -c Release
~~~

### Cấu hình API

Gộp các cấu hình sau vào `src/ClinicManagementSystem.Api/appsettings.Development.json` của máy bạn; không ghi đè các cấu hình cần thiết đã có:

~~~json
{
  "ConnectionStrings": {
    "ClinicDb": "Server=localhost;Database=ClinicManagementSystem;Trusted_Connection=True;TrustServerCertificate=True"
  },
  "Jwt": {
    "Key": "local-development-key-at-least-32-characters",
    "Issuer": "ClinicManagementSystem",
    "Audience": "ClinicManagementSystem.Web"
  },
  "Email": {
    "SmtpHost": "smtp.gmail.com",
    "SmtpPort": 587,
    "Username": "your-smtp-user",
    "SenderEmail": "your-sender@example.com",
    "Password": "your-smtp-password"
  },
  "FileStorage": {
    "Provider": "Local",
    "LocalRootPath": ""
  }
}
~~~

Dùng giá trị riêng cho môi trường local và giữ secret ngoài Git. Có thể cấp qua `ConnectionStrings__ClinicDb`, `Jwt__Key` và `Email__Password`. Nếu `LocalRootPath` để trống, trên Windows tệp được lưu dưới `%LOCALAPPDATA%\ClinicManagementSystem\medical-records` của tài khoản chạy API. Xem [cấu hình thư mục riêng](docs/attachment-storage.md#local-trước-khi-cấu-hình-azure).

### Cấu hình Web

File local: src/ClinicManagement.Web/appsettings.Development.json

~~~json
{
  "ApiSettings": {
    "BaseUrl": "http://localhost:5238"
  }
}
~~~

Khi deploy Azure, đặt Application Setting:

~~~text
ApiSettings__BaseUrl=https://clinicmanagement-api-2026-aubbg7gbecdnchbu.southeastasia-01.azurewebsites.net
~~~

Web dùng API local khi cấu hình local trỏ vào localhost; trên Azure phải trỏ vào API production. Các module Admin không cần thêm URL riêng hoặc appsettings riêng cho từng VC. Nếu thêm một cấu hình mới cho chức năng, khai báo riêng giá trị Development/Production tương ứng.

| Nơi chạy | Web → API | API → Database |
|---|---|---|
| Local | `ApiSettings:BaseUrl = http://localhost:5238` | `ConnectionStrings:ClinicDb` trỏ vào SQL Server local |
| Azure | `ApiSettings__BaseUrl` là hostname API production | `ConnectionStrings__ClinicDb` trỏ vào database production |

CORS hiện cho phép origin `http://localhost:5050`. Các lời gọi `HttpClient` từ Blazor Server không chịu kiểm tra CORS của trình duyệt; nếu bổ sung JavaScript gọi API trực tiếp thì cần cấu hình origin Web tương ứng. CORS không thay thế JWT/authorization.

## 7. Database

Database dùng cách tiếp cận **Database First**. Với database local mới, đọc các điều kiện đầu script và chạy phần schema theo thứ tự:

1. [`001_InitDatabase.sql`](database/001_InitDatabase.sql)
2. [`002_CreateTables.sql`](database/002_CreateTables.sql)
3. [`003_UpdateDatabaseV1_29Tables.sql`](database/003_UpdateDatabaseV1_29Tables.sql)
4. [`005_CreatePasswordResetTokens.sql`](database/005_CreatePasswordResetTokens.sql)
5. Nếu dùng chat, chạy [`007_AddSupportChat.sql`](database/007_AddSupportChat.sql) trên đúng database rồi đặt `SupportChat__Enabled=true` cho API. Cấu hình chung mặc định tắt. Bước này tạo schema `support` cho hội thoại, tin nhắn và nhật ký xử lý. Xem [quyền và thứ tự bật chat](docs/SUPPORT_CHAT_SETUP.md#2-bật-trên-local).

Sau khi có schema, chọn từng phần phù hợp trong [`004_SeedInitialData.sql`](database/004_SeedInitialData.sql) để tạo role, danh mục và dữ liệu thử. Đọc điều kiện đầu mỗi phần. Một số đoạn cần tài khoản bác sĩ đã có để lấy password hash; với database mới, hãy chuẩn bị các tài khoản thử và liên kết hồ sơ Doctor/Patient tương ứng trước khi chạy phần seed phụ thuộc.

database/003_UpdateDatabaseV1.sql là bản cập nhật cũ; không chạy đồng thời hai file 003 trên cùng database.

### Tài khoản Admin để test local

Script có sẵn: [database/006_SeedLocalAdmin.sql](database/006_SeedLocalAdmin.sql).

1. Trong SSMS chọn SQL Server/database **local**, bảo đảm có `auth.users`, `auth.roles`, `auth.user_roles` và role `Admin`.
2. Chuẩn bị password hash BCrypt work factor 12 cho mật khẩu test bạn biết; thay `PASTE_BCRYPT_HASH_HERE` trong bản script chạy local. Không đưa mật khẩu/hash thật vào commit.
3. Chỉnh username/email nếu cần. Giá trị mẫu trong file là `admin.local` và `admin.local@example.com`.
4. Chạy script, kiểm tra dòng kết quả có role Admin, rồi đăng nhập Web bằng mật khẩu tương ứng.

`006_SeedLocalAdmin.sql` chỉ dùng cho local và chủ động chặn Azure SQL. Production cần một tài khoản Admin riêng theo quy trình quản lý tài khoản của môi trường đó. Không chạy lại schema/seed local trên database production hoặc database dùng chung.

Các module Admin dùng các bảng sẵn có như `auth.users/roles/user_roles/audit_logs`, `scheduling.doctor_schedules/doctor_time_off`, `billing.services/invoices/payments/invoice_items` và dữ liệu khám. Deploy các VC này không tự chạy script SQL, không tạo lại schema và không tự seed Admin.

## 8. Chạy ứng dụng

Terminal API:

~~~powershell
dotnet run --project .\src\ClinicManagementSystem.Api\ClinicManagementSystem.Api.csproj --launch-profile http
~~~

Terminal Web:

~~~powershell
dotnet run --project .\src\ClinicManagement.Web\ClinicManagement.Web.csproj --launch-profile http
~~~

Mở Web tại http://localhost:5050. API chạy tại http://localhost:5238 hoặc HTTPS URL được in trong terminal.

Swagger được bật trong Development. API dùng route prefix rỗng nên trang tài liệu thường nằm ở root khi chạy local. Production có thể trả 404 ở root hoặc Swagger theo điều kiện môi trường trong Program.cs.

Sau khi đăng nhập Admin, mở [trang Admin local](http://localhost:5050/admin). Chuyển qua các trang trong mục 1.1 để kiểm tra từng module. Khi đổi role/reset mật khẩu/khóa tài khoản, dùng phiên đăng nhập mới thay cho token cũ.

## 9. API chính

### API nghiệp vụ tiêu biểu

| Method | Route | Mục đích / quyền |
|---|---|---|
| POST | `/api/auth/login`, `/api/auth/register`, `/api/auth/refresh-token` | Đăng nhập, đăng ký, refresh token |
| POST | `/api/auth/forgot-password`, `/api/auth/reset-password` | Yêu cầu OTP và đặt lại mật khẩu |
| GET | `/api/specialties`, `/api/doctors`, `/api/doctors/{id}/available-slots` | Danh mục và slot công khai dùng cho đặt khám |
| POST | `/api/appointments` | Patient đặt lịch |
| GET | `/api/appointments/my` | Patient xem lịch của mình |
| PATCH | `/api/appointments/{id}/cancel`, `/api/appointments/{id}/reschedule` | Patient hủy/đổi lịch |
| GET/POST | `/api/patients` | Receptionist tra cứu/tạo hồ sơ bệnh nhân |
| GET | `/api/doctors/me/queue` | Doctor xem hàng đợi |
| GET | `/api/doctors/me/medical-records`, `/api/doctors/me/schedule`, `/api/doctors/me/time-off` | Doctor xem bệnh án phụ trách, ca làm và lịch nghỉ |
| GET | `/api/patients/me/records` | Patient xem lịch sử khám của mình |
| GET/PUT | `/api/medical-records/{id}` | Đọc/cập nhật theo role và quyền của từng action |
| POST | `/api/medical-records/{id}/finalize` | Doctor hoàn tất bệnh án |
| PUT | `/api/medical-records/{id}/prescription` | Doctor lưu đơn thuốc |
| POST | `/api/medical-records/{id}/attachments` | Doctor phụ trách tải tệp lên bệnh án nháp |
| GET | `/api/attachments/{id}/download` | Patient sở hữu dữ liệu tải tệp |
| GET | `/api/invoices/{id}` | Receptionist xem hóa đơn |
| POST | `/api/invoices/{id}/payments`, `/api/invoices/{id}/refunds` | Receptionist ghi thanh toán/hoàn tiền |
| GET | `/api/invoices/{id}/pdf`, `/api/patients/me/invoices` | Receptionist xuất PDF; Patient xem hóa đơn của mình |
| GET | `/api/queue` | Receptionist xem hàng đợi |
| GET | `/api/medicines/inventory` | Pharmacist xem tồn kho |
| GET | `/api/pharmacy/prescriptions` | Pharmacist xem danh sách đơn cần xử lý |
| POST | `/api/prescriptions/{id}/dispense`, `/api/prescriptions/{id}/report-shortage` | Pharmacist cấp thuốc hoặc báo thiếu |
| GET | `/api/support/settings` | Đọc trạng thái bật/tắt và thông tin hỗ trợ |
| GET | `/api/notifications` | Doctor/Receptionist/Patient xem thông báo của mình |

Swagger local và các controller mô tả request, response và quyền của từng action. Các endpoint chat theo vai trò và luồng realtime có trong [hướng dẫn chat](docs/SUPPORT_CHAT_SETUP.md#3-api).

### API Admin

Tất cả endpoint trong các bảng sau cần JWT của tài khoản **Admin đang hoạt động**. Những action quản lý tài khoản, báo cáo và audit còn kiểm tra mã người dùng hợp lệ trong token.

| Module | Method | Endpoint | Chức năng |
|---|---|---|---|
| Chuyên khoa | GET / POST | `/api/admin/specialties` | Danh sách có lọc/phân trang; tạo chuyên khoa |
| Chuyên khoa | PUT | `/api/admin/specialties/{id}` | Sửa thông tin |
| Chuyên khoa | PATCH | `/api/admin/specialties/{id}/status` | Bật/ngừng hoạt động |
| VC20 | GET | `/api/admin/scheduling/doctors` | Bác sĩ cho bộ chọn/lọc |
| VC20 | GET / POST | `/api/admin/doctor-schedules` | Danh sách; tạo ca làm việc |
| VC20 | PUT | `/api/admin/doctor-schedules/{id}` | Sửa/tạo phiên bản ca |
| VC20 | GET / POST | `/api/admin/time-off` | Danh sách; đăng ký nghỉ |
| VC20 | DELETE | `/api/admin/time-off/{id}` | Xóa khoảng nghỉ, giữ lịch hẹn đã đặt |
| VC21 | GET / POST | `/api/admin/services` | Danh sách có lọc/phân trang; tạo dịch vụ |
| VC21 | GET / PUT | `/api/admin/services/{id}` | Chi tiết; sửa dịch vụ |
| VC21 | PATCH | `/api/admin/services/{id}/deactivate` | Ngừng sử dụng |
| VC22 | GET | `/api/admin/users` | Lọc keyword, role, trạng thái và phân trang |
| VC22 | GET | `/api/admin/users/{id}` | Chi tiết tài khoản |
| VC22 | GET | `/api/admin/users/roles` | Danh sách role thực tế trong database |
| VC22 | POST | `/api/admin/users/{id}/roles` | Thay tập vai trò bằng `roleIds` |
| VC22 | POST | `/api/admin/users/{id}/reset-password` | Reset với `newPassword`, `confirmPassword` |
| VC22 | PATCH | `/api/admin/users/{id}/lock` | Khóa/mở khóa bằng `isActive`, `reason` |
| VC23 | GET | `/api/admin/reports?from=&to=&doctorId=` | Báo cáo; `doctorId` tùy chọn |
| VC23 | GET | `/api/admin/reports/export?from=&to=&doctorId=` | Xuất file `.xlsx` theo bộ lọc |
| VC24 | GET | `/api/admin/audit-logs/accesses` | Danh sách gộp lượt truy cập cho Web |
| VC24 | GET | `/api/admin/audit-logs/accesses/{id}` | Chi tiết lượt và các log gốc; ID lấy từ danh sách gộp |
| VC24 | GET | `/api/admin/audit-logs` | Danh sách log gốc |
| VC24 | GET | `/api/admin/audit-logs/{id}` | Chi tiết một log gốc |
| VC24 | GET | `/api/admin/audit-logs/users` | Tìm/phân trang người dùng đã có log |

Audit hỗ trợ `entity`, `entityId`, `userId`, `from`, `to`, `page`, `pageSize`. Các trường ngày dùng `yyyy-MM-dd`. Không có endpoint sửa/xóa audit.

Luồng nối tiếp VC20 dành cho **Receptionist**:

| Method | Endpoint | Chức năng |
|---|---|---|
| GET | `/api/reception/appointments/affected` | Lọc lịch hẹn cần xử lý sau thay đổi ca/nghỉ |
| PATCH | `/api/reception/appointments/{id}/reschedule` | Đổi sang slot hợp lệ |
| PATCH | `/api/reception/appointments/{id}/cancel` | Hủy theo yêu cầu nghiệp vụ |

### Response và payload mẫu

Các API Admin trả JSON dùng `HttpResponseData<T>`; HTTP status khớp `statusCode`. Ví dụ cấu trúc response danh sách rỗng:

~~~json
{
  "statusCode": 200,
  "message": "Danh sách nhật ký truy cập.",
  "content": {
    "items": [],
    "page": 1,
    "pageSize": 20,
    "totalItems": 0,
    "totalPages": 0
  }
}
~~~

`message` phụ thuộc endpoint. Validation tự động của `[ApiController]` có thể trả `ProblemDetails`; lỗi authentication/authorization có thể không có JSON envelope. Export báo cáo và tải tệp trả file thay vì envelope thành công.

Tạo dịch vụ (`POST /api/admin/services`):

~~~json
{
  "code": "DV_TEST_01",
  "name": "Dịch vụ kiểm thử local",
  "description": "Dữ liệu dùng để kiểm thử",
  "specialtyId": null,
  "price": 150000,
  "durationMinutes": 30
}
~~~

Gán vai trò (`POST /api/admin/users/{id}/roles`):

~~~json
{ "roleIds": [1, 3] }
~~~

Các ID trên chỉ minh họa; lấy ID thật từ `GET /api/admin/users/roles`, không giả định Admin luôn có ID 1. Với bác sĩ, dùng `/api/admin/scheduling/doctors` để chọn ID trước khi tạo ca.

Các status thường gặp: `200/201` thành công; `400` dữ liệu không hợp lệ; `401` token/phiên không hợp lệ; `403` không có role phù hợp; `404` không tìm thấy; `409` trùng dữ liệu/xung đột nghiệp vụ hoặc cần xác nhận lịch bị ảnh hưởng; `500` lỗi hệ thống; `503` không thể ghi audit trước khi trả dữ liệu y tế.

## 10. Kiểm thử

~~~powershell
dotnet build .\ClinicManagementSystem.slnx -c Release
dotnet test .\tests\ClinicManagementSystem.UnitTests\ClinicManagementSystem.UnitTests.csproj -c Release --no-build
~~~

Unit test gồm service, validation, truy vấn, state service và Razor rendering. Các test HTTP dùng `WebApplicationFactory` kiểm tra routing/JWT/role; các integration test database dùng SQL Server container qua Testcontainers và cần Docker engine đang chạy.

Chạy toàn bộ integration test khi có Docker:

~~~powershell
dotnet test .\tests\ClinicManagementSystem.IntegrationTests\ClinicManagementSystem.IntegrationTests.csproj -c Release --no-build
~~~

Nếu chỉ chạy các test HTTP authorization và pipeline audit không cần container:

~~~powershell
dotnet test .\tests\ClinicManagementSystem.IntegrationTests\ClinicManagementSystem.IntegrationTests.csproj -c Release --no-build --filter "FullyQualifiedName~AuthorizationTests|FullyQualifiedName~MedicalAuditPipelineTests"
~~~

### Hướng dẫn kiểm thử từng module

| Module | Backend | Frontend/local |
|---|---|---|
| VC20 — Lịch/nghỉ phép | [VC20_BACKEND_TEST](docs/VC20_BACKEND_TEST.md) | [VC20_FRONTEND_TEST](docs/VC20_FRONTEND_TEST.md) |
| Xử lý lịch bị ảnh hưởng | [Reception backend](docs/RECEPTION_AFFECTED_BACKEND_TEST.md) | [Reception frontend](docs/RECEPTION_AFFECTED_FRONTEND_TEST.md) |
| VC21 — Dịch vụ | [VC21_BACKEND_TEST](docs/VC21_BACKEND_TEST.md) | [VC21_FRONTEND_TEST](docs/VC21_FRONTEND_TEST.md) |
| VC22 — Tài khoản | [VC22_BACKEND_TEST](docs/VC22_BACKEND_TEST.md) | [VC22_FRONTEND_TEST](docs/VC22_FRONTEND_TEST.md) |
| VC23 — Báo cáo | [VC23_BACKEND_TEST](docs/VC23_BACKEND_TEST.md) | [VC23_FRONTEND_TEST](docs/VC23_FRONTEND_TEST.md) |
| VC24 — Audit | [Hướng dẫn API và ghi log](docs/VC24_TEST_GUIDE.md) | [Hướng dẫn Web và giới hạn](docs/VC24_TEST_GUIDE.md) |
| Bệnh án bác sĩ | [Kiểm tra API](docs/DOCTOR_MEDICAL_RECORDS_TEST.md#kiểm-tra-api) | [Kiểm thử Bệnh án của tôi](docs/DOCTOR_MEDICAL_RECORDS_TEST.md) |
| Lịch của bác sĩ | [Kiểm tra API](docs/DOCTOR_SCHEDULE_TEST.md#api) | [Kiểm tra thủ công](docs/DOCTOR_SCHEDULE_TEST.md#kiểm-tra-thủ-công-ở-local) |
| Tệp bệnh án | [Upload, download và quyền truy cập](docs/attachment-storage.md#test-backend-local) | [Cấu hình Blob trên Azure](docs/attachment-storage.md#azure-trước-khi-deploy-production) |
| Hỗ trợ trực tuyến | [Kiểm thử tự động](docs/SUPPORT_CHAT_SETUP.md#5-kiểm-thử-tự-động) | [Kịch bản nhiều vai trò và thiết bị](docs/SUPPORT_CHAT_SETUP.md#4-kịch-bản-kiểm-tra-thủ-công) |

### Kiểm tra nhanh Admin local

1. Chạy API/Web với database local; đăng nhập Admin và mở `/admin`.
2. Chuyên khoa/dịch vụ: thêm dữ liệu test, sửa, lọc; kiểm tra mã trùng và trạng thái ngừng hoạt động.
3. Lịch/nghỉ phép: tạo ca đúng thứ/ngày hiệu lực; kiểm tra slot bằng tài khoản Patient; đăng ký nghỉ và xử lý lịch bị ảnh hưởng bằng tài khoản Receptionist.
4. Người dùng: dùng tài khoản test riêng để gán role/reset/khóa/mở khóa; xác nhận phiên cũ bị từ chối, không thử tự khóa Admin đang thao tác.
5. Báo cáo: chọn kỳ có dữ liệu, đối chiếu lượt `Completed`/`NoShow`, các khoản thu/hoàn và file Excel.
6. Audit: Patient mở lịch sử có nhiều bệnh án; Admin thấy một lượt chứa các bệnh án đó, mở chi tiết thấy mã log gốc. Thử tải tệp qua API và kiểm tra log thành công/bị chặn theo hướng dẫn VC24.

Sau khi backend và frontend local đạt yêu cầu, deploy API/Web rồi lặp lại các ca kiểm tra phù hợp trên production bằng tài khoản/dữ liệu test riêng. Không coi trạng thái workflow xanh là bằng chứng đã kiểm thử toàn bộ chức năng production.

## 11. Publish và Docker

~~~powershell
dotnet publish .\src\ClinicManagementSystem.Api\ClinicManagementSystem.Api.csproj -c Release -o .\publish\api
dotnet publish .\src\ClinicManagement.Web\ClinicManagement.Web.csproj -c Release -o .\publish\web
docker build -t clinicmanagement-api .
docker run --rm -p 5238:8080 clinicmanagement-api
~~~

Container cần được cung cấp connection string, JWT key và biến môi trường bằng secret manager hoặc tham số runtime.

## 12. Azure và CI/CD

Kiến trúc triển khai production của project gồm hai Azure App Service dùng cùng App Service Plan B1, Azure SQL Database và Azure Blob Storage:

- `clinicmanagement-api-2026` chạy ASP.NET Core API.
- `clinicmanagement-web-2026` chạy Blazor Server. Đặt `ApiSettings__BaseUrl` trỏ tới API production.
- Trên API, đặt `ConnectionStrings__ClinicDb`, `Jwt__Key`, `Jwt__Issuer`, `Jwt__Audience` và `Email__...` trong **Settings → Environment variables**.
- Với tệp bệnh án, đặt `FileStorage__Provider=AzureBlob`, `FileStorage__AzureBlob__ServiceUri` và `FileStorage__AzureBlob__ContainerName`. Managed Identity của API cần quyền dữ liệu trên container private. Xem [thiết lập Blob production](docs/attachment-storage.md#azure-trước-khi-deploy-production).
- Với chat, chạy script `007` trên database đích và deploy API/Web tương thích trước khi đặt `SupportChat__Enabled=true` trên API. Xem [deploy và kiểm thử SignalR](docs/SUPPORT_CHAT_SETUP.md#6-deploy-azure-một-instance).

Các workflow hiện có:

| Workflow | Trigger | Công việc |
|---|---|---|
| [CI](.github/workflows/ci.yml) | Push `develop`/`main`; pull request vào `main` | Restore, build Release, chạy unit và integration test |
| [CD - API](.github/workflows/deploy-api.yml) | Push `develop` có thay đổi API/Application/Infrastructure, solution hoặc workflow API; chạy thủ công | Restore, publish API, loại `appsettings.Development.json`, deploy App Service API |
| [CD - Web](.github/workflows/deploy-web.yml) | Push `develop` có thay đổi Web/Application/Infrastructure, solution hoặc workflow Web; chạy thủ công | Restore, publish Web, loại `appsettings.Development.json`, deploy App Service Web |

CI và hai workflow CD **chạy độc lập**; cấu hình hiện tại không chờ CI thành công rồi mới deploy. Thay đổi thư viện Application/Infrastructure có thể kích hoạt cả API lẫn Web. Chỉ sửa README/docs thì CI vẫn chạy khi push, còn hai CD không tự chạy theo bộ lọc path hiện tại. Merge vào `main` kích hoạt CI nhưng không tự kích hoạt hai CD này.

Khi phát hành từ `main`, kiểm tra CI rồi vào **GitHub Actions → CD - API / CD - Web → Run workflow**, chọn `main` cho bản cần triển khai. Đối chiếu commit và kiểm tra chức năng trên website sau khi deploy.

Publish profile phải lưu trong GitHub Actions Secrets:

~~~text
AZURE_API_PUBLISH_PROFILE
AZURE_WEB_PUBLISH_PROFILE
~~~

Không đưa publish profile, JWT key, SMTP password hoặc SQL password vào commit, README, issue hay log.

Kiểm tra production sau khi cả hai App Service đã cập nhật: đăng nhập, kiểm tra quyền theo vai trò và thử luồng chính với dữ liệu test. Nếu dùng chat hoặc Blob, thử thêm gửi/nhận tin giữa hai phiên và tải tệp đúng quyền. Chuẩn bị schema và tài khoản production riêng trước deploy vì workflow chỉ build và phát hành ứng dụng.

### Sao lưu production về local và dừng hosting

1. Chọn thời điểm chốt dữ liệu. Dừng Web/API, tác vụ ghi dữ liệu và deploy trong lúc xuất bản sao. Dùng SSMS **Tasks → Export Data-tier Application** để lưu Azure SQL thành `.bacpac`. Tải cả container tệp và giữ cấu trúc theo khóa blob. Kiểm tra thêm tệp cũ lưu riêng trên App Service nếu còn dùng. [Export Azure SQL](https://learn.microsoft.com/en-us/azure/azure-sql/database/database-export?view=azuresql) · [Tải Blob bằng AzCopy](https://learn.microsoft.com/en-us/azure/storage/common/storage-use-azcopy-blobs-download)
2. Import `.bacpac` vào một database local mới. Trỏ `ConnectionStrings:ClinicDb` vào bản import và đặt `FileStorage:Provider=Local`. Với blob `medical-records/{recordId}/{name}`, đặt tệp tại `{LocalRootPath}/{recordId}/{name}`. Đối chiếu số dòng các bảng chính, chạy `DBCC CHECKDB` trên bản local, so khớp `clinical.attachments.file_url` với tệp đã tải và thử mở bệnh án/tải tệp qua API local. Giữ bản lưu trữ riêng trên ổ mã hóa, ngoài Git. [Import SQL Server](https://learn.microsoft.com/en-us/sql/relational-databases/data-tier-applications/import-a-bacpac-file-to-create-a-new-user-database)
3. Sau khi xác nhận phục hồi thành công, xóa App Service Plan B1 hoặc hạ về Free F1 nếu khả dụng. Plan B1 vẫn tính phí khi app đã Stop hoặc plan trống. SQL và Blob tính phí riêng. Nếu ngừng toàn bộ project trên Azure, rà tài nguyên dùng chung trước khi xóa và kiểm tra chi phí theo từng tài nguyên trong **Cost Management → Cost analysis**. [Cách App Service tính phí](https://learn.microsoft.com/azure/app-service/overview-manage-costs)

## 13. Quy trình phát triển chức năng

~~~text
DTO / validation → Repository / UnitOfWork → Application service
→ Controller API → Backend test → Web state service
→ Razor page / CSS / JS → Frontend local test → Commit / CI / deploy
→ Production test
~~~

1. Xác định entity, DTO và quyền truy cập.
2. Thêm repository/truy vấn cần thiết ở Infrastructure.
3. Thêm business rule ở Application service.
4. Tạo endpoint và authorization ở API.
5. Chạy test backend và kiểm tra từng endpoint trên local trước khi nối frontend.
6. Cập nhật StateService, Razor, CSS riêng và link trong `_Host.cshtml`; thêm JS hỗ trợ khi cần.
7. Bổ sung/chạy test phù hợp và kiểm tra frontend local bằng các role liên quan.
8. Commit/push theo quy trình dự án; kiểm tra CI và cả hai CD, sau đó test production.

Giữ cấu trúc module hiện có, không truy vấn database trực tiếp từ Razor/controller và không đổi schema Database First ngoài phạm vi đã thống nhất.

## 14. Xử lý lỗi thường gặp

### API dừng khi khởi động

Kiểm tra Jwt__Key hoặc Jwt:Key. API dừng nếu JWT key không được cấu hình.

### Web báo không kết nối hệ thống

Kiểm tra ApiSettings:BaseUrl. Local dùng http://localhost:5238; Azure dùng hostname API HTTPS.

### HTTP 401

Token hết hạn, sai JWT key/issuer/audience, thiếu hoặc không còn khớp access stamp, tài khoản bị khóa/xóa hoặc quyền đã thay đổi. Đăng nhập lại bằng tài khoản còn hoạt động. Phiên tạo trước khi triển khai access stamp cũng cần đăng nhập lại.

### HTTP 403 trên trang/API Admin

Tài khoản có phiên hợp lệ nhưng không có role Admin. Kiểm tra dữ liệu `auth.user_roles` và dùng token mới; chỉ có hằng số `UserRoleConstant.Admin` trong code chưa đủ để cấp quyền cho tài khoản.

### HTTP 404 ở root hoặc Swagger trên Azure

Thử `/api/specialties` hoặc một endpoint đã khai báo để xác nhận API phản hồi. Project hiện không có `/api/health`; root/Swagger chỉ bật khi Development.

### CORS khi gọi API từ local

CORS hiện cho phép origin `http://localhost:5050`. Với JavaScript gọi API từ trình duyệt, đổi origin cần cập nhật policy. Với Blazor Server gọi bằng HttpClient, kiểm tra BaseUrl/kết nối server và JWT trước.

### Có lịch làm việc nhưng Patient không thấy slot

Kiểm tra đúng bác sĩ, đúng thứ, ngày nằm trong giai đoạn hiệu lực, ca đang hoạt động, giờ chưa qua và slot chưa bị đặt. Lịch nghỉ `doctorId = null` áp dụng cho toàn phòng khám nên cũng loại slot của bác sĩ đã chọn.

### HTTP 409 khi sửa ca/đăng ký nghỉ

Đọc message để phân biệt ca trùng giờ với cảnh báo lịch hẹn bị ảnh hưởng. Khi `requiresConfirmation = true`, kiểm tra `affectedAppointments` rồi xác nhận trên UI. Sau khi lưu, lễ tân xử lý lịch hẹn cũ; không mặc định các lịch đó đã tự hủy.

### Một lượt xem lịch sử có nhiều log

Mỗi bệnh án được trả về có một log gốc. Trang Admin gộp các log cùng request thành một lượt, còn chi tiết vẫn liệt kê đủ bệnh án/log. Log nghiệp vụ cũ thiếu metadata không tự gộp; phương thức/mã yêu cầu vẫn lưu ở backend phục vụ truy vết.

### HTTP 503 khi truy cập dữ liệu y tế

Kiểm tra kết nối database và việc lưu `auth.audit_logs`. VC24 không cung cấp JSON/tệp y tế khi chưa lưu được audit. Với thao tác ghi, dữ liệu nghiệp vụ có thể đã commit trước lỗi audit; kiểm tra trạng thái trước khi gửi lại để tránh lặp thao tác.

### Gửi mã OTP báo lỗi

Xem log API tại dòng `Forgot password request failed.` để phân biệt lỗi ghi `auth.password_reset_tokens` với lỗi gửi SMTP. Trên Azure, xem **Monitoring → Log stream** của API. Nếu Gmail báo lỗi xác thực `534`/`535`, kiểm tra tài khoản gửi và App Password trong cấu hình `Email__...` của API.

### SSMS báo IP không có quyền truy cập Azure SQL

Nếu gặp lỗi `40615` khi kết nối SSMS, mở logical SQL server trong Azure Portal, vào **Networking** và thêm đúng IP client hiện trong lỗi vào firewall. Kết nối từ API có thể đi qua địa chỉ khác. [Cấu hình firewall Azure SQL](https://learn.microsoft.com/en-us/azure/azure-sql/database/firewall-create-server-level-portal-quickstart?view=azuresql)

## 15. Bảo mật

- Không commit appsettings.Development.json chứa secret thật.
- Không commit JWT key, SMTP password, SQL password hoặc publish profile.
- Dùng Azure App Service Configuration hoặc GitHub Secrets cho production.
- Không dùng dữ liệu bệnh nhân thật trong seed, test hoặc screenshot.
- Bật HTTPS trên Azure và chỉ mở các endpoint cần thiết.
- Phân quyền ở API; ẩn nút trên UI không thay thế authorization backend.
- Không ghi mật khẩu, password hash, token hoặc nội dung bệnh án vào metadata truy cập VC24. Audit lưu các mã đối tượng và thông tin truy vết cần thiết.
- IP trong audit là địa chỉ API quan sát từ kết nối; với Blazor Server/proxy có thể là server Web/proxy, không mặc định là IP trình duyệt.
- Quyền Admin tra cứu audit không tự cấp quyền đọc/tải mọi bệnh án hoặc tệp; API y tế vẫn kiểm tra role và ownership theo từng chức năng.

## 16. Giấy phép

Dự án phục vụ mục đích học tập và triển khai nội bộ. Bổ sung thông tin license nếu repository được phát hành công khai.
