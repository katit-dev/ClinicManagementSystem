using System.Globalization;

namespace ClinicManagementSystem.Application.Services;

public static class AttachmentStoragePath
{
    public const long MaxFileBytes = 10 * 1024 * 1024;
    // Multipart boundaries and headers are part of the HTTP request, not the file.
    public const long MaxRequestBytes = MaxFileBytes + 1024 * 1024;

    public static string Create(int medicalRecordId, string fileName)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(medicalRecordId);
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        _ = ContentTypeFor(fileName);
        return $"medical-records/{medicalRecordId.ToString(CultureInfo.InvariantCulture)}/{Guid.NewGuid():N}{extension}";
    }

    public static string ContentTypeFor(string fileName) => Path.GetExtension(fileName).ToLowerInvariant() switch
    {
        ".jpg" or ".jpeg" => "image/jpeg",
        ".png" => "image/png",
        ".pdf" => "application/pdf",
        ".dcm" or ".dicom" => "application/dicom",
        _ => throw new ArgumentException("Unsupported medical attachment extension.", nameof(fileName))
    };

    public static string? Normalize(string? reference, int medicalRecordId)
    {
        if (string.IsNullOrWhiteSpace(reference) || medicalRecordId <= 0) return null;
        var key = reference.Replace('\\', '/');
        const string legacyPrefix = "/uploads/medical-records/";
        if (key.StartsWith(legacyPrefix, StringComparison.OrdinalIgnoreCase))
            key = "medical-records/" + key[legacyPrefix.Length..];
        else if (key.StartsWith("uploads/medical-records/", StringComparison.OrdinalIgnoreCase))
            key = "medical-records/" + key["uploads/medical-records/".Length..];

        var parts = key.Split('/');
        if (parts.Length != 3 || parts[0] != "medical-records" ||
            parts[1] != medicalRecordId.ToString(CultureInfo.InvariantCulture) ||
            string.IsNullOrWhiteSpace(parts[2]) || parts[2] is "." or ".." ||
            parts[2].Length > 200 || parts[2].Any(c => !char.IsAsciiLetterOrDigit(c) && c is not '.' and not '-' and not '_'))
            return null;
        return key;
    }

    public static bool IsWithin(string path, string root)
    {
        var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        var normalizedRoot = Path.TrimEndingDirectorySeparator(Path.GetFullPath(root));
        var normalizedPath = Path.GetFullPath(path);
        return normalizedPath.Equals(normalizedRoot, comparison) ||
            normalizedPath.StartsWith(normalizedRoot + Path.DirectorySeparatorChar, comparison);
    }

    public static async Task CopyUploadAsync(Stream source, Stream target)
    {
        ArgumentNullException.ThrowIfNull(source);
        var buffer = new byte[81920];
        long total = 0;
        int read;
        while ((read = await source.ReadAsync(buffer)) != 0)
        {
            total += read;
            if (total > MaxFileBytes) throw new AttachmentTooLargeException();
            await target.WriteAsync(buffer.AsMemory(0, read));
        }
        if (total == 0) throw new InvalidDataException("The attachment is empty.");
    }
}

public sealed class AttachmentTooLargeException : IOException
{
    public AttachmentTooLargeException() : base("Medical attachments must not exceed 10 MB.") { }
}
