# Đặc tả module hỗ trợ trực tuyến

Clinic Management System

Phiên bản 1.1 | Ngày 22 tháng 10 năm 2026 | Đặc tả thiết kế

Ghi chú triển khai ngày 02/11/2026: xem [SUPPORT_CHAT_SETUP.md](SUPPORT_CHAT_SETUP.md) để chạy script bổ sung, bật module và nghiệm thu. Giờ hỗ trợ là giờ hành chính hằng ngày kể cả Thứ Bảy/Chủ Nhật; hotline 0389479884. Tin xác nhận hiển thị khung nhạt, bỏ nhãn riêng theo trao đổi sau đặc tả.

## 1 Tổng quan và quyết định thiết kế

Module Hỗ trợ trực tuyến cho phép bệnh nhân trao đổi với bộ phận lễ tân ngay trên website. Hệ thống lưu lịch sử, quản lý người phụ trách và cập nhật tin nhắn bằng ASP.NET Core SignalR. Một yêu cầu có thể được bàn giao giữa các lễ tân nhưng bệnh nhân luôn trao đổi với phía “Hỗ trợ phòng khám”.

Tài liệu dành cho người phát triển, kiểm thử và trình bày đồ án. Đây là đặc tả nghiệp vụ và kế hoạch triển khai; các bảng, API và đường dẫn mới bên dưới là thiết kế dự kiến. Tiêu chí hoàn thành bao gồm chat realtime trên website Azure production, sử dụng hai tài khoản trên hai thiết bị; chạy được ở local mới là một bước triển khai.

Các quyết định đã chốt:

- Có ba actor: Bệnh nhân, Lễ tân và Admin. Bệnh nhân với Lễ tân trực tiếp trao đổi; Admin điều phối.
- Dùng ba bảng mới: support_conversations, support_messages và support_conversation_events.
- Mỗi bệnh nhân có tối đa một yêu cầu chưa kết thúc. Mỗi yêu cầu đang hỗ trợ có một lễ tân phụ trách.
- Lễ tân tự bấm Tiếp nhận; backend bảo đảm chỉ một người nhận thành công khi có thao tác đồng thời.
- Admin chuyển người phụ trách; lễ tân có thể trả yêu cầu về hàng chờ. Chưa có chức năng mời nhiều nhân viên cùng chat.
- Patient không thấy nhật ký tiếp nhận, bàn giao, tên người chuyển hoặc lý do nội bộ. Tin phía nhân viên mang nhãn “Hỗ trợ phòng khám”.
- Database vẫn lưu người gửi thật của từng tin và toàn bộ các mốc thay đổi người phụ trách.
- Mỗi yêu cầu mới có đúng một tin xác nhận tự động ngay sau tin đầu của patient. Dùng một nội dung chung có giờ hỗ trợ; không gửi lại theo từng tin, theo thời gian chờ hoặc theo giờ trong ngày.
- Lịch sử được lưu trong SQL. Đóng khung chat, logout và kết thúc hỗ trợ là ba hành động khác nhau.
- Bản đầu chạy Chat Hub trong API ở cả local và Azure App Service. Cấu hình production và kiểm thử sau deploy nằm trong phạm vi thực hiện.

### 1 1 Phạm vi bản đầu

Bao gồm chat văn bản, một tin xác nhận tự động cho yêu cầu mới, lịch sử phân trang, số tin chưa đọc, hàng chờ chung, tiếp nhận, trả lời, trả về hàng chờ, Admin bàn giao, kết thúc, nhật ký nội bộ, reconnect và hotline. Nội dung hỗ trợ hướng đến lịch khám, dịch vụ và thủ tục; thao tác đổi lịch thực tế vẫn dùng chức năng đặt lịch hiện có. Một câu trả lời trong chat không tự thay đổi lịch hẹn.

Gửi ảnh và tệp, chat với bác sĩ, chatbot, gọi thoại hoặc video trong trình duyệt, tự động phân công, tự đóng do im lặng, đánh giá hỗ trợ và thông báo ngoài ứng dụng được để lại cho giai đoạn sau. Hotline dùng liên kết tel và không dùng SignalR.

## 2 Actors và quyền truy cập

| Actor | Mục tiêu | Quyền chính |
| --- | --- | --- |
| Bệnh nhân | Nhận hỗ trợ và xem lại trao đổi | Tạo yêu cầu, gửi bổ sung, xem lịch sử của mình, đánh dấu đã đọc và kết thúc yêu cầu |
| Lễ tân | Tiếp nhận và giải quyết yêu cầu | Xem hàng chờ, nhận yêu cầu, đọc và trả lời yêu cầu được giao, trả về hàng chờ, kết thúc và đọc nhật ký trong phạm vi được phép |
| Admin | Điều phối và truy vết trách nhiệm | Xem metadata các yêu cầu, xem nhật ký, giao hoặc chuyển người, trả về hàng chờ, đóng yêu cầu có lý do |

Admin không mặc định có quyền xem nội dung tin nhắn hoặc gửi tin. Quyền Admin trong module này là quyền điều phối. Nếu sau này cần giám sát nội dung, phải bổ sung nghiệp vụ và quyền cụ thể. Tài khoản có nhiều role dùng endpoint tương ứng với vai trò đang thao tác; backend xác định phía người gửi từ endpoint và quyền, không nhận phía người gửi từ client.

### 2 1 Ma trận phân quyền

| Hành động | Bệnh nhân | Lễ tân | Admin |
| --- | --- | --- | --- |
| Xem danh sách | Chỉ của mình | Hàng chờ, đang giao cho mình, đã kết thúc do mình phụ trách | Metadata toàn module |
| Đọc nội dung chat | Hội thoại của mình | Người được giao hiện tại hoặc người phụ trách cuối khi đã đóng | Không mặc định |
| Gửi tin | Hội thoại của mình còn mở | Đang được giao và chưa kết thúc | Không |
| Tiếp nhận | Không | Yêu cầu đang chờ và chưa có người nhận | Có thể giao cho một lễ tân đủ quyền |
| Trả về hàng chờ | Không | Yêu cầu đang giao cho mình | Yêu cầu đang được xử lý |
| Chuyển sang người khác | Không | Chưa có trong bản đầu | Có, đích là Receptionist đang hoạt động |
| Kết thúc | Yêu cầu của mình | Yêu cầu đang giao cho mình | Có, bắt buộc lý do |
| Xem nhật ký nội bộ | Không | Cùng phạm vi được đọc hội thoại | Có |

Mọi lễ tân được phép xem metadata hàng chờ, gồm bệnh nhân, chủ đề, thời gian chờ và mã yêu cầu. Hàng chờ không trả nội dung hay bản xem trước tin nhắn. Lễ tân A đã bàn giao cho B mất quyền đọc tiếp nội dung và gửi tin; dữ liệu tác giả A vẫn được giữ. Phiếu đã kết thúc nằm trong lịch sử của người phụ trách cuối. Các mốc A từng xử lý vẫn nằm trong nhật ký để Admin truy vết.

