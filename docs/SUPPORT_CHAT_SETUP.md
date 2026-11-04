# Hỗ trợ trực tuyến — cài đặt và kiểm thử

Module dùng ba bảng SQL và SignalR Hub trực tiếp trong API. Không cần tạo Azure SignalR Service cho phương án một API instance đã thống nhất. Không có worker, chatbot, upload tệp chat hoặc gọi video.

## 1 Chức năng

- Bệnh nhân: hai nút nổi chat/hotline trên giao diện bệnh nhân và các trang công khai khi đã đăng nhập; yêu cầu hiện tại, lịch sử phân trang, tin chưa đọc, kết thúc yêu cầu. Không đặt widget trên các trang nội bộ của tài khoản có nhiều role.
- Lễ tân: `/reception/support`; hàng chờ chung, tự tiếp nhận, trả lời, nhật ký, trả về hàng chờ, kết thúc và lịch sử do mình phụ trách cuối.
- Admin: `/admin/support`; metadata, chọn lễ tân, giao/chuyển/trả/đóng và nhật ký. Không có API đọc nội dung hoặc gửi tin dưới vai trò Admin.
- Mỗi yêu cầu mới có một lời xác nhận tự động ngay sau tin đầu. Tin này hiển thị khung nhạt, không thêm nhãn riêng và không tính chưa đọc.
- Giờ hỗ trợ: **giờ hành chính hằng ngày, gồm Thứ Bảy và Chủ Nhật**. Hotline: **0389479884**. Chưa tự suy diễn giờ bắt đầu/kết thúc cụ thể.

## 2 Bật trên local

1. Dùng database local riêng của bạn đã có schema project hiện tại. Chọn đúng DB trong SSMS.
2. Chạy **`database/007_AddSupportChat.sql`**. Script chỉ bổ sung schema `support`, ba bảng, khóa ngoại, CHECK và index; không chạy init/seed, không tự tạo tài khoản, không xóa bảng.
3. Tài khoản ứng dụng cần SELECT/INSERT/UPDATE trên conversations; SELECT/INSERT trên messages và events. Cuối script có mẫu GRANT để đổi đúng principal thực tế. Không cấp quyền DDL cho tài khoản runtime chỉ để ứng dụng tự chạy migration.
4. `SupportChat:Enabled` trong `appsettings.json` mặc định **false**. Bật ở cấu hình Development hoặc bằng biến môi trường `SupportChat__Enabled=true` sau bước 2. Cấu hình Development đã có phần SupportChat bật cho local.
5. Kiểm tra `ConnectionStrings:ClinicDb` trỏ DB local và `ApiSettings:BaseUrl` của Web trỏ đúng API local.
6. Chạy API và Web như các chức năng hiện có. Dùng các phiên trình duyệt độc lập cho bệnh nhân, hai lễ tân và Admin; không đăng nhập nhiều tài khoản trong cùng local storage của một phiên.

Các khóa API:

| Khóa | Mặc định |
| --- | --- |
| `SupportChat__Enabled` | false ở cấu hình chung |
| `SupportChat__SupportHours` | Giờ hành chính hằng ngày, gồm Thứ Bảy và Chủ Nhật |
| `SupportChat__Hotline` | 0389479884 |
| `SupportChat__MaxMessageLength` | 2000, giới hạn cứng tối đa 2000 |
| `SupportChat__MessagesPerMinute` | 30 cho mỗi tài khoản |
| `SupportChat__CreatesPerHour` | 10 cho mỗi tài khoản |

Web có thể cấu hình `SupportChat__ReconciliationIntervalSeconds`, mặc định 30, giới hạn 10–120 giây. Khi mất sự kiện, đối soát sẽ tải bù từ SQL. Kết nối và dữ liệu nằm trong service scoped theo Blazor circuit, không dùng singleton.

## 3 API

- `/api/support/settings`: cấu hình công khai, không chứa khóa hoặc connection string.
- `/api/support/me`: danh sách/tạo yêu cầu, messages, read, close, unread-summary.
- `/api/support/reception`: danh sách theo `scope=waiting|mine|closed`, claim, messages, read, release, close, events.
- `/api/support/admin`: danh sách metadata, receptionists, assign, transfer, release, close, events.
- `/hubs/support-chat`: xác thực JWT qua .NET SignalR Client từ server Web. Không truyền JWT trong query string và không mở CORS tùy ý.
- API ghi lưu SQL trước, sau đó mới phát invalidation. Payload realtime chỉ chứa mã hội thoại hoặc tín hiệu tải lại, không chứa nội dung/tên bệnh nhân.
- Không có endpoint để client tự tạo tin System hoặc tự chọn người gửi.

