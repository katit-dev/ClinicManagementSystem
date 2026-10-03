using ClinicManagementSystem.Infrastructure.Data;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Xunit;


namespace ClinicManagementSystem.IntegrationTests;


// =====================================================
// INTEGRATION INFRASTRUCTURE TESTS
// =====================================================

public class IntegrationInfrastructureTests
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;


    public IntegrationInfrastructureTests(
        CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }


    // =================================================
    // TEST 01
    //
    // Kiểm tra:
    // - SQL Server container
    // - API test host
    // - DI
    // - DbContext
    // - Database connection
    // - Database schema
    // =================================================

    [Fact]
    public async Task TestInfrastructure_ShouldCreateDatabaseAndConnect()
    {
        // =============================================
        // Arrange
        // =============================================

        using var scope =
            _factory.Services.CreateScope();

        var dbContext =
            scope.ServiceProvider
                .GetRequiredService<
                    ClinicManagementDbContext
                >();


        // =============================================
        // Act
        // =============================================

        await dbContext.Database.EnsureCreatedAsync();

        var canConnect =
            await dbContext.Database.CanConnectAsync();


        // =============================================
        // Assert
        // =============================================

        Assert.True(canConnect);
    }
}