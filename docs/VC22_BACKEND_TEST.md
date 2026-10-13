# VC22 — Test backend người dùng & phân quyền

Thứ tự: **backend → bạn test Swagger local → frontend → test local toàn luồng → production**.

Backend sử dụng các bảng hiện có: `auth.users`, `auth.roles`, `auth.user_roles`, `auth.refresh_tokens`, `auth.password_reset_tokens`, `auth.audit_logs`. Không cần migration hoặc đổi schema.

## 1. Khởi động và chuẩn bị tài khoản

Dừng API cũ bằng `Ctrl+C`, kiểm tra API trỏ tới database local của bạn rồi chạy tại thư mục dự án:

```powershell
dotnet run --project .\src\ClinicManagementSystem.Api\ClinicManagementSystem.Api.csproj --launch-profile http
```

Mở [Swagger local](http://localhost:5238/). Swagger UI của dự án nằm ở `/`; tài liệu JSON nằm ở `/swagger/v1/swagger.json`.

Gọi `POST /api/auth/login` bằng Admin local:

```json
{
  "username": "admin.local@clinic.local",
  "password": "<mật khẩu thật của Admin local>"
}
```

Kỳ vọng **200**. Ghi lại `content.user.id`, gọi là **ADMIN_ID**. Bấm **Authorize**, dán riêng `content.accessToken` (Swagger dùng Bearer HTTP tự thêm tiền tố).

Chuẩn bị một tài khoản local riêng để test, gọi ID của nó là **TEST_USER_ID**. Có thể dùng bệnh nhân test đã có, hoặc đăng ký mới qua `POST /api/auth/register` với email/số điện thoại chưa sử dụng. Đăng ký bệnh nhân tạo cả tài khoản, hồ sơ Patient và role Patient. Dùng mật khẩu bạn biết để kiểm tra login sau mỗi thao tác.

Không dùng tài khoản Admin đang điều khiển test làm đối tượng reset/khóa. Mỗi lần đổi token trong Swagger, bấm Logout trong Authorize rồi nhập token cần test; nhớ chuyển lại token Admin trước các thao tác quản trị.

JWT đã cấp trước VC22 không có dấu kiểm tra phiên mới. Đăng nhập lại để lấy token mới trước khi test. Refresh token cũ còn hợp lệ có thể đổi lấy token mới, nhưng các thao tác quản trị bên dưới sẽ thu hồi refresh token của tài khoản bị thay đổi.

## 2. Endpoint và quyền truy cập

| Method | Endpoint | Chức năng | Thành công |
|---|---|---|---|
| GET | `/api/admin/users` | Danh sách, tìm kiếm, lọc, phân trang | 200 |
| GET | `/api/admin/users/{id}` | Chi tiết tài khoản | 200 |
| GET | `/api/admin/users/roles` | Lấy ID/tên các role thật trong DB | 200 |
| POST | `/api/admin/users/{id}/roles` | Thay toàn bộ tập vai trò | 200 |
| POST | `/api/admin/users/{id}/reset-password` | Admin đặt mật khẩu mới | 200 |
| PATCH | `/api/admin/users/{id}/lock` | Khóa hoặc mở khóa | 200 |

Tất cả endpoint trong bảng yêu cầu **Admin**:

- Không có JWT: **401**.
- JWT hợp lệ của Doctor/Patient/Receptionist/Pharmacist không có Admin: **403**.
- JWT đã bị vô hiệu hóa do đổi quyền, reset hoặc khóa: **401**, kể cả chữ ký và hạn JWT còn hợp lệ.
- User không tồn tại hoặc đã bị xóa mềm: GET chi tiết/thao tác quản trị trả **404** khi người gọi vẫn là Admin hợp lệ.

Trong URL, thay `{id}` bằng số **TEST_USER_ID** thật; không gửi nguyên dấu ngoặc. ID 0/âm không khớp route và trả 404.

## 3. GET — danh sách và chi tiết

Gọi bằng token Admin:

```text
GET /api/admin/users
GET /api/admin/users?keyword=admin.local
GET /api/admin/users?role=Patient
GET /api/admin/users?active=true
GET /api/admin/users?active=false
GET /api/admin/users?page=1&pageSize=10
```

Kỳ vọng **200**, `content` có `items`, `page`, `pageSize`, `totalItems`, `totalPages`.

- `keyword` tìm trong username, họ tên, email và số điện thoại.
- `role` là **tên role**, ví dụ `Receptionist`, không phải ID.
- Bỏ `active`: lấy cả hoạt động và bị khóa; luôn loại tài khoản xóa mềm.
- Trang vượt dữ liệu hoặc tên role không tồn tại: `items: []`, không phải lỗi.
- Mỗi tài khoản có `id`, `username`, `fullName`, `email`, `phone`, `isActive`, `lastLoginAt`, `roles`.
- Response không có password, password hash hoặc refresh token. `lastLoginAt` có thể null nếu chưa đăng nhập; thời gian lưu theo UTC.

Gọi `GET /api/admin/users/{TEST_USER_ID}`: 200, đúng thông tin và vai trò. Lưu lại tập role ban đầu để khôi phục sau khi test.

Validation: `page=0`, `pageSize=0`, `pageSize=101`, `active=invalid` → **400**.

## 4. GET — danh mục role

Gọi `GET /api/admin/users/roles`: 200; `content` là mảng `{id, name, description}`.

Ghi lại ID thật của Patient, Receptionist, Pharmacist, Doctor, Admin nếu chúng có trong DB. Không mặc định ID role giống nhau giữa local và Azure. Endpoint chỉ đọc danh mục; không tạo role mới.

## 5. POST — gán/gỡ nhiều vai trò

Đầu tiên login tài khoản test, lưu access token và refresh token của nó. Chuyển Swagger lại token Admin.

Gọi `POST /api/admin/users/{TEST_USER_ID}/roles`:

```json
{
  "roleIds": [4, 2, 5]
}
```

**Các số 4, 2, 5 chỉ minh họa**. Thay bằng ID thật của **Patient + Receptionist + Pharmacist** lấy ở bước 4. Giữ Patient của tài khoản bệnh nhân test để tiếp tục kiểm tra luồng bệnh nhân.

Kỳ vọng:

1. **200**, response và GET chi tiết có đủ ba role, không bị trùng.
2. Login lại tài khoản test: 200, `content.user.roles` có quyền mới.
3. Dùng access token cũ của tài khoản test gọi `GET /api/admin/users`: **401**. Token mới không có Admin gọi cùng URL: **403** — phân biệt phiên bị thu hồi với thiếu quyền.
4. `POST /api/auth/refresh-token` với refresh token cũ của tài khoản test: **401**.
5. Với token mới có Receptionist, `GET /api/patients?page=1`: **200**.

Body `roleIds` là **toàn bộ tập role muốn giữ**, không phải chỉ role muốn thêm. Để gỡ Receptionist/Pharmacist, gửi riêng ID Patient; response phải chỉ còn Patient. Những lần thay đổi tập role đều yêu cầu login lại.

Test thêm:

| Trường hợp | Kỳ vọng |
|---|---|
| Gửi lại đúng tập role hiện tại, kể cả đảo thứ tự | 200; không thu hồi phiên hoặc tạo audit thay đổi mới |
| `{"roleIds": []}` với tài khoản test không phải Admin | 200; gỡ hết role, không xóa tài khoản/hồ sơ |
| Thiếu `roleIds`, null, ID 0/âm hoặc ID trùng nhau | 400 |
| ID role không tồn tại | 400; không đổi tập role hiện tại |
| Gán Doctor cho tài khoản chưa có hồ sơ Doctor | 409 |
| Gán Patient cho tài khoản chưa có hồ sơ Patient | 409 |
| Gửi tập role bỏ Admin cho chính ADMIN_ID | 409; Admin vẫn còn quyền |

Sau case `[]`, gán lại Patient bằng token Admin để tiếp tục test. Gán Doctor chỉ dùng tài khoản bác sĩ có hồ sơ sẵn. VC22 không tự tạo hồ sơ Doctor/Patient.

Backend bảo vệ ít nhất một Admin hoạt động và chặn tự gỡ Admin. Có thể test hai tài khoản Admin local riêng: Admin A gỡ quyền Admin của B khi A vẫn hoạt động; token cũ B phải 401, A vẫn quản trị được. Không thay dữ liệu SQL thủ công để ép mất Admin cuối cùng.

## 6. POST — reset mật khẩu

Login tài khoản test, lưu token của phiên hiện tại; chuyển Swagger về Admin. Gọi `POST /api/admin/users/{TEST_USER_ID}/reset-password`:

```json
{
  "newPassword": "<mat-khau-test-moi>",
  "confirmPassword": "<mat-khau-test-moi>"
}
```

Thay cả hai giá trị bằng cùng một mật khẩu mới bạn tự chọn. Kỳ vọng:

- **200**; mật khẩu được hash BCrypt trong DB, response/audit không chứa mật khẩu hoặc hash.
- Login bằng mật khẩu cũ: **401**; mật khẩu mới: **200** nếu tài khoản đang hoạt động.
- Access token đã lưu trước reset gọi endpoint có Authorize: **401**.
- Refresh token đã lưu trước reset: **401**.
- OTP reset chưa sử dụng của tài khoản cũng bị thu hồi. Nếu đã tạo OTP trước bước này thì không dùng được OTP đó để reset tiếp.
- Đây là Admin đặt mật khẩu trực tiếp; không tự gửi email hoặc trả mật khẩu tạm trong response.
- Reset một tài khoản đang khóa không tự mở khóa nó.

Mật khẩu dưới 8 ký tự, xác nhận khác nhau, quá 128 ký tự hoặc quá 72 bytes UTF-8: **400**; mật khẩu hiện tại vẫn dùng được. Giới hạn bytes khiến mật khẩu chứa nhiều ký tự Unicode có thể chạm giới hạn trước 72 ký tự.

## 7. PATCH — khóa và mở khóa

Login tài khoản test bằng mật khẩu hiện tại, lưu access/refresh token. Chuyển về token Admin, gọi `PATCH /api/admin/users/{TEST_USER_ID}/lock`:

```json
{
  "isActive": false,
  "reason": "VC22 local: kiểm tra khóa tài khoản"
}
```

Kỳ vọng **200**, `isActive: false` ở response và GET chi tiết. Token cũ của tài khoản test gọi endpoint có Authorize: **401**; refresh cũ: **401**. Login với **đúng mật khẩu hiện tại**: **403** vì bị khóa. Tài khoản xuất hiện khi lọc `active=false`.

Mở khóa bằng cùng endpoint:

```json
{
  "isActive": true,
  "reason": "VC22 local: hoàn tất kiểm tra khóa"
}
```

Kỳ vọng **200**, `isActive: true`; login mới thành công. Access/refresh token từ trước khi khóa vẫn **401**, không được sống lại khi mở khóa.

- Gửi lại cùng trạng thái hiện tại: 200, không thu hồi thêm phiên hoặc tạo audit thay đổi mới.
- Thiếu/null `isActive`, thiếu `reason`, lý do chỉ có khoảng trắng hoặc dài hơn 500 ký tự: 400.
- Admin tự khóa ADMIN_ID: 409; tài khoản và token Admin vẫn hoạt động.
- Khóa tài khoản Admin khác chỉ hợp lệ khi vẫn còn Admin hoạt động. Không xóa bản ghi tài khoản.

Body để test refresh token đã thu hồi:

```json
{
  "refreshToken": "<refresh-token-cua-tai-khoan-test-truoc-thao-tac>"
}
```

Gửi tới `POST /api/auth/refresh-token`, không gửi access token vào trường này.

## 8. Kiểm tra audit và dữ liệu local

Trong SSMS chọn đúng **database local**, có thể dùng query chỉ đọc:

```sql
SELECT TOP (30)
    id, user_id, action, entity_name, entity_id, details, occurred_at, succeeded
FROM auth.audit_logs
WHERE entity_name = 'User'
  AND action IN ('SetUserRoles', 'AdminResetPassword', 'LockUser', 'UnlockUser')
ORDER BY id DESC;
```

Kiểm tra `user_id` là Admin thực hiện, `entity_id` là tài khoản test; details thể hiện role trước/sau hoặc trạng thái/lý do. Audit reset chỉ ghi việc reset, không ghi password/hash. Role mới có `assigned_by` = Admin thực hiện và `assigned_at`; role giữ nguyên không bị đổi thông tin gán.

Các thay đổi tài khoản, role, thu hồi token và audit nằm trong cùng transaction. Login/refresh/reset OTP dùng cùng khóa transaction để tránh phát phiên từ dữ liệu quyền/mật khẩu cũ khi Admin đang thay đổi. HTTP tests chưa thay thế việc xác minh hành vi transaction trên SQL Server thật.

Nếu gặp `Invalid object name 'auth.roles'`, kiểm tra database đang chọn/connection string; không tạo bảng mới để né lỗi. API dùng schema hiện có của dự án.

## 9. Tests tự động và phạm vi xác minh

Các lệnh đã chạy thành công khi bàn giao backend:

```powershell
dotnet build .\ClinicManagementSystem.slnx -c Release
dotnet test .\tests\ClinicManagementSystem.UnitTests\ClinicManagementSystem.UnitTests.csproj -c Release --no-build --no-restore
dotnet test .\tests\ClinicManagementSystem.IntegrationTests\ClinicManagementSystem.IntegrationTests.csproj -c Release --no-build --no-restore --filter "FullyQualifiedName~AuthorizationTests"
```

Kết quả: **398 unit tests pass; 220 HTTP authorization tests pass**. HTTP tests VC22 dùng JWT và bộ kiểm tra phiên thật, với repository tài khoản trong bộ nhớ; nghiệp vụ quản trị được kiểm tra bằng unit tests.

Tests integration với SQL Server container chưa chạy vì Docker engine chưa hoạt động. Chưa thao tác database local/Azure trong quá trình triển khai này. Bước Swagger ở trên xác minh backend với database local trước khi làm frontend.

## 10. File đã thay đổi

Đường dẫn dưới đây tính từ thư mục gốc repository.

**API:**

- `src/ClinicManagementSystem.Api/Controllers/AdminUserController.cs` (mới).
- `src/ClinicManagementSystem.Api/Program.cs` (DI và kiểm tra phiên JWT).

**Application:**

- `src/ClinicManagementSystem.Application/DTOs/User/AdminUserDTO.cs` (mới, có cả AdminRoleDTO).
- `src/ClinicManagementSystem.Application/DTOs/User/AdminUserQueryDTO.cs` (mới).
- `src/ClinicManagementSystem.Application/DTOs/User/SetUserRolesRequestDTO.cs` (mới).
- `src/ClinicManagementSystem.Application/DTOs/User/AdminResetPasswordRequestDTO.cs` (mới).
- `src/ClinicManagementSystem.Application/DTOs/User/SetUserLockRequestDTO.cs` (mới).
- `src/ClinicManagementSystem.Application/Helpers/AccountAccessStamp.cs` (mới).
- `src/ClinicManagementSystem.Application/Services/AccountAccessValidationService.cs` (mới).
- `src/ClinicManagementSystem.Application/Services/AdminUserManagementService.cs` (mới).
- `src/ClinicManagementSystem.Application/Services/JwtAuthService.cs`.
- `src/ClinicManagementSystem.Application/Services/UserService.cs`.

**Infrastructure repositories:**

- `src/ClinicManagementSystem.Infrastructure/Repositories/UserRepository.cs`.
- `src/ClinicManagementSystem.Infrastructure/Repositories/RoleRepository.cs`.
- `src/ClinicManagementSystem.Infrastructure/Repositories/UserRoleRepository.cs`.
- `src/ClinicManagementSystem.Infrastructure/Repositories/RefreshTokenRepository.cs`.
- `src/ClinicManagementSystem.Infrastructure/Repositories/PasswordResetTokenRepository.cs`.

**Tests và hướng dẫn:**

- `tests/ClinicManagementSystem.UnitTests/Application/User/AdminUserManagementServiceTests.cs` (mới).
- `tests/ClinicManagementSystem.UnitTests/Application/User/AccountAccessValidationServiceTests.cs` (mới).
- `tests/ClinicManagementSystem.UnitTests/Application/User/UserServiceTests.cs`.
- `tests/ClinicManagementSystem.IntegrationTests/User/AdminUserAuthorizationTests.cs` (mới).
- `tests/ClinicManagementSystem.IntegrationTests/TestAccountAccessValidationService.cs` (mới, chỉ dùng trong fixtures test cũ).
- `tests/ClinicManagementSystem.IntegrationTests/CustomWebApplicationFactory.cs` (giữ fixture nghiệp vụ SQL cũ dùng JWT giả; VC22 dùng host riêng với validator thật).
- `tests/ClinicManagementSystem.IntegrationTests/Service/AdminServiceAuthorizationTests.cs`.
- `tests/ClinicManagementSystem.IntegrationTests/Specialty/AdminSpecialtyAuthorizationTests.cs`.
- `tests/ClinicManagementSystem.IntegrationTests/Scheduling/AdminSchedulingAuthorizationTests.cs`.
- `tests/ClinicManagementSystem.IntegrationTests/Reception/ReceptionAppointmentAuthorizationTests.cs`.
- `docs/VC22_BACKEND_TEST.md` (mới).

Sau khi các bước local pass, tiếp tục frontend `/admin/users`: StateService, Razor, CSS riêng trong `wwwroot/css` và link ở `_Host.cshtml`. Commit/push và kiểm tra production thực hiện sau test local toàn luồng.
