# Clinic Management System

Hệ thống quản lý phòng khám gồm cổng giao diện cho quản trị viên, nhân viên và bệnh nhân, REST API, cơ sở dữ liệu SQL Server và quy trình CI/CD trên Azure App Service. Nhóm Admin đã có quản lý chuyên khoa, lịch làm việc/nghỉ phép, dịch vụ, tài khoản/phân quyền, báo cáo và nhật ký truy cập bệnh án.

**Nội dung:** [Chức năng Admin](#11-các-trang-admin) · [Kiến trúc](#3-kiến-trúc-và-flow) · [Setup local](#6-cài-đặt-local) · [Database và tài khoản Admin](#7-database) · [API](#9-api-chính) · [Kiểm thử](#10-kiểm-thử) · [CI/CD](#12-azure-và-cicd)

| Môi trường | Địa chỉ |
|---|---|
| Web production | [Mở Web](https://clinicmanagement-web-2026-aubeawcgfda8bmdx.southeastasia-01.azurewebsites.net) |
| API production | [Danh mục chuyên khoa](https://clinicmanagement-api-2026-aubbg7gbecdnchbu.southeastasia-01.azurewebsites.net/api/specialties) |
| API local | [Swagger](http://localhost:5238/) |
| Web local | [Mở Web](http://localhost:5050/) |

> Các URL production là địa chỉ triển khai của dự án; bảng này không xác nhận trạng thái chạy hiện tại. Swagger chỉ bật trong Development. API hiện chưa khai báo `/api/health`; có thể kiểm tra một endpoint công khai như `/api/specialties` để xác nhận API phản hồi. Root `/` trên production trả 404 không đồng nghĩa API đã dừng.

## 1. Chức năng

| Nhóm | Chức năng chính |
|---|---|
| Quản trị viên | Quản lý chuyên khoa, lịch làm việc/nghỉ phép, danh mục dịch vụ, người dùng/vai trò, báo cáo doanh thu và nhật ký truy cập bệnh án |
| Bệnh nhân | Đăng ký, đăng nhập, đặt lịch, xem lịch sử khám, hồ sơ bệnh án, đơn thuốc, hóa đơn và thông báo |
| Bác sĩ | Xem lịch làm việc, hàng đợi, khám bệnh, cập nhật hồ sơ, kê đơn và theo dõi lịch hẹn |
| Lễ tân | Tiếp nhận bệnh nhân, quản lý lịch hẹn, hàng đợi và thanh toán |
| Dược sĩ | Quản lý thuốc, xử lý đơn và cập nhật trạng thái cấp phát |
| Hệ thống | JWT authentication, phân quyền theo role, refresh token, email reset password, file đính kèm, log và báo cáo PDF |

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

## 2. Công nghệ

| Thành phần | Công nghệ |
|---|---|
| API | ASP.NET Core 10, C# 14, REST Controllers |
| Frontend | Blazor Server, Razor Components, Server-side SignalR |
| Data access | Entity Framework Core 10, SQL Server |
| Authentication | JWT Bearer, refresh token, role-based authorization, BCrypt, account access stamp |
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

AuthorizedApiService tự gắn Bearer token. Khi API trả về 401, service thử refresh token rồi gửi lại request một lần.

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
    "Username": "your-smtp-user",
    "SenderEmail": "your-sender@example.com",
    "Password": "your-smtp-password"
  }
}
~~~

Không commit giá trị thật. Có thể dùng biến môi trường ConnectionStrings__ClinicDb và Jwt__Key.

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

1. database/001_InitDatabase.sql
2. database/002_CreateTables.sql
3. database/003_UpdateDatabaseV1_29Tables.sql
4. database/005_CreatePasswordResetTokens.sql

Sau khi có schema, dùng `database/004_SeedInitialData.sql` để tạo dữ liệu thử phù hợp. File seed có nhiều phần và có đoạn yêu cầu một tài khoản bác sĩ đã tồn tại để sao chép password hash; không coi đây là script bootstrap một lần chạy cho mọi database rỗng.

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
| GET | `/api/specialties`, `/api/doctors`, `/api/doctors/{id}/available-slots` | Danh mục và slot công khai dùng cho đặt khám |
| POST | `/api/appointments` | Patient đặt lịch |
| GET | `/api/appointments/my` | Patient xem lịch của mình |
| PATCH | `/api/appointments/{id}/cancel`, `/api/appointments/{id}/reschedule` | Patient hủy/đổi lịch |
| GET/POST | `/api/patients` | Receptionist tra cứu/tạo hồ sơ bệnh nhân |
| GET | `/api/doctors/me/queue` | Doctor xem hàng đợi |
| GET | `/api/patients/me/records` | Patient xem lịch sử khám của mình |
| GET/PUT | `/api/medical-records/{id}` | Đọc/cập nhật theo role và quyền của từng action |
| POST | `/api/medical-records/{id}/finalize` | Doctor hoàn tất bệnh án |
| PUT | `/api/medical-records/{id}/prescription` | Doctor lưu đơn thuốc |
| GET | `/api/attachments/{id}/download` | Patient sở hữu dữ liệu tải tệp |
| GET | `/api/invoices/{id}` | Receptionist xem hóa đơn |
| POST | `/api/invoices/{id}/payments`, `/api/invoices/{id}/refunds` | Receptionist ghi thanh toán/hoàn tiền |
| GET | `/api/queue` | Receptionist xem hàng đợi |
| GET | `/api/medicines/inventory` | Pharmacist xem tồn kho |
| GET | `/api/notifications` | Doctor/Receptionist/Patient xem thông báo của mình |

Đây là các route tiêu biểu, không phải toàn bộ API. Swagger local và các controller là nguồn đối chiếu cho request, response và quyền của từng action.

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

Production gồm hai Azure App Service dùng cùng App Service Plan B1:

- clinicmanagement-api-2026 chạy ASP.NET Core API.
- clinicmanagement-web-2026 chạy Blazor Server.
- Web đặt ApiSettings__BaseUrl trỏ tới API production.
- API đặt ConnectionStrings__ClinicDb, Jwt__Key, Jwt__Issuer, Jwt__Audience và thông tin email trong Configuration > Environment variables.

Các workflow hiện có:

| Workflow | Trigger | Công việc |
|---|---|---|
| [CI](.github/workflows/ci.yml) | Push `develop`/`main`; pull request vào `main` | Restore, build Release, chạy unit và integration test |
| [CD - API](.github/workflows/deploy-api.yml) | Push `develop` có thay đổi API/Application/Infrastructure, solution hoặc workflow API; chạy thủ công | Restore, publish API, loại `appsettings.Development.json`, deploy App Service API |
| [CD - Web](.github/workflows/deploy-web.yml) | Push `develop` có thay đổi Web/Application/Infrastructure, solution hoặc workflow Web; chạy thủ công | Restore, publish Web, loại `appsettings.Development.json`, deploy App Service Web |

CI và hai workflow CD **chạy độc lập**; cấu hình hiện tại không chờ CI thành công rồi mới deploy. Thay đổi thư viện Application/Infrastructure có thể kích hoạt cả API lẫn Web. Chỉ sửa README/docs thì CI vẫn chạy khi push, còn hai CD không tự chạy theo bộ lọc path hiện tại. Merge vào `main` kích hoạt CI nhưng không tự kích hoạt hai CD này.

Publish profile phải lưu trong GitHub Actions Secrets:

~~~text
AZURE_API_PUBLISH_PROFILE
AZURE_WEB_PUBLISH_PROFILE
~~~

Không đưa publish profile, JWT key, SMTP password hoặc SQL password vào commit, README, issue hay log.

Kiểm tra production sau khi cả hai App Service đã cập nhật: đăng nhập Admin, mở các trang quản trị, kiểm tra tài khoản/role trong database production và `ApiSettings__BaseUrl` của Web. Workflow không tự tạo Admin, không cập nhật schema và không chạy seed SQL.

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

