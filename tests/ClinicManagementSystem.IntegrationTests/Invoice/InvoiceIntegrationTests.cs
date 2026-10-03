using System.Net;

using Xunit;


namespace ClinicManagementSystem.IntegrationTests.Invoice;


// =====================================================
// INVOICE INTEGRATION TESTS
// =====================================================

public class InvoiceIntegrationTests
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;


    // =====================================================
    // CONSTRUCTOR
    // =====================================================

    public InvoiceIntegrationTests(
        CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }


    // =====================================================
    // TEST 01
    // GET INVOICE - UNAUTHORIZED
    // =====================================================

    [Fact]
    public async Task GetInvoice_ShouldReturn401_WhenUserIsNotAuthenticated()
    {
        // =============================================
        // Act
        // =============================================

        var response =
            await _client.GetAsync(
                "/api/invoices/1"
            );


        // =============================================
        // Assert
        // =============================================

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode
        );
    }
}