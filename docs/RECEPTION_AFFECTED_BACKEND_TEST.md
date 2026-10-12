# Test backend: lễ tân xử lý lịch hẹn bị ảnh hưởng

Backend nối tiếp VC20. Chưa có trang frontend cho lễ tân trong giai đoạn này.

## 1. Khởi động và quyền truy cập

Kiểm tra API đang dùng database **local của bạn** trước khi test ghi dữ liệu. Không dùng database dùng chung hoặc production để tạo dữ liệu thử.

Tại thư mục gốc project:

```powershell
dotnet run --project .\src\ClinicManagementSystem.Api --launch-profile http
```

Swagger: <http://localhost:5238/>. Đăng nhập qua `POST /api/auth/login`, lấy `content.accessToken` và dán token vào **Authorize**. Thay token khi chuyển vai trò.

| Endpoint | Vai trò | Kỳ vọng |
|---|---|---|
| `GET /api/reception/appointments/affected` | Receptionist | 200 |
| `PATCH /api/reception/appointments/{id}/reschedule` | Receptionist | 200 khi hợp lệ |
| `PATCH /api/reception/appointments/{id}/cancel` | Receptionist | 200 khi hợp lệ |
| Ba endpoint trên | Chưa đăng nhập | 401 |
| Ba endpoint trên | Admin, Doctor, Patient, Pharmacist | 403 |
| `GET /api/notifications` | Doctor, Receptionist, Patient | Chỉ thông báo của user đăng nhập |
| `GET /api/notifications` | Admin, Pharmacist | 403 |
| API Patient `/api/appointments/{id}/reschedule`, `/cancel` | Receptionist | Vẫn 403 |

Các role dùng để kiểm tra phải có tài khoản tương ứng. Không thay role cho một tài khoản đang có phiên đăng nhập rồi dùng lại token cũ.

## 2. Chuẩn bị hai lịch hẹn test

Dùng một bác sĩ và một bệnh nhân test đang hoạt động. Thay tất cả ID bên dưới bằng ID thực tế; không dùng ID của dữ liệu khám thật.

Ví dụ thời gian bên dưới là 19–20/10/2026. Nếu những ngày này đã qua, chọn ngày tương lai và chỉnh `dayOfWeek` cho đúng (`0` Chủ nhật, `1` thứ Hai, ..., `6` thứ Bảy).

1. Bằng token Admin, bảo đảm bác sĩ có ca làm hợp lệ ngày 19 và ngày 20. Dùng VC20 để thêm ca nếu chưa có; không tạo ca trùng với ca hiện có.
2. Bằng token Receptionist, gọi `GET /api/doctors/{doctorId}/available-slots?date=2026-10-19` và chọn **hai slot trống khác nhau**. Đây là dữ liệu thật do API trả, không tự đoán slot.
3. Tạo hai lịch qua `POST /api/appointments/reception`, ví dụ:

```json
{
  "patientId": 2,
  "doctorId": 1,
  "startTime": "2026-10-19T09:00:00",
  "reason": "Test xử lý lịch nghỉ VC20",
  "source": 2
}
```

Thay ID và giờ. Ghi lại `content.id`, `appointmentCode`, giờ và trạng thái của từng lịch. Không tạo hai lịch cùng bác sĩ, cùng slot. Lịch test hủy nên chưa có thanh toán.

## 3. Admin đăng ký nghỉ và xác nhận

Bằng token Admin, `POST /api/admin/time-off`:

```json
{
  "doctorId": 1,
  "startAt": "2026-10-19T00:00:00",
  "endAt": "2026-10-20T00:00:00",
  "type": 0,
  "reason": "Test bác sĩ nghỉ một ngày",
  "isFullDay": true,
  "confirmAffectedAppointments": false
}
```

- Lần đầu: 409, `requiresConfirmation=true`, có hai lịch test trong `affectedAppointments`; chưa lưu lịch nghỉ.
- Gửi lại với `confirmAffectedAppointments=true`: 201, lịch nghỉ được lưu.
- Hai lịch hẹn vẫn giữ nguyên giờ và trạng thái; không tự hủy.
- Ghi lại ID lịch nghỉ để kiểm tra xóa lịch nghỉ ở mục 8.

