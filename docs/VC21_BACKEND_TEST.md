# VC21 — Test backend danh mục dịch vụ

Thứ tự triển khai: **backend → test Swagger local → frontend → test local toàn luồng → production**.

Backend quản lý `billing.services`, không cần thay đổi schema hoặc seed lại database. Chỉ dùng dữ liệu test trên database local của bạn trong giai đoạn này.

## 1. Chạy API và đăng nhập

Tại thư mục dự án, dừng API cũ bằng `Ctrl+C` rồi chạy lại để nạp code mới:

```powershell
dotnet run --project .\src\ClinicManagementSystem.Api\ClinicManagementSystem.Api.csproj --launch-profile http
```

Mở [Swagger local](http://localhost:5238/swagger). Kiểm tra API đang dùng connection string database local.

Gọi `POST /api/auth/login` với tài khoản Admin local đã tạo:

```json
{
  "username": "admin.local@clinic.local",
  "password": "<mật khẩu thật của Admin local>"
}
```

Copy access token trong `content` của response, bấm **Authorize** và nhập `Bearer <token>` theo ô hướng dẫn của Swagger. Dùng mật khẩu thật khi login; password hash chỉ lưu trong database.

## 2. Các endpoint và quyền truy cập

| Method | Endpoint | Chức năng | Thành công |
|---|---|---|---|
| GET | `/api/admin/services` | Danh sách có lọc và phân trang | 200 |
| GET | `/api/admin/services/{id}` | Chi tiết dịch vụ | 200 |
| POST | `/api/admin/services` | Thêm dịch vụ, mặc định đang dùng | 201 |
| PUT | `/api/admin/services/{id}` | Sửa thông tin, giữ trạng thái hiện tại | 200 |
| PATCH | `/api/admin/services/{id}/deactivate` | Ngừng sử dụng, không xóa bản ghi | 200 |

Tất cả endpoint trong bảng yêu cầu role **Admin** và user ID hợp lệ trong JWT. Không đăng nhập: **401**. Token Doctor/Patient/Receptionist/Pharmacist: **403**.

`GET /api/services` là API danh mục đang dùng dành cho **Doctor**, vẫn giữ quyền Doctor như trước. Dùng token Admin gọi endpoint này sẽ nhận 403.

## 3. Test danh sách

Gọi lần lượt:

```text
GET /api/admin/services
GET /api/admin/services?active=true
GET /api/admin/services?active=false
GET /api/admin/services?page=1&pageSize=10
GET /api/admin/services?keyword=VC21_LOCAL
```

Kỳ vọng 200; `content` gồm `items`, `page`, `pageSize`, `totalItems`, `totalPages`.

- Bỏ `active`: lấy cả dịch vụ đang dùng và ngừng dùng.
- `keyword`: tìm trong mã hoặc tên dịch vụ.
- `specialtyId`: lọc theo ID chuyên khoa thật, ví dụ `?specialtyId=2`. Không mặc định ID 2 có trong DB của bạn.
- Dữ liệu cũ có `code` hoặc `durationMinutes = null` vẫn được trả về.
- Trang vượt số trang có thể trả `items: []`, không phải lỗi.
- `page=0`, `pageSize=0`, `pageSize=101`, `specialtyId=0`, `active=invalid`: 400.

## 4. POST — thêm dịch vụ

Gọi `POST /api/admin/services`:

```json
{
  "code": "VC21_LOCAL_001",
  "name": "Xét nghiệm VC21 local",
  "description": "Dữ liệu kiểm tra chức năng VC21",
  "specialtyId": null,
  "price": 250000,
  "durationMinutes": 15
}
```

Kỳ vọng **201**, `content.id` là ID mới, `isActive: true`, mã được chuẩn hóa chữ hoa. Ghi lại ID này và thay `{id}` trong các bước sau bằng số đó.

Gọi `GET /api/admin/services/{id}`: 200 và dữ liệu vừa tạo.

Gửi lại body cùng mã, hoặc đổi mã thành `vc21_local_001`: **409**, không tạo dòng trùng. Nếu đã dùng mã này trong lần test trước, chọn mã mới như `VC21_LOCAL_002` cho lần test mới.

Để test chuyên khoa, lấy một ID đang hoạt động từ `GET /api/specialties?active=true`, dùng mã mới và gán `specialtyId` vào POST. Kỳ vọng 201, response có ID/tên chuyên khoa tương ứng. Chuyên khoa không tồn tại: 400; chuyên khoa đã ngừng hoạt động khi tạo dịch vụ đang dùng: 409.

## 5. Test validation

Sửa từng trường của body hợp lệ và gửi riêng từng lần:

| Dữ liệu lỗi | Kỳ vọng |
|---|---|
| Thiếu `code`, `name`, `price` hoặc `durationMinutes` | 400 |
| Mã có dấu, khoảng trắng giữa mã, hoặc dài hơn 30 ký tự | 400 |
| Tên chỉ gồm khoảng trắng hoặc dài hơn 255 ký tự | 400 |
| `price: -1` hoặc `price: 10000000000` | 400 |
| `price: 1.001` — quá 2 chữ số thập phân | 400 |
| `durationMinutes: 0`, `-1` hoặc `1.5` | 400 |
| `specialtyId: 0` | 400 |
| Mô tả dài hơn 500 ký tự | 400 |

Giá 0 hợp lệ (dịch vụ miễn phí). Giá tối đa `9999999999.99`, khớp cột `decimal(12,2)`. Thời lượng là số phút nguyên dương. Không có chuyên khoa: gửi `null` hoặc bỏ trường `specialtyId`.

Validation MVC có thể trả `ProblemDetails` với `errors`; lỗi nghiệp vụ từ service trả `{ statusCode, message, content }` theo mẫu hiện có.

## 6. PUT — sửa dịch vụ

Gọi `PUT /api/admin/services/{id}` với **toàn bộ thông tin dịch vụ**:

```json
{
  "code": "VC21_LOCAL_001",
  "name": "Xét nghiệm VC21 local cập nhật",
  "description": "Đã cập nhật giá và thời lượng",
  "specialtyId": null,
  "price": 300000,
  "durationMinutes": 20
}
```

Kỳ vọng 200. ID và `createdAt` giữ nguyên; `updatedAt` có giá trị; giá/thời lượng/tên thay đổi; `isActive` vẫn true.

- Giữ nguyên mã của chính dịch vụ: cập nhật thành công.
- Đổi sang mã của dịch vụ khác: 409.
- `specialtyId: null` bỏ liên kết chuyên khoa; `description: null` bỏ mô tả.
- ID dương không tồn tại: 404. Route ID 0/âm không khớp route hợp lệ nên trả 404.
- PUT không dùng để kích hoạt lại một dịch vụ đã ngừng dùng; trạng thái hiện tại được giữ nguyên.

## 7. PATCH — ngừng sử dụng

Gọi `PATCH /api/admin/services/{id}/deactivate`, **không cần body**.

Kỳ vọng 200, `content.isActive: false`.

1. GET chi tiết vẫn trả dịch vụ: bản ghi không bị xóa.
2. Danh sách `active=true` không chứa ID này; `active=false` có chứa.
3. PATCH lần nữa: vẫn 200, không tạo thêm audit ngừng dùng.
4. POST dịch vụ mới với mã vừa ngừng dùng: 409, mã cũ vẫn được giữ.
5. PUT dịch vụ ngừng dùng: có thể cập nhật thông tin, nhưng `isActive` vẫn false.
6. PATCH ID dương không tồn tại: 404.

VC21 hiện có chức năng ngừng sử dụng; chưa có thao tác kích hoạt lại hoặc xóa dịch vụ.

## 8. Kiểm tra phía bác sĩ

Login Doctor để lấy token mới, thay token Admin trong Swagger.

1. Gọi `GET /api/services`: 200, chỉ thấy dịch vụ đang dùng; không thấy ID đã ngừng sử dụng.
2. Chuẩn bị một medical record **Draft** của chính Doctor đang đăng nhập trên DB local.
3. Gửi trực tiếp ID dịch vụ đã ngừng dùng vào `POST /api/medical-records/{medicalRecordId}/services`:

```json
{
  "serviceId": 123,
  "quantity": 1
}
```

Thay `123` bằng ID test ở trên. Kỳ vọng **409**, thông báo dịch vụ đã ngừng sử dụng; không tạo chỉ định mới.

Nếu dùng record của bác sĩ khác, record không tồn tại hoặc không còn Draft, có thể nhận lỗi quyền/trạng thái trước bước kiểm tra dịch vụ. Chọn đúng record để kiểm tra case 409 này.

Chỉ định đã tạo trước khi ngừng dịch vụ vẫn được lưu; không tự hủy chỉ định hoặc thay giá snapshot đã lưu.

## 9. Kiểm tra audit và hóa đơn cũ

Trong SSMS, chọn đúng **database local**. Có thể kiểm tra audit bằng query chỉ đọc:

```sql
SELECT TOP (20)
    id, user_id, action, entity_name, entity_id, succeeded, occurred_at, details
FROM auth.audit_logs
WHERE entity_name = 'Service'
  AND entity_id = 123 -- thay bằng ID dịch vụ test
ORDER BY id DESC;
```

Kỳ vọng các action `CreateService`, `UpdateService`, `DeactivateService`, đúng user Admin và ID dịch vụ. `details` lưu `Before`/`After`. Bản ghi audit và thay đổi dịch vụ được commit cùng transaction.

Để kiểm tra hóa đơn, dùng **một dịch vụ test có hóa đơn đã tạo** trên local:

1. Dùng token Receptionist gọi `GET /api/invoices/{invoiceId}`; ghi lại description, unitPrice, quantity, tổng tiền và trạng thái.
2. Dùng token Admin sửa tên/giá dịch vụ bằng PUT, rồi ngừng dịch vụ bằng PATCH.
3. Dùng token Receptionist gọi lại hóa đơn cũ.
4. Description, unitPrice, tổng tiền và trạng thái của hóa đơn cũ phải giữ nguyên.

`invoice_items` lưu description/đơn giá; `medical_record_services` lưu `unit_price_snapshot`. VC21 không ghi lại các snapshot này. Với chỉ định đã tạo nhưng chưa lập hóa đơn, đơn giá snapshot vẫn giữ nguyên; mô tả hóa đơn được lấy tại lúc tạo hóa đơn theo luồng hiện có.

Có thể kiểm tra thêm tranh chấp mã trên local bằng hai request POST đồng thời cùng mã: kỳ vọng một lần 201 và một lần 409. API khóa ghi danh mục bằng `sp_getapplock` trong transaction; case SQL thực tế này cần kiểm tra trên DB local, chưa được mô phỏng bằng tests HTTP.

## 10. Tests tự động và bước tiếp theo

Đã chạy khi bàn giao backend:

- API Release rebuild: thành công, 4 warning nullable sẵn có.
- Toàn bộ unit tests: **344 pass**.
- Nhóm HTTP `AuthorizationTests`: **168 pass**.
- Các tests trên dùng mocks/test server, không kết nối database thật.

Chạy lại nếu cần:

```powershell
dotnet test .\tests\ClinicManagementSystem.UnitTests\ClinicManagementSystem.UnitTests.csproj -c Release
dotnet test .\tests\ClinicManagementSystem.IntegrationTests\ClinicManagementSystem.IntegrationTests.csproj -c Release --filter "FullyQualifiedName~AuthorizationTests"
```

Sau khi các bước Swagger trên pass, triển khai frontend `/admin/services` theo mẫu StateService → Razor → CSS riêng trong `wwwroot/css`, link CSS tại `_Host.cshtml`. Sau đó test local với Admin và Doctor, rồi mới đưa lên production và kiểm tra trên Azure.

Workflow hiện tại theo dõi thay đổi Application/Infrastructure trên `develop`; khi push các file này, workflow deploy API và Web có thể cùng được kích hoạt. Giai đoạn backend này chưa commit/push hoặc deploy.

## 11. File thay đổi

| File | Nội dung |
|---|---|
| `src/ClinicManagementSystem.Application/DTOs/Service/AdminServiceDTO.cs` | Thông tin dịch vụ cho Admin |
| `src/ClinicManagementSystem.Application/DTOs/Service/AdminServiceQueryDTO.cs` | Filter và phân trang |
| `src/ClinicManagementSystem.Application/DTOs/Service/SaveServiceRequestDTO.cs` | Request và validation |
| `src/ClinicManagementSystem.Infrastructure/Repositories/ServiceRepository.cs` | Query Admin, kiểm tra mã trùng, khóa ghi |
| `src/ClinicManagementSystem.Application/Services/AdminServiceCatalogService.cs` | Nghiệp vụ CRUD giới hạn và audit |
| `src/ClinicManagementSystem.Api/Controllers/AdminServiceController.cs` | 5 endpoint yêu cầu Admin |
| `src/ClinicManagementSystem.Api/Program.cs` | Đăng ký DI |
| `src/ClinicManagementSystem.Application/Services/MedicalRecordService.cs` | Chặn tạo chỉ định mới cho dịch vụ ngừng dùng |
| `tests/ClinicManagementSystem.UnitTests/Application/Service/AdminServiceCatalogServiceTests.cs` | Tests nghiệp vụ VC21 |
| `tests/ClinicManagementSystem.UnitTests/Application/MedicalRecord/MedicalRecordServiceTests.cs` | Regression và cập nhật fixture active |
| `tests/ClinicManagementSystem.IntegrationTests/Service/AdminServiceAuthorizationTests.cs` | JWT, quyền, HTTP validation |
| `docs/VC21_BACKEND_TEST.md` | Hướng dẫn này |
