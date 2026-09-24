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

public async Task<HttpResponseData<PagedResult<PatientDTO>>> SearchPatientsAsync(
    string? keyword,
    int page)
{
    try
    {
        // =================================================
        // NORMALIZE PAGE
        // =================================================

        if (page < 1)
        {
            page = 1;
        }


        // =================================================
        // NORMALIZE KEYWORD
        // =================================================

        keyword =
            string.IsNullOrWhiteSpace(keyword)
                ? null
                : keyword.Trim();


        // =================================================
        // BASE QUERY
        //
        // Chỉ lấy bệnh nhân đang hoạt động.
        // =================================================

        var query =
            _unitOfWork
                .PatientRepository
                .WhereSql(
                    p => p.IsActive
                );


        // =================================================
        // SEARCH
        //
        // Tìm theo:
        // - Họ tên
        // - Số điện thoại
        // - Mã bệnh nhân
        // =================================================

        if (keyword != null)
        {
            query =
                query.Where(
                    p =>
                        p.FullName.Contains(keyword) ||
                        p.Phone.Contains(keyword) ||
                        p.PatientCode.Contains(keyword)
                );
        }


        // =================================================
        // QUERY + MAP DTO
        //
        // Pagination sẽ làm ở bước tiếp theo.
        // =================================================

        var patients =
            await query
                .OrderBy(
                    p => p.FullName
                )
                .Select(
                    p =>
                        new PatientDTO
                        {
                            Id = p.Id,
                            PatientCode = p.PatientCode,

                            FullName = p.FullName,
                            Gender = p.Gender,
                            DateOfBirth = p.DateOfBirth,

                            Phone = p.Phone,
                            Email = p.Email,
                            Address = p.Address,

                            NationalId = p.NationalId,
                            InsuranceNumber = p.InsuranceNumber,

                            BloodType = p.BloodType
                        }
                )
                .ToListAsync();


        // =================================================
        // TEMPORARY PAGED RESULT
        //
        // Hiện tại chưa Skip / Take.
        // Bước 6.4.4 sẽ thay phần này.
        // =================================================

        var result =
            new PagedResult<PatientDTO>
            {
                Items = patients,

                Page = page,

                PageSize = patients.Count,

                TotalItems = patients.Count,

                TotalPages =
                    patients.Count == 0
                        ? 0
                        : 1
            };


        // =================================================
        // SUCCESS
        // =================================================

        return new HttpResponseData<PagedResult<PatientDTO>>
        {
            StatusCode = 200,
            Message = "Lấy danh sách bệnh nhân thành công.",
            Content = result
        };
    }
    catch (Exception ex)
    {
        // =================================================
        // LOG ERROR
        // =================================================

        _logger.LogError(
            ex,
            "Failed to search patients. Keyword: {Keyword}, Page: {Page}",
            keyword,
            page
        );


        // =================================================
        // ERROR RESPONSE
        // =================================================

        return new HttpResponseData<PagedResult<PatientDTO>>
        {
            StatusCode = 500,
            Message = "Không thể lấy danh sách bệnh nhân.",
            Content = new PagedResult<PatientDTO>()
        };
    }
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