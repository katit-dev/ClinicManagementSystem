# VC23 — Test backend báo cáo doanh thu & lượt khám

Giai đoạn hiện tại: **backend → test Swagger local → frontend → test local → production**.

Backend cung cấp báo cáo JSON và file Excel. Trang `/admin/reports` sẽ được làm ở giai đoạn frontend sau khi bạn kiểm tra backend.

## 1. API và công thức

| Method | Endpoint | Kết quả | Quyền |
| --- | --- | --- | --- |
| GET | `/api/admin/reports?from=2026-10-01&to=2026-10-31` | `HttpResponseData<ClinicReportDTO>` | Admin |
| GET | `/api/admin/reports/export?from=2026-10-01&to=2026-10-31` | File `.xlsx` | Admin |

Cả hai endpoint hỗ trợ thêm `&doctorId=ID`. Bỏ `doctorId` để xem toàn phòng khám. Bác sĩ đã ngừng hoạt động vẫn được tính trong dữ liệu lịch sử.

| Chỉ số | Cách tính |
| --- | --- |
| `totalVisits` | Số lịch `Completed`, có `appointments.start_time` trong kỳ |
| `noShowCount` | Số lịch `NoShow`, có `appointments.start_time` trong kỳ |
| `concludedAppointments` | `totalVisits + noShowCount` |
| `noShowRate` | `NoShow / (Completed + NoShow) × 100`, làm tròn 2 chữ số; mẫu số 0 trả 0 |
| `collectedAmount` | Tổng tiền thu theo `payments.paid_at` trong kỳ |
| `refundedAmount` | Tổng trị tuyệt đối của các khoản hoàn trong kỳ |
| `totalRevenue` | `collectedAmount - refundedAmount`; có thể âm nếu kỳ chỉ có hoàn tiền |
| `outstandingDebt` | Công nợ của **hóa đơn phát hành trong kỳ**, sau khi trừ thanh toán đến hết ngày cuối kỳ |
| `outstandingInvoiceCount` | Số hóa đơn thuộc nhóm trên còn nợ lớn hơn 0 |

**Công thức không đến đã thống nhất:** 2 Completed + 1 NoShow → 2 lượt khám, tỷ lệ 33,33%. Pending, Confirmed, CheckedIn và Cancelled không nằm trong mẫu số. Tỷ lệ toàn phòng khám được tính từ tổng số lịch, không lấy trung bình tỷ lệ của từng bác sĩ.

### Doanh thu và phân bổ

- Hóa đơn phát hành tháng trước nhưng có thanh toán tháng này: tiền thanh toán vẫn thuộc doanh thu tháng này.
- Hóa đơn chưa thu tiền: chưa có doanh thu thực thu, dù đã có lượt khám Completed.
- Hoàn tiền đang được backend lưu với `amount < 0`; báo cáo trừ tiền hoàn đúng một lần. Dữ liệu cũ có `is_refund = 1` nhưng amount dương cũng được chuẩn hóa.
- Phí khám hiện nằm trong `invoices.total_amount`, không có một dòng `invoice_items` riêng. Phần tổng hóa đơn còn lại sau các dòng dịch vụ/thuốc được xếp vào phí khám khi hóa đơn có liên kết khám.
- Các dòng dịch vụ CLS và thuốc lấy **mô tả và giá trị snapshot trên invoice_items**, không đọc lại giá danh mục hiện tại. `invoice_items.amount` đã bao gồm giảm giá dòng; không trừ lại lần nữa.
- Khi thanh toán một phần, tiền thực thu được phân bổ theo tỷ trọng phí khám/CLS/thuốc trên hóa đơn. Thuế, giảm giá và bảo hiểm của hóa đơn cũng được chia theo tỷ trọng này. Đây là phân bổ tiền thực thu, không phải doanh thu ghi nhận theo toàn bộ giá trị dịch vụ đã làm.
- Sai số làm tròn được đưa vào phần cuối để tổng các nhóm luôn bằng `totalRevenue`.
- Hóa đơn không có liên kết bác sĩ vẫn nằm trong tổng toàn phòng khám, với `doctorId = null`, `doctorName = "Chưa gắn bác sĩ"`. Khoản không phân loại được có nhóm `Other`.
- Bác sĩ của hóa đơn lấy từ hồ sơ khám; nếu không có hồ sơ thì lấy từ lịch hẹn. Không lấy người thu tiền làm bác sĩ.

