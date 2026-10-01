namespace ClinicManagementSystem.Application.DTOs.Service;

public class ServiceDTO
{
    public int Id { get; set; }

    public string? Code { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public decimal Price { get; set; }

    public bool IsActive { get; set; }
}