using ClinicManagementSystem.Infrastructure.Data;
using ClinicManagementSystem.Infrastructure.Models;
using ClinicManagementSystem.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagementSystem.UnitTests.Infrastructure;

public class DoctorMedicalRecordQueryTests
{
    [Fact]
    public void ScopeAndDates_UseAppointmentDay_NotRecordCreationDay()
    {
        var rows = Rows();
        var result = MedicalRecordRepository.FilterDoctorRecords(rows.AsQueryable(), 7, null, null,
            new(2026, 10, 19), new(2026, 10, 19)).ToList();
        Assert.Equal(new[] { 3, 2, 1 }, result.Select(r => r.Id));
        Assert.All(result, r => Assert.Equal(7, r.DoctorId));
        Assert.All(result, r => Assert.Equal(new DateTime(2026, 9, 20), r.CreatedAt));
    }

    [Theory]
    [InlineData("#2", null, 2)]
    [InlineData("BN01", 0, 1)]
    [InlineData("Nguyễn An", 1, 2)]
    public void KeywordAndStatus_AreCombinedWithinDoctorScope(string keyword, int? status, int expected)
    {
        var result = MedicalRecordRepository.FilterDoctorRecords(Rows().AsQueryable(), 7, keyword, status.HasValue ? (byte)status.Value : null, null, null).ToList();
        Assert.Equal(expected, Assert.Single(result).Id);
    }

    [Fact]
    public void Paging_IsStable_WhenAppointmentTimesMatch()
    {
        var query = MedicalRecordRepository.FilterDoctorRecords(Rows().AsQueryable(), 7, null, null,
            new(2026, 10, 19), new(2026, 10, 19));
        Assert.Equal(new[] { 3, 2 }, query.Take(2).Select(r => r.Id));
        Assert.Equal(new[] { 1 }, query.Skip(2).Take(2).Select(r => r.Id));
    }

    [Fact]
    public void AllFilters_TranslateToSqlServerWithoutOpeningAConnection()
    {
        using var db = new ClinicManagementDbContext(new DbContextOptionsBuilder<ClinicManagementDbContext>()
            .UseSqlServer("Server=unused.invalid;Database=SqlTranslationOnly;Integrated Security=true;TrustServerCertificate=true").Options);
        var query = MedicalRecordRepository.FilterDoctorRecords(db.MedicalRecords, 7, "Nguyễn", 0, new(2026, 10, 1), new(2026, 10, 19));
        var sql = query.Skip(20).Take(20).ToQueryString();
        Assert.Contains("[doctor_id]", sql);
        Assert.Contains("[start_time]", sql);
        Assert.Contains("[full_name]", sql);
        Assert.Contains("OFFSET", sql);
    }

    private static List<MedicalRecord> Rows() =>
    [
        Row(1, 7, "BN01", "Lê Bình", 0, new(2026, 10, 19, 8, 0, 0)),
        Row(2, 7, "BN02", "Nguyễn An", 1, new(2026, 10, 19, 9, 0, 0)),
        Row(3, 7, "BN03", "Trần C", 0, new(2026, 10, 19, 9, 0, 0)),
        Row(4, 99, "BN01", "Nguyễn An", 0, new(2026, 10, 19, 10, 0, 0)),
        Row(5, 7, "BN05", "Võ D", 0, new(2026, 10, 20, 0, 0, 0))
    ];
    private static MedicalRecord Row(int id, int doctor, string code, string name, byte status, DateTime start) => new()
    {
        Id = id, DoctorId = doctor, Status = status, CreatedAt = new(2026, 9, 20),
        Appointment = new() { StartTime = start }, Patient = new() { PatientCode = code, FullName = name }
    };
}
