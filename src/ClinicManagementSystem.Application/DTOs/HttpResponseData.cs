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
    public const string RegisterSuccess =
        "Đăng ký tài khoản thành công";

    public const string RegisterFailed =
        "Đăng ký tài khoản thất bại";

    public const string UsernameAlreadyExists =
        "Tên đăng nhập đã tồn tại";

    public const string EmailAlreadyExists =
        "Email đã được sử dụng";

    public const string PhoneAlreadyExists =
        "Số điện thoại đã được sử dụng";

    public const string PatientRoleNotFound =
        "Không tìm thấy vai trò bệnh nhân";

    public const string PatientProfileAlreadyLinked =
        "Hồ sơ bệnh nhân đã được liên kết với tài khoản khác";

    public const string InvalidRegisterData =
        "Thông tin đăng ký không hợp lệ";

    public const string InvalidDateOfBirth =
        "Ngày sinh không được lớn hơn ngày hiện tại";

    public const string RegisterConflict =
        "Không thể hoàn tất đăng ký. Nếu đã có tài khoản hoặc hồ sơ, vui lòng liên hệ phòng khám để được hỗ trợ.";

    public const string ExistingPatientNeedsVerification =
        "Hồ sơ bệnh nhân đã tồn tại. Vui lòng xác minh hồ sơ trước khi liên kết tài khoản.";

    public const string RegisterRetry =
        "Hệ thống đang bận. Vui lòng thử lại sau.";

    public const string PasswordTooLong =
        "Mật khẩu quá dài. Vui lòng sử dụng mật khẩu ngắn hơn.";

    // Login
    public const string LoginSuccess =
    "Đăng nhập thành công";

    public const string InvalidCredentials =
        "Tên đăng nhập hoặc mật khẩu không chính xác";

    public const string AccountUnavailable =
        "Không thể đăng nhập. Vui lòng liên hệ phòng khám để được hỗ trợ.";

    public const string LoginFailed =
        "Đăng nhập thất bại";

    // Refresh Token
    public const string RefreshTokenSuccess =
    "Làm mới token thành công";

    public const string InvalidRefreshToken =
        "Refresh token không hợp lệ hoặc đã hết hạn";

    public const string RefreshTokenFailed =
        "Không thể làm mới token";

    // forgot password
    public const string ForgotPasswordSuccess =
    "Nếu email tồn tại trong hệ thống, hướng dẫn đặt lại mật khẩu sẽ được gửi đến email.";

    public const string ForgotPasswordFailed =
        "Không thể xử lý yêu cầu quên mật khẩu";

    public const string EmailNotFound =
    "Email không tồn tại trong hệ thống.";
}

public static class PatientResponseMessageDTO
{
    public const string LookupSuccess =
        "Tìm thấy hồ sơ bệnh nhân";

    public const string LookupNotFound =
        "Không tìm thấy hồ sơ bệnh nhân";

    public const string InvalidPhone =
        "Số điện thoại không hợp lệ";

    public const string LookupFailed =
        "Không thể tra cứu hồ sơ bệnh nhân";
}