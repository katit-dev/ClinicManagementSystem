using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using ClinicManagementSystem.Application.Services;
using Microsoft.Extensions.Logging;

namespace ClinicManagementSystem.Infrastructure.Services;

public sealed class AzureBlobFileStorageService : IFileStorageService
{
    private readonly BlobContainerClient _container;
    private readonly ILogger<AzureBlobFileStorageService> _logger;

    public AzureBlobFileStorageService(BlobContainerClient container, ILogger<AzureBlobFileStorageService> logger)
    {
        _container = container;
        _logger = logger;
    }

    public async Task<string> UploadAsync(Stream fileStream, string fileName, string contentType, int medicalRecordId)
    {
        var key = AttachmentStoragePath.Create(medicalRecordId, fileName);
        await using var boundedContent = new MemoryStream();
        await AttachmentStoragePath.CopyUploadAsync(fileStream, boundedContent);
        boundedContent.Position = 0;
        var blob = _container.GetBlobClient(key);
        try
        {
            await blob.UploadAsync(boundedContent, new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders { ContentType = AttachmentStoragePath.ContentTypeFor(fileName) },
                Conditions = new BlobRequestConditions { IfNoneMatch = ETag.All }
            });
            return key;
        }
        catch (RequestFailedException ex) when (ex.Status == 409 || ex.Status == 412)
        {
            // Do not delete an existing blob if the create-only precondition failed.
            throw;
        }
        catch
        {
            try { await blob.DeleteIfExistsAsync(); }
            catch (Exception cleanupError) { _logger.LogError(cleanupError, "Could not reconcile incomplete blob {StorageKey}", key); }
            throw;
        }
    }

    public async Task<byte[]?> DownloadAsync(string fileReference, int medicalRecordId)
    {
        var key = AttachmentStoragePath.Normalize(fileReference, medicalRecordId);
        if (key == null) return null;
        try
        {
            var result = await _container.GetBlobClient(key).DownloadContentAsync(CancellationToken.None);
            return result.Value.Content.ToArray();
        }
        catch (RequestFailedException ex) when (ex.Status == 404 && ex.ErrorCode == BlobErrorCode.BlobNotFound.ToString())
        {
            return null;
        }
    }

    public async Task DeleteAsync(string fileReference, int medicalRecordId)
    {
        var key = AttachmentStoragePath.Normalize(fileReference, medicalRecordId)
            ?? throw new ArgumentException("Invalid attachment key.", nameof(fileReference));
        await _container.GetBlobClient(key).DeleteIfExistsAsync();
    }
}
