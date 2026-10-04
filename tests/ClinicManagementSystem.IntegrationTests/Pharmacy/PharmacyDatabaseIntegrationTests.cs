using ClinicManagementSystem.Application.DTOs.Pharmacy;
using ClinicManagementSystem.Application.Enums;
using ClinicManagementSystem.Application.Services;
using ClinicManagementSystem.Infrastructure.Data;
using ClinicManagementSystem.Infrastructure.Models;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Xunit;


namespace ClinicManagementSystem.IntegrationTests;


// =====================================================
// PHARMACY DATABASE INTEGRATION TESTS
// =====================================================
//
// xUnit
//    ↓
// CustomWebApplicationFactory
//    ↓
// ASP.NET Core DI
//    ↓
// PharmacyService
//    ↓
// UnitOfWork / Repository
//    ↓
// EF Core
//    ↓
// SQL Server Testcontainer
//
// =====================================================

public class PharmacyDatabaseIntegrationTests
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;


    // =====================================================
    // CONSTRUCTOR
    // =====================================================

    public PharmacyDatabaseIntegrationTests(
        CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }


    // =====================================================
    // TEST 1
    //
    // Search medicine
    // keyword empty
    //
    // Expected:
    // 400
    // =====================================================

    [Fact]
    public async Task SearchMedicine_ShouldReturn400_WhenKeywordIsEmpty()
    {
        await using var scope =
            _factory.Services.CreateAsyncScope();

        var db =
            scope.ServiceProvider
                .GetRequiredService<ClinicManagementDbContext>();

        await db.Database.EnsureCreatedAsync();


        var service =
            scope.ServiceProvider
                .GetRequiredService<IPharmacyService>();


        var result =
            await service.SearchMedicinesAsync("");


        Assert.Equal(
            400,
            result.StatusCode
        );

        Assert.Equal(
            "Từ khóa tìm thuốc không được để trống.",
            result.Message
        );
    }


    // =====================================================
    // TEST 2
    //
    // Search medicine
    // active medicine
    //
    // Expected:
    // 200
    // medicine returned
    // =====================================================

    [Fact]
    public async Task SearchMedicine_ShouldReturnActiveMedicine()
    {
        await using var scope =
            _factory.Services.CreateAsyncScope();

        var db =
            scope.ServiceProvider
                .GetRequiredService<ClinicManagementDbContext>();

        await db.Database.EnsureCreatedAsync();


        var token =
            Guid.NewGuid()
                .ToString("N")[..10];


        var medicine =
            await SeedMedicineAsync(
                db,
                name:
                    $"IT-Pharmacy-Active-{token}",
                code:
                    $"IT-A-{token}",
                activeIngredient:
                    $"Ingredient-{token}",
                isActive: true
            );


        var service =
            scope.ServiceProvider
                .GetRequiredService<IPharmacyService>();


        var result =
            await service.SearchMedicinesAsync(
                $"IT-Pharmacy-Active-{token}"
            );


        Assert.Equal(
            200,
            result.StatusCode
        );

        Assert.NotNull(
            result.Content
        );

        Assert.Contains(
            result.Content!,
            x => x.Id == medicine.Id
        );
    }


    // =====================================================
    // TEST 3
    //
    // Search medicine
    // inactive medicine must not be returned
    // =====================================================

    [Fact]
    public async Task SearchMedicine_ShouldExcludeInactiveMedicine()
    {
        await using var scope =
            _factory.Services.CreateAsyncScope();

        var db =
            scope.ServiceProvider
                .GetRequiredService<ClinicManagementDbContext>();

        await db.Database.EnsureCreatedAsync();


        var token =
            Guid.NewGuid()
                .ToString("N")[..10];


        var activeMedicine =
            await SeedMedicineAsync(
                db,
                name:
                    $"IT-Pharmacy-Search-{token}",
                code:
                    $"IT-S-A-{token}",
                activeIngredient:
                    $"Ingredient-{token}",
                isActive: true
            );


        var inactiveMedicine =
            await SeedMedicineAsync(
                db,
                name:
                    $"IT-Pharmacy-Search-{token}",
                code:
                    $"IT-S-I-{token}",
                activeIngredient:
                    $"Ingredient-{token}",
                isActive: false
            );


        var service =
            scope.ServiceProvider
                .GetRequiredService<IPharmacyService>();


        var result =
            await service.SearchMedicinesAsync(
                $"IT-Pharmacy-Search-{token}"
            );


        Assert.Equal(
            200,
            result.StatusCode
        );

        Assert.NotNull(
            result.Content
        );


        Assert.Contains(
            result.Content!,
            x => x.Id == activeMedicine.Id
        );


        Assert.DoesNotContain(
            result.Content!,
            x => x.Id == inactiveMedicine.Id
        );
    }


    // =====================================================
    // TEST 4
    //
    // Search medicine
    // no matching medicine
    //
    // Expected:
    // 200
    // empty list
    // =====================================================

    [Fact]
    public async Task SearchMedicine_ShouldReturnEmpty_WhenNoMedicineMatches()
    {
        await using var scope =
            _factory.Services.CreateAsyncScope();

        var db =
            scope.ServiceProvider
                .GetRequiredService<ClinicManagementDbContext>();

        await db.Database.EnsureCreatedAsync();


        var token =
            Guid.NewGuid()
                .ToString("N");


        var service =
            scope.ServiceProvider
                .GetRequiredService<IPharmacyService>();


        var result =
            await service.SearchMedicinesAsync(
                $"NO-MATCH-{token}"
            );


        Assert.Equal(
            200,
            result.StatusCode
        );

        Assert.NotNull(
            result.Content
        );

        Assert.Empty(
            result.Content!
        );

        Assert.Equal(
            "Không tìm thấy thuốc phù hợp.",
            result.Message
        );
    }


    // =====================================================
    // TEST 5
    //
    // Create medicine receipt
    // invalid user
    //
    // Expected:
    // 401
    // =====================================================

    [Fact]
    public async Task CreateMedicineReceipt_ShouldReturn401_WhenUserIsInvalid()
    {
        await using var scope =
            _factory.Services.CreateAsyncScope();

        var db =
            scope.ServiceProvider
                .GetRequiredService<ClinicManagementDbContext>();

        await db.Database.EnsureCreatedAsync();


        var service =
            scope.ServiceProvider
                .GetRequiredService<IPharmacyService>();


        var request =
            new MedicineReceiptRequestDTO
            {
                SupplierName =
                    "Integration Supplier",

                ReferenceCode =
                    "IT-REF-401",

                Items =
                [
                    new MedicineReceiptItemRequestDTO
                    {
                        MedicineId = 1,

                        BatchNo =
                            "IT-BATCH-401",

                        ExpiryDate =
                            DateTime.UtcNow.AddYears(1),

                        Quantity = 5,

                        ImportPrice = 100
                    }
                ]
            };


        var result =
            await service.CreateMedicineReceiptAsync(
                0,
                request
            );


        Assert.Equal(
            401,
            result.StatusCode
        );

        Assert.Equal(
            "Không xác định được người nhập thuốc.",
            result.Message
        );
    }


    // =====================================================
    // TEST 6
    //
    // Create medicine receipt
    // empty items
    //
    // Expected:
    // 400
    // =====================================================

    [Fact]
    public async Task CreateMedicineReceipt_ShouldReturn400_WhenItemsAreEmpty()
    {
        await using var scope =
            _factory.Services.CreateAsyncScope();

        var db =
            scope.ServiceProvider
                .GetRequiredService<ClinicManagementDbContext>();

        await db.Database.EnsureCreatedAsync();


        var user =
            await SeedUserAsync(
                db
            );


        var service =
            scope.ServiceProvider
                .GetRequiredService<IPharmacyService>();


        var request =
            new MedicineReceiptRequestDTO
            {
                SupplierName =
                    "Integration Supplier",

                ReferenceCode =
                    "IT-REF-EMPTY",

                Items = []
            };


        var result =
            await service.CreateMedicineReceiptAsync(
                user.Id,
                request
            );


        Assert.Equal(
            400,
            result.StatusCode
        );

        Assert.Equal(
            "Danh sách thuốc nhập không được rỗng.",
            result.Message
        );
    }


    // =====================================================
    // TEST 7
    //
    // Create medicine receipt
    // medicine does not exist
    //
    // Expected:
    // 404
    // =====================================================

    [Fact]
    public async Task CreateMedicineReceipt_ShouldReturn404_WhenMedicineDoesNotExist()
    {
        await using var scope =
            _factory.Services.CreateAsyncScope();

        var db =
            scope.ServiceProvider
                .GetRequiredService<ClinicManagementDbContext>();

        await db.Database.EnsureCreatedAsync();


        var user =
            await SeedUserAsync(
                db
            );


        var service =
            scope.ServiceProvider
                .GetRequiredService<IPharmacyService>();


        var request =
            new MedicineReceiptRequestDTO
            {
                SupplierName =
                    "Integration Supplier",

                ReferenceCode =
                    "IT-REF-NOTFOUND",

                Items =
                [
                    new MedicineReceiptItemRequestDTO
                    {
                        MedicineId = 999999999,

                        BatchNo =
                            "IT-NOTFOUND",

                        ExpiryDate =
                            DateTime.UtcNow.AddYears(1),

                        Quantity = 5,

                        ImportPrice = 100
                    }
                ]
            };


        var result =
            await service.CreateMedicineReceiptAsync(
                user.Id,
                request
            );


        Assert.Equal(
            404,
            result.StatusCode
        );

        Assert.Equal(
            "Không tìm thấy thuốc.",
            result.Message
        );
    }


    // =====================================================
    // TEST 8
    //
    // Create medicine receipt
    // new batch
    //
    // Expected:
    // - medicine stock increases
    // - batch created
    // - stock transaction created
    // - Type = 0 (IN)
    // =====================================================

    [Fact]
    public async Task CreateMedicineReceipt_ShouldCreateNewBatchAndStockTransaction()
    {
        await using var scope =
            _factory.Services.CreateAsyncScope();

        var db =
            scope.ServiceProvider
                .GetRequiredService<ClinicManagementDbContext>();

        await db.Database.EnsureCreatedAsync();


        var user =
            await SeedUserAsync(
                db
            );


        var medicine =
            await SeedMedicineAsync(
                db,
                name:
                    $"IT-Receipt-New-{Guid.NewGuid():N}",
                code:
                    $"IT-R-N-{Guid.NewGuid():N}"[..20],
                activeIngredient:
                    "Integration Ingredient",
                isActive: true,
                stockQuantity: 10
            );


        var batchNo =
            $"IT-BATCH-NEW-{Guid.NewGuid():N}"[..30];


        var service =
            scope.ServiceProvider
                .GetRequiredService<IPharmacyService>();


        var request =
            new MedicineReceiptRequestDTO
            {
                SupplierName =
                    "Integration Supplier",

                ReferenceCode =
                    "IT-RECEIPT-NEW",

                Items =
                [
                    new MedicineReceiptItemRequestDTO
                    {
                        MedicineId =
                            medicine.Id,

                        BatchNo =
                            batchNo,

                        ExpiryDate =
                            DateTime.UtcNow.AddYears(1),

                        Quantity = 7,

                        ImportPrice = 55
                    }
                ]
            };


        var result =
            await service.CreateMedicineReceiptAsync(
                user.Id,
                request
            );


        // =================================================
        // RESPONSE
        // =================================================

        Assert.Equal(
            200,
            result.StatusCode
        );

        Assert.True(
            result.Content
        );


        // =================================================
        // MEDICINE STOCK
        // =================================================

        var savedMedicine =
            await db.Medicines
                .SingleAsync(
                    m => m.Id == medicine.Id
                );


        Assert.Equal(
            17,
            savedMedicine.StockQuantity
        );


        // =================================================
        // BATCH
        // =================================================

        var batch =
            await db.MedicineBatches
                .SingleAsync(
                    b =>
                        b.MedicineId ==
                            medicine.Id
                        &&
                        b.BatchNo ==
                            batchNo
                );


        Assert.Equal(
            7,
            batch.Quantity
        );


        // =================================================
        // STOCK TRANSACTION
        // =================================================

        var transaction =
            await db.MedicineStockTransactions
                .SingleAsync(
                    t =>
                        t.BatchId ==
                            batch.Id
                        &&
                        t.MedicineId ==
                            medicine.Id
                        &&
                        t.CreatedBy ==
                            user.Id
                );


        Assert.Equal(
            0,
            transaction.Type
        );

        Assert.Equal(
            7,
            transaction.Quantity
        );

        Assert.Equal(
            0,
            transaction.QuantityBefore
        );

        Assert.Equal(
            7,
            transaction.QuantityAfter
        );
    }


    // =====================================================
    // TEST 9
    //
    // Create medicine receipt
    // existing batch
    //
    // Expected:
    // - existing batch quantity increases
    // - medicine stock increases
    // - new stock transaction created
    // =====================================================

    [Fact]
    public async Task CreateMedicineReceipt_ShouldIncreaseExistingBatchAndStock()
    {
        await using var scope =
            _factory.Services.CreateAsyncScope();

        var db =
            scope.ServiceProvider
                .GetRequiredService<ClinicManagementDbContext>();

        await db.Database.EnsureCreatedAsync();


        var user =
            await SeedUserAsync(
                db
            );


        var medicine =
            await SeedMedicineAsync(
                db,
                name:
                    $"IT-Receipt-Existing-{Guid.NewGuid():N}",
                code:
                    $"IT-R-E-{Guid.NewGuid():N}"[..20],
                activeIngredient:
                    "Integration Ingredient",
                isActive: true,
                stockQuantity: 10
            );


        var batchNo =
            $"IT-BATCH-EXISTING-{Guid.NewGuid():N}"[..30];


        var existingBatch =
            new MedicineBatch
            {
                MedicineId =
                    medicine.Id,

                BatchNo =
                    batchNo,

                ExpiryDate =
                    DateOnly.FromDateTime(
                        DateTime.Today.AddYears(1)
                    ),

                Quantity = 5,

                ImportPrice = 50
            };


        db.MedicineBatches.Add(
            existingBatch
        );

        await db.SaveChangesAsync();


        var service =
            scope.ServiceProvider
                .GetRequiredService<IPharmacyService>();


        var request =
            new MedicineReceiptRequestDTO
            {
                SupplierName =
                    "Integration Supplier",

                ReferenceCode =
                    "IT-RECEIPT-EXISTING",

                Items =
                [
                    new MedicineReceiptItemRequestDTO
                    {
                        MedicineId =
                            medicine.Id,

                        BatchNo =
                            batchNo,

                        ExpiryDate =
                            DateTime.UtcNow.AddYears(1),

                        Quantity = 3,

                        ImportPrice = 60
                    }
                ]
            };


        var result =
            await service.CreateMedicineReceiptAsync(
                user.Id,
                request
            );


        // =================================================
        // RESPONSE
        // =================================================

        Assert.Equal(
            200,
            result.StatusCode
        );

        Assert.True(
            result.Content
        );


        // =================================================
        // MEDICINE STOCK
        // 10 + 3 = 13
        // =================================================

        var savedMedicine =
            await db.Medicines
                .SingleAsync(
                    m => m.Id == medicine.Id
                );


        Assert.Equal(
            13,
            savedMedicine.StockQuantity
        );


        // =================================================
        // EXISTING BATCH
        // 5 + 3 = 8
        // =================================================

        var savedBatch =
            await db.MedicineBatches
                .SingleAsync(
                    b =>
                        b.Id ==
                        existingBatch.Id
                );


        Assert.Equal(
            8,
            savedBatch.Quantity
        );


        // =================================================
        // STOCK TRANSACTION
        // =================================================

        var transaction =
            await db.MedicineStockTransactions
                .SingleAsync(
                    t =>
                        t.BatchId ==
                            existingBatch.Id
                        &&
                        t.MedicineId ==
                            medicine.Id
                        &&
                        t.CreatedBy ==
                            user.Id
                );


        Assert.Equal(
            0,
            transaction.Type
        );

        Assert.Equal(
            3,
            transaction.Quantity
        );

        Assert.Equal(
            5,
            transaction.QuantityBefore
        );

        Assert.Equal(
            8,
            transaction.QuantityAfter
        );
    }

    // =====================================================
    // TEST 10
    //
    // Dispense prescription
    // invalid prescription ID
    //
    // Expected:
    // 400
    // =====================================================

    [Fact]
    public async Task DispensePrescription_ShouldReturn400_WhenPrescriptionIdIsInvalid()
    {
        await using var scope =
            _factory.Services.CreateAsyncScope();

        var db =
            scope.ServiceProvider
                .GetRequiredService<ClinicManagementDbContext>();

        await db.Database.EnsureCreatedAsync();

        var service =
            scope.ServiceProvider
                .GetRequiredService<IPharmacyService>();

        var request =
            new DispenseRequestDTO
            {
                Items =
                [
                    new DispenseItemRequestDTO
                {
                    PrescriptionItemId = 1,
                    BatchId = 1,
                    Quantity = 1
                }
                ]
            };

        var result =
            await service.DispensePrescriptionAsync(
                0,
                1,
                request
            );

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
    // TEST 11
    //
    // Dispense prescription
    // invalid current user
    //
    // Expected:
    // 401
    // =====================================================

    [Fact]
    public async Task DispensePrescription_ShouldReturn401_WhenUserIsInvalid()
    {
        await using var scope =
            _factory.Services.CreateAsyncScope();

        var db =
            scope.ServiceProvider
                .GetRequiredService<ClinicManagementDbContext>();

        await db.Database.EnsureCreatedAsync();

        var service =
            scope.ServiceProvider
                .GetRequiredService<IPharmacyService>();

        var request =
            new DispenseRequestDTO
            {
                Items =
                [
                    new DispenseItemRequestDTO
                {
                    PrescriptionItemId = 1,
                    BatchId = 1,
                    Quantity = 1
                }
                ]
            };

        var result =
            await service.DispensePrescriptionAsync(
                1,
                0,
                request
            );

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
    // TEST 12
    //
    // Dispense prescription
    // empty request
    //
    // Expected:
    // 400
    // =====================================================

    [Fact]
    public async Task DispensePrescription_ShouldReturn400_WhenRequestIsEmpty()
    {
        await using var scope =
            _factory.Services.CreateAsyncScope();

        var db =
            scope.ServiceProvider
                .GetRequiredService<ClinicManagementDbContext>();

        await db.Database.EnsureCreatedAsync();

        var service =
            scope.ServiceProvider
                .GetRequiredService<IPharmacyService>();

        var result =
            await service.DispensePrescriptionAsync(
                1,
                1,
                new DispenseRequestDTO
                {
                    Items = []
                }
            );

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
    // TEST 13
    //
    // Dispense prescription
    // prescription does not exist
    //
    // Expected:
    // 404
    // =====================================================

    [Fact]
    public async Task DispensePrescription_ShouldReturn404_WhenPrescriptionDoesNotExist()
    {
        await using var scope =
            _factory.Services.CreateAsyncScope();

        var db =
            scope.ServiceProvider
                .GetRequiredService<ClinicManagementDbContext>();

        await db.Database.EnsureCreatedAsync();

        var service =
            scope.ServiceProvider
                .GetRequiredService<IPharmacyService>();

        var request =
            new DispenseRequestDTO
            {
                Items =
                [
                    new DispenseItemRequestDTO
                {
                    PrescriptionItemId = 1,
                    BatchId = 1,
                    Quantity = 1
                }
                ]
            };

        var result =
            await service.DispensePrescriptionAsync(
                999999999,
                1,
                request
            );

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
    // TEST 14
    //
    // Prescription đã Dispensed
    //
    // Expected:
    // 409
    // =====================================================

    [Fact]
    public async Task DispensePrescription_ShouldReturn409_WhenPrescriptionAlreadyDispensed()
    {
        await using var scope =
            _factory.Services.CreateAsyncScope();

        var db =
            scope.ServiceProvider
                .GetRequiredService<ClinicManagementDbContext>();

        await db.Database.EnsureCreatedAsync();

        var scenario =
            await SeedPrescriptionScenarioAsync(
                db,
                status:
                    (byte)PrescriptionStatus.Dispensed,
                itemCount: 1
            );

        var service =
            scope.ServiceProvider
                .GetRequiredService<IPharmacyService>();

        var result =
            await service.DispensePrescriptionAsync(
                scenario.Prescription.Id,
                scenario.User.Id,
                new DispenseRequestDTO
                {
                    Items =
                    [
                        new DispenseItemRequestDTO
                    {
                        PrescriptionItemId =
                            scenario.Items[0].Id,

                        BatchId = 1,

                        Quantity =
                            scenario.Items[0].Quantity
                    }
                    ]
                }
            );

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
    // TEST 15
    //
    // Prescription chưa Finalized
    // Draft = 0
    //
    // Expected:
    // 409
    // =====================================================

    [Fact]
    public async Task DispensePrescription_ShouldReturn409_WhenPrescriptionIsNotFinalized()
    {
        await using var scope =
            _factory.Services.CreateAsyncScope();

        var db =
            scope.ServiceProvider
                .GetRequiredService<ClinicManagementDbContext>();

        await db.Database.EnsureCreatedAsync();

        var scenario =
            await SeedPrescriptionScenarioAsync(
                db,
                status:
                    (byte)PrescriptionStatus.Draft,
                itemCount: 1
            );

        var service =
            scope.ServiceProvider
                .GetRequiredService<IPharmacyService>();

        var result =
            await service.DispensePrescriptionAsync(
                scenario.Prescription.Id,
                scenario.User.Id,
                new DispenseRequestDTO
                {
                    Items =
                    [
                        new DispenseItemRequestDTO
                    {
                        PrescriptionItemId =
                            scenario.Items[0].Id,

                        BatchId = 1,

                        Quantity =
                            scenario.Items[0].Quantity
                    }
                    ]
                }
            );

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
    // TEST 16
    //
    // Prescription Finalized nhưng không có item
    //
    // Expected:
    // 409
    // =====================================================

    [Fact]
    public async Task DispensePrescription_ShouldReturn409_WhenPrescriptionHasNoItems()
    {
        await using var scope =
            _factory.Services.CreateAsyncScope();

        var db =
            scope.ServiceProvider
                .GetRequiredService<ClinicManagementDbContext>();

        await db.Database.EnsureCreatedAsync();

        var scenario =
            await SeedPrescriptionScenarioAsync(
                db,
                status:
                    (byte)PrescriptionStatus.Finalized,
                itemCount: 0
            );

        var service =
            scope.ServiceProvider
                .GetRequiredService<IPharmacyService>();

        var result =
            await service.DispensePrescriptionAsync(
                scenario.Prescription.Id,
                scenario.User.Id,
                new DispenseRequestDTO
                {
                    Items =
                    [
                        new DispenseItemRequestDTO
                    {
                        PrescriptionItemId = 1,
                        BatchId = 1,
                        Quantity = 1
                    }
                    ]
                }
            );

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
    // TEST 17
    //
    // Prescription có 2 items
    // Request chỉ có 1 item
    //
    // Expected:
    // 400
    // =====================================================

    [Fact]
    public async Task DispensePrescription_ShouldReturn400_WhenPrescriptionItemIsMissing()
    {
        await using var scope =
            _factory.Services.CreateAsyncScope();

        var db =
            scope.ServiceProvider
                .GetRequiredService<ClinicManagementDbContext>();

        await db.Database.EnsureCreatedAsync();

        var scenario =
            await SeedPrescriptionScenarioAsync(
                db,
                status:
                    (byte)PrescriptionStatus.Finalized,
                itemCount: 2
            );

        var service =
            scope.ServiceProvider
                .GetRequiredService<IPharmacyService>();

        var prescriptionItems =
    await db.PrescriptionItems
        .Where(
            x =>
                x.PrescriptionId ==
                scenario.Prescription.Id
        )
        .OrderBy(
            x => x.Id
        )
        .ToListAsync();

        Assert.Equal(
            2,
            prescriptionItems.Count
        );

        var firstItem =
            prescriptionItems[0];

        var result =
            await service.DispensePrescriptionAsync(
                scenario.Prescription.Id,
                scenario.User.Id,
                new DispenseRequestDTO
                {
                    Items =
                    [
                        new DispenseItemRequestDTO
                    {
                        PrescriptionItemId =
                            firstItem.Id,

                        BatchId = 1,

                        Quantity =
                            firstItem.Quantity
                    }
                    ]
                }
            );

        Assert.Equal(
            400,
            result.StatusCode
        );

        Assert.StartsWith(
            PharmacyResponseMessageDTO
                .DispenseItemMissing,
            result.Message
        );
    }


    // =====================================================
    // TEST 18
    //
    // Quantity request != quantity prescription
    //
    // Expected:
    // 400
    // =====================================================

    [Fact]
    public async Task DispensePrescription_ShouldReturn400_WhenQuantityIsInvalid()
    {
        await using var scope =
            _factory.Services.CreateAsyncScope();

        var db =
            scope.ServiceProvider
                .GetRequiredService<ClinicManagementDbContext>();

        await db.Database.EnsureCreatedAsync();

        var scenario =
            await SeedPrescriptionScenarioAsync(
                db,
                status:
                    (byte)PrescriptionStatus.Finalized,
                itemCount: 1
            );

        var service =
            scope.ServiceProvider
                .GetRequiredService<IPharmacyService>();

        var prescriptionItem =
            scenario.Items[0];

        var result =
            await service.DispensePrescriptionAsync(
                scenario.Prescription.Id,
                scenario.User.Id,
                new DispenseRequestDTO
                {
                    Items =
                    [
                        new DispenseItemRequestDTO
                    {
                        PrescriptionItemId =
                            prescriptionItem.Id,

                        BatchId = 1,

                        Quantity =
    prescriptionItem.Quantity + 1
                    }
                    ]
                }
            );

        Assert.Equal(
            400,
            result.StatusCode
        );

        Assert.StartsWith(
            PharmacyResponseMessageDTO
                .DispenseQuantityInvalid,
            result.Message
        );
    }


    // =====================================================
    // TEST 19
    //
    // Request chứa PrescriptionItem không thuộc prescription
    //
    // Expected:
    // 400
    // =====================================================

    [Fact]
    public async Task DispensePrescription_ShouldReturn400_WhenRequestContainsExtraPrescriptionItem()
    {
        await using var scope =
            _factory.Services.CreateAsyncScope();

        var db =
            scope.ServiceProvider
                .GetRequiredService<ClinicManagementDbContext>();

        await db.Database.EnsureCreatedAsync();

        var scenario =
            await SeedPrescriptionScenarioAsync(
                db,
                status:
                    (byte)PrescriptionStatus.Finalized,
                itemCount: 1
            );

        var service =
            scope.ServiceProvider
                .GetRequiredService<IPharmacyService>();

        var prescriptionItem =
            scenario.Items[0];

        var result =
            await service.DispensePrescriptionAsync(
                scenario.Prescription.Id,
                scenario.User.Id,
                new DispenseRequestDTO
                {
                    Items =
                    [
                        // Item thật
                        new DispenseItemRequestDTO
                    {
                        PrescriptionItemId =
                            prescriptionItem.Id,

                        BatchId = 1,

                        Quantity =
                            prescriptionItem.Quantity
                    },

                    // Item không thuộc prescription
                    new DispenseItemRequestDTO
                    {
                        PrescriptionItemId =
                            999999999,

                        BatchId = 1,

                        Quantity = 1
                    }
                    ]
                }
            );

        Assert.Equal(
            400,
            result.StatusCode
        );

        Assert.Equal(
            PharmacyResponseMessageDTO
                .PrescriptionItemInvalid,
            result.Message
        );
    }



    // =====================================================
    // HELPER
    // SEED USER
    // =====================================================

    // =====================================================
    // HELPER
    // SEED PRESCRIPTION SCENARIO
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
    //   ↓
    // Prescription
    //   ↓
    // PrescriptionItem(s)
    //
    // =====================================================

    private static async Task<PrescriptionScenario>
        SeedPrescriptionScenarioAsync(
            ClinicManagementDbContext db,
            byte status,
            int itemCount)
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
                    $"it_pharmacy_doctor_{token}",

                PasswordHash =
                    "integration-test-password-hash",

                Email =
                    $"it_pharmacy_doctor_{token}@example.com",

                FullName =
                    "Integration Pharmacy Doctor",

                IsActive = true,

                EmailConfirmed = true,

                IsDeleted = false,

                CreatedAt =
                    DateTime.UtcNow,

                FailedLoginCount = 0
            };


        // =====================================================
        // SPECIALTY
        // =====================================================

        var specialty =
            new Specialty
            {
                Code =
                    $"SP{token[..17]}",

                Name =
                    $"Integration Specialty {token[..8]}",

                Description =
                    "Integration test specialty",

                IsActive = true,

                CreatedAt =
                    DateTime.UtcNow
            };


        // =====================================================
        // DOCTOR
        // =====================================================

        var doctor =
            new Doctor
            {
                User = user,

                Specialty = specialty,

                FullName =
                    "Integration Pharmacy Doctor",

                Title =
                    "Doctor",

                Phone =
                    "0900000001",

                Email =
                    $"doctor_{token}@example.com",

                IsActive = true,

                CreatedAt =
                    DateTime.UtcNow,

                ConsultationFee = 100
            };


        // =====================================================
        // PATIENT
        // =====================================================

        var patient =
            new Patient
            {
                FullName =
                    "Integration Pharmacy Patient",

                Phone =
                    "0900000002",

                Email =
                    $"patient_{token}@example.com",

                PatientCode =
                    $"PT{token[..18]}",

                IsActive = true,

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
                Patient = patient,

                Doctor = doctor,

                StartTime =
                    startTime,

                EndTime =
                    startTime.AddMinutes(30),

                Status = 0,

                Reason =
                    "Integration test",

                AppointmentCode =
                    $"APT{token[..17]}",

                CreatedAt =
                    DateTime.UtcNow,

                Source = 0,

                FeeSnapshot = 100
            };


        // =====================================================
        // MEDICAL RECORD
        // =====================================================

        var medicalRecord =
            new MedicalRecord
            {
                Appointment =
                    appointment,

                Patient =
                    patient,

                Doctor =
                    doctor,

                Symptoms =
                    "Integration test",

                Diagnosis =
                    "Integration test diagnosis",

                CreatedAt =
                    DateTime.UtcNow,

                Status = 0
            };


        // =====================================================
        // PRESCRIPTION
        // =====================================================

        var prescription =
            new Prescription
            {
                MedicalRecord =
                    medicalRecord,

                Doctor =
                    doctor,

                Status =
                    status,

                Note =
                    "Integration test prescription",

                CreatedAt =
                    DateTime.UtcNow
            };


        // =====================================================
        // PRESCRIPTION ITEMS
        // =====================================================

        var items =
            new List<PrescriptionItem>();


        for (var i = 1; i <= itemCount; i++)
        {
            var medicine =
                new Medicine
                {
                    Name =
                        $"IT-Prescription-Medicine-{token[..8]}-{i}",

                    Code =
                        $"IT-PM-{token[..8]}-{i}",

                    ActiveIngredient =
                        $"Ingredient-{i}",

                    Concentration =
                        "500mg",

                    Unit =
                        "tablet",

                    Price = 100,

                    CostPrice = 50,

                    StockQuantity = 10,

                    MinStock = 5,

                    IsActive = true,

                    CreatedAt =
                        DateTime.UtcNow
                };


            var prescriptionItem =
    new PrescriptionItem
    {
        Prescription =
            prescription,

        Medicine =
            medicine,

        Quantity = 2,

        Dosage =
            "1 lần/ngày",

        Instruction =
            "Sau ăn",

        MedicineNameSnapshot =
            medicine.Name,

        UnitPriceSnapshot =
            medicine.Price,

        DurationDays = 5,

        Frequency =
            "1 lần/ngày"
    };


            // =====================================================
            // ADD ITEM TO PRESCRIPTION GRAPH
            // =====================================================

            prescription.PrescriptionItems.Add(
                prescriptionItem
            );


            items.Add(
                prescriptionItem
            );
        }


        // =====================================================
        // SAVE GRAPH
        // =====================================================

        db.Prescriptions.Add(
            prescription
        );

        await db.SaveChangesAsync();


        return new PrescriptionScenario(
            user,
            prescription,
            items
        );
    }


    // =====================================================
    // TEST SCENARIO
    // =====================================================

    private sealed record PrescriptionScenario(
        User User,
        Prescription Prescription,
        List<PrescriptionItem> Items
    );

    private static async Task<User> SeedUserAsync(
        ClinicManagementDbContext db)
    {
        var token =
            Guid.NewGuid()
                .ToString("N");


        var user =
            new User
            {
                Username =
                    $"it_pharmacy_{token}",

                PasswordHash =
                    "integration-test-password-hash",

                Email =
                    $"it_pharmacy_{token}@example.com",

                FullName =
                    "Integration Pharmacy User",

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


        db.Users.Add(
            user
        );

        await db.SaveChangesAsync();


        return user;
    }


    // =====================================================
    // HELPER
    // SEED MEDICINE
    // =====================================================

    private static async Task<Medicine> SeedMedicineAsync(
        ClinicManagementDbContext db,
        string name,
        string code,
        string activeIngredient,
        bool isActive,
        int stockQuantity = 0)
    {
        var medicine =
            new Medicine
            {
                Name =
                    name,

                Code =
                    code,

                ActiveIngredient =
                    activeIngredient,

                Concentration =
                    "500mg",

                Unit =
                    "tablet",

                Price =
                    100,

                CostPrice =
                    50,

                StockQuantity =
                    stockQuantity,

                MinStock =
                    5,

                IsActive =
                    isActive,

                CreatedAt =
                    DateTime.UtcNow
            };


        db.Medicines.Add(
            medicine
        );

        await db.SaveChangesAsync();


        return medicine;
    }
}
