namespace ClinicManagementSystem.Application.Helpers;

public static class PatientCodeHelper
{
    // =====================================================
    // GENERATE PATIENT CODE
    // =====================================================

    public static string Generate()
    {
        return "BN" +
               Guid.NewGuid()
                   .ToString("N")[..18]
                   .ToUpperInvariant();
    }
}