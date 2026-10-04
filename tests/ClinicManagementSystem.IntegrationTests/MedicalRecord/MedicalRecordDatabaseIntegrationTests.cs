using ClinicManagementSystem.Application.DTOs.MedicalRecord;
using ClinicManagementSystem.Application.Enums;
using ClinicManagementSystem.Application.Services;
using ClinicManagementSystem.Infrastructure.Data;
using ClinicManagementSystem.Infrastructure.Models;
using MedicalRecordEntity =
    ClinicManagementSystem.Infrastructure.Models.MedicalRecord;

using MedicalRecordServiceEntity =
    ClinicManagementSystem.Infrastructure.Models.MedicalRecordService;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Xunit;


namespace ClinicManagementSystem.IntegrationTests.MedicalRecord;


// =====================================================
// MEDICAL RECORD DATABASE INTEGRATION TESTS
// =====================================================
//
// xUnit
//    ↓
// CustomWebApplicationFactory
//    ↓
// ASP.NET Core DI
//    ↓
// MedicalRecordService
//    ↓
// UnitOfWork / Repository
//    ↓
// EF Core
//    ↓
// SQL Server Testcontainer
//
// =====================================================

public class MedicalRecordDatabaseIntegrationTests
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;


    // =====================================================
    // CONSTRUCTOR
    // =====================================================

    public MedicalRecordDatabaseIntegrationTests(
        CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }


    // =====================================================
    // TEST 1
    //
    // Update Medical Record Draft
    //
    // Kiểm tra:
    // - Doctor hợp lệ
    // - MedicalRecord thuộc Doctor
    // - MedicalRecord đang Draft
    // - Clinical information được lưu xuống database
    //
    // Expected:
    // 200
    // =====================================================

    [Fact]
    public async Task UpdateMedicalRecordDraft_ShouldUpdateClinicalInformation()
    {
        await using var scope =
            _factory.Services.CreateAsyncScope();

        var db =
            scope.ServiceProvider
                .GetRequiredService<ClinicManagementDbContext>();

        await db.Database.EnsureCreatedAsync();


        // =====================================================
        // SEED
        // =====================================================

        var scenario =
            await SeedMedicalRecordScenarioAsync(db);


        // =====================================================
        // GET SERVICE
        // =====================================================

        var service =
            scope.ServiceProvider
                .GetRequiredService<IMedicalRecordService>();


        // =====================================================
        // REQUEST
        // =====================================================

        var request =
            new MedicalRecordDraftRequestDTO
            {
                Symptoms =
                    "Đau đầu, chóng mặt",

                Diagnosis =
                    "Migraine",

                Icd10Code =
                    "G43.9",

                TreatmentPlan =
                    "Nghỉ ngơi và theo dõi",

                Note =
                    "Tái khám nếu triệu chứng kéo dài",

                FollowUpDate =
                    DateOnly.FromDateTime(
                        DateTime.UtcNow.AddDays(14)
                    )
            };


        // =====================================================
        // ACT
        // =====================================================

        var result =
            await service.UpdateMedicalRecordDraftAsync(
                scenario.MedicalRecord.Id,
                scenario.User.Id,
                request
            );


        // =====================================================
        // RESPONSE
        // =====================================================

        Assert.Equal(
            200,
            result.StatusCode
        );

        Assert.NotNull(
            result.Content
        );


        // =====================================================
        // RESPONSE CONTENT
        // =====================================================

        Assert.Equal(
            "Đau đầu, chóng mặt",
            result.Content!.Symptoms
        );

        Assert.Equal(
            "Migraine",
            result.Content.Diagnosis
        );

        Assert.Equal(
            "G43.9",
            result.Content.Icd10Code
        );

        Assert.Equal(
            "Nghỉ ngơi và theo dõi",
            result.Content.TreatmentPlan
        );

        Assert.Equal(
            "Tái khám nếu triệu chứng kéo dài",
            result.Content.Note
        );

        Assert.Equal(
            request.FollowUpDate,
            result.Content.FollowUpDate
        );

        Assert.Equal(
            (byte)MedicalRecordStatus.Draft,
            result.Content.Status
        );


        // =====================================================
        // DATABASE VERIFICATION
        // =====================================================

        var savedMedicalRecord =
            await db.MedicalRecords
                .SingleAsync(
                    m =>
                        m.Id ==
                        scenario.MedicalRecord.Id
                );


        Assert.Equal(
            "Đau đầu, chóng mặt",
            savedMedicalRecord.Symptoms
        );

        Assert.Equal(
            "Migraine",
            savedMedicalRecord.Diagnosis
        );

        Assert.Equal(
            "G43.9",
            savedMedicalRecord.Icd10Code
        );

        Assert.Equal(
            "Nghỉ ngơi và theo dõi",
            savedMedicalRecord.TreatmentPlan
        );

        Assert.Equal(
            "Tái khám nếu triệu chứng kéo dài",
            savedMedicalRecord.Note
        );

        Assert.Equal(
            request.FollowUpDate,
            savedMedicalRecord.FollowUpDate
        );

        Assert.Equal(
            (byte)MedicalRecordStatus.Draft,
            savedMedicalRecord.Status
        );
    }

    // =====================================================
    // TEST 2
    //
    // Update Medical Record Draft
    // + Create Patient Vital
    //
    // Kiểm tra:
    // - MedicalRecord vẫn được update
    // - PatientVital chưa tồn tại
    // - Service tạo PatientVital mới
    // - PatientVital gắn đúng MedicalRecordId
    // - Các giá trị vital được lưu đúng DB
    // =====================================================

    [Fact]
    public async Task UpdateMedicalRecordDraft_ShouldCreatePatientVital()
    {
        await using var scope =
            _factory.Services.CreateAsyncScope();

        var db =
            scope.ServiceProvider
                .GetRequiredService<ClinicManagementDbContext>();

        await db.Database.EnsureCreatedAsync();


        // =====================================================
        // SEED
        // =====================================================

        var scenario =
            await SeedMedicalRecordScenarioAsync(db);


        // =====================================================
        // VERIFY INITIAL STATE
        //
        // Scenario chưa có PatientVital.
        // =====================================================

        var initialVital =
            await db.PatientVitals
                .SingleOrDefaultAsync(
                    v =>
                        v.MedicalRecordId ==
                        scenario.MedicalRecord.Id
                );

        Assert.Null(initialVital);


        // =====================================================
        // GET SERVICE
        // =====================================================

        var service =
            scope.ServiceProvider
                .GetRequiredService<IMedicalRecordService>();


        // =====================================================
        // REQUEST
        // =====================================================

        var request =
            new MedicalRecordDraftRequestDTO
            {
                Symptoms =
                    "Sốt nhẹ",

                Diagnosis =
                    "Theo dõi nhiễm trùng",

                Icd10Code =
                    "R50.9",

                TreatmentPlan =
                    "Theo dõi nhiệt độ",

                Note =
                    "Đo lại sinh hiệu",

                FollowUpDate =
                    DateOnly.FromDateTime(
                        DateTime.UtcNow.AddDays(7)
                    ),

                Vitals =
                    new PatientVitalRequestDTO
                    {
                        Temperature =
                            38.2m,

                        Pulse =
                            88,

                        BloodPressure =
                            "120/80",

                        Weight =
                            62.5m,

                        Height =
                            170m
                    }
            };


        // =====================================================
        // ACT
        // =====================================================

        var result =
            await service.UpdateMedicalRecordDraftAsync(
                scenario.MedicalRecord.Id,
                scenario.User.Id,
                request
            );


        // =====================================================
        // RESPONSE
        // =====================================================

        Assert.Equal(
            200,
            result.StatusCode
        );

        Assert.NotNull(
            result.Content
        );


        // =====================================================
        // RESPONSE VITAL
        // =====================================================

        Assert.NotNull(
            result.Content!.Vitals
        );

        Assert.Equal(
            scenario.MedicalRecord.Id,
            result.Content.Vitals!.MedicalRecordId
        );

        Assert.Equal(
            38.2m,
            result.Content.Vitals.Temperature
        );

        Assert.Equal(
            88,
            result.Content.Vitals.Pulse
        );

        Assert.Equal(
            "120/80",
            result.Content.Vitals.BloodPressure
        );

        Assert.Equal(
            62.5m,
            result.Content.Vitals.Weight
        );

        Assert.Equal(
            170m,
            result.Content.Vitals.Height
        );


        // =====================================================
        // DATABASE VERIFICATION
        // =====================================================

        var savedVitals =
            await db.PatientVitals
                .Where(
                    v =>
                        v.MedicalRecordId ==
                        scenario.MedicalRecord.Id
                )
                .ToListAsync();


        // Chỉ được tạo đúng 1 PatientVital.
        Assert.Single(
            savedVitals
        );


        var savedVital =
            savedVitals.Single();


        // =====================================================
        // VERIFY RELATIONSHIP
        // =====================================================

        Assert.Equal(
            scenario.MedicalRecord.Id,
            savedVital.MedicalRecordId
        );


        // =====================================================
        // VERIFY VALUES
        // =====================================================

        Assert.Equal(
            38.2m,
            savedVital.Temperature
        );

        Assert.Equal(
            88,
            savedVital.Pulse
        );

        Assert.Equal(
            "120/80",
            savedVital.BloodPressure
        );

        Assert.Equal(
            62.5m,
            savedVital.Weight
        );

        Assert.Equal(
            170m,
            savedVital.Height
        );
    }

    // =====================================================
    // TEST 3
    //
    // Update Medical Record Draft
    // + Update Existing Patient Vital
    //
    // Kiểm tra:
    // - MedicalRecord đã có PatientVital
    // - Service không INSERT PatientVital mới
    // - PatientVital cũ được UPDATE
    // - Chỉ tồn tại đúng 1 PatientVital
    // - MedicalRecordId không thay đổi
    // =====================================================

    [Fact]
    public async Task UpdateMedicalRecordDraft_ShouldUpdateExistingPatientVital()
    {
        await using var scope =
            _factory.Services.CreateAsyncScope();

        var db =
            scope.ServiceProvider
                .GetRequiredService<ClinicManagementDbContext>();

        await db.Database.EnsureCreatedAsync();


        // =====================================================
        // SEED MEDICAL RECORD
        // =====================================================

        var scenario =
            await SeedMedicalRecordScenarioAsync(db);


        // =====================================================
        // SEED EXISTING PATIENT VITAL
        // =====================================================

        var existingVital =
            new PatientVital
            {
                MedicalRecordId =
                    scenario.MedicalRecord.Id,

                Temperature =
                    37.0m,

                Pulse =
                    70,

                BloodPressure =
                    "110/70",

                Weight =
                    60m,

                Height =
                    168m
            };


        db.PatientVitals.Add(
            existingVital
        );

        await db.SaveChangesAsync();


        var originalVitalId =
            existingVital.Id;


        // =====================================================
        // VERIFY INITIAL STATE
        // =====================================================

        var initialVitals =
            await db.PatientVitals
                .Where(
                    v =>
                        v.MedicalRecordId ==
                        scenario.MedicalRecord.Id
                )
                .ToListAsync();


        Assert.Single(
            initialVitals
        );

        Assert.Equal(
            originalVitalId,
            initialVitals.Single().Id
        );


        // =====================================================
        // GET SERVICE
        // =====================================================

        var service =
            scope.ServiceProvider
                .GetRequiredService<IMedicalRecordService>();


        // =====================================================
        // REQUEST
        // =====================================================

        var request =
            new MedicalRecordDraftRequestDTO
            {
                Symptoms =
                    "Đau đầu nhiều hơn",

                Diagnosis =
                    "Migraine",

                Icd10Code =
                    "G43.9",

                TreatmentPlan =
                    "Theo dõi và điều trị",

                Note =
                    "Cập nhật sinh hiệu",

                FollowUpDate =
                    DateOnly.FromDateTime(
                        DateTime.UtcNow.AddDays(10)
                    ),

                Vitals =
                    new PatientVitalRequestDTO
                    {
                        Temperature =
                            38.1m,

                        Pulse =
                            92,

                        BloodPressure =
                            "125/82",

                        Weight =
                            61.5m,

                        Height =
                            169m
                    }
            };


        // =====================================================
        // ACT
        // =====================================================

        var result =
            await service.UpdateMedicalRecordDraftAsync(
                scenario.MedicalRecord.Id,
                scenario.User.Id,
                request
            );


        // =====================================================
        // RESPONSE
        // =====================================================

        Assert.Equal(
            200,
            result.StatusCode
        );

        Assert.NotNull(
            result.Content
        );

        Assert.NotNull(
            result.Content!.Vitals
        );


        // =====================================================
        // RESPONSE VITAL
        // =====================================================

        Assert.Equal(
            originalVitalId,
            result.Content.Vitals!.Id
        );

        Assert.Equal(
            scenario.MedicalRecord.Id,
            result.Content.Vitals.MedicalRecordId
        );

        Assert.Equal(
            38.1m,
            result.Content.Vitals.Temperature
        );

        Assert.Equal(
            92,
            result.Content.Vitals.Pulse
        );

        Assert.Equal(
            "125/82",
            result.Content.Vitals.BloodPressure
        );

        Assert.Equal(
            61.5m,
            result.Content.Vitals.Weight
        );

        Assert.Equal(
            169m,
            result.Content.Vitals.Height
        );


        // =====================================================
        // DATABASE VERIFICATION
        // =====================================================

        var savedVitals =
            await db.PatientVitals
                .Where(
                    v =>
                        v.MedicalRecordId ==
                        scenario.MedicalRecord.Id
                )
                .ToListAsync();


        // =====================================================
        // QUAN TRỌNG:
        // KHÔNG ĐƯỢC CÓ PATIENT VITAL THỨ 2
        // =====================================================

        Assert.Single(
            savedVitals
        );


        var savedVital =
            savedVitals.Single();


        // =====================================================
        // VERIFY ID KHÔNG THAY ĐỔI
        // =====================================================

        Assert.Equal(
            originalVitalId,
            savedVital.Id
        );


        // =====================================================
        // VERIFY RELATIONSHIP
        // =====================================================

        Assert.Equal(
            scenario.MedicalRecord.Id,
            savedVital.MedicalRecordId
        );


        // =====================================================
        // VERIFY UPDATED VALUES
        // =====================================================

        Assert.Equal(
            38.1m,
            savedVital.Temperature
        );

        Assert.Equal(
            92,
            savedVital.Pulse
        );

        Assert.Equal(
            "125/82",
            savedVital.BloodPressure
        );

        Assert.Equal(
            61.5m,
            savedVital.Weight
        );

        Assert.Equal(
            169m,
            savedVital.Height
        );
    }

    // =====================================================
    // TEST 4
    //
    // Không được update MedicalRecord đã Finalized.
    //
    // Kiểm tra:
    // - MedicalRecord đang Finalized
    // - Doctor đúng owner
    // - Service từ chối update
    // - Status = 409
    // - Dữ liệu cũ không bị thay đổi
    // =====================================================

    [Fact]
    public async Task UpdateMedicalRecordDraft_ShouldReturn409_WhenMedicalRecordIsFinalized()
    {
        await using var scope =
            _factory.Services.CreateAsyncScope();

        var db =
            scope.ServiceProvider
                .GetRequiredService<ClinicManagementDbContext>();


        await db.Database.EnsureCreatedAsync();


        // =====================================================
        // SEED
        // =====================================================

        var scenario =
            await SeedMedicalRecordScenarioAsync(db);


        // =====================================================
        // SAVE ORIGINAL VALUES
        // =====================================================

        var originalSymptoms =
            scenario.MedicalRecord.Symptoms;

        var originalDiagnosis =
            scenario.MedicalRecord.Diagnosis;

        var originalIcd10Code =
            scenario.MedicalRecord.Icd10Code;


        // =====================================================
        // FINALIZE MEDICAL RECORD
        // =====================================================

        scenario.MedicalRecord.Status =
            (byte)MedicalRecordStatus.Finalized;

        scenario.MedicalRecord.FinalizedAt =
            DateTime.UtcNow;

        await db.SaveChangesAsync();


        // =====================================================
        // GET SERVICE
        // =====================================================

        var service =
            scope.ServiceProvider
                .GetRequiredService<IMedicalRecordService>();


        // =====================================================
        // REQUEST
        //
        // Cố tình gửi dữ liệu khác để xác nhận
        // service không cho phép update.
        // =====================================================

        var request =
            new MedicalRecordDraftRequestDTO
            {
                Symptoms =
                    "DỮ LIỆU KHÔNG ĐƯỢC PHÉP UPDATE",

                Diagnosis =
                    "DỮ LIỆU MỚI",

                Icd10Code =
                    "Z99.99",

                TreatmentPlan =
                    "Không được thay đổi",

                Note =
                    "Không được thay đổi"
            };


        // =====================================================
        // ACT
        // =====================================================

        var result =
            await service.UpdateMedicalRecordDraftAsync(
                scenario.MedicalRecord.Id,
                scenario.User.Id,
                request
            );


        // =====================================================
        // RESPONSE
        // =====================================================

        Assert.Equal(
            409,
            result.StatusCode
        );


        // =====================================================
        // RELOAD FROM DATABASE
        // =====================================================

        var savedMedicalRecord =
            await db.MedicalRecords
                .AsNoTracking()
                .SingleAsync(
                    m =>
                        m.Id ==
                        scenario.MedicalRecord.Id
                );


        // =====================================================
        // VERIFY STATUS
        // =====================================================

        Assert.Equal(
            (byte)MedicalRecordStatus.Finalized,
            savedMedicalRecord.Status
        );


        Assert.NotNull(
            savedMedicalRecord.FinalizedAt
        );


        // =====================================================
        // VERIFY ORIGINAL DATA WAS NOT CHANGED
        // =====================================================

        Assert.Equal(
            originalSymptoms,
            savedMedicalRecord.Symptoms
        );

        Assert.Equal(
            originalDiagnosis,
            savedMedicalRecord.Diagnosis
        );

        Assert.Equal(
            originalIcd10Code,
            savedMedicalRecord.Icd10Code
        );
    }

    // =====================================================
    // TEST 5
    //
    // Doctor lấy MedicalRecord của chính mình.
    //
    // Kiểm tra:
    // - Doctor tồn tại
    // - MedicalRecord thuộc Doctor hiện tại
    // - Status Draft vẫn được đọc
    // - Mapping sang MedicalRecordExamDTO đúng
    // - PatientVital chưa có => Vital = null
    // - Chưa có service => Services rỗng
    // =====================================================

    [Fact]
    public async Task GetMedicalRecordByIdForDoctor_ShouldReturnMedicalRecord()
    {
        await using var scope =
            _factory.Services.CreateAsyncScope();

        var db =
            scope.ServiceProvider
                .GetRequiredService<ClinicManagementDbContext>();

        await db.Database.EnsureCreatedAsync();


        // =====================================================
        // SEED
        // =====================================================

        var scenario =
            await SeedMedicalRecordScenarioAsync(db);


        // =====================================================
        // UPDATE SOME DATA FOR VERIFICATION
        // =====================================================

        scenario.MedicalRecord.Symptoms =
            "Ho và đau họng";

        scenario.MedicalRecord.Diagnosis =
            "Viêm họng";

        scenario.MedicalRecord.Icd10Code =
            "J02.9";

        scenario.MedicalRecord.TreatmentPlan =
            "Theo dõi và uống thuốc";

        scenario.MedicalRecord.Note =
            "Tái khám nếu không cải thiện";

        scenario.MedicalRecord.FollowUpDate =
            DateOnly.FromDateTime(
                DateTime.UtcNow.AddDays(7)
            );

        await db.SaveChangesAsync();


        // =====================================================
        // GET SERVICE
        // =====================================================

        var service =
            scope.ServiceProvider
                .GetRequiredService<IMedicalRecordService>();


        // =====================================================
        // ACT
        // =====================================================

        var result =
            await service.GetMedicalRecordByIdForDoctorAsync(
                scenario.MedicalRecord.Id,
                scenario.User.Id
            );


        // =====================================================
        // RESPONSE
        // =====================================================

        Assert.Equal(
            200,
            result.StatusCode
        );

        Assert.NotNull(
            result.Content
        );


        var content =
            result.Content!;


        // =====================================================
        // MEDICAL RECORD
        // =====================================================

        Assert.Equal(
            scenario.MedicalRecord.Id,
            content.Id
        );

        Assert.Equal(
            scenario.Appointment.Id,
            content.AppointmentId
        );

        Assert.Equal(
            scenario.Patient.Id,
            content.PatientId
        );


        // =====================================================
        // DOCTOR
        // =====================================================

        Assert.Equal(
            scenario.Doctor.FullName,
            content.DoctorName
        );


        // =====================================================
        // CLINICAL INFORMATION
        // =====================================================

        Assert.Equal(
            "Ho và đau họng",
            content.Symptoms
        );

        Assert.Equal(
            "Viêm họng",
            content.Diagnosis
        );

        Assert.Equal(
            "J02.9",
            content.Icd10Code
        );

        Assert.Equal(
            "Theo dõi và uống thuốc",
            content.TreatmentPlan
        );

        Assert.Equal(
            "Tái khám nếu không cải thiện",
            content.Note
        );


        // =====================================================
        // STATUS
        // =====================================================

        Assert.Equal(
            (byte)MedicalRecordStatus.Draft,
            content.Status
        );


        Assert.Null(
            content.FinalizedAt
        );


        // =====================================================
        // FOLLOW UP
        // =====================================================

        Assert.Equal(
            scenario.MedicalRecord.FollowUpDate,
            content.FollowUpDate
        );


        // =====================================================
        // VITAL
        // =====================================================

        Assert.Null(
            content.Vital
        );


        // =====================================================
        // MEDICAL RECORD SERVICES
        // =====================================================

        Assert.NotNull(
            content.Services
        );

        Assert.Empty(
            content.Services
        );
    }

    // =====================================================
    // TEST 6
    //
    // Doctor khác không được xem MedicalRecord
    // =====================================================

    [Fact]
    public async Task GetMedicalRecordByIdForDoctor_ShouldReturn404_WhenBelongsToAnotherDoctor()
    {
        await using var scope =
            _factory.Services.CreateAsyncScope();

        var db =
            scope.ServiceProvider
                .GetRequiredService<ClinicManagementDbContext>();

        await db.Database.EnsureCreatedAsync();


        // =====================================================
        // SEED MEDICAL RECORD
        // =====================================================

        var scenario =
            await SeedMedicalRecordScenarioAsync(db);


        // =====================================================
        // CREATE ANOTHER DOCTOR
        // =====================================================

        var token =
            Guid.NewGuid()
                .ToString("N");

        var anotherUser =
            new User
            {
                Username =
                    $"it_other_doctor_{token}",

                PasswordHash =
                    "integration-test-password-hash",

                Email =
                    $"it_other_doctor_{token}@example.com",

                FullName =
                    "Another Integration Doctor",

                IsActive =
                    true,

                EmailConfirmed =
                    true,

                IsDeleted =
                    false,

                CreatedAt =
                    DateTime.UtcNow,

                FailedLoginCount =
                    0
            };


        var anotherDoctor =
            new Doctor
            {
                User =
                    anotherUser,

                SpecialtyId =
                    scenario.Doctor.SpecialtyId,

                FullName =
                    "Another Integration Doctor",

                Title =
                    "Doctor",

                Phone =
                    "0900000201",

                Email =
                    $"another_doctor_{token}@example.com",

                IsActive =
                    true,

                CreatedAt =
                    DateTime.UtcNow,

                ConsultationFee =
                    100
            };


        db.Users.Add(
            anotherUser
        );

        db.Doctors.Add(
            anotherDoctor
        );

        await db.SaveChangesAsync();


        // =====================================================
        // GET SERVICE
        // =====================================================

        var service =
            scope.ServiceProvider
                .GetRequiredService<IMedicalRecordService>();


        // =====================================================
        // ACT
        // =====================================================

        var result =
            await service.GetMedicalRecordByIdForDoctorAsync(
                scenario.MedicalRecord.Id,
                anotherUser.Id
            );


        // =====================================================
        // ASSERT
        // =====================================================

        Assert.Equal(
            404,
            result.StatusCode
        );

        Assert.Null(
            result.Content
        );
    }

    // =====================================================
    // TEST 7
    //
    // Add MedicalRecordService
    //
    // Expected:
    // 201
    // Status = Ordered
    // UnitPriceSnapshot = Service.Price
    // =====================================================

    [Fact]
    public async Task AddMedicalRecordService_ShouldCreateOrderedService()
    {
        await using var scope =
            _factory.Services.CreateAsyncScope();

        var db =
            scope.ServiceProvider
                .GetRequiredService<ClinicManagementDbContext>();

        await db.Database.EnsureCreatedAsync();


        // =====================================================
        // SEED
        // =====================================================

        var scenario =
            await SeedMedicalRecordScenarioAsync(db);


        var serviceEntity =
            await SeedBillingServiceAsync(
                db,
                scenario.Doctor.SpecialtyId
            );


        // =====================================================
        // GET SERVICE
        // =====================================================

        var service =
            scope.ServiceProvider
                .GetRequiredService<IMedicalRecordService>();


        // =====================================================
        // REQUEST
        // =====================================================

        var request =
            new MedicalRecordServiceRequestDTO
            {
                ServiceId =
                    serviceEntity.Id,

                Quantity =
                    2
            };


        // =====================================================
        // ACT
        // =====================================================

        var result =
            await service.AddMedicalRecordServiceAsync(
                scenario.MedicalRecord.Id,
                scenario.User.Id,
                request
            );


        // =====================================================
        // RESPONSE
        // =====================================================

        Assert.Equal(
            201,
            result.StatusCode
        );

        Assert.NotNull(
            result.Content
        );

        Assert.Equal(
            serviceEntity.Id,
            result.Content!.ServiceId
        );

        Assert.Equal(
            serviceEntity.Name,
            result.Content.ServiceName
        );

        Assert.Equal(
            2,
            result.Content.Quantity
        );

        Assert.Equal(
            serviceEntity.Price,
            result.Content.UnitPriceSnapshot
        );

        Assert.Equal(
            (byte)MedicalRecordServiceStatus.Ordered,
            result.Content.Status
        );


        // =====================================================
        // DATABASE
        // =====================================================

        var saved =
            await db.MedicalRecordServices
                .SingleAsync(
                    x =>
                        x.Id ==
                        result.Content.Id
                );


        Assert.Equal(
            scenario.MedicalRecord.Id,
            saved.MedicalRecordId
        );

        Assert.Equal(
            serviceEntity.Id,
            saved.ServiceId
        );

        Assert.Equal(
            2,
            saved.Quantity
        );

        Assert.Equal(
            serviceEntity.Price,
            saved.UnitPriceSnapshot
        );

        Assert.Equal(
            (byte)MedicalRecordServiceStatus.Ordered,
            saved.Status
        );
    }

    // =====================================================
    // TEST 8
    //
    // Cancel MedicalRecordService
    //
    // Ordered → Cancelled
    // =====================================================

    [Fact]
    public async Task CancelMedicalRecordService_ShouldSetStatusCancelled()
    {
        await using var scope =
            _factory.Services.CreateAsyncScope();

        var db =
            scope.ServiceProvider
                .GetRequiredService<ClinicManagementDbContext>();

        await db.Database.EnsureCreatedAsync();


        // =====================================================
        // SEED
        // =====================================================

        var scenario =
            await SeedMedicalRecordScenarioAsync(db);


        var serviceEntity =
            await SeedBillingServiceAsync(
                db,
                scenario.Doctor.SpecialtyId
            );


        var medicalRecordService =
            new MedicalRecordServiceEntity
            {
                MedicalRecordId =
                    scenario.MedicalRecord.Id,

                ServiceId =
                    serviceEntity.Id,

                Quantity =
                    1,

                UnitPriceSnapshot =
                    serviceEntity.Price,

                Status =
                    (byte)MedicalRecordServiceStatus.Ordered,

                OrderedAt =
                    DateTime.UtcNow
            };


        db.MedicalRecordServices.Add(
            medicalRecordService
        );

        await db.SaveChangesAsync();


        // =====================================================
        // GET SERVICE
        // =====================================================

        var service =
            scope.ServiceProvider
                .GetRequiredService<IMedicalRecordService>();


        // =====================================================
        // ACT
        // =====================================================

        var result =
            await service.CancelMedicalRecordServiceAsync(
                medicalRecordService.Id,
                scenario.User.Id
            );


        // =====================================================
        // RESPONSE
        // =====================================================

        Assert.Equal(
            200,
            result.StatusCode
        );

        Assert.NotNull(
            result.Content
        );

        Assert.Equal(
            (byte)MedicalRecordServiceStatus.Cancelled,
            result.Content!.Status
        );


        // =====================================================
        // DATABASE
        // =====================================================

        var saved =
            await db.MedicalRecordServices
                .SingleAsync(
                    x =>
                        x.Id ==
                        medicalRecordService.Id
                );


        Assert.Equal(
            (byte)MedicalRecordServiceStatus.Cancelled,
            saved.Status
        );
    }

    // =====================================================
    // TEST 9
    //
    // Add Lab Result
    //
    // Expected:
    // MedicalRecordService
    // Ordered → Completed
    //
    // LabResult được INSERT
    // =====================================================

    [Fact]
    public async Task AddMedicalRecordServiceResult_ShouldCreateLabResultAndCompleteService()
    {
        await using var scope =
            _factory.Services.CreateAsyncScope();

        var db =
            scope.ServiceProvider
                .GetRequiredService<ClinicManagementDbContext>();

        await db.Database.EnsureCreatedAsync();


        // =====================================================
        // SEED
        // =====================================================

        var scenario =
            await SeedMedicalRecordScenarioAsync(db);


        var serviceEntity =
            await SeedBillingServiceAsync(
                db,
                scenario.Doctor.SpecialtyId
            );


        var medicalRecordService =
            new MedicalRecordServiceEntity
            {
                MedicalRecordId =
                    scenario.MedicalRecord.Id,

                ServiceId =
                    serviceEntity.Id,

                Quantity =
                    1,

                UnitPriceSnapshot =
                    serviceEntity.Price,

                Status =
                    (byte)MedicalRecordServiceStatus.Ordered,

                OrderedAt =
                    DateTime.UtcNow
            };


        db.MedicalRecordServices.Add(
            medicalRecordService
        );

        await db.SaveChangesAsync();


        // =====================================================
        // REQUEST
        // =====================================================

        var resultedAt =
            DateTime.UtcNow;


        var request =
            new LabResultRequestDTO
            {
                ResultValue =
                    "Âm tính",

                ReferenceRange =
                    "Âm tính",

                Conclusion =
                    "Không phát hiện bất thường",

                ResultedAt =
                    resultedAt
            };


        // =====================================================
        // GET SERVICE
        // =====================================================

        var service =
            scope.ServiceProvider
                .GetRequiredService<IMedicalRecordService>();


        // =====================================================
        // ACT
        // =====================================================

        var result =
            await service.AddMedicalRecordServiceResultAsync(
                medicalRecordService.Id,
                scenario.User.Id,
                request
            );


        // =====================================================
        // RESPONSE
        // =====================================================

        Assert.Equal(
            200,
            result.StatusCode
        );

        Assert.NotNull(
            result.Content
        );

        Assert.Equal(
            (byte)MedicalRecordServiceStatus.Completed,
            result.Content!.Status
        );

        Assert.NotNull(
            result.Content.Result
        );

        Assert.Equal(
            "Âm tính",
            result.Content.Result!.ResultValue
        );

        Assert.Equal(
            "Âm tính",
            result.Content.Result.ReferenceRange
        );

        Assert.Equal(
            "Không phát hiện bất thường",
            result.Content.Result.Conclusion
        );


        // =====================================================
        // DATABASE - SERVICE
        // =====================================================

        var savedService =
            await db.MedicalRecordServices
                .SingleAsync(
                    x =>
                        x.Id ==
                        medicalRecordService.Id
                );


        Assert.Equal(
            (byte)MedicalRecordServiceStatus.Completed,
            savedService.Status
        );

        Assert.Equal(
            scenario.User.Id,
            savedService.PerformedBy
        );

        Assert.NotNull(
            savedService.CompletedAt
        );


        // =====================================================
        // DATABASE - LAB RESULT
        // =====================================================

        var savedResult =
            await db.LabResults
                .SingleAsync(
                    x =>
                        x.MedicalRecordServiceId ==
                        medicalRecordService.Id
                );


        Assert.Equal(
            "Âm tính",
            savedResult.ResultValue
        );

        Assert.Equal(
            "Âm tính",
            savedResult.ReferenceRange
        );

        Assert.Equal(
            "Không phát hiện bất thường",
            savedResult.Conclusion
        );

        Assert.Equal(
            resultedAt,
            savedResult.ResultedAt
        );
    }

    // =====================================================
    // TEST 10
    //
    // Finalize MedicalRecord
    //
    // Kiểm tra:
    // - Diagnosis bắt buộc
    // - MedicalRecord → Finalized
    // - FinalizedAt được set
    // - Invoice được tạo
    // - Appointment → Completed
    // - AppointmentStatusHistory được tạo
    // - AuditLog được tạo
    // =====================================================

    [Fact]
    public async Task FinalizeMedicalRecord_ShouldFinalizeRecordCreateInvoiceAndAudit()
    {
        await using var scope =
            _factory.Services.CreateAsyncScope();

        var db =
            scope.ServiceProvider
                .GetRequiredService<ClinicManagementDbContext>();

        await db.Database.EnsureCreatedAsync();


        // =====================================================
        // SEED
        // =====================================================

        var scenario =
            await SeedMedicalRecordScenarioAsync(db);


        // =====================================================
        // DIAGNOSIS REQUIRED FOR FINALIZE
        // =====================================================

        scenario.MedicalRecord.Diagnosis =
            "Viêm họng";

        await db.SaveChangesAsync();


        // =====================================================
        // GET SERVICE
        // =====================================================

        var service =
            scope.ServiceProvider
                .GetRequiredService<IMedicalRecordService>();


        // =====================================================
        // ACT
        // =====================================================

        var result =
            await service.FinalizeMedicalRecordAsync(
                scenario.MedicalRecord.Id,
                scenario.User.Id
            );


        // =====================================================
        // RESPONSE
        // =====================================================

        Assert.Equal(
            200,
            result.StatusCode
        );

        Assert.NotNull(
            result.Content
        );


        var invoice =
            result.Content!;


        // =====================================================
        // INVOICE
        // =====================================================

        Assert.Equal(
            scenario.Patient.Id,
            invoice.PatientId
        );

        Assert.Equal(
            scenario.Appointment.Id,
            invoice.AppointmentId
        );

        Assert.Equal(
            scenario.MedicalRecord.Id,
            invoice.MedicalRecordId
        );

        Assert.Equal(
            scenario.Patient.FullName,
            invoice.PatientName
        );

        Assert.Equal(
            scenario.Appointment.FeeSnapshot,
            invoice.TotalAmount
        );

        Assert.Equal(
            (byte)InvoiceStatus.Unpaid,
            invoice.Status
        );

        Assert.Equal(
            0m,
            invoice.PaidAmount
        );


        // =====================================================
        // DATABASE - MEDICAL RECORD
        // =====================================================

        var savedMedicalRecord =
            await db.MedicalRecords
                .AsNoTracking()
                .SingleAsync(
                    x =>
                        x.Id ==
                        scenario.MedicalRecord.Id
                );


        Assert.Equal(
            (byte)MedicalRecordStatus.Finalized,
            savedMedicalRecord.Status
        );

        Assert.NotNull(
            savedMedicalRecord.FinalizedAt
        );


        // =====================================================
        // DATABASE - APPOINTMENT
        // =====================================================

        var savedAppointment =
            await db.Appointments
                .AsNoTracking()
                .SingleAsync(
                    x =>
                        x.Id ==
                        scenario.Appointment.Id
                );


        Assert.Equal(
            (byte)AppointmentStatus.Completed,
            savedAppointment.Status
        );


        // =====================================================
        // APPOINTMENT STATUS HISTORY
        // =====================================================

        var history =
            await db.AppointmentStatusHistories
                .Where(
                    x =>
                        x.AppointmentId ==
                        scenario.Appointment.Id
                )
                .OrderByDescending(
                    x => x.Id
                )
                .FirstOrDefaultAsync();


        Assert.NotNull(
            history
        );

        Assert.Equal(
            (byte)AppointmentStatus.Completed,
            history!.ToStatus
        );

        Assert.Equal(
            scenario.User.Id,
            history.ChangedBy
        );

        Assert.Equal(
            "Chốt bệnh án.",
            history.Reason
        );


        // =====================================================
        // AUDIT LOG
        // =====================================================

        var auditLog =
            await db.AuditLogs
                .Where(
                    x =>
                        x.EntityName ==
                            "MedicalRecord"
                        &&
                        x.EntityId ==
                            scenario.MedicalRecord.Id
                        &&
                        x.Action ==
                            "FINALIZE_MEDICAL_RECORD"
                )
                .OrderByDescending(
                    x => x.Id
                )
                .FirstOrDefaultAsync();


        Assert.NotNull(
            auditLog
        );

        Assert.Equal(
            scenario.User.Id,
            auditLog!.UserId
        );

        Assert.True(
            auditLog.Succeeded
        );


        // =====================================================
        // DATABASE - INVOICE
        // =====================================================

        var savedInvoice =
            await db.Invoices
                .AsNoTracking()
                .SingleAsync(
                    x =>
                        x.Id ==
                        invoice.Id
                );


        Assert.Equal(
            scenario.MedicalRecord.Id,
            savedInvoice.MedicalRecordId
        );

        Assert.Equal(
            scenario.Appointment.Id,
            savedInvoice.AppointmentId
        );

        Assert.Equal(
            scenario.Patient.Id,
            savedInvoice.PatientId
        );

        Assert.Equal(
            scenario.Appointment.FeeSnapshot,
            savedInvoice.TotalAmount
        );


        // Không có CLS / thuốc trong scenario này.
        var invoiceItems =
            await db.InvoiceItems
                .Where(
                    x =>
                        x.InvoiceId ==
                        invoice.Id
                )
                .ToListAsync();


        Assert.Empty(
            invoiceItems
        );
    }

    // =====================================================
    // HELPER
    // CREATE BILLING SERVICE
    // =====================================================

    private static async Task<Service>
        SeedBillingServiceAsync(
            ClinicManagementDbContext db,
            int specialtyId)
    {
        var token =
            Guid.NewGuid()
                .ToString("N");

        var service =
            new Service
            {
                Code =
                    $"ITMR{token[..12]}",

                Name =
                    $"Integration Medical Service {token[..8]}",

                Description =
                    "Integration test service",

                SpecialtyId =
                    specialtyId,

                Price =
                    250m,

                DurationMinutes =
                    30,

                IsActive =
                    true,

                CreatedAt =
                    DateTime.UtcNow
            };

        db.Services.Add(
            service
        );

        await db.SaveChangesAsync();

        return service;
    }

    // =====================================================
    // HELPER
    // SEED MEDICAL RECORD SCENARIO
    // =====================================================
    //
    // Tạo:
    //
    // User
    //   ↓
    // Doctor ← Specialty
    //
    // Patient
    //   ↓
    // Appointment
    //   ↓
    // MedicalRecord
    //
    // =====================================================

    private static async Task<MedicalRecordScenario>
        SeedMedicalRecordScenarioAsync(
            ClinicManagementDbContext db)
    {
        var token =
            Guid.NewGuid()
                .ToString("N");


        // =====================================================
        // USER
        // =====================================================

        var user =
            new User
            {
                Username =
                    $"it_medical_record_{token}",

                PasswordHash =
                    "integration-test-password-hash",

                Email =
                    $"it_medical_record_{token}@example.com",

                FullName =
                    "Integration Medical Record Doctor",

                IsActive =
                    true,

                EmailConfirmed =
                    true,

                IsDeleted =
                    false,

                CreatedAt =
                    DateTime.UtcNow,

                FailedLoginCount =
                    0
            };


        // =====================================================
        // SPECIALTY
        // =====================================================

        var specialty =
            new Specialty
            {
                Code =
                    $"MR{token[..17]}",

                Name =
                    $"Integration Medical Record Specialty {token[..8]}",

                Description =
                    "Integration test specialty",

                IsActive =
                    true,

                CreatedAt =
                    DateTime.UtcNow
            };


        // =====================================================
        // DOCTOR
        // =====================================================

        var doctor =
            new Doctor
            {
                User =
                    user,

                Specialty =
                    specialty,

                FullName =
                    "Integration Medical Record Doctor",

                Title =
                    "Doctor",

                Phone =
                    "0900000101",

                Email =
                    $"doctor_medical_record_{token}@example.com",

                IsActive =
                    true,

                CreatedAt =
                    DateTime.UtcNow,

                ConsultationFee =
                    100
            };


        // =====================================================
        // PATIENT
        // =====================================================

        var patient =
            new Patient
            {
                FullName =
                    "Integration Medical Record Patient",

                Phone =
                    "0900000102",

                Email =
                    $"patient_medical_record_{token}@example.com",

                PatientCode =
                    $"MRPT{token[..16]}",

                IsActive =
                    true,

                CreatedAt =
                    DateTime.UtcNow
            };


        // =====================================================
        // APPOINTMENT
        // =====================================================

        var startTime =
            DateTime.UtcNow.AddDays(1);


        var appointment =
            new Appointment
            {
                Patient =
                    patient,

                Doctor =
                    doctor,

                StartTime =
                    startTime,

                EndTime =
                    startTime.AddMinutes(30),

                Status =
                    0,

                Reason =
                    "Integration test medical record",

                AppointmentCode =
                    $"MRAPT{token[..15]}",

                CreatedAt =
                    DateTime.UtcNow,

                Source =
                    0,

                FeeSnapshot =
                    100
            };


        // =====================================================
        // MEDICAL RECORD
        // =====================================================

        var medicalRecord =
            new MedicalRecordEntity
            {
                Appointment =
                    appointment,

                Patient =
                    patient,

                Doctor =
                    doctor,

                Symptoms =
                    "Old symptoms",

                Diagnosis =
                    "Old diagnosis",

                Icd10Code =
                    "Z00.00",

                TreatmentPlan =
                    "Old treatment",

                Note =
                    "Old note",

                CreatedAt =
                    DateTime.UtcNow,

                Status =
                    (byte)MedicalRecordStatus.Draft
            };


        // =====================================================
        // SAVE GRAPH
        // =====================================================

        db.MedicalRecords.Add(
            medicalRecord
        );

        await db.SaveChangesAsync();


        // =====================================================
        // RETURN SCENARIO
        // =====================================================

        return new MedicalRecordScenario(
            user,
            doctor,
            patient,
            appointment,
            medicalRecord
        );
    }


    // =====================================================
    // TEST SCENARIO
    // =====================================================

    private sealed record MedicalRecordScenario(
        User User,
        Doctor Doctor,
        Patient Patient,
        Appointment Appointment,
       MedicalRecordEntity MedicalRecord
    );


}