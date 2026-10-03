using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

using ClinicManagementSystem.Application.DTOs.Payment;

using Xunit;


namespace ClinicManagementSystem.IntegrationTests.Invoice;


// =====================================================
// INVOICE INTEGRATION TESTS
// =====================================================
//
// HTTP
//  ↓
// InvoiceController
//  ↓
// InvoiceService
//  ↓
// Repository / EF Core
//  ↓
// SQL Server Testcontainer
//
// Test 01 - 10
// =====================================================

public class InvoiceIntegrationTests
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;


    // =====================================================
    // TEST JWT CONFIGURATION
    //
    // Phải giống CustomWebApplicationFactory.cs
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

    public InvoiceIntegrationTests(
        CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }


    // =====================================================
    // TEST 01
    //
    // GET invoice
    //
    // Không có authentication
    //
    // Expected: 401
    // =====================================================

    [Fact]
    public async Task GetInvoice_ShouldReturn401_WhenUserIsNotAuthenticated()
    {
        // Arrange

        using var client =
            _factory.CreateClient();


        // Act

        var response =
            await client.GetAsync(
                "/api/invoices/1"
            );


        // Assert

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode
        );
    }


    // =====================================================
    // TEST 02
    //
    // GET invoice
    //
    // Receptionist hợp lệ
    // invoiceId = 0
    //
    // InvoiceService:
    // invoiceId <= 0 → 400
    // =====================================================

    [Fact]
    public async Task GetInvoice_ShouldReturn400_WhenInvoiceIdIsZero()
    {
        // Arrange

        using var client =
            CreateAuthenticatedClient(
                "Receptionist"
            );


        // Act

        var response =
            await client.GetAsync(
                "/api/invoices/0"
            );


        // Assert

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode
        );
    }


    // =====================================================
    // TEST 03
    //
    // GET invoice
    //
    // User có role Patient
    //
    // Endpoint yêu cầu Receptionist
    //
    // Expected: 403
    // =====================================================

    [Fact]
    public async Task GetInvoice_ShouldReturn403_WhenUserIsNotReceptionist()
    {
        // Arrange

        using var client =
            CreateAuthenticatedClient(
                "Patient"
            );


        // Act

        var response =
            await client.GetAsync(
                "/api/invoices/1"
            );


        // Assert

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode
        );
    }


    // =====================================================
    // TEST 04
    //
    // CREATE PAYMENT
    //
    // Không authentication
    //
    // Expected: 401
    // =====================================================

    [Fact]
    public async Task CreatePayment_ShouldReturn401_WhenUserIsNotAuthenticated()
    {
        // Arrange

        using var client =
            _factory.CreateClient();


        var request =
            new PaymentRequestDTO
            {
                Amount = 100000,
                Method = 0,
                IsRefund = false
            };


        // Act

        var response =
            await client.PostAsJsonAsync(
                "/api/invoices/1/payments",
                request
            );


        // Assert

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode
        );
    }


    // =====================================================
    // TEST 05
    //
    // REFUND PAYMENT
    //
    // Không authentication
    //
    // Expected: 401
    // =====================================================

    [Fact]
    public async Task RefundPayment_ShouldReturn401_WhenUserIsNotAuthenticated()
    {
        // Arrange

        using var client =
            _factory.CreateClient();


        var request =
            new PaymentRequestDTO
            {
                Amount = 100000,
                Method = 0,
                IsRefund = true
            };


        // Act

        var response =
            await client.PostAsJsonAsync(
                "/api/invoices/1/refunds",
                request
            );


        // Assert

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode
        );
    }


    // =====================================================
    // TEST 06
    //
    // CANCEL INVOICE
    //
    // Không authentication
    //
    // Expected: 401
    // =====================================================

    [Fact]
    public async Task CancelInvoice_ShouldReturn401_WhenUserIsNotAuthenticated()
    {
        // Arrange

        using var client =
            _factory.CreateClient();


        var request =
            new
            {
                cancelReason = "Integration test"
            };


        // Act

        var response =
            await client.PatchAsJsonAsync(
                "/api/invoices/1/cancel",
                request
            );


        // Assert

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode
        );
    }


    // =====================================================
    // TEST 07
    //
    // CREATE PAYMENT
    //
    // Receptionist hợp lệ
    // nhưng IsRefund = true
    //
    // CreatePaymentAsync:
    // IsRefund == true → 400
    //
    // Không cần invoice trong DB vì Service reject
    // trước khi query invoice.
    // =====================================================

    [Fact]
    public async Task CreatePayment_ShouldReturn400_WhenRequestIsRefund()
    {
        // Arrange

        using var client =
            CreateAuthenticatedClient(
                "Receptionist"
            );


        var request =
            new PaymentRequestDTO
            {
                Amount = 100000,
                Method = 0,
                IsRefund = true
            };


        // Act

        var response =
            await client.PostAsJsonAsync(
                "/api/invoices/1/payments",
                request
            );


        // Assert

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode
        );
    }


    // =====================================================
    // TEST 08
    //
    // REFUND PAYMENT
    //
    // Receptionist hợp lệ
    // nhưng IsRefund = false
    //
    // RefundPaymentAsync:
    // IsRefund == false → 400
    //
    // Không cần invoice trong DB vì Service reject
    // trước khi query invoice.
    // =====================================================

    [Fact]
    public async Task RefundPayment_ShouldReturn400_WhenRequestIsNotRefund()
    {
        // Arrange

        using var client =
            CreateAuthenticatedClient(
                "Receptionist"
            );


        var request =
            new PaymentRequestDTO
            {
                Amount = 100000,
                Method = 0,
                IsRefund = false
            };


        // Act

        var response =
            await client.PostAsJsonAsync(
                "/api/invoices/1/refunds",
                request
            );


        // Assert

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode
        );
    }


    // =====================================================
    // TEST 09
    //
    // CREATE PAYMENT
    //
    // Method = 4
    //
    // PaymentRequestDTO:
    // Method phải nằm trong 0..3
    //
    // [ApiController] sẽ trả 400.
    // =====================================================

    [Fact]
    public async Task CreatePayment_ShouldReturn400_WhenPaymentMethodIsInvalid()
    {
        // Arrange

        using var client =
            CreateAuthenticatedClient(
                "Receptionist"
            );


        var request =
            new PaymentRequestDTO
            {
                Amount = 100000,
                Method = 4,
                IsRefund = false
            };


        // Act

        var response =
            await client.PostAsJsonAsync(
                "/api/invoices/1/payments",
                request
            );


        // Assert

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode
        );
    }


    // =====================================================
    // TEST 10
    //
    // REFUND PAYMENT
    //
    // Method = 4
    //
    // PaymentRequestDTO:
    // Method phải nằm trong 0..3
    //
    // [ApiController] sẽ trả 400.
    // =====================================================

    [Fact]
    public async Task RefundPayment_ShouldReturn400_WhenPaymentMethodIsInvalid()
    {
        // Arrange

        using var client =
            CreateAuthenticatedClient(
                "Receptionist"
            );


        var request =
            new PaymentRequestDTO
            {
                Amount = 100000,
                Method = 4,
                IsRefund = true
            };


        // Act

        var response =
            await client.PostAsJsonAsync(
                "/api/invoices/1/refunds",
                request
            );


        // Assert

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode
        );
    }


    // =====================================================
    // AUTHENTICATED CLIENT
    // =====================================================

    private HttpClient CreateAuthenticatedClient(
        string role)
    {
        var client =
            _factory.CreateClient();


        var token =
            CreateTestJwt(role);


        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                token
            );


        return client;
    }


    // =====================================================
    // CREATE TEST JWT
    //
    // Không cần thêm package JWT.
    //
    // Token dùng:
    // HS256
    // Issuer
    // Audience
    // Expiration
    // NameIdentifier
    // Role
    // UserName
    // =====================================================

    private static string CreateTestJwt(
        string role)
    {
        var now =
            DateTimeOffset.UtcNow;


        // =============================================
        // HEADER
        // =============================================

        var header =
            new Dictionary<string, object>
            {
                ["alg"] = "HS256",
                ["typ"] = "JWT"
            };


        // =============================================
        // PAYLOAD
        // =============================================

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
                    "999001",

                [ClaimTypes.Role] =
                    role
            };


        // =============================================
        // SERIALIZE
        // =============================================

        var headerJson =
            JsonSerializer.Serialize(
                header
            );


        var payloadJson =
            JsonSerializer.Serialize(
                payload
            );


        // =============================================
        // BASE64URL
        // =============================================

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


        // =============================================
        // SIGNING INPUT
        // =============================================

        var signingInput =
            $"{encodedHeader}.{encodedPayload}";


        // =============================================
        // SIGNATURE
        // =============================================

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


        // =============================================
        // FINAL JWT
        // =============================================

        return
            $"{signingInput}.{encodedSignature}";
    }


    // =====================================================
    // BASE64URL ENCODER
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
}