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

    public async Task<HttpResponseData<PatientDTO>> CreatePatientAsync(
        PatientRequestDTO request)
    {
        try
        {
            // =================================================
            // NORMALIZE INPUT
            // =================================================

            var fullName = request.FullName.Trim();
            var phone = request.Phone.Trim();

            var email = string.IsNullOrWhiteSpace(request.Email)
                ? null
                : request.Email.Trim();

            var address = string.IsNullOrWhiteSpace(request.Address)
                ? null
                : request.Address.Trim();

            var nationalId = string.IsNullOrWhiteSpace(request.NationalId)
                ? null
                : request.NationalId.Trim();

            var insuranceNumber = string.IsNullOrWhiteSpace(request.InsuranceNumber)
                ? null
                : request.InsuranceNumber.Trim();

            var bloodType = string.IsNullOrWhiteSpace(request.BloodType)
                ? null
                : request.BloodType.Trim();


            // =================================================
            // VALIDATE REQUIRED DATA
            // =================================================

            if (string.IsNullOrWhiteSpace(fullName))
            {
                return new HttpResponseData<PatientDTO>
                {
                    StatusCode = 400,
                    Message = "Họ tên bệnh nhân không được để trống."
                };
            }

            if (string.IsNullOrWhiteSpace(phone))
            {
                return new HttpResponseData<PatientDTO>
                {
                    StatusCode = 400,
                    Message = "Số điện thoại không được để trống."
                };
            }


            // =================================================
            // VALIDATE DATE OF BIRTH
            // =================================================

            if (request.DateOfBirth.HasValue &&
                request.DateOfBirth.Value > DateOnly.FromDateTime(DateTime.Today))
            {
                return new HttpResponseData<PatientDTO>
                {
                    StatusCode = 400,
                    Message = "Ngày sinh không thể lớn hơn ngày hiện tại."
                };
            }


            // =================================================
            // CHECK DUPLICATE PHONE
            // =================================================

            var existingPatient = await _unitOfWork.PatientRepository
                .WhereSql(p => p.Phone == phone)
                .FirstOrDefaultAsync();

            if (existingPatient != null)
            {
                return new HttpResponseData<PatientDTO>
                {
                    StatusCode = 409,
                    Message = "Số điện thoại đã tồn tại trong hồ sơ bệnh nhân."
                };
            }


            // =================================================
            // CREATE PATIENT
            //
            // Sẽ thực hiện ở bước 6.5.3 - 6.5.4.
            // =================================================

            return new HttpResponseData<PatientDTO>
            {
                StatusCode = 501,
                Message = "Dữ liệu hợp lệ. Chức năng tạo bệnh nhân đang được triển khai."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to validate patient creation request.");

            return new HttpResponseData<PatientDTO>
            {
                StatusCode = 500,
                Message = "Không thể xử lý thông tin bệnh nhân."
            };
        }
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