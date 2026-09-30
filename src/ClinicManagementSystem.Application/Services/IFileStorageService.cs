using System.IO;

namespace ClinicManagementSystem.Application.Services;

// =====================================================
// FILE STORAGE SERVICE CONTRACT
// =====================================================
//
// Dùng để tách business logic khỏi nơi lưu file.
//
// Hiện tại:
// IFileStorageService
//        ↓
// LocalFileStorageService
//
// deploy Azure:
// IFileStorageService
//        ↓
// AzureBlobStorageService
// =====================================================

public interface IFileStorageService
{
    // =================================================
    // UPLOAD FILE
    //
    // Trả về FileUrl để lưu vào Attachment.FileUrl.
    // =================================================

    Task<string> UploadAsync(
        Stream fileStream,
        string fileName,
        string contentType,
        int medicalRecordId);
}