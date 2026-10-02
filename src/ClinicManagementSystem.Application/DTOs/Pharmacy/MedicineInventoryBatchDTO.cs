namespace ClinicManagementSystem.Application.DTOs.Pharmacy;

public class MedicineInventoryBatchDTO
{
    public int BatchId { get; set; }

    public string BatchNo { get; set; } = string.Empty;

    public DateOnly ExpiryDate { get; set; }

    public int Quantity { get; set; }
}