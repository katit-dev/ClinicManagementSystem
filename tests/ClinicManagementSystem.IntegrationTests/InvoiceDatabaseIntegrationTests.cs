using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Net.Http.Json;

using ClinicManagementSystem.Application.DTOs.Payment;
using ClinicManagementSystem.Infrastructure.Data;
using ClinicManagementSystem.Infrastructure.Models;
using ClinicManagementSystem.IntegrationTests;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Xunit;


// =====================================================
// ALIAS
// =====================================================

using InvoiceEntity =
    ClinicManagementSystem.Infrastructure.Models.Invoice;


namespace ClinicManagementSystem.IntegrationTests.Invoice;


// =====================================================
// INVOICE DATABASE INTEGRATION TESTS
// =====================================================
//
// Test 11 - 20
//
// HTTP
//   ↓
// Controller
//   ↓
// InvoiceService
//   ↓
// Repository
//   ↓
// EF Core
//   ↓
// SQL Server Testcontainer
//
// =====================================================

public class InvoiceDatabaseIntegrationTests
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;


    // =====================================================
    // TEST JWT
    // =====================================================

    private const string TestJwtKey =
        "ClinicIntegrationTestSecretKey_2026_AtLeast32Characters!";

    private const string TestJwtIssuer =
        "ClinicManagementSystem.IntegrationTests";

    private const string TestJwtAudience =
        "ClinicManagementSystem.IntegrationTests";


    // =====================================================
    // CONSTRUCTOR
    // =====================================================

    public InvoiceDatabaseIntegrationTests(
        CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }


    // =====================================================
    // TEST 11
    //
    // GET invoice có dữ liệu thật
    //
    // Kiểm tra:
    // - invoice được đọc từ SQL Server
    // - payment được đọc
    // - PaidAmount
    // - RemainingAmount
    // - DTO response
    //
    // Expected: 200
    // =====================================================

    [Fact]
    public async Task GetInvoice_ShouldReturn200_AndReadDataFromDatabase()
    {
        // Arrange

        var data =
            await SeedInvoiceAsync(
                totalAmount: 1_000_000m,
                paidAmount: 300_000m,
                status: 1,
                createPayment: true
            );


        using var client =
            CreateAuthenticatedClient(
                data.UserId
            );


        // Act

        var response =
            await client.GetAsync(
                $"/api/invoices/{data.InvoiceId}"
            );


        // Assert

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode
        );


        var body =
            await response.Content
                .ReadFromJsonAsync<JsonElement>();


        Assert.Equal(
            200,
            body
                .GetProperty("statusCode")
                .GetInt32()
        );


        var content =
            body.GetProperty("content");


        Assert.Equal(
            data.InvoiceId,
            content
                .GetProperty("id")
                .GetInt32()
        );


        Assert.Equal(
            300_000m,
            content
                .GetProperty("paidAmount")
                .GetDecimal()
        );


        Assert.Equal(
            700_000m,
            content
                .GetProperty("remainingAmount")
                .GetDecimal()
        );


        Assert.Equal(
            1,
            content
                .GetProperty("payments")
                .GetArrayLength()
        );
    }


    // =====================================================
    // TEST 12
    //
    // GET invoice không tồn tại
    //
    // Expected: 404
    // =====================================================

    [Fact]
    public async Task GetInvoice_ShouldReturn404_WhenInvoiceDoesNotExist()
    {
        // Arrange

        await EnsureDatabaseAsync();


        using var client =
            CreateAuthenticatedClient(
                999999
            );


        // Act

        var response =
            await client.GetAsync(
                "/api/invoices/2147483647"
            );


        // Assert

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode
        );
    }


    // =====================================================
    // TEST 13
    //
    // CREATE PAYMENT
    //
    // Invoice:
    // Total = 1,000,000
    // Paid  = 0
    //
    // Payment:
    // 300,000
    //
    // Expected:
    // - 200
    // - PaidAmount = 300,000
    // - Status = PartiallyPaid
    // - Payment được INSERT
    // - AuditLog được INSERT
    // =====================================================

    [Fact]
    public async Task CreatePayment_ShouldPersistPayment_AndSetPartiallyPaid()
    {
        // Arrange

        var data =
            await SeedInvoiceAsync(
                totalAmount: 1_000_000m,
                paidAmount: 0m,
                status: 0
            );


        using var client =
            CreateAuthenticatedClient(
                data.UserId
            );


        var request =
            new PaymentRequestDTO
            {
                Amount = 300_000m,

                Method = 0,

                ReferenceCode =
                    "IT-PAY-13",

                Note =
                    "Integration test payment",

                IsRefund = false
            };


        // Act

        var response =
            await client.PostAsJsonAsync(
                $"/api/invoices/{data.InvoiceId}/payments",
                request
            );


        // Assert - HTTP

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode
        );


        // Assert - DATABASE

        await using var scope =
            _factory.Services
                .CreateAsyncScope();


        var db =
            scope.ServiceProvider
                .GetRequiredService<
                    ClinicManagementDbContext
                >();


        var invoice =
            await db.Invoices
                .SingleAsync(
                    i =>
                        i.Id ==
                        data.InvoiceId
                );


        Assert.Equal(
            300_000m,
            invoice.PaidAmount
        );


        Assert.Equal(
            1,
            invoice.Status
        );


        var payment =
            await db.Payments
                .SingleAsync(
                    p =>
                        p.InvoiceId ==
                        data.InvoiceId &&
                        p.ReferenceCode ==
                        "IT-PAY-13"
                );


        Assert.Equal(
            300_000m,
            payment.Amount
        );


        Assert.False(
            payment.IsRefund
        );


        Assert.Equal(
            data.UserId,
            payment.ReceivedBy
        );


        var auditExists =
            await db.AuditLogs
                .AnyAsync(
                    a =>
                        a.EntityName ==
                            "Invoice" &&

                        a.EntityId ==
                            data.InvoiceId &&

                        a.Action ==
                            "CREATE_PAYMENT" &&

                        a.UserId ==
                            data.UserId
                );


        Assert.True(
            auditExists
        );
    }


    // =====================================================
    // TEST 14
    //
    // CREATE PAYMENT FULL
    //
    // Total = 1,000,000
    // Pay   = 1,000,000
    //
    // Expected:
    // - 200
    // - PaidAmount = 1,000,000
    // - Status = Paid
    // =====================================================

    [Fact]
    public async Task CreatePayment_ShouldSetPaid_WhenFullAmountIsPaid()
    {
        // Arrange

        var data =
            await SeedInvoiceAsync(
                totalAmount: 1_000_000m,
                paidAmount: 0m,
                status: 0
            );


        using var client =
            CreateAuthenticatedClient(
                data.UserId
            );


        var request =
            new PaymentRequestDTO
            {
                Amount =
                    1_000_000m,

                Method = 0,

                ReferenceCode =
                    "IT-PAY-14",

                IsRefund = false
            };


        // Act

        var response =
            await client.PostAsJsonAsync(
                $"/api/invoices/{data.InvoiceId}/payments",
                request
            );


        // Assert

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode
        );


        // Check database

        await using var scope =
            _factory.Services
                .CreateAsyncScope();


        var db =
            scope.ServiceProvider
                .GetRequiredService<
                    ClinicManagementDbContext
                >();


        var invoice =
            await db.Invoices
                .SingleAsync(
                    i =>
                        i.Id ==
                        data.InvoiceId
                );


        Assert.Equal(
            1_000_000m,
            invoice.PaidAmount
        );


        Assert.Equal(
            2,
            invoice.Status
        );


        var paymentExists =
            await db.Payments
                .AnyAsync(
                    p =>
                        p.InvoiceId ==
                            data.InvoiceId &&

                        p.Amount ==
                            1_000_000m &&

                        !p.IsRefund
                );


        Assert.True(
            paymentExists
        );
    }


    // =====================================================
    // TEST 15
    //
    // CREATE PAYMENT
    //
    // Total = 1,000,000
    // Paid  = 300,000
    //
    // Request = 700,001
    //
    // Expected:
    // - 400
    // - PaidAmount unchanged
    // - Không INSERT payment
    // =====================================================

    [Fact]
    public async Task CreatePayment_ShouldReturn400_AndNotPersist_WhenAmountExceedsRemaining()
    {
        // Arrange

        var data =
            await SeedInvoiceAsync(
                totalAmount: 1_000_000m,
                paidAmount: 300_000m,
                status: 1
            );


        using var client =
            CreateAuthenticatedClient(
                data.UserId
            );


        var request =
            new PaymentRequestDTO
            {
                Amount =
                    700_001m,

                Method = 0,

                ReferenceCode =
                    "IT-PAY-15",

                IsRefund = false
            };


        // Act

        var response =
            await client.PostAsJsonAsync(
                $"/api/invoices/{data.InvoiceId}/payments",
                request
            );


        // Assert

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode
        );


        // Check database

        await using var scope =
            _factory.Services
                .CreateAsyncScope();


        var db =
            scope.ServiceProvider
                .GetRequiredService<
                    ClinicManagementDbContext
                >();


        var invoice =
            await db.Invoices
                .SingleAsync(
                    i =>
                        i.Id ==
                        data.InvoiceId
                );


        Assert.Equal(
            300_000m,
            invoice.PaidAmount
        );


        var paymentExists =
            await db.Payments
                .AnyAsync(
                    p =>
                        p.InvoiceId ==
                            data.InvoiceId &&

                        p.ReferenceCode ==
                            "IT-PAY-15"
                );


        Assert.False(
            paymentExists
        );
    }


    // =====================================================
    // TEST 16
    //
    // CREATE PAYMENT
    //
    // Invoice đã Cancelled
    //
    // Expected:
    // - 400
    // - Không tạo payment
    // =====================================================

    [Fact]
    public async Task CreatePayment_ShouldReturn400_WhenInvoiceIsCancelled()
    {
        // Arrange

        var data =
            await SeedInvoiceAsync(
                totalAmount: 1_000_000m,
                paidAmount: 0m,
                status: 3
            );


        using var client =
            CreateAuthenticatedClient(
                data.UserId
            );


        var request =
            new PaymentRequestDTO
            {
                Amount =
                    100_000m,

                Method = 0,

                ReferenceCode =
                    "IT-PAY-16",

                IsRefund = false
            };


        // Act

        var response =
            await client.PostAsJsonAsync(
                $"/api/invoices/{data.InvoiceId}/payments",
                request
            );


        // Assert

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode
        );


        // Check database

        await using var scope =
            _factory.Services
                .CreateAsyncScope();


        var db =
            scope.ServiceProvider
                .GetRequiredService<
                    ClinicManagementDbContext
                >();


        var paymentExists =
            await db.Payments
                .AnyAsync(
                    p =>
                        p.InvoiceId ==
                            data.InvoiceId &&

                        p.ReferenceCode ==
                            "IT-PAY-16"
                );


        Assert.False(
            paymentExists
        );


        var invoice =
            await db.Invoices
                .SingleAsync(
                    i =>
                        i.Id ==
                        data.InvoiceId
                );


        Assert.Equal(
            0m,
            invoice.PaidAmount
        );


        Assert.Equal(
            3,
            invoice.Status
        );
    }


    // =====================================================
    // TEST 17
    //
    // REFUND SUCCESS
    //
    // Total = 1,000,000
    // Paid  = 1,000,000
    //
    // Refund = 200,000
    //
    // Expected:
    // - 200
    // - PaidAmount = 800,000
    // - Status = PartiallyPaid
    // - Payment refund = -200,000
    // - IsRefund = true
    // =====================================================

    [Fact]
    public async Task RefundPayment_ShouldPersistRefund_AndDecreasePaidAmount()
    {
        // Arrange

        var data =
            await SeedInvoiceAsync(
                totalAmount: 1_000_000m,
                paidAmount: 1_000_000m,
                status: 2,
                createPayment: true
            );


        using var client =
            CreateAuthenticatedClient(
                data.UserId
            );


        var request =
            new PaymentRequestDTO
            {
                Amount =
                    200_000m,

                Method = 0,

                ReferenceCode =
                    "IT-REFUND-17",

                Note =
                    "Integration test refund",

                IsRefund = true
            };


        // Act

        var response =
            await client.PostAsJsonAsync(
                $"/api/invoices/{data.InvoiceId}/refunds",
                request
            );


        // Assert

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode
        );


        // Check database

        await using var scope =
            _factory.Services
                .CreateAsyncScope();


        var db =
            scope.ServiceProvider
                .GetRequiredService<
                    ClinicManagementDbContext
                >();


        var invoice =
            await db.Invoices
                .SingleAsync(
                    i =>
                        i.Id ==
                        data.InvoiceId
                );


        Assert.Equal(
            800_000m,
            invoice.PaidAmount
        );


        Assert.Equal(
            1,
            invoice.Status
        );


        var refund =
            await db.Payments
                .SingleAsync(
                    p =>
                        p.InvoiceId ==
                            data.InvoiceId &&

                        p.ReferenceCode ==
                            "IT-REFUND-17"
                );


        Assert.Equal(
            -200_000m,
            refund.Amount
        );


        Assert.True(
            refund.IsRefund
        );


        Assert.Equal(
            data.UserId,
            refund.ReceivedBy
        );


        var auditExists =
            await db.AuditLogs
                .AnyAsync(
                    a =>
                        a.EntityName ==
                            "Invoice" &&

                        a.EntityId ==
                            data.InvoiceId &&

                        a.Action ==
                            "REFUND_INVOICE" &&

                        a.UserId ==
                            data.UserId
                );


        Assert.True(
            auditExists
        );
    }


    // =====================================================
    // TEST 18
    //
    // REFUND vượt quá PaidAmount
    //
    // Paid = 300,000
    // Refund = 400,000
    //
    // Expected:
    // - 400
    // - PaidAmount unchanged
    // - Không tạo refund payment
    // =====================================================

    [Fact]
    public async Task RefundPayment_ShouldReturn400_AndNotPersist_WhenRefundExceedsPaidAmount()
    {
        // Arrange

        var data =
            await SeedInvoiceAsync(
                totalAmount: 1_000_000m,
                paidAmount: 300_000m,
                status: 1,
                createPayment: true
            );


        using var client =
            CreateAuthenticatedClient(
                data.UserId
            );


        var request =
            new PaymentRequestDTO
            {
                Amount =
                    400_000m,

                Method = 0,

                ReferenceCode =
                    "IT-REFUND-18",

                IsRefund = true
            };


        // Act

        var response =
            await client.PostAsJsonAsync(
                $"/api/invoices/{data.InvoiceId}/refunds",
                request
            );


        // Assert

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode
        );


        // Check database

        await using var scope =
            _factory.Services
                .CreateAsyncScope();


        var db =
            scope.ServiceProvider
                .GetRequiredService<
                    ClinicManagementDbContext
                >();


        var invoice =
            await db.Invoices
                .SingleAsync(
                    i =>
                        i.Id ==
                        data.InvoiceId
                );


        Assert.Equal(
            300_000m,
            invoice.PaidAmount
        );


        var refundExists =
            await db.Payments
                .AnyAsync(
                    p =>
                        p.InvoiceId ==
                            data.InvoiceId &&

                        p.ReferenceCode ==
                            "IT-REFUND-18"
                );


        Assert.False(
            refundExists
        );
    }


    // =====================================================
    // TEST 19
    //
    // CANCEL INVOICE
    //
    // PaidAmount = 0
    //
    // Expected:
    // - 200
    // - Status = Cancelled
    // - CancelledAt != null
    // - CancelReason được lưu
    // - AuditLog được lưu
    // =====================================================

    [Fact]
    public async Task CancelInvoice_ShouldPersistCancellation_WhenInvoiceIsUnpaid()
    {
        // Arrange

        var data =
            await SeedInvoiceAsync(
                totalAmount: 1_000_000m,
                paidAmount: 0m,
                status: 0
            );


        using var client =
            CreateAuthenticatedClient(
                data.UserId
            );


        var request =
            new
            {
                cancelReason =
                    "Integration test cancellation"
            };


        // Act

        var response =
            await client.PatchAsJsonAsync(
                $"/api/invoices/{data.InvoiceId}/cancel",
                request
            );


        // Assert

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode
        );


        // Check database

        await using var scope =
            _factory.Services
                .CreateAsyncScope();


        var db =
            scope.ServiceProvider
                .GetRequiredService<
                    ClinicManagementDbContext
                >();


        var invoice =
            await db.Invoices
                .SingleAsync(
                    i =>
                        i.Id ==
                        data.InvoiceId
                );


        Assert.Equal(
            3,
            invoice.Status
        );


        Assert.NotNull(
            invoice.CancelledAt
        );


        Assert.Equal(
            "Integration test cancellation",
            invoice.CancelReason
        );


        var auditExists =
            await db.AuditLogs
                .AnyAsync(
                    a =>
                        a.EntityName ==
                            "Invoice" &&

                        a.EntityId ==
                            data.InvoiceId &&

                        a.Action ==
                            "CANCEL_INVOICE" &&

                        a.UserId ==
                            data.UserId
                );


        Assert.True(
            auditExists
        );
    }


    // =====================================================
    // TEST 20
    //
    // CANCEL INVOICE
    //
    // PaidAmount > 0
    //
    // Expected:
    // - 400
    // - Không chuyển sang Cancelled
    // - Không có CancelledAt
    // =====================================================

    [Fact]
    public async Task CancelInvoice_ShouldReturn400_AndNotCancel_WhenInvoiceHasPaidAmount()
    {
        // Arrange

        var data =
            await SeedInvoiceAsync(
                totalAmount: 1_000_000m,
                paidAmount: 300_000m,
                status: 1
            );


        using var client =
            CreateAuthenticatedClient(
                data.UserId
            );


        var request =
            new
            {
                cancelReason =
                    "Should not cancel"
            };


        // Act

        var response =
            await client.PatchAsJsonAsync(
                $"/api/invoices/{data.InvoiceId}/cancel",
                request
            );


        // Assert

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode
        );


        // Check database

        await using var scope =
            _factory.Services
                .CreateAsyncScope();


        var db =
            scope.ServiceProvider
                .GetRequiredService<
                    ClinicManagementDbContext
                >();


        var invoice =
            await db.Invoices
                .SingleAsync(
                    i =>
                        i.Id ==
                        data.InvoiceId
                );


        Assert.Equal(
            300_000m,
            invoice.PaidAmount
        );


        Assert.Equal(
            1,
            invoice.Status
        );


        Assert.Null(
            invoice.CancelledAt
        );


        Assert.Null(
            invoice.CancelReason
        );
    }


    // =====================================================
    // DATABASE INITIALIZATION
    // =====================================================

    private async Task EnsureDatabaseAsync()
    {
        await using var scope =
            _factory.Services
                .CreateAsyncScope();


        var db =
            scope.ServiceProvider
                .GetRequiredService<
                    ClinicManagementDbContext
                >();


        await db.Database
            .EnsureCreatedAsync();
    }


    // =====================================================
    // SEED TEST INVOICE
    // =====================================================

    private async Task<TestInvoiceData>
        SeedInvoiceAsync(
            decimal totalAmount,
            decimal paidAmount,
            byte status,
            bool createPayment = false)
    {
        await EnsureDatabaseAsync();


        await using var scope =
            _factory.Services
                .CreateAsyncScope();


        var db =
            scope.ServiceProvider
                .GetRequiredService<
                    ClinicManagementDbContext
                >();


        var suffix =
            Guid.NewGuid()
                .ToString("N");


        var now =
            DateTime.UtcNow;


        // =================================================
        // USER
        // =================================================

        var user =
            new User
            {
                Username =
                    $"it_{suffix}",

                PasswordHash =
                    "integration-test-password-hash",

                Email =
                    $"it_{suffix}@example.test",

                FullName =
                    "Integration Test Receptionist",

                IsActive =
                    true,

                IsDeleted =
                    false,

                EmailConfirmed =
                    true,

                FailedLoginCount =
                    0,

                CreatedAt =
                    now
            };


        db.Users.Add(
            user
        );


        // =================================================
        // PATIENT
        // =================================================

        var patient =
            new Patient
            {
                FullName =
                    "Integration Test Patient",

                PatientCode =
                    $"PT{suffix[..12]}",

                IsActive =
                    true,

                CreatedAt =
                    now
            };


        db.Patients.Add(
            patient
        );


        // =================================================
        // SAVE USER + PATIENT
        // =================================================

        await db.SaveChangesAsync();


        // =================================================
        // INVOICE
        // =================================================

        var invoice =
            new InvoiceEntity
            {
                PatientId =
                    patient.Id,

                PatientName =
                    patient.FullName,

                InvoiceNo =
                    $"IT-{suffix[..20]}",

                TotalAmount =
                    totalAmount,

                DiscountAmount =
                    0m,

                TaxAmount =
                    0m,

                InsuranceAmount =
                    0m,

                PaidAmount =
                    paidAmount,

                Status =
                    status,

                CreatedBy =
                    user.Id,

                CreatedAt =
                    now
            };


        db.Invoices.Add(
            invoice
        );


        // =================================================
        // SAVE INVOICE
        // =================================================

        await db.SaveChangesAsync();


        // =================================================
        // OPTIONAL INITIAL PAYMENT
        // =================================================

        if (createPayment &&
            paidAmount > 0)
        {
            var payment =
                new Payment
                {
                    InvoiceId =
                        invoice.Id,

                    Amount =
                        paidAmount,

                    Method =
                        0,

                    PaidAt =
                        now,

                    ReferenceCode =
                        $"SEED-{suffix[..12]}",

                    ReceivedBy =
                        user.Id,

                    IsRefund =
                        false
                };


            db.Payments.Add(
                payment
            );


            await db.SaveChangesAsync();
        }


        return new TestInvoiceData(
            user.Id,
            patient.Id,
            invoice.Id
        );
    }


    // =====================================================
    // AUTHENTICATED HTTP CLIENT
    // =====================================================

    private HttpClient
        CreateAuthenticatedClient(
            int userId)
    {
        var client =
            _factory.CreateClient();


        var token =
            CreateTestJwt(
                userId,
                "Receptionist"
            );


        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                token
            );


        return client;
    }


    // =====================================================
    // CREATE TEST JWT
    // =====================================================

    private static string CreateTestJwt(
        int userId,
        string role)
    {
        var now =
            DateTimeOffset.UtcNow;


        // =================================================
        // HEADER
        // =================================================

        var header =
            new Dictionary<string, object>
            {
                ["alg"] =
                    "HS256",

                ["typ"] =
                    "JWT"
            };


        // =================================================
        // PAYLOAD
        // =================================================

        var payload =
            new Dictionary<string, object>
            {
                ["iss"] =
                    TestJwtIssuer,

                ["aud"] =
                    TestJwtAudience,

                ["iat"] =
                    now.ToUnixTimeSeconds(),

                ["nbf"] =
                    now
                        .AddSeconds(-5)
                        .ToUnixTimeSeconds(),

                ["exp"] =
                    now
                        .AddMinutes(10)
                        .ToUnixTimeSeconds(),

                ["UserName"] =
                    "integration-test",

                [ClaimTypes.NameIdentifier] =
                    userId.ToString(),

                [ClaimTypes.Role] =
                    role
            };


        // =================================================
        // SERIALIZE
        // =================================================

        var headerJson =
            JsonSerializer.Serialize(
                header
            );


        var payloadJson =
            JsonSerializer.Serialize(
                payload
            );


        // =================================================
        // BASE64URL
        // =================================================

        var encodedHeader =
            Base64UrlEncode(
                Encoding.UTF8.GetBytes(
                    headerJson
                )
            );


        var encodedPayload =
            Base64UrlEncode(
                Encoding.UTF8.GetBytes(
                    payloadJson
                )
            );


        // =================================================
        // SIGNING INPUT
        // =================================================

        var signingInput =
            $"{encodedHeader}.{encodedPayload}";


        // =================================================
        // SIGNATURE
        // =================================================

        using var hmac =
            new HMACSHA256(
                Encoding.UTF8.GetBytes(
                    TestJwtKey
                )
            );


        var signature =
            hmac.ComputeHash(
                Encoding.UTF8.GetBytes(
                    signingInput
                )
            );


        var encodedSignature =
            Base64UrlEncode(
                signature
            );


        // =================================================
        // JWT
        // =================================================

        return
            $"{signingInput}.{encodedSignature}";
    }


    // =====================================================
    // BASE64URL
    // =====================================================

    private static string Base64UrlEncode(
        byte[] data)
    {
        return Convert
            .ToBase64String(data)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }


    // =====================================================
    // TEST DATA
    // =====================================================

    private sealed record TestInvoiceData(
        int UserId,
        int PatientId,
        int InvoiceId
    );
}