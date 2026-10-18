# Lưu tệp đính kèm VC14 và VC05

## Luồng backend

- Doctor upload bằng `POST /api/medical-records/{id}/attachments`, multipart field `file`.
- Chỉ bác sĩ đang phụ trách bệnh án Draft được upload. Giới hạn tệp 10 MiB; định dạng jpg, jpeg, png, pdf, dcm, dicom. MIME lưu theo phần mở rộng được hỗ trợ, không lấy tùy ý từ client. Đây là kiểm tra định dạng theo tên tệp, chưa phải quét virus hay kiểm tra nội dung nhị phân.
- SQL `clinical.attachments.file_url` lưu khóa dạng `medical-records/{recordId}/{guid}.pdf`, không lưu SAS hay URL public. Không cần đổi schema.
- Patient tải bằng `GET /api/attachments/{id}/download`. Kiểm tra bệnh nhân đang hoạt động và sở hữu bệnh án trước khi gọi storage. API trả nội dung tệp, không redirect ra Blob.
- VC24 vẫn audit truy cập và chặn `/uploads/medical-records/...` trực tiếp. Quyền Admin xem audit không cấp quyền download bệnh án.
- Upload lưu metadata thất bại: kiểm tra lại SQL, chỉ xóa tệp mới khi xác nhận chưa có metadata. Nếu SQL đã commit trước timeout thì giữ tệp. Nếu không thể xác nhận hoặc cleanup cũng lỗi, giữ tệp và ghi log yêu cầu đối soát bằng StorageKey. Không có API xóa attachment cho người dùng.

## Local trước khi cấu hình Azure

Mặc định `FileStorage:Provider=Local`. Nếu `LocalRootPath` trống, dùng thư mục dữ liệu của tài khoản chạy API: `%LOCALAPPDATA%\ClinicManagementSystem\medical-records` trên Windows. Có thể đặt thư mục riêng bằng biến môi trường trong terminal chạy API:

```powershell
cd D:\_KhanhVy\ClinicManagementSystem
$env:FileStorage__Provider = 'Local'
$env:FileStorage__LocalRootPath = 'D:\ClinicManagementData\medical-records'
dotnet run --project src/ClinicManagementSystem.Api
```

Các biến `$env:` chỉ áp dụng cho terminal hiện tại. Để giữ đường dẫn khi mở terminal mới, thêm section sau vào `src/ClinicManagementSystem.Api/appsettings.Development.json`, giữ nguyên các section khác:

```json
"FileStorage": {
  "Provider": "Local",
  "LocalRootPath": "D:\\ClinicManagementData\\medical-records"
}
```

File Development được Git ignore. Biến môi trường vẫn ưu tiên hơn cấu hình JSON. Sau khi đổi cấu hình storage, restart API để áp dụng.

Thư mục phải là đường dẫn tuyệt đối ngoài source API, tài khoản chạy API có quyền đọc/ghi. Tệp mới được ghi dưới `{LocalRootPath}/{recordId}/{guid}.{extension}`. Chỉ cấu hình backend; không cần Azure hoặc đổi BaseUrl Web khi test local.

Tệp cũ dùng `/uploads/medical-records/{recordId}/{name}` vẫn được đọc từ `wwwroot/uploads/medical-records`. Đường dẫn tuyệt đối cũ chỉ được chấp nhận nếu nằm trong đúng thư mục storage hoặc thư mục uploads cũ và đúng recordId. Đường dẫn ra ngoài storage bị từ chối. Nhánh tương thích cũ không ghi thêm tệp vào wwwroot.

## Test backend local

