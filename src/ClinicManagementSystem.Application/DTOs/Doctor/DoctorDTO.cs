namespace ClinicManagementSystem.Application.DTOs.Doctor;

public class DoctorDTO
{
    public int Id { get; set; }

    public int SpecialtyId { get; set; }

    public string FullName { get; set; }
        = string.Empty;

    public string? Title { get; set; }

    public string? Room { get; set; }

    public decimal ConsultationFee { get; set; }

    public string? AvatarUrl { get; set; }

    public int? ExperienceYears { get; set; }
}