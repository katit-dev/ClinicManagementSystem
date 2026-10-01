using System.Security.Claims;
using ClinicManagementSystem.Application.DTOs.MedicalRecord;
using ClinicManagementSystem.Application.DTOs.Prescription;
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
    // Response:
    // MedicalRecordExamDTO
    //
    // Bao gồm:
    // - Medical Record
    // - Patient Vital
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

    // =====================================================
    // ADD MEDICAL RECORD SERVICE
    //
    // POST:
    // /api/medical-records/{id}/services
    //
    // Doctor hiện tại lấy từ JWT.
    // =====================================================

    [HttpPost("{id:int}/services")]
    [Authorize(Roles = "Doctor")]
    public async Task<IActionResult> AddMedicalRecordService(
        int id,
        [FromBody] MedicalRecordServiceRequestDTO request)
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
        // ADD SERVICE
        // =================================================

        var result =
            await _medicalRecordService
                .AddMedicalRecordServiceAsync(
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
    // CANCEL MEDICAL RECORD SERVICE
    //
    // PATCH:
    // /api/medical-record-services/{id}/cancel
    // =====================================================

    [HttpPatch("~/api/medical-record-services/{id:int}/cancel")]
    [Authorize(Roles = "Doctor")]
    public async Task<IActionResult> CancelMedicalRecordService(
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
        // CANCEL
        // =================================================

        var result =
            await _medicalRecordService
                .CancelMedicalRecordServiceAsync(
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

    // =====================================================
    // ADD LAB RESULT
    //
    // POST:
    // /api/medical-record-services/{id}/result
    // =====================================================

    [HttpPost(
        "~/api/medical-record-services/{id:int}/result"
    )]
    [Authorize(Roles = "Doctor")]
    public async Task<IActionResult> AddMedicalRecordServiceResult(
        int id,
        [FromBody] LabResultRequestDTO request)
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
        // ADD RESULT
        // =================================================

        var result =
            await _medicalRecordService
                .AddMedicalRecordServiceResultAsync(
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
    // UPLOAD MEDICAL RECORD ATTACHMENT
    //
    // POST:
    // /api/medical-records/{id}/attachments
    //
    // Request:
    // multipart/form-data
    // =====================================================

    [HttpPost("{id:int}/attachments")]
    [Authorize(Roles = "Doctor")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<IActionResult> UploadMedicalRecordAttachment(
        int id,
        IFormFile file)
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
        // VALIDATE FILE
        // =================================================

        if (file == null)
        {
            return BadRequest(
                new
                {
                    statusCode = 400,
                    message =
                        MedicalRecordResponseMessageDTO
                            .AttachmentFileRequired
                }
            );
        }


        // =================================================
        // OPEN FILE STREAM
        // =================================================

        await using var fileStream =
            file.OpenReadStream();


        // =================================================
        // CALL SERVICE
        // =================================================

        var result =
            await _medicalRecordService
                .UploadMedicalRecordAttachmentAsync(
                    id,
                    currentUserId,
                    fileStream,
                    file.FileName,
                    file.ContentType
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
    // CREATE PRESCRIPTION
    //
    // PUT:
    // /api/medical-records/{id}/prescription
    //
    // Doctor lấy từ JWT.
    // =====================================================

    [HttpPut("{id:int}/prescription")]
    [Authorize(Roles = "Doctor")]
    public async Task<IActionResult> CreatePrescription(
        int id,
        [FromBody] PrescriptionRequestDTO request)
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
        // CALL SERVICE
        // =================================================

        var result =
            await _medicalRecordService
                .CreatePrescriptionAsync(
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
    // GET PRESCRIPTION PDF
    //
    // GET:
    // /api/prescriptions/{id}/pdf
    // =====================================================

    [HttpGet("/api/prescriptions/{id:int}/pdf")]
    [Authorize(Roles = "Doctor")]
    public async Task<IActionResult> GetPrescriptionPdf(
        int id)
    {
        var pdf =
            await _medicalRecordService
                .GetPrescriptionPdfAsync(id);


        if (pdf == null)
        {
            return NotFound();
        }


        return File(
            pdf,
            "application/pdf",
            $"prescription-{id}.pdf"
        );
    }

}