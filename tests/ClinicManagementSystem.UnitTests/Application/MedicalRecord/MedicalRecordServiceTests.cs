using System.Linq.Expressions;

using ClinicManagementSystem.Application.Enums;
using ClinicManagementSystem.Application.DTOs.Prescription;
using ClinicManagementSystem.Application.Services;

using ClinicManagementSystem.Infrastructure.Models;
using ClinicManagementSystem.Infrastructure.Repositories;
using ClinicManagementSystem.Infrastructure.UnitOfWork;

using Microsoft.Extensions.Logging.Abstractions;

using MockQueryable;

using Moq;

using ClinicManagementSystem.Application.DTOs.MedicalRecord;

using MedicalRecordAppService =
    ClinicManagementSystem.Application.Services.MedicalRecordService;

using MedicalRecordEntity =
    ClinicManagementSystem.Infrastructure.Models.MedicalRecord;

using MedicalRecordServiceEntity =
    ClinicManagementSystem.Infrastructure.Models.MedicalRecordService;


namespace ClinicManagementSystem.UnitTests.Application.MedicalRecord;


public class MedicalRecordServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;

    private readonly Mock<IInvoiceService> _invoiceServiceMock;

    private readonly Mock<IFileStorageService> _fileStorageServiceMock;

    private readonly MedicalRecordAppService _service;


    // =====================================================
    // CONSTRUCTOR
    // =====================================================

    public MedicalRecordServiceTests()
    {
        _unitOfWorkMock =
            new Mock<IUnitOfWork>();

        _invoiceServiceMock =
            new Mock<IInvoiceService>();

        _fileStorageServiceMock =
            new Mock<IFileStorageService>();

        _service =
            new MedicalRecordAppService(
                _unitOfWorkMock.Object,
                _invoiceServiceMock.Object,
                _fileStorageServiceMock.Object,
                NullLogger<MedicalRecordAppService>.Instance
            );
    }


    // =====================================================
    // HELPER
    // =====================================================

    private static PrescriptionRequestDTO CreateValidRequest()
    {
        return new PrescriptionRequestDTO
        {
            Items =
            [
                new()
                {
                    MedicineId = 1,
                    Quantity = 1,
                    Dosage = "1 lần/ngày"
                }
            ]
        };
    }


    private void SetupDoctors(
        params Doctor[] doctors)
    {
        var repositoryMock =
            new Mock<IDoctorRepository>();

        repositoryMock
            .Setup(
                x =>
                    x.WhereSql(
                        It.IsAny<
                            Expression<
                                Func<
                                    Doctor,
                                    bool
                                >
                            >
                        >()
                    )
            )
            .Returns(
                (
                    Expression<
                        Func<
                            Doctor,
                            bool
                        >
                    > predicate
                ) =>
                    doctors
                        .Where(
                            predicate.Compile()
                        )
                        .ToList()
                        .BuildMock()
            );

        _unitOfWorkMock
            .Setup(
                x =>
                    x.DoctorRepository
            )
            .Returns(
                repositoryMock.Object
            );
    }

    private void SetupMedicalRecords(
        params MedicalRecordEntity[] medicalRecords)
    {
        var repositoryMock =
            new Mock<IMedicalRecordRepository>();

        repositoryMock
            .Setup(
                x =>
                    x.WhereSql(
                        It.IsAny<
                            Expression<
                                Func<
                                    MedicalRecordEntity,
                                    bool
                                >
                            >
                        >()
                    )
            )
            .Returns(
                (
                    Expression<
                        Func<
                            MedicalRecordEntity,
                            bool
                        >
                    > predicate
                ) =>
                    medicalRecords
                        .Where(
                            predicate.Compile()
                        )
                        .ToList()
                        .BuildMock()
            );

        _unitOfWorkMock
            .Setup(
                x =>
                    x.MedicalRecordRepository
            )
            .Returns(
                repositoryMock.Object
            );
    }


    private void SetupPrescriptions(
        params Prescription[] prescriptions)
    {
        var repositoryMock =
            new Mock<IPrescriptionRepository>();

        repositoryMock
            .Setup(
                x =>
                    x.WhereSql(
                        It.IsAny<
                            Expression<
                                Func<
                                    Prescription,
                                    bool
                                >
                            >
                        >()
                    )
            )
            .Returns(
                (
                    Expression<
                        Func<
                            Prescription,
                            bool
                        >
                    > predicate
                ) =>
                    prescriptions
                        .Where(
                            predicate.Compile()
                        )
                        .ToList()
                        .BuildMock()
            );

        _unitOfWorkMock
            .Setup(
                x =>
                    x.PrescriptionRepository
            )
            .Returns(
                repositoryMock.Object
            );
    }


    private void VerifyNoTransaction()
    {
        _unitOfWorkMock.Verify(
            x =>
                x.BeginTransactionAsync(),
            Times.Never
        );

        _unitOfWorkMock.Verify(
            x =>
                x.SaveChangesAsync(),
            Times.Never
        );

        _unitOfWorkMock.Verify(
            x =>
                x.CommitTransactionAsync(),
            Times.Never
        );

        _unitOfWorkMock.Verify(
            x =>
                x.RollbackTransactionAsync(),
            Times.Never
        );
    }


    // =====================================================
    // TEST 1
    //
    // Invalid medicalRecordId
    //
    // medicalRecordId <= 0
    //
    // Expected:
    // 400
    // MedicalRecordNotFound
    // =====================================================

    [Fact]
    public async Task CreatePrescriptionAsync_ShouldReturn400_WhenMedicalRecordIdIsInvalid()
    {
        // Arrange

        var request =
            CreateValidRequest();


        // Act

        var result =
            await _service
                .CreatePrescriptionAsync(
                    medicalRecordId: 0,
                    currentUserId: 1,
                    request: request
                );


        // Assert

        Assert.Equal(
            400,
            result.StatusCode
        );

        Assert.Equal(
            PrescriptionResponseMessageDTO
                .MedicalRecordNotFound,
            result.Message
        );

        VerifyNoTransaction();
    }


    // =====================================================
    // TEST 2
    //
    // Invalid currentUserId
    //
    // currentUserId <= 0
    //
    // Expected:
    // 401
    // MedicalRecordAccessDenied
    // =====================================================

    [Fact]
    public async Task CreatePrescriptionAsync_ShouldReturn401_WhenCurrentUserIdIsInvalid()
    {
        // Arrange

        var request =
            CreateValidRequest();


        // Act

        var result =
            await _service
                .CreatePrescriptionAsync(
                    medicalRecordId: 1,
                    currentUserId: 0,
                    request: request
                );


        // Assert

        Assert.Equal(
            401,
            result.StatusCode
        );

        Assert.Equal(
            PrescriptionResponseMessageDTO
                .MedicalRecordAccessDenied,
            result.Message
        );

        VerifyNoTransaction();
    }


    // =====================================================
    // TEST 3
    //
    // Null request
    //
    // Expected:
    // 400
    // InvalidRequest
    // =====================================================

    [Fact]
    public async Task CreatePrescriptionAsync_ShouldReturn400_WhenRequestIsNull()
    {
        // Act

        var result =
            await _service
                .CreatePrescriptionAsync(
                    medicalRecordId: 1,
                    currentUserId: 1,
                    request: null!
                );


        // Assert

        Assert.Equal(
            400,
            result.StatusCode
        );

        Assert.Equal(
            PrescriptionResponseMessageDTO
                .InvalidRequest,
            result.Message
        );

        VerifyNoTransaction();
    }


    // =====================================================
    // TEST 4
    //
    // Empty request items
    //
    // Expected:
    // 400
    // InvalidRequest
    // =====================================================

    [Fact]
    public async Task CreatePrescriptionAsync_ShouldReturn400_WhenRequestItemsAreEmpty()
    {
        // Arrange

        var request =
            new PrescriptionRequestDTO
            {
                Items = []
            };


        // Act

        var result =
            await _service
                .CreatePrescriptionAsync(
                    medicalRecordId: 1,
                    currentUserId: 1,
                    request: request
                );


        // Assert

        Assert.Equal(
            400,
            result.StatusCode
        );

        Assert.Equal(
            PrescriptionResponseMessageDTO
                .InvalidRequest,
            result.Message
        );

        VerifyNoTransaction();
    }


    // =====================================================
    // TEST 5
    //
    // Invalid prescription item
    //
    // MedicineId <= 0
    //
    // Expected:
    // 400
    // InvalidRequest
    // =====================================================

    [Fact]
    public async Task CreatePrescriptionAsync_ShouldReturn400_WhenPrescriptionItemIsInvalid()
    {
        // Arrange

        var request =
            new PrescriptionRequestDTO
            {
                Items =
                [
                    new()
                    {
                        MedicineId = 0,
                        Quantity = 1,
                        Dosage = "1 lần/ngày"
                    }
                ]
            };


        // Act

        var result =
            await _service
                .CreatePrescriptionAsync(
                    medicalRecordId: 1,
                    currentUserId: 1,
                    request: request
                );


        // Assert

        Assert.Equal(
            400,
            result.StatusCode
        );

        Assert.Equal(
            PrescriptionResponseMessageDTO
                .InvalidRequest,
            result.Message
        );

        VerifyNoTransaction();
    }


    // =====================================================
    // TEST 6
    //
    // Doctor không tồn tại / không active
    //
    // Expected:
    // 403
    // MedicalRecordAccessDenied
    // =====================================================

    [Fact]
    public async Task CreatePrescriptionAsync_ShouldReturn403_WhenDoctorDoesNotExist()
    {
        // Arrange

        SetupDoctors();

        var request =
            CreateValidRequest();


        // Act

        var result =
            await _service
                .CreatePrescriptionAsync(
                    medicalRecordId: 1,
                    currentUserId: 1,
                    request: request
                );


        // Assert

        Assert.Equal(
            403,
            result.StatusCode
        );

        Assert.Equal(
            PrescriptionResponseMessageDTO
                .MedicalRecordAccessDenied,
            result.Message
        );

        VerifyNoTransaction();
    }


    // =====================================================
    // TEST 7
    //
    // MedicalRecord không tồn tại
    //
    // Doctor tồn tại hợp lệ,
    // nhưng MedicalRecord không tìm thấy.
    //
    // Expected:
    // 404
    // MedicalRecordNotFound
    // =====================================================

    [Fact]
    public async Task CreatePrescriptionAsync_ShouldReturn404_WhenMedicalRecordDoesNotExist()
    {
        // Arrange

        var doctor =
            new Doctor
            {
                Id = 1,
                UserId = 1,
                IsActive = true
            };

        SetupDoctors(
            doctor
        );

        SetupMedicalRecords();

        var request =
            CreateValidRequest();


        // Act

        var result =
            await _service
                .CreatePrescriptionAsync(
                    medicalRecordId: 1,
                    currentUserId: 1,
                    request: request
                );


        // Assert

        Assert.Equal(
            404,
            result.StatusCode
        );

        Assert.Equal(
            PrescriptionResponseMessageDTO
                .MedicalRecordNotFound,
            result.Message
        );

        VerifyNoTransaction();
    }


    // =====================================================
    // TEST 8
    //
    // MedicalRecord thuộc doctor khác
    //
    // Expected:
    // 403
    // MedicalRecordAccessDenied
    // =====================================================

    [Fact]
    public async Task CreatePrescriptionAsync_ShouldReturn403_WhenMedicalRecordBelongsToAnotherDoctor()
    {
        // Arrange

        var doctor =
            new Doctor
            {
                Id = 1,
                UserId = 1,
                IsActive = true
            };

        var medicalRecord =
            new MedicalRecordEntity
            {
                Id = 1,

                // Doctor hiện tại = Id 1
                // MedicalRecord thuộc doctor Id 2
                DoctorId = 2,

                PatientId = 1,

                Status =
                    (byte)MedicalRecordStatus.Draft
            };


        SetupDoctors(
            doctor
        );

        SetupMedicalRecords(
            medicalRecord
        );

        var request =
            CreateValidRequest();


        // Act

        var result =
            await _service
                .CreatePrescriptionAsync(
                    medicalRecordId: 1,
                    currentUserId: 1,
                    request: request
                );


        // Assert

        Assert.Equal(
            403,
            result.StatusCode
        );

        Assert.Equal(
            PrescriptionResponseMessageDTO
                .MedicalRecordAccessDenied,
            result.Message
        );

        VerifyNoTransaction();
    }


    // =====================================================
    // TEST 9
    //
    // MedicalRecord không còn ở trạng thái Draft
    //
    // Finalized / Cancelled đều đi vào cùng rule:
    // Status != Draft
    //
    // Expected:
    // 409
    // MedicalRecordNotDraft
    // =====================================================

    [Fact]
    public async Task CreatePrescriptionAsync_ShouldReturn409_WhenMedicalRecordIsNotDraft()
    {
        // Arrange

        var doctor =
            new Doctor
            {
                Id = 1,
                UserId = 1,
                IsActive = true
            };

        var medicalRecord =
            new MedicalRecordEntity
            {
                Id = 1,
                DoctorId = 1,
                PatientId = 1,

                Status =
                    (byte)MedicalRecordStatus.Finalized
            };


        SetupDoctors(
            doctor
        );

        SetupMedicalRecords(
            medicalRecord
        );

        var request =
            CreateValidRequest();


        // Act

        var result =
            await _service
                .CreatePrescriptionAsync(
                    medicalRecordId: 1,
                    currentUserId: 1,
                    request: request
                );


        // Assert

        Assert.Equal(
            409,
            result.StatusCode
        );

        Assert.Equal(
            PrescriptionResponseMessageDTO
                .MedicalRecordNotDraft,
            result.Message
        );

        VerifyNoTransaction();
    }


    // =====================================================
    // TEST 10
    //
    // MedicalRecord đã có Prescription
    //
    // Mỗi MedicalRecord chỉ có 1 Prescription.
    //
    // Expected:
    // 409
    // PrescriptionAlreadyExists
    // =====================================================

    [Fact]
    public async Task CreatePrescriptionAsync_ShouldReturn409_WhenPrescriptionAlreadyExists()
    {
        // Arrange

        var doctor =
            new Doctor
            {
                Id = 1,
                UserId = 1,
                IsActive = true
            };

        var medicalRecord =
            new MedicalRecordEntity
            {
                Id = 1,
                DoctorId = 1,
                PatientId = 1,

                Status =
                    (byte)MedicalRecordStatus.Draft
            };

        var existingPrescription =
            new Prescription
            {
                Id = 100,
                MedicalRecordId = 1,
                DoctorId = 1
            };


        SetupDoctors(
            doctor
        );

        SetupMedicalRecords(
            medicalRecord
        );

        SetupPrescriptions(
            existingPrescription
        );


        var request =
            CreateValidRequest();


        // Act

        var result =
            await _service
                .CreatePrescriptionAsync(
                    medicalRecordId: 1,
                    currentUserId: 1,
                    request: request
                );


        // Assert

        Assert.Equal(
            409,
            result.StatusCode
        );

        Assert.Equal(
            PrescriptionResponseMessageDTO
                .PrescriptionAlreadyExists,
            result.Message
        );

        VerifyNoTransaction();
    }

    // =====================================================
    // TEST 11
    //
    // Quantity <= 0
    //
    // Expected:
    // 400
    // InvalidRequest
    // =====================================================

    [Fact]
    public async Task CreatePrescriptionAsync_ShouldReturn400_WhenQuantityIsInvalid()
    {
        // Arrange

        var request =
            new PrescriptionRequestDTO
            {
                Items =
                [
                    new()
                {
                    MedicineId = 1,
                    Quantity = 0,
                    Dosage = "1 lần/ngày"
                }
                ]
            };


        // Act

        var result =
            await _service
                .CreatePrescriptionAsync(
                    medicalRecordId: 1,
                    currentUserId: 1,
                    request: request
                );


        // Assert

        Assert.Equal(
            400,
            result.StatusCode
        );

        Assert.Equal(
            PrescriptionResponseMessageDTO
                .InvalidRequest,
            result.Message
        );

        VerifyNoTransaction();
    }

    // =====================================================
    // TEST 12
    //
    // Dosage rỗng
    //
    // Expected:
    // 400
    // InvalidRequest
    // =====================================================

    [Fact]
    public async Task CreatePrescriptionAsync_ShouldReturn400_WhenDosageIsEmpty()
    {
        // Arrange

        var request =
            new PrescriptionRequestDTO
            {
                Items =
                [
                    new()
                {
                    MedicineId = 1,
                    Quantity = 1,
                    Dosage = ""
                }
                ]
            };


        // Act

        var result =
            await _service
                .CreatePrescriptionAsync(
                    medicalRecordId: 1,
                    currentUserId: 1,
                    request: request
                );


        // Assert

        Assert.Equal(
            400,
            result.StatusCode
        );

        Assert.Equal(
            PrescriptionResponseMessageDTO
                .InvalidRequest,
            result.Message
        );

        VerifyNoTransaction();
    }

    // =====================================================
    // TEST 13
    //
    // DurationDays <= 0
    //
    // Expected:
    // 400
    // InvalidRequest
    // =====================================================

    [Fact]
    public async Task CreatePrescriptionAsync_ShouldReturn400_WhenDurationDaysIsInvalid()
    {
        // Arrange

        var request =
            new PrescriptionRequestDTO
            {
                Items =
                [
                    new()
                {
                    MedicineId = 1,
                    Quantity = 1,
                    Dosage = "1 lần/ngày",
                    DurationDays = 0
                }
                ]
            };


        // Act

        var result =
            await _service
                .CreatePrescriptionAsync(
                    medicalRecordId: 1,
                    currentUserId: 1,
                    request: request
                );


        // Assert

        Assert.Equal(
            400,
            result.StatusCode
        );

        Assert.Equal(
            PrescriptionResponseMessageDTO
                .InvalidRequest,
            result.Message
        );

        VerifyNoTransaction();
    }

    // =====================================================
    // TEST 14
    //
    // Medicine không tồn tại.
    //
    // Expected:
    // 404
    // MedicineNotFound
    // =====================================================

    [Fact]
    public async Task CreatePrescriptionAsync_ShouldReturn404_WhenMedicineDoesNotExist()
    {
        // Arrange

        SetupValidPrescriptionContext();

        SetupMedicines();

        var request =
            CreateValidRequest();


        // Act

        var result =
            await _service
                .CreatePrescriptionAsync(
                    medicalRecordId: 1,
                    currentUserId: 1,
                    request: request
                );


        // Assert

        Assert.Equal(
            404,
            result.StatusCode
        );

        Assert.Equal(
            PrescriptionResponseMessageDTO
                .MedicineNotFound,
            result.Message
        );

        VerifyNoTransaction();
    }

    // =====================================================
    // TEST 15
    //
    // Medicine tồn tại nhưng inactive.
    //
    // Expected:
    // 409
    // MedicineInactive
    // =====================================================

    [Fact]
    public async Task CreatePrescriptionAsync_ShouldReturn409_WhenMedicineIsInactive()
    {
        // Arrange

        SetupValidPrescriptionContext();

        SetupMedicines(
            new Medicine
            {
                Id = 1,
                Name = "Test Medicine",
                Price = 100,
                StockQuantity = 10,
                IsActive = false
            }
        );

        var request =
            CreateValidRequest();


        // Act

        var result =
            await _service
                .CreatePrescriptionAsync(
                    medicalRecordId: 1,
                    currentUserId: 1,
                    request: request
                );


        // Assert

        Assert.Equal(
            409,
            result.StatusCode
        );

        Assert.Equal(
            PrescriptionResponseMessageDTO
                .MedicineInactive,
            result.Message
        );

        VerifyNoTransaction();
    }

    // =====================================================
    // TEST 16
    //
    // Patient dị ứng với ActiveIngredient.
    // Request chưa AllergyConfirmed.
    //
    // Expected:
    // 409
    // AllergyConfirmationRequired
    // =====================================================

    [Fact]
    public async Task CreatePrescriptionAsync_ShouldReturn409_WhenAllergyConfirmationIsRequired()
    {
        // Arrange

        SetupValidPrescriptionContext();

        SetupMedicines(
            new Medicine
            {
                Id = 1,
                Name = "Test Medicine",
                Price = 100,
                StockQuantity = 10,
                IsActive = true,
                ActiveIngredient = "Paracetamol"
            }
        );

        SetupPatientAllergies(
            new PatientAllergy
            {
                PatientId = 1,
                Allergen = "Paracetamol"
            }
        );

        var request =
            CreateValidRequest();


        // Act

        var result =
            await _service
                .CreatePrescriptionAsync(
                    medicalRecordId: 1,
                    currentUserId: 1,
                    request: request
                );


        // Assert

        Assert.Equal(
            409,
            result.StatusCode
        );

        Assert.Equal(
            PrescriptionResponseMessageDTO
                .AllergyConfirmationRequired,
            result.Message
        );

        VerifyNoTransaction();
    }

    // =====================================================
    // TEST 17
    //
    // Có batch còn quantity nhưng tất cả đều expired.
    //
    // Expected:
    // 409
    // MedicineExpired
    // =====================================================

    [Fact]
    public async Task CreatePrescriptionAsync_ShouldReturn409_WhenMedicineIsExpired()
    {
        // Arrange

        SetupValidPrescriptionContext();

        SetupMedicines(
            new Medicine
            {
                Id = 1,
                Name = "Test Medicine",
                Price = 100,
                StockQuantity = 10,
                IsActive = true
            }
        );

        SetupPatientAllergies();

        SetupMedicineBatches(
            new MedicineBatch
            {
                Id = 1,
                MedicineId = 1,
                BatchNo = "B001",
                ExpiryDate =
                    DateOnly.FromDateTime(
                        DateTime.Now.AddDays(-1)
                    ),
                Quantity = 10,
                ImportPrice = 50
            }
        );

        var request =
            CreateValidRequest();


        // Act

        var result =
            await _service
                .CreatePrescriptionAsync(
                    medicalRecordId: 1,
                    currentUserId: 1,
                    request: request
                );


        // Assert

        Assert.Equal(
            409,
            result.StatusCode
        );

        Assert.Equal(
            PrescriptionResponseMessageDTO
                .MedicineExpired,
            result.Message
        );

        VerifyNoTransaction();
    }

    // =====================================================
    // TEST 18
    //
    // Batch còn hạn nhưng tổng quantity không đủ.
    //
    // Expected:
    // 409
    // MedicineOutOfStock
    // =====================================================

    [Fact]
    public async Task CreatePrescriptionAsync_ShouldReturn409_WhenMedicineIsOutOfStock()
    {
        // Arrange

        SetupValidPrescriptionContext();

        SetupMedicines(
            new Medicine
            {
                Id = 1,
                Name = "Test Medicine",
                Price = 100,
                StockQuantity = 2,
                IsActive = true
            }
        );

        SetupPatientAllergies();

        SetupMedicineBatches(
            new MedicineBatch
            {
                Id = 1,
                MedicineId = 1,
                BatchNo = "B001",
                ExpiryDate =
                    DateOnly.FromDateTime(
                        DateTime.Now.AddDays(30)
                    ),
                Quantity = 2,
                ImportPrice = 50
            }
        );

        var request =
            CreateValidRequest();

        request.Items[0].Quantity = 5;


        // Act

        var result =
            await _service
                .CreatePrescriptionAsync(
                    medicalRecordId: 1,
                    currentUserId: 1,
                    request: request
                );


        // Assert

        Assert.Equal(
            409,
            result.StatusCode
        );

        Assert.Equal(
            PrescriptionResponseMessageDTO
                .MedicineOutOfStock,
            result.Message
        );

        VerifyNoTransaction();
    }

    // =====================================================
    // TEST 19
    //
    // Create Prescription thành công.
    //
    // Expected:
    // 200
    // PrescriptionCreateSuccess
    // BeginTransaction
    // SaveChanges
    // Commit
    // =====================================================

    [Fact]
    public async Task CreatePrescriptionAsync_ShouldCreatePrescriptionSuccessfully()
    {
        // Arrange

        SetupValidPrescriptionContext();

        SetupMedicines(
            new Medicine
            {
                Id = 1,
                Name = "Test Medicine",
                Price = 100,
                StockQuantity = 10,
                IsActive = true,
                ActiveIngredient = null
            }
        );

        SetupPatientAllergies();

        SetupMedicineBatches(
            new MedicineBatch
            {
                Id = 1,
                MedicineId = 1,
                BatchNo = "B001",
                ExpiryDate =
                    DateOnly.FromDateTime(
                        DateTime.Now.AddDays(30)
                    ),
                Quantity = 10,
                ImportPrice = 50
            }
        );

        SetupPrescriptionItemsRepository();

        var request =
            CreateValidRequest();

        request.Items[0].Quantity = 2;

        request.Note =
            "Test prescription";


        // Act

        var result =
            await _service
                .CreatePrescriptionAsync(
                    medicalRecordId: 1,
                    currentUserId: 1,
                    request: request
                );


        // Assert

        Assert.Equal(
            200,
            result.StatusCode
        );

        Assert.Equal(
            PrescriptionResponseMessageDTO
                .PrescriptionCreateSuccess,
            result.Message
        );

        Assert.NotNull(
            result.Content
        );

        Assert.Equal(
            1,
            result.Content!.MedicalRecordId
        );

        Assert.Equal(
            "Test prescription",
            result.Content.Note
        );

        Assert.Single(
            result.Content.Items
        );

        Assert.Equal(
            "Test Medicine",
            result.Content.Items[0].MedicineName
        );

        Assert.Equal(
            2,
            result.Content.Items[0].Quantity
        );


        // Transaction flow

        _unitOfWorkMock.Verify(
            x =>
                x.BeginTransactionAsync(),
            Times.Once
        );

        _unitOfWorkMock.Verify(
            x =>
                x.SaveChangesAsync(),
            Times.Once
        );

        _unitOfWorkMock.Verify(
            x =>
                x.CommitTransactionAsync(),
            Times.Once
        );

        _unitOfWorkMock.Verify(
            x =>
                x.RollbackTransactionAsync(),
            Times.Never
        );
    }

    // =====================================================
    // TEST 20
    //
    // Exception xảy ra sau BeginTransaction.
    //
    // Expected:
    // 500
    // PrescriptionCreateFailed
    // Rollback
    // Không Commit
    // =====================================================

    [Fact]
    public async Task CreatePrescriptionAsync_ShouldRollbackAndReturn500_WhenExceptionOccurs()
    {
        // Arrange

        SetupValidPrescriptionContext();

        SetupMedicines(
            new Medicine
            {
                Id = 1,
                Name = "Test Medicine",
                Price = 100,
                StockQuantity = 10,
                IsActive = true
            }
        );

        SetupPatientAllergies();

        SetupMedicineBatches(
            new MedicineBatch
            {
                Id = 1,
                MedicineId = 1,
                BatchNo = "B001",
                ExpiryDate =
                    DateOnly.FromDateTime(
                        DateTime.Now.AddDays(30)
                    ),
                Quantity = 10,
                ImportPrice = 50
            }
        );

        SetupPrescriptionItemsRepository();

        // Cố tình throw tại AddAsync()
        // sau khi transaction đã bắt đầu.
        SetupPrescriptionRepositoryThrowOnAdd();

        var request =
            CreateValidRequest();


        // Act

        var result =
            await _service
                .CreatePrescriptionAsync(
                    medicalRecordId: 1,
                    currentUserId: 1,
                    request: request
                );


        // Assert

        Assert.Equal(
            500,
            result.StatusCode
        );

        Assert.Equal(
            PrescriptionResponseMessageDTO
                .PrescriptionCreateFailed,
            result.Message
        );


        // Transaction đã bắt đầu

        _unitOfWorkMock.Verify(
            x =>
                x.BeginTransactionAsync(),
            Times.Once
        );


        // Exception -> rollback

        _unitOfWorkMock.Verify(
            x =>
                x.RollbackTransactionAsync(),
            Times.Once
        );


        // Không commit

        _unitOfWorkMock.Verify(
            x =>
                x.CommitTransactionAsync(),
            Times.Never
        );


        // Exception xảy ra trước SaveChanges

        _unitOfWorkMock.Verify(
            x =>
                x.SaveChangesAsync(),
            Times.Never
        );
    }

    // =====================================================
    // TEST 21
    //
    // medicalRecordId <= 0
    //
    // Expected:
    // 400
    // MedicalRecordNotFound
    // =====================================================

    [Fact]
    public async Task AddMedicalRecordServiceAsync_ShouldReturn400_WhenMedicalRecordIdIsInvalid()
    {
        var request =
            CreateValidMedicalRecordServiceRequest();

        var result =
            await _service
                .AddMedicalRecordServiceAsync(
                    medicalRecordId: 0,
                    currentUserId: 1,
                    request: request
                );

        Assert.Equal(
            400,
            result.StatusCode
        );

        Assert.Equal(
            MedicalRecordResponseMessageDTO
                .MedicalRecordNotFound,
            result.Message
        );

        VerifyNoTransaction();
    }

    // =====================================================
    // TEST 22
    //
    // currentUserId <= 0
    //
    // Expected:
    // 401
    // MedicalRecordAccessDenied
    // =====================================================

    [Fact]
    public async Task AddMedicalRecordServiceAsync_ShouldReturn401_WhenCurrentUserIdIsInvalid()
    {
        var request =
            CreateValidMedicalRecordServiceRequest();

        var result =
            await _service
                .AddMedicalRecordServiceAsync(
                    medicalRecordId: 1,
                    currentUserId: 0,
                    request: request
                );

        Assert.Equal(
            401,
            result.StatusCode
        );

        Assert.Equal(
            MedicalRecordResponseMessageDTO
                .MedicalRecordAccessDenied,
            result.Message
        );

        VerifyNoTransaction();
    }

    // =====================================================
    // TEST 23
    //
    // request == null
    //
    // Expected:
    // 400
    // InvalidMedicalRecordServiceRequest
    // =====================================================

    [Fact]
    public async Task AddMedicalRecordServiceAsync_ShouldReturn400_WhenRequestIsNull()
    {
        var result =
            await _service
                .AddMedicalRecordServiceAsync(
                    medicalRecordId: 1,
                    currentUserId: 1,
                    request: null!
                );

        Assert.Equal(
            400,
            result.StatusCode
        );

        Assert.Equal(
            MedicalRecordResponseMessageDTO
                .InvalidMedicalRecordServiceRequest,
            result.Message
        );

        VerifyNoTransaction();
    }

    // =====================================================
    // TEST 24
    //
    // ServiceId <= 0
    //
    // Expected:
    // 400
    // ServiceNotFound
    // =====================================================

    [Fact]
    public async Task AddMedicalRecordServiceAsync_ShouldReturn400_WhenServiceIdIsInvalid()
    {
        var request =
            new MedicalRecordServiceRequestDTO
            {
                ServiceId = 0,
                Quantity = 1
            };

        var result =
            await _service
                .AddMedicalRecordServiceAsync(
                    medicalRecordId: 1,
                    currentUserId: 1,
                    request: request
                );

        Assert.Equal(
            400,
            result.StatusCode
        );

        Assert.Equal(
            MedicalRecordResponseMessageDTO
                .ServiceNotFound,
            result.Message
        );

        VerifyNoTransaction();
    }

    // =====================================================
    // TEST 25
    //
    // Quantity <= 0
    //
    // Expected:
    // 400
    // InvalidServiceQuantity
    // =====================================================

    [Fact]
    public async Task AddMedicalRecordServiceAsync_ShouldReturn400_WhenQuantityIsInvalid()
    {
        var request =
            new MedicalRecordServiceRequestDTO
            {
                ServiceId = 1,
                Quantity = 0
            };

        var result =
            await _service
                .AddMedicalRecordServiceAsync(
                    medicalRecordId: 1,
                    currentUserId: 1,
                    request: request
                );

        Assert.Equal(
            400,
            result.StatusCode
        );

        Assert.Equal(
            MedicalRecordResponseMessageDTO
                .InvalidServiceQuantity,
            result.Message
        );

        VerifyNoTransaction();
    }

    // =====================================================
    // TEST 26
    //
    // Doctor không tồn tại / inactive
    //
    // Expected:
    // 403
    // MedicalRecordAccessDenied
    // =====================================================

    [Fact]
    public async Task AddMedicalRecordServiceAsync_ShouldReturn403_WhenDoctorDoesNotExist()
    {
        SetupDoctors();

        var request =
            CreateValidMedicalRecordServiceRequest();

        var result =
            await _service
                .AddMedicalRecordServiceAsync(
                    medicalRecordId: 1,
                    currentUserId: 1,
                    request: request
                );

        Assert.Equal(
            403,
            result.StatusCode
        );

        Assert.Equal(
            MedicalRecordResponseMessageDTO
                .MedicalRecordAccessDenied,
            result.Message
        );

        VerifyNoTransaction();
    }

    // =====================================================
    // TEST 27
    //
    // Doctor hợp lệ nhưng MedicalRecord không tồn tại.
    //
    // Expected:
    // 404
    // MedicalRecordNotFound
    // =====================================================

    [Fact]
    public async Task AddMedicalRecordServiceAsync_ShouldReturn404_WhenMedicalRecordDoesNotExist()
    {
        SetupDoctors(
            new Doctor
            {
                Id = 1,
                UserId = 1,
                IsActive = true
            }
        );

        SetupMedicalRecords();

        var request =
            CreateValidMedicalRecordServiceRequest();

        var result =
            await _service
                .AddMedicalRecordServiceAsync(
                    medicalRecordId: 1,
                    currentUserId: 1,
                    request: request
                );

        Assert.Equal(
            404,
            result.StatusCode
        );

        Assert.Equal(
            MedicalRecordResponseMessageDTO
                .MedicalRecordNotFound,
            result.Message
        );

        VerifyNoTransaction();
    }

    // =====================================================
    // TEST 28
    //
    // MedicalRecord.DoctorId != current doctor.Id
    //
    // Expected:
    // 403
    // MedicalRecordAccessDenied
    // =====================================================

    [Fact]
    public async Task AddMedicalRecordServiceAsync_ShouldReturn403_WhenMedicalRecordBelongsToAnotherDoctor()
    {
        SetupDoctors(
            new Doctor
            {
                Id = 1,
                UserId = 1,
                IsActive = true
            }
        );

        SetupMedicalRecords(
            new MedicalRecordEntity
            {
                Id = 1,
                DoctorId = 2,
                PatientId = 1,
                Status =
                    (byte)MedicalRecordStatus.Draft
            }
        );

        var request =
            CreateValidMedicalRecordServiceRequest();

        var result =
            await _service
                .AddMedicalRecordServiceAsync(
                    medicalRecordId: 1,
                    currentUserId: 1,
                    request: request
                );

        Assert.Equal(
            403,
            result.StatusCode
        );

        Assert.Equal(
            MedicalRecordResponseMessageDTO
                .MedicalRecordAccessDenied,
            result.Message
        );

        VerifyNoTransaction();
    }

    // =====================================================
    // TEST 29
    //
    // MedicalRecord đã Finalized.
    //
    // Expected:
    // 409
    // MedicalRecordNotDraft
    // =====================================================

    [Fact]
    public async Task AddMedicalRecordServiceAsync_ShouldReturn409_WhenMedicalRecordIsNotDraft()
    {
        SetupDoctors(
            new Doctor
            {
                Id = 1,
                UserId = 1,
                IsActive = true
            }
        );

        SetupMedicalRecords(
            new MedicalRecordEntity
            {
                Id = 1,
                DoctorId = 1,
                PatientId = 1,
                Status =
                    (byte)MedicalRecordStatus.Finalized
            }
        );

        var request =
            CreateValidMedicalRecordServiceRequest();

        var result =
            await _service
                .AddMedicalRecordServiceAsync(
                    medicalRecordId: 1,
                    currentUserId: 1,
                    request: request
                );

        Assert.Equal(
            409,
            result.StatusCode
        );

        Assert.Equal(
            MedicalRecordResponseMessageDTO
                .MedicalRecordNotDraft,
            result.Message
        );

        VerifyNoTransaction();
    }

    // =====================================================
    // TEST 30
    //
    // MedicalRecord hợp lệ nhưng Service không tồn tại.
    //
    // Expected:
    // 404
    // ServiceNotFound
    // =====================================================

    [Fact]
    public async Task AddMedicalRecordServiceAsync_ShouldReturn404_WhenServiceDoesNotExist()
    {
        SetupDoctors(
            new Doctor
            {
                Id = 1,
                UserId = 1,
                IsActive = true
            }
        );

        SetupMedicalRecords(
            new MedicalRecordEntity
            {
                Id = 1,
                DoctorId = 1,
                PatientId = 1,
                Status =
                    (byte)MedicalRecordStatus.Draft
            }
        );

        SetupServices();

        var request =
            CreateValidMedicalRecordServiceRequest();

        var result =
            await _service
                .AddMedicalRecordServiceAsync(
                    medicalRecordId: 1,
                    currentUserId: 1,
                    request: request
                );

        Assert.Equal(
            404,
            result.StatusCode
        );

        Assert.Equal(
            MedicalRecordResponseMessageDTO
                .ServiceNotFound,
            result.Message
        );

        VerifyNoTransaction();
    }

    // =====================================================
