using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.DTOs.MedicalRecord;

namespace ClinicManagementSystem.Application.Services;


// =====================================================
// MEDICAL RECORD SERVICE CONTRACT
// =====================================================

public interface IMedicalRecordService
{
    Task<HttpResponseData<MedicalRecordDTO>>GetMedicalRecordByAppointmentAsync(int appointmentId, int currentUserId);
}