Danh sách: `page`, `pageSize` (tối đa 100), `search` theo mã yêu cầu/tên/mã bệnh nhân, `from`, `to`; Admin thêm status/assignedUserId. Ngày lọc là ngày GMT+7, đổi sang UTC ở server.

Tin và nhật ký: `beforeSequence` hoặc `afterSequence`, không dùng cả hai, `pageSize` mặc định 30 và tối đa 100. Trạng thái đọc không được cập nhật chỉ vì nhận SignalR hay đóng khung. Lễ tân gửi phiên bản hiện tại cùng read để chống request đọc trễ sau khi đổi lượt phân công.

## 4 Kịch bản kiểm tra thủ công

1. Bệnh nhân gửi tin đầu → xuất hiện đúng hai tin: tin bệnh nhân và xác nhận. Mở khung trống không tạo yêu cầu.
2. Gửi tiếp, tải lại, logout/login → không phát sinh thêm xác nhận.
3. Hai lễ tân cùng nhận → một người thành công, người còn lại nhận xung đột.
4. Lễ tân trả lời → bệnh nhân thấy “Hỗ trợ phòng khám”, không thấy tên thật hoặc người bàn giao.
5. Admin chuyển A sang B có lý do → A không tải/gửi tiếp; B đọc được lịch sử; Admin chỉ thấy metadata và nhật ký.
6. Bệnh nhân đã đọc tin 3, tự gửi tin 4, lễ tân gửi tin 5 → chỉ một tin chưa đọc.
7. Mở cùng tài khoản trên thiết bị thứ hai → dùng chung vị trí đọc. Tin đến lúc khung đóng không tự được đánh dấu đã đọc.
8. Mất mạng/khởi động lại API → giữ tin đã xác nhận lưu; nối lại và tải bù, không lặp tin. Nếu HTTP mất phản hồi, thử lại cùng clientMessageId.
9. Đóng đồng thời với gửi → không có tin sau mốc đóng. Lịch sử chỉ đọc và yêu cầu mới tạo hội thoại khác.
10. Kiểm tra mobile, bàn phím, mở lịch sử cũ và tải thêm trang tin; dùng tài khoản khác không thấy dữ liệu trước đó.

## 5 Kiểm thử tự động

```powershell
dotnet test tests/ClinicManagementSystem.UnitTests/ClinicManagementSystem.UnitTests.csproj --filter "FullyQualifiedName~SupportChat"
dotnet test tests/ClinicManagementSystem.IntegrationTests/ClinicManagementSystem.IntegrationTests.csproj --filter "FullyQualifiedName~SupportChatSqlTests"
```

Integration fixture tạo database **`ClinicSupportChatTests_<GUID>`**, dùng SQL LocalDB trên Windows hoặc SQL Server Testcontainer trên Linux, chạy script 007 thật rồi kiểm tra HTTP/JWT và SignalR qua cả WebSockets và Long Polling. Fixture xóa đúng database test của mình sau khi chạy. Không dùng connection string production. Windows cần LocalDB đã được cài và khả dụng; Linux cần Docker.

Kiểm tra render dùng Razor thật với dữ liệu fixture. Các kiểm thử này không thay thế nghiệm thu WebSockets, hai thiết bị thật và tải thực trên Azure.

## 6 Deploy Azure một instance

1. Xác nhận Plan/SKU, **Manual = 1**, Web/API dùng chung hay riêng Plan, tài nguyên và chi phí hiện tại. Không bật thêm instance khi chưa có Azure SignalR Service/backplane phù hợp.
2. Sao lưu theo quy trình môi trường; chọn đúng database production, chạy riêng script 007 và cấp quyền dữ liệu cần thiết. Workflow hiện không tự chạy script schema.
3. Deploy API và Web tương thích. Giữ module tắt cho đến khi cả hai và schema đã sẵn sàng.
4. Web: kiểm tra `ApiSettings__BaseUrl` là HTTPS API production. API: giữ database production và cấu hình auth/storage hiện có.
5. Kiểm tra khả năng WebSockets, session affinity theo hosting cho cả Blazor Server và Chat Hub; bật Always On nếu gói hỗ trợ. Always On không thay reconnect hoặc lưu SQL.
6. Thêm các biến `SupportChat__...` ở bảng trên cho API và bật `SupportChat__Enabled=true`; Apply/Restart theo Azure Portal.
7. Nghiệm thu các kịch bản mục 4 trên URL production bằng hai thiết bị/mạng, tắt Web/API local để kiểm tra độc lập máy phát triển.
8. Đo CPU/RAM, độ trễ, HTTP lỗi, ngắt kết nối và tải SQL ở tải dự kiến cùng tải dự phòng trước khi chốt sức chứa một instance. Không suy ra số người chịu tải chỉ từ tên B1.