1. Chạy unit tests và integration tests cho attachment/audit. Các SQL integration test của toàn solution cần Docker SQL Server.
2. Đăng nhập Doctor qua Swagger, dùng JWT. Chọn bệnh án Draft thuộc bác sĩ, upload một PDF/PNG nhỏ. Response có khóa tương đối, file xuất hiện trong thư mục private và có metadata trong SQL.
3. Đăng nhập Patient sở hữu bệnh án, gọi download theo **attachmentId** vừa nhận. So sánh nội dung file tải về với file gốc.
4. Patient khác không tải được; chưa đăng nhập bị 401, sai role bị 403. Doctor khác bị 403 khi upload; bệnh án đã chốt bị 409.
5. Tệp trống/đuôi không được hỗ trợ bị 400; tệp lớn hơn 10 MiB bị 413. Tệp đúng 10 MiB vẫn nằm trong giới hạn; tổng request lớn hơn 11 MiB có thể bị web server chặn trước controller.
6. Dừng rồi chạy lại API, tải lại tệp vừa upload. Tải một tệp cũ trong wwwroot để kiểm tra tương thích.
7. Mở Admin audit, kiểm tra lượt upload/download tương ứng; gọi URL uploads trực tiếp vẫn trả 404.

Nếu download trả `404`, kiểm tra đúng **attachmentId**, Patient sở hữu bệnh án và tệp còn tồn tại. Nếu tệp vẫn ở ổ D nhưng `LocalRootPath` trống sau khi mở terminal mới, API có thể đang tìm ở thư mục mặc định trên ổ C. Đặt lại đúng thư mục storage, restart API rồi tải lại; không đổi role hoặc quyền sở hữu để bỏ qua kiểm tra.

## Azure trước khi deploy production

1. Tạo Storage Account, tắt anonymous Blob access; tạo container **private** `medical-records`. API không tự tạo container hoặc đổi quyền public.
2. Bật Managed Identity cho App Service API. Gán role `Storage Blob Data Contributor` tại phạm vi container để API upload/download và cleanup tệp mới.
3. Đặt App Service Application Settings:

```text
FileStorage__Provider=AzureBlob
FileStorage__AzureBlob__ServiceUri=https://<storage-account>.blob.core.windows.net
FileStorage__AzureBlob__ContainerName=medical-records
```

Nếu dùng user-assigned identity, thêm `FileStorage__AzureBlob__ManagedIdentityClientId`. Không đặt connection string/khóa trong Git hoặc Web. Backend dùng Azure Identity; khi test Azure từ máy dev có thể đăng nhập Azure CLI với tài khoản có quyền vào container.

4. Sao lưu metadata và file production. Tạm dừng upload trong thời gian chuyển; copy file từ mỗi thư mục `{recordId}/{name}` thành Blob key `medical-records/{recordId}/{name}`. Giữ nguyên tên, kiểm tra đủ số lượng và nội dung trước khi đổi provider.
5. URL cũ `/uploads/medical-records/...` được chuẩn hóa thành cùng Blob key nên không cần sửa metadata của các URL tương đối này. Nếu database có đường dẫn tuyệt đối cũ, lập danh sách và chuyển tham chiếu có kiểm soát trước khi dùng Blob; Blob provider không đọc ổ đĩa cũ.
6. Đổi provider sau khi copy xong. Test file cũ và file mới, đúng/sai chủ hồ sơ, audit và restart/redeploy. Giữ bản sao cũ đến khi xác nhận hoàn tất. Nếu rollback về Local, phải copy ngược cả tệp đã phát sinh trên Blob để không mất khả năng tải.

## Test Blob trên máy bằng Azurite nếu cần

Chỉ trong Development, đặt `FileStorage__Provider=AzureBlob` và `FileStorage__AzureBlob__ConnectionString=UseDevelopmentStorage=true`, khởi động Azurite và tạo container private trước. Cấu hình ConnectionString bị từ chối ngoài Development. Test này không thay thế kiểm tra Managed Identity/quyền truy cập trên Azure thật.

Tài liệu SDK: [Azure Blob Storage với .NET](https://learn.microsoft.com/en-us/azure/storage/blobs/storage-quickstart-blobs-dotnet).
