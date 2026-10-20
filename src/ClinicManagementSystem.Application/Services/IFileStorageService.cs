namespace ClinicManagementSystem.Application.Services;

public interface IFileStorageService
{
    // Store an opaque relative key in Attachment.FileUrl, never a public URL or SAS token.
    Task<string> UploadAsync(Stream fileStream, string fileName, string contentType, int medicalRecordId);
    Task<byte[]?> DownloadAsync(string fileReference, int medicalRecordId);
    Task DeleteAsync(string fileReference, int medicalRecordId);
}
