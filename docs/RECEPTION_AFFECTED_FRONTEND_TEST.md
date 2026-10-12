# Kiểm tra frontend: lễ tân xử lý lịch hẹn bị ảnh hưởng

## 1. Chạy local

Mở hai terminal PowerShell tại `D:\_KhanhVy\ClinicManagementSystem`.

Terminal API:

```powershell
dotnet run --project .\src\ClinicManagementSystem.Api --launch-profile http
```

Terminal Web:

```powershell
dotnet run --project .\src\ClinicManagement.Web --launch-profile http
```

Web Development cần có `ApiSettings:BaseUrl = http://localhost:5238`. Giữ cấu hình local và Azure riêng; không đổi sang API production để chạy bài kiểm tra này.

Mở `http://localhost:5050/login`, đăng nhập tài khoản có role `Receptionist`.

Trang mới:

- `http://localhost:5050/reception/affected-appointments` — xử lý lịch bị ảnh hưởng.
- `http://localhost:5050/reception/notifications` — thông báo của lễ tân hiện tại.

Menu lễ tân có hai mục tương ứng; trang `/reception` cũng có liên kết đến danh sách cần xử lý.

## 2. Chuẩn bị dữ liệu kiểm tra

Dùng database local của bạn và tài khoản thử nghiệm. Cần bác sĩ, bệnh nhân đang hoạt động, tài khoản Admin và Receptionist. Không cần thay đổi schema.

1. Admin vào `/admin/schedules`: bảo đảm bác sĩ có ca làm ở hai ngày tương lai, ví dụ thứ Hai 19/10/2026 và thứ Ba 20/10/2026, 08:00–17:00, slot 30 phút. Nếu đã có ca làm thì dùng ca hiện tại, tránh thêm trùng.
2. Lễ tân vào `/reception/booking`: tạo hai lịch ở ngày đầu, chọn hai slot khác nhau. Ghi mã lịch, bệnh nhân, bác sĩ và giờ khám. Dùng lịch `Pending` hoặc `Confirmed`, chưa check-in, chưa thanh toán.
3. Admin đăng ký nghỉ cho bác sĩ vào ngày đầu. Nếu báo lịch hẹn bị ảnh hưởng, xem danh sách rồi xác nhận giữ các lịch hẹn để lễ tân xử lý.
4. Lịch hẹn vẫn còn giờ/trạng thái cũ. Ngày thứ hai phải có slot trống để thử đổi lịch.

Nếu đã chuẩn bị dữ liệu ở bước backend, có thể dùng lại các lịch còn bị ảnh hưởng. Xem [hướng dẫn backend](RECEPTION_AFFECTED_BACKEND_TEST.md) để kiểm tra bằng Swagger.

## 3. Thông báo và danh sách

1. Đăng nhập Receptionist, mở **Thông báo**, bấm **Làm mới**.
2. Thấy thông báo thay đổi ca/nghỉ và nội dung liên quan lịch hẹn. Chỉ hiển thị thông báo của tài khoản đang đăng nhập.
3. Bấm **Xem lịch cần xử lý**.
4. Chọn khoảng ngày chứa hai lịch thử, chọn bác sĩ, bấm **Lọc**.
5. Kiểm tra mã lịch, tên bệnh nhân, số điện thoại, bác sĩ, giờ khám, trạng thái và nguyên nhân ảnh hưởng.
6. Thử **Đặt lại**, lọc một bác sĩ, khoảng ngày không có lịch, phân trang khi có hơn 10 lịch. Khoảng ngày ngược hoặc dài hơn 91 ngày phải báo lỗi.

Khi đổi ô lọc, kết quả chỉ thay đổi sau khi bấm **Lọc**. **Làm mới** và phân trang dùng bộ lọc đã áp dụng.

## 4. Đổi lịch

