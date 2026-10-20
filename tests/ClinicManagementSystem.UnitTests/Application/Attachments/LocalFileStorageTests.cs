using System.Text;
using ClinicManagementSystem.Application.Services;
using ClinicManagementSystem.Infrastructure.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace ClinicManagementSystem.UnitTests.Application.Attachments;

public sealed class LocalFileStorageTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "clinic-storage-" + Guid.NewGuid().ToString("N"));
    private string PrivateRoot => Path.Combine(_root, "private");
    private string AppRoot => Path.Combine(_root, "app");

    private LocalFileStorageService Service(string? root = null)
    {
        var environment = new Mock<IHostEnvironment>();
        environment.SetupGet(x => x.ContentRootPath).Returns(AppRoot);
        return new(environment.Object, Options.Create(new FileStorageOptions { LocalRootPath = root ?? PrivateRoot }),
            NullLogger<LocalFileStorageService>.Instance);
    }

    [Fact]
    public async Task PrivateFile_SurvivesNewServiceInstance_AndCanBeDeleted()
    {
        var key = await Service().UploadAsync(new MemoryStream("clinical file"u8.ToArray()), "result.PDF", "text/html", 12);
        Assert.StartsWith("medical-records/12/", key);
        Assert.EndsWith(".pdf", key);
        Assert.DoesNotContain("uploads", key);
        Assert.False(Directory.Exists(Path.Combine(AppRoot, "wwwroot")));
        var path = Path.Combine(PrivateRoot, key["medical-records/".Length..]);
        Assert.True(File.Exists(path));
        Assert.Equal("clinical file", Encoding.UTF8.GetString((await Service().DownloadAsync(key, 12))!));
        Assert.Null(await Service().DownloadAsync(key, 13));
        await Service().DeleteAsync(key, 12);
        Assert.Null(await Service().DownloadAsync(key, 12));
    }

    [Fact]
    public async Task LegacyUrl_AndAbsoluteLegacyPath_CanStillBeRead()
    {
        var path = Path.Combine(AppRoot, "wwwroot", "uploads", "medical-records", "12", "old.pdf");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await File.WriteAllTextAsync(path, "legacy");
        Assert.NotNull(await Service().DownloadAsync("/uploads/medical-records/12/old.pdf", 12));
        Assert.NotNull(await Service().DownloadAsync(path, 12));
        Assert.True(File.Exists(path));
    }

    [Theory]
    [InlineData("/uploads/medical-records/12/../../outside.txt")]
    [InlineData("medical-records/12/%2e%2e%2fsecret.pdf")]
    [InlineData("medical-records/13/result.pdf")]
    [InlineData("medical-records/12/file.pdf:secret")]
    [InlineData("https://example.com/file.pdf")]
    public async Task UnsafeOrDifferentRecordKey_IsRejected(string reference)
    {
        Assert.Null(await Service().DownloadAsync(reference, 12));
    }

    [Fact]
    public async Task AbsolutePathOutsideStorage_IsRejected()
    {
        Directory.CreateDirectory(_root);
        var path = Path.Combine(_root, "secret.pdf");
        await File.WriteAllTextAsync(path, "secret");
        Assert.Null(await Service().DownloadAsync(path, 12));
    }

    [Fact]
    public async Task OversizedOrEmptyUpload_LeavesNoPartialFile()
    {
        var service = Service();
        await Assert.ThrowsAsync<AttachmentTooLargeException>(() => service.UploadAsync(
            new MemoryStream(new byte[AttachmentStoragePath.MaxFileBytes + 1]), "large.pdf", "application/pdf", 12));
        await Assert.ThrowsAsync<InvalidDataException>(() => service.UploadAsync(new MemoryStream(), "empty.pdf", "application/pdf", 12));
        Assert.Empty(Directory.EnumerateFiles(PrivateRoot, "*", SearchOption.AllDirectories));
    }

    [Fact]
    public async Task ExactlyTenMb_IsAccepted()
    {
        var key = await Service().UploadAsync(new MemoryStream(new byte[AttachmentStoragePath.MaxFileBytes]), "limit.pdf", "application/pdf", 12);
        Assert.Equal(AttachmentStoragePath.MaxFileBytes, (await Service().DownloadAsync(key, 12))!.LongLength);
    }

    [Fact]
    public void StorageInsideApi_IsRejected()
    {
        Assert.Throws<InvalidOperationException>(() => Service(Path.Combine(AppRoot, "wwwroot", "files")));
        Assert.Throws<InvalidOperationException>(() => Service("relative-path"));
    }

    public void Dispose()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, true);
    }
}
