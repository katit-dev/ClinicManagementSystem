using System.Linq.Expressions;
using ClinicManagementSystem.Application.Enums;
using ClinicManagementSystem.Application.Services;
using ClinicManagementSystem.Infrastructure.Models;
using ClinicManagementSystem.Infrastructure.Repositories;
using ClinicManagementSystem.Infrastructure.UnitOfWork;
using Microsoft.Extensions.Logging.Abstractions;
using MockQueryable;
using Moq;
using Record = ClinicManagementSystem.Infrastructure.Models.MedicalRecord;
using UploadService = ClinicManagementSystem.Application.Services.MedicalRecordService;

namespace ClinicManagementSystem.UnitTests.Application.Attachments;

public class AttachmentUploadTests
{
    private readonly Mock<IUnitOfWork> _unit = new();
    private readonly Mock<IFileStorageService> _storage = new();
    private readonly Mock<IAttachmentRepository> _attachments = new();
    private UploadService Service(int doctorId = 7, byte status = (byte)MedicalRecordStatus.Draft)
    {
        var doctors = new Mock<IDoctorRepository>();
        var records = new Mock<IMedicalRecordRepository>();
        var doctorRows = new[] { new Doctor { Id = 7, UserId = 42, IsActive = true } };
        var recordRows = new[] { new Record { Id = 12, DoctorId = doctorId, Status = status } };
        doctors.Setup(x => x.WhereSql(It.IsAny<Expression<Func<Doctor, bool>>>()))
            .Returns((Expression<Func<Doctor, bool>> p) => doctorRows.Where(p.Compile()).ToList().BuildMock());
        records.Setup(x => x.WhereSql(It.IsAny<Expression<Func<Record, bool>>>()))
            .Returns((Expression<Func<Record, bool>> p) => recordRows.Where(p.Compile()).ToList().BuildMock());
        _unit.SetupGet(x => x.DoctorRepository).Returns(doctors.Object);
        _unit.SetupGet(x => x.MedicalRecordRepository).Returns(records.Object);
        _unit.SetupGet(x => x.AttachmentRepository).Returns(_attachments.Object);
        _attachments.Setup(x => x.WhereSql(It.IsAny<Expression<Func<Attachment, bool>>>()))
            .Returns(new List<Attachment>().BuildMock());
        _unit.Setup(x => x.SaveChangesAsync()).Returns(Task.CompletedTask);
        _storage.Setup(x => x.UploadAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), 12))
            .ReturnsAsync("medical-records/12/new.pdf");
        _storage.Setup(x => x.DeleteAsync(It.IsAny<string>(), 12)).Returns(Task.CompletedTask);
        return new(_unit.Object, Mock.Of<IInvoiceService>(), _storage.Object, NullLogger<UploadService>.Instance);
    }

    [Fact]
    public async Task SuccessfulUpload_StoresKeyAndContentType_WithoutDeletingFile()
    {
        var result = await Service().UploadMedicalRecordAttachmentAsync(12, 42, new MemoryStream([1]), "result.pdf", "text/html");
        Assert.Equal(200, result.StatusCode);
        Assert.Equal("medical-records/12/new.pdf", result.Content!.FileUrl);
        Assert.Equal("application/pdf", result.Content.FileType);
        _storage.Verify(x => x.DeleteAsync(It.IsAny<string>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task DatabaseSaveFailure_DeletesNewFile()
    {
        var service = Service();
        _unit.Setup(x => x.SaveChangesAsync()).ThrowsAsync(new InvalidOperationException("save failed"));
        var result = await service.UploadMedicalRecordAttachmentAsync(12, 42, new MemoryStream([1]), "result.pdf", "application/pdf");
        Assert.Equal(500, result.StatusCode);
        _storage.Verify(x => x.DeleteAsync("medical-records/12/new.pdf", 12), Times.Once);
    }

    [Fact]
    public async Task CleanupFailure_StillReturnsUploadFailure()
    {
        var service = Service();
        _unit.Setup(x => x.SaveChangesAsync()).ThrowsAsync(new InvalidOperationException("save failed"));
        _storage.Setup(x => x.DeleteAsync(It.IsAny<string>(), 12)).ThrowsAsync(new IOException("storage down"));
        Assert.Equal(500, (await service.UploadMedicalRecordAttachmentAsync(12, 42, new MemoryStream([1]), "result.pdf", "application/pdf")).StatusCode);
    }

    [Fact]
    public async Task AmbiguousSaveFailure_WhenMetadataCommitted_DoesNotDeleteFile()
    {
        var service = Service();
        _unit.Setup(x => x.SaveChangesAsync()).ThrowsAsync(new TimeoutException("commit response lost"));
        _attachments.Setup(x => x.WhereSql(It.IsAny<Expression<Func<Attachment, bool>>>()))
            .Returns(new List<Attachment> { new() { MedicalRecordId = 12, FileUrl = "medical-records/12/new.pdf" } }.BuildMock());
        Assert.Equal(500, (await service.UploadMedicalRecordAttachmentAsync(12, 42, new MemoryStream([1]), "result.pdf", "application/pdf")).StatusCode);
        _storage.Verify(x => x.DeleteAsync(It.IsAny<string>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task DatabaseUnavailable_DoesNotDeleteFileWithoutConfirmingMetadataAbsence()
    {
        var service = Service();
        _unit.Setup(x => x.SaveChangesAsync()).ThrowsAsync(new TimeoutException("database down"));
        _attachments.Setup(x => x.WhereSql(It.IsAny<Expression<Func<Attachment, bool>>>()))
            .Throws(new InvalidOperationException("cannot reconcile"));
        Assert.Equal(500, (await service.UploadMedicalRecordAttachmentAsync(12, 42, new MemoryStream([1]), "result.pdf", "application/pdf")).StatusCode);
        _storage.Verify(x => x.DeleteAsync(It.IsAny<string>(), It.IsAny<int>()), Times.Never);
    }

    [Theory]
    [InlineData(8, (byte)MedicalRecordStatus.Draft, 403)]
    [InlineData(7, (byte)MedicalRecordStatus.Finalized, 409)]
    public async Task WrongDoctorOrFinalizedRecord_DoesNotUpload(int doctorId, byte status, int expected)
    {
        var result = await Service(doctorId, status).UploadMedicalRecordAttachmentAsync(12, 42, new MemoryStream([1]), "result.pdf", "application/pdf");
        Assert.Equal(expected, result.StatusCode);
        _storage.Verify(x => x.UploadAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task OversizedUpload_Returns413_BeforeStorageAccess()
    {
        var result = await Service().UploadMedicalRecordAttachmentAsync(12, 42,
            new MemoryStream(new byte[AttachmentStoragePath.MaxFileBytes + 1]), "result.pdf", "application/pdf");
        Assert.Equal(413, result.StatusCode);
        _storage.Verify(x => x.UploadAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()), Times.Never);
    }
}
