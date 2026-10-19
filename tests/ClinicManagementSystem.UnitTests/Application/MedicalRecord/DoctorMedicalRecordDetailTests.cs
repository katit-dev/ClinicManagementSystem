using System.Linq.Expressions;
using ClinicManagementSystem.Application.Services;
using ClinicManagementSystem.Infrastructure.Models;
using ClinicManagementSystem.Infrastructure.Repositories;
using ClinicManagementSystem.Infrastructure.UnitOfWork;
using Microsoft.Extensions.Logging.Abstractions;
using MockQueryable;
using Moq;
using Record = ClinicManagementSystem.Infrastructure.Models.MedicalRecord;
using AppService = ClinicManagementSystem.Application.Services.MedicalRecordService;

namespace ClinicManagementSystem.UnitTests.Application.MedicalRecord;

public class DoctorMedicalRecordDetailTests
{
    [Theory]
    [InlineData(0, 2, true)]
    [InlineData(0, 4, false)]
    [InlineData(0, 5, false)]
    [InlineData(1, 3, false)]
    public async Task Detail_ExposesEditPermissionAndExistingPrescription(byte recordStatus, byte appointmentStatus, bool canEdit)
    {
        var record = new Record
        {
            Id = 2, DoctorId = 7, PatientId = 5, Status = recordStatus,
            Doctor = new() { FullName = "Doctor A" }, Patient = new() { FullName = "Patient A", PatientCode = "BN005" },
            Appointment = new() { Status = appointmentStatus, StartTime = new(2026, 10, 19, 9, 0, 0) },
            Prescription = new() { Id = 10, PrescriptionItems = [new() { Id = 1, MedicineId = 9, MedicineNameSnapshot = "Tên thuốc tại lúc kê", Quantity = 2 }] }
        };
        var result = await Service(record).GetMedicalRecordByIdForDoctorAsync(2, 42);
        Assert.Equal(200, result.StatusCode);
        Assert.Equal(canEdit, result.Content!.CanEdit);
        Assert.Equal(!canEdit, !string.IsNullOrEmpty(result.Content.ReadOnlyReason));
        Assert.Equal("Patient A", result.Content.PatientName);
        Assert.Equal("Tên thuốc tại lúc kê", Assert.Single(result.Content.Prescription!.Items).MedicineName);
    }

    [Fact]
    public async Task AnotherDoctorsRecord_IsNotReturnedEvenWithKnownId()
    {
        var result = await Service(new Record { Id = 2, DoctorId = 99 }).GetMedicalRecordByIdForDoctorAsync(2, 42);
        Assert.Equal(404, result.StatusCode); Assert.Null(result.Content);
    }

    [Theory]
    [InlineData(99, true)]
    [InlineData(42, false)]
    public async Task PrescriptionPdf_IsDeniedForOtherOrInactiveDoctor(int linkedUser, bool active)
    {
        var unit = new Mock<IUnitOfWork>(); var doctors = new Mock<IDoctorRepository>(); var prescriptions = new Mock<IPrescriptionRepository>();
        doctors.Setup(x => x.WhereSql(It.IsAny<Expression<Func<Doctor, bool>>>()))
            .Returns((Expression<Func<Doctor, bool>> p) => new[] { new Doctor { Id = 7, UserId = linkedUser, IsActive = active } }.Where(p.Compile()).ToList().BuildMock());
        prescriptions.Setup(x => x.WhereSql(It.IsAny<Expression<Func<Prescription, bool>>>()))
            .Returns((Expression<Func<Prescription, bool>> p) => new[] { new Prescription { Id = 3, DoctorId = 7, MedicalRecordId = 2 } }.Where(p.Compile()).ToList().BuildMock());
        unit.SetupGet(x => x.DoctorRepository).Returns(doctors.Object);
        unit.SetupGet(x => x.PrescriptionRepository).Returns(prescriptions.Object);
        var service = new AppService(unit.Object, Mock.Of<IInvoiceService>(), Mock.Of<IFileStorageService>(), NullLogger<AppService>.Instance);
        Assert.Null(await service.GetPrescriptionPdfAsync(3, 42));
        unit.VerifyGet(x => x.PrescriptionItemRepository, Times.Never);
    }

    private static AppService Service(Record record)
    {
        var unit = new Mock<IUnitOfWork>(); var doctors = new Mock<IDoctorRepository>(); var records = new Mock<IMedicalRecordRepository>();
        doctors.Setup(x => x.WhereSql(It.IsAny<Expression<Func<Doctor, bool>>>()))
            .Returns((Expression<Func<Doctor, bool>> p) => new[] { new Doctor { Id = 7, UserId = 42, IsActive = true } }.Where(p.Compile()).ToList().BuildMock());
        records.Setup(x => x.WhereSql(It.IsAny<Expression<Func<Record, bool>>>()))
            .Returns((Expression<Func<Record, bool>> p) => new[] { record }.Where(p.Compile()).ToList().BuildMock());
        unit.SetupGet(x => x.DoctorRepository).Returns(doctors.Object);
        unit.SetupGet(x => x.MedicalRecordRepository).Returns(records.Object);
        return new(unit.Object, Mock.Of<IInvoiceService>(), Mock.Of<IFileStorageService>(), NullLogger<AppService>.Instance);
    }
}