## 3 Trạng thái và vòng đời yêu cầu

Hội thoại là một yêu cầu hỗ trợ có điểm bắt đầu và kết thúc. Quy ước lưu status là 0 Chờ tiếp nhận, 1 Đang hỗ trợ và 2 Đã kết thúc. Trạng thái kết nối mạng của người dùng là thông tin riêng, không phải trạng thái nghiệp vụ của yêu cầu.

| Trạng thái hiện tại | Hành động | Người thực hiện | Trạng thái sau |
| --- | --- | --- | --- |
| Chưa có yêu cầu | Gửi tin đầu tiên | Bệnh nhân | Chờ tiếp nhận |
| Chờ tiếp nhận | Tiếp nhận hoặc giao người | Lễ tân hoặc Admin | Đang hỗ trợ |
| Đang hỗ trợ | Bàn giao A sang B | Admin | Đang hỗ trợ, người phụ trách đổi sang B |
| Đang hỗ trợ | Trả về hàng chờ | Người phụ trách hoặc Admin | Chờ tiếp nhận, người phụ trách để trống |
| Chờ tiếp nhận | Rút yêu cầu | Bệnh nhân hoặc Admin | Đã kết thúc |
| Đang hỗ trợ | Kết thúc hỗ trợ | Bệnh nhân, người phụ trách hoặc Admin | Đã kết thúc |
| Đã kết thúc | Yêu cầu hỗ trợ mới | Bệnh nhân | Tạo hội thoại khác |

Kết thúc không xóa lịch sử và không cho gửi thêm vào yêu cầu cũ. Các thao tác đọc lịch sử và đánh dấu đã đọc vẫn được phép sau khi kết thúc trong đúng phạm vi. Không mở lại yêu cầu đã kết thúc trong bản đầu. Chỉ đóng khung chat, logout, hết phiên hoặc mất mạng không làm đổi trạng thái nghiệp vụ.

Lý do kết thúc dùng mã ổn định, ví dụ Resolved, PatientEnded, Withdrawn và AdminClosed. Ghi chú xử lý của nhân viên là nội bộ; patient chỉ nhận trạng thái công khai. Tự động đóng do hết thời gian chưa nằm trong phạm vi.

## 4 Luồng nghiệp vụ chi tiết

### 4 1 Bệnh nhân tạo yêu cầu

1. Bệnh nhân đăng nhập và bấm nút tin nhắn ở góc phải dưới. Nếu chưa đăng nhập, giao diện hướng dẫn đăng nhập; không tạo chat ẩn danh.
2. Backend kiểm tra tài khoản và hồ sơ bệnh nhân hợp lệ. Nếu đã có yêu cầu còn mở thì tải lại yêu cầu đó.
3. Nếu chưa có, bệnh nhân có thể chọn chủ đề (không bắt buộc) và nhập tin đầu tiên. Để trống dùng “Hỗ trợ chung”; chọn “Khác” mới hiện ô nhập chủ đề riêng, tối đa 150 ký tự. Mở khung trống chưa tạo bản ghi.
4. API lấy UserId từ JWT, tìm PatientId tương ứng rồi tạo hội thoại, tin đầu tiên của patient, một tin xác nhận System và sự kiện Created trong một giao dịch.
5. Sau khi lưu thành công, patient nhận hai tin theo thứ tự và trạng thái Chờ tiếp nhận. Tin System xuất hiện ngay, không đợi lễ tân. SignalR thông báo hàng chờ cho các lễ tân.

Patient có thể gửi bổ sung khi chưa được nhận. Hệ thống thông báo đang chờ; không hiển thị đã có người hỗ trợ chỉ vì có kết nối mạng. Ngoài giờ hỗ trợ vẫn lưu yêu cầu và hiển thị giờ hỗ trợ cùng hotline; không hứa trả lời ngay.

Tin xác nhận dùng cùng một mẫu cho mọi thời điểm, với khung giờ lấy từ cấu hình phòng khám:

“Phòng khám đã nhận được yêu cầu của bạn. Giờ hỗ trợ: [khung giờ hỗ trợ]. Nếu bạn nhắn trong khung giờ này, lễ tân sẽ phản hồi sớm nhất có thể. Nếu nhắn ngoài giờ hỗ trợ, yêu cầu của bạn sẽ được phản hồi trong giờ làm việc tiếp theo.”

Chỉ tạo một lần cho mỗi yêu cầu mới. Patient nhắn tiếp, tải lại trang hoặc logout rồi vào lại không phát sinh thêm tin xác nhận. Không có tin nhắc sau hai phút hoặc mẫu riêng ngoài giờ; không cần tác vụ nền cho xác nhận này. Tin tự động không được tính là lễ tân đã trả lời, không đổi trạng thái và không nhận người phụ trách.

### 4 2 Lễ tân tiếp nhận và trả lời

1. Lễ tân mở tab Chờ tiếp nhận, mặc định sắp xếp yêu cầu lâu nhất lên trước.
2. Bấm Tiếp nhận. Backend kiểm tra yêu cầu còn chờ, chưa có người phụ trách và tài khoản còn role Receptionist hoạt động.
3. Chỉ một thao tác được chuyển trạng thái thành công. Người bấm sau nhận thông báo yêu cầu đã được người khác tiếp nhận và tải lại danh sách.
4. Người nhận thành công mở được nội dung, gửi câu trả lời và đánh dấu đã đọc khi thực sự xem tin.
5. Patient thấy tin từ Hỗ trợ phòng khám. Nội bộ thấy tên người gửi thật.

### 4 3 Trả về hàng chờ và bàn giao

Người phụ trách có thể trả yêu cầu về hàng chờ với lý do. Hệ thống bỏ người phụ trách, thiết lập lại vị trí đã đọc của phía phụ trách và ghi sự kiện Released. Lễ tân khác có thể nhận tiếp và đọc lại nội dung trước đó.

Admin có thể chuyển A sang B khi B là Receptionist còn hoạt động. Việc kiểm tra role không dùng trạng thái kết nối làm bằng chứng B đang trực; Admin chủ động xác nhận ca trực trước khi giao. Hệ thống đổi người phụ trách, đặt lại vị trí đã đọc của người mới, ghi lý do, người thực hiện và mốc tin cuối lúc bàn giao. A mất quyền gửi hoặc tải tiếp nội dung. Patient không thấy sự kiện bàn giao.

Không tự bàn giao chỉ vì logout hoặc mất mạng. Khi kết thúc ca, lễ tân chủ động trả về hàng chờ hoặc nhờ Admin chuyển. Nếu người phụ trách bị khóa hoặc mất role, người đó bị từ chối truy cập; yêu cầu được giữ lại để Admin điều phối.

### 4 4 Kết thúc và xem lại lịch sử

Bệnh nhân hoặc người phụ trách kết thúc yêu cầu đang hỗ trợ. Bệnh nhân rút yêu cầu đang chờ; Admin được đóng trong cả hai trường hợp với lý do. Backend cập nhật hội thoại và ghi sự kiện Closed cùng giao dịch. Nếu đóng đồng thời với gửi tin, giao dịch nào được xử lý trước quyết định kết quả; không lưu tin mới sau khi trạng thái đã đóng.

