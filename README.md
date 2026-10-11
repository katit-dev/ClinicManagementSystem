# Clinic Management System

Hệ thống quản lý phòng khám gồm cổng giao diện cho nhân viên và bệnh nhân, REST API, cơ sở dữ liệu SQL Server và quy trình CI/CD trên Azure App Service.

| Môi trường | Địa chỉ |
|---|---|
| Web production | https://clinicmanagement-web-2026-aubeawcgfda8bmdx.southeastasia-01.azurewebsites.net |
| API production | https://clinicmanagement-api-2026-aubbg7gbecdnchbu.southeastasia-01.azurewebsites.net |
| API local | http://localhost:5238 |
| Web local | http://localhost:5050 |

> API production chỉ hiển thị Swagger khi chạy ở Development. Vì vậy truy cập / hoặc /swagger/index.html trên Azure có thể trả về 404; hãy dùng /api/health để kiểm tra service.

## 1. Chức năng

| Nhóm | Chức năng chính |
|---|---|
| Bệnh nhân | Đăng ký, đăng nhập, đặt lịch, xem lịch sử khám, hồ sơ bệnh án, đơn thuốc, hóa đơn và thông báo |
| Bác sĩ | Xem lịch làm việc, hàng đợi, khám bệnh, cập nhật hồ sơ, kê đơn và theo dõi lịch hẹn |
| Lễ tân | Tiếp nhận bệnh nhân, quản lý lịch hẹn, hàng đợi và thanh toán |
| Dược sĩ | Quản lý thuốc, xử lý đơn và cập nhật trạng thái cấp phát |
| Hệ thống | JWT authentication, phân quyền theo role, refresh token, email reset password, file đính kèm, log và báo cáo PDF |

## 2. Công nghệ

| Thành phần | Công nghệ |
|---|---|
| API | ASP.NET Core 10, C# 14, REST Controllers |
| Frontend | Blazor Server, Razor Components, Server-side SignalR |
| Data access | Entity Framework Core 10, SQL Server |
| Authentication | JWT Bearer, refresh token, role-based authorization |
| API documentation | Swashbuckle / OpenAPI |
| Logging | Serilog, Application Insights trên Azure |
| PDF và email | QuestPDF, MailKit |
| Test | xUnit, Moq, MockQueryable, WebApplicationFactory, Testcontainers |
| Hosting | Azure App Service Linux B1, GitHub Actions |
| Container | Dockerfile cho API |

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

JWT được kiểm tra bởi authentication middleware. Authorization kiểm tra role trên từng controller hoặc action.

### Luồng frontend

~~~text
Razor page/component → State service → HttpClient service
                     → API → State update → Blazor re-render
~~~

AuthorizedApiService tự gắn Bearer token. Khi API trả về 401, service thử refresh token rồi gửi lại request một lần.

## 4. Cấu trúc thư mục

~~~text
ClinicManagementSystem/
├── src/
│   ├── ClinicManagement.Web/                   # Blazor Server frontend
│   ├── ClinicManagementSystem.Api/             # ASP.NET Core REST API
│   ├── ClinicManagementSystem.Application/     # DTO, service, business rules
│   └── ClinicManagementSystem.Infrastructure/  # EF Core, DbContext, repository
├── database/                                   # Script schema và seed
├── tests/
│   ├── ClinicManagementSystem.UnitTests/
│   └── ClinicManagementSystem.IntegrationTests/
├── docs/
├── .github/workflows/                          # CI/CD GitHub Actions
├── Dockerfile
└── ClinicManagementSystem.slnx
~~~

## 5. Yêu cầu môi trường

- .NET SDK 10.0
- SQL Server 2019 trở lên hoặc SQL Server Developer/Express
- Visual Studio 2022 hoặc VS Code
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

Tạo hoặc cập nhật file src/ClinicManagementSystem.Api/appsettings.Development.json:

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

Web dùng API local khi chạy local và API production khi chạy trên Azure.

## 7. Database

Chạy script theo thứ tự:

1. database/001_InitDatabase.sql
2. database/002_CreateTables.sql
3. database/003_UpdateDatabaseV1_29Tables.sql
4. database/004_SeedInitialData.sql
5. database/005_CreatePasswordResetTokens.sql

database/003_UpdateDatabaseV1.sql là bản cập nhật cũ; không chạy đồng thời hai file 003 trên cùng database.

## 8. Chạy ứng dụng

Terminal API:

~~~powershell
dotnet run --project .\src\ClinicManagementSystem.Api\ClinicManagementSystem.Api.csproj
~~~

