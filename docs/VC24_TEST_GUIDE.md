# VC24 — Nhật ký truy cập bệnh án

## 1. Phạm vi đã triển khai

- Trang Admin `/admin/audit-logs`, menu bên trái và thẻ trên trang `/admin`.
- Tra cứu theo đối tượng, mã đối tượng, người dùng và khoảng ngày; phân trang **theo lượt truy cập**; xem chi tiết chỉ đọc. Một lượt tải bốn bệnh án hiển thị một dòng “4 bệnh án”.
- Phần chi tiết dùng nhãn tiếng Việt, liệt kê các mã bệnh án/log gốc; JSON kỹ thuật không hiển thị trên giao diện; metadata và log gốc vẫn được lưu trong database.
- Middleware ghi truy cập dữ liệu y tế và kết quả HTTP; MVC result filter lưu log trước khi trả JSON/tệp cho client. Ghi cả truy cập bị từ chối `401/403` và lỗi `400/404/500` trên endpoint được theo dõi.
- Log mới lưu UTC; giao diện hiển thị giờ Việt Nam (GMT+7). Ngày kết thúc được tính hết ngày.
- Dùng bảng `auth.audit_logs` đã có. Không cần migration hoặc seed riêng cho VC24.
- Chặn tải trực tiếp `/uploads/medical-records/...` để không bỏ qua kiểm tra quyền và audit; dùng endpoint `/api/attachments/{id}/download` theo luồng hiện có. Log lần thử tải trực tiếp là `DIRECT_ATTACHMENT_ACCESS_DENIED`, trả `404`.
- Sửa việc resolve URL tệp `/uploads/...` sang thư mục `wwwroot` khi tải qua AttachmentService, giữ kiểm tra ownership và chặn URL upload đi ra ngoài thư mục tệp y tế.
- Không có API sửa/xóa log; `DbContext.SaveChanges` chặn thay đổi hoặc xóa AuditLog qua EF đang tracking. Vẫn cho phép thêm log.

## 2. Chạy kiểm thử tự động

Mở PowerShell ở thư mục dự án:

```powershell
Set-Location D:\_KhanhVy\ClinicManagementSystem
dotnet build .\ClinicManagementSystem.slnx -c Release
dotnet test .\tests\ClinicManagementSystem.UnitTests\ClinicManagementSystem.UnitTests.csproj -c Release --no-build
dotnet test .\tests\ClinicManagementSystem.IntegrationTests\ClinicManagementSystem.IntegrationTests.csproj -c Release --no-build --filter "FullyQualifiedName~AuthorizationTests|FullyQualifiedName~MedicalAuditPipelineTests"
```

Các kiểm thử HTTP trên dùng host API thật với dịch vụ/persistence giả, không truy cập database của bạn. Chúng kiểm tra routing, JWT, phân quyền, log bệnh án, PDF, kết quả xét nghiệm, lỗi ghi log và việc không lưu nội dung nhạy cảm.

Muốn kiểm thử SQL Server thật, mở Docker Desktop, chờ engine chạy rồi dùng:

```powershell
dotnet test .\tests\ClinicManagementSystem.IntegrationTests\ClinicManagementSystem.IntegrationTests.csproj -c Release --filter "FullyQualifiedName~AuditLogDatabaseIntegrationTests"
```

SQL tests tạo database riêng trong SQL Server Testcontainer, không dùng database local/shared/Azure. Nếu Docker chưa chạy, bộ kiểm thử này không thể thực thi. Không coi kết quả HTTP giả là đã kiểm chứng persistence trên SQL Server.

## 3. Khởi động local

Kiểm tra cấu hình Development hiện có: API trỏ vào database local của bạn; Web `ApiSettings:BaseUrl` trỏ vào API local. Không đổi cấu hình production để test local.

Terminal API:

```powershell
dotnet run --project .\src\ClinicManagementSystem.Api\ClinicManagementSystem.Api.csproj --launch-profile http
```

Terminal Web:

```powershell
dotnet run --project .\src\ClinicManagement.Web\ClinicManagement.Web.csproj --launch-profile http
```

- API/Swagger Development: `http://localhost:5238/` (Swagger UI nằm ở trang gốc).
- Web: `http://localhost:5050/`.
- Trang VC24: `http://localhost:5050/admin/audit-logs`.

Nếu đang chạy bản cũ, dừng terminal bằng Ctrl+C, chạy lại API và Web rồi tải lại trình duyệt. Menu/thẻ Admin mới xuất hiện sau khi Web chạy assembly mới.

## 4. Tạo log bằng thao tác y tế thật trên local

Chỉ dùng tài khoản và bệnh nhân kiểm thử trong database local.

