using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

using Testcontainers.MsSql;

using Xunit;


namespace ClinicManagementSystem.IntegrationTests;


// =====================================================
// CUSTOM WEB APPLICATION FACTORY
// =====================================================
//
// xUnit
//     ↓
// WebApplicationFactory
//     ↓
// ClinicManagementSystem.Api
//     ↓
// SQL Server Testcontainer
//
// Container chỉ dùng cho Integration Test.
// =====================================================

public class CustomWebApplicationFactory
    : WebApplicationFactory<Program>,
      IAsyncLifetime
{
    // =================================================
    // SQL SERVER TEST CONTAINER
    // =================================================

    private readonly MsSqlContainer _sqlServer =
        new MsSqlBuilder(
            "mcr.microsoft.com/mssql/server:2022-CU14-ubuntu-22.04"
        )
        .Build();


    // =================================================
    // TEST DATABASE CONNECTION STRING
    // =================================================

    public string ConnectionString =>
        _sqlServer.GetConnectionString();


    // =================================================
    // INITIALIZE
    // =================================================

    public async Task InitializeAsync()
    {
        await _sqlServer.StartAsync();
    }


    // =================================================
    // CONFIGURE API TEST HOST
    // =================================================

    protected override void ConfigureWebHost(
        IWebHostBuilder builder)
    {
        // =============================================
        // TEST ENVIRONMENT
        // =============================================

        builder.UseEnvironment(
            "Testing"
        );


        // =============================================
        // TEST DATABASE
        // =============================================

        builder.UseSetting(
            "ConnectionStrings:ClinicDb",
            _sqlServer.GetConnectionString()
        );


        // =============================================
        // TEST JWT
        // =============================================

        builder.UseSetting(
            "Jwt:Key",
            "ClinicIntegrationTestSecretKey_2026_AtLeast32Characters!"
        );

        builder.UseSetting(
            "Jwt:Issuer",
            "ClinicManagementSystem.IntegrationTests"
        );

        builder.UseSetting(
            "Jwt:Audience",
            "ClinicManagementSystem.IntegrationTests"
        );
    }


    // =================================================
    // WEB APPLICATION FACTORY DISPOSAL
    // =================================================
    //
    // WebApplicationFactory.DisposeAsync()
    // trả về ValueTask.
    //
    // Override để đảm bảo SQL Server container cũng
    // được dispose.
    // =================================================

    public override async ValueTask DisposeAsync()
    {
        await _sqlServer.DisposeAsync();

        await base.DisposeAsync();
    }


    // =================================================
    // xUnit 2 IAsyncLifetime DISPOSAL
    // =================================================
    //
    // xUnit 2.9.3 yêu cầu:
    //
    // Task DisposeAsync()
    //
    // Explicit interface implementation giúp tránh
    // CS0114 với DisposeAsync() của WebApplicationFactory.
    // =================================================

    Task IAsyncLifetime.DisposeAsync()
    {
        return DisposeAsync().AsTask();
    }
}