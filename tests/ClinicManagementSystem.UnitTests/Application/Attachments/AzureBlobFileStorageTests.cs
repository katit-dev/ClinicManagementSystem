using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using ClinicManagementSystem.Application.Services;
using ClinicManagementSystem.Infrastructure.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace ClinicManagementSystem.UnitTests.Application.Attachments;

public class AzureBlobFileStorageTests
{
    private readonly Mock<BlobContainerClient> _container = new();
    private readonly Mock<BlobClient> _blob = new();
    private AzureBlobFileStorageService Service()
    {
        _container.Setup(x => x.GetBlobClient(It.IsAny<string>())).Returns(_blob.Object);
        return new(_container.Object, NullLogger<AzureBlobFileStorageService>.Instance);
    }

    [Fact]
    public async Task Upload_UsesPrivateKey_CreateOnly_AndCanonicalContentType()
    {
        BlobUploadOptions? captured = null;
        byte[]? uploaded = null;
        _blob.Setup(x => x.UploadAsync(It.IsAny<Stream>(), It.IsAny<BlobUploadOptions>(), It.IsAny<CancellationToken>()))
            .Callback<Stream, BlobUploadOptions, CancellationToken>((s, o, _) =>
            {
                captured = o;
                using var copy = new MemoryStream(); s.CopyTo(copy); uploaded = copy.ToArray();
            })
            .ReturnsAsync((Response<BlobContentInfo>)null!);
        var key = await Service().UploadAsync(new MemoryStream("test"u8.ToArray()), "result.pdf", "text/html", 12);
        Assert.StartsWith("medical-records/12/", key);
        Assert.DoesNotContain("http", key);
        Assert.Equal("application/pdf", captured!.HttpHeaders.ContentType);
        Assert.Equal(ETag.All, captured.Conditions.IfNoneMatch);
        Assert.Equal("test"u8.ToArray(), uploaded);
    }

    [Fact]
    public async Task LegacyUrl_MapsToMigratedBlobKey()
    {
        _blob.Setup(x => x.DownloadContentAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(BlobsModelFactory.BlobDownloadResult(BinaryData.FromString("legacy")), Mock.Of<Response>()));
        Assert.Equal("legacy"u8.ToArray(), await Service().DownloadAsync("/uploads/medical-records/12/old.pdf", 12));
        _container.Verify(x => x.GetBlobClient("medical-records/12/old.pdf"), Times.Once);
    }

    [Fact]
    public async Task NotFound_ReturnsNull_ButStorageOutageIsNotTreatedAsMissingFile()
    {
        _blob.Setup(x => x.DownloadContentAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new RequestFailedException(404, "missing", "BlobNotFound", null));
        Assert.Null(await Service().DownloadAsync("medical-records/12/file.pdf", 12));
        _blob.Setup(x => x.DownloadContentAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new RequestFailedException(503, "unavailable"));
        await Assert.ThrowsAsync<RequestFailedException>(() => Service().DownloadAsync("medical-records/12/file.pdf", 12));
    }

    [Fact]
    public async Task InvalidKey_DoesNotCallStorage()
    {
        Assert.Null(await Service().DownloadAsync("medical-records/13/file.pdf", 12));
        _container.Verify(x => x.GetBlobClient(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task FailedUpload_AttemptsCleanup_WithoutMaskingOriginalFailure()
    {
        _blob.Setup(x => x.UploadAsync(It.IsAny<Stream>(), It.IsAny<BlobUploadOptions>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new RequestFailedException(503, "unavailable"));
        _blob.Setup(x => x.DeleteIfExistsAsync(It.IsAny<DeleteSnapshotsOption>(), It.IsAny<BlobRequestConditions>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new RequestFailedException(503, "cleanup unavailable"));
        var error = await Assert.ThrowsAsync<RequestFailedException>(() => Service().UploadAsync(new MemoryStream([1]), "file.pdf", "application/pdf", 12));
        Assert.Equal("unavailable", error.Message);
        _blob.Verify(x => x.DeleteIfExistsAsync(It.IsAny<DeleteSnapshotsOption>(), It.IsAny<BlobRequestConditions>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