// TEST 31
//
// medicalRecordServiceId <= 0
//
// Expected:
// 400
// MedicalRecordServiceNotFound
// =====================================================

[Fact]
public async Task CancelMedicalRecordServiceAsync_ShouldReturn400_WhenIdIsInvalid()
{
    var result =
        await _service
            .CancelMedicalRecordServiceAsync(
                medicalRecordServiceId: 0,
                currentUserId: 1
            );

    Assert.Equal(
        400,
        result.StatusCode
    );

    Assert.Equal(
        MedicalRecordResponseMessageDTO
            .MedicalRecordServiceNotFound,
        result.Message
    );

    VerifyNoTransaction();
}

// =====================================================
// TEST 32
//
// currentUserId <= 0
//
// Expected:
// 401
// MedicalRecordAccessDenied
// =====================================================

[Fact]
public async Task CancelMedicalRecordServiceAsync_ShouldReturn401_WhenCurrentUserIdIsInvalid()
{
    var result =
        await _service
            .CancelMedicalRecordServiceAsync(
                medicalRecordServiceId: 1,
                currentUserId: 0
            );

    Assert.Equal(
        401,
        result.StatusCode
    );

    Assert.Equal(
        MedicalRecordResponseMessageDTO
            .MedicalRecordAccessDenied,
        result.Message
    );

    VerifyNoTransaction();
}

