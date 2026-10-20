# Bệnh án của tôi — kiểm thử

## Phạm vi

- Web: `/doctor/medical-records`, role Doctor. Menu **Bệnh án của tôi**.
- API: `GET /api/doctors/me/medical-records`.
- Bác sĩ xác định qua UserId trong JWT và hồ sơ Doctor đang hoạt động.
- Tất cả/Bản nháp/Đã chốt chỉ gồm bệnh án bác sĩ phụ trách.
- Ngày lọc là `scheduling.appointments.start_time`, không phải ngày tạo bệnh án.
- Dùng bảng hiện có, không có script đổi schema.

## Chạy local

Từ thư mục repo, mở hai terminal:

```powershell
dotnet run --project src/ClinicManagementSystem.Api
```

```powershell
dotnet run --project src/ClinicManagement.Web
```

Mở `http://localhost:5050/doctor/medical-records`, đăng nhập Doctor đang liên kết hồ sơ.
API/Swagger local theo launch profile hiện có: `http://localhost:5238/`.

## Chuẩn bị dữ liệu

Sử dụng tài khoản và dữ liệu thử riêng: hai bác sĩ A/B; bệnh án của A gồm Draft + lịch CheckedIn,
Finalized + lịch Completed, Draft + lịch Cancelled và Draft + lịch NoShow.
B có bệnh án riêng để đối chiếu phạm vi. Không đổi trạng thái bằng SQL để chạy thử nghiệp vụ.

Dữ liệu mẫu cũ: bệnh án #2 gắn lịch #3 đã hủy nên chỉ xem; các ID này chỉ là ví dụ,
cần đối chiếu trạng thái thực tế trên môi trường đang test.

## Danh sách và bộ lọc

1. Đăng nhập A: chỉ thấy bệnh án A, không thấy của B.
2. Tab Tất cả/Bản nháp/Đã chốt trả đúng trạng thái; đổi tab về trang 1.
3. Tìm mã bệnh án có/không có `#`, mã bệnh nhân và tên bệnh nhân.
4. Lọc ngày khám: kiểm tra lịch khám và ngày tạo khác nhau, hai đầu ngày đều được tính.
5. Không nhập ngày: tra cứu toàn bộ thời gian. Nhập thiếu một đầu, đảo ngày hoặc quá 366 ngày: báo lỗi.
6. Đổi số dòng 10/20/50, chuyển trang: tổng số theo bộ lọc và thứ tự ổn định.
7. Sửa bộ lọc chưa tra cứu: hiển thị nhắc; phân trang bị khóa đến khi tra cứu lại.
8. API lỗi hoặc phiên hết quyền: không để lại danh sách bệnh nhân cũ; có thông báo rõ ràng.

## Mở lại và chỉ xem

1. Draft + CheckedIn: **Tiếp tục khám** mở `/doctor/exam/{id}`; sửa, lưu, mở lại thấy dữ liệu đã lưu.
2. Đơn thuốc đã lưu được nạp lại khi mở màn hình đơn thuốc, có thể xem/xuất PDF theo quyền hiện có.
3. Finalized: **Xem chi tiết**; trường nhập, lưu/chốt và thao tác ghi CLS/upload bị khóa.
4. Draft + Cancelled/NoShow: hiển thị lý do chỉ xem. Chỉ thị trạng thái bệnh án vẫn là Draft.
5. Gọi thẳng các API lưu nháp, kê đơn, thêm/hủy CLS, nhập kết quả, upload và chốt với lịch bị khóa:
   API từ chối; không tạo hóa đơn, không đổi trạng thái, không upload tệp.
6. Bác sĩ B nhập URL chi tiết bệnh án A: không nhận được nội dung bệnh án.
   Gọi URL PDF đơn thuốc của A bằng tài khoản B cũng bị từ chối.
7. Đọc danh sách/chi tiết không chốt bệnh án hoặc tạo hóa đơn.

## Kiểm tra API

```text
GET /api/doctors/me/medical-records?status=0&keyword=BN&page=1&pageSize=20
GET /api/doctors/me/medical-records?from=2026-10-01&to=2026-10-31
```

- Không token: 401; chỉ có Patient/Receptionist/Pharmacist/Admin: 403.
- Doctor có hồ sơ ngừng hoạt động/không liên kết: không đọc được danh sách.
- Truyền `doctorId` không thay đổi phạm vi được xác định từ tài khoản.
- `status`: bỏ trống hoặc 0/1; `page >= 1`, `pageSize` từ 1–100; từ khóa tối đa 100 ký tự.
- Admin → Nhật ký truy cập: một lượt đọc danh sách gắn đúng các bệnh án thực sự trả về trong trang đó.

## Production

Sau khi được yêu cầu commit/push/deploy: đưa API lên trước rồi Web, test bằng tài khoản thử.
Lặp lại lọc, phân quyền, chỉ xem, mở lại Draft, audit và upload/download Azure Blob.
Không cần cấu hình thêm biến môi trường hoặc chuyển dữ liệu cho tính năng này.
