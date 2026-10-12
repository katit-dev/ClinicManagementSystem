using System.Linq.Expressions;
using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.DTOs.Appointment;
using ClinicManagementSystem.Application.Services;
using ClinicManagementSystem.Infrastructure.Models;
using ClinicManagementSystem.Infrastructure.Repositories;
using ClinicManagementSystem.Infrastructure.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using MockQueryable;
using Moq;

namespace ClinicManagementSystem.UnitTests.Application.Reception;

public class AppointmentMutationGuardTests
{
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IDoctorService> _doctorService = new();
    private readonly Mock<IDoctorScheduleRepository> _schedules = new();
    private readonly AppointmentService _service;
    private readonly DateTime _start = DateTime.Now.Date.AddDays(10).AddHours(9);
    private readonly List<Appointment> _appointments = new();
    private readonly List<Patient> _patients = new() { new() { Id = 2, UserId = 20, IsActive = true } };

    public AppointmentMutationGuardTests()
    {
        var appointments = new Mock<IAppointmentRepository>();
        var patients = new Mock<IPatientRepository>();
        var doctors = new Mock<IDoctorRepository>();
        var specialties = new Mock<ISpecialtyRepository>();
        _appointments.Add(new() { Id = 1, PatientId = 2, DoctorId = 3, StartTime = _start.AddDays(-1), EndTime = _start.AddDays(-1).AddMinutes(30), Status = 0 });
        appointments.Setup(x => x.WhereSql(It.IsAny<Expression<Func<Appointment, bool>>>()))
            .Returns((Expression<Func<Appointment, bool>> p) => _appointments.Where(p.Compile()).ToList().BuildMock());
        patients.Setup(x => x.WhereSql(It.IsAny<Expression<Func<Patient, bool>>>()))
            .Returns((Expression<Func<Patient, bool>> p) => _patients.Where(p.Compile()).ToList().BuildMock());
        doctors.Setup(x => x.WhereSql(It.IsAny<Expression<Func<Doctor, bool>>>()))
            .Returns(new List<Doctor> { new() { Id = 3, IsActive = true, SpecialtyId = 4, FullName = "Test", ConsultationFee = 100000 } }.BuildMock());
        specialties.Setup(x => x.WhereSql(It.IsAny<Expression<Func<Specialty, bool>>>()))
            .Returns(new List<Specialty> { new() { Id = 4, Name = "Test" } }.BuildMock());
        _uow.Setup(x => x.AppointmentRepository).Returns(appointments.Object);
        _uow.Setup(x => x.PatientRepository).Returns(patients.Object);
        _uow.Setup(x => x.DoctorRepository).Returns(doctors.Object);
        _uow.Setup(x => x.SpecialtyRepository).Returns(specialties.Object);
        _uow.Setup(x => x.DoctorScheduleRepository).Returns(_schedules.Object);
        _uow.Setup(x => x.AppointmentStatusHistoryRepository).Returns(Mock.Of<IAppointmentStatusHistoryRepository>());
        _uow.Setup(x => x.NotificationRepository).Returns(Mock.Of<INotificationRepository>());
        _service = new(_uow.Object, _doctorService.Object, NullLogger<AppointmentService>.Instance);
    }

    private HttpResponseData<List<SlotDTO>> Available() => new()
    { StatusCode = 200, Content = new() { new() { StartTime = _start, EndTime = _start.AddMinutes(30), IsAvailable = true } } };

    [Theory]
    [InlineData("patient-create")]
    [InlineData("reception-create")]
    [InlineData("patient-reschedule")]
    public async Task BookingWriters_RecheckSlotAfterSchedulingLock(string operation)
    {
        _doctorService.SetupSequence(x => x.GetAvailableSlotsAsync(3, DateOnly.FromDateTime(_start)))
            .ReturnsAsync(Available())
            .ReturnsAsync(new HttpResponseData<List<SlotDTO>> { StatusCode = 200, Content = new() });
        var result = operation switch
        {
            "patient-create" => await _service.CreateAppointmentAsync(new() { DoctorId = 3, StartTime = _start }, 20),
            "reception-create" => await _service.CreateReceptionAppointmentAsync(new() { PatientId = 2, DoctorId = 3, StartTime = _start }, 7),
            _ => await _service.RescheduleAppointmentAsync(1, new() { NewStartTime = _start }, 20)
        };
        Assert.Equal(409, result.StatusCode);
        _schedules.Verify(x => x.AcquireAdminLockAsync(), Times.Once);
        _doctorService.Verify(x => x.GetAvailableSlotsAsync(3, DateOnly.FromDateTime(_start)), Times.Exactly(2));
        _uow.Verify(x => x.RollbackTransactionAsync(), Times.Once);
        _uow.Verify(x => x.SaveChangesAsync(), Times.Never);
    }

    [Theory]
    [InlineData("reschedule")]
    [InlineData("cancel")]
    public async Task PatientMutation_ReturnsConflictAndRollsBackWhenReceptionChangedAppointment(string operation)
    {
        _doctorService.Setup(x => x.GetAvailableSlotsAsync(3, DateOnly.FromDateTime(_start))).ReturnsAsync(Available());
        _uow.Setup(x => x.SaveChangesAsync()).ThrowsAsync(new DbUpdateConcurrencyException());
        var result = operation == "reschedule"
            ? await _service.RescheduleAppointmentAsync(1, new() { NewStartTime = _start }, 20)
            : await _service.CancelAppointmentAsync(1, new() { CancelReason = "Test" }, 20);
        Assert.Equal(409, result.StatusCode);
        _uow.Verify(x => x.RollbackTransactionAsync(), Times.Once);
        _uow.Verify(x => x.CommitTransactionAsync(), Times.Never);
    }

    [Fact]
    public async Task PatientOwnership_IsStillRequired()
    {
        _patients.Add(new() { Id = 5, UserId = 99, IsActive = true });
        Assert.Equal(404, (await _service.RescheduleAppointmentAsync(1, new() { NewStartTime = _start }, 99)).StatusCode);
        Assert.Equal(404, (await _service.CancelAppointmentAsync(1, new() { CancelReason = "Test" }, 99)).StatusCode);
        _uow.Verify(x => x.BeginTransactionAsync(), Times.Never);
    }
}