## 4. Lễ tân xem danh sách và thông báo

Chuyển token Receptionist:

```text
GET /api/reception/appointments/affected?from=2026-10-19&to=2026-10-20&doctorId=1&page=1&pageSize=10
GET /api/notifications
```

Kỳ vọng trong `content.items`:

- Hai lịch có ID/mã đúng, tên và điện thoại bệnh nhân, bác sĩ, giờ cũ, `affectedReasons`.
- Lịch Pending/Confirmed chưa đến giờ có `canProcess=true`.
- Mỗi lịch có `version` dài 64 ký tự. **Copy nguyên giá trị này**, không sửa hoặc tự tạo; nó là phiên bản lịch khi bạn đọc.
- Thông báo của lễ tân có mã lịch bị ảnh hưởng. Gửi `?userId=999` không cho đọc thông báo của user khác.

Không truyền ngày: mặc định từ 7 ngày trước đến 30 ngày sau hôm nay (UTC+7). Hai ngày lọc tính cả ngày, tối đa 91 ngày. `totalItems` là số lịch còn xung đột sau kiểm tra, không phải số thông báo.

## 5. Test đổi lịch

1. `GET /api/doctors/1/available-slots?date=2026-10-20`, chọn slot trống.
2. `PATCH /api/reception/appointments/{ID_LICH_THU_NHAT}/reschedule`:

```json
{
  "expectedVersion": "COPY_VERSION_CUA_LICH_THU_NHAT_TU_GET_AFFECTED",
  "newStartTime": "2026-10-20T09:00:00",
  "reason": "Đã liên hệ, bệnh nhân đồng ý chuyển sang ngày hôm sau"
}
```

Chuỗi mẫu trong `expectedVersion` phải được thay bằng 64 ký tự API trả. Giờ mới phải được lấy từ API slot; gửi giờ phòng khám **không có `Z` hoặc `+07:00`**.

Kỳ vọng:

- 200; ID/mã lịch, bác sĩ, bệnh nhân, trạng thái và phí snapshot giữ nguyên.
- StartTime/EndTime đổi theo slot mới, `version` thay đổi.
- GET affected lại: lịch này rời danh sách.
- Bệnh nhân và bác sĩ gọi GET notifications bằng token của chính họ thấy thông báo kết quả.
- Audit ghi người xử lý, giờ cũ/mới và lý do. Không tạo chuyển trạng thái giả khi chỉ đổi giờ.
- Nhắc lịch 24 giờ trước chưa gửi được cập nhật theo giờ mới; các thông báo đã gửi giữ nguyên.

## 6. Test hủy lịch

GET affected lại để lấy **version mới nhất của lịch thứ hai**.

`PATCH /api/reception/appointments/{ID_LICH_THU_HAI}/cancel`:

```json
{
  "expectedVersion": "COPY_VERSION_CUA_LICH_THU_HAI_TU_GET_AFFECTED",
  "reason": "Đã liên hệ, bệnh nhân không tiếp tục đặt lịch"
}
```

Kỳ vọng:

- 200, `status=4` (Cancelled), giữ nguyên ID/mã/giờ của lịch đã hủy.
- GET affected lại: lịch rời danh sách.
- Có `CancelReason`, lịch sử trạng thái, actor và audit.
- Bệnh nhân/bác sĩ có thông báo kết quả. Nhắc lịch chưa gửi chuyển trạng thái 3 để ngừng gửi, không xóa thông báo lịch sử.
- Slot trong ngày bác sĩ vẫn nghỉ **không mở lại** vì lịch nghỉ còn hiệu lực.
- Nếu đã có `PaidAmount > 0`, trả 409 yêu cầu xử lý thanh toán riêng; chức năng này không hoàn tiền hay sửa hóa đơn.

## 7. Test lỗi, dữ liệu cũ và thao tác đồng thời

Tạo dữ liệu test mới nếu hai lịch ở trên đã xử lý xong.

