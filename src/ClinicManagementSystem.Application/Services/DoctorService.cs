using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.DTOs.Doctor;
using ClinicManagementSystem.Infrastructure.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ClinicManagementSystem.Application.Services;


public interface IDoctorService
{
    Task<HttpResponseData<List<DoctorDTO>>>
        GetDoctorsBySpecialtyAsync(
            int specialtyId);
}


