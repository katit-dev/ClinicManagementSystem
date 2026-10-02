namespace ClinicManagementSystem.Application.DTOs.Pharmacy;

// =====================================================
// PHARMACY PRESCRIPTION LIST RESPONSE DTO
// =====================================================
//
// Response cho danh sách đơn thuốc chờ phát.
//
// Có pagination để không load toàn bộ lịch sử một lần.
// =====================================================

public class PharmacyPrescriptionListResponseDTO
{
    // =================================================
    // DATA
    // =================================================

    public List<PharmacyPrescriptionListItemDTO> Items { get; set; }
        = new();


    // =================================================
    // PAGINATION
    // =================================================

    public int PageNumber { get; set; }

    public int PageSize { get; set; }

    public int TotalCount { get; set; }

    public int TotalPages { get; set; }
}