1. Bấm **Đổi lịch** của lịch thứ nhất, kể cả khi đang ở cuối bảng: trang phải cuộn và focus đến tiêu đề form.
2. Kiểm tra thông tin bệnh nhân, số điện thoại, bác sĩ và giờ cũ.
3. Chọn ngày thứ hai. Giờ trống được tải từ API cho đúng bác sĩ.
4. Bấm một slot. Dòng **Giờ đã chọn** phải hiện đúng ngày và giờ.
5. Đổi sang ngày khác: giờ đã chọn phải được xóa. Chọn lại ngày thứ hai và slot mong muốn.
6. Thử bấm xác nhận khi chưa chọn slot, chưa ghi lý do, hoặc chưa đánh dấu đã liên hệ. Form phải báo lỗi và đưa focus đến phần lỗi.
7. Ghi lý do và kết quả liên hệ, đánh dấu đã thống nhất với bệnh nhân, bấm **Xác nhận đổi lịch**.
8. Kết quả: thông báo thành công được focus; form đóng; lịch đã hợp lệ biến mất khỏi danh sách cần xử lý.
9. Tại `/reception`, chọn ngày mới: kiểm tra đúng giờ mới, cùng mã lịch, bác sĩ, bệnh nhân và trạng thái cũ. Giờ kết thúc theo thời lượng slot của ca mới.
10. Đăng nhập Doctor kiểm tra thông báo đổi lịch. Nếu bệnh nhân có tài khoản, kiểm tra lịch cá nhân; thông báo bệnh nhân hiện có thể kiểm tra qua `GET /api/notifications` trong Swagger.

Chỉ cho đổi sang slot còn trống của **cùng bác sĩ**. Ngày bác sĩ nghỉ, giờ nghỉ trưa, slot đã có người đặt và giờ đã qua không được chọn. Nếu không có slot, giao diện hiển thị hướng dẫn chọn ngày khác/kiểm tra lịch bác sĩ.

## 5. Hủy lịch

1. Bấm **Hủy lịch** của lịch thứ hai. Trang phải focus đến form xác nhận hủy.
2. Kiểm tra mã lịch và thông tin bệnh nhân, nhập lý do, đánh dấu đã liên hệ rồi xác nhận.
3. Kết quả: thông báo thành công; form đóng; lịch biến mất khỏi danh sách bị ảnh hưởng.
4. Tại `/reception`, chọn ngày cũ, trạng thái **Đã hủy**: lịch vẫn tồn tại và có trạng thái hủy. Dữ liệu không bị xóa.
5. Doctor nhận thông báo hủy; bệnh nhân có tài khoản kiểm tra trạng thái lịch cá nhân/thông báo qua API.
6. Thử **Đóng** form: không gửi đổi/hủy; focus trở về nút thao tác của dòng.

Nếu lịch có `PaidAmount > 0`, API phải từ chối hủy và giao diện hiện thông báo cần xử lý thanh toán. Tính năng này không tự hoàn tiền hoặc sửa hóa đơn.

## 6. Hai lễ tân cùng thao tác

Dùng hai phiên trình duyệt, đăng nhập hai tài khoản Receptionist.

1. Cả hai tải cùng lịch và mở form trước khi xử lý.
2. Phiên A đổi hoặc hủy thành công.
3. Phiên B gửi form cũ: báo lịch đã cập nhật, không ghi đè kết quả của A. Form cũ đóng và danh sách được tải lại.
4. Nếu slot đích vừa được bệnh nhân khác đặt, API từ chối; kiểm tra lại danh sách, mở lại form và chọn slot mới.
5. Nếu Admin đã xóa ngày nghỉ/khôi phục ca làm trong lúc mở form, lịch có thể không còn bị ảnh hưởng. Gửi form cũ phải bị từ chối và danh sách tải lại.

Không tự gửi lại form cũ sau HTTP 409/404. Với lỗi mạng hoặc quá thời gian chờ, bấm **Làm mới** để kiểm tra lịch trước khi thử lại: máy chủ có thể đã hoàn thành thao tác trước khi mất kết nối.

## 7. Trường hợp chỉ xem và phân quyền

- Lịch bị ảnh hưởng nhưng đã check-in/quá giờ: hiện **Cần xử lý riêng** và lý do; không có nút đổi/hủy.
- Lịch đã xử lý hoặc trở lại hợp lệ: không còn trong danh sách. Thông báo cũ vẫn được giữ làm lịch sử.
- Patient/Doctor/Admin không có role Receptionist: không được truy cập hai trang lễ tân mới, API thao tác trả 403.
- Chưa đăng nhập: không được thao tác; API trả 401.
- Lỗi API: hiện lỗi; không hiển thị dữ liệu cũ như thể tải thành công.
- Kiểm tra màn hình hẹp, cuộn ngang bảng, tab bàn phím, đóng form và focus thông báo lỗi/thành công.

