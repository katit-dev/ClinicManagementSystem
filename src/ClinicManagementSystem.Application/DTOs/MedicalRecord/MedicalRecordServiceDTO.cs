namespace ClinicManagementSystem.Application.DTOs.MedicalRecord;

public class MedicalRecordServiceDTO
{
    public int Id { get; set; }

    public int ServiceId { get; set; }

    public string ServiceName { get; set; } = string.Empty;

    public int Quantity { get; set; }

    public decimal UnitPriceSnapshot { get; set; }

    public byte Status { get; set; }

    public DateTime OrderedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public LabResultDTO? Result { get; set; }
}