// =====================================================
// TEST 33
//
// Doctor không tồn tại / inactive
//
// Expected:
// 403
// MedicalRecordAccessDenied
// =====================================================

[Fact]
public async Task CancelMedicalRecordServiceAsync_ShouldReturn403_WhenDoctorDoesNotExist()
{
    SetupDoctors();

    var result =
        await _service
            .CancelMedicalRecordServiceAsync(
                medicalRecordServiceId: 1,
                currentUserId: 1
            );

    Assert.Equal(
        403,
        result.StatusCode
    );

    Assert.Equal(
        MedicalRecordResponseMessageDTO
            .MedicalRecordAccessDenied,
        result.Message
    );

    VerifyNoTransaction();
}

// =====================================================
// TEST 34
//
// Doctor hợp lệ nhưng service không tồn tại.
//
// Expected:
// 404
// MedicalRecordServiceNotFound
// =====================================================

[Fact]
public async Task CancelMedicalRecordServiceAsync_ShouldReturn404_WhenMedicalRecordServiceDoesNotExist()
{
    SetupDoctors(
        new Doctor
        {
            Id = 1,
            UserId = 1,
            IsActive = true
        }
    );

    SetupMedicalRecordServices();

    var result =
        await _service
            .CancelMedicalRecordServiceAsync(
                medicalRecordServiceId: 1,
                currentUserId: 1
            );

    Assert.Equal(
        404,
        result.StatusCode
    );

    Assert.Equal(
        MedicalRecordResponseMessageDTO
            .MedicalRecordServiceNotFound,
        result.Message
    );

    VerifyNoTransaction();
}