## 8. Chạy kiểm tra tự động

```powershell
dotnet test .\tests\ClinicManagementSystem.UnitTests\ClinicManagementSystem.UnitTests.csproj -c Release
dotnet test .\tests\ClinicManagementSystem.IntegrationTests\ClinicManagementSystem.IntegrationTests.csproj -c Release --filter "FullyQualifiedName~AuthorizationTests"
```

Tests frontend dùng HTTP giả lập và `HtmlRenderer` cho Razor thật; không gọi database. Kiểm tra tự động không thay thế thao tác trình duyệt với API/database local ở các bước trên.

## 9. Kiểm tra production sau khi local đạt

Sau khi local pass, commit/push những file của tính năng theo quy trình dự án và chờ CI, CD API, CD Web cùng xanh. Xác nhận Web Azure trỏ đến API Azure bằng `ApiSettings__BaseUrl` và cả hai App Service đang chạy.

Lặp lại các bước thông báo → danh sách → đổi → hủy bằng **tài khoản/dữ liệu thử riêng** và ngày tương lai trên production. Kiểm tra lại phân quyền, thông báo, hai phiên cập nhật cùng lúc và trạng thái lịch. Không dùng lịch bệnh nhân thật để kiểm tra hủy.

Việc commit, push, tạo dữ liệu production và deployment cần được thực hiện ở bước tiếp theo; chưa được chạy trong lần triển khai frontend này.

## 10. Các file frontend thay đổi

| File | Nội dung |
| --- | --- |
| [ReceptionAffectedAppointments.razor](../src/ClinicManagement.Web/Pages/Reception/ReceptionAffectedAppointments.razor) | Trang lọc lịch bị ảnh hưởng, đổi slot, xác nhận hủy và kiểm tra đã liên hệ |
| [ReceptionNotifications.razor](../src/ClinicManagement.Web/Pages/Reception/ReceptionNotifications.razor) | Trang thông báo của lễ tân, hiển thị giờ UTC+7 |
| [ReceptionAffectedAppointmentStateService.cs](../src/ClinicManagement.Web/Services/ReceptionAffectedAppointmentStateService.cs) | Gọi API bằng JWT, giữ phiên bản lịch đã xem, trạng thái tải/gửi và lỗi |
| [reception-affected.css](../src/ClinicManagement.Web/wwwroot/css/reception-affected.css) | CSS riêng trang xử lý lịch |
| [reception-notifications.css](../src/ClinicManagement.Web/wwwroot/css/reception-notifications.css) | CSS riêng trang thông báo |
| [reception-affected.js](../src/ClinicManagement.Web/wwwroot/js/reception-affected.js) | Focus và cuộn đến form/thông báo hoặc trở về nút thao tác |
| [_Host.cshtml](../src/ClinicManagement.Web/Pages/_Host.cshtml) | Link hai stylesheet và script |
| [Program.cs](../src/ClinicManagement.Web/Program.cs) | Đăng ký state service scoped |
| [HomePageMaster.razor](../src/ClinicManagement.Web/Shared/HomePageMaster.razor) | Hai mục menu lễ tân mới |
| [Index.razor](../src/ClinicManagement.Web/Pages/Reception/Index.razor) | Liên kết từ bàn tiếp đón đến danh sách cần xử lý |
| [ReceptionAffectedAppointmentStateServiceTests.cs](../tests/ClinicManagementSystem.UnitTests/Web/ReceptionAffectedAppointmentStateServiceTests.cs) | Kiểm tra yêu cầu HTTP, phiên bản, slot, lỗi và gửi đồng thời |
| [ReceptionAffectedRenderingTests.cs](../tests/ClinicManagementSystem.UnitTests/Web/ReceptionAffectedRenderingTests.cs) | Render Razor thật, validate form, xử lý kết quả, focus và phân quyền trang |
| [RECEPTION_AFFECTED_FRONTEND_TEST.md](RECEPTION_AFFECTED_FRONTEND_TEST.md) | Hướng dẫn local/production và danh sách file |