Terminal Web:

~~~powershell
dotnet run --project .\src\ClinicManagement.Web\ClinicManagement.Web.csproj
~~~

Mở Web tại http://localhost:5050. API chạy tại http://localhost:5238 hoặc HTTPS URL được in trong terminal.

Swagger được bật trong Development. API dùng route prefix rỗng nên trang tài liệu thường nằm ở root khi chạy local. Production có thể trả 404 ở root hoặc Swagger theo điều kiện môi trường trong Program.cs.

## 9. API chính

| Method | Route | Mục đích |
|---|---|---|
| POST | /api/auth/login | Đăng nhập và nhận token |
| POST | /api/auth/register | Tạo tài khoản |
| POST | /api/auth/refresh-token | Làm mới access token |
| GET/POST | /api/appointments | Danh sách và tạo lịch hẹn |
| GET/PUT | /api/doctors | Thông tin và lịch làm việc bác sĩ |
| GET/POST | /api/patients | Tra cứu và cập nhật bệnh nhân |
| GET/POST | /api/medical-records | Hồ sơ bệnh án |
| GET/POST | /api/prescriptions | Đơn thuốc |
| GET/POST | /api/invoices | Hóa đơn |
| GET/POST | /api/queue | Hàng đợi tiếp nhận |
| GET/POST | /api/medicines | Thuốc và tồn kho |
| GET/POST | /api/notifications | Thông báo |
| GET | /api/health | Kiểm tra trạng thái API |

Phần lớn endpoint nghiệp vụ yêu cầu JWT và role phù hợp.

## 10. Kiểm thử

~~~powershell
dotnet test .\ClinicManagementSystem.slnx -c Release
dotnet test .\tests\ClinicManagementSystem.UnitTests\ClinicManagementSystem.UnitTests.csproj
~~~

Integration test dùng SQL Server container qua Testcontainers. Cần Docker Desktop đang chạy.

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

GitHub Actions thực hiện restore, build Release, test, publish và deploy vào App Service tương ứng. Push branch develop dùng cho kiểm tra và deploy theo workflow; pull request vào main chạy CI trước khi merge.

Publish profile phải lưu trong GitHub Actions Secrets:

~~~text
AZURE_API_PUBLISH_PROFILE
AZURE_WEB_PUBLISH_PROFILE
~~~

Không đưa publish profile, JWT key, SMTP password hoặc SQL password vào commit, README, issue hay log.

## 13. Quy trình phát triển chức năng

~~~text
DTO / validation → Application service → Repository / UnitOfWork
→ Controller API → Unit / integration test → Web state service
→ Razor page/component → CI → Azure deployment
~~~

1. Xác định entity, DTO và quyền truy cập.
2. Thêm business rule ở Application service.
3. Thêm repository hoặc truy vấn ở Infrastructure.
4. Tạo endpoint và authorization ở API.
5. Cập nhật state service và page của Web.
6. Bổ sung test.
7. Chạy build/test local rồi push branch.

## 14. Xử lý lỗi thường gặp

### API dừng khi khởi động

Kiểm tra Jwt__Key hoặc Jwt:Key. API dừng nếu JWT key không được cấu hình.

### Web báo không kết nối hệ thống

Kiểm tra ApiSettings:BaseUrl. Local dùng http://localhost:5238; Azure dùng hostname API HTTPS.

### HTTP 401

Token hết hạn, sai JWT key hoặc tài khoản không có role yêu cầu. Đăng nhập lại và kiểm tra Jwt:Issuer, Jwt:Audience.

### HTTP 404 ở root hoặc Swagger trên Azure

Kiểm tra /api/health để xác nhận service đang chạy. 404 ở root không đồng nghĩa API đã dừng.

### CORS khi gọi API từ local

CORS hiện cho phép Web local tại http://localhost:5050. Nếu đổi port hoặc domain, cập nhật policy trong API và deploy lại.

## 15. Bảo mật

- Không commit appsettings.Development.json chứa secret thật.
- Không commit JWT key, SMTP password, SQL password hoặc publish profile.
- Dùng Azure App Service Configuration hoặc GitHub Secrets cho production.
- Không dùng dữ liệu bệnh nhân thật trong seed, test hoặc screenshot.
- Bật HTTPS trên Azure và chỉ mở các endpoint cần thiết.
- Phân quyền ở API; ẩn nút trên UI không thay thế authorization backend.

## 16. Giấy phép

Dự án phục vụ mục đích học tập và triển khai nội bộ. Bổ sung thông tin license nếu repository được phát hành công khai.

