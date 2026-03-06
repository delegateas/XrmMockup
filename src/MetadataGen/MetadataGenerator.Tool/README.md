# XrmMockup Metadata Generator

[![Build Status](https://github.com/delegateas/XrmMockup/actions/workflows/ci.yml/badge.svg?branch=master)](https://github.com/delegateas/XrmMockup/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/v/XrmMockup.MetadataGenerator.svg)](https://www.nuget.org/packages/XrmMockup.MetadataGenerator)

A .NET CLI tool for generating metadata from Microsoft Dataverse/Dynamics 365 environments. This tool is a companion to [XrmMockup](https://github.com/delegateas/XrmMockup), extracting the metadata files required for local CRM simulation in your tests.

## Installation

Install the tool globally:

```bash
dotnet tool install --global XrmMockup.MetadataGenerator
```

Or install locally in your project:

```bash
dotnet new tool-manifest # if you don't have a manifest yet
dotnet tool install XrmMockup.MetadataGenerator
```

## Quick Start

1. Create an `appsettings.json` in your project directory
2. Run the tool:

```bash
dotnet tool xrmmockup-metadata
```

The tool will connect to Dataverse using interactive authentication and generate metadata files.

## Authentication

This tool uses the [DataverseConnection](https://github.com/delegateas/DataverseConnection) library for authentication via Azure Default Credentials. See the DataverseConnection documentation for configuration details.

Set the Dataverse URL via environment variable or appsettings.json:

| Variable | Description |
|----------|-------------|
| `DATAVERSE_URL` | Your Dataverse environment URL (e.g., `https://your-org.crm4.dynamics.com`) |

## Configuration

### JSON Configuration (appsettings.json)

Create an `appsettings.json` file in your working directory:

```json
{
  "DATAVERSE_URL": "https://your-org.crm4.dynamics.com",
  "XrmMockup": {
    "Metadata": {
      "OutputDirectory": "./Metadata",
      "Solutions": ["MySolution", "AnotherSolution"],
      "Entities": ["account", "contact", "opportunity"],
      "SecurityRoles": ["System Administrator", "Basic User"],
      "AllSecurityRoles": false,
      "PrettyPrint": false
    }
  }
}
```

#### Configuration Options

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `OutputDirectory` | string | `"./Metadata"` | Directory where metadata files will be written |
| `Solutions` | string[] | `[]` | Solution unique names to extract entities from |
| `Entities` | string[] | `[]` | Additional entity logical names to include |
| `SecurityRoles` | string[] | not set | Security role names to include (see [Security Role Filtering](#security-role-filtering)) |
| `AllSecurityRoles` | bool | `false` | Include all security roles regardless of other filtering |
| `PrettyPrint` | bool | `false` | Format XML output for readability (increases file size) |

### Environment-Specific Configuration

The tool supports environment-specific configuration files. Set the `DOTNET_ENVIRONMENT` environment variable and create a matching config file:

- `appsettings.json` - Base configuration (always loaded)
- `appsettings.Development.json` - Loaded when `DOTNET_ENVIRONMENT=Development`
- `appsettings.Production.json` - Loaded when `DOTNET_ENVIRONMENT=Production`

Environment-specific files override values from the base configuration.

## CLI Options

All CLI options override corresponding values from the configuration file.

```
xrmmockup-metadata [options]

Options:
  -o, --output <path>        Output directory for generated metadata files
  -s, --solutions <names>    Comma-separated list of solution unique names
  -e, --entities <names>     Comma-separated list of additional entity logical names
  -r, --security-roles <names>  Comma-separated list of security role names to include
  -a, --all-security-roles   Include all security roles regardless of filtering
  -c, --config <path>        Path to appsettings.json configuration file
  -p, --pretty-print         Format XML output for readability
  --help                     Show help information
  --version                  Show version information
```

### Examples

Generate metadata for a specific solution:

```bash
xrmmockup-metadata --solutions "MySolution"
```

Generate metadata with custom output directory:

```bash
xrmmockup-metadata -o "./tests/Metadata" -s "MySolution,CoreSolution"
```

Include additional entities not in any solution:

```bash
xrmmockup-metadata -s "MySolution" -e "account,contact,lead"
```

Use a specific configuration file:

```bash
xrmmockup-metadata --config "/path/to/appsettings.json"
```

Include specific security roles:

```bash
xrmmockup-metadata -r "System Administrator,Basic User"
```

Include all security roles (useful when solutions contain no roles):

```bash
xrmmockup-metadata -s "MySolution" --all-security-roles
```

Enable pretty-printed XML output:

```bash
xrmmockup-metadata --pretty-print
```

## Security Role Filtering

Security role inclusion depends on the combination of `Solutions`, `SecurityRoles`, and `AllSecurityRoles`:

| `AllSecurityRoles` | `Solutions` | `SecurityRoles` | Result |
|--------------------|-------------|-----------------|--------|
| `true` | any | any | **All roles** |
| `false` | none | not set | **All roles** (backward compatible default) |
| `false` | none | `[]` (empty) | **No roles** |
| `false` | none | `["X", ...]` | **Only named roles** |
| `false` | specified | not set or `[]` | **Solution roles only** (may be empty) |
| `false` | specified | `["X", ...]` | **Solution roles + named roles** |

When `SecurityRoles` is not set (omitted from config), the behavior depends on whether solutions are specified. If no solutions are specified, all roles are included for backward compatibility. If solutions are specified, only roles within those solutions are included.

Setting `SecurityRoles` to an empty array explicitly opts out of additional roles. Use `AllSecurityRoles` to force inclusion of all roles regardless of other settings.

## Default Entities

The following entities are always included regardless of solution or entity configuration:

- `businessunit`
- `systemuser`
- `transactioncurrency`
- `role`
- `systemuserroles`
- `team`
- `teamroles`
- `activitypointer`
- `roletemplate`
- `fileattachment`

These entities are required for XrmMockup's core functionality (security model, user management, etc.).

## Generated Files

The tool generates the following files in the output directory:

| File | Contents |
|------|----------|
| `Metadata.xml` | Serialized MetadataSkeleton containing entity metadata, option sets, plugins, currencies, and organization settings |
| `Workflows/` | Individual XML files for each workflow definition |
| `SecurityRoles/` | Individual XML files for each security role with privilege definitions |
| `TypeDeclarations.cs` | C# source file with security role GUIDs for use in tests |

## Using with XrmMockup

Configure XrmMockup to use the generated metadata:

```csharp
var settings = new XrmMockupSettings
{
    MetadataDirectoryPath = "./Metadata",
    // ... other settings
};

using var crm = XrmMockup365.GetInstance(settings);
```

## CI/CD Integration

Configure Azure Default Credentials for your pipeline (see [DataverseConnection](https://github.com/delegateas/DataverseConnection) documentation) and set `DATAVERSE_URL`:

```yaml
# Azure DevOps example
- task: DotNetCoreCLI@2
  displayName: 'Generate XrmMockup Metadata'
  env:
    DATAVERSE_URL: $(DataverseUrl)
  inputs:
    command: 'custom'
    custom: 'tool'
    arguments: 'run xrmmockup-metadata -o ./tests/Metadata -s "$(SolutionName)"'
```

```yaml
# GitHub Actions example
- name: Generate XrmMockup Metadata
  env:
    DATAVERSE_URL: ${{ secrets.DATAVERSE_URL }}
  run: dotnet tool run xrmmockup-metadata -o ./tests/Metadata -s "${{ vars.SOLUTION_NAME }}"
```

## Troubleshooting

### Authentication Issues

- Verify `DATAVERSE_URL` is correct and accessible
- See [DataverseConnection](https://github.com/delegateas/DataverseConnection) documentation for authentication troubleshooting
- Ensure the authenticating user/service principal has the System Administrator or System Customizer security role

### Missing Entities

- Verify the entity is included in one of the specified solutions, or add it explicitly to the `Entities` list
- Check that the authenticating user has read access to the entity metadata

### Large Metadata Files

- Keep `PrettyPrint` disabled (default) for production use
- Consider limiting the number of solutions/entities to only those needed for testing
