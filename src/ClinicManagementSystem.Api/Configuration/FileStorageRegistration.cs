using Azure.Identity;
using Azure.Storage.Blobs;
using ClinicManagementSystem.Application.Services;
using ClinicManagementSystem.Infrastructure.Services;
using Microsoft.Extensions.Options;
using System.Text.RegularExpressions;

namespace ClinicManagementSystem.Api.Configuration;

public static class FileStorageRegistration
{
    public static IServiceCollection AddAttachmentFileStorage(this IServiceCollection services,
        IConfiguration configuration, IHostEnvironment environment)
    {
        var options = new FileStorageOptions();
        configuration.GetSection(FileStorageOptions.SectionName).Bind(options);
        services.AddSingleton<IOptions<FileStorageOptions>>(Options.Create(options));

        if (options.Provider.Equals("Local", StringComparison.OrdinalIgnoreCase))
        {
            services.AddSingleton<IFileStorageService, LocalFileStorageService>();
            return services;
        }
        if (!options.Provider.Equals("AzureBlob", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("FileStorage:Provider must be Local or AzureBlob.");

        var blobOptions = options.AzureBlob;
        if (!Regex.IsMatch(blobOptions.ContainerName, "^[a-z0-9](?:[a-z0-9-]{1,61})[a-z0-9]$") ||
            blobOptions.ContainerName.Contains("--"))
            throw new InvalidOperationException("FileStorage:AzureBlob:ContainerName must be a valid private container name.");

        BlobServiceClient client;
        if (!string.IsNullOrWhiteSpace(blobOptions.ConnectionString))
        {
            if (!environment.IsDevelopment())
                throw new InvalidOperationException("Production Blob storage requires Managed Identity; ConnectionString is only for local development.");
            client = new BlobServiceClient(blobOptions.ConnectionString);
        }
        else
        {
            if (!Uri.TryCreate(blobOptions.ServiceUri, UriKind.Absolute, out var uri) || uri.Scheme != "https" ||
                !string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment) ||
                !string.IsNullOrEmpty(uri.UserInfo) || uri.AbsolutePath != "/")
                throw new InvalidOperationException("Configure FileStorage:AzureBlob:ServiceUri as an HTTPS storage account endpoint without credentials or a container path.");

            var credential = new DefaultAzureCredential(new DefaultAzureCredentialOptions
            {
                ManagedIdentityClientId = blobOptions.ManagedIdentityClientId
            });
            client = new BlobServiceClient(uri, credential);
        }

        // The private container is provisioned before deployment; the API does not change its ACL.
        services.AddSingleton(client.GetBlobContainerClient(blobOptions.ContainerName));
        services.AddSingleton<IFileStorageService, AzureBlobFileStorageService>();
        return services;
    }
}
