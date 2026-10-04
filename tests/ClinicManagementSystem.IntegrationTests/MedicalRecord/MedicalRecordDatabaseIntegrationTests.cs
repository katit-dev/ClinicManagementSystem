using System.Net;
using System.Net.Http.Json;

using ClinicManagementSystem.Application.DTOs.MedicalRecord;

using Xunit;

namespace ClinicManagementSystem.IntegrationTests.MedicalRecord;


// =====================================================
// MEDICAL RECORD DATABASE INTEGRATION TESTS
// =====================================================
//
// xUnit
//   ↓
// CustomWebApplicationFactory
//   ↓
// API
//   ↓
// MedicalRecordService
//   ↓
// SQL Server Testcontainer
//
// =====================================================

public class MedicalRecordDatabaseIntegrationTests
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    private readonly HttpClient _client;


    // =====================================================
    // CONSTRUCTOR
    // =====================================================

    public MedicalRecordDatabaseIntegrationTests(
        CustomWebApplicationFactory factory)
    {
        _factory = factory;

        _client =
            factory.CreateClient();
    }
}