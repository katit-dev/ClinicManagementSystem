using System.Security.Claims;
using ClinicManagementSystem.Application.DTOs.MedicalRecord;
using ClinicManagementSystem.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicManagementSystem.Api.Controllers;


// =====================================================
// MEDICAL RECORD CONTROLLER
// =====================================================

[ApiController]
[Route("api/medical-records")]
public class MedicalRecordController : ControllerBase
{
    private readonly IMedicalRecordService
        _medicalRecordService;


    // =====================================================
    // CONSTRUCTOR
    // =====================================================

    public MedicalRecordController(
        IMedicalRecordService medicalRecordService)
    {
        _medicalRecordService =
            medicalRecordService;
    }


    // =====================================================
    // GET MEDICAL RECORD BY APPOINTMENT
    //
    // GET:
    // /api/medical-records?appointmentId=15
    // =====================================================

    [HttpGet]
    [Authorize(Roles = "Patient")]
    public async Task<IActionResult> GetMedicalRecord([FromQuery] int appointmentId)
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!int.TryParse(userIdValue, out var currentUserId))
        {
            return Unauthorized();
        }

        var result = await _medicalRecordService.GetMedicalRecordByAppointmentAsync(appointmentId, currentUserId);

        return StatusCode(result.StatusCode, result);
    }

    // =====================================================
    // GET PATIENT MEDICAL HISTORY
    //
    // GET:
    // /api/medical-records/patient/{patientId}
    // =====================================================

    [HttpGet("patient/{patientId}")]
    [Authorize(Roles = "Doctor,Receptionist")]
    public async Task<IActionResult> GetPatientMedicalRecords(
    int patientId)
    {
        var userIdValue =
            User.FindFirstValue(
                ClaimTypes.NameIdentifier
            );

        if (!int.TryParse(
            userIdValue,
            out var currentUserId))
        {
            return Unauthorized();
        }

        var result =
            await _medicalRecordService
                .GetPatientMedicalRecordsAsync(
                    patientId,
                    currentUserId
                );

        return StatusCode(
            result.StatusCode,
            result
        );
    }

    // =====================================================
    // SAVE MEDICAL RECORD DRAFT
    //
    // PUT:
    // /api/medical-records/{id}
    //
    // id:
    // MedicalRecord.Id
    //
    // Request:
    // MedicalRecordDraftRequestDTO
    //
    // DoctorId lấy từ JWT
    // =====================================================

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Doctor")]
    public async Task<IActionResult> UpdateMedicalRecordDraft(
        int id,
        [FromBody] MedicalRecordDraftRequestDTO request)
    {
        // =================================================
        // GET CURRENT USER ID
        // =================================================

        var userIdValue =
            User.FindFirstValue(
                ClaimTypes.NameIdentifier
            );

        if (!int.TryParse(
            userIdValue,
            out var currentUserId))
        {
            return Unauthorized();
        }


        // =================================================
        // UPDATE MEDICAL RECORD DRAFT
        // =================================================

        var result =
            await _medicalRecordService
                .UpdateMedicalRecordDraftAsync(
                    id,
                    currentUserId,
                    request
                );


        // =================================================
        // RESPONSE
        // =================================================

        return StatusCode(
            result.StatusCode,
            result
        );
    }

    // =====================================================
    // FINALIZE MEDICAL RECORD
    //
    // POST:
    // /api/medical-records/{id}/finalize
    //
    // Doctor hiện tại lấy từ JWT.
    // =====================================================

    [HttpPost("{id}/finalize")]
    [Authorize(Roles = "Doctor")]
    public async Task<IActionResult> FinalizeMedicalRecord(
        int id)
    {
        // =================================================
        // GET CURRENT USER ID
        // =================================================

        var userIdValue =
            User.FindFirstValue(
                ClaimTypes.NameIdentifier
            );


        if (!int.TryParse(
            userIdValue,
            out var currentUserId))
        {
            return Unauthorized();
        }


        // =================================================
        // FINALIZE MEDICAL RECORD
        // =================================================

        var result =
            await _medicalRecordService
                .FinalizeMedicalRecordAsync(
                    id,
                    currentUserId
                );


        // =================================================
        // RETURN RESPONSE
        // =================================================

        return StatusCode(
            result.StatusCode,
            result
        );
    }

    // =====================================================
    // GET MEDICAL RECORD BY ID - DOCTOR
    //
    // GET:
    // /api/medical-records/{id}
    //
    // Doctor hiện tại lấy từ JWT.
    //
    // Cho phép đọc:
    // - Draft
    // - Finalized
    // =====================================================

    [HttpGet("{id:int}")]
    [Authorize(Roles = "Doctor")]
    public async Task<IActionResult> GetMedicalRecordByIdForDoctor(
        int id)
    {
        // =================================================
        // GET CURRENT USER ID
        // =================================================

        var userIdValue =
            User.FindFirstValue(
                ClaimTypes.NameIdentifier
            );


        if (!int.TryParse(
            userIdValue,
            out var currentUserId))
        {
            return Unauthorized();
        }


        // =================================================
        // GET MEDICAL RECORD
        // =================================================

        var result =
            await _medicalRecordService
                .GetMedicalRecordByIdForDoctorAsync(
                    id,
                    currentUserId
                );


        // =================================================
        // RESPONSE
        // =================================================

        return StatusCode(
            result.StatusCode,
            result
        );
    }

}