| Case | Kỳ vọng |
|---|---|
| Lý do rỗng/toàn khoảng trắng, thiếu version, thiếu giờ mới | 400 |
| Giờ mới gửi `Z`/offset, giờ quá khứ | 400 |
| Giờ mới trong nghỉ trưa/nghỉ phòng khám, ngoài ca, lệch slot hoặc slot đã đặt | 409 |
| Bác sĩ hoặc bệnh nhân ngừng hoạt động khi đổi lịch | 409 |
| ID không tồn tại, body hợp lệ | 404 |
| Lịch đã check-in | Hiện cảnh báo, `canProcess=false`; mutation 409 |
| Lịch đã tới/quá giờ trong khoảng lọc | Vẫn hiện nếu còn xung đột, `canProcess=false`; mutation 409 |
| Completed/Cancelled/NoShow | Không nằm trong danh sách xử lý; mutation 409 |
| Admin bỏ lịch nghỉ và lịch trở lại hợp lệ sau khi lễ tân đã đọc | Mutation 409, yêu cầu tải lại |

### Hai lễ tân cùng xử lý một lịch

1. Mở hai cửa sổ Swagger/Postman, lấy cùng `version` của một lịch.
2. Cửa sổ A đổi/hủy thành công.
3. Cửa sổ B gửi yêu cầu với version cũ: 409. Không có lần ghi lịch sử/thông báo thứ hai.
4. GET affected lại để đồng bộ.

### Bệnh nhân tự đổi/hủy trong lúc lễ tân xử lý

1. Lễ tân lấy version và chưa gửi yêu cầu.
2. Patient dùng endpoint của mình để đổi/hủy cùng lịch.
3. Lễ tân gửi version cũ: 409.
4. Test hai yêu cầu thực sự đồng thời trên SQL Server local: không được ghi đè, không chiếm hai lần cùng slot. Một yêu cầu thành công; yêu cầu cạnh tranh bị từ chối hoặc thấy lịch không còn khả dụng.

### Hai lịch chọn cùng một slot

Hai cửa sổ chọn cùng slot mới cho hai lịch khác nhau. Sau khi A lưu, B phải nhận 409. Kiểm tra cả trường hợp Patient/Receptionist đặt lịch mới đồng thời với lễ tân đổi lịch. Các writer đặt/đổi dùng cùng khóa transaction với Admin và kiểm tra slot lại khi đã có khóa.

Unit/HTTP tests dùng mocks và kiểm tra metadata EF; chúng **không thay thế** phép thử cạnh tranh thật trên SQL Server local.

## 8. Danh sách xử lý phải theo trạng thái hiện tại

- Tạo nghỉ toàn phòng khám (`doctorId=null`): lịch của mọi bác sĩ trong khoảng nghỉ đều được kiểm tra.
- Xóa lịch nghỉ trước khi đổi/hủy lịch hẹn: các lịch khớp ca làm hiện tại phải rời danh sách, thông báo cũ vẫn còn.
- Sửa ca làm/thời lượng slot gây xung đột: lịch phải xuất hiện; khôi phục ca phù hợp: lịch rời danh sách.
- Một lịch bị cả ca làm và lịch nghỉ ảnh hưởng vẫn chỉ có một dòng, có thể có nhiều lý do.
- Kiểm tra lọc bác sĩ/ngày, page/pageSize, và tổng số sau lọc. Một khoảng lọc tối đa 91 ngày, pageSize tối đa 100.

## 9. Tests tự động

```powershell
dotnet test .\tests\ClinicManagementSystem.UnitTests\ClinicManagementSystem.UnitTests.csproj -c Release
dotnet test .\tests\ClinicManagementSystem.IntegrationTests\ClinicManagementSystem.IntegrationTests.csproj -c Release --filter "FullyQualifiedName~AuthorizationTests"
```

Lệnh thứ hai chỉ chạy tests HTTP/JWT với services stub. Không chạy nhóm database integration trên database thật/dùng chung để thử tính năng này.

Backend local pass mới làm frontend. Chưa commit/push/deploy trong giai đoạn này.