Khi patient logout, giao diện đóng khung, ngắt kết nối của phiên hiện tại và xóa dữ liệu trong bộ nhớ. Đăng nhập lại cùng tài khoản sẽ lấy lịch sử từ SQL và tải các câu trả lời chưa đọc. Đăng nhập tài khoản khác không được thấy dữ liệu phiên trước. Tin đã gửi thành công được lưu; nội dung chưa gửi không cam kết khôi phục.

## 5 Màn hình của từng actor

Có ba giao diện theo actor: một khung chat bệnh nhân và hai trang nội bộ. Hai trang nội bộ có thể dùng chung thành phần danh sách, nhưng API và dữ liệu trả về phải tách theo quyền.

### 5 1 Bệnh nhân

Hai nút nổi đặt ở góc phải dưới, theo nền trắng và xanh nhạt của project. Nút tin nhắn mở khung Hỗ trợ phòng khám; nút điện thoại gọi hotline. Có nhãn truy cập, thao tác bàn phím và khoảng cách để không che nút chính của trang. Trên máy tính dùng khung gọn; điện thoại mở khung lớn phù hợp bàn phím và có nút đóng rõ ràng.

Khung chat có tab Trò chuyện và Lịch sử. Desktop dùng khung gọn 360 × 500 px; mobile dùng bottom sheet điều chỉnh theo visual viewport để ô nhập không bị bàn phím che. Bệnh nhân được đổi chủ đề hội thoại của mình khi còn mở; đổi chủ đề không tạo thêm tin, tin tự động hoặc thay đổi phân công/vị trí đã đọc. Tab đầu hiển thị yêu cầu còn mở, trạng thái công khai, tin nhắn, ô nhập và nút kết thúc. Tab lịch sử hiển thị chủ đề, ngày, trạng thái; mở yêu cầu đã kết thúc chỉ để đọc, kèm nút Yêu cầu hỗ trợ mới. Menu tài khoản có thể có lối vào cùng khung chat, không tạo thêm trang riêng.

Tin của bệnh nhân mang nhãn Bạn; tin nhân viên mang nhãn Hỗ trợ phòng khám; tin xác nhận không có nhãn riêng và hiển thị nhẹ hơn theo lựa chọn UI đã chốt. Tin tự động được lưu và xem lại trong lịch sử. Không hiển thị tên lễ tân, người bàn giao, lý do nội bộ hoặc timeline xử lý. API patient cũng loại bỏ những trường đó. Khi đang nối lại, báo trạng thái rõ; không báo đã gửi trước khi API xác nhận lưu thành công. Số chưa đọc chỉ tính tin phía nhân viên mà bệnh nhân chưa xem, không tính xác nhận tự động.

### 5 2 Lễ tân

Trang dự kiến /reception/support có ba tab Chờ tiếp nhận, Tôi đang xử lý và Tôi đã xử lý. Máy tính dùng danh sách bên trái và nội dung bên phải; điện thoại mở danh sách rồi chuyển đến chi tiết.

Danh sách hàng chờ có mã yêu cầu, tên bệnh nhân, chủ đề và thời gian chờ. Chi tiết được giao có tên tác giả từng tin, thời gian, trạng thái, ô trả lời, nút Trả về hàng chờ và Kết thúc. Timeline nội bộ hiển thị các mốc nhận, trả, chuyển và đóng. Nếu mất quyền do bàn giao, giao diện xóa nội dung đang giữ và tải lại danh sách.

Bộ lọc gồm trạng thái hoặc tab, khoảng ngày và từ khóa mã yêu cầu hoặc tên bệnh nhân; không tìm toàn văn tin nhắn ở bản đầu. Có phân trang, trạng thái trống, lỗi, tải dữ liệu và thông báo xung đột tiếp nhận. Số yêu cầu chờ và số tin chưa đọc là hai chỉ số riêng.

### 5 3 Admin

Trang dự kiến /admin/support hiển thị metadata: mã, bệnh nhân, chủ đề, trạng thái, thời gian chờ, người phụ trách và các mốc xử lý. Admin mở nhật ký, chọn lễ tân đủ quyền, nhập lý do để giao hoặc chuyển; có thể trả về hàng chờ hoặc kết thúc với lý do.

Trang Admin không có nội dung tin nhắn, bản xem trước, nút trả lời hoặc tìm kiếm nội dung. Danh sách chọn người hiển thị lễ tân hoạt động và số yêu cầu đang phụ trách để hỗ trợ quyết định. Bộ lọc gồm trạng thái, người phụ trách, ngày và mã hoặc tên bệnh nhân.

## 6 Database cần bổ sung

Đề xuất tạo schema support và ba bảng giữ nguyên tên đã trao đổi: support.support_conversations, support.support_messages và support.support_conversation_events. Đây là thiết kế bổ sung theo Database First; chưa phải script đã chạy. Không thêm bảng participants trong bản đầu.

Quan hệ với dữ liệu có sẵn:

- support_conversations.patient_id tham chiếu scheduling.patients.id.
- Các trường tài khoản tham chiếu auth.users.id, gồm chủ sở hữu, tác giả, người phụ trách và người thực hiện hành động.
- Mỗi conversation có nhiều messages và nhiều conversation_events.
- Không dùng auth.notifications làm nơi lưu tin vì bảng hiện tại gắn nghiệp vụ thông báo với appointment_id. Không tạo lịch hẹn giả để lưu chat.
- Không cần chuyển ảnh hoặc thay cấu hình Azure Blob cho module chat văn bản.

UserId và PatientId là hai mã khác nhau. Backend tra quan hệ scheduling.patients.user_id khi tạo yêu cầu. patient_user_id lưu chủ tài khoản của lịch sử lúc tạo, do server xác định; không lấy từ body. Thay đổi liên kết hồ sơ không tự chuyển lịch sử chat sang tài khoản khác.

### 6 1 Bảng yêu cầu hỗ trợ

Mỗi dòng là một yêu cầu hỗ trợ. Bảng này trả lời hiện tại ai chịu trách nhiệm và yêu cầu đang ở đâu trong quy trình.

| Cột | Kiểu dự kiến | Ý nghĩa nghiệp vụ |
| --- | --- | --- |
| id | bigint PK | Mã yêu cầu |
| patient_id | int FK | Hồ sơ bệnh nhân cần hỗ trợ |
| patient_user_id | int FK | Tài khoản sở hữu lịch sử |
| subject | nvarchar 150 | Chủ đề hỗ trợ |
| status | tinyint | 0 Chờ, 1 Đang hỗ trợ, 2 Đã kết thúc |
| assigned_receptionist_user_id | int FK nullable | Người phụ trách hiện tại; giữ người cuối khi đóng |
| last_message_sequence | bigint | Thứ tự tin cuối, ban đầu bằng 0 |
| last_event_sequence | bigint | Thứ tự sự kiện cuối, ban đầu bằng 0 |
| patient_last_read_sequence | bigint | Vị trí bệnh nhân đã đọc |
| assigned_last_read_sequence | bigint | Vị trí người phụ trách hiện tại đã đọc |
| created_at và updated_at | datetime2 UTC | Thời điểm tạo và thay đổi |
| last_message_at | datetime2 UTC nullable | Thời điểm tin mới nhất |
| assigned_at | datetime2 UTC nullable | Thời điểm giao cho người hiện tại |
| closed_at | datetime2 UTC nullable | Thời điểm kết thúc |
| closed_by_user_id | int FK nullable | Ai kết thúc |
| close_reason | tinyint nullable | Mã lý do kết thúc |
| row_version | rowversion | Kiểm soát cập nhật đồng thời |

