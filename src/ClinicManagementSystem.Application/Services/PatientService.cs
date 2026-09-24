using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.DTOs.Patient;
using ClinicManagementSystem.Infrastructure.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ClinicManagementSystem.Application.Helpers;
using ClinicManagementSystem.Infrastructure.Models;

namespace ClinicManagementSystem.Application.Services;

public interface IPatientService
{
    Task<HttpResponseData<PatientLookupDTO?>> LookupByPhoneAsync(string phone);

    Task<HttpResponseData<PagedResult<PatientDTO>>> SearchPatientsAsync(string? keyword, int page);

    Task<HttpResponseData<PatientDTO>> CreatePatientAsync(PatientRequestDTO request);

    Task<HttpResponseData<PatientDTO>> UpdatePatientAsync(int patientId, PatientRequestDTO request);

    Task<HttpResponseData<PatientAllergyDTO>> AddPatientAllergyAsync(int patientId, PatientAllergyRequestDTO request);

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
    // ADD PATIENT ALLERGY
    // =====================================================

    public Task<HttpResponseData<PatientAllergyDTO>> AddPatientAllergyAsync(
        int patientId,
        PatientAllergyRequestDTO request)
    {
        return Task.FromResult(
            new HttpResponseData<PatientAllergyDTO>
            {
                StatusCode = 501,
                Message = "Chức năng thêm dị ứng bệnh nhân đang được triển khai."
            }
        );
    }

    // =====================================================
    // UPDATE PATIENT
    // =====================================================

    public async Task<HttpResponseData<PatientDTO>> UpdatePatientAsync(
        int patientId,
        PatientRequestDTO request)
    {
        try
        {
            // =================================================
            // FIND PATIENT
            // =================================================

            var patient = await _unitOfWork.PatientRepository
                .WhereSql(p => p.Id == patientId && p.IsActive)
                .FirstOrDefaultAsync();

            if (patient == null)
            {
                return new HttpResponseData<PatientDTO>
                {
                    StatusCode = 404,
                    Message = "Không tìm thấy hồ sơ bệnh nhân."
                };
            }


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
            //
            // Chỉ conflict nếu phone thuộc Patient KHÁC.
            // =================================================

            var duplicatePhone = await _unitOfWork.PatientRepository
                .WhereSql(p =>
                    p.Phone == phone &&
                    p.Id != patientId)
                .FirstOrDefaultAsync();

            if (duplicatePhone != null)
            {
                return new HttpResponseData<PatientDTO>
                {
                    StatusCode = 409,
                    Message = "Số điện thoại đã tồn tại trong hồ sơ bệnh nhân khác."
                };
            }


            // =================================================
            // UPDATE
            //
            // Sẽ thực hiện ở bước 6.6.3.
            // =================================================

            return new HttpResponseData<PatientDTO>
            {
                StatusCode = 501,
                Message = "Dữ liệu hợp lệ. Chức năng cập nhật đang được triển khai."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to validate patient update. PatientId: {PatientId}",
                patientId);

            return new HttpResponseData<PatientDTO>
            {
                StatusCode = 500,
                Message = "Không thể xử lý thông tin bệnh nhân."
            };
        }
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
            // CREATE ENTITY
            //
            // Bệnh nhân do Reception tạo là bệnh nhân vãng lai,
            // chưa có tài khoản nên UserId = null.
            // =================================================

            var now = DateTime.UtcNow;

            var patient = new Patient
            {
                UserId = null,

                FullName = fullName,
                Gender = request.Gender,
                DateOfBirth = request.DateOfBirth,

                Phone = phone,
                Email = email,
                Address = address,

                NationalId = nationalId,
                InsuranceNumber = insuranceNumber,
                BloodType = bloodType,

                PatientCode = PatientCodeHelper.Generate(),

                IsActive = true,
                CreatedAt = now
            };


            // =================================================
            // ADD PATIENT
            // =================================================

            await _unitOfWork.PatientRepository.AddAsync(patient);


            // =================================================
            // SAVE CHANGES
            // =================================================

            await _unitOfWork.SaveChangesAsync();


            // =================================================
            // MAP RESPONSE
            // =================================================

            var result = new PatientDTO
            {
                Id = patient.Id,
                PatientCode = patient.PatientCode,

                FullName = patient.FullName,
                Gender = patient.Gender,
                DateOfBirth = patient.DateOfBirth,

                Phone = patient.Phone!,
                Email = patient.Email,
                Address = patient.Address,

                NationalId = patient.NationalId,
                InsuranceNumber = patient.InsuranceNumber,

                BloodType = patient.BloodType
            };


            // =================================================
            // SUCCESS
            // =================================================

            return new HttpResponseData<PatientDTO>
            {
                StatusCode = 201,
                Message = "Tạo hồ sơ bệnh nhân thành công.",
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
                "Failed to create patient.");


            // =================================================
            // ERROR RESPONSE
            // =================================================

            return new HttpResponseData<PatientDTO>
            {
                StatusCode = 500,
                Message = "Không thể tạo hồ sơ bệnh nhân."
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