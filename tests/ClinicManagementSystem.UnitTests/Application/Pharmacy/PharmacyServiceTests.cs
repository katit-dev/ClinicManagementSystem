using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.DTOs.Pharmacy;
using ClinicManagementSystem.Application.DTOs.Prescription;
using ClinicManagementSystem.Application.Services;
using ClinicManagementSystem.Infrastructure.UnitOfWork;

using Microsoft.Extensions.Logging.Abstractions;
using System.Linq.Expressions;

using ClinicManagementSystem.Infrastructure.Models;
using ClinicManagementSystem.Infrastructure.Repositories;

using MockQueryable;
using Moq;
using ClinicManagementSystem.Application.Enums;


namespace ClinicManagementSystem.UnitTests.Application.Pharmacy;


public class PharmacyServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;

    private readonly PharmacyService _service;


    // =====================================================
    // CONSTRUCTOR
    // =====================================================

    public PharmacyServiceTests()
    {
        _unitOfWorkMock =
            new Mock<IUnitOfWork>();


        _service =
            new PharmacyService(
                _unitOfWorkMock.Object,
                NullLogger<PharmacyService>.Instance
            );
    }


    // =====================================================
    // TEST 1
    // Invalid prescription ID
    //
    // prescriptionId <= 0
    //
    // Expected:
    // 400
    // =====================================================

    [Fact]
    public async Task DispensePrescriptionAsync_ShouldReturn400_WhenPrescriptionIdIsInvalid()
    {
        // Arrange

        var request =
            new DispenseRequestDTO
            {
                Items = []
            };


        // Act

        var result =
            await _service
                .DispensePrescriptionAsync(
                    prescriptionId: 0,
                    currentUserId: 1,
                    request: request
                );


        // Assert

        Assert.Equal(
            400,
            result.StatusCode
        );

        Assert.Equal(
            PharmacyResponseMessageDTO
                .PrescriptionIdInvalid,
            result.Message
        );
    }


    // =====================================================
    // TEST 2
    // Invalid pharmacist/user ID
    //
    // currentUserId <= 0
    //
    // Expected:
    // 401
    // =====================================================

    [Fact]
    public async Task DispensePrescriptionAsync_ShouldReturn401_WhenCurrentUserIdIsInvalid()
    {
        // Arrange

        var request =
            new DispenseRequestDTO
            {
                Items = []
            };


        // Act

        var result =
            await _service
                .DispensePrescriptionAsync(
                    prescriptionId: 1,
                    currentUserId: 0,
                    request: request
                );


        // Assert

        Assert.Equal(
            401,
            result.StatusCode
        );

        Assert.Equal(
            PharmacyResponseMessageDTO
                .DispenseUserInvalid,
            result.Message
        );
    }


    // =====================================================
    // TEST 3
    // Null request
    //
    // Expected:
    // 400
    // =====================================================

    [Fact]
    public async Task DispensePrescriptionAsync_ShouldReturn400_WhenRequestIsNull()
    {
        // Act

        var result =
            await _service
                .DispensePrescriptionAsync(
                    prescriptionId: 1,
                    currentUserId: 1,
                    request: null!
                );


        // Assert

        Assert.Equal(
            400,
            result.StatusCode
        );

        Assert.Equal(
            PharmacyResponseMessageDTO
                .DispenseRequestEmpty,
            result.Message
        );
    }


    // =====================================================
    // TEST 4
    // Empty request items
    //
    // Expected:
    // 400
    // =====================================================

    [Fact]
    public async Task DispensePrescriptionAsync_ShouldReturn400_WhenRequestItemsAreEmpty()
    {
        // Arrange

        var request =
            new DispenseRequestDTO
            {
                Items = []
            };


        // Act

        var result =
            await _service
                .DispensePrescriptionAsync(
                    prescriptionId: 1,
                    currentUserId: 1,
                    request: request
                );


        // Assert

        Assert.Equal(
            400,
            result.StatusCode
        );

        Assert.Equal(
            PharmacyResponseMessageDTO
                .DispenseRequestEmpty,
            result.Message
        );
    }

    // =====================================================
    // TEST 5
    //
    // PrescriptionItemId <= 0
    //
    // Expected:
    // 400
    // "Thông tin phát thuốc không hợp lệ."
    // =====================================================

    [Fact]
    public async Task DispensePrescriptionAsync_ShouldReturn400_WhenPrescriptionItemIdIsInvalid()
    {
        // Arrange

        var request =
            new DispenseRequestDTO
            {
                Items =
                [
                    new()
                {
                    PrescriptionItemId = 0,

                    BatchId = 1,

                    Quantity = 1
                }
                ]
            };


        // Act

        var result =
            await _service
                .DispensePrescriptionAsync(
                    prescriptionId: 1,
                    currentUserId: 1,
                    request: request
                );


        // Assert

        Assert.Equal(
            400,
            result.StatusCode
        );


        Assert.Equal(
            "Thông tin phát thuốc không hợp lệ.",
            result.Message
        );
    }

    // =====================================================
    // TEST 6
    //
    // BatchId <= 0
    //
    // Expected:
    // 400
    // "Thông tin phát thuốc không hợp lệ."
    // =====================================================

    [Fact]
    public async Task DispensePrescriptionAsync_ShouldReturn400_WhenBatchIdIsInvalid()
    {
        // Arrange

        var request =
            new DispenseRequestDTO
            {
                Items =
                [
                    new()
                {
                    PrescriptionItemId = 1,

                    BatchId = 0,

                    Quantity = 1
                }
                ]
            };


        // Act

        var result =
            await _service
                .DispensePrescriptionAsync(
                    prescriptionId: 1,
                    currentUserId: 1,
                    request: request
                );


        // Assert

        Assert.Equal(
            400,
            result.StatusCode
        );


        Assert.Equal(
            "Thông tin phát thuốc không hợp lệ.",
            result.Message
        );
    }

    // =====================================================
    // TEST 7
    //
    // Quantity <= 0
    //
    // Expected:
    // 400
    // "Thông tin phát thuốc không hợp lệ."
    // =====================================================

    [Fact]
    public async Task DispensePrescriptionAsync_ShouldReturn400_WhenQuantityIsInvalid()
    {
        // Arrange

        var request =
            new DispenseRequestDTO
            {
                Items =
                [
                    new()
                {
                    PrescriptionItemId = 1,

                    BatchId = 1,

                    Quantity = 0
                }
                ]
            };


        // Act

        var result =
            await _service
                .DispensePrescriptionAsync(
                    prescriptionId: 1,
                    currentUserId: 1,
                    request: request
                );


        // Assert

        Assert.Equal(
            400,
            result.StatusCode
        );


        Assert.Equal(
            "Thông tin phát thuốc không hợp lệ.",
            result.Message
        );
    }

    // =====================================================
    // TEST 8
    //
    // Prescription không tồn tại
    //
    // Expected:
    // 404
    // PharmacyResponseMessageDTO.PrescriptionNotFound
    // =====================================================

    [Fact]
    public async Task DispensePrescriptionAsync_ShouldReturn404_WhenPrescriptionDoesNotExist()
    {
        // Arrange

        var request =
            new DispenseRequestDTO
            {
                Items =
                [
                    new()
                {
                    PrescriptionItemId = 1,

                    BatchId = 1,

                    Quantity = 1
                }
                ]
            };


        var prescriptionRepositoryMock =
            new Mock<IPrescriptionRepository>();


        var prescriptions =
            new List<Prescription>();


        var prescriptionQuery =
            prescriptions
                .BuildMock();


        prescriptionRepositoryMock
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
                prescriptionQuery
            );


        _unitOfWorkMock
            .Setup(
                x =>
                    x.PrescriptionRepository
            )
            .Returns(
                prescriptionRepositoryMock.Object
            );


        // Act

        var result =
            await _service
                .DispensePrescriptionAsync(
                    prescriptionId: 1,
                    currentUserId: 1,
                    request: request
                );


        // Assert

        Assert.Equal(
            404,
            result.StatusCode
        );

        Assert.Equal(
            PharmacyResponseMessageDTO
                .PrescriptionNotFound,
            result.Message
        );
    }
    // =====================================================
    // TEST 9
    //
    // Prescription đã Dispensed
    //
    // Expected:
    // 409
    // PharmacyResponseMessageDTO.PrescriptionAlreadyDispensed
    // =====================================================

    [Fact]
    public async Task DispensePrescriptionAsync_ShouldReturn409_WhenPrescriptionIsAlreadyDispensed()
    {
        // Arrange

        var request =
            new DispenseRequestDTO
            {
                Items =
                [
                    new()
                {
                    PrescriptionItemId = 1,

                    BatchId = 1,

                    Quantity = 1
                }
                ]
            };


        var prescription =
            new Prescription
            {
                Id = 1,

                Status =
                    (byte)PrescriptionStatus.Dispensed
            };


        var prescriptions =
            new List<Prescription>
            {
            prescription
            };


        var prescriptionQuery =
            prescriptions
                .BuildMock();


        var prescriptionRepositoryMock =
            new Mock<IPrescriptionRepository>();


        prescriptionRepositoryMock
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
                prescriptionQuery
            );


        _unitOfWorkMock
            .Setup(
                x =>
                    x.PrescriptionRepository
            )
            .Returns(
                prescriptionRepositoryMock.Object
            );


        // Act

        var result =
            await _service
                .DispensePrescriptionAsync(
                    prescriptionId: 1,
                    currentUserId: 1,
                    request: request
                );


        // Assert

        Assert.Equal(
            409,
            result.StatusCode
        );

        Assert.Equal(
            PharmacyResponseMessageDTO
                .PrescriptionAlreadyDispensed,
            result.Message
        );
    }

    // =====================================================
    // TEST 10
    //
    // Prescription chưa Finalized
    //
    // Expected:
    // 409
    // PharmacyResponseMessageDTO.PrescriptionNotFinalized
    // =====================================================

    [Fact]
    public async Task DispensePrescriptionAsync_ShouldReturn409_WhenPrescriptionIsNotFinalized()
    {
        // Arrange

        var request =
            new DispenseRequestDTO
            {
                Items =
                [
                    new()
                {
                    PrescriptionItemId = 1,
                    BatchId = 1,
                    Quantity = 1
                }
                ]
            };


        var prescription =
            new Prescription
            {
                Id = 1,

                Status =
                    (byte)PrescriptionStatus.Draft
            };


        var prescriptions =
            new List<Prescription>
            {
            prescription
            };


        var prescriptionQuery =
            prescriptions
                .BuildMock();


        var prescriptionRepositoryMock =
            new Mock<IPrescriptionRepository>();


        prescriptionRepositoryMock
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
                prescriptionQuery
            );


        _unitOfWorkMock
            .Setup(
                x =>
                    x.PrescriptionRepository
            )
            .Returns(
                prescriptionRepositoryMock.Object
            );


        // Act

        var result =
            await _service
                .DispensePrescriptionAsync(
                    prescriptionId: 1,
                    currentUserId: 1,
                    request: request
                );


        // Assert

        Assert.Equal(
            409,
            result.StatusCode
        );

        Assert.Equal(
            PharmacyResponseMessageDTO
                .PrescriptionNotFinalized,
            result.Message
        );
    }

    // =====================================================
    // TEST 11
    //
    // Prescription không có item
    //
    // Expected:
    // 409
    // PharmacyResponseMessageDTO.PrescriptionHasNoItems
    // =====================================================

    [Fact]
    public async Task DispensePrescriptionAsync_ShouldReturn409_WhenPrescriptionHasNoItems()
    {
        // Arrange

        var request =
            new DispenseRequestDTO
            {
                Items =
                [
                    new()
                {
                    PrescriptionItemId = 1,
                    BatchId = 1,
                    Quantity = 1
                }
                ]
            };


        var prescription =
            new Prescription
            {
                Id = 1,

                Status =
                    (byte)PrescriptionStatus.Finalized
            };


        var prescriptions =
            new List<Prescription>
            {
            prescription
            };


        var prescriptionQuery =
            prescriptions
                .BuildMock();


        var prescriptionRepositoryMock =
            new Mock<IPrescriptionRepository>();


        prescriptionRepositoryMock
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
                prescriptionQuery
            );


        _unitOfWorkMock
            .Setup(
                x =>
                    x.PrescriptionRepository
            )
            .Returns(
                prescriptionRepositoryMock.Object
            );


        // =====================================================
        // PRESCRIPTION ITEM REPOSITORY
        //
        // Không có prescription item
        // =====================================================

        var prescriptionItems =
            new List<PrescriptionItem>();


        var prescriptionItemQuery =
            prescriptionItems
                .BuildMock();


        var prescriptionItemRepositoryMock =
            new Mock<IPrescriptionItemRepository>();


        prescriptionItemRepositoryMock
            .Setup(
                x =>
                    x.WhereSql(
                        It.IsAny<
                            Expression<
                                Func<
                                    PrescriptionItem,
                                    bool
                                >
                            >
                        >()
                    )
            )
            .Returns(
                prescriptionItemQuery
            );


        _unitOfWorkMock
            .Setup(
                x =>
                    x.PrescriptionItemRepository
            )
            .Returns(
                prescriptionItemRepositoryMock.Object
            );


        // Act

        var result =
            await _service
                .DispensePrescriptionAsync(
                    prescriptionId: 1,
                    currentUserId: 1,
                    request: request
                );


        // Assert

        Assert.Equal(
            409,
            result.StatusCode
        );

        Assert.Equal(
            PharmacyResponseMessageDTO
                .PrescriptionHasNoItems,
            result.Message
        );
    }

    // =====================================================
    // TEST 12
    //
    // Batch đã expired phải bị bỏ qua.
    // =====================================================

    [Fact]
    public async Task DispensePrescriptionAsync_ShouldIgnoreExpiredBatch()
    {
        // Arrange

        var today =
            DateOnly.FromDateTime(DateTime.Now);

        var prescription =
            new Prescription
            {
                Id = 1,
                Status = (byte)PrescriptionStatus.Finalized
            };

        var prescriptionItem =
            new PrescriptionItem
            {
                Id = 1,
                PrescriptionId = 1,
                MedicineId = 1,
                Quantity = 2,
                MedicineNameSnapshot = "Test Medicine"
            };

        var medicine =
            new Medicine
            {
                Id = 1,
                Name = "Test Medicine",
                StockQuantity = 10
            };

        var expiredBatch =
            new MedicineBatch
            {
                Id = 1,
                MedicineId = 1,
                BatchNo = "EXPIRED",
                ExpiryDate = today.AddDays(-1),
                Quantity = 5,
                ImportPrice = 100
            };

        var validBatch =
            new MedicineBatch
            {
                Id = 2,
                MedicineId = 1,
                BatchNo = "VALID",
                ExpiryDate = today.AddDays(10),
                Quantity = 5,
                ImportPrice = 100
            };

        SetupPrescription(prescription);

        SetupPrescriptionItems(
            prescriptionItem
        );

        SetupMedicine(
            medicine
        );

        SetupMedicineBatches(
            expiredBatch,
            validBatch
        );

        var transactions =
            SetupTransactionMocks();

        var request =
            new DispenseRequestDTO
            {
                Items =
                [
                    new()
                {
                    PrescriptionItemId = 1,
                    BatchId = 2,
                    Quantity = 2
                }
                ]
            };


        // Act

        var result =
            await _service
                .DispensePrescriptionAsync(
                    1,
                    1,
                    request
                );


        // Assert

        Assert.Equal(
            200,
            result.StatusCode
        );

        Assert.Equal(
            5,
            expiredBatch.Quantity
        );

        Assert.Equal(
            3,
            validBatch.Quantity
        );

        Assert.Equal(
            8,
            medicine.StockQuantity
        );

        Assert.Single(transactions);

        Assert.Equal(
            2,
            transactions[0].BatchId
        );
    }

    // =====================================================
    // TEST 13
    //
    // Batch Quantity <= 0 phải bị bỏ qua.
    // =====================================================

    [Fact]
    public async Task DispensePrescriptionAsync_ShouldIgnoreEmptyBatch()
    {
        // Arrange

        var today =
            DateOnly.FromDateTime(DateTime.Now);

        var prescription =
            new Prescription
            {
                Id = 1,
                Status = (byte)PrescriptionStatus.Finalized
            };

        var prescriptionItem =
            new PrescriptionItem
            {
                Id = 1,
                PrescriptionId = 1,
                MedicineId = 1,
                Quantity = 2,
                MedicineNameSnapshot = "Test Medicine"
            };

        var medicine =
            new Medicine
            {
                Id = 1,
                Name = "Test Medicine",
                StockQuantity = 10
            };

        var emptyBatch =
            new MedicineBatch
            {
                Id = 1,
                MedicineId = 1,
                BatchNo = "EMPTY",
                ExpiryDate = today.AddDays(5),
                Quantity = 0,
                ImportPrice = 100
            };

        var validBatch =
            new MedicineBatch
            {
                Id = 2,
                MedicineId = 1,
                BatchNo = "VALID",
                ExpiryDate = today.AddDays(10),
                Quantity = 5,
                ImportPrice = 100
            };

        SetupPrescription(prescription);

        SetupPrescriptionItems(
            prescriptionItem
        );

        SetupMedicine(
            medicine
        );

        SetupMedicineBatches(
            emptyBatch,
            validBatch
        );

        SetupTransactionMocks();

        var request =
            new DispenseRequestDTO
            {
                Items =
                [
                    new()
                {
                    PrescriptionItemId = 1,
                    BatchId = 2,
                    Quantity = 2
                }
                ]
            };


        // Act

        var result =
            await _service
                .DispensePrescriptionAsync(
                    1,
                    1,
                    request
                );


        // Assert

        Assert.Equal(
            200,
            result.StatusCode
        );

        Assert.Equal(
            0,
            emptyBatch.Quantity
        );

        Assert.Equal(
            3,
            validBatch.Quantity
        );

        Assert.Equal(
            8,
            medicine.StockQuantity
        );
    }

    // =====================================================
    // TEST 14
    //
    // Chọn batch có ExpiryDate gần nhất.
    // =====================================================

    [Fact]
    public async Task DispensePrescriptionAsync_ShouldSelectNearestExpiryBatch()
    {
        // Arrange

        var today =
            DateOnly.FromDateTime(DateTime.Now);

        var prescription =
            new Prescription
            {
                Id = 1,
                Status = (byte)PrescriptionStatus.Finalized
            };

        var prescriptionItem =
            new PrescriptionItem
            {
                Id = 1,
                PrescriptionId = 1,
                MedicineId = 1,
                Quantity = 2,
                MedicineNameSnapshot = "Test Medicine"
            };

        var medicine =
            new Medicine
            {
                Id = 1,
                Name = "Test Medicine",
                StockQuantity = 10
            };

        // Expiry gần hơn → phải được chọn
        var nearestBatch =
            new MedicineBatch
            {
                Id = 10,
                MedicineId = 1,
                BatchNo = "NEAREST",
                ExpiryDate = today.AddDays(2),
                Quantity = 5,
                ImportPrice = 100
            };

        // Expiry xa hơn → chưa được lấy
        var laterBatch =
            new MedicineBatch
            {
                Id = 20,
                MedicineId = 1,
                BatchNo = "LATER",
                ExpiryDate = today.AddDays(10),
                Quantity = 5,
                ImportPrice = 100
            };


        SetupPrescription(
            prescription
        );

        SetupPrescriptionItems(
            prescriptionItem
        );

        SetupMedicine(
            medicine
        );

        SetupMedicineBatches(
            laterBatch,
            nearestBatch
        );

        var transactions =
            SetupTransactionMocks();


        var request =
            new DispenseRequestDTO
            {
                Items =
                [
                    new()
                {
                    PrescriptionItemId = 1,

                    // Chọn đúng FEFO batch
                    BatchId = 10,

                    Quantity = 2
                }
                ]
            };


        // Act

        var result =
            await _service
                .DispensePrescriptionAsync(
                    prescriptionId: 1,
                    currentUserId: 1,
                    request: request
                );


        // Assert

        Assert.Equal(
            200,
            result.StatusCode
        );

        // Batch gần expiry được lấy
        Assert.Equal(
            3,
            nearestBatch.Quantity
        );

        // Batch xa expiry không bị lấy
        Assert.Equal(
            5,
            laterBatch.Quantity
        );

        // Tổng stock Medicine giảm 2
        Assert.Equal(
            8,
            medicine.StockQuantity
        );

        // Chỉ tạo transaction cho batch FEFO
        Assert.Single(
            transactions
        );

        Assert.Equal(
            10,
            transactions[0].BatchId
        );

        Assert.Equal(
            2,
            transactions[0].Quantity
        );
    }

    // =====================================================
    // TEST 15
    //
    // Một batch đủ thuốc.
    // Chỉ lấy từ batch đó.
    // =====================================================

    [Fact]
    public async Task DispensePrescriptionAsync_ShouldDispenseFromOneBatch_WhenStockIsEnough()
    {
        // Arrange

        var today =
            DateOnly.FromDateTime(DateTime.Now);

        var prescription =
            new Prescription
            {
                Id = 1,
                Status = (byte)PrescriptionStatus.Finalized
            };

        var prescriptionItem =
            new PrescriptionItem
            {
                Id = 1,
                PrescriptionId = 1,
                MedicineId = 1,
                Quantity = 3,
                MedicineNameSnapshot = "Test Medicine"
            };

        var medicine =
            new Medicine
            {
                Id = 1,
                Name = "Test Medicine",
                StockQuantity = 5
            };

        var batch =
            new MedicineBatch
            {
                Id = 1,
                MedicineId = 1,
                BatchNo = "B001",
                ExpiryDate = today.AddDays(10),
                Quantity = 5,
                ImportPrice = 100
            };

        SetupPrescription(
            prescription
        );

        SetupPrescriptionItems(
            prescriptionItem
        );

        SetupMedicine(
            medicine
        );

        SetupMedicineBatches(
            batch
        );

        var transactions =
            SetupTransactionMocks();

        var request =
            new DispenseRequestDTO
            {
                Items =
                [
                    new()
                {
                    PrescriptionItemId = 1,
                    BatchId = 1,
                    Quantity = 3
                }
                ]
            };


        // Act

        var result =
            await _service
                .DispensePrescriptionAsync(
                    prescriptionId: 1,
                    currentUserId: 1,
                    request: request
                );


        // Assert

        Assert.Equal(
            200,
            result.StatusCode
        );

        // Batch: 5 - 3 = 2
        Assert.Equal(
            2,
            batch.Quantity
        );

        // Medicine: 5 - 3 = 2
        Assert.Equal(
            2,
            medicine.StockQuantity
        );

        // Chỉ có 1 transaction
        Assert.Single(
            transactions
        );

        Assert.Equal(
            1,
            transactions[0].BatchId
        );

        Assert.Equal(
            3,
            transactions[0].Quantity
        );
    }

    // =====================================================
    // TEST 16
    //
    // Batch FEFO đầu tiên không đủ.
    // Service phải lấy tiếp batch sau.
    // =====================================================

    [Fact]
    public async Task DispensePrescriptionAsync_ShouldUseNextBatch_WhenFirstBatchIsInsufficient()
    {
        // Arrange

        var today =
            DateOnly.FromDateTime(DateTime.Now);

        var prescription =
            new Prescription
            {
                Id = 1,
                Status = (byte)PrescriptionStatus.Finalized
            };

        var prescriptionItem =
            new PrescriptionItem
            {
                Id = 1,
                PrescriptionId = 1,
                MedicineId = 1,
                Quantity = 4,
                MedicineNameSnapshot = "Test Medicine"
            };

        var medicine =
            new Medicine
            {
                Id = 1,
                Name = "Test Medicine",
                StockQuantity = 7
            };

        var firstBatch =
            new MedicineBatch
            {
                Id = 1,
                MedicineId = 1,
                BatchNo = "FIRST",
                ExpiryDate = today.AddDays(2),
                Quantity = 2,
                ImportPrice = 100
            };

        var secondBatch =
            new MedicineBatch
            {
                Id = 2,
                MedicineId = 1,
                BatchNo = "SECOND",
                ExpiryDate = today.AddDays(10),
                Quantity = 5,
                ImportPrice = 100
            };

        SetupPrescription(
            prescription
        );

        SetupPrescriptionItems(
            prescriptionItem
        );

        SetupMedicine(
            medicine
        );

        SetupMedicineBatches(
            firstBatch,
            secondBatch
        );

        SetupTransactionMocks();

        var request =
            new DispenseRequestDTO
            {
                Items =
                [
                    new()
                {
                    PrescriptionItemId = 1,
                    BatchId = 1,
                    Quantity = 4
                }
                ]
            };


        // Act

        var result =
            await _service
                .DispensePrescriptionAsync(
                    prescriptionId: 1,
                    currentUserId: 1,
                    request: request
                );


        // Assert

        Assert.Equal(
            200,
            result.StatusCode
        );

        // Batch 1: lấy hết 2
        Assert.Equal(
            0,
            firstBatch.Quantity
        );

        // Batch 2: lấy thêm 2
        Assert.Equal(
            3,
            secondBatch.Quantity
        );

        // Medicine: 7 - 4 = 3
        Assert.Equal(
            3,
            medicine.StockQuantity
        );
    }

    // =====================================================
    // TEST 17
    //
    // Lấy thuốc từ nhiều batch.
    //
    // Expected:
    // - Batch 1 lấy 2
    // - Batch 2 lấy 2
    // - Có 2 stock transactions
    // =====================================================

    [Fact]
    public async Task DispensePrescriptionAsync_ShouldCreateTransactionsForMultipleBatches()
    {
        // Arrange

        var today =
            DateOnly.FromDateTime(DateTime.Now);

        var prescription =
            new Prescription
            {
                Id = 1,
                Status = (byte)PrescriptionStatus.Finalized
            };

        var prescriptionItem =
            new PrescriptionItem
            {
                Id = 1,
                PrescriptionId = 1,
                MedicineId = 1,
                Quantity = 4,
                MedicineNameSnapshot = "Test Medicine"
            };

        var medicine =
            new Medicine
            {
                Id = 1,
                Name = "Test Medicine",
                StockQuantity = 7
            };

        var firstBatch =
            new MedicineBatch
            {
                Id = 1,
                MedicineId = 1,
                BatchNo = "FIRST",
                ExpiryDate = today.AddDays(2),
                Quantity = 2,
                ImportPrice = 100
            };

        var secondBatch =
            new MedicineBatch
            {
                Id = 2,
                MedicineId = 1,
                BatchNo = "SECOND",
                ExpiryDate = today.AddDays(10),
                Quantity = 5,
                ImportPrice = 100
            };


        SetupPrescription(
            prescription
        );

        SetupPrescriptionItems(
            prescriptionItem
        );

        SetupMedicine(
            medicine
        );

        SetupMedicineBatches(
            firstBatch,
            secondBatch
        );


        var transactions =
            SetupTransactionMocks();


        var request =
            new DispenseRequestDTO
            {
                Items =
                [
                    new()
                {
                    PrescriptionItemId = 1,

                    // Phải là batch FEFO đầu tiên
                    BatchId = 1,

                    Quantity = 4
                }
                ]
            };


        // Act

        var result =
            await _service
                .DispensePrescriptionAsync(
                    prescriptionId: 1,
                    currentUserId: 1,
                    request: request
                );


        // Assert

        Assert.Equal(
            200,
            result.StatusCode
        );


        // =====================================================
        // STOCK
        // =====================================================

        Assert.Equal(
            0,
            firstBatch.Quantity
        );

        Assert.Equal(
            3,
            secondBatch.Quantity
        );

        Assert.Equal(
            3,
            medicine.StockQuantity
        );


        // =====================================================
        // TRANSACTIONS
        // =====================================================

        Assert.Equal(
            2,
            transactions.Count
        );


        // Transaction của batch 1

        Assert.Equal(
            1,
            transactions[0].BatchId
        );

        Assert.Equal(
            2,
            transactions[0].Quantity
        );

        Assert.Equal(
            1,
            transactions[0].Type
        );

        Assert.Equal(
            1,
            transactions[0].PrescriptionId
        );


        // Transaction của batch 2

        Assert.Equal(
            2,
            transactions[1].BatchId
        );

        Assert.Equal(
            2,
            transactions[1].Quantity
        );

        Assert.Equal(
            1,
            transactions[1].Type
        );

        Assert.Equal(
            1,
            transactions[1].PrescriptionId
        );
    }

    // =====================================================
    // TEST 18
    //
    // Request chọn batch không phải FEFO batch đầu tiên.
    //
    // Expected:
    // 409
    // InvalidFefoBatch
    // Rollback
    // Không Commit
    // =====================================================

    [Fact]
    public async Task DispensePrescriptionAsync_ShouldReturn409_WhenPreferredBatchIsNotFefoBatch()
    {
        // Arrange

        var today =
            DateOnly.FromDateTime(DateTime.Now);

        var prescription =
            new Prescription
            {
                Id = 1,
                Status = (byte)PrescriptionStatus.Finalized
            };

        var prescriptionItem =
            new PrescriptionItem
            {
                Id = 1,
                PrescriptionId = 1,
                MedicineId = 1,
                Quantity = 3,
                MedicineNameSnapshot = "Test Medicine"
            };

        var medicine =
            new Medicine
            {
                Id = 1,
                Name = "Test Medicine",
                StockQuantity = 10
            };

        // FEFO batch
        var firstBatch =
            new MedicineBatch
            {
                Id = 1,
                MedicineId = 1,
                BatchNo = "FEFO",
                ExpiryDate = today.AddDays(2),
                Quantity = 5,
                ImportPrice = 100
            };

        // Batch có expiry xa hơn
        var secondBatch =
            new MedicineBatch
            {
                Id = 2,
                MedicineId = 1,
                BatchNo = "LATER",
                ExpiryDate = today.AddDays(10),
                Quantity = 5,
                ImportPrice = 100
            };


        SetupPrescription(
            prescription
        );

        SetupPrescriptionItems(
            prescriptionItem
        );

        SetupMedicine(
            medicine
        );

        SetupMedicineBatches(
            firstBatch,
            secondBatch
        );

        SetupTransactionMocks();


        // Request cố tình chọn Batch 2
        // trong khi Batch 1 mới là FEFO batch.

        var request =
            new DispenseRequestDTO
            {
                Items =
                [
                    new()
                {
                    PrescriptionItemId = 1,

                    BatchId = 2,

                    Quantity = 3
                }
                ]
            };


        // Act

        var result =
            await _service
                .DispensePrescriptionAsync(
                    prescriptionId: 1,
                    currentUserId: 1,
                    request: request
                );


        // Assert

        Assert.Equal(
            409,
            result.StatusCode
        );

        Assert.Contains(
        PharmacyResponseMessageDTO.InvalidFefoBatch,
        result.Message
    );

        Assert.Contains(
            "Test Medicine",
            result.Message
        );


        // Không được trừ stock.

        Assert.Equal(
            5,
            firstBatch.Quantity
        );

        Assert.Equal(
            5,
            secondBatch.Quantity
        );

        Assert.Equal(
            10,
            medicine.StockQuantity
        );


        // Phải rollback.

        _unitOfWorkMock.Verify(
            x =>
                x.RollbackTransactionAsync(),
            Times.Once
        );


        // Không được commit.

        _unitOfWorkMock.Verify(
            x =>
                x.CommitTransactionAsync(),
            Times.Never
        );
    }

    // =====================================================
    // TEST 19
    //
    // Tổng stock các batch hợp lệ không đủ.
    //
    // Expected:
    // 409
    // InsufficientStock
    // Rollback
    // Không Commit
    // =====================================================

    [Fact]
    public async Task DispensePrescriptionAsync_ShouldReturn409_WhenTotalStockIsInsufficient()
    {
        // Arrange

        var today =
            DateOnly.FromDateTime(DateTime.Now);

        var prescription =
            new Prescription
            {
                Id = 1,
                Status = (byte)PrescriptionStatus.Finalized
            };

        var prescriptionItem =
            new PrescriptionItem
            {
                Id = 1,
                PrescriptionId = 1,
                MedicineId = 1,
                Quantity = 10,
                MedicineNameSnapshot = "Test Medicine"
            };

        var medicine =
            new Medicine
            {
                Id = 1,
                Name = "Test Medicine",
                StockQuantity = 5
            };

        var batch =
            new MedicineBatch
            {
                Id = 1,
                MedicineId = 1,
                BatchNo = "B001",
                ExpiryDate = today.AddDays(10),
                Quantity = 5,
                ImportPrice = 100
            };

        SetupPrescription(
            prescription
        );

        SetupPrescriptionItems(
            prescriptionItem
        );

        SetupMedicine(
            medicine
        );

        SetupMedicineBatches(
            batch
        );

        SetupTransactionMocks();

        var request =
            new DispenseRequestDTO
            {
                Items =
                [
                    new()
                {
                    PrescriptionItemId = 1,
                    BatchId = 1,
                    Quantity = 10
                }
                ]
            };


        // Act

        var result =
            await _service
                .DispensePrescriptionAsync(
                    prescriptionId: 1,
                    currentUserId: 1,
                    request: request
                );


        // Assert

        Assert.Equal(
            409,
            result.StatusCode
        );

        Assert.Contains(
            PharmacyResponseMessageDTO.InsufficientStock,
            result.Message
        );

        // Stock không được thay đổi.

        Assert.Equal(
            5,
            batch.Quantity
        );

        Assert.Equal(
            5,
            medicine.StockQuantity
        );

        // Phải rollback.

        _unitOfWorkMock.Verify(
            x =>
                x.RollbackTransactionAsync(),
            Times.Once
        );

        // Không commit.

        _unitOfWorkMock.Verify(
            x =>
                x.CommitTransactionAsync(),
            Times.Never
        );
    }

    // =====================================================
    // TEST 20
    //
    // Medicine không tồn tại.
    //
    // Expected:
    // 404
    // MedicineNotFound
    // Rollback
    // Không Commit
    // =====================================================

    [Fact]
    public async Task DispensePrescriptionAsync_ShouldReturn404_WhenMedicineDoesNotExist()
    {
        // Arrange

        var today =
            DateOnly.FromDateTime(DateTime.Now);

        var prescription =
            new Prescription
            {
                Id = 1,
                Status = (byte)PrescriptionStatus.Finalized
            };

        var prescriptionItem =
            new PrescriptionItem
            {
                Id = 1,
                PrescriptionId = 1,
                MedicineId = 1,
                Quantity = 2,
                MedicineNameSnapshot = "Missing Medicine"
            };

        var batch =
            new MedicineBatch
            {
                Id = 1,
                MedicineId = 1,
                BatchNo = "B001",
                ExpiryDate = today.AddDays(10),
                Quantity = 5,
                ImportPrice = 100
            };

        SetupPrescription(
            prescription
        );

        SetupPrescriptionItems(
            prescriptionItem
        );

        // Không truyền medicine nào
        // => MedicineRepository trả về empty query.
        SetupMedicine();

        SetupMedicineBatches(
            batch
        );

        SetupTransactionMocks();

        var request =
            new DispenseRequestDTO
            {
                Items =
                [
                    new()
                {
                    PrescriptionItemId = 1,
                    BatchId = 1,
                    Quantity = 2
                }
                ]
            };


        // Act

        var result =
            await _service
                .DispensePrescriptionAsync(
                    prescriptionId: 1,
                    currentUserId: 1,
                    request: request
                );


        // Assert

        Assert.Equal(
            404,
            result.StatusCode
        );

        Assert.Equal(
            PharmacyResponseMessageDTO.MedicineNotFound,
            result.Message
        );


        // Phải rollback.

        _unitOfWorkMock.Verify(
            x =>
                x.RollbackTransactionAsync(),
            Times.Once
        );


        // Không commit.

        _unitOfWorkMock.Verify(
            x =>
                x.CommitTransactionAsync(),
            Times.Never
        );
    }

    // =====================================================
    // TEST 21
    //
    // Medicine.StockQuantity không đủ.
    //
    // Batch có đủ thuốc nhưng Medicine.StockQuantity không đủ.
    //
    // Expected:
    // 409
    // InvalidMedicineStock
    // Rollback
    // Không Commit
    // =====================================================

    [Fact]
    public async Task DispensePrescriptionAsync_ShouldReturn409_WhenMedicineStockIsInsufficient()
    {
        // Arrange

        var today =
            DateOnly.FromDateTime(DateTime.Now);

        var prescription =
            new Prescription
            {
                Id = 1,
                Status = (byte)PrescriptionStatus.Finalized
            };

        var prescriptionItem =
            new PrescriptionItem
            {
                Id = 1,
                PrescriptionId = 1,
                MedicineId = 1,
                Quantity = 3,
                MedicineNameSnapshot = "Test Medicine"
            };

        // Medicine chỉ còn 2
        var medicine =
            new Medicine
            {
                Id = 1,
                Name = "Test Medicine",
                StockQuantity = 2
            };

        // Nhưng batch có 5 -> batch stock đủ
        var batch =
            new MedicineBatch
            {
                Id = 1,
                MedicineId = 1,
                BatchNo = "B001",
                ExpiryDate = today.AddDays(10),
                Quantity = 5,
                ImportPrice = 100
            };


        SetupPrescription(
            prescription
        );

        SetupPrescriptionItems(
            prescriptionItem
        );

        SetupMedicine(
            medicine
        );

        SetupMedicineBatches(
            batch
        );

        SetupTransactionMocks();


        var request =
            new DispenseRequestDTO
            {
                Items =
                [
                    new()
                {
                    PrescriptionItemId = 1,
                    BatchId = 1,
                    Quantity = 3
                }
                ]
            };


        // Act

        var result =
            await _service
                .DispensePrescriptionAsync(
                    prescriptionId: 1,
                    currentUserId: 1,
                    request: request
                );


        // Assert

        Assert.Equal(
            409,
            result.StatusCode
        );

        Assert.Contains(
            PharmacyResponseMessageDTO.InvalidMedicineStock,
            result.Message
        );

        // Medicine.StockQuantity không bị giảm.
        Assert.Equal(
            2,
            medicine.StockQuantity
        );

        // Rollback phải được gọi.
        _unitOfWorkMock.Verify(
            x =>
                x.RollbackTransactionAsync(),
            Times.Once
        );

        // Không commit.
        _unitOfWorkMock.Verify(
            x =>
                x.CommitTransactionAsync(),
            Times.Never
        );

        // Không SaveChanges.
        _unitOfWorkMock.Verify(
            x =>
                x.SaveChangesAsync(),
            Times.Never
        );
    }

    // =====================================================
    // TEST 22
    //
    // Tổng quantity trong request không khớp
    // quantity được kê.
    //
    // Expected:
    // 400
    // DispenseQuantityInvalid
    // =====================================================

    [Fact]
    public async Task DispensePrescriptionAsync_ShouldReturn400_WhenRequestedQuantityDoesNotMatchPrescription()
    {
        // Arrange

        var prescription =
            new Prescription
            {
                Id = 1,
                Status = (byte)PrescriptionStatus.Finalized
            };

        var prescriptionItem =
            new PrescriptionItem
            {
                Id = 1,
                PrescriptionId = 1,
                MedicineId = 1,
                Quantity = 5,
                MedicineNameSnapshot = "Test Medicine"
            };

        SetupPrescription(
            prescription
        );

        SetupPrescriptionItems(
            prescriptionItem
        );

        // Prescription yêu cầu 5
        // Request chỉ phát 3
        var request =
            new DispenseRequestDTO
            {
                Items =
                [
                    new()
                {
                    PrescriptionItemId = 1,
                    BatchId = 1,
                    Quantity = 3
                }
                ]
            };


        // Act

        var result =
            await _service
                .DispensePrescriptionAsync(
                    prescriptionId: 1,
                    currentUserId: 1,
                    request: request
                );


        // Assert

        Assert.Equal(
            400,
            result.StatusCode
        );

        Assert.Contains(
            PharmacyResponseMessageDTO.DispenseQuantityInvalid,
            result.Message
        );

        Assert.Contains(
            "Test Medicine",
            result.Message
        );


        // Chưa bắt đầu transaction
        // vì validation xảy ra trước BeginTransactionAsync.

        _unitOfWorkMock.Verify(
            x =>
                x.BeginTransactionAsync(),
            Times.Never
        );

        _unitOfWorkMock.Verify(
            x =>
                x.RollbackTransactionAsync(),
            Times.Never
        );

        _unitOfWorkMock.Verify(
            x =>
                x.CommitTransactionAsync(),
            Times.Never
        );
    }

    // =====================================================
    // TEST 23
    //
    // Prescription có item nhưng request không gửi item đó.
    //
    // Expected:
    // 400
    // DispenseItemMissing
    // =====================================================

    [Fact]
    public async Task DispensePrescriptionAsync_ShouldReturn400_WhenPrescriptionItemIsMissingFromRequest()
    {
        // Arrange

        var prescription =
            new Prescription
            {
                Id = 1,
                Status = (byte)PrescriptionStatus.Finalized
            };

        var prescriptionItem =
            new PrescriptionItem
            {
                Id = 1,
                PrescriptionId = 1,
                MedicineId = 1,
                Quantity = 2,
                MedicineNameSnapshot = "Test Medicine"
            };


        SetupPrescription(
            prescription
        );

        SetupPrescriptionItems(
            prescriptionItem
        );


        // Request không chứa PrescriptionItemId = 1.
        // Dùng một ID khác.

        var request =
            new DispenseRequestDTO
            {
                Items =
                [
                    new()
                {
                    PrescriptionItemId = 999,
                    BatchId = 1,
                    Quantity = 2
                }
                ]
            };


        // Act

        var result =
            await _service
                .DispensePrescriptionAsync(
                    prescriptionId: 1,
                    currentUserId: 1,
                    request: request
                );


        // Assert

        Assert.Equal(
            400,
            result.StatusCode
        );

        Assert.Contains(
            PharmacyResponseMessageDTO.DispenseItemMissing,
            result.Message
        );

        Assert.Contains(
            "#1",
            result.Message
        );


        // Validation xảy ra trước transaction.

        _unitOfWorkMock.Verify(
            x =>
                x.BeginTransactionAsync(),
            Times.Never
        );

        _unitOfWorkMock.Verify(
            x =>
                x.RollbackTransactionAsync(),
            Times.Never
        );

        _unitOfWorkMock.Verify(
            x =>
                x.CommitTransactionAsync(),
            Times.Never
        );
    }

    // =====================================================
    // TEST 24
    //
    // Request chứa PrescriptionItem không thuộc prescription.
    //
    // Expected:
    // 400
    // PrescriptionItemInvalid
    // =====================================================

    [Fact]
    public async Task DispensePrescriptionAsync_ShouldReturn400_WhenRequestContainsExtraPrescriptionItem()
    {
        // Arrange

        var prescription =
            new Prescription
            {
                Id = 1,
                Status = (byte)PrescriptionStatus.Finalized
            };

        var prescriptionItem =
            new PrescriptionItem
            {
                Id = 1,
                PrescriptionId = 1,
                MedicineId = 1,
                Quantity = 2,
                MedicineNameSnapshot = "Test Medicine"
            };


        SetupPrescription(
            prescription
        );

        SetupPrescriptionItems(
            prescriptionItem
        );


        // Request có:
        // - Item 1: hợp lệ, thuộc prescription
        // - Item 999: không thuộc prescription

        var request =
            new DispenseRequestDTO
            {
                Items =
                [
                    new()
                {
                    PrescriptionItemId = 1,
                    BatchId = 1,
                    Quantity = 2
                },

                new()
                {
                    PrescriptionItemId = 999,
                    BatchId = 2,
                    Quantity = 1
                }
                ]
            };


        // Act

        var result =
            await _service
                .DispensePrescriptionAsync(
                    prescriptionId: 1,
                    currentUserId: 1,
                    request: request
                );


        // Assert

        Assert.Equal(
            400,
            result.StatusCode
        );

        Assert.Equal(
            PharmacyResponseMessageDTO.PrescriptionItemInvalid,
            result.Message
        );


        // Validation xảy ra trước transaction.

        _unitOfWorkMock.Verify(
            x =>
                x.BeginTransactionAsync(),
            Times.Never
        );

        _unitOfWorkMock.Verify(
            x =>
                x.RollbackTransactionAsync(),
            Times.Never
        );

        _unitOfWorkMock.Verify(
            x =>
                x.CommitTransactionAsync(),
            Times.Never
        );
    }

    // =====================================================
    // TEST 25
    //
    // Dispense thành công.
    //
    // Kiểm tra:
    // - 200
    // - Prescription -> Dispensed
    // - DispensedBy
    // - DispensedAt
    // - Batch giảm stock
    // - Medicine giảm stock
    // - StockTransaction được tạo
    // - BeginTransaction
    // - SaveChanges
    // - Commit
    // =====================================================

    [Fact]
    public async Task DispensePrescriptionAsync_ShouldDispenseSuccessfully()
    {
        // Arrange

        var today =
            DateOnly.FromDateTime(DateTime.Now);

        var prescription =
            new Prescription
            {
                Id = 1,
                MedicalRecordId = 10,
                Status = (byte)PrescriptionStatus.Finalized,
                CreatedAt = DateTime.Now,
                Note = "Test prescription"
            };

        var prescriptionItem =
            new PrescriptionItem
            {
                Id = 1,
                PrescriptionId = 1,
                MedicineId = 1,
                Quantity = 2,
                MedicineNameSnapshot = "Test Medicine"
            };

        var medicine =
            new Medicine
            {
                Id = 1,
                Name = "Test Medicine",
                StockQuantity = 5
            };

        var batch =
            new MedicineBatch
            {
                Id = 1,
                MedicineId = 1,
                BatchNo = "B001",
                ExpiryDate = today.AddDays(10),
                Quantity = 5,
                ImportPrice = 100
            };


        SetupPrescription(
            prescription
        );

        SetupPrescriptionItems(
            prescriptionItem
        );

        SetupMedicine(
            medicine
        );

        SetupMedicineBatches(
            batch
        );


        var transactions =
            SetupTransactionMocks();


        var request =
            new DispenseRequestDTO
            {
                Items =
                [
                    new()
                {
                    PrescriptionItemId = 1,
                    BatchId = 1,
                    Quantity = 2
                }
                ]
            };


        // Act

        var result =
            await _service
                .DispensePrescriptionAsync(
                    prescriptionId: 1,
                    currentUserId: 99,
                    request: request
                );


        // Assert

        Assert.Equal(
            200,
            result.StatusCode
        );

        Assert.Equal(
            PharmacyResponseMessageDTO.DispenseSuccess,
            result.Message
        );


        // =====================================================
        // PRESCRIPTION
        // =====================================================

        Assert.Equal(
            (byte)PrescriptionStatus.Dispensed,
            prescription.Status
        );

        Assert.Equal(
            99,
            prescription.DispensedBy
        );

        Assert.NotNull(
            prescription.DispensedAt
        );


        // =====================================================
        // STOCK
        // =====================================================

        Assert.Equal(
            3,
            batch.Quantity
        );

        Assert.Equal(
            3,
            medicine.StockQuantity
        );


        // =====================================================
        // STOCK TRANSACTION
        // =====================================================

        Assert.Single(
            transactions
        );

        Assert.Equal(
            1,
            transactions[0].BatchId
        );

        Assert.Equal(
            1,
            transactions[0].MedicineId
        );

        Assert.Equal(
            2,
            transactions[0].Quantity
        );

        Assert.Equal(
            1,
            transactions[0].Type
        );

        Assert.Equal(
            1,
            transactions[0].PrescriptionId
        );


        // =====================================================
        // TRANSACTION FLOW
        // =====================================================

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
    // TEST 26
    //
    // Exception xảy ra sau khi transaction bắt đầu.
    //
    // Expected:
    // 500
    // DispenseFailed
    // Rollback
    // Không Commit
    // =====================================================

    [Fact]
    public async Task DispensePrescriptionAsync_ShouldRollbackAndReturn500_WhenExceptionOccurs()
    {
        // Arrange

        var prescription =
            new Prescription
            {
                Id = 1,
                Status = (byte)PrescriptionStatus.Finalized
            };

        var prescriptionItem =
            new PrescriptionItem
            {
                Id = 1,
                PrescriptionId = 1,
                MedicineId = 1,
                Quantity = 2,
                MedicineNameSnapshot = "Test Medicine"
            };


        SetupPrescription(
            prescription
        );

        SetupPrescriptionItems(
            prescriptionItem
        );

        SetupTransactionMocks();


        // =====================================================
        // MedicineBatchRepository
        //
        // Cố tình throw exception khi service
        // truy vấn batch.
        //
        // Lúc này transaction đã được bắt đầu.
        // =====================================================

        var medicineBatchRepositoryMock =
            new Mock<IMedicineBatchRepository>();


        medicineBatchRepositoryMock
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
            .Throws(
                new InvalidOperationException(
                    "Test exception"
                )
            );


        _unitOfWorkMock
            .Setup(
                x =>
                    x.MedicineBatchRepository
            )
            .Returns(
                medicineBatchRepositoryMock.Object
            );


        var request =
            new DispenseRequestDTO
            {
                Items =
                [
                    new()
                {
                    PrescriptionItemId = 1,
                    BatchId = 1,
                    Quantity = 2
                }
                ]
            };


        // Act

        var result =
            await _service
                .DispensePrescriptionAsync(
                    prescriptionId: 1,
                    currentUserId: 1,
                    request: request
                );


        // Assert

        Assert.Equal(
            500,
            result.StatusCode
        );

        Assert.Equal(
            PharmacyResponseMessageDTO.DispenseFailed,
            result.Message
        );


        // Transaction phải được bắt đầu.

        _unitOfWorkMock.Verify(
            x =>
                x.BeginTransactionAsync(),
            Times.Once
        );


        // Exception xảy ra sau khi transaction bắt đầu
        // nên phải rollback.

        _unitOfWorkMock.Verify(
            x =>
                x.RollbackTransactionAsync(),
            Times.Once
        );


        // Không được commit.

        _unitOfWorkMock.Verify(
            x =>
                x.CommitTransactionAsync(),
            Times.Never
        );


        // Exception xảy ra trước SaveChanges.

        _unitOfWorkMock.Verify(
            x =>
                x.SaveChangesAsync(),
            Times.Never
        );
    }




    // =====================================================
    // TEST HELPERS
    // =====================================================

    private void SetupPrescription(
        Prescription prescription)
    {
        var repositoryMock =
            new Mock<IPrescriptionRepository>();

        var query =
            new List<Prescription>
            {
            prescription
            }
            .BuildMock();

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
                query
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


    private void SetupPrescriptionItems(
        params PrescriptionItem[] items)
    {
        var repositoryMock =
            new Mock<IPrescriptionItemRepository>();

        var query =
            items
                .ToList()
                .BuildMock();

        repositoryMock
            .Setup(
                x =>
                    x.WhereSql(
                        It.IsAny<
                            Expression<
                                Func<
                                    PrescriptionItem,
                                    bool
                                >
                            >
                        >()
                    )
            )
            .Returns(
                query
            );

        _unitOfWorkMock
            .Setup(
                x =>
                    x.PrescriptionItemRepository
            )
            .Returns(
                repositoryMock.Object
            );
    }


    private void SetupMedicine(
        params Medicine[] medicines)
    {
        var repositoryMock =
            new Mock<IMedicineRepository>();

        var query =
            medicines
                .ToList()
                .BuildMock();

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
                query
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
                {
                    var filteredBatches =
                        batches
                            .Where(
                                predicate.Compile()
                            )
                            .ToList();

                    return filteredBatches
                        .BuildMock();
                }
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

    private List<MedicineStockTransaction>
        SetupTransactionMocks()
    {
        var transactions =
            new List<MedicineStockTransaction>();


        var transactionRepositoryMock =
            new Mock<
                IMedicineStockTransactionRepository
            >();


        transactionRepositoryMock
            .Setup(
                x =>
                    x.AddAsync(
                        It.IsAny<
                            MedicineStockTransaction
                        >()
                    )
            )
            .Callback(
                (
                    MedicineStockTransaction transaction
                ) =>
                {
                    transactions.Add(
                        transaction
                    );
                }
            )
            .Returns(
                Task.CompletedTask
            );


        _unitOfWorkMock
            .Setup(
                x =>
                    x.MedicineStockTransactionRepository
            )
            .Returns(
                transactionRepositoryMock.Object
            );


        _unitOfWorkMock
            .Setup(
                x =>
                    x.BeginTransactionAsync()
            )
            .Returns(
                Task.CompletedTask
            );


        _unitOfWorkMock
            .Setup(
                x =>
                    x.SaveChangesAsync()
            )
            .Returns(
                Task.CompletedTask
            );


        _unitOfWorkMock
            .Setup(
                x =>
                    x.CommitTransactionAsync()
            )
            .Returns(
                Task.CompletedTask
            );


        _unitOfWorkMock
            .Setup(
                x =>
                    x.RollbackTransactionAsync()
            )
            .Returns(
                Task.CompletedTask
            );


        return transactions;
    }


}