# VC20 — Lịch làm việc và nghỉ phép: test backend

Backend dùng cấu trúc Controller → Application Service → Repository / Unit of Work → SQL Server.
Đây là mốc backend để kiểm tra trước khi triển khai frontend `/admin/schedules`.

## 1. Phạm vi và quy tắc

- Tất cả API `/api/admin/...` trong VC20 yêu cầu JWT có role `Admin`.
- Mỗi lịch làm việc là **một bác sĩ, một thứ, một ca, một giai đoạn hiệu lực**.
- `dayOfWeek`: Chủ nhật = 0, Thứ hai = 1, ..., Thứ bảy = 6.
- `slotMinutes`: từ 5 đến 120. Ca làm không qua đêm.
- Hai ca cùng bác sĩ/thứ có thể nằm sát nhau, nhưng không trùng giờ trong cùng giai đoạn hiệu lực.
- Phiên bản lịch được kiểm tra theo khóa database hiện có `(doctor_id, day_of_week, start_time, effective_from)`.
- `effectiveFrom` bắt buộc; `effectiveTo = null` là không có ngày kết thúc. Ngày kết thúc hiệu lực bao gồm cả ngày đó.
- Sửa một lịch đã có hiệu lực trong quá khứ phải chọn ngày hiệu lực mới từ hôm nay; phiên bản cũ được giữ lại.
- Khi `effectiveFrom` mới lớn hơn ngày bắt đầu bản cũ, API tạo bản mới và kết thúc bản cũ vào ngày trước đó nếu bản cũ còn kéo dài.
- Sửa lịch chưa có hiệu lực, giữ cùng `effectiveFrom`: cập nhật chính bản ghi đó.
- Không chuyển bác sĩ/thứ của một bản lịch hiện có; tạo lịch riêng cho bác sĩ/thứ khác.
- Đổi giờ, nghỉ trưa, slot hoặc rút ngắn hiệu lực có thể ảnh hưởng lịch hẹn đã đặt.
- Khi có lịch hẹn bị ảnh hưởng, API trả **409**, `content.requiresConfirmation = true` và `content.affectedAppointments`. Chưa lưu dữ liệu tại thời điểm này.
- Chỉ gửi lại cùng yêu cầu với `confirmAffectedAppointments = true` sau khi đã xem danh sách. API tính lại xung đột trước khi lưu.
- Xác nhận không tự đổi giờ, hủy hoặc xóa lịch hẹn. Lễ tân xử lý đổi lịch bằng chức năng hiện có.
- Có thông báo `InApp` cho lễ tân đang hoạt động và bác sĩ của từng lịch hẹn bị ảnh hưởng; không gửi email/SMS.
- Nghỉ riêng: `doctorId` là ID bác sĩ. Nghỉ toàn phòng khám: `doctorId = null`.
- `type`: 0 = nghỉ phép, 1 = nghỉ lễ, 2 = nghỉ khác. `reason` bắt buộc, tối đa 255 ký tự.
- `startAt`, `endAt` dùng giờ phòng khám UTC+7, **không ghi Z hay +07:00 trong JSON**. `endAt` là mốc kết thúc không bao gồm.
- Nghỉ cả ngày dùng từ 00:00 ngày đầu đến 00:00 ngày sau ngày cuối; ví dụ nghỉ 19/10 dùng 19/10 00:00 → 20/10 00:00.
- Không tạo khoảng nghỉ trùng nghỉ riêng/toàn phòng khám đã có.
- Xóa nghỉ phép có audit lưu nội dung trước khi xóa; không xóa lịch hẹn và không xóa thông báo đã tạo.
- Slot loại cả giờ nghỉ trưa, nghỉ riêng, nghỉ toàn phòng khám, lịch đã đặt và giờ đã qua; lấy đầy đủ các ca trong ngày.
- Không thay đổi schema, scaffold models hoặc tự chạy seed khi API khởi động.

## 2. Chuẩn bị local

Kiểm tra `ConnectionStrings:ClinicDb` trong API Development trỏ đúng database local.
Database cần có schema hiện tại của dự án (bản 003 có 29 bảng); không chạy lại migration trên database đã có schema.
Tài khoản local cần đã có role Admin. Không dùng database production hoặc database dùng chung để thử thay đổi lịch.

Chạy PowerShell tại thư mục gốc:

```powershell
dotnet run --project .\src\ClinicManagementSystem.Api --launch-profile http
```

Mở Swagger: `http://localhost:5238/`.

Gọi `POST /api/auth/login`, lấy `content.accessToken` rồi bấm **Authorize**.
Gọi `GET /api/admin/scheduling/doctors` để chọn ID bác sĩ đang hoạt động (`isActive = true`).

## 3. Các API

