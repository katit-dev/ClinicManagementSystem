using ClinicManagementSystem.Application.DTOs.Pharmacy;
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
    // HELPER
    // SEED USER
    // =====================================================

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
