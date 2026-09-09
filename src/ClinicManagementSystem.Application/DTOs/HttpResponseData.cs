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
    public const string PatientProfileAlreadyLinked = "Hồ sơ bệnh nhân đã được liên kết với tài khoản khác";
}