Ràng buộc: status chỉ nhận 0, 1, 2; Chờ phải không có người phụ trách; Đang hỗ trợ phải có người phụ trách; Đã kết thúc phải có mốc và người đóng. Dùng unique filtered index trên patient_id với status nhỏ hơn 2 để bảo đảm một yêu cầu đang mở cho mỗi bệnh nhân. Các vị trí đã đọc nằm từ 0 đến last_message_sequence.

Chỉ mục đọc dự kiến phục vụ lịch sử theo patient_user_id và thời gian, hàng chờ theo status và created_at, danh sách được giao theo assigned_receptionist_user_id và status. Không xóa dây chuyền dữ liệu chat khi tài khoản hoặc hồ sơ thay đổi; giữ khóa ngoại và quy trình khóa hoặc xóa mềm của project.

### 6 2 Bảng tin nhắn

Mỗi dòng là một tin đã được lưu thành công. Tác giả của tin không thay đổi khi bàn giao hội thoại.

| Cột | Kiểu dự kiến | Ý nghĩa nghiệp vụ |
| --- | --- | --- |
| id | bigint PK | Mã tin |
| conversation_id | bigint FK | Yêu cầu chứa tin |
| message_sequence | bigint | Thứ tự tăng dần trong yêu cầu |
| sender_user_id | int FK nullable | Tài khoản gửi thật; để trống với tin System |
| sender_side | tinyint | Patient, Receptionist hoặc System, server xác định |
| sender_name_snapshot | nvarchar 150 | Tên tại lúc gửi; System dùng nhãn Tin nhắn tự động |
| message_kind | tinyint | Text hoặc AutoAcknowledgement |
| content | nvarchar 2000 | Nội dung văn bản |
| client_message_id | uniqueidentifier | Mã ổn định khi retry; server tự cấp cho tin System |
| sent_at | datetime2 UTC | Thời gian server xác nhận lưu |

Unique theo conversation_id và message_sequence để xác định thứ tự; unique theo sender_user_id và client_message_id để chống lưu trùng. Tin đầu tiên cũng dùng cơ chế này. Cùng mã gửi lại cùng nội dung trả kết quả đã lưu trong phạm vi được phép; tái sử dụng mã cho nội dung hoặc yêu cầu khác bị từ chối. Kiểm tra quyền vẫn phải chạy, kể cả khi retry.

Thêm unique filtered index theo conversation_id cho message_kind bằng AutoAcknowledgement để mỗi yêu cầu chỉ có một tin xác nhận. sender_user_id chỉ được trống khi sender_side là System; tin của patient và lễ tân phải có tài khoản. Loại tin và tác giả System do server tự tạo, client không được tự gửi giả tin hệ thống. Nội dung đã lưu giữ nguyên khi cấu hình giờ hỗ trợ được đổi về sau.

Giới hạn ban đầu là 2000 ký tự mỗi tin, trim đầu cuối và không chấp nhận nội dung rỗng. Hiển thị như văn bản, không render HTML do người dùng nhập. Không sửa hoặc xóa tin trong bản đầu. Patient DTO chỉ có phía và loại tin, nhãn công khai, nội dung, thời gian và thứ tự; không trả sender_user_id hoặc sender_name_snapshot của nhân viên.

### 6 3 Bảng nhật ký xử lý

Mỗi dòng là một hành động làm thay đổi quy trình. Bảng này giải thích ai đã nhận, ai chuyển và vì sao; không lưu lại nội dung từng tin nhắn.

| Cột | Kiểu dự kiến | Ý nghĩa nghiệp vụ |
| --- | --- | --- |
| id | bigint PK | Mã sự kiện |
| conversation_id | bigint FK | Yêu cầu liên quan |
| event_sequence | bigint | Thứ tự sự kiện trong yêu cầu |
| event_type | tinyint | Created, Claimed, Released, Transferred hoặc Closed |
| actor_user_id | int FK | Người thực hiện hành động |
| from_assignee_user_id | int FK nullable | Người phụ trách trước |
| to_assignee_user_id | int FK nullable | Người phụ trách sau |
| message_sequence_at_event | bigint | Mốc tin cuối tại thời điểm hành động |
| reason | nvarchar 500 nullable | Lý do nội bộ nếu áp dụng |
| occurred_at | datetime2 UTC | Thời gian server ghi nhận |

Unique theo conversation_id và event_sequence. Chỉ thêm sự kiện, không chỉnh sửa nhật ký qua API. reason bắt buộc khi trả về hàng chờ, chuyển người hoặc Admin đóng. Các trường người trước và sau phụ thuộc loại hành động; giao từ hàng chờ có người trước để trống. Created gắn với tin đầu tiên.

Mốc tin được lưu cùng giao dịch với thay đổi người phụ trách. Không chỉ dựa vào so sánh timestamp để xác định trước hay sau bàn giao. Tác giả trong messages cho biết ai nhắn; events cho biết ai được giao xử lý. Được giao không phải bằng chứng người đó đã đọc mọi tin.

### 6 4 Vì sao ba bảng đủ

Quyền đọc dựa vào chủ sở hữu hoặc một người phụ trách; không có nhiều nhân viên cùng chat. Hai vị trí đã đọc được đặt trên conversations. Khi đổi người, vị trí của người phụ trách được đặt lại bằng 0 để người mới xem lại; bản đầu không giữ trạng thái đã đọc riêng của từng lễ tân cũ.

Nếu cần mời B cùng tham gia trong khi A vẫn phụ trách, hoặc giữ quyền và vị trí đã đọc cho nhiều nhân viên, khi đó mới bổ sung support_participants. Bàn giao A sang B, truy vết tác giả và lịch sử xử lý hiện tại đều thực hiện được với ba bảng.

## 7 Ví dụ dữ liệu và trách nhiệm xử lý

Ví dụ một bệnh nhân gửi yêu cầu đổi lịch; A nhận rồi Admin chuyển sang B. Các tên và mã dưới đây dùng để minh họa.

