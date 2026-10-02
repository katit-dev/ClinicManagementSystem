namespace ClinicManagementSystem.Application.DTOs.Pharmacy;

public class StockTransactionDTO
{
    public int Id { get; set; }

    public int MedicineId { get; set; }

    public int BatchId { get; set; }

    public string BatchNo { get; set; } = string.Empty;

    public byte Type { get; set; }

    public int Quantity { get; set; }

    public int QuantityBefore { get; set; }

    public int QuantityAfter { get; set; }

    public int? PrescriptionId { get; set; }

    public int CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public string? Note { get; set; }
}