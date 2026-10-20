# Lịch làm việc và nghỉ phép của bác sĩ

## Phạm vi

- Menu **Lịch của tôi**: `/doctor/schedule`, chỉ dành cho Doctor.
- Bác sĩ xem ca làm và lịch nghỉ Admin đã đăng ký; việc thêm/sửa/xóa vẫn thực hiện ở Admin.
- Hiển thị lịch tuần, chọn ngày xem chi tiết, chuyển tuần, chọn ngày và về hôm nay.
- Tab **Nghỉ đã đăng ký** có lọc khoảng ngày, phạm vi, loại nghỉ và phân trang.
- Dùng nền trắng/xanh nhạt giống project; CSS riêng, không đổi theme các module khác.
- Không thay đổi schema, cấu hình storage hoặc dữ liệu production.

## Dữ liệu và quy tắc

1. Backend lấy `UserId` từ JWT rồi tìm bác sĩ đang hoạt động. Không có tham số chọn bác sĩ khác.
2. Lịch lấy từ `DoctorSchedules`, lọc bác sĩ, thứ, trạng thái hoạt động và ngày hiệu lực.
3. Nếu dữ liệu cũ còn nhiều phiên bản cùng giờ bắt đầu, chọn phiên bản mới nhất như luồng đặt lịch.
4. Lịch nghỉ gồm `DoctorTimeOffs.DoctorId` của chính bác sĩ và `DoctorId = NULL` (toàn phòng khám).
5. Ca được phân công vẫn hiển thị trong chi tiết. Giờ làm thực tế được trừ nghỉ giữa ca và các khoảng nghỉ áp dụng.
6. Đây là lịch làm việc, không phải danh sách slot trống: giờ đã có lịch hẹn vẫn thuộc ca làm.
7. Thời gian lịch là giờ phòng khám GMT+7; ngày hôm nay được backend xác định, không phụ thuộc múi giờ trình duyệt.
8. `EndAt` của lịch nghỉ là mốc loại trừ. Nghỉ cả ngày 21/10 lưu đến 00:00 ngày 22/10 nhưng UI hiển thị **21/10 · Cả ngày**.
9. Sắp tới/Đang diễn ra/Đã kết thúc là trạng thái thời gian, không phải trạng thái xét duyệt đơn.
10. Màn hình chỉ đọc không thay đổi lịch hẹn. Luồng Admin hiện có tiếp tục cảnh báo lịch hẹn bị ảnh hưởng để lễ tân xử lý.

## API

```text
GET /api/doctors/me/schedule
GET /api/doctors/me/schedule?from=2026-10-19&to=2026-10-25
GET /api/doctors/me/time-off?page=1&pageSize=10
GET /api/doctors/me/time-off?from=2026-10-01&to=2026-10-31&scope=0&type=0&page=1&pageSize=10
```

- Schedule mặc định tuần hiện tại, từ thứ Hai đến Chủ Nhật. Khoảng lọc tối đa 42 ngày.
- Time-off mặc định toàn bộ lịch nghỉ, mới nhất trước; khoảng lọc tối đa 366 ngày.
- `scope`: bỏ trống = tất cả; `0` = cá nhân; `1` = toàn phòng khám.
- `type`: bỏ trống = tất cả; `0` = nghỉ phép; `1` = nghỉ lễ; `2` = nghỉ khác.
- Khi lọc ngày phải nhập đủ `from` và `to`; `to` là ngày cuối cùng cần xem, bao gồm cả ngày đó.
- Trang >= 1, pageSize từ 1 đến 100. Ngày/scope/type không hợp lệ trả 400.
- Chưa đăng nhập: 401. Sai vai trò hoặc không có hồ sơ bác sĩ đang hoạt động: 403.
- Hai API có `Cache-Control: no-store` vì chứa lịch và lý do nghỉ cá nhân.

## Kiểm tra thủ công ở local

1. Chạy API và Web; đăng nhập bác sĩ, vào **Lịch của tôi**.
2. Kiểm tra tuần mặc định, ngày hôm nay và GMT+7.
3. Chọn một ngày: ca ban đầu, giờ nghỉ, thời lượng lượt khám, ngày hiệu lực phải khớp dữ liệu Admin.
4. Chuyển tuần trước/sau, chọn ngày khác, bấm **Hôm nay** và tải lại.
5. Dùng dữ liệu thử có nghỉ nửa ngày: giờ làm còn lại phải loại đúng khoảng nghỉ.
6. Nghỉ cả ngày/nhiều ngày: ngày cuối hiển thị đúng; không lan sang ngày kết thúc loại trừ.
7. Nghỉ chung hiển thị cho mọi bác sĩ. Nghỉ riêng của bác sĩ khác không được xuất hiện.
8. Mở tab nghỉ, lọc ngày/phạm vi/loại, đổi số dòng và chuyển trang.
9. Nhập thiếu một ngày: UI báo lỗi, không gửi yêu cầu; bộ lọc chưa áp dụng phải có thông báo.
10. Trên điện thoại, lịch tuần thành danh sách theo ngày, trang không bị tràn ngang.
11. Admin đổi lịch thử rồi bác sĩ tải lại để thấy dữ liệu mới.
12. Thử JWT Patient/Admin gọi hai API: phải bị từ chối; thêm `doctorId` trên query không thay đổi phạm vi.

Chỉ tạo dữ liệu thử trong database local riêng. Không sửa dữ liệu phòng khám/production để làm bài test.

## Kiểm thử tự động

```powershell
dotnet test tests/ClinicManagementSystem.UnitTests/ClinicManagementSystem.UnitTests.csproj --filter "FullyQualifiedName~DoctorSchedule"
dotnet test tests/ClinicManagementSystem.IntegrationTests/ClinicManagementSystem.IntegrationTests.csproj --filter "FullyQualifiedName~DoctorScheduleAuthorizationTests"
```

Bài xuyên suốt JWT -> API -> SQL Server yêu cầu Docker đang hoạt động. Database do Testcontainers tạo riêng và tự dọn:

```powershell
dotnet test tests/ClinicManagementSystem.IntegrationTests/ClinicManagementSystem.IntegrationTests.csproj --filter "FullyQualifiedName~DoctorScheduleDatabaseIntegrationTests"
```

## Sau khi deploy

- Không cần migration hoặc biến môi trường mới.
- Deploy cả API và Web rồi đăng nhập bác sĩ, mở `/doctor/schedule`.
- Kiểm tra ca/nghỉ thực tế, tải lại, quyền tài khoản và giao diện điện thoại.
