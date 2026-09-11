namespace ClinicManagementSystem.Application.DTOs.Auth;

public class AuthResponseDTO
{
    public string AccessToken { get; set; } = string.Empty;

    public string RefreshToken { get; set; } = string.Empty;

    public int ExpiresIn { get; set; }

    public AuthUserDTO User { get; set; } = new();
}

public class AuthUserDTO
{
    public int Id { get; set; }

    public string? FullName { get; set; }

    public List<string> Roles { get; set; } = [];

    public int? DoctorId { get; set; }

    public int? PatientId { get; set; }
}