// =====================================================
// TEST 35
//
// MedicalRecordService tồn tại
// nhưng MedicalRecord không tồn tại.
//
// Expected:
// 404
// MedicalRecordNotFound
// =====================================================

[Fact]
public async Task CancelMedicalRecordServiceAsync_ShouldReturn404_WhenMedicalRecordDoesNotExist()
{
    SetupDoctors(
        new Doctor
        {
            Id = 1,
            UserId = 1,
            IsActive = true
        }
    );

    var medicalRecordService =
        new MedicalRecordServiceEntity
        {
            Id = 1,
            MedicalRecordId = 999,
            ServiceId = 1,
            Quantity = 1,
            UnitPriceSnapshot = 100,
            Status =
                (byte)MedicalRecordServiceStatus.Ordered,

            Service =
                new Service
                {
                    Id = 1,
                    Name = "Test Service",
                    Price = 100
                }
        };

    SetupMedicalRecordServices(
        medicalRecordService
    );

    SetupMedicalRecords();

    var result =
        await _service
            .CancelMedicalRecordServiceAsync(
                medicalRecordServiceId: 1,
                currentUserId: 1
            );

    Assert.Equal(
        404,
        result.StatusCode
    );

    Assert.Equal(
        MedicalRecordResponseMessageDTO
            .MedicalRecordNotFound,
        result.Message
    );

    VerifyNoTransaction();
}

