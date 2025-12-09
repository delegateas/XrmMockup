using Microsoft.Extensions.Configuration;

namespace XrmMockup.MetadataGenerator.Tool.Options;

/// <summary>
/// Reads configuration from appsettings.json and environment variables.
/// </summary>
internal sealed class ConfigReader : IConfigReader
{
    public const string ConfigFileBase = "appsettings";

    private IConfiguration? _configuration;

    public IConfiguration GetConfiguration()
    {
        _configuration ??= new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile($"{ConfigFileBase}.json", optional: true)
            .AddJsonFile($"{ConfigFileBase}.{Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production"}.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        return _configuration;
    }
}