Ví dụ minh họa: hóa đơn có phí khám 500, CLS 300, thuốc 200; thuế 100, giảm giá 100, bảo hiểm 200 → phải thu 800. Đã thu 400 đến cuối kỳ → doanh thu 400, công nợ 400. Phân bổ doanh thu: khám 200, CLS 120, thuốc 80. Các số này là ví dụ, không phải dữ liệu database của bạn.

### Công nợ theo kỳ

```text
Phải thu = TotalAmount + TaxAmount - DiscountAmount - InsuranceAmount
Đã thu đến hết kỳ = tổng payments trước 00:00 ngày sau To, có trừ tiền hoàn
Công nợ = MAX(0, Phải thu - Đã thu đến hết kỳ)
```

Chỉ tính hóa đơn phát hành từ `From` đến hết `To`. **Không bao gồm nợ của hóa đơn phát hành trước From**; đây không phải số dư toàn bộ công nợ tích lũy.

Hóa đơn hủy trước khi kỳ kết thúc không còn nợ trong báo cáo. Nếu hóa đơn bị hủy sau kỳ đang xem, công nợ lịch sử của kỳ đó vẫn được giữ. Thanh toán sau `To` không làm giảm công nợ lịch sử. Vì vậy backend đọc sổ thanh toán, không dùng `invoices.paid_amount` hiện tại để tính số tiền đã thu tại một mốc quá khứ.

### Ngày giờ

`from` và `to` là ngày ISO `yyyy-MM-dd`, bắt buộc, tối đa 366 ngày mỗi báo cáo. Ngày `To` được tính cả ngày:

```text
2026-10-01 00:00:00 <= timestamp < 2026-11-01 00:00:00
```

Lọc theo timestamp đang lưu trong database, không tự cộng/trừ 7 giờ. Code thanh toán hiện có dùng `DateTime.Now`; dữ liệu local/Azure có thể khác múi giờ theo host ghi dữ liệu. Khi đối chiếu, xem `paid_at`, `created_at`, `start_time` thực tế; báo cáo không sửa hay chuẩn hóa lại các timestamps cũ. `generatedAtUtc` là thời điểm tạo báo cáo theo UTC.

## 2. Khởi động API local

Đảm bảo cấu hình Development trỏ đến database local của bạn. VC23 không cần seed mới hoặc thay đổi schema.

Trong terminal ở thư mục gốc dự án:

```powershell
dotnet run --project .\src\ClinicManagementSystem.Api\ClinicManagementSystem.Api.csproj --launch-profile http
```

Nếu API đang chạy bản cũ, dừng terminal bằng `Ctrl+C` rồi chạy lại lệnh trên. Nếu bạn chạy profile HTTPS, dùng `https://localhost:7201` thay cho HTTP.

