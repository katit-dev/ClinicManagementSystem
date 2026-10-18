using System.Linq.Expressions;
using ClinicManagementSystem.Application.Services;
using ClinicManagementSystem.Infrastructure.Models;
using ClinicManagementSystem.Infrastructure.Repositories;
using ClinicManagementSystem.Infrastructure.UnitOfWork;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using ClinicManagementSystem.Infrastructure.Services;
using Microsoft.Extensions.Logging.Abstractions;
using MockQueryable;
using Moq;
using MedicalRecordEntity = ClinicManagementSystem.Infrastructure.Models.MedicalRecord;

namespace ClinicManagementSystem.UnitTests.Application.Audit;

public class AuditedAttachmentDownloadTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "vc24-attachment-" + Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task UploadedUrl_CanBeDownloadedByOwnerThroughAuthorizedService()
    {
        var path = Path.Combine(_root, "app", "wwwroot", "uploads", "medical-records", "1204", "file.txt");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!); await File.WriteAllTextAsync(path, "test medical file");
        var result = await Service("/uploads/medical-records/1204/file.txt").DownloadAttachmentAsync(65, 42);
        Assert.NotNull(result); Assert.Equal("test medical file", System.Text.Encoding.UTF8.GetString(result.Value.FileBytes));
        Assert.Equal("text/plain", result.Value.ContentType); Assert.Equal("file.txt", result.Value.FileName);
    }

    [Theory]
    [InlineData(false, 42, 125)]
    [InlineData(true, 43, 125)]
    [InlineData(true, 42, 126)]
    public async Task NonOwnerOrInactivePatient_CannotDownload(bool active, int user, int recordPatientId)
    {
        var storage = new Mock<IFileStorageService>();
        storage.Setup(x => x.DownloadAsync(It.IsAny<string>(), It.IsAny<int>())).ReturnsAsync(new byte[] { 1 });
        Assert.Null(await Service("/uploads/medical-records/1204/file.txt", active, recordPatientId, storage.Object).DownloadAttachmentAsync(65, user));
        storage.Verify(x => x.DownloadAsync(It.IsAny<string>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task StoredUploadUrl_CannotEscapePrivateDirectory()
    {
        var outside = Path.Combine(_root, "app", "wwwroot", "outside.txt");
        Directory.CreateDirectory(Path.GetDirectoryName(outside)!); await File.WriteAllTextAsync(outside, "private outside file");
        Assert.Null(await Service("/uploads/medical-records/../../outside.txt").DownloadAttachmentAsync(65, 42));
    }

    [Fact]
    public async Task AbsolutePathsOutsideAttachmentStorage_AreRejected()
    {
        var legacy = Path.Combine(_root, "legacy.txt"); Directory.CreateDirectory(_root); await File.WriteAllTextAsync(legacy, "legacy");
        Assert.Null(await Service(legacy).DownloadAttachmentAsync(65, 42));
    }

    private AttachmentService Service(string fileUrl, bool active = true, int recordPatientId = 125, IFileStorageService? overrideStorage = null)
    {
        var unit = new Mock<IUnitOfWork>(); var patients = new Mock<IPatientRepository>();
        var attachments = new Mock<IAttachmentRepository>(); var records = new Mock<IMedicalRecordRepository>();
        var patientRows = new[] { new Patient { Id = 125, UserId = 42, IsActive = active } };
        var fileRows = new[] { new Attachment { Id = 65, MedicalRecordId = 1204, FileUrl = fileUrl, FileType = "text/plain" } };
        var recordRows = new[] { new MedicalRecordEntity { Id = 1204, PatientId = recordPatientId } };
        patients.Setup(x => x.WhereSql(It.IsAny<Expression<Func<Patient, bool>>>())).Returns((Expression<Func<Patient, bool>> predicate) => patientRows.Where(predicate.Compile()).ToList().BuildMock());
        attachments.Setup(x => x.WhereSql(It.IsAny<Expression<Func<Attachment, bool>>>())).Returns((Expression<Func<Attachment, bool>> predicate) => fileRows.Where(predicate.Compile()).ToList().BuildMock());
        records.Setup(x => x.WhereSql(It.IsAny<Expression<Func<MedicalRecordEntity, bool>>>())).Returns((Expression<Func<MedicalRecordEntity, bool>> predicate) => recordRows.Where(predicate.Compile()).ToList().BuildMock());
        unit.SetupGet(x => x.PatientRepository).Returns(patients.Object); unit.SetupGet(x => x.AttachmentRepository).Returns(attachments.Object);
        unit.SetupGet(x => x.MedicalRecordRepository).Returns(records.Object);
        var environment = new Mock<IHostEnvironment>(); environment.SetupGet(x => x.ContentRootPath).Returns(Path.Combine(_root, "app"));
        var storage = new LocalFileStorageService(environment.Object, Options.Create(new FileStorageOptions { LocalRootPath = Path.Combine(_root, "private") }), NullLogger<LocalFileStorageService>.Instance);
        return new(unit.Object, NullLogger<AttachmentService>.Instance, overrideStorage ?? storage);
    }
    public void Dispose() { if (Directory.Exists(_root)) Directory.Delete(_root, true); }
}