| Method | Endpoint | Mục đích |
|---|---|---|
| GET | `/api/admin/scheduling/doctors` | Bác sĩ để chọn; gồm trạng thái hoạt động |
| GET | `/api/admin/doctor-schedules` | Danh sách lịch: `doctorId`, `date`, `active`, `page`, `pageSize` |
| POST | `/api/admin/doctor-schedules` | Tạo lịch làm việc |
| PUT | `/api/admin/doctor-schedules/{id}` | Sửa hoặc tạo phiên bản mới của lịch |
| GET | `/api/admin/time-off` | Danh sách nghỉ: `doctorId`, `clinicWideOnly`, `from`, `to`, `page`, `pageSize` |
| POST | `/api/admin/time-off` | Đăng ký nghỉ |
| DELETE | `/api/admin/time-off/{id}` | Xóa đăng ký nghỉ |
| GET | `/api/doctors/{id}/available-slots?date=...` | API đặt lịch hiện có để kiểm tra slot |

Lọc nghỉ theo `doctorId` cũng trả nghỉ toàn phòng khám ảnh hưởng bác sĩ đó.
`clinicWideOnly = true` chỉ trả nghỉ toàn phòng khám; không kết hợp với `doctorId`.
`from`, `to` lọc khoảng nghỉ giao với ngày được chọn; `to` bao gồm cả ngày cuối.
`pageSize` từ 1 đến 100; mặc định 10.
Response vẫn dùng `statusCode`, `message`, `content` như các chức năng đã có.

## 4. Tạo lịch

Đổi `doctorId` theo dữ liệu local. Chọn ngày tương lai phù hợp khi thực hiện test; ngày 19/10/2026 trong ví dụ là Thứ hai.
Kiểm tra danh sách lịch trước để chọn bác sĩ/ca chưa bị trùng. Không cần xóa lịch đang dùng chỉ để chạy ví dụ.

`POST /api/admin/doctor-schedules`:

```json
{
  "doctorId": 1,
  "dayOfWeek": 1,
  "startTime": "08:00:00",
  "endTime": "17:00:00",
  "breakStart": "11:30:00",
  "breakEnd": "13:30:00",
  "slotMinutes": 30,
  "effectiveFrom": "2026-10-19",
  "effectiveTo": "2026-12-31",
  "confirmAffectedAppointments": false
}
```

Kỳ vọng **201**, `content.schedule.id` có giá trị. Ghi lại ID.
Gọi GET danh sách và available-slots cho ngày tương ứng: chỉ có slot nằm trong ca, ngoài giờ nghỉ.
Tạo lại cùng lịch: **409**.
Tạo ca khác chồng giờ/giai đoạn: **409**.
Tạo ca liền kề 17:00 → 19:00, hai break = null: **201**, available-slots trả thêm ca chiều tối.

## 5. Sửa lịch và kiểm tra phiên bản

Gửi PUT tới ID vừa tạo, đổi `effectiveFrom` thành `2026-10-26`, giữ các trường khác và đổi `slotMinutes` thành 20.

- Nếu không có lịch hẹn bị ảnh hưởng: **201**, bản mới có ID khác, `previousVersionId` là ID cũ.
- GET lịch: bản cũ kết thúc 25/10, bản mới bắt đầu 26/10.
- Kiểm tra slot ngày 19/10 vẫn dùng 30 phút; ngày 26/10 dùng 20 phút.
- Sửa một lịch có `effectiveFrom` trước hôm nay mà vẫn giữ ngày bắt đầu cũ: **409**, không ghi đè lịch sử.
- Sửa một lịch tương lai với cùng ngày hiệu lực: **200**, giữ nguyên ID.

Muốn kiểm tra cảnh báo: dùng chức năng đặt lịch hiện có tạo một lịch hẹn tương lai thuộc ca đó, rồi đổi slot/giờ/nghỉ trưa hoặc rút ngắn hiệu lực.

- Gửi với `confirmAffectedAppointments = false`: **409**, có danh sách lịch hẹn bị ảnh hưởng và không có dữ liệu mới.
- Xem danh sách; gửi lại với `true`: **200/201** tùy cập nhật hay tạo phiên bản.
- Kiểm tra lịch hẹn vẫn giữ nguyên thời gian và trạng thái; có thông báo nội bộ cho lễ tân/bác sĩ.

## 6. Đăng ký nghỉ và xóa nghỉ

`POST /api/admin/time-off`, nghỉ riêng một phần ngày:

```json
{
  "doctorId": 1,
  "startAt": "2026-10-26T09:00:00",
  "endAt": "2026-10-26T10:00:00",
  "type": 0,
  "reason": "Việc gia đình - dữ liệu test local",
  "isFullDay": false,
  "confirmAffectedAppointments": false
}
```

