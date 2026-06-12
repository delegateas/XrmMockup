using DataverseConnection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using XrmMockup.TestEnvProvisioner;

// ---------------------------------------------------------------------------------------------
// XrmMockup test-environment provisioner
//
// Recreates the custom test entities, relationships, option sets and security roles that the
// XrmMockup test suite depends on, so that after running this tool you can regenerate metadata
// (Regenerate-TestMetadata.ps1) and the corresponding tests can be migrated/un-skipped.
//
// Auth + target org: identical to the metadata generator. It reads DATAVERSE_URL from a config
// file (default ./appsettings.json, override with --config <path>) or the DATAVERSE_URL env var,
// and connects with the DataverseConnection package (Azure default credentials / interactive).
//
// Run:
//   dotnet run --project tests/TestEnvProvisioner -- --config tests/appsettings.json
//
// The tool is idempotent: it checks for existing components before creating them, so it is safe
// to re-run. Use --whatif to print the plan without writing anything.
// ---------------------------------------------------------------------------------------------

string? configPath = GetArg(args, "--config");
bool whatIf = args.Contains("--whatif", StringComparer.OrdinalIgnoreCase);
// Unmanaged solution that the created entities/roles are added to (so metadata regeneration and
// solution export pick them up). Override with --solution <uniquename>.
string solutionName = GetArg(args, "--solution") ?? "XrmMockup";

// Inspect mode: print an existing attribute's SourceType + FormulaDefinition (the classic calculated
// or rollup XAML, or the Power Fx text) and exit — use it to capture XAML to bake into TestSchema.
//   --dump-formula <entitylogicalname>.<attributelogicalname>
string? dumpTarget = GetArg(args, "--dump-formula");

// Build configuration the same way the metadata generator does — appsettings.json (from the
// --config file's folder, or the current directory) plus environment variables. This IConfiguration
// is registered in DI below, because DataverseConnection's AddDataverse() reads DATAVERSE_URL from
// IConfiguration, NOT from the environment variable directly.
var baseDir = Directory.GetCurrentDirectory();
var jsonFile = "appsettings.json";
if (!string.IsNullOrEmpty(configPath))
{
    var full = Path.GetFullPath(configPath);
    baseDir = Path.GetDirectoryName(full)!;
    jsonFile = Path.GetFileName(full);
}

IConfiguration configuration = new ConfigurationBuilder()
    .SetBasePath(baseDir)
    .AddJsonFile(jsonFile, optional: true)
    .AddEnvironmentVariables()
    .Build();

// Work from the config folder so relative paths line up with the generator's behaviour.
if (!string.IsNullOrEmpty(configPath))
{
    Directory.SetCurrentDirectory(baseDir);
}

if (string.IsNullOrEmpty(configuration["DATAVERSE_URL"]))
{
    Console.Error.WriteLine($"DATAVERSE_URL is not set. Looked in '{Path.Combine(baseDir, jsonFile)}' and " +
        "environment variables. Pass --config <appsettings.json> or set the DATAVERSE_URL environment variable.");
    return 1;
}

Console.WriteLine($"Target environment: {configuration["DATAVERSE_URL"]}");
if (whatIf) Console.WriteLine("** --whatif: no changes will be written **");

var services = new ServiceCollection();
services.AddSingleton(configuration);   // AddDataverse() resolves DATAVERSE_URL from IConfiguration
services.AddDataverse();
await using var provider = services.BuildServiceProvider();
var client = provider.GetRequiredService<ServiceClient>();
if (!client.IsReady)
{
    Console.Error.WriteLine($"Failed to connect: {client.LastError}");
    return 1;
}
Console.WriteLine($"Connected to org {client.ConnectedOrgFriendlyName}.");

var builder = new MetadataBuilder((IOrganizationService)client, whatIf);

if (!string.IsNullOrEmpty(dumpTarget))
{
    var dot = dumpTarget.IndexOf('.');
    if (dot <= 0 || dot == dumpTarget.Length - 1)
    {
        Console.Error.WriteLine("Usage: --dump-formula <entitylogicalname>.<attributelogicalname>");
        return 1;
    }
    builder.DumpFormula(dumpTarget[..dot], dumpTarget[(dot + 1)..]);
    return 0;
}

try
{
    // 1. Publisher (provides the "ctx" prefix) + the unmanaged solution all components are added to.
    //    NOTE: the solution's publisher must own the "ctx" prefix; if --solution points at an existing
    //    solution with a different publisher prefix, creating ctx_* components in it will be rejected.
    var publisherId = builder.EnsurePublisher("ContextAnd", "ctx", 77079);
    builder.EnsureSolution(solutionName, solutionName, publisherId);

    // 2. Custom entities + their scalar/lookup/optionset/multiselect attributes + relationships.
    TestSchema.CreateEntities(builder);
    TestSchema.CreateRelationships(builder);

    // 3. Computed columns (Power Fx formula auto; classic calculated + rollups once their XAML is
    //    captured into TestSchema.Definitions via --dump-formula). Reference the base columns above.
    TestSchema.CreateComputedColumns(builder);

    // 4. Security roles required by the security/privilege tests.
    TestRoles.CreateRoles(builder);

    // 4. Publish everything.
    builder.PublishAll();

    // 5. Report the pieces that are intentionally NOT scripted (see TestSchema.PrintManualSteps).
    TestSchema.PrintManualSteps();

    Console.WriteLine("Done. Now regenerate metadata + context (scripts/Regenerate-TestMetadata.ps1) and migrate/un-skip the dependent tests.");
    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Provisioning failed: {ex.Message}");
    Console.Error.WriteLine(ex);
    return 1;
}

static string? GetArg(string[] argv, string name)
{
    var i = Array.FindIndex(argv, a => string.Equals(a, name, StringComparison.OrdinalIgnoreCase));
    return i >= 0 && i + 1 < argv.Length ? argv[i + 1] : null;
}
