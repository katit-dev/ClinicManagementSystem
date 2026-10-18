using ClinicManagementSystem.Api.Configuration;
using ClinicManagementSystem.Application.Services;
using ClinicManagementSystem.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace ClinicManagementSystem.IntegrationTests.Attachments;

public class FileStorageRegistrationTests
{
    [Fact]
    public void DefaultLocalProvider_DoesNotRequireAzureCredentials()
    {
        using var provider = Register(new());
        Assert.IsType<LocalFileStorageService>(provider.GetRequiredService<IFileStorageService>());
    }

    [Fact]
    public void AzureProvider_UsesConfiguredPrivateContainer_WithoutNetworkAtStartup()
    {
        using var provider = Register(new()
        {
            ["FileStorage:Provider"] = "AzureBlob",
            ["FileStorage:AzureBlob:ServiceUri"] = "https://clinicstorage.blob.core.windows.net",
            ["FileStorage:AzureBlob:ContainerName"] = "medical-records"
        });
        Assert.IsType<AzureBlobFileStorageService>(provider.GetRequiredService<IFileStorageService>());
    }

    [Theory]
    [InlineData("https://clinicstorage.blob.core.windows.net/container")]
    [InlineData("https://clinicstorage.blob.core.windows.net/?sig=secret")]
    [InlineData("http://clinicstorage.blob.core.windows.net")]
    [InlineData("")]
    public void InvalidAzureEndpoint_IsRejected(string endpoint)
    {
        Assert.Throws<InvalidOperationException>(() => Register(new()
        {
            ["FileStorage:Provider"] = "AzureBlob",
            ["FileStorage:AzureBlob:ServiceUri"] = endpoint
        }));
    }

    [Fact]
    public void ProductionRejectsConnectionStrings_AndInvalidProvider()
    {
        Assert.Throws<InvalidOperationException>(() => Register(new()
        {
            ["FileStorage:Provider"] = "AzureBlob",
            ["FileStorage:AzureBlob:ConnectionString"] = "UseDevelopmentStorage=true"
        }));
        Assert.Throws<InvalidOperationException>(() => Register(new() { ["FileStorage:Provider"] = "typo" }));
    }

    [Fact]
    public void AzuriteConnectionString_IsAllowedOnlyInDevelopment()
    {
        using var provider = Register(new()
        {
            ["FileStorage:Provider"] = "AzureBlob",
            ["FileStorage:AzureBlob:ConnectionString"] = "UseDevelopmentStorage=true"
        }, Environments.Development);
        Assert.IsType<AzureBlobFileStorageService>(provider.GetRequiredService<IFileStorageService>());
    }

    private static ServiceProvider Register(Dictionary<string, string?> settings, string environment = "Production")
    {
        var host = new TestHostEnvironment { EnvironmentName = environment };
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IHostEnvironment>(host);
        services.AddAttachmentFileStorage(new ConfigurationBuilder().AddInMemoryCollection(settings).Build(), host);
        return services.BuildServiceProvider();
    }

    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Production";
        public string ApplicationName { get; set; } = "StorageTests";
        public string ContentRootPath { get; set; } = Path.Combine(Path.GetTempPath(), "clinic-storage-api");
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
