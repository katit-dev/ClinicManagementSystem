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

    Task<HttpResponseData<PatientDTO>> CreatePatientAsync(PatientRequestDTO request);

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
    // CREATE PATIENT
    // =====================================================

    public Task<HttpResponseData<PatientDTO>> CreatePatientAsync(
        PatientRequestDTO request)
    {
        return Task.FromResult(
            new HttpResponseData<PatientDTO>
            {
                StatusCode = 501,
                Message = "Chức năng tạo bệnh nhân đang được triển khai.",
                Content = null
            }
        );
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
            // PAGE SIZE
            // =================================================

            const int pageSize = 10;


            // =================================================
            // TOTAL ITEMS
            // =================================================

            var totalItems =
                await query.CountAsync();


            // =================================================
            // TOTAL PAGES
            // =================================================

            var totalPages =
                totalItems == 0
                    ? 0
                    : (int)Math.Ceiling(
                        totalItems / (double)pageSize
                    );


            // =================================================
            // QUERY + PAGINATION + MAP DTO
            // =================================================

            var patients =
                await query
                    .OrderBy(p => p.FullName)
                    .ThenBy(p => p.Id)
                    .Skip(
                        (page - 1) * pageSize
                    )
                    .Take(
                        pageSize
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
            // PAGED RESULT
            // =================================================

            var result =
                new PagedResult<PatientDTO>
                {
                    Items = patients,
                    Page = page,
                    PageSize = pageSize,
                    TotalItems = totalItems,
                    TotalPages = totalPages
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