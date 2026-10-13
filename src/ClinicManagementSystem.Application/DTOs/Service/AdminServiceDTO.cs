namespace ClinicManagementSystem.Application.DTOs.Service;

public class AdminServiceDTO
{
    public int Id { get; set; }
    public string? Code { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? SpecialtyId { get; set; }
    public string? SpecialtyName { get; set; }
    public bool? SpecialtyIsActive { get; set; }
    public decimal Price { get; set; }
    public int? DurationMinutes { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
