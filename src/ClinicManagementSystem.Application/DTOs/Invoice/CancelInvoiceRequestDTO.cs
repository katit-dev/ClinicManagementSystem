using System.ComponentModel.DataAnnotations;

namespace ClinicManagementSystem.Application.DTOs.Invoice;

public class CancelInvoiceRequestDTO
{
    [Required(
        ErrorMessage = "Lý do hủy hóa đơn không được để trống")]
    [MaxLength(
        500,
        ErrorMessage = "Lý do hủy không được vượt quá 500 ký tự")]
    public string CancelReason { get; set; }
        = string.Empty;
}