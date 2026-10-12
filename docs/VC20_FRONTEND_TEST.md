# VC20 — Test frontend lịch làm việc và nghỉ phép

Trang: `/admin/schedules`, role `Admin`, layout `HomePageMaster`.
Frontend gọi API qua `AdminSchedulingStateService` → `AuthorizedApiService`; dùng JWT và refresh token hiện có.
CSS riêng: `src/ClinicManagement.Web/wwwroot/css/admin-schedules.css`, liên kết trong `Pages/_Host.cshtml`.

## 1. Chạy local

Tại `D:\_KhanhVy\ClinicManagementSystem`, mở hai terminal PowerShell.

Terminal API:

```powershell
dotnet run --project .\src\ClinicManagementSystem.Api --launch-profile http
```

Terminal Web:

```powershell
dotnet run --project .\src\ClinicManagement.Web --launch-profile http
```

Kiểm tra `ApiSettings:BaseUrl` của Web trong Development là `http://localhost:5238`.
API cần trỏ database local. Giữ nguyên các cấu hình Azure để sau này test production.

Mở `http://localhost:5050/login`, đăng nhập tài khoản Admin local.
Chọn **Lịch làm việc & nghỉ phép** trong sidebar hoặc thẻ tương ứng ở trang chủ Admin.
Có thể mở trực tiếp `http://localhost:5050/admin/schedules`.

## 2. Kiểm tra trang và bộ lọc

- Hai bảng độc lập: lịch làm việc và đăng ký nghỉ, có tổng bản ghi và phân trang riêng.
- Lịch làm: lọc bác sĩ, ngày có hiệu lực và trạng thái. Bấm Lọc để áp dụng; phân trang giữ bộ lọc đã áp dụng.
- Nghỉ: lọc bác sĩ và từ/đến ngày. Chọn bác sĩ cũng trả nghỉ toàn phòng khám ảnh hưởng bác sĩ đó.
- Chọn **Chỉ nghỉ toàn phòng khám** tự bỏ lọc bác sĩ và khóa ô bác sĩ để tránh tổ hợp không hợp lệ.
- Đặt lại trả bộ lọc về mặc định, trang 1. Lịch làm mặc định lọc đang hoạt động.
- Bác sĩ ngừng hoạt động vẫn xuất hiện trong bộ lọc; không được chọn để tạo lịch/nghỉ riêng mới.
- Nếu API không kết nối được, hiển thị lỗi và nút Thử lại, không hiển thị lại dữ liệu cũ như vừa tải thành công.
- Khi cửa sổ hẹp, form xếp dọc; bảng cuộn ngang. Menu mobile theo layout hiện có.

## 3. Thêm lịch làm việc

Bấm **Thêm lịch làm việc**. Chọn một bác sĩ/ca chưa trùng dữ liệu backend đã test.

Ví dụ (đổi ngày nếu ngày mẫu đã qua):

| Trường | Giá trị |
|---|---|
| Bác sĩ | Một bác sĩ đang hoạt động |
| Thứ | Thứ hai |
| Giờ bắt đầu / kết thúc | 08:00 / 17:00 |
| Nghỉ trưa | 11:30 / 13:30 |
| Slot | 30 phút |
| Hiệu lực từ / đến | 19/10/2026 / 31/12/2026 |

Bấm Lưu lịch làm việc. Không có xung đột: thông báo thành công, form đóng, bảng tải lại.
Nếu bản mới không xuất hiện, kiểm tra bộ lọc đang áp dụng; Đặt lại để xem danh sách.
Đối chiếu Swagger `GET /api/doctors/{id}/available-slots?date=2026-10-19`: chỉ có slot phù hợp, ngoài nghỉ trưa và lịch đã đặt.

Test lỗi: không chọn bác sĩ, slot 4/121, kết thúc trước bắt đầu, chỉ nhập một giờ nghỉ trưa,
hoặc ngày kết thúc trước ngày bắt đầu. Form/API phải báo lỗi, không đóng form, không lưu dữ liệu.
Tạo lại ca đã có: hiển thị lỗi trùng; không xuất hiện nút xác nhận lịch hẹn nếu đó chỉ là lỗi ca trùng.

## 4. Sửa và giữ lịch sử

- Bấm Sửa ở lịch đang hoạt động. Bác sĩ/thứ bị khóa, các trường còn lại có thể chỉnh.
- Lịch đã bắt đầu trong quá khứ: form đặt ngày hiệu lực từ hôm nay; chọn ngày mong muốn trong tương lai để tạo phiên bản.
- Giữ ngày hiệu lực của lịch tương lai: cập nhật bản hiện có.
- Chọn ngày hiệu lực mới lớn hơn ngày bắt đầu bản cũ: tạo phiên bản, bản cũ kết thúc ngày trước đó.
- Lịch đã hết hiệu lực hoặc ngừng hoạt động không có nút Sửa khả dụng.
- Sau lưu, bỏ lọc ngày để xem cả hai bản; kiểm tra slot ngày cũ và ngày mới theo hướng dẫn backend.

## 5. Đăng ký nghỉ riêng

Bấm Đăng ký nghỉ, chọn bác sĩ, Nghỉ phép, khoảng tương lai phù hợp, nhập lý do.
Ví dụ: 26/10/2026 09:00 → 26/10/2026 10:00, không chọn Nghỉ cả ngày.