Không cần thay cấu hình Blob hoặc luồng đặt lịch, bệnh án. Azure SignalR Service là hướng mở rộng sau này; module hiện không yêu cầu tài nguyên Azure trả phí bổ sung riêng cho SignalR.

## 7 Rollback

Tắt `SupportChat__Enabled` rồi quay về API/Web trước nếu cần. **Giữ nguyên ba bảng và tin đã lưu**; không DROP bảng để rollback ứng dụng. Phiên bản cũ không dùng các bảng bổ sung này.

Đừng ghi nội dung tin nhắn, JWT hoặc giá trị tham số SQL vào log. Theo dõi lỗi bằng ID tài khoản/hội thoại và mã lỗi. Giới hạn tốc độ hiện được giữ trong bộ nhớ tiến trình, phù hợp phương án một API instance đã chọn; phải đánh giá lại khi scale.

## 8 Kết quả kiểm tra ngày 02/11/2026

- Build API và Web thành công.
- Toàn bộ unit test: 774 passed, không fail/skip; trong đó có 24 ca quy tắc/state/render của chat.
- Integration chat: 7 passed trên database LocalDB riêng, gồm JWT thật, nhận đồng thời, retry, quyền nội dung, bàn giao, gửi/đóng đồng thời, cursor đọc dùng chung, ràng buộc SQL, append-only và SignalR WebSockets/Long Polling.
- Render ba giao diện bằng Razor thật; kiểm tra desktop 1440 px và mobile 390/320 px không tràn ngang. Dữ liệu render là fixture, chưa phải phiên browser tương tác với database production.
- Chưa chạy script trên database của bạn hoặc production; chưa deploy, chưa đo tải Azure hay thực hiện nghiệm thu hai thiết bị trên URL production.
- Build nền có cảnh báo nullable/using và dependency PKCS đã tồn tại; không nâng cấp ngoài phạm vi module.

Dependency SignalR .NET Client dùng bản 10.0.13 đã vá GHSA-334f-vvp4-7g6p. Khi deploy cần đối chiếu ASP.NET Core runtime của App Service với bản vá tương ứng; package client không tự cập nhật runtime server. Tham khảo https://github.com/advisories/GHSA-334f-vvp4-7g6p .

## 9 Cập nhật chatbox bệnh nhân (02/11/2026)

- Desktop: 360 × 500 px, giới hạn theo viewport; nền trắng/xanh nhạt, header và tin của bệnh nhân màu xanh rõ hơn. Khi mở khung, ẩn hai nút nổi để không che nội dung. Thu nhỏ chỉ ẩn khung, không kết thúc yêu cầu.
- Mobile: bottom sheet tối đa 560 px, khoảng 78% chiều cao vùng nhìn thấy; dùng visualViewport để giữ ô nhập trên bàn phím, khóa cuộn trang nền và khôi phục khi đóng. Ô nhập tự tăng tối đa ba dòng.
- Chọn chủ đề không bắt buộc. Có tám chủ đề định sẵn và “Khác”; chọn Khác mới hiện ô nhập riêng (tối đa 150 ký tự). Để trống gửi lên API sẽ được chuẩn hóa thành “Hỗ trợ chung”.
- Bệnh nhân có thể đổi chủ đề yêu cầu của mình khi chưa kết thúc qua `POST /api/support/me/conversations/{id}/subject`, body `{ "subject": "Đặt lịch khám", "expectedSubject": "Hỗ trợ chung" }`.
- Đổi chủ đề chỉ cập nhật Subject/UpdatedAt và phát invalidation sau commit. Không tạo tin/tin xác nhận/sự kiện phân công mới, không đổi trạng thái, người phụ trách hoặc vị trí đã đọc. Yêu cầu đã đóng trả 409; sai chủ hội thoại trả 404; vai trò khác bị chặn.
- `expectedSubject` chặn hai phiên ghi đè chủ đề khác nhau đã đổi trước đó. Retry cùng chủ đề là idempotent. Retry gửi tin đầu dùng GUID cũ vẫn trả yêu cầu hiện có và giữ tên mới, không nhân đôi tin tự động.
- Không thay đổi schema; **không chạy lại 007 hoặc seed** cho bản cập nhật UI này. Cần deploy/restart cả API và Web tương thích, hard refresh trình duyệt để nhận CSS/JS mới.

