namespace ClinicManagementSystem.Application.DTOs.Pharmacy;

public class MedicineReceiptItemRequestDTO
{
    public int MedicineId { get; set; }

    public string BatchNo { get; set; } = string.Empty;

    public DateTime ExpiryDate { get; set; }

    public int Quantity { get; set; }

    public decimal ImportPrice { get; set; }
}