Lưu thành công: form đóng, bảng nghỉ tải lại. Kiểm tra slot bác sĩ ngày đó loại các slot giao với khoảng nghỉ.
Thử bỏ lý do, quá 255 ký tự, hoặc kết thúc trước bắt đầu: báo lỗi và giữ form.
Khoảng nghỉ đã có bị trùng: báo lỗi API, không lưu thêm.

## 6. Nghỉ toàn phòng khám và cả ngày

- Chọn Áp dụng cho = Cả phòng khám; hiển thị cảnh báo chặn slot của mọi bác sĩ.
- Chọn Nghỉ cả ngày: giờ được đưa về 00:00. Kiểm tra lại ngày kết thúc trước khi lưu.
- Nghỉ ngày 26/10: nhập từ **26/10/2026 00:00** đến **27/10/2026 00:00**.
- Kết thúc nghỉ không thuộc khoảng nghỉ; không nhập 23:59 để thay thế ngày kế tiếp.
- Bảng ghi Cả phòng khám, nhãn Toàn phòng khám và Cả ngày.
- Sau lưu: mọi bác sĩ có ca ngày đó không còn slot; ngày kế tiếp không bị khoảng nghỉ này chặn.

## 7. Lịch hẹn bị ảnh hưởng — cần kiểm tra kỹ

Dùng chức năng đặt lịch hiện có tạo một lịch hẹn local tương lai, rồi thay đổi lịch/nghỉ phủ lên lịch hẹn.

1. Bấm Lưu: API trả 409 và danh sách lịch hẹn. Form giữ nguyên, chưa lưu; xuất hiện bảng mã hẹn, bác sĩ, bệnh nhân, thời gian, trạng thái.
2. **Quay lại chỉnh sửa**: đóng cảnh báo, mở khóa form, không gửi request xác nhận. Đổi dữ liệu rồi Lưu lại để tính danh sách mới.
3. **Xác nhận và lưu**: gửi lại đúng request đã xem với `confirmAffectedAppointments=true`. Không tự thay đổi lịch hẹn.
4. Lưu thành công: form/cảnh báo đóng, thông báo thành công, bảng tải lại. Đối chiếu audit/notifications như hướng dẫn backend.
5. Nếu gửi xác nhận bị lỗi, giữ cảnh báo và báo lỗi, cho phép thử lại hoặc quay lại chỉnh sửa.

Trong lúc chờ API, nút lưu/xác nhận bị khóa để tránh bấm lặp.

## 8. Xóa đăng ký nghỉ

- Bấm Xóa: hiển thị tên bác sĩ/phạm vi và khoảng nghỉ để xác nhận.
- Bấm Hủy: không xóa.
- Bấm Xác nhận xóa: API DELETE; thành công đóng xác nhận và tải lại bảng nghỉ.
- Nếu vừa xóa bản cuối ở trang sau, quay về trang trước.
- Slot có thể được mở lại nếu không có nguyên nhân chặn khác. Lịch hẹn/thông báo cũ không bị xóa.

## 9. Phân quyền và lỗi kết nối

- Đăng xuất rồi mở `/admin/schedules`: không render trang quản trị.
- Đăng nhập Doctor/Patient/Receptionist/Pharmacist: không có menu VC20, truy cập trực tiếp bị chặn.
- Backend vẫn kiểm tra JWT Admin, không dựa riêng vào sidebar.
- Dừng API local sau khi đã tải trang, thử lọc/lưu: hiển thị lỗi, không báo lưu thành công. Khởi động lại API rồi thử lại.
- Không dùng mật khẩu/token thật để gửi kèm ảnh debug.

## 10. Kiểm tra tự động và production

```powershell
dotnet test .\tests\ClinicManagementSystem.UnitTests\ClinicManagementSystem.UnitTests.csproj -c Release
dotnet test .\tests\ClinicManagementSystem.IntegrationTests\ClinicManagementSystem.IntegrationTests.csproj -c Release --filter "FullyQualifiedName~AdminSchedulingAuthorizationTests|FullyQualifiedName~AdminSpecialtyAuthorizationTests"
dotnet build .\ClinicManagementSystem.slnx -c Release
```

Frontend tests dùng HTTP giả lập và Blazor HtmlRenderer, kiểm tra snapshot xác nhận, xử lý lỗi, chống submit lặp,
bộ lọc và render các form thực tế. Không thay thế kiểm tra click/nhập liệu trong browser với API và SQL local.
SQL integration tests đầy đủ cần Docker Desktop như tài liệu backend.

Sau khi UI local pass: xem diff, commit/push theo yêu cầu để CI/CD triển khai **cả API lẫn Web**.
Azure Web dùng `ApiSettings__BaseUrl` trỏ API deploy; không đổi Development sang Azure chỉ để triển khai.
Production đăng nhập Admin và kiểm tra menu/lọc trước. Nếu thử tạo/sửa/nghỉ, dùng bác sĩ và khoảng thời gian dành cho test,
kiểm tra lịch hẹn bị ảnh hưởng trước khi xác nhận; không thử trên ca đang phục vụ người dùng thực.
