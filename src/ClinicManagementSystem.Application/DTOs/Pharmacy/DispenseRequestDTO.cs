namespace ClinicManagementSystem.Application.DTOs.Pharmacy;

// =====================================================
// DISPENSE REQUEST DTO
// =====================================================

public class DispenseRequestDTO
{
    public List<DispenseItemRequestDTO> Items { get; set; } = new();
}


// =====================================================
// DISPENSE ITEM REQUEST DTO
// =====================================================

public class DispenseItemRequestDTO
{
    public int PrescriptionItemId { get; set; }

    public int BatchId { get; set; }

    public int Quantity { get; set; }
}