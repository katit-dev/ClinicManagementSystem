namespace ClinicManagementSystem.Application.DTOs.Pharmacy;


// =====================================================
// MEDICINE DTO
//
// Dùng cho:
// - Tìm thuốc
// - Kê đơn
// - Nhập thuốc
//
// GET:
// /api/medicines?keyword=
// =====================================================

public class MedicineDTO
{
    public int Id { get; set; }


    public string? Code { get; set; }


    public string Name { get; set; } =
        string.Empty;


    public string? ActiveIngredient { get; set; }


    public string? Concentration { get; set; }


    public string? Unit { get; set; }


    public decimal Price { get; set; }


    public int StockQuantity { get; set; }


    public bool IsActive { get; set; }
}