// =====================================================
// TEST 36
//
// Service đã Completed.
//
// Expected:
// 409
// MedicalRecordServiceNotOrdered
// =====================================================

[Fact]
public async Task CancelMedicalRecordServiceAsync_ShouldReturn409_WhenServiceIsNotOrdered()
{
    SetupDoctors(
        new Doctor
        {
            Id = 1,
            UserId = 1,
            IsActive = true
        }
    );

    var medicalRecord =
        new MedicalRecordEntity
        {
            Id = 1,
            DoctorId = 1,
            PatientId = 1,
            Status =
                (byte)MedicalRecordStatus.Draft
        };

    SetupMedicalRecords(
        medicalRecord
    );

    var medicalRecordService =
        new MedicalRecordServiceEntity
        {
            Id = 1,
            MedicalRecordId = 1,
            ServiceId = 1,
            Quantity = 1,
            UnitPriceSnapshot = 100,

            Status =
                (byte)MedicalRecordServiceStatus.Completed,

            Service =
                new Service
                {
                    Id = 1,
                    Name = "Test Service",
                    Price = 100
                }
        };

    SetupMedicalRecordServices(
        medicalRecordService
    );

    var result =
        await _service
            .CancelMedicalRecordServiceAsync(
                medicalRecordServiceId: 1,
                currentUserId: 1
            );

    Assert.Equal(
        409,
        result.StatusCode
    );

    Assert.Equal(
        MedicalRecordResponseMessageDTO
            .MedicalRecordServiceNotOrdered,
        result.Message
    );

    VerifyNoTransaction();
}