Mở Swagger: [http://localhost:5238](http://localhost:5238).

## 3. Lấy token Admin

1. Mở `POST /api/auth/login` → **Try it out**.
2. Dùng tài khoản Admin local đã có:

```json
{
  "username": "admin.local@clinic.local",
  "password": "MAT_KHAU_ADMIN_LOCAL_CUA_BAN"
}
```

3. Copy `content.accessToken` từ response 200.
4. Bấm **Authorize** trong Swagger, dán token. Swagger dùng HTTP bearer và tự thêm tiền tố `Bearer`.
5. Mở `GET /api/admin/scheduling/doctors` để biết `id` bác sĩ có thật.

Dùng token mới sau khi role/tài khoản thay đổi vì VC22 kiểm tra access stamp. `401` cần kiểm tra token/hạn dùng/tài khoản; `403` nghĩa là tài khoản chưa có quyền Admin.

## 4. Test GET báo cáo

Mở `GET /api/admin/reports` → **Try it out**:

```text
from: 2026-10-01
to: 2026-10-31
doctorId: để trống
```

Chọn khoảng ngày chứa dữ liệu local của bạn, rồi bấm **Execute**. Kỳ vọng **200**, `statusCode = 200`, báo cáo nằm trong `content`.

Các trường chính:

```json
{
  "statusCode": 200,
  "message": "Tải báo cáo thành công.",
  "content": {
    "from": "2026-10-01",
    "to": "2026-10-31",
    "doctorId": null,
    "totalVisits": 2,
    "noShowCount": 1,
    "concludedAppointments": 3,
    "noShowRate": 33.33,
    "collectedAmount": 400,
    "refundedAmount": 0,
    "totalRevenue": 400,
    "outstandingDebt": 400,
    "outstandingInvoiceCount": 1,
    "consultationRevenue": 200,
    "serviceRevenue": 120,
    "medicineRevenue": 80,
    "otherRevenue": 0,
    "doctors": [],
    "revenueItems": []
  }
}
```

Đây là ví dụ rút gọn cấu trúc; trong báo cáo có dữ liệu, `doctors` và `revenueItems` chứa các dòng tương ứng, cùng `generatedAtUtc`.

Kiểm tra:

- `noShowRate = 33.33` nghĩa là **33,33%**, không phải 0,3333.
- `totalRevenue = collectedAmount - refundedAmount`.
- Tổng `doctors[*].totalRevenue` bằng doanh thu toàn báo cáo.
- Tổng `revenueItems[*].revenue` bằng doanh thu toàn báo cáo.
- Tổng khám + CLS + thuốc + khác bằng doanh thu toàn báo cáo.
- Tổng công nợ các bác sĩ, bao gồm nhóm chưa gắn bác sĩ, bằng công nợ toàn báo cáo.

Sau đó điền `doctorId` thật và chạy lại. Chỉ còn lịch/hóa đơn của bác sĩ đó; các trường From/To/DoctorId phải đúng bộ lọc đã gửi.

### Các trường hợp cần kiểm tra

| Trường hợp | Kết quả mong đợi |
| --- | --- |
| Chọn kỳ không có dữ liệu | 200, các tổng bằng 0, danh sách trống |
| Chọn bác sĩ có Completed nhưng chưa có thanh toán | Có lượt khám, doanh thu có thể bằng 0 |
| Có lịch Pending/Confirmed/CheckedIn/Cancelled | Không làm tăng lượt khám hoặc mẫu số tỷ lệ không đến |
| 2 Completed + 1 NoShow | 2 lượt khám, tỷ lệ 33,33% |
| Thanh toán một phần | Doanh thu chỉ bằng tiền đã thu; còn công nợ |
| Hóa đơn cũ được thanh toán trong kỳ | Có doanh thu trong kỳ; không đưa hóa đơn cũ vào nhóm công nợ phát hành trong kỳ |
| Có hoàn tiền trong kỳ | Tiền hoàn dương ở `refundedAmount`; trừ khỏi `totalRevenue` |
| Chỉ có hoàn tiền | `totalRevenue` có thể âm; không bị ép về 0 |
| Hủy hóa đơn đã hoàn tiền hết | Không còn công nợ; các khoản thu/hoàn vẫn được tính theo ngày thanh toán |
| Hóa đơn thanh toán hết sau To | Công nợ của kỳ trước vẫn còn đúng số tại cuối kỳ trước |
| Đổi giá/tên dịch vụ trong VC21 sau khi xuất hóa đơn | Báo cáo giữ giá trị/mô tả snapshot của hóa đơn cũ |
| Kỳ một ngày: From = To | Tính các timestamp thuộc ngày đó, không tính 00:00 ngày tiếp theo |

Nếu cần tạo giao dịch để kiểm tra, sử dụng **dữ liệu test local** và luồng khám/thanh toán hiện có. API hóa đơn hiện yêu cầu role `Receptionist`; tài khoản chỉ có Admin không gọi được API thanh toán.

Ví dụ thu tiền: `POST /api/invoices/{invoiceId}/payments`, dùng token Receptionist, số tiền không vượt số còn phải thu:

```json
{
  "amount": 100000,
  "method": 0,
  "note": "Test local VC23 thu một phần"
}
```

Ví dụ hoàn tiền: `POST /api/invoices/{invoiceId}/refunds`, amount gửi lên vẫn **dương**, không vượt số đã thu chưa hoàn:

```json
{
  "amount": 50000,
  "method": 0,
  "note": "Test local VC23 hoàn tiền"
}
```

Sau đó đăng nhập lại bằng Admin và chạy báo cáo. Không cần chỉnh ngày hay tiền trực tiếp bằng SQL để test luồng này.

## 5. Test GET xuất Excel

Mở `GET /api/admin/reports/export` với **cùng From/To/DoctorId** vừa dùng ở mục 4.

Kỳ vọng:

- HTTP **200**.
- Content-Type: `application/vnd.openxmlformats-officedocument.spreadsheetml.sheet`.
- Header `Content-Disposition: attachment`, tên file có From/To và bộ lọc bác sĩ.
- Swagger hiển thị liên kết tải file. Mở file tải về bằng Excel hoặc LibreOffice.

Workbook có 3 sheet:

| Sheet | Nội dung |
| --- | --- |
| Tổng quan | Bộ lọc, thời điểm UTC, chỉ số và công thức tính |
| Theo bác sĩ | Lượt khám, không đến, tỷ lệ, doanh thu từng nhóm và công nợ |
| Dịch vụ & thuốc | Tiền thực thu phân bổ theo nhóm, mã nguồn và mô tả snapshot |

Đối chiếu JSON và Excel khi dữ liệu không có giao dịch mới chen vào giữa hai lần gọi. Số tiền và số lượt là ô số; tỷ lệ hiển thị có `%`; tên tiếng Việt không lỗi dấu. File được tạo bằng gói OOXML theo [cấu trúc SpreadsheetML của Microsoft](https://learn.microsoft.com/en-us/office/open-xml/spreadsheet/structure-of-a-spreadsheetml-document), không thêm package Excel vào dự án.

Tên dịch vụ/thuốc bắt đầu bằng `=`, `+`, `-`, `@` vẫn được ghi như văn bản, không thành công thức Excel. Response dùng `Cache-Control: no-store`.

## 6. Test validation và phân quyền

Áp dụng các ca này cho **cả `/reports` và `/reports/export`**:

| Input/token | HTTP mong đợi |
| --- | --- |
| Chưa Authorize hoặc token hết hạn | 401 |
| Token Patient/Doctor/Receptionist/Pharmacist không có Admin | 403 |
| Token Admin hợp lệ, query đúng | 200 |
| Thiếu From hoặc To | 400 |
| `from=abc` hoặc ngày không hợp lệ | 400 |
| `from=2026-10-31&to=2026-10-01` | 400 |
| `from=2026-01-01&to=2027-01-02` (367 ngày) | 400 |
| `from=2026-01-01&to=2027-01-01` (366 ngày) | 200 |
| `doctorId=0`, `-1`, `abc` | 400 |
| DoctorId dương nhưng không tồn tại | 404 |

Lỗi validation của MVC trả JSON ProblemDetails; lỗi nghiệp vụ/service trả `HttpResponseData`. Endpoint export khi lỗi trả JSON đúng mã lỗi, không gửi file Excel giả. Lỗi hệ thống trả thông báo chung, không lộ connection string.

## 7. SQL đối chiếu chỉ đọc

Trong SSMS, chọn đúng database local rồi chạy các truy vấn **SELECT** dưới đây. Các câu lệnh này không seed và không sửa dữ liệu.

```sql
DECLARE @From datetime2 = '2026-10-01T00:00:00';
DECLARE @End datetime2 = '2026-11-01T00:00:00'; -- ngày sau To
DECLARE @DoctorId int = NULL; -- thay bằng id thật nếu lọc bác sĩ

-- Lượt khám và không đến, chưa join với hóa đơn/thanh toán.
SELECT
    SUM(CASE WHEN a.status = 3 THEN 1 ELSE 0 END) AS completed_count,
    SUM(CASE WHEN a.status = 5 THEN 1 ELSE 0 END) AS no_show_count
FROM scheduling.appointments a
WHERE a.start_time >= @From AND a.start_time < @End
  AND (@DoctorId IS NULL OR a.doctor_id = @DoctorId);

-- Tiền thu/hoàn theo thời điểm thanh toán, bao gồm hóa đơn cũ trả trong kỳ.
SELECT
    COALESCE(SUM(CASE WHEN p.is_refund = 0 AND p.amount > 0 THEN p.amount ELSE 0 END), 0) AS collected_amount,
    COALESCE(SUM(CASE WHEN p.is_refund = 1 OR p.amount < 0 THEN ABS(p.amount) ELSE 0 END), 0) AS refunded_amount
FROM billing.payments p
JOIN billing.invoices i ON i.id = p.invoice_id
LEFT JOIN clinical.medical_records mr ON mr.id = i.medical_record_id
LEFT JOIN scheduling.appointments a ON a.id = i.appointment_id
WHERE p.paid_at >= @From AND p.paid_at < @End
  AND (@DoctorId IS NULL OR COALESCE(mr.doctor_id, a.doctor_id) = @DoctorId);

-- Nợ của hóa đơn phát hành trong kỳ, với thanh toán tính đến hết kỳ.
SELECT i.id, i.invoice_no,
       i.total_amount + i.tax_amount - i.discount_amount - i.insurance_amount AS payable,
       ledger.paid_through_end,
       CASE WHEN i.total_amount + i.tax_amount - i.discount_amount - i.insurance_amount - ledger.paid_through_end > 0
            THEN i.total_amount + i.tax_amount - i.discount_amount - i.insurance_amount - ledger.paid_through_end
            ELSE 0 END AS outstanding_debt
FROM billing.invoices i
LEFT JOIN clinical.medical_records mr ON mr.id = i.medical_record_id
LEFT JOIN scheduling.appointments a ON a.id = i.appointment_id
OUTER APPLY (
    SELECT COALESCE(SUM(CASE WHEN p.is_refund = 1 THEN -ABS(p.amount) ELSE p.amount END), 0) AS paid_through_end
    FROM billing.payments p
    WHERE p.invoice_id = i.id AND p.paid_at < @End
) ledger
WHERE i.created_at >= @From AND i.created_at < @End
  AND (i.status <> 3 OR i.cancelled_at >= @End)
  AND (@DoctorId IS NULL OR COALESCE(mr.doctor_id, a.doctor_id) = @DoctorId);
```

Tổng cột `outstanding_debt` phải bằng báo cáo; số dòng có nợ > 0 phải bằng `outstandingInvoiceCount`. Nếu dữ liệu seed cũ có `paid_amount` nhưng không có payments tương ứng, báo cáo theo payments sẽ khác số tổng lưu trên invoice; đối chiếu sổ thanh toán trước.

## 8. Tests tự động

Chạy trong thư mục gốc dự án:

```powershell
dotnet build .\ClinicManagementSystem.slnx -c Release
dotnet test .\tests\ClinicManagementSystem.UnitTests\ClinicManagementSystem.UnitTests.csproj -c Release --no-build
dotnet test .\tests\ClinicManagementSystem.IntegrationTests\ClinicManagementSystem.IntegrationTests.csproj -c Release --no-build --filter "FullyQualifiedName~AuthorizationTests"
```

Riêng SQL Server container, cần Docker Desktop đang chạy Linux containers:

```powershell
dotnet test .\tests\ClinicManagementSystem.IntegrationTests\ClinicManagementSystem.IntegrationTests.csproj -c Release --filter "FullyQualifiedName~ClinicReportDatabaseIntegrationTests"
```

Kết quả đã chạy khi hoàn thiện backend VC23:

- Build solution Release: thành công. Các warnings nullable/using có sẵn ở chức năng khác được giữ nguyên.
- Toàn bộ unit tests: **458 passed**; trong đó **44 tests VC23**.
- HTTP tests phân quyền: **250 passed**; trong đó **30 tests VC23**.
- Queries báo cáo đã kiểm tra dịch sang SQL Server bằng `ToQueryString()` mà không mở kết nối DB.
- Workbook mẫu đã được đọc bằng `openpyxl`: đủ 3 sheet, số tiền/tỷ lệ đúng, tiếng Việt đúng, nội dung bắt đầu bằng `=` vẫn là văn bản.
- 2 SQL Server container tests đã biên dịch, **chưa chạy** do Docker engine chưa hoạt động. Chưa xác nhận truy vấn với SQL Server thực tế; bước Swagger local của bạn sẽ kiểm tra với database local.

## 9. File thay đổi

| File | Nội dung |
| --- | --- |
| `src/ClinicManagementSystem.Api/Controllers/AdminReportController.cs` | 2 endpoint Admin, export binary và không cache |
| `src/ClinicManagementSystem.Api/Program.cs` | Đăng ký report repository/service |
| `src/ClinicManagementSystem.Application/DTOs/Report/ClinicReportQueryDTO.cs` | Query ngày/bác sĩ và validation |
| `src/ClinicManagementSystem.Application/DTOs/Report/ClinicReportDTO.cs` | Report, dòng bác sĩ, dòng doanh thu, file export |
| `src/ClinicManagementSystem.Application/Services/AdminReportService.cs` | Tổng hợp chỉ số, công nợ và phân bổ tiền thực thu |
| `src/ClinicManagementSystem.Application/Helpers/ClinicReportExcelWriter.cs` | Tạo `.xlsx` 3 sheet bằng OOXML |
| `src/ClinicManagementSystem.Infrastructure/Models/Reports/ClinicReportData.cs` | Read projections; không phải bảng database |
| `src/ClinicManagementSystem.Infrastructure/Repositories/ClinicReportRepository.cs` | Truy vấn appointments/invoices/payments/items |
| `src/ClinicManagementSystem.Infrastructure/UnitOfWork/UnitOfWork.cs` | Thêm report repository vào UoW |
| `tests/ClinicManagementSystem.UnitTests/Application/Report/AdminReportServiceTests.cs` | Công thức, validation và export |
| `tests/ClinicManagementSystem.UnitTests/Application/Report/ClinicReportExcelWriterTests.cs` | Cấu trúc/nội dung Excel |
| `tests/ClinicManagementSystem.UnitTests/Infrastructure/Report/ClinicReportRepositoryTests.cs` | Bộ lọc, tổng payments và SQL translation |
| `tests/ClinicManagementSystem.IntegrationTests/Report/AdminReportAuthorizationTests.cs` | JWT, Admin, model binding và tải file |
| `tests/ClinicManagementSystem.IntegrationTests/Report/ClinicReportDatabaseIntegrationTests.cs` | 2 tests database riêng trong SQL Server container |
| `docs/VC23_BACKEND_TEST.md` | Hướng dẫn này |

Không cần migration, đổi appsettings, hoặc seed riêng cho VC23. Khi các kiểm tra Swagger ở trên đạt, bước tiếp theo là frontend theo cấu trúc StateService → Razor page → CSS riêng, link trong `_Host.cshtml`. Sau frontend mới kiểm tra local toàn luồng và chuẩn bị commit/deploy production.
