using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using XrmMockup.MetadataGenerator.Core.Models;
using XrmMockup.MetadataGenerator.Core.Services;
using XrmMockup.MetadataGenerator.Tool;
using XrmMockup.MetadataGenerator.Tool.Extensions;

// Define CLI options
var outputOption = new Option<string?>(CliOptions.Output.Primary, CliOptions.Output.Alias)
{
    Description = CliOptions.Output.Description,
    Arity = ArgumentArity.ZeroOrOne
};

var solutionsOption = new Option<string?>(CliOptions.Solutions.Primary, CliOptions.Solutions.Alias)
{
    Description = CliOptions.Solutions.Description,
    Arity = ArgumentArity.ZeroOrOne
};

var entitiesOption = new Option<string?>(CliOptions.Entities.Primary, CliOptions.Entities.Alias)
{
    Description = CliOptions.Entities.Description,
    Arity = ArgumentArity.ZeroOrOne
};

var configOption = new Option<string?>(CliOptions.Config.Primary, CliOptions.Config.Alias)
{
    Description = CliOptions.Config.Description,
    Arity = ArgumentArity.ZeroOrOne
};

var prettyPrintOption = new Option<bool>(CliOptions.PrettyPrint.Primary, CliOptions.PrettyPrint.Alias)
{
    Description = CliOptions.PrettyPrint.Description
};

var securityRolesOption = new Option<string?>(CliOptions.SecurityRoles.Primary, CliOptions.SecurityRoles.Alias)
{
    Description = CliOptions.SecurityRoles.Description,
    Arity = ArgumentArity.ZeroOrOne
};

// Build root command
var rootCommand = new RootCommand("XrmMockup Metadata Generator - Generate metadata from Dataverse for XrmMockup testing")
{
    outputOption,
    solutionsOption,
    entitiesOption,
    configOption,
    prettyPrintOption,
    securityRolesOption
};

rootCommand.SetAction(async (parseResult, cancellationToken) =>
{
    var output = parseResult.GetValue(outputOption);
    var solutions = parseResult.GetValue(solutionsOption);
    var entities = parseResult.GetValue(entitiesOption);
    var config = parseResult.GetValue(configOption);
    var prettyPrint = parseResult.GetValue(prettyPrintOption);
    var securityRoles = parseResult.GetValue(securityRolesOption);

    // If config path specified, change to that directory for config loading
    if (!string.IsNullOrEmpty(config))
    {
        var configDir = Path.GetDirectoryName(Path.GetFullPath(config));
        if (!string.IsNullOrEmpty(configDir))
        {
            Directory.SetCurrentDirectory(configDir);
        }
    }

    // Build service provider
    var services = new ServiceCollection();
    services.AddMetadataGeneratorTool(metadataConfig => new GeneratorOptions
    {
        OutputDirectory = output ?? metadataConfig.OutputDirectory,
        Solutions = ParseCommaSeparated(solutions) ?? metadataConfig.Solutions,
        Entities = ParseCommaSeparated(entities) ?? metadataConfig.Entities,
        SecurityRoles = ParseCommaSeparated(securityRoles) ?? metadataConfig.SecurityRoles,
        PrettyPrint = prettyPrint || metadataConfig.PrettyPrint
    });

    await using var serviceProvider = services.BuildServiceProvider();

    try
    {
        var generator = serviceProvider.GetRequiredService<IMetadataGeneratorService>();
        await generator.GenerateAsync(cancellationToken);
        return 0;
    }
    catch (Exception ex)
    {
        var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Metadata generation failed");
        return 1;
    }
});

return await rootCommand.Parse(args).InvokeAsync();

static string[]? ParseCommaSeparated(string? value)
{
    if (string.IsNullOrWhiteSpace(value))
        return null;

    return value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
