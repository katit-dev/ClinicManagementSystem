namespace ClinicManagementSystem.Application.DTOs.Pharmacy;

public class MedicineInventoryDTO
{
    public int Id { get; set; }

    public string? Code { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? ActiveIngredient { get; set; }

    public int StockQuantity { get; set; }

    public int MinStock { get; set; }

    public bool IsLowStock { get; set; }

    public string? NearExpiryBatchNo { get; set; }

    public DateOnly? NearExpiryDate { get; set; }

    public int? NearExpiryBatchQuantity { get; set; }

    public bool IsNearExpiry { get; set; }

    public int NearExpiryBatchCount { get; set; }
}