- Không có lịch hẹn bị ảnh hưởng: **201**. Slot giao với 09:00 → 10:00 bị loại; slot bắt đầu đúng 10:00 có thể vẫn còn.
- Có lịch hẹn: **409** kèm danh sách. Gửi lại `confirmAffectedAppointments = true` để lưu và tạo thông báo.
- Lặp lại khoảng nghỉ đã lưu: **409**, kể cả đã xác nhận.
- DELETE tới `content.timeOff.id`: **200**, slot được phép xuất hiện lại nếu không có nguyên nhân chặn khác.
- DELETE cùng ID lần nữa: **404**.

Nghỉ cả phòng khám trọn một ngày:

```json
{
  "doctorId": null,
  "startAt": "2026-10-27T00:00:00",
  "endAt": "2026-10-28T00:00:00",
  "type": 1,
  "reason": "Nghỉ toàn phòng khám - dữ liệu test local",
  "isFullDay": true,
  "confirmAffectedAppointments": false
}
```

Kỳ vọng slot của **mọi bác sĩ có lịch làm ngày 27/10** là rỗng sau khi lưu.
Ngày 28/10 không bị chặn bởi đăng ký nghỉ này. Nếu nghỉ chưa lưu do 409, slots chưa thay đổi.

## 7. Validation và phân quyền

| Trường hợp | Kết quả |
|---|---|
| Không token, gọi bất kỳ API quản trị VC20 | 401 |
| Token Patient/Doctor/Receptionist/Pharmacist | 403 |
| Token Admin, nhưng mutation không có user ID hợp lệ | 401 |
| Bỏ `dayOfWeek`, `startTime`, `endTime` hoặc `effectiveFrom` | 400 |
| Thứ = 7, slot = 4 hoặc 121 | 400 |
| Giờ kết thúc <= bắt đầu, ca qua đêm | 400 |
| Chỉ có một đầu giờ nghỉ, nghỉ nằm ngoài ca hoặc phủ hết ca | 400 |
| Ngày kết thúc hiệu lực < ngày bắt đầu | 400 |
| Bác sĩ không tồn tại / ngừng hoạt động | 404 / 409 |
| Khoảng nghỉ không hợp lệ, bỏ type/lý do, lý do >255 ký tự | 400 |
| Nghỉ trọn ngày nhưng giờ không phải 00:00 | 400 |
| Gửi giờ nghỉ kèm Z hoặc offset | 400 |
| Giai đoạn lịch/ca chồng lấn hoặc nghỉ chồng lấn | 409 |
| `page = 0`, `pageSize = 101`, khoảng lọc ngược | 400 |
| Sửa/xóa một ID dương không tồn tại | 404 |

Swagger có thể giữ token sau khi đăng xuất trên Web. Dùng **Authorize → Logout** khi test 401; thay token khi test role khác.

## 8. Kiểm tra database (chỉ SELECT)

```sql
SELECT id, doctor_id, day_of_week, start_time, end_time,
       break_start, break_end, slot_minutes, effective_from, effective_to, is_active
FROM scheduling.doctor_schedules
WHERE doctor_id = 1
ORDER BY day_of_week, effective_from, start_time;

SELECT TOP (20) id, doctor_id, start_at, end_at, type, reason, is_full_day, approved_by
FROM scheduling.doctor_time_off
ORDER BY id DESC;

SELECT TOP (30) user_id, action, entity_name, entity_id, succeeded, occurred_at, details
FROM auth.audit_logs
WHERE entity_name IN ('DoctorSchedule', 'DoctorTimeOff')
ORDER BY id DESC;

SELECT TOP (30) appointment_id, user_id, channel, title, status, created_at
FROM auth.notifications
WHERE title IN (N'Lịch làm việc thay đổi', N'Lịch nghỉ ảnh hưởng lịch hẹn')
ORDER BY id DESC;
```

## 9. Tests và mốc tiếp theo

```powershell
dotnet test .\tests\ClinicManagementSystem.UnitTests\ClinicManagementSystem.UnitTests.csproj -c Release

dotnet test .\tests\ClinicManagementSystem.IntegrationTests\ClinicManagementSystem.IntegrationTests.csproj -c Release --filter "FullyQualifiedName~AdminSchedulingAuthorizationTests|FullyQualifiedName~AdminSpecialtyAuthorizationTests"
```

Unit tests dùng mock; HTTP tests trên dùng test host và stub, không chạm database thật.
Toàn bộ integration tests còn lại cần Docker Desktop để chạy SQL Server Testcontainers riêng.
Chưa có xác nhận kiểm thử persistence trên SQL Server thật khi Docker chưa chạy; dùng các bước Swagger/local ở trên để kiểm tra repository và transaction trên database local.

Sau khi backend được xác nhận: triển khai StateService và trang `/admin/schedules`, kiểm tra UI local,
chạy tests, rồi mới commit/push qua CI/CD và test production theo yêu cầu.
Chưa thay đổi frontend, commit, push hoặc deploy tại mốc backend này.
