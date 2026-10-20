namespace ClinicManagementSystem.Application.Services;

public sealed class FileStorageOptions
{
    public const string SectionName = "FileStorage";
    public string Provider { get; set; } = "Local";
    public string LocalRootPath { get; set; } = "";
    public AzureBlobStorageOptions AzureBlob { get; set; } = new();
}

public sealed class AzureBlobStorageOptions
{
    public string ServiceUri { get; set; } = "";
    public string ContainerName { get; set; } = "medical-records";
    public string? ManagedIdentityClientId { get; set; }
    // For local Azurite only; production uses Managed Identity.
    public string? ConnectionString { get; set; }
}
