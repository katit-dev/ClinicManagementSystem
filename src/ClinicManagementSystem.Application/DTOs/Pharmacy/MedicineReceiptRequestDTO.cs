namespace ClinicManagementSystem.Application.DTOs.Pharmacy;

public class MedicineReceiptRequestDTO
{
    public string? SupplierName { get; set; }

    public string? ReferenceCode { get; set; }

    public List<MedicineReceiptItemRequestDTO> Items { get; set; } = [];
}