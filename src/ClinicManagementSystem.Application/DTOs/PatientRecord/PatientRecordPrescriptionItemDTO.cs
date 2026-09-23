namespace ClinicManagementSystem.Application.DTOs.PatientRecord;


// =====================================================
// PATIENT RECORD PRESCRIPTION ITEM DTO
// =====================================================

public class PatientRecordPrescriptionItemDTO
{
    public int Id { get; set; }

    public string MedicineName { get; set; } = string.Empty;

    public int Quantity { get; set; }

    public string? Dosage { get; set; }

    public string? Instruction { get; set; }

    public int? DurationDays { get; set; }

    public string? Frequency { get; set; }
}