| Thời gian | Hành động | Dữ liệu được ghi |
| --- | --- | --- |
| 08 giờ 59 | Bệnh nhân gửi tin 1 | Tạo conversation Chờ, message 1 |
| 08 giờ 59 | Hệ thống xác nhận ngay | Tạo message 2 loại AutoAcknowledgement và event Created, cùng giao dịch với tin 1 |
| 09 giờ 00 | A tiếp nhận | assigned bằng A, status Đang hỗ trợ, event Claimed tại mốc tin 2 |
| 09 giờ 01 | A gửi tin 3 | sender_user_id bằng A, message_sequence bằng 3 |
| 09 giờ 02 | Bệnh nhân gửi tin 4 | sender_user_id bằng bệnh nhân, message_sequence bằng 4; không thêm xác nhận |
| 09 giờ 05 | Admin chuyển A sang B | assigned bằng B, event Transferred tại mốc tin 4, lưu lý do |
| 09 giờ 06 | B gửi tin 5 | sender_user_id bằng B, message_sequence bằng 5 |
| 09 giờ 15 | B kết thúc | status Đã kết thúc, closed_by bằng B, event Closed |

Patient thấy các tin 1 đến 5; tin 2 mang nhãn Tin nhắn tự động, tin 3 và 5 mang nhãn Hỗ trợ phòng khám. Patient không thấy sự kiện lúc 09 giờ 05. Lễ tân được phép đọc thấy rõ tin 3 do A gửi, tin 5 do B gửi và timeline bàn giao. Admin đọc metadata cùng sự kiện, không đọc nội dung tin 1 đến 5.

Tin 1 được gửi trước khi có người nhận; sự kiện lúc 09 giờ 00 cho biết A bắt đầu tiếp nhận cả yêu cầu và phần tin đang chờ. Không gắn nhầm tác giả tin patient thành người phụ trách. B nhận toàn bộ ngữ cảnh để tiếp tục giải quyết; bàn giao không tách hoặc viết lại lịch sử.

## 8 Quy tắc xử lý và tính nhất quán

| Mã | Quy tắc bắt buộc |
| --- | --- |
| BR01 | Chủ sở hữu và tác giả lấy từ JWT và database, không tin UserId hoặc PatientId client gửi |
| BR02 | Một bệnh nhân có tối đa một yêu cầu còn mở, bảo đảm bằng ràng buộc database |
| BR03 | Chỉ tạo yêu cầu khi tin đầu tiên được lưu; không để hội thoại trống |
| BR04 | Một yêu cầu đang hỗ trợ có đúng một người phụ trách |
| BR05 | Chỉ người phụ trách hiện tại được trả lời; đã kết thúc thì không gửi thêm |
| BR06 | Tiếp nhận đồng thời chỉ có một người thành công; xử lý xung đột có thông báo |
| BR07 | Bàn giao đổi quyền ngay; người cũ không được đọc hoặc gửi tiếp |
| BR08 | Mỗi thay đổi quy trình và sự kiện tương ứng lưu trong cùng giao dịch |
| BR09 | Tin đã lưu giữ nguyên tác giả; không đổi tên tác giả theo người phụ trách hiện tại |
| BR10 | Không làm lộ sự kiện, lý do hoặc danh tính nhân viên trong API patient |
| BR11 | Ngắt mạng hoặc logout không tự kết thúc hay trả yêu cầu về hàng chờ |
| BR12 | Retry cùng một lần gửi không tạo tin hoặc yêu cầu trùng |
| BR13 | Chỉ đánh dấu đã đọc khi nội dung đã hiển thị trong khung đang được xem |
| BR14 | Tài khoản khóa, xóa mềm hoặc mất role bị từ chối theo quy tắc truy cập hiện hành |
| BR15 | Một xác nhận System ngay sau tin đầu mỗi yêu cầu; dùng chung nội dung, không gửi lặp hoặc theo hẹn giờ |

### 8 1 Giao dịch và thứ tự

Tạo yêu cầu lưu conversation, tin đầu của patient, tin xác nhận System và event Created cùng giao dịch; nếu một phần lỗi thì rollback toàn bộ. Tin patient có sequence 1, tin xác nhận có sequence 2 và event Created ghi mốc 2. Tiếp nhận dùng cập nhật có điều kiện; một giao dịch đổi trạng thái và thêm event Claimed. Bàn giao, trả về hàng chờ và đóng cũng cập nhật trạng thái cùng event. Khi gửi tin, kiểm tra quyền và trạng thái dưới cơ chế đồng bộ trên conversation, cấp message_sequence rồi lưu tin và tăng bộ đếm.

Các thao tác trên cùng conversation phải được tuần tự hóa bằng cơ chế khóa hoặc cập nhật phiên bản phù hợp SQL Server. Gửi tin không được kiểm tra quyền ở một bước rồi chèn ở bước khác sau khi quyền đã đổi. Nhờ vậy mốc sequence lúc bàn giao có ý nghĩa, và không có tin gửi sau khi người gửi mất quyền hoặc yêu cầu đã đóng.

### 8 2 Đã đọc và chưa đọc

patient_last_read_sequence và assigned_last_read_sequence chỉ tăng trong một lần phân công, không vượt quá tin đã tồn tại. Số chưa đọc là số tin từ phía đối diện có sequence lớn hơn vị trí đã đọc; loại trừ tin System. Không lấy tổng sequence trừ vị trí vì sẽ tính cả tin do chính phía đó gửi và tin tự động.

Ví dụ patient đã đọc đến 3, sau đó patient gửi tin 4 và nhân viên gửi tin 5 thì patient chỉ có một tin chưa đọc. Khi giao cho người mới, assigned_last_read_sequence đặt về 0; người đó xem lại và cập nhật vị trí của mình. Việc nhận được sự kiện SignalR hoặc đóng khung không tự đánh dấu đã đọc. Những thiết bị cùng tài khoản đồng bộ cùng vị trí đọc của tài khoản đó.

Thời gian lưu UTC, hiển thị giờ phòng khám GMT cộng 7. Dùng sequence cho thứ tự và tải bù, không dùng giờ máy người dùng. Không ghi nội dung chat hoặc JWT vào log ứng dụng. Chỉ log mã yêu cầu, mã tin, mã lỗi và định danh phục vụ truy vết theo chính sách của project.

## 9 API dự kiến và dữ liệu trả về

Đây là hợp đồng đề xuất để triển khai theo từng luồng. Các đường dẫn me, reception và admin giúp tách quyền và hình dạng dữ liệu. Mọi API dữ liệu chat dùng xác thực và Cache Control no store.

### 9 1 API bệnh nhân

| Phương thức | Đường dẫn sau /api/support/me | Chức năng |
| --- | --- | --- |
| GET | /conversations | Danh sách của mình, lọc trạng thái và phân trang |
| POST | /conversations | Tạo yêu cầu với tin đầu và clientMessageId; trả kèm xác nhận System |
| GET | /conversations/{id}/messages | Đọc lịch sử hoặc tải bù của mình |
| POST | /conversations/{id}/messages | Gửi content và clientMessageId |
| POST | /conversations/{id}/read | Cập nhật vị trí đã đọc |
| POST | /conversations/{id}/close | Rút hoặc kết thúc yêu cầu của mình |
| GET | /unread-summary | Tổng số tin phía nhân viên chưa đọc |