1. Đăng nhập Doctor. Mở một bệnh án mà bác sĩ có quyền xem, ghi lại mã bệnh án.
2. Lưu nháp một thay đổi nhỏ trên bệnh án test. Nếu cần kiểm tra đọc thuần túy, chỉ mở bệnh án và bỏ qua thao tác lưu.
3. Đăng nhập Patient ở trình duyệt khác, xem lịch sử bệnh án của chính tài khoản này.
4. Đăng nhập Receptionist và xem danh sách/chi tiết bệnh nhân, lịch sử bệnh án theo luồng hiện có.
5. Đăng nhập Admin, vào **Nhật ký truy cập bệnh án**, chọn **Bệnh án**, chọn ngày hôm nay rồi bấm **Tra cứu**.
6. Mong đợi log `VIEW_RECORD`, `UPDATE_RECORD`, `VIEW_RECORD_HISTORY` tương ứng. Mã đối tượng là mã bệnh án; mã lịch hẹn/bệnh nhân liên quan nằm trong metadata tham chiếu, không bị ghi nhầm thành mã bệnh án.
7. Mở **Chi tiết**, kiểm tra người dùng, kết quả, UTC/GMT+7, IP, HTTP method, endpoint template và request ID. Không có triệu chứng/chẩn đoán/ghi chú, số điện thoại, mật khẩu hoặc token trong log truy cập mới.
8. Thử URL tĩnh của tệp test `/uploads/medical-records/{medicalRecordId}/{fileName}` trên API: phải `404`, không nhận nội dung tệp. Chọn đối tượng **Tệp đính kèm** để thấy lần thử bị chặn; tên tệp không được ghi trong Details.

Database vẫn lưu một log gốc cho mỗi bệnh án được trả về. Giao diện gộp các log mới cùng `requestId` và cùng người dùng/hành động/đối tượng/kết quả/IP/thời gian lưu thành một lượt. Các lần gọi khác nhau của cùng một người vẫn là các lượt riêng. Log cũ thiếu metadata nhận diện request giữ riêng từng dòng. Không sửa/xóa lịch sử để gộp.

Ví dụ: hai lần tải lịch sử, mỗi lần trả #1, #8, #9, #10 → database có tám log gốc, bảng Admin hiển thị hai lượt, mỗi lượt “4 bệnh án”. Bấm Chi tiết thấy đủ bốn mã bệnh án và mã từng log gốc.

Danh sách rỗng/lần truy cập bị từ chối vẫn có log với mã đối tượng rỗng hoặc mã được yêu cầu. Một request có thể có cả log nghiệp vụ cũ và log truy cập mới; log nghiệp vụ cũ vẫn hiển thị riêng. Khi kiểm tra kỹ thuật qua API/database, phân biệt bằng `source = VC24_MEDICAL_ACCESS` trong metadata.

## 5. Test từng endpoint tra cứu

Đăng nhập Admin qua Swagger/Postman, dùng access token của Admin trong `Authorization: Bearer ...`. Không dán token vào Git hay tài liệu.

| Method | Endpoint | Kết quả mong đợi |
| --- | --- | --- |
| GET | `/api/admin/audit-logs/accesses?entity=medical_records&from=2026-10-14&to=2026-10-14&page=1&pageSize=20` | Danh sách gộp lượt dùng cho Web; tổng/trang tính theo lượt, không theo số log gốc |
| GET | `/api/admin/audit-logs/accesses/{id}` | Chi tiết lượt có toàn bộ Entries, số đối tượng và metadata dễ đọc; `{id}` lấy từ danh sách gộp |
| GET | `/api/admin/audit-logs?entity=medical_records&from=2026-10-13&to=2026-10-13&page=1&pageSize=20` | `200`, envelope `HttpResponseData<PagedResult<AuditLogDTO>>`; thay ngày bằng ngày test |
| GET | `/api/admin/audit-logs?entity=medical_records&entityId=1204&userId=7&page=1&pageSize=20` | Chỉ log của đối tượng/tài khoản được chọn; thay ID bằng dữ liệu local |
| GET | `/api/admin/audit-logs?page=1&pageSize=20` | Mọi đối tượng, mọi thời điểm; thứ tự thời gian giảm dần, cùng giờ thì ID giảm dần |
| GET | `/api/admin/audit-logs/{id}` | `200` với một log và Details; lấy `{id}` từ danh sách |
| GET | `/api/admin/audit-logs/users?keyword=&page=1&pageSize=50` | Các người dùng đã có log; hỗ trợ từ khóa tên/tài khoản hoặc ID |
| GET | `/api/admin/audit-logs/{id-khong-ton-tai}` | `404` |

Các API list không trả `Details`; chỉ endpoint chi tiết Admin mới trả trường này. API raw cũ vẫn giữ để tra cứu log gốc. Tài khoản đã khóa/soft delete vẫn xuất hiện trong lịch sử. Lọc mã đối tượng trên API gộp chọn các lượt có chứa mã đó, nhưng chi tiết vẫn giữ đầy đủ các đối tượng trong cùng lượt.

