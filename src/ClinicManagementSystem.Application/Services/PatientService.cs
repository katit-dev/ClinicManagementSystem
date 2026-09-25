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

    Task<HttpResponseData<List<PatientAllergyDTO>>> GetPatientAllergiesAsync(int patientId);

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

    public async Task<HttpResponseData<List<PatientAllergyDTO>>>
    GetPatientAllergiesAsync(int patientId)
    {
        try
        {
            // =================================================
            // CHECK PATIENT
            // =================================================

            var patient =
                await _unitOfWork.PatientRepository
                    .WhereSql(p =>
                        p.Id == patientId &&
                        p.IsActive)
                    .FirstOrDefaultAsync();


            if (patient == null)
            {
                return new HttpResponseData<List<PatientAllergyDTO>>
                {
                    StatusCode = 404,
                    Message = "Không tìm thấy hồ sơ bệnh nhân."
                };
            }



            // =================================================
            // GET ALLERGIES
            // =================================================

            var allergies =
                await _unitOfWork.PatientAllergyRepository
                    .WhereSql(a =>
                        a.PatientId == patientId)
                    .ToListAsync();



            // =================================================
            // MAP DTO
            // =================================================

            var result =
                allergies.Select(a =>
                    new PatientAllergyDTO
                    {
                        Id = a.Id,

                        PatientId = a.PatientId,

                        Allergen = a.Allergen,

                        Severity = a.Severity,

                        Note = a.Note
                    }
                )
                .ToList();



            return new HttpResponseData<List<PatientAllergyDTO>>
            {
                StatusCode = 200,

                Message = "Lấy danh sách dị ứng thành công.",

                Content = result
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to get patient allergies. PatientId: {PatientId}",
                patientId
            );


            return new HttpResponseData<List<PatientAllergyDTO>>
            {
                StatusCode = 500,

                Message = "Không thể lấy thông tin dị ứng."
            };
        }
    }

    // =====================================================
    // ADD PATIENT ALLERGY
    // =====================================================

    public async Task<HttpResponseData<PatientAllergyDTO>> AddPatientAllergyAsync(
        int patientId,
        PatientAllergyRequestDTO request)
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
                return new HttpResponseData<PatientAllergyDTO>
                {
                    StatusCode = 404,
                    Message = "Không tìm thấy hồ sơ bệnh nhân."
                };
            }


            // =================================================
            // NORMALIZE INPUT
            // =================================================

            var allergen = request.Allergen.Trim();

            var note = string.IsNullOrWhiteSpace(request.Note)
                ? null
                : request.Note.Trim();


            // =================================================
            // VALIDATE ALLERGEN
            // =================================================

            if (string.IsNullOrWhiteSpace(allergen))
            {
                return new HttpResponseData<PatientAllergyDTO>
                {
                    StatusCode = 400,
                    Message = "Tên chất gây dị ứng không được để trống."
                };
            }


            // =================================================
            // CREATE ALLERGY
            // =================================================

            var allergy = new PatientAllergy
            {
                PatientId = patientId,
                Allergen = allergen,
                Severity = request.Severity,
                Note = note
            };


            // =================================================
            // ADD ALLERGY
            // =================================================

            await _unitOfWork.PatientAllergyRepository.AddAsync(allergy);


            // =================================================
            // SAVE CHANGES
            // =================================================

            await _unitOfWork.SaveChangesAsync();


            // =================================================
            // MAP RESPONSE
            // =================================================

            var result = new PatientAllergyDTO
            {
                Id = allergy.Id,
                PatientId = allergy.PatientId,
                Allergen = allergy.Allergen,
                Severity = allergy.Severity,
                Note = allergy.Note
            };


            // =================================================
            // SUCCESS
            // =================================================

            return new HttpResponseData<PatientAllergyDTO>
            {
                StatusCode = 201,
                Message = "Thêm dị ứng cho bệnh nhân thành công.",
                Content = result
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to add patient allergy. PatientId: {PatientId}",
                patientId);

            return new HttpResponseData<PatientAllergyDTO>
            {
                StatusCode = 500,
                Message = "Không thể thêm thông tin dị ứng."
            };
        }
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
            // =================================================

            patient.FullName = fullName;

            patient.Gender = request.Gender;

            patient.DateOfBirth = request.DateOfBirth;

            patient.Phone = phone;

            patient.Email = email;

            patient.Address = address;

            patient.NationalId = nationalId;

            patient.InsuranceNumber = insuranceNumber;

            patient.BloodType = bloodType;


            // =================================================
            // SAVE CHANGE
            // =================================================

            await _unitOfWork.SaveChangesAsync();


            // =================================================
            // RETURN UPDATED PATIENT
            // =================================================

            var patientDTO = new PatientDTO
            {
                Id = patient.Id,

                PatientCode = patient.PatientCode,

                FullName = patient.FullName,

                Gender = patient.Gender,

                DateOfBirth = patient.DateOfBirth,

                Phone = patient.Phone,

                Email = patient.Email,

                Address = patient.Address,

                NationalId = patient.NationalId,

                InsuranceNumber = patient.InsuranceNumber,

                BloodType = patient.BloodType
            };


            return new HttpResponseData<PatientDTO>
            {
                StatusCode = 200,
                Message = "Cập nhật hồ sơ bệnh nhân thành công.",
                Content = patientDTO
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