Patient DTO không có trường người phụ trách, sender_user_id của nhân viên, tên nhân viên, người chuyển, lý do hoặc sự kiện nội bộ. Các trạng thái công khai chỉ gồm Chờ tiếp nhận, Đang hỗ trợ, Đã kết thúc; nhãn nhân viên luôn là Hỗ trợ phòng khám. Tin xác nhận do server thêm và mang nhãn Tin nhắn tự động; không có endpoint cho client yêu cầu gửi System.

### 9 2 API lễ tân

| Phương thức | Đường dẫn sau /api/support/reception | Chức năng |
| --- | --- | --- |
| GET | /conversations | scope waiting, mine hoặc closed; bộ lọc và phân trang |
| GET | /conversations/{id}/messages | Nội dung trong phạm vi được giao |
| POST | /conversations/{id}/claim | Tiếp nhận có kiểm soát đồng thời |
| POST | /conversations/{id}/messages | Trả lời yêu cầu đang giao |
| POST | /conversations/{id}/read | Cập nhật vị trí người phụ trách đã đọc |
| POST | /conversations/{id}/release | Trả về hàng chờ kèm lý do |
| POST | /conversations/{id}/close | Kết thúc yêu cầu đang phụ trách |
| GET | /conversations/{id}/events | Nhật ký cùng phạm vi được phép |

### 9 3 API Admin

| Phương thức | Đường dẫn sau /api/support/admin | Chức năng |
| --- | --- | --- |
| GET | /conversations | Danh sách metadata và các bộ lọc |
| GET | /receptionists | Lễ tân đủ role và hoạt động, kèm số đang phụ trách |
| GET | /conversations/{id}/events | Nhật ký xử lý |
| POST | /conversations/{id}/assign | Giao một yêu cầu đang chờ |
| POST | /conversations/{id}/transfer | Chuyển người đang phụ trách, có lý do |
| POST | /conversations/{id}/release | Đưa yêu cầu về hàng chờ, có lý do |
| POST | /conversations/{id}/close | Đóng yêu cầu, có lý do |

Không có API Admin đọc hoặc gửi messages trong phạm vi này. Assign và transfer kiểm tra người nhận đang có role Receptionist hoạt động; không được giao cho Admin chỉ có role Admin. Các thao tác điều phối dùng expectedVersion từ row_version để phát hiện màn hình đã cũ.

### 9 4 Phân trang và lỗi

Danh sách yêu cầu dùng page từ 1, pageSize mặc định 20 và tối đa 100. Tin nhắn dùng cursor beforeSequence để đọc cũ hoặc afterSequence để tải bù; không gửi cả hai cùng lúc. Mặc định 30 tin, tối đa 100; có hasMore và nextCursor. Sự kiện có eventSequence và phân trang tương tự. Thứ tự hiển thị tin tăng dần, danh sách yêu cầu dùng thời gian cùng id để ổn định khi bằng giờ.

401 cho phiên không hợp lệ; 403 cho sai vai trò; 404 khi mã không có hoặc ngoài phạm vi truy cập; 400 cho dữ liệu không hợp lệ; 409 cho trạng thái hoặc phiên bản xung đột. Trường hợp nhận trùng từ hàng chờ trả 409 và yêu cầu tải lại. Có giới hạn tốc độ cho tạo yêu cầu và gửi tin; ngưỡng cụ thể được cấu hình và kiểm thử trước deploy.

## 10 Kiến trúc và SignalR

Giữ kiến trúc Blazor, API, Application, Infrastructure và SQL Server hiện có. Web đang là Blazor Server; kết nối SignalR của Blazor phục vụ giao diện trình duyệt. Chat bổ sung Hub nghiệp vụ trong API tại /hubs/support-chat; không coi kết nối Blazor sẵn có là đã hoàn thành module chat.

Phương án client: một SupportChatStateService theo phạm vi phiên giao diện, dùng SignalR .NET Client từ server Web sang API, kèm JWT của đúng tài khoản qua AccessTokenProvider. Không dùng singleton giữ token hoặc nội dung chat dùng chung. Trình duyệt vẫn kết nối với Web qua Blazor. ApiSettings BaseUrl dùng lại để xác định hostname API và ghép đường dẫn Hub.

Luồng lưu tin là UI gọi API, API kiểm tra quyền, lưu SQL, commit rồi phát sự kiện SignalR. HTTP xác nhận mã tin sau khi lưu thành công. SignalR là kênh thông báo thay đổi; SQL là nguồn lịch sử. Các thao tác ghi vẫn đi qua một bộ quy tắc Application service, không tạo luồng ghi riêng bỏ qua kiểm tra qua Hub.

### 10 1 Sự kiện realtime

| Sự kiện đề xuất | Người nhận | Dữ liệu tối thiểu |
| --- | --- | --- |
| SupportConversationChanged | Chủ tài khoản patient và lễ tân liên quan | Mã yêu cầu, vị trí thay đổi để tải lại |
| SupportInboxChanged | Nhóm lễ tân đủ quyền | Tín hiệu danh sách cần làm mới, không có nội dung tin |
| SupportDispatchChanged | Nhóm Admin đủ quyền | Tín hiệu metadata cần làm mới |
| SupportReadChanged | Người liên quan | Mã yêu cầu và vị trí đọc công khai phù hợp |

Hub xác thực tài khoản và tự xác định nhóm theo quyền, không nhận UserId hoặc tên nhóm tùy ý từ client. Các nhóm không thay thế kiểm tra quyền. Payload phát rộng không chứa nội dung, tên bệnh nhân hoặc thông tin bàn giao; giao diện tải dữ liệu qua API theo quyền hiện tại. Điều này hạn chế lộ dữ liệu nếu kết nối cũ chưa kịp rời nhóm sau khi thay đổi role.

### 10 2 Reconnect và phiên đăng nhập

Khi mất kết nối, báo Đang kết nối lại và giữ rõ trạng thái những tin chưa được API xác nhận. Khi kết nối lại, đăng ký nhận sự kiện rồi tải dữ liệu mới theo sequence; lặp tải bù khi còn trang và gộp theo mã tin. Làm mới quyền và danh sách được giao; không giả định nhóm hoặc quyền cũ còn hiệu lực.

Khi access token hết hạn, thực hiện refresh theo cơ chế hiện có rồi nối lại Hub; nếu thất bại thì ngắt chat và yêu cầu đăng nhập. Cấu hình đóng kết nối theo hết hạn phù hợp JWT và kiểm tra tài khoản hiện hành ở các API. Logout hoặc chuyển tài khoản phải dispose kết nối, hủy đăng ký handler và xóa state của tài khoản cũ.

Không có bảng outbox trong phương án ba bảng. Nếu API dừng sau commit nhưng trước phát sự kiện, tin vẫn nằm trong SQL; khi reconnect, quay lại tab hoặc đối soát định kỳ sẽ tải được. Đối soát dự kiến mỗi 30 giây khi giao diện liên quan đang mở để bù trường hợp mất sự kiện; cập nhật trực tiếp bình thường vẫn dùng SignalR. Không cam kết thông báo đúng một lần; chống trùng tin được bảo đảm ở bước lưu và khi gộp dữ liệu.

