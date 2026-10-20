using ClinicManagementSystem.Infrastructure.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ClinicManagementSystem.Application.Services;


// =====================================================
// ATTACHMENT SERVICE CONTRACT
// =====================================================

public interface IAttachmentService
{
    Task<(byte[] FileBytes, string ContentType, string FileName)?> DownloadAttachmentAsync(int attachmentId, int currentUserId);
}


// =====================================================
// ATTACHMENT SERVICE
// =====================================================

public class AttachmentService : IAttachmentService
{
    private readonly IUnitOfWork _unitOfWork;

    private readonly ILogger<AttachmentService> _logger;
    private readonly IFileStorageService _fileStorage;


    // =====================================================
    // CONSTRUCTOR
    // =====================================================

    public AttachmentService(
        IUnitOfWork unitOfWork,
        ILogger<AttachmentService> logger,
        IFileStorageService fileStorage)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _fileStorage = fileStorage;
    }


    // =====================================================
    // DOWNLOAD ATTACHMENT
    // =====================================================

    public async Task<(byte[] FileBytes, string ContentType, string FileName)?> DownloadAttachmentAsync(
        int attachmentId,
        int currentUserId)
    {
        try
        {
            // =================================================
            // GET CURRENT PATIENT
            // =================================================

            var patient = await _unitOfWork.PatientRepository
                .WhereSql(p =>
                    p.UserId == currentUserId &&
                    p.IsActive
                )
                .FirstOrDefaultAsync();


            // =================================================
            // PATIENT NOT FOUND
            // =================================================

            if (patient == null)
            {
                return null;
            }


            // =================================================
            // GET ATTACHMENT
            // =================================================

            var attachment = await _unitOfWork.AttachmentRepository
                .WhereSql(a => a.Id == attachmentId)
                .FirstOrDefaultAsync();


            // =================================================
            // ATTACHMENT NOT FOUND
            // =================================================

            if (attachment == null)
            {
                return null;
            }


            // =================================================
            // CHECK MEDICAL RECORD OWNERSHIP
            // =================================================

            var medicalRecord = await _unitOfWork.MedicalRecordRepository
                .WhereSql(m =>
                    m.Id == attachment.MedicalRecordId &&
                    m.PatientId == patient.Id
                )
                .FirstOrDefaultAsync();


            // =================================================
            // NOT OWNER
            // =================================================

            if (medicalRecord == null)
            {
                return null;
            }


            // Storage access happens only after patient ownership has been verified.
            var fileBytes = await _fileStorage.DownloadAsync(attachment.FileUrl, medicalRecord.Id);
            if (fileBytes == null) return null;

            // =================================================
            // CONTENT TYPE
            // =================================================

            var contentType = string.IsNullOrWhiteSpace(attachment.FileType)
                ? "application/octet-stream"
                : attachment.FileType;


            // =================================================
            // FILE NAME
            // =================================================

            var fileName = Path.GetFileName(attachment.FileUrl.Replace('\\', '/'));


            // =================================================
            // SUCCESS
            // =================================================

            return (
                fileBytes,
                contentType,
                fileName
            );
        }
        catch (Exception ex)
        {
            // =================================================
            // LOG ERROR
            // =================================================

            _logger.LogError(
                ex,
                "Failed to download attachment. AttachmentId: {AttachmentId}, UserId: {UserId}",
                attachmentId,
                currentUserId
            );

            return null;
        }
    }
}
