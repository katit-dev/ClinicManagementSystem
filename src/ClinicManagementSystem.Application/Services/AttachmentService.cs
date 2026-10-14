using ClinicManagementSystem.Infrastructure.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;

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
    private readonly string _contentRootPath;


    // =====================================================
    // CONSTRUCTOR
    // =====================================================

    public AttachmentService(
        IUnitOfWork unitOfWork,
        ILogger<AttachmentService> logger,
        IHostEnvironment? environment = null)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _contentRootPath = environment?.ContentRootPath ?? Directory.GetCurrentDirectory();
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


            // =================================================
            // RESOLVE FILE PATH
            // =================================================

            var filePath = attachment.FileUrl;
            var storedUrl = filePath.Replace('\\', '/');
            if (storedUrl.TrimStart('/').StartsWith("uploads/medical-records/", StringComparison.OrdinalIgnoreCase))
            {
                // LocalFileStorageService stores a URL relative to wwwroot,
                // not an absolute filesystem path. Downloads must use the
                // authorized API now that direct static access is blocked.
                var privateRoot = Path.GetFullPath(Path.Combine(_contentRootPath, "wwwroot", "uploads", "medical-records"));
                filePath = Path.GetFullPath(Path.Combine(_contentRootPath, "wwwroot", storedUrl.TrimStart('/')));
                var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
                if (!filePath.StartsWith(privateRoot + Path.DirectorySeparatorChar, comparison)) return null;
            }
            else if (!Path.IsPathRooted(filePath))
            {
                filePath = Path.Combine(
                    _contentRootPath,
                    filePath
                );
            }

            filePath = Path.GetFullPath(filePath);


            // =================================================
            // FILE NOT FOUND
            // =================================================

            if (!File.Exists(filePath))
            {
                return null;
            }


            // =================================================
            // READ FILE
            // =================================================

            var fileBytes = await File.ReadAllBytesAsync(filePath);


            // =================================================
            // CONTENT TYPE
            // =================================================

            var contentType = string.IsNullOrWhiteSpace(attachment.FileType)
                ? "application/octet-stream"
                : attachment.FileType;


            // =================================================
            // FILE NAME
            // =================================================

            var fileName = Path.GetFileName(filePath);


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
