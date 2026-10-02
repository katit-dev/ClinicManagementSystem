namespace ClinicManagementSystem.Application.DTOs.Pharmacy;

public class AdjustMedicineStockRequestDTO
{
    public int BatchId { get; set; }

    /// <summary>
    /// Số lượng điều chỉnh:
    /// > 0: cộng tồn
    /// < 0: trừ tồn
    /// </summary>
    public int Quantity { get; set; }

    public string? Reason { get; set; }
}