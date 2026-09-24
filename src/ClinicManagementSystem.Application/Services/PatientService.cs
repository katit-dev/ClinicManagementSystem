using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.DTOs.Patient;
using ClinicManagementSystem.Infrastructure.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ClinicManagementSystem.Application.Services;

public interface IPatientService
{
    Task<HttpResponseData<PatientLookupDTO?>> LookupByPhoneAsync(string phone);

    Task<HttpResponseData<PagedResult<PatientDTO>>> SearchPatientsAsync(string? keyword, int page);

}

public class PatientService : IPatientService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<PatientService> _logger;

    public PatientService(
        IUnitOfWork unitOfWork,
        ILogger<PatientService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    // =====================================================
    // SEARCH PATIENTS
    // =====================================================

    public Task<HttpResponseData<PagedResult<PatientDTO>>> SearchPatientsAsync(string? keyword, int page)
    {
        return Task.FromResult(
            new HttpResponseData<PagedResult<PatientDTO>>
            {
                StatusCode = 501,
                Message = "Chức năng tìm kiếm bệnh nhân đang được triển khai.",
                Content = new PagedResult<PatientDTO>()
            }
        );
    }

    public async Task<HttpResponseData<PatientLookupDTO?>> LookupByPhoneAsync(
        string phone)
    {
        try
        {
            phone = phone.Trim();

            var patient = await _unitOfWork.PatientRepository
                .WhereSql(p => p.Phone == phone)
                .FirstOrDefaultAsync();

            if (patient == null)
            {
                return Response(
                    200,
                    PatientResponseMessageDTO.LookupNotFound);
            }

            var result = new PatientLookupDTO
            {
                FullName = patient.FullName,
                DateOfBirth = patient.DateOfBirth,
                HasAccount = patient.UserId.HasValue
            };

            return Response(
                200,
                PatientResponseMessageDTO.LookupSuccess,
                result);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to lookup patient by phone.");

            return Response(
                500,
                PatientResponseMessageDTO.LookupFailed);
        }
    }

    private static HttpResponseData<PatientLookupDTO?> Response(
        int statusCode,
        string message,
        PatientLookupDTO? content = null)
    {
        return new HttpResponseData<PatientLookupDTO?>
        {
            StatusCode = statusCode,
            Message = message,
            Content = content
        };
    }
}