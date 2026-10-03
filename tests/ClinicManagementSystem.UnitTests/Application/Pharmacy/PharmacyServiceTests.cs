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

}