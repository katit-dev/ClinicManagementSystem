namespace ClinicManagementSystem.Application.DTOs.Pharmacy;

// =====================================================
// PHARMACY RESPONSE MESSAGE
// =====================================================

public static class PharmacyResponseMessageDTO
{
    // =====================================================
    // COMMON
    // =====================================================

    public const string PrescriptionNotFound =
        "Không tìm thấy đơn thuốc.";

    public const string MedicalRecordNotFound =
        "Không tìm thấy bệnh án.";

    public const string MedicineNotFound =
        "Không tìm thấy thuốc.";

    public const string UserNotFound =
        "Không xác định được người dùng.";


    // =====================================================
    // DISPENSE
    // =====================================================

    public const string PrescriptionIdInvalid =
        "Không xác định được đơn thuốc.";

    public const string DispenseUserInvalid =
        "Không xác định được người phát thuốc.";

    public const string DispenseRequestEmpty =
        "Danh sách thuốc phát không được rỗng.";

    public const string DispenseRequestInvalid =
        "Thông tin phát thuốc không hợp lệ.";

    public const string PrescriptionAlreadyDispensed =
        "Đơn thuốc đã được phát.";

    public const string PrescriptionNotFinalized =
        "Đơn thuốc chưa được chốt.";

    public const string PrescriptionHasNoItems =
        "Đơn thuốc không có thuốc để phát.";

    public const string DispenseItemMissing =
        "Thiếu thông tin phát thuốc cho thuốc.";

    public const string DispenseQuantityInvalid =
        "Số lượng phát thuốc không đúng số lượng kê.";

    public const string PrescriptionItemInvalid =
        "Có thuốc không thuộc đơn thuốc.";

    public const string InsufficientStock =
        "Không đủ tồn kho.";

    public const string InvalidMedicineStock =
        "Tồn tổng của thuốc không hợp lệ.";

    public const string InvalidFefoBatch =
        "Lô thuốc không đúng thứ tự FEFO.";

    public const string DispenseQuantityInsufficient =
        "Không đủ thuốc để phát.";

    public const string DispenseSuccess =
        "Phát thuốc thành công.";

    public const string DispenseFailed =
        "Không thể phát thuốc.";


    // =====================================================
    // REPORT SHORTAGE
    // =====================================================

    public const string ShortageRequestInvalid =
        "Thông tin báo thiếu thuốc không hợp lệ.";

    public const string ShortagePrescriptionNotFinalized =
        "Chỉ có thể báo thiếu thuốc đối với đơn thuốc đã được chốt.";

    public const string ShortageDoctorNotFound =
        "Không tìm thấy bác sĩ của đơn thuốc.";

    public const string MedicineNotInPrescription =
        "Thuốc không thuộc đơn thuốc.";

    public const string ShortageSuccess =
        "Đã báo thiếu thuốc cho bác sĩ.";

    public const string ShortageFailed =
        "Không thể báo thiếu thuốc.";

    // =====================================================
    // PRESCRIPTION LIST
    // =====================================================

    public const string GetPendingPrescriptionsSuccess =
        "Lấy danh sách đơn thuốc thành công.";

    public const string GetPendingPrescriptionsFailed =
        "Không thể tải danh sách đơn thuốc.";

    // =====================================================
    // PRESCRIPTION DETAIL
    // =====================================================

    public const string GetPrescriptionDetailSuccess =
        "Lấy thông tin đơn thuốc thành công.";

    public const string GetPrescriptionDetailFailed =
        "Không thể tải thông tin đơn thuốc.";

    public const string PrescriptionNotAvailableForPharmacy =
        "Đơn thuốc không ở trạng thái chờ phát.";

    public const string InventoryGetSuccess =
    "Lấy tồn kho thành công.";

    public const string InventoryGetFailed =
        "Không thể lấy tồn kho.";

    public const string StockAdjustSuccess =
        "Điều chỉnh tồn kho thành công.";

    public const string StockAdjustFailed =
        "Không thể điều chỉnh tồn kho.";

    public const string StockTransactionGetSuccess =
        "Lấy thẻ kho thành công.";

    public const string StockTransactionGetFailed =
        "Không thể lấy thẻ kho.";

        public const string InvalidUser =
            "Không xác định được người thực hiện.";

        public const string ReceiptInvalidRequest =
            "Thông tin phiếu nhập không hợp lệ.";

        public const string ReceiptDuplicateBatch =
            "Không được nhập trùng thuốc và số lô trong cùng phiếu.";

        public const string MedicineInactive =
            "Thuốc đã ngừng hoạt động.";

        public const string ReceiptBatchExpiryMismatch =
            "Hạn sử dụng của lô thuốc không khớp với dữ liệu hiện tại.";

        public const string ReceiptBatchPriceMismatch =
            "Giá nhập của lô thuốc không khớp với dữ liệu hiện tại.";

        public const string ReceiptBatchNotFound =
            "Không tìm thấy lô thuốc.";

        public const string ReceiptCreateSuccess =
            "Nhập thuốc thành công.";

        public const string ReceiptCreateFailed =
            "Không thể nhập thuốc.";
    }