Kiểm tra thủ công: tạo yêu cầu không chọn chủ đề; thử từng lựa chọn và Khác; đổi chủ đề trong hội thoại mở; thu nhỏ/mở lại giữ tin đang gõ; kết thúc và kiểm tra không còn nút đổi/ô gửi; đăng xuất/đăng nhập xem lại lịch sử. Trên điện thoại thật, kiểm tra cả Safari/Chrome, bàn phím mở/đóng, xoay màn hình và nút gửi luôn nhìn thấy.

Kết quả kiểm tra bản UI: **783 unit test**, **12 integration test chat** đều đạt. Kiểm tra HTML render bằng Razor thật với Edge headless: **30 trường hợp bố cục/bàn phím** (desktop 1440, mobile 390/320), gồm khôi phục cuộn trang và cleanup khi DOM bị gỡ. Chưa thay thế kiểm tra bàn phím trên iPhone/Android thật hoặc nghiệm thu production. Build vẫn có các cảnh báo dependency PKCS/nullable đã có trước.

## 10 Sửa bố cục mobile (02/11/2026)

- Mobile tách ba vùng: phần đầu, nội dung cuộn, ô nhập cố định trong khung. Phần chào dùng tên ngắn “Bạn cần hỗ trợ gì?”, ẩn dòng khẩu hiệu. Giờ hỗ trợ và hotline nằm trong nút thông tin ở header; desktop tiếp tục có thanh thông tin.
- Theo tinh chỉnh sau khi thử điện thoại thật: các trạng thái dùng khung cao khoảng 82% vùng nhìn thấy, tối đa 720 px. Khung căn giữa, rộng tối đa 520 px; lề hai bên 12 px, màn hình nhỏ ≤360 px dùng lề 8 px. Khi mở bàn phím, khung dùng phần còn lại phía trên, chừa lề 8 px; thu gọn tab/thông tin phụ. Hỗ trợ cả điện thoại xoay ngang.
- Định vị bằng tọa độ trên và chiều cao từ vùng nhìn thấy, tránh neo đáy theo innerHeight khi browser toolbar và vùng fixed dùng số đo khác nhau. Thông báo lỗi nằm trong nội dung cuộn; không được đẩy ô nhập khỏi khung.
- Tính hiển thị tin cuối cũng đối chiếu với visual viewport; không đánh dấu đã đọc chỉ vì nhận sự kiện hoặc thu nhỏ chat.
- Chỉ cập nhật Web (Razor/CSS/JS và kiểm tra render), không đổi backend/schema. Restart Web, hard refresh/xóa cache trang trên trình duyệt mobile để nhận module JS v4 và CSS mới. Không chạy lại SQL.

Nghiệm thu trên thiết bị thật: mở chat khi thanh địa chỉ đang hiện; thu gọn/hiện lại thanh địa chỉ; mở/đóng bàn phím; chọn Khác và nhập chủ đề; gõ tin nhiều dòng; xoay ngang/dọc; mở thông tin hỗ trợ; kiểm tra lỗi mạng; thu nhỏ/mở lại giữ nháp; kiểm tra lịch sử dài. Ô nhập và nút gửi luôn nằm trong vùng nhìn thấy, nội dung cuộn bên trong, trang nền được khôi phục vị trí cuộn khi thu nhỏ.

Kiểm tra bản sửa mobile: build Web và 36 test chat đạt; 47 ca ghi nhận bằng Edge headless (320/390 px, điện thoại xoay ngang 844 × 390, desktop) đạt, kèm kiểm tra nháp nhiều dòng, lịch sử dài, vị trí đọc và khôi phục cuộn trang. Ca regression giả lập layout 844 px nhưng vùng nhìn thấy/innerHeight 724 px: bản cũ đặt đáy ô nhập ở 843 px; bản mới khoảng 715 px. Đây là kiểm tra bằng viewport giả lập và HTML Razor thật, chưa phải Safari/iPhone/Android vật lý hoặc production.

