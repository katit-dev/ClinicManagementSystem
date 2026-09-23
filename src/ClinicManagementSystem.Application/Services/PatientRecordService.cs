using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.DTOs.PatientRecord;
using ClinicManagementSystem.Infrastructure.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ClinicManagementSystem.Application.Services;


// =====================================================
// PATIENT RECORD SERVICE CONTRACT
// =====================================================

public interface IPatientRecordService
{
    Task<HttpResponseData<List<PatientRecordDTO>>> GetPatientRecordsAsync(
        int currentUserId
    );
}


// =====================================================
// PATIENT RECORD SERVICE
// =====================================================

public class PatientRecordService : IPatientRecordService
{
    private readonly IUnitOfWork _unitOfWork;

    private readonly ILogger<PatientRecordService> _logger;


    // =====================================================
    // CONSTRUCTOR
    // =====================================================

    public PatientRecordService(
        IUnitOfWork unitOfWork,
        ILogger<PatientRecordService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }


    // =====================================================
    // GET PATIENT RECORDS
    // =====================================================

    public async Task<HttpResponseData<List<PatientRecordDTO>>> GetPatientRecordsAsync(
        int currentUserId)
    {
        try
        {
            // GET CURRENT PATIENT
            var patient = await _unitOfWork.PatientRepository
                .WhereSql(p =>
                    p.UserId == currentUserId &&
                    p.IsActive
                )
                .FirstOrDefaultAsync();

            // PATIENT NOT FOUND
            if (patient == null)
            {
                return new HttpResponseData<List<PatientRecordDTO>>
                {
                    StatusCode = 404,
                    Message = "Không tìm thấy bệnh nhân.",
                    Content = null
                };
            }


            // =================================================
            // TEMPORARY RESULT
            //
            // Bước tiếp theo mới query MedicalRecord.
            // =================================================

            return new HttpResponseData<List<PatientRecordDTO>>
            {
                StatusCode = 200,
                Message = "Đã xác định bệnh nhân hiện tại.",
                Content = new List<PatientRecordDTO>()
            };
        }
        catch (Exception ex)
        {
            // LOG ERROR

            _logger.LogError(
                ex,
                "Failed to get current patient for record history. UserId: {UserId}",
                currentUserId
            );


            return new HttpResponseData<List<PatientRecordDTO>>
            {
                StatusCode = 500,
                Message = "Lấy lịch sử khám thất bại.",
                Content = null
            };
        }
    }
}