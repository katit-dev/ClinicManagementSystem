namespace ClinicManagementSystem.Application.DTOs.PatientRecord;


// =====================================================
// PATIENT RECORD PRESCRIPTION DTO
// =====================================================

public class PatientRecordPrescriptionDTO
{
    public int Id { get; set; }

    public string? Note { get; set; }

    public byte Status { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? DispensedAt { get; set; }

    public List<PatientRecordPrescriptionItemDTO> Items { get; set; } = new();
}

