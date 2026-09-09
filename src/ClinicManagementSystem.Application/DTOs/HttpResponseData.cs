namespace ClinicManagementSystem.Application.DTOs;

// Chuẩn hóa dữ liệu trả về từ API
public class HttpResponseData<T>
{
    public int StatusCode { get; set; }

    public string Message { get; set; } = string.Empty;

    public T? Content { get; set; }
}

// Các message liên quan đến User
public static class UserResponseMessageDTO
{
    public const string RegisterSuccess = "Đăng ký tài khoản thành công";

    public const string RegisterFailed = "Đăng ký tài khoản thất bại";

    public const string UsernameAlreadyExists = "Tên đăng nhập đã tồn tại";

    public const string EmailAlreadyExists = "Email đã được sử dụng";

    public const string PhoneAlreadyExists = "Số điện thoại đã được sử dụng";

    public const string PatientRoleNotFound = "Không tìm thấy vai trò bệnh nhân";

    public const string PatientProfileAlreadyLinked =
        "Hồ sơ bệnh nhân đã được liên kết với tài khoản khác";

    public const string InvalidRegisterData =
        "Thông tin đăng ký không hợp lệ";

    public const string RegisterConflict =
        "Không thể hoàn tất đăng ký. Nếu đã có tài khoản hoặc hồ sơ, vui lòng liên hệ phòng khám để được hỗ trợ.";

    public const string ExistingPatientNeedsVerification =
        "Hồ sơ bệnh nhân đã tồn tại. Vui lòng xác minh hồ sơ trước khi liên kết tài khoản.";

    public const string RegisterRetry =
        "Hệ thống đang bận. Vui lòng thử lại sau.";

    public const string PasswordTooLong =
        "Mật khẩu quá dài. Vui lòng sử dụng mật khẩu ngắn hơn.";
}