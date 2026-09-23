namespace ClinicManagementSystem.Application.Services;


// =====================================================
// ATTACHMENT SERVICE CONTRACT
// =====================================================

public interface IAttachmentService
{
    Task<(byte[] FileBytes, string ContentType, string FileName)?> DownloadAttachmentAsync(int attachmentId, int currentUserId);
}