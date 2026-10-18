using ClinicManagementSystem.Application.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ClinicManagementSystem.Infrastructure.Services;

public sealed class LocalFileStorageService : IFileStorageService
{
    private readonly string _root;
    private readonly string _legacyRoot;
    private readonly ILogger<LocalFileStorageService> _logger;

    public LocalFileStorageService(IHostEnvironment environment, IOptions<FileStorageOptions> options,
        ILogger<LocalFileStorageService> logger)
    {
        var configuredRoot = options.Value.LocalRootPath;
        _root = Path.GetFullPath(string.IsNullOrWhiteSpace(configuredRoot)
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "ClinicManagementSystem", "medical-records")
            : configuredRoot);
        if ((!string.IsNullOrWhiteSpace(configuredRoot) && !Path.IsPathFullyQualified(configuredRoot)) ||
            AttachmentStoragePath.IsWithin(_root, environment.ContentRootPath))
            throw new InvalidOperationException("FileStorage:LocalRootPath must be an absolute private directory outside the API source.");

        _legacyRoot = Path.GetFullPath(Path.Combine(environment.ContentRootPath, "wwwroot", "uploads", "medical-records"));
        _logger = logger;
    }

    public async Task<string> UploadAsync(Stream fileStream, string fileName, string contentType, int medicalRecordId)
    {
        var key = AttachmentStoragePath.Create(medicalRecordId, fileName);
        var path = Resolve(_root, key);
        EnsureNoLinks(path, _root);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var created = false;
        try
        {
            await using var output = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None,
                81920, FileOptions.Asynchronous);
            created = true;
            await AttachmentStoragePath.CopyUploadAsync(fileStream, output);
            return key;
        }
        catch
        {
            try { if (created) File.Delete(path); }
            catch (Exception cleanupError) { _logger.LogError(cleanupError, "Could not remove incomplete attachment {StorageKey}", key); }
            throw;
        }
    }

    public async Task<byte[]?> DownloadAsync(string fileReference, int medicalRecordId)
    {
        var key = NormalizeReference(fileReference, medicalRecordId);
        if (key == null) return null;
        // Existing wwwroot files are read-only compatibility data until they are migrated.
        foreach (var root in new[] { _root, _legacyRoot })
        {
            var path = Resolve(root, key);
            EnsureNoLinks(path, root);
            try { return await File.ReadAllBytesAsync(path); }
            catch (FileNotFoundException) { }
            catch (DirectoryNotFoundException) { }
        }
        return null;
    }

    public Task DeleteAsync(string fileReference, int medicalRecordId)
    {
        var key = AttachmentStoragePath.Normalize(fileReference, medicalRecordId)
            ?? throw new ArgumentException("Invalid attachment key.", nameof(fileReference));
        var path = Resolve(_root, key);
        EnsureNoLinks(path, _root);
        // Compensation only deletes new private files, never legacy files in wwwroot.
        try { File.Delete(path); }
        catch (DirectoryNotFoundException) { }
        return Task.CompletedTask;
    }

    private string? NormalizeReference(string reference, int recordId)
    {
        var key = AttachmentStoragePath.Normalize(reference, recordId);
        if (key != null) return key;
        if (!Path.IsPathFullyQualified(reference)) return null;
        foreach (var root in new[] { _root, _legacyRoot })
        {
            if (!AttachmentStoragePath.IsWithin(reference, root)) continue;
            var relative = Path.GetRelativePath(root, Path.GetFullPath(reference)).Replace('\\', '/');
            return AttachmentStoragePath.Normalize("medical-records/" + relative, recordId);
        }
        return null;
    }

    private static string Resolve(string root, string key)
    {
        var path = Path.GetFullPath(Path.Combine(root, key["medical-records/".Length..].Replace('/', Path.DirectorySeparatorChar)));
        if (!AttachmentStoragePath.IsWithin(path, root)) throw new IOException("Invalid attachment path.");
        return path;
    }

    private static void EnsureNoLinks(string path, string root)
    {
        for (var current = path; AttachmentStoragePath.IsWithin(current, root); current = Path.GetDirectoryName(current)!)
        {
            if ((File.Exists(current) || Directory.Exists(current)) &&
                (File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
                throw new IOException("Attachment storage must not contain symbolic links or junctions.");
            if (Path.GetFullPath(current) == root) break;
        }
    }
}