Test validation và phân quyền:

| Case | Kết quả |
| --- | --- |
| Không có token / token không hợp lệ | `401` |
| Doctor/Receptionist/Patient/Pharmacist gọi API audit | `403` |
| `page=0`, `pageSize=101`, `userId=0`, `entityId=0` | `400` |
| Chỉ truyền `from`, không truyền `to` | `400` |
| `to` nhỏ hơn `from`, hoặc khoảng ngày >366 ngày | `400` |
| Bộ lọc hợp lệ nhưng không có log | `200`, Items rỗng |
| POST list hoặc PUT/PATCH/DELETE log | `405`, không có chức năng ghi/sửa/xóa qua API audit |

## 6. Test giao diện

1. Trang `/admin` có thẻ **Nhật ký truy cập bệnh án**; sidebar có cùng mục. Cả hai mở `/admin/audit-logs`.
2. Mặc định chọn Bệnh án, từ đầu tháng đến hôm nay. Đổi từng bộ lọc, bấm **Tra cứu**, đối chiếu với API.
3. Chọn **Tất cả đối tượng** để xem cả log nghiệp vụ từ VC trước. Bộ lọc Bệnh án cũng nhận entity legacy `MedicalRecord`.
4. Đổi bộ lọc nhưng chưa Tra cứu: kết quả vẫn ghi rõ bộ lọc đã áp dụng, nút phân trang khóa đến khi tra cứu lại.
5. Với >20 lượt, kiểm tra Trước/Sau, tổng lượt và trang; một lượt có nhiều bệnh án không bị tách giữa hai trang. Mở lịch sử hai lần phải hiện hai lượt riêng.
6. Mở **Tìm người dùng trong nhật ký**, tìm bằng tài khoản/họ tên/ID. Với >50 người dùng, kiểm tra phân trang tìm kiếm và giữ người đang chọn.
7. Ở gần cuối bảng, bấm **Chi tiết**: trang cuộn/focus đến panel. Bấm **Đóng chi tiết**: focus trở về nút đã mở.
8. Chi tiết hiển thị **Thời gian, Người dùng, Thao tác, Đối tượng, Kết quả, Địa chỉ IP** và danh sách bệnh án/log gốc. Không hiển thị phương thức HTTP, mã yêu cầu, mã trạng thái HTTP, mục **Thông tin kỹ thuật** hoặc JSON gốc; metadata vẫn lưu ở backend để truy vết và gộp lượt truy cập. Không có nút sửa/xóa, không render HTML/script.
9. Xóa cả hai ngày → tra cứu mọi thời điểm. Xóa một ngày → thông báo validation. Kiểm tra ngày đảo ngược và khoảng quá dài.
10. Bộ lọc không có kết quả → trạng thái rỗng rõ ràng. API dừng/mất kết nối → thông báo lỗi. Không giữ chi tiết cũ nếu tải thất bại; `401/403` xóa dữ liệu nhạy cảm trong state.
11. Thu nhỏ cửa sổ: form xuống hàng; bảng có cuộn ngang bên trong vùng bảng, không kéo rộng cả trang.

## 7. Giới hạn cần biết

- Log cũ đã dùng lẫn `DateTime.Now` và `UtcNow`, nên giữ nguyên giá trị lưu và hiển thị nhãn chưa xác định múi giờ. Bộ lọc ngày dùng khoảng UTC quy đổi từ GMT+7; log cũ gần ranh giới ngày có thể không khớp ngày hiển thị. Bỏ cả hai ngày khi cần tìm log cũ. Không tự sửa lịch sử.
- IP là địa chỉ API quan sát được từ kết nối. Với Blazor Server/Azure proxy, thường là Web server/proxy, chưa chắc là IP trình duyệt. Không tin tùy ý `X-Forwarded-For`.
- Nếu audit persistence thất bại, API trả `503` trước khi cung cấp JSON/PDF y tế. Thao tác cập nhật có thể đã commit trong service trước đó; kiểm tra trạng thái trước khi thử lại. Không tuyên bố rollback khi chỉ ghi audit bị lỗi.
- `Succeeded` phản ánh kết quả HTTP của action, không chứng minh client đã nhận toàn bộ dữ liệu. Lỗi tạo response tiếp theo được ghi thêm với `phase = response_failed` khi có thể.
- Chặn sửa/xóa ở tầng EF tracking; không ngăn tài khoản DBA hoặc code raw SQL/bulk update tự thay đổi bảng. Chưa thêm trigger/phân quyền database vì VC24 sử dụng schema hiện có.
- Tải tệp trả dữ liệu file thay vì JSON, nên `returnedCount` kỹ thuật có thể bằng 0. Giao diện dùng mã đối tượng của log để thể hiện tệp đã tải; không dùng `returnedCount` làm số tệp.
- Phân quyền các luồng y tế giữ theo chức năng hiện tại. Middleware ghi log không thay thế kiểm tra quyền/ownership. Tra cứu nhật ký luôn yêu cầu Admin.

