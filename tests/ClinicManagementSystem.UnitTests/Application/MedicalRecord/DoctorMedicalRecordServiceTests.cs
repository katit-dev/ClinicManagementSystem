using ClinicManagementSystem.Application.DTOs.Doctor;
using ClinicManagementSystem.Application.Helpers;
using ClinicManagementSystem.Application.Services;
using ClinicManagementSystem.Infrastructure.Models;
using ClinicManagementSystem.Infrastructure.Repositories;
using ClinicManagementSystem.Infrastructure.UnitOfWork;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace ClinicManagementSystem.UnitTests.Application.MedicalRecord;

public class DoctorMedicalRecordServiceTests
{
    [Fact]
    public async Task UsesLinkedDoctorId_AndReturnsPagedReadOnlyMetadata()
    {
        var unit = new Mock<IUnitOfWork>();
        var doctors = new Mock<IDoctorRepository>();
        var records = new Mock<IMedicalRecordRepository>();
        unit.SetupGet(x => x.DoctorRepository).Returns(doctors.Object);
        unit.SetupGet(x => x.MedicalRecordRepository).Returns(records.Object);
        doctors.Setup(x => x.GetActiveByUserIdAsync(42)).ReturnsAsync(new Doctor { Id = 7 });
        records.Setup(x => x.GetDoctorPageAsync(7, "Lieu", null, null, null, 2, 20))
            .ReturnsAsync((new List<ClinicManagementSystem.Infrastructure.Models.MedicalRecord>
            {
                Record(1, 0, 2), Record(2, 0, 4), Record(3, 1, 3)
            }, 41));
        var service = new DoctorMedicalRecordService(unit.Object, NullLogger<DoctorMedicalRecordService>.Instance);
        var result = await service.GetMyRecordsAsync(new() { Keyword = " Lieu ", Page = 2 }, 42);
        Assert.Equal(200, result.StatusCode);
        Assert.Equal(41, result.Content!.TotalItems);
        Assert.Equal(3, result.Content.TotalPages);
        Assert.True(result.Content.Items[0].CanContinue);
        Assert.False(result.Content.Items[1].CanContinue);
        Assert.Contains("đã hủy", result.Content.Items[1].ReadOnlyReason);
        Assert.False(result.Content.Items[2].CanContinue);
        records.VerifyAll();
    }

    [Theory]
    [InlineData(0, 401)]
    [InlineData(42, 403)]
    public async Task MissingActorOrDoctor_DoesNotQueryRecords(int user, int status)
    {
        var unit = new Mock<IUnitOfWork>();
        var doctors = new Mock<IDoctorRepository>();
        var records = new Mock<IMedicalRecordRepository>();
        unit.SetupGet(x => x.DoctorRepository).Returns(doctors.Object);
        unit.SetupGet(x => x.MedicalRecordRepository).Returns(records.Object);
        var service = new DoctorMedicalRecordService(unit.Object, NullLogger<DoctorMedicalRecordService>.Instance);
        Assert.Equal(status, (await service.GetMyRecordsAsync(new(), user)).StatusCode);
        records.Verify(x => x.GetDoctorPageAsync(It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<byte?>(),
            It.IsAny<DateOnly?>(), It.IsAny<DateOnly?>(), It.IsAny<int>(), It.IsAny<int>()), Times.Never);
    }

    [Theory]
    [InlineData(0, 2, true)]
    [InlineData(0, 4, false)]
    [InlineData(0, 5, false)]
    [InlineData(0, 3, false)]
    [InlineData(0, 0, false)]
    [InlineData(0, 1, false)]
    [InlineData(1, 2, false)]
    [InlineData(1, 3, false)]
    public void EditPolicy_OnlyAllowsDraftAndCheckedIn(byte record, byte appointment, bool editable)
        => Assert.Equal(editable, MedicalRecordEditPolicy.ReadOnlyReason(record, appointment) == null);

    private static ClinicManagementSystem.Infrastructure.Models.MedicalRecord Record(int id, byte status, byte appointmentStatus) => new()
    {
        Id = id, DoctorId = 7, Status = status, PatientId = 5,
        Patient = new Patient { Id = 5, FullName = "Lieu", PatientCode = "BN005" },
        Appointment = new Appointment { Status = appointmentStatus, StartTime = new(2026, 10, 19, 9, 0, 0) }
    };
}
