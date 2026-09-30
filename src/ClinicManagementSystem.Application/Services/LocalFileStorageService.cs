using ClinicManagementSystem.Application.Services;
using Microsoft.Extensions.Hosting;

namespace ClinicManagementSystem.Infrastructure.Services;

// =====================================================
// LOCAL FILE STORAGE SERVICE
// =====================================================
// =====================================================

public class LocalFileStorageService : IFileStorageService
{
    private readonly IHostEnvironment _environment;

    public LocalFileStorageService(
        IHostEnvironment environment)
    {
        _environment = environment;
    }


    // =====================================================
    // UPLOAD FILE
    // =====================================================

    public async Task<string> UploadAsync(
        Stream fileStream,
        string fileName,
        string contentType,
        int medicalRecordId)
    {
        // =================================================
        // VALIDATE
        // =================================================

        if (fileStream == null)
        {
            throw new ArgumentNullException(
                nameof(fileStream));
        }

        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException(
                "File name is required.",
                nameof(fileName));
        }

        if (medicalRecordId <= 0)
        {
            throw new ArgumentException(
                "Medical record ID must be greater than zero.",
                nameof(medicalRecordId));
        }


        // =================================================
        // GET FILE EXTENSION
        // =================================================

        var extension =
            Path.GetExtension(fileName);

        if (string.IsNullOrWhiteSpace(extension))
        {
            extension = ".bin";
        }


        // =================================================
        // GENERATE UNIQUE FILE NAME
        // =================================================

        var storedFileName =
            $"{Guid.NewGuid():N}{extension}";


        // =================================================
        // GET WWWROOT
        // =================================================

        var webRootPath =
            Path.Combine(
                _environment.ContentRootPath,
                "wwwroot"
            );


        // =================================================
        // GET UPLOAD DIRECTORY
        //
        // wwwroot
        //   └── uploads
        //       └── medical-records
        //           └── {medicalRecordId}
        // =================================================

        var uploadDirectory =
            Path.Combine(
                webRootPath,
                "uploads",
                "medical-records",
                medicalRecordId.ToString()
            );


        // =================================================
        // CREATE DIRECTORY
        // =================================================

        Directory.CreateDirectory(
            uploadDirectory
        );


        // =================================================
        // FULL FILE PATH
        // =================================================

        var filePath =
            Path.Combine(
                uploadDirectory,
                storedFileName
            );


        // =================================================
        // SAVE FILE
        // =================================================

        await using var outputStream =
            new FileStream(
                filePath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 81920,
                useAsync: true
            );

        await fileStream.CopyToAsync(
            outputStream
        );


        // =================================================
        // RETURN URL
        //
        // Đây là giá trị sẽ lưu vào:
        // Attachment.FileUrl
        // =================================================

        return
            $"/uploads/medical-records/" +
            $"{medicalRecordId}/{storedFileName}";
    }
}