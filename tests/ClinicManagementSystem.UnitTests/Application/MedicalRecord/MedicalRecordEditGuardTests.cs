using System.Linq.Expressions;
using ClinicManagementSystem.Application.DTOs.MedicalRecord;
using ClinicManagementSystem.Application.DTOs.Prescription;
using ClinicManagementSystem.Application.Services;
using ClinicManagementSystem.Infrastructure.Models;
using ClinicManagementSystem.Infrastructure.Repositories;
using ClinicManagementSystem.Infrastructure.UnitOfWork;
using Microsoft.Extensions.Logging.Abstractions;
using MockQueryable;
using Moq;
using AppService = ClinicManagementSystem.Application.Services.MedicalRecordService;
using Record = ClinicManagementSystem.Infrastructure.Models.MedicalRecord;
using OrderedService = ClinicManagementSystem.Infrastructure.Models.MedicalRecordService;

namespace ClinicManagementSystem.UnitTests.Application.MedicalRecord;

public class MedicalRecordEditGuardTests
{
    public static IEnumerable<object[]> Blocked => from operation in new[] { "draft", "prescription", "service", "cancel", "result", "upload", "finalize" }
        from status in new byte[] { 0, 1, 3, 4, 5 }
        select new object[] { operation, status };

    [Theory, MemberData(nameof(Blocked))]
    public async Task InvalidAppointmentState_CannotWriteOrUpload(string operation, byte status)
    {
        var unit = new Mock<IUnitOfWork>();
        var storage = new Mock<IFileStorageService>();
        var doctors = new Mock<IDoctorRepository>();
        var records = new Mock<IMedicalRecordRepository>();
        var appointments = new Mock<IAppointmentRepository>();
        var services = new Mock<IMedicalRecordServiceRepository>();
        var doctor = new Doctor { Id = 7, UserId = 42, IsActive = true };
        var appointment = new Appointment { Id = 3, DoctorId = 7, PatientId = 5, Status = status };
        var record = new Record { Id = 2, DoctorId = 7, PatientId = 5, AppointmentId = 3, Status = 0, Diagnosis = "Test" };
        var ordered = new OrderedService { Id = 9, MedicalRecordId = 2, Status = 0 };
        doctors.Setup(x => x.WhereSql(It.IsAny<Expression<Func<Doctor, bool>>>()))
            .Returns((Expression<Func<Doctor, bool>> p) => new[] { doctor }.Where(p.Compile()).ToList().BuildMock());
        records.Setup(x => x.WhereSql(It.IsAny<Expression<Func<Record, bool>>>()))
            .Returns((Expression<Func<Record, bool>> p) => new[] { record }.Where(p.Compile()).ToList().BuildMock());
        appointments.Setup(x => x.WhereSql(It.IsAny<Expression<Func<Appointment, bool>>>()))
            .Returns((Expression<Func<Appointment, bool>> p) => new[] { appointment }.Where(p.Compile()).ToList().BuildMock());
        services.Setup(x => x.WhereSql(It.IsAny<Expression<Func<OrderedService, bool>>>()))
            .Returns((Expression<Func<OrderedService, bool>> p) => new[] { ordered }.Where(p.Compile()).ToList().BuildMock());
        unit.SetupGet(x => x.DoctorRepository).Returns(doctors.Object);
        unit.SetupGet(x => x.MedicalRecordRepository).Returns(records.Object);
        appointments.Setup(x => x.GetMedicalRecordAppointmentStatusAsync(3)).ReturnsAsync((byte?)status);
        unit.SetupGet(x => x.AppointmentRepository).Returns(appointments.Object);
        unit.SetupGet(x => x.MedicalRecordServiceRepository).Returns(services.Object);
        var service = new AppService(unit.Object, Mock.Of<IInvoiceService>(), storage.Object, NullLogger<AppService>.Instance);
        var code = operation switch
        {
            "draft" => (await service.UpdateMedicalRecordDraftAsync(2, 42, new MedicalRecordDraftRequestDTO { Diagnosis = "Test" })).StatusCode,
            "prescription" => (await service.CreatePrescriptionAsync(2, 42, new PrescriptionRequestDTO
                { Items = [new() { MedicineId = 1, Quantity = 1, Dosage = "Test" }] })).StatusCode,
            "service" => (await service.AddMedicalRecordServiceAsync(2, 42, new() { ServiceId = 1, Quantity = 1 })).StatusCode,
            "cancel" => (await service.CancelMedicalRecordServiceAsync(9, 42)).StatusCode,
            "result" => (await service.AddMedicalRecordServiceResultAsync(9, 42, new LabResultRequestDTO { ResultValue = "Test", ResultedAt = DateTime.Now })).StatusCode,
            "upload" => (await service.UploadMedicalRecordAttachmentAsync(2, 42, new MemoryStream([1]), "test.png", "image/png")).StatusCode,
            _ => (await service.FinalizeMedicalRecordAsync(2, 42)).StatusCode
        };
        Assert.Equal(409, code);
        Assert.Equal((byte)0, record.Status);
        unit.Verify(x => x.SaveChangesAsync(), Times.Never);
        unit.Verify(x => x.BeginTransactionAsync(), Times.Never);
        storage.Verify(x => x.UploadAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()), Times.Never);
    }
}
