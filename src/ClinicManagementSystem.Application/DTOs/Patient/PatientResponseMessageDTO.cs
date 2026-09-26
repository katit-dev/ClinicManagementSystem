namespace ClinicManagementSystem.Application.DTOs.Patient;


public static class PatientResponseMessageDTO
{
    // =====================================================
    // LOOKUP
    // =====================================================

    public const string LookupSuccess =
        "Tìm thấy hồ sơ bệnh nhân";

    public const string LookupNotFound =
        "Không tìm thấy hồ sơ bệnh nhân";

    public const string LookupFailed =
        "Không thể tra cứu hồ sơ bệnh nhân";


    // =====================================================
    // CREATE
    // =====================================================

    public const string CreateSuccess =
        "Tạo hồ sơ bệnh nhân thành công.";

    public const string CreateFailed =
        "Không thể tạo hồ sơ bệnh nhân.";

    public const string PhoneAlreadyExists =
        "Số điện thoại đã tồn tại trong hồ sơ bệnh nhân.";


    // =====================================================
    // UPDATE
    // =====================================================

    public const string UpdateSuccess =
        "Cập nhật hồ sơ bệnh nhân thành công.";

    public const string UpdateFailed =
        "Không thể xử lý thông tin bệnh nhân.";


    // =====================================================
    // ALLERGY
    // =====================================================

    public const string AllergyCreateSuccess =
        "Thêm dị ứng cho bệnh nhân thành công.";

    public const string AllergyCreateFailed =
        "Không thể thêm thông tin dị ứng.";

    public const string AllergyDeleteSuccess =
        "Xóa thông tin dị ứng thành công.";

    public const string AllergyDeleteFailed =
        "Không thể xóa thông tin dị ứng.";

    public const string AllergyNotFound =
        "Không tìm thấy thông tin dị ứng.";


    // =====================================================
    // QUICK CREATE
    // =====================================================

    public const string QuickCreateSuccess =
        "Tạo nhanh bệnh nhân thành công.";

    public const string QuickCreateFailed =
        "Không thể tạo nhanh bệnh nhân.";


    // =====================================================
    // COMMON
    // =====================================================

    public const string PatientNotFound =
        "Không tìm thấy hồ sơ bệnh nhân.";

    public const string InvalidFullName =
        "Họ tên bệnh nhân không được để trống.";

    public const string InvalidPhone =
        "Số điện thoại không hợp lệ";

    public const string InvalidDateOfBirth =
        "Ngày sinh không thể lớn hơn ngày hiện tại.";
}