## 11 Quy trình local và production

### 11 1 Local

Dùng SQL Server local riêng, thêm schema và ba bảng bằng script bổ sung theo Database First. Chuẩn bị hai tài khoản bệnh nhân, hai lễ tân và một Admin trong dữ liệu thử riêng. Không chạy seed hoặc thử nghiệm trên database production hoặc database dùng chung.

Web dùng API local từ ApiSettings BaseUrl; Hub dự kiến http://localhost:5238/hubs/support-chat. Chạy Web và API, mở các phiên trình duyệt độc lập để thử đúng actor. Các bảng và hợp đồng API giống production; khác ở địa chỉ, database và cấu hình môi trường.

### 11 2 Cấu hình dự kiến

| Khóa cấu hình | Local | Production |
| --- | --- | --- |
| ApiSettings BaseUrl của Web | API localhost | Hostname HTTPS API Azure |
| ConnectionStrings ClinicDb của API | Database local riêng | Database production |
| SupportChat Enabled | Bật khi phát triển | Bật sau khi schema và hai ứng dụng sẵn sàng |
| SupportChat Hotline | Số thử được cấu hình | Hotline thật đã xác nhận |
| SupportChat MaxMessageLength | 2000 | 2000, đồng nhất schema |
| SupportChat ReconciliationIntervalSeconds | 30 | 30, có thể điều chỉnh sau kiểm thử |
| Giờ hỗ trợ | Giá trị thử | Theo giờ trực thực tế của phòng khám |

Tên khóa trong bảng dùng dấu cách cho dễ đọc; khi triển khai dùng cấu trúc dấu hai chấm trong .NET và hai dấu gạch dưới trên Azure, ví dụ ApiSettings__BaseUrl. Đây là các khóa dự kiến cần hiện thực hóa, không phải cấu hình đã tồn tại đầy đủ.

### 11 3 Deploy trên Azure

1. Kiểm tra App Service Plan, số instance, giới hạn kết nối và cấu hình đang áp dụng. README mô tả Web và API trên App Service Linux B1; phải đối chiếu cấu hình thực trước rollout.
2. Chuẩn bị script chỉ bổ sung schema, ba bảng, ràng buộc và chỉ mục; sao lưu theo quy trình môi trường. Không chạy lại bộ init hoặc seed local.
3. Cấp quyền dữ liệu cần thiết cho ứng dụng trên các bảng mới; tách quyền chạy script schema khỏi quyền ứng dụng nếu môi trường hỗ trợ.
4. Deploy API và Web tương thích, kiểm tra ApiSettings__BaseUrl, kết nối database và cờ bật module. Workflow hiện không tự cập nhật schema.
5. Với Hub chạy trực tiếp trên App Service, kiểm tra WebSockets và Session affinity theo kiểu hosting. Kiểm tra cả Web Blazor Server và API Chat Hub. CORS chỉ áp dụng đúng khi có browser client gọi khác origin; kết nối .NET từ server Web không giải quyết bằng cách mở CORS tùy ý.
6. Kiểm tra Always On và hành vi khởi động lại theo khả năng gói để giảm gián đoạn và thời gian khởi động. Chat không có worker nhắc lịch; Always On không thay bước reconnect hoặc lưu SQL.
7. Bắt đầu với một API instance đã xác nhận phù hợp. Nếu chạy nhiều instance, bổ sung Azure SignalR Service hoặc backplane trước khi scale để chuyển sự kiện giữa các instance. Không chỉ bật thêm instance rồi coi chat đã hoạt động đồng bộ.
8. Chạy kịch bản nghiệm thu trên URL production, với thiết bị hoặc mạng khác nhau. Tắt Web và API local để chứng minh website không phụ thuộc máy phát triển.

Azure SignalR Service là tùy chọn quản lý và mở rộng kết nối, không phải điều kiện để SignalR chạy trên Azure. Cả phương án Hub trực tiếp và dịch vụ Azure đều phải vượt qua các ca nghiệm thu production. Lịch sử vẫn lưu trong SQL trong cả hai phương án.

Khi rollback ứng dụng, tắt module nếu cần và giữ nguyên các bảng cùng tin đã lưu. Không tự DROP bảng có dữ liệu để rollback. API và Web phiên bản cũ phải tiếp tục chạy với schema bổ sung; không sửa luồng lịch khám, bệnh án hoặc storage hiện có.

## 12 Kế hoạch triển khai theo từng luồng

Thực hiện từng phần từ backend đến frontend. Mỗi phần có DTO, repository nếu cần, service, controller, kiểm tra Swagger, state service, UI và kiểm thử tương ứng. Không hoàn thành toàn bộ backend rồi mới bắt đầu giao diện.

| Bước | Công việc | Điều kiện hoàn thành |
| --- | --- | --- |
| 0 | Chốt đặc tả, tên cột, script bổ sung và dữ liệu thử local | Ràng buộc và hợp đồng rõ; có phương án rollout và rollback |
| 1 | Tạo yêu cầu, một xác nhận tự động, gửi tin, lịch sử và khung patient | Đúng chủ sở hữu; xác nhận ngay và duy nhất; tải lại hoặc retry không tạo trùng |
| 2 | Hàng chờ, tiếp nhận, trả lời và UI lễ tân | Hai lễ tân nhận đồng thời chỉ một người thành công |
| 3 | Trả về hàng chờ, Admin điều phối, kết thúc và nhật ký | Đổi quyền ngay; patient không nhận dữ liệu nội bộ |
| 4 | Hub, badge, read cursor, refresh token, reconnect và logout | Các tài khoản cập nhật trực tiếp và tải bù đủ sau mất mạng |
| 5 | Kiểm thử tự động, SQL đồng thời và giao diện responsive | Các ca quyền, trạng thái, retry và xung đột đều đạt |
| 6 | Cấu hình Azure, cập nhật schema có kiểm soát, deploy API và Web | Demo realtime và lịch sử trên production thành công |

### 12 1 Phạm vi mã nguồn dự kiến

Database có script bổ sung module. Infrastructure bổ sung ba model, mapping DbContext và repository theo quy ước hiện hành. Application bổ sung DTO tách actor, service xử lý nghiệp vụ, quy tắc quyền và phát tín hiệu sau commit. API bổ sung controller theo actor, Chat Hub, đăng ký dependency, cấu hình JWT cho kết nối và giới hạn request.

Web bổ sung SupportChatStateService, khung chat patient, trang Reception Support, trang Admin Support, menu, CSS trắng xanh nhạt và hook dọn state khi logout hoặc đổi tài khoản. Tận dụng ApiSettings BaseUrl và cơ chế refresh token hiện có. Tests bổ sung kiểm thử service, API JWT, SQL transaction và UI. Các tên file cụ thể được chốt sau khi rà lại cấu trúc khi bắt đầu lập trình.

## 13 Kiểm thử và tiêu chí nghiệm thu

### 13 1 Các ca kiểm thử bắt buộc

| Mã | Ca kiểm thử | Kết quả cần đạt |
| --- | --- | --- |
| T01 | Patient tạo yêu cầu bằng tin đầu | Lưu yêu cầu, tin patient, một tin System và event cùng giao dịch; cùng mẫu trong và ngoài giờ |
| T02 | Mở khung nhưng chưa gửi | Không tạo yêu cầu rỗng |
| T03 | Hai lần tạo đồng thời của cùng patient | Tối đa một yêu cầu mở, không nhân đôi tin retry |
| T04 | Hai lễ tân nhận cùng lúc | Chỉ một người nhận; người còn lại thấy xung đột |
| T05 | Lễ tân chưa được giao đọc hoặc gửi | Bị từ chối, không lộ nội dung |
| T06 | Patient đổi id để đọc người khác | Bị từ chối; không trả dữ liệu hoặc metadata ngoài phạm vi |
| T07 | Admin gọi API messages | Admin chỉ có role Admin bị từ chối |
| T08 | A gửi tin rồi Admin bàn giao sang B | Tác giả cũ giữ A; B tiếp tục; A mất quyền |
| T09 | Gửi đồng thời bàn giao hoặc đóng | Thứ tự và quyền đúng theo giao dịch; không có tin không hợp lệ |
| T10 | Patient xem phản hồi A và B | Cùng nhãn Hỗ trợ phòng khám; không nhận nhật ký hoặc danh tính nội bộ |
| T11 | Lễ tân trả về hàng chờ | Bỏ người phụ trách, có lý do và sự kiện; người khác nhận tiếp được |
| T12 | Retry hoặc patient nhắn tiếp | Không nhân đôi tin retry, không thêm xác nhận; không gửi tin nhắc theo thời gian |
| T13 | Đánh dấu đã đọc | Chỉ tính tin phía đối diện; không vượt tin hiện có; đồng bộ thiết bị |
| T14 | Logout rồi login cùng tài khoản | Lịch sử và tin chưa đọc còn nguyên |
| T15 | Đổi tài khoản trên cùng trình duyệt | State và kết nối cũ được dọn, không lộ chat trước |
| T16 | Khóa tài khoản hoặc thu hồi role | API từ chối; giao diện làm mới quyền và không tải tiếp nội dung |
| T17 | Token hết hạn | Refresh hoặc yêu cầu login đúng; không giữ Hub trái với phiên |
| T18 | Mất mạng và reconnect | Tải bù đầy đủ, không lặp tin hoặc nhận nhầm hội thoại |
| T19 | API restart sau khi đã lưu tin | Lịch sử còn; reconnect đọc được; yêu cầu không tự đóng |
| T20 | Đã đóng rồi gửi hoặc tạo mới | Gửi cũ bị chặn; tạo yêu cầu mới giữ nguyên lịch sử cũ |
| T21 | Nội dung rỗng, quá dài hoặc chứa HTML | Validation đúng; HTML hiển thị như văn bản |
| T22 | Điện thoại và máy tính | Không tràn ngang, bàn phím không che ô nhập, thao tác đóng rõ |
| T23 | URL production với hai thiết bị | Nhận và trả lời trực tiếp không F5, cả khi local tắt |
| T24 | Lỗi phát sự kiện sau commit | Tin không mất; tải bù hoặc đối soát lấy lại được |

Kiểm thử đồng thời phải dùng SQL Server riêng cho test, không chỉ mock repository. Build các project bị ảnh hưởng, chạy test mới và kiểm tra hồi quy đăng nhập, phân quyền, lịch khám và thông báo hiện có. Chỉ ghi kết quả đạt khi đã chạy và có bằng chứng.

### 13 2 Kịch bản demo production

1. Tắt các tiến trình Web và API local. Mở URL production trên máy tính và điện thoại hoặc phiên trình duyệt độc lập.
2. Patient P gửi yêu cầu, nhận ngay đúng một tin tự động có giờ hỗ trợ. Hai lễ tân A và B thấy hàng chờ mới mà không tải lại trang; patient nhắn tiếp không có xác nhận lặp.
3. A và B thử tiếp nhận cùng lúc; chỉ một người thành công. Người đó trả lời và patient thấy tin ngay.
4. Admin chuyển sang người còn lại có lý do; người cũ mất quyền, người mới tiếp tục. Patient không thấy bàn giao.
5. Patient logout, lễ tân trả lời thêm, patient login lại và đọc được tin chưa đọc.
6. Kết thúc yêu cầu, đọc lại lịch sử và tạo yêu cầu mới. Restart API rồi kiểm tra lịch sử cùng reconnect.

Module hoàn thành khi các luồng, quyền và ca bắt buộc đạt trên local; schema và ứng dụng được triển khai có kiểm soát; chat hoạt động trên production; có bằng chứng demo và tài liệu cấu hình. Việc thêm Hub hoặc có sẵn SignalR của Blazor chưa đủ để nghiệm thu chức năng.

## 14 Các giá trị cần cấu hình khi triển khai

Tên module, ba actor, ba bảng, trạng thái, một người phụ trách, ẩn nhật ký khỏi patient và yêu cầu chạy production đã được quyết định trong tài liệu này. Các mục dưới đây là giá trị vận hành cần điền khi triển khai, không thay đổi mô hình nghiệp vụ:

- Hotline thật và giờ hỗ trợ thực tế của phòng khám.
- Các tài khoản và người chịu trách nhiệm trực hỗ trợ.
- Ngưỡng giới hạn tốc độ gửi tin phù hợp kịch bản sử dụng.
- Số instance và cấu hình App Service thực tế, cùng lựa chọn dịch vụ mở rộng nếu cần.
- Quyền chạy script database và thời điểm rollout production.

Không có thời hạn tự xóa lịch sử trong bản đầu. Chính sách lưu trữ lâu dài, xóa theo quy trình được phê duyệt hoặc xuất dữ liệu sẽ cần đặc tả riêng trước khi thêm thao tác xóa.

## 15 Tài liệu tham chiếu

Thiết kế nghiệp vụ dựa trên các quyết định của module trong trao đổi dự án. Cấu trúc hiện tại đối chiếu README, Program của Web và API, model User và Patient, mapping DbContext, AccountAccessValidationService và quy ước AGENTS của API.

- Microsoft Learn về xác thực và phân quyền SignalR: https://learn.microsoft.com/en-us/aspnet/core/signalr/authn-and-authz?view=aspnetcore-10.0
- Microsoft Learn về người dùng và nhóm SignalR: https://learn.microsoft.com/en-us/aspnet/core/signalr/groups?view=aspnetcore-10.0
- Microsoft Learn về triển khai SignalR trên App Service: https://learn.microsoft.com/en-us/aspnet/core/signalr/publish-to-azure-web-app?view=aspnetcore-10.0
- Microsoft Learn về hosting và mở rộng SignalR: https://learn.microsoft.com/en-us/aspnet/core/signalr/scale?view=aspnetcore-10.0