## 8. Các file chính để review

| Phần | File |
| --- | --- |
| DTO | `src/ClinicManagementSystem.Application/DTOs/Audit/AuditLogDTOs.cs` |
| Repository | `src/ClinicManagementSystem.Infrastructure/Repositories/AuditLogRepository.cs` |
| Service | `src/ClinicManagementSystem.Application/Services/AdminAuditLogService.cs`, `MedicalAuditWriter.cs`, `AttachmentService.cs` |
| API | `src/ClinicManagementSystem.Api/Controllers/AdminAuditLogController.cs` |
| Ghi truy cập | `src/ClinicManagementSystem.Api/Middleware/MedicalAuditEndpoint.cs`, `MedicalAuditMiddleware.cs` |
| Chặn sửa/xóa và hàm truy vấn | `src/ClinicManagementSystem.Infrastructure/Data/ClinicManagementDbContext.AuditProtection.cs`, `ClinicManagementDbContext.AuditQueryFunctions.cs` (mapping các hàm SQL có sẵn, không tạo schema) |
| State/Razor | `src/ClinicManagement.Web/Services/AdminAuditLogStateService.cs`, `Pages/Admin/AuditLogs.razor` |
| CSS/JS | `src/ClinicManagement.Web/wwwroot/css/admin-audit-logs.css`, `wwwroot/js/admin-audit-logs.js` |
| Tích hợp | API/Web `Program.cs`, Web `Pages/_Host.cshtml`, `Shared/HomePageMaster.razor`, `Pages/Admin/Index.razor` |
| Tests | UnitTests `Application/Audit`, `Infrastructure/Audit`, `Web/AdminAudit*`; IntegrationTests `Audit`, hai factory Authorization Reception/Scheduling và `CustomWebApplicationFactory.cs` |

## 9. Sau khi local pass

Review diff, commit/push theo quy trình CI/CD của dự án rồi chờ CI, CD API và CD Web xanh. Với production, kiểm tra bằng tài khoản test riêng và thao tác đọc trước; sau đó tra cứu Admin cùng ngày. Không cần tạo bảng mới/seed log mẫu trên Azure. Bản này chưa tự commit, push hoặc deploy.

## 10. Kết quả kiểm tra bản triển khai ngày 13/10/2026

- `dotnet build ClinicManagementSystem.slnx -c Release --no-restore`: thành công.
- Unit tests: **604/604 pass**.
- HTTP Authorization + MedicalAuditPipeline tests: **325/325 pass** (26 ca pipeline audit).
- `git diff --check`: không có lỗi whitespace.
- Browser fixture dùng Razor/layout/CSS thật và dữ liệu giả: đã kiểm tra sidebar/thẻ Admin, bộ lọc, phân trang, tìm người dùng/giữ lựa chọn, mở/đóng chi tiết và focus, nội dung script chỉ hiển thị dạng chữ, cửa sổ desktop và màn hình 390px.
- Ba SQL Server integration cases đã viết/build nhưng chưa chạy vì Docker engine chưa hoạt động. Chưa kiểm thử dữ liệu local thật hoặc production trong lượt này; thực hiện các bước 3–6 rồi mới chuyển sang deploy.

## 11. Bản hiển thị theo lượt truy cập ngày 14/10/2026

- Thêm GET `accesses` / `accesses/{id}`; API raw cũ và log gốc không đổi. Gộp trên SQL trước phân trang; lọc mã bệnh án vẫn mở đủ đối tượng của lượt.
- Build thành công; **620 unit tests và 338 HTTP tests pass**.
- Kiểm thử riêng trên SQL LocalDB của task: **8 kiểm tra pass**, gồm tách request/người dùng/kết quả/giờ lưu, legacy JSON lỗi, phân trang nhóm, lọc đối tượng, chi tiết đủ bệnh án, trang rất lớn và bảo toàn log gốc. Database test đã xóa sau kiểm tra; không dùng database ứng dụng/shared/Azure.
- Razor HtmlRenderer đã kiểm tra nhóm bốn bệnh án, hai request cùng người, tệp tải có `returnedCount: 0`, ẩn JSON gốc và focus. Công cụ browser không khởi động được ở lượt này, nên cần chạy các bước test giao diện trên local như mục 6.
- SQL Testcontainer case gộp đã thêm; Docker engine chưa hoạt động nên chưa chạy bộ container. Chưa commit/push/deploy.