## 11 Sửa tải lại hội thoại và tin mới (02/11/2026)

- Mở lại chat luôn tải lại yêu cầu đang mở và tin mới; không chỉ hiển thị dữ liệu đang giữ trong bộ nhớ. Chọn hội thoại lịch sử có trạng thái đang tải, lỗi riêng và nút tải lại; không báo hội thoại trống khi nội dung chưa tải thành công.
- Mỗi lượt chọn hội thoại có phiên tải riêng; phản hồi chậm của lượt chọn cũ không được ghi đè lượt mở mới, kể cả cùng ID. Các lượt tải tin được tuần tự; sự kiện tới khi đang refresh được gom xử lý tiếp. Phân biệt cursor tải tin cũ và tải bù tin mới.
- Bỏ số chưa đọc tổng khỏi tab Lịch sử để tránh hiểu nhầm mọi tin mới nằm ở đó. Khung có dòng “N tin chưa đọc — Xem tin mới”; bấm sẽ tìm và mở hội thoại có tin chưa đọc, kể cả nằm ở trang lịch sử sau. Mỗi dòng lịch sử có “Đọc tin mới” hoặc “Xem hội thoại”.
- Lúc mở hội thoại hoặc đang ở cuối, tin mới được đưa vào vùng nhìn thấy. Nếu đang đọc tin cũ, giữ vị trí và hiện nút “Tin nhắn mới” để chuyển xuống. Tải thêm tin cũ giữ vị trí đọc. Chỉ cập nhật vị trí đã đọc đến sequence đã kiểm tra hiển thị, không bao gồm tin đến sau lúc kiểm tra hoặc hội thoại khác.
- Chỉ sửa Web, không đổi API/schema. Restart Web, hard refresh trình duyệt để nhận module JS v5 và CSS có version mới. Không chạy lại SQL.

Kiểm tra: 795 unit test đạt (45 ca chat); 48 ca browser/layout/scroll giả lập đạt. Đã kiểm tra tương tác trên Blazor Server thật với API fixture và tin giả: nhận khi thu nhỏ, mở lại thấy tin cuối, mở lịch sử, kết thúc/chỉ đọc, reload rồi đọc lại, đổi kích thước mobile; không có lỗi JavaScript. Fixture này không thay thế nghiệm thu hai tài khoản thật trên database của bạn hoặc production.

## 12 Chống treo trạng thái tải tin ở lễ tân (03/11/2026)

- Web giới hạn mỗi lượt tải tin và thời gian chờ hàng đợi/request bằng `SupportChat:RequestTimeoutSeconds` (mặc định 15 giây, giới hạn cấu hình 1–60 giây). Token cancellation áp dụng cả chờ semaphore, nhận HTTP headers/body và làm mới phiên; lỗi tải có nút thử lại. Không đồng nghĩa thao tác ghi thất bại: nếu gửi bị timeout, UI vẫn dùng GUID cũ khi thử lại để tránh lưu trùng.
- `SupportThread` tự yêu cầu tải khi vừa được mở mà chưa có dữ liệu, đăng ký sự kiện thay đổi tải tin để render ngay khi hoàn tất. Chỉ hiện “Đang tải” khi có lượt tải thực sự, không dùng trạng thái chưa tải để hiển thị spinner vô hạn.
- Màn hình lễ tân render tin trước khi chờ tải xong nhật ký; gom các sự kiện đến trong lúc nhận/chọn/xử lý yêu cầu để không giẫm lên lượt chọn. Timeout ghi log Web gồm route không có query, giai đoạn chờ và ID tài khoản; không ghi chat/JWT.
- Không đổi schema/API. Restart Web và tải lại cả màn hình bệnh nhân/lễ tân. Nếu API chậm hoặc ngừng hoạt động, UI báo lỗi để thử lại thay vì giữ spinner. Chưa xác minh được request thực tế trong ảnh đã kẹt ở đâu; log timeout mới hỗ trợ phân biệt chờ hàng đợi, response, authentication và body.

Kiểm tra: 796 unit test đạt, gồm 46 ca chat và regression request treo rồi thử lại thành công. Đã bấm thử UI lễ tân trên Blazor Server với API giả: tin hiển thị trước nhật ký có độ trễ; request treo kết thúc loading theo timeout 2 giây của fixture; bấm tải lại phục hồi; không lỗi JavaScript. Không thay thế kiểm tra lại trên database local/production thật.
