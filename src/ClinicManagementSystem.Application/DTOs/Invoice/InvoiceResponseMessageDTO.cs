namespace ClinicManagementSystem.Application.DTOs.Invoice;

public static class InvoiceResponseMessageDTO
{
    // =====================================================
    // COMMON
    // =====================================================

    public const string InvalidInvoiceId =
        "Mã hóa đơn không hợp lệ.";

    public const string InvoiceNotFound =
        "Không tìm thấy hóa đơn.";


    // =====================================================
    // GET INVOICE
    // =====================================================

    public const string GetSuccess =
        "Lấy hóa đơn thành công.";

    public const string GetFailed =
        "Không thể lấy thông tin hóa đơn.";


    // =====================================================
    // PAYMENT
    // =====================================================

    public const string PaymentSuccess =
        "Thu tiền thành công.";

    public const string PaymentFailed =
        "Không thể thực hiện thanh toán.";

    public const string PaymentRequestRequired =
        "Thông tin thanh toán không được để trống.";

    public const string PaymentAmountInvalid =
        "Số tiền thanh toán phải lớn hơn 0.";

    public const string PaymentMethodInvalid =
        "Phương thức thanh toán không hợp lệ.";

    public const string PaymentInvoiceCancelled =
        "Hóa đơn đã bị hủy, không thể thu tiền.";

    public const string InvoiceAlreadyPaid =
        "Hóa đơn không còn số tiền cần thanh toán.";

    public const string PaymentAmountExceeded =
        "Số tiền thanh toán vượt quá số tiền còn phải thu.";

    public const string RefundMustUseRefundFunction =
        "Giao dịch hoàn tiền phải được xử lý bằng chức năng hoàn tiền.";


    // =====================================================
    // REFUND
    // =====================================================

    public const string RefundSuccess =
        "Hoàn tiền thành công.";

    public const string RefundFailed =
        "Không thể thực hiện hoàn tiền.";

    public const string RefundRequestRequired =
        "Thông tin hoàn tiền không được để trống.";

    public const string RefundFlagInvalid =
        "Giao dịch này không phải giao dịch hoàn tiền.";

    public const string RefundAmountInvalid =
        "Số tiền hoàn phải lớn hơn 0.";

    public const string RefundInvoiceCancelled =
        "Hóa đơn đã bị hủy, không thể hoàn tiền.";

    public const string RefundNotAvailable =
        "Hóa đơn chưa có khoản tiền đã thu để hoàn.";

    public const string RefundAmountExceeded =
        "Số tiền hoàn không được vượt quá số tiền đã thanh toán.";


    // =====================================================
    // AUTHENTICATION / USER
    // =====================================================

    public const string PaymentUserNotFound =
        "Không xác định được người thực hiện giao dịch.";

    public const string RefundUserNotFound =
        "Không xác định được người thực hiện hoàn tiền.";
}