// =====================================================
// TEST 37
//
// Ordered + Draft + đúng Doctor
//
// Expected:
// 200
// CancelServiceSuccess
// Status -> Cancelled
// SaveChanges -> Once
// =====================================================

[Fact]
public async Task CancelMedicalRecordServiceAsync_ShouldCancelSuccessfully()
{
    SetupDoctors(
        new Doctor
        {
            Id = 1,
            UserId = 1,
            IsActive = true
        }
    );

    SetupMedicalRecords(
        new MedicalRecordEntity
        {
            Id = 1,
            DoctorId = 1,
            PatientId = 1,
            Status =
                (byte)MedicalRecordStatus.Draft
        }
    );

    var medicalRecordService =
        new MedicalRecordServiceEntity
        {
            Id = 1,
            MedicalRecordId = 1,
            ServiceId = 1,
            Quantity = 2,
            UnitPriceSnapshot = 150,

            Status =
                (byte)MedicalRecordServiceStatus.Ordered,

            Service =
                new Service
                {
                    Id = 1,
                    Name = "Xét nghiệm máu",
                    Price = 150
                }
        };

    SetupMedicalRecordServices(
        medicalRecordService
    );

    var result =
        await _service
            .CancelMedicalRecordServiceAsync(
                medicalRecordServiceId: 1,
                currentUserId: 1
            );

    Assert.Equal(
        200,
        result.StatusCode
    );

    Assert.Equal(
        MedicalRecordResponseMessageDTO
            .CancelServiceSuccess,
        result.Message
    );

    Assert.NotNull(
        result.Content
    );

    Assert.Equal(
        (byte)MedicalRecordServiceStatus.Cancelled,
        result.Content!.Status
    );

    Assert.Equal(
        "Xét nghiệm máu",
        result.Content.ServiceName
    );

    Assert.Equal(
        2,
        result.Content.Quantity
    );

    _unitOfWorkMock.Verify(
        x =>
            x.SaveChangesAsync(),
        Times.Once
    );

    _unitOfWorkMock.Verify(
        x =>
            x.BeginTransactionAsync(),
        Times.Never
    );
}

// =====================================================
// TEST 38
//
// Add service thành công.
//
// Expected:
// 201
// AddServiceSuccess
// Status = Ordered
// UnitPriceSnapshot = Service.Price
// SaveChanges = Once
// =====================================================

[Fact]
public async Task AddMedicalRecordServiceAsync_ShouldCreateServiceSuccessfully()
{
    SetupDoctors(
        new Doctor
        {
            Id = 1,
            UserId = 1,
            IsActive = true
        }
    );

    SetupMedicalRecords(
        new MedicalRecordEntity
        {
            Id = 1,
            DoctorId = 1,
            PatientId = 1,
            Status =
                (byte)MedicalRecordStatus.Draft
        }
    );

    SetupServices(
        new Service
        {
            Id = 1,
            Name = "Xét nghiệm máu",
            Price = 250
        }
    );

    var repositoryMock =
        new Mock<IMedicalRecordServiceRepository>();

    MedicalRecordServiceEntity? addedEntity = null;

    repositoryMock
        .Setup(
            x =>
                x.AddAsync(
                    It.IsAny<MedicalRecordServiceEntity>()
                )
        )
        .Callback(
            (MedicalRecordServiceEntity entity) =>
            {
                addedEntity = entity;
                entity.Id = 10;
            }
        )
        .Returns(
            Task.CompletedTask
        );

    _unitOfWorkMock
        .Setup(
            x =>
                x.MedicalRecordServiceRepository
        )
        .Returns(
            repositoryMock.Object
        );

    var request =
        new MedicalRecordServiceRequestDTO
        {
            ServiceId = 1,
            Quantity = 3
        };

    var result =
        await _service
            .AddMedicalRecordServiceAsync(
                medicalRecordId: 1,
                currentUserId: 1,
                request: request
            );

    Assert.Equal(
        201,
        result.StatusCode
    );

    Assert.Equal(
        MedicalRecordResponseMessageDTO
            .AddServiceSuccess,
        result.Message
    );

    Assert.NotNull(
        result.Content
    );

    Assert.NotNull(
        addedEntity
    );

    Assert.Equal(
        1,
        addedEntity!.MedicalRecordId
    );

    Assert.Equal(
        1,
        addedEntity.ServiceId
    );

    Assert.Equal(
        3,
        addedEntity.Quantity
    );

    Assert.Equal(
        250,
        addedEntity.UnitPriceSnapshot
    );

    Assert.Equal(
        (byte)MedicalRecordServiceStatus.Ordered,
        addedEntity.Status
    );

    Assert.Equal(
        10,
        result.Content!.Id
    );

    Assert.Equal(
        "Xét nghiệm máu",
        result.Content.ServiceName
    );

    Assert.Equal(
        250,
        result.Content.UnitPriceSnapshot
    );

    _unitOfWorkMock.Verify(
        x =>
            x.SaveChangesAsync(),
        Times.Once
    );

    _unitOfWorkMock.Verify(
        x =>
            x.BeginTransactionAsync(),
        Times.Never
    );
}

// =====================================================
// TEST 39
//
// Exception xảy ra khi AddAsync.
//
// Expected:
// 500
// AddServiceFailed
// Không SaveChanges
// =====================================================

[Fact]
public async Task AddMedicalRecordServiceAsync_ShouldReturn500_WhenAddFails()
{
    SetupDoctors(
        new Doctor
        {
            Id = 1,
            UserId = 1,
            IsActive = true
        }
    );

    SetupMedicalRecords(
        new MedicalRecordEntity
        {
            Id = 1,
            DoctorId = 1,
            PatientId = 1,
            Status =
                (byte)MedicalRecordStatus.Draft
        }
    );

    SetupServices(
        new Service
        {
            Id = 1,
            Name = "Xét nghiệm máu",
            Price = 250
        }
    );

    var repositoryMock =
        new Mock<IMedicalRecordServiceRepository>();

    repositoryMock
        .Setup(
            x =>
                x.AddAsync(
                    It.IsAny<MedicalRecordServiceEntity>()
                )
        )
        .ThrowsAsync(
            new InvalidOperationException(
                "Test exception"
            )
        );

    _unitOfWorkMock
        .Setup(
            x =>
                x.MedicalRecordServiceRepository
        )
        .Returns(
            repositoryMock.Object
        );

    var request =
        CreateValidMedicalRecordServiceRequest();

    var result =
        await _service
            .AddMedicalRecordServiceAsync(
                medicalRecordId: 1,
                currentUserId: 1,
                request: request
            );

    Assert.Equal(
        500,
        result.StatusCode
    );

    Assert.Equal(
        MedicalRecordResponseMessageDTO
            .AddServiceFailed,
        result.Message
    );

    _unitOfWorkMock.Verify(
        x =>
            x.SaveChangesAsync(),
        Times.Never
    );
}

// =====================================================
// TEST 40
//
// Exception khi query MedicalRecordService.
//
// Expected:
// 500
// CancelServiceFailed
// =====================================================

[Fact]
public async Task CancelMedicalRecordServiceAsync_ShouldReturn500_WhenExceptionOccurs()
{
    SetupDoctors(
        new Doctor
        {
            Id = 1,
            UserId = 1,
            IsActive = true
        }
    );

    var repositoryMock =
        new Mock<IMedicalRecordServiceRepository>();

    repositoryMock
        .Setup(
            x =>
                x.WhereSql(
                    It.IsAny<
                        Expression<
                            Func<
                                MedicalRecordServiceEntity,
                                bool
                            >
                        >
                    >()
                )
        )
        .Throws(
            new InvalidOperationException(
                "Test exception"
            )
        );

    _unitOfWorkMock
        .Setup(
            x =>
                x.MedicalRecordServiceRepository
        )
        .Returns(
            repositoryMock.Object
        );

    var result =
        await _service
            .CancelMedicalRecordServiceAsync(
                medicalRecordServiceId: 1,
                currentUserId: 1
            );

    Assert.Equal(
        500,
        result.StatusCode
    );

    Assert.Equal(
        MedicalRecordResponseMessageDTO
            .CancelServiceFailed,
        result.Message
    );

    _unitOfWorkMock.Verify(
        x =>
            x.SaveChangesAsync(),
        Times.Never
    );

    _unitOfWorkMock.Verify(
        x =>
            x.BeginTransactionAsync(),
        Times.Never
    );
}










    // Helper

    private void SetupServices(
    params Service[] services)
    {
        var repositoryMock =
            new Mock<IServiceRepository>();

        repositoryMock
            .Setup(
                x =>
                    x.WhereSql(
                        It.IsAny<
                            Expression<
                                Func<
                                    Service,
                                    bool
                                >
                            >
                        >()
                    )
            )
            .Returns(
                (
                    Expression<
                        Func<
                            Service,
                            bool
                        >
                    > predicate
                ) =>
                    services
                        .Where(
                            predicate.Compile()
                        )
                        .ToList()
                        .BuildMock()
            );

        _unitOfWorkMock
            .Setup(
                x =>
                    x.ServiceRepository
            )
            .Returns(
                repositoryMock.Object
            );
    }


    private void SetupMedicalRecordServices(
        params MedicalRecordServiceEntity[] services)
    {
        var repositoryMock =
            new Mock<IMedicalRecordServiceRepository>();

        repositoryMock
            .Setup(
                x =>
                    x.WhereSql(
                        It.IsAny<
                            Expression<
                                Func<
                                    MedicalRecordServiceEntity,
                                    bool
                                >
                            >
                        >()
                    )
            )
            .Returns(
                (
                    Expression<
                        Func<
                            MedicalRecordServiceEntity,
                            bool
                        >
                    > predicate
                ) =>
                    services
                        .Where(
                            predicate.Compile()
                        )
                        .ToList()
                        .BuildMock()
            );

        _unitOfWorkMock
            .Setup(
                x =>
                    x.MedicalRecordServiceRepository
            )
            .Returns(
                repositoryMock.Object
            );
    }


    private static MedicalRecordServiceRequestDTO
        CreateValidMedicalRecordServiceRequest()
    {
        return new MedicalRecordServiceRequestDTO
        {
            ServiceId = 1,
            Quantity = 1
        };
    }

    private void SetupMedicines(
    params Medicine[] medicines)
    {
        var repositoryMock =
            new Mock<IMedicineRepository>();

        repositoryMock
            .Setup(
                x =>
                    x.WhereSql(
                        It.IsAny<
                            Expression<
                                Func<
                                    Medicine,
                                    bool
                                >
                            >
                        >()
                    )
            )
            .Returns(
                (
                    Expression<
                        Func<
                            Medicine,
                            bool
                        >
                    > predicate
                ) =>
                    medicines
                        .Where(
                            predicate.Compile()
                        )
                        .ToList()
                        .BuildMock()
            );

        _unitOfWorkMock
            .Setup(
                x =>
                    x.MedicineRepository
            )
            .Returns(
                repositoryMock.Object
            );
    }


    private void SetupPatientAllergies(
        params PatientAllergy[] allergies)
    {
        var repositoryMock =
            new Mock<IPatientAllergyRepository>();

        repositoryMock
            .Setup(
                x =>
                    x.WhereSql(
                        It.IsAny<
                            Expression<
                                Func<
                                    PatientAllergy,
                                    bool
                                >
                            >
                        >()
                    )
            )
            .Returns(
                (
                    Expression<
                        Func<
                            PatientAllergy,
                            bool
                        >
                    > predicate
                ) =>
                    allergies
                        .Where(
                            predicate.Compile()
                        )
                        .ToList()
                        .BuildMock()
            );

        _unitOfWorkMock
            .Setup(
                x =>
                    x.PatientAllergyRepository
            )
            .Returns(
                repositoryMock.Object
            );
    }


    private void SetupMedicineBatches(
        params MedicineBatch[] batches)
    {
        var repositoryMock =
            new Mock<IMedicineBatchRepository>();

        repositoryMock
            .Setup(
                x =>
                    x.WhereSql(
                        It.IsAny<
                            Expression<
                                Func<
                                    MedicineBatch,
                                    bool
                                >
                            >
                        >()
                    )
            )
            .Returns(
                (
                    Expression<
                        Func<
                            MedicineBatch,
                            bool
                        >
                    > predicate
                ) =>
                    batches
                        .Where(
                            predicate.Compile()
                        )
                        .ToList()
                        .BuildMock()
            );

        _unitOfWorkMock
            .Setup(
                x =>
                    x.MedicineBatchRepository
            )
            .Returns(
                repositoryMock.Object
            );
    }


    private void SetupPrescriptionItemsRepository()
    {
        var repositoryMock =
            new Mock<IPrescriptionItemRepository>();

        _unitOfWorkMock
            .Setup(
                x =>
                    x.PrescriptionItemRepository
            )
            .Returns(
                repositoryMock.Object
            );
    }


    private void SetupValidPrescriptionContext()
    {
        SetupDoctors(
            new Doctor
            {
                Id = 1,
                UserId = 1,
                IsActive = true
            }
        );

        SetupMedicalRecords(
            new MedicalRecordEntity
            {
                Id = 1,
                DoctorId = 1,
                PatientId = 1,
                Status =
                    (byte)MedicalRecordStatus.Draft
            }
        );

        SetupPrescriptions();
    }


    private void SetupPrescriptionRepositoryThrowOnAdd()
    {
        var repositoryMock =
            new Mock<IPrescriptionRepository>();

        repositoryMock
            .Setup(
                x =>
                    x.WhereSql(
                        It.IsAny<
                            Expression<
                                Func<
                                    Prescription,
                                    bool
                                >
                            >
                        >()
                    )
            )
            .Returns(
                (
                    Expression<
                        Func<
                            Prescription,
                            bool
                        >
                    > predicate
                ) =>
                    new List<Prescription>()
                        .BuildMock()
            );

        repositoryMock
            .Setup(
                x =>
                    x.AddAsync(
                        It.IsAny<Prescription>()
                    )
            )
            .ThrowsAsync(
                new InvalidOperationException(
                    "Test exception"
                )
            );

        _unitOfWorkMock
            .Setup(
                x =>
                    x.PrescriptionRepository
            )
            .Returns(
                repositoryMock.Object
            );
    }

}