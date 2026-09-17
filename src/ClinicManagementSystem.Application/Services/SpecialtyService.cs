using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.DTOs.Specialty;

namespace ClinicManagementSystem.Application.Interfaces.Services;

public interface ISpecialtyService
{
    Task<HttpResponseData<List<SpecialtyDTO>>>
        GetSpecialtiesAsync(bool? active);
}