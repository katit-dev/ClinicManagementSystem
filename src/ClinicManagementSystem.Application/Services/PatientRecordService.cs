using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.DTOs.PatientRecord;

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