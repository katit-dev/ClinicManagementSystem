using System.ComponentModel.DataAnnotations;

namespace ClinicManagementSystem.Application.DTOs.Payment;

public class PaymentRequestDTO
{
    // =====================================================
    // AMOUNT
    // =====================================================

    [Range(
        0.01,
        double.MaxValue,
        ErrorMessage = "Số tiền phải lớn hơn 0")]
    public decimal Amount { get; set; }


    // =====================================================
    // METHOD
    // =====================================================

    [Range(
        0,
        3,
        ErrorMessage = "Phương thức thanh toán không hợp lệ")]
    public byte Method { get; set; }


    // =====================================================
    // REFERENCE CODE
    // =====================================================

    [MaxLength(
        100,
        ErrorMessage = "Mã tham chiếu không được vượt quá 100 ký tự")]
    public string? ReferenceCode { get; set; }


    // =====================================================
    // NOTE
    // =====================================================

    [MaxLength(
        255,
        ErrorMessage = "Ghi chú không được vượt quá 255 ký tự")]
    public string? Note { get; set; }


    // =====================================================
    // REFUND
    // =====================================================

    public bool IsRefund { get; set; }
}