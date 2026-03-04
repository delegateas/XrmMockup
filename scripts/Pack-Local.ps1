param(
    [string]$Output = "./nupkg"
)

# Local pack script for XrmMockup packages
# Sets versions from changelogs and creates NuGet packages locally

# Set XrmMockup365 version from RELEASE_NOTES.md
./scripts/Set-VersionFromChangelog.ps1 -ChangelogPath ./RELEASE_NOTES.md -CsprojPath ./src/XrmMockup365/XrmMockup365.csproj

# Set MetadataGenerator versions from CHANGELOG.md
./scripts/Set-VersionFromChangelog.ps1 -ChangelogPath ./src/MetadataGen/MetadataGenerator.Tool/CHANGELOG.md -CsprojPath ./src/MetadataGen/MetadataGenerator.Tool/MetadataGenerator.Tool.csproj
./scripts/Set-VersionFromChangelog.ps1 -ChangelogPath ./src/MetadataGen/MetadataGenerator.Tool/CHANGELOG.md -CsprojPath ./src/MetadataGen/MetadataGenerator.Core/MetadataGenerator.Core.csproj
./scripts/Set-VersionFromChangelog.ps1 -ChangelogPath ./src/MetadataGen/MetadataGenerator.Tool/CHANGELOG.md -CsprojPath ./src/MetadataGen/MetadataGenerator.Context/MetadataGenerator.Context.csproj

# Build
dotnet build --configuration Release

# Pack specific projects (not the entire solution to avoid legacy project errors)
dotnet pack ./src/XrmMockup365/XrmMockup365.csproj --configuration Release --no-build --output $Output
dotnet pack ./src/MetadataGen/MetadataGenerator.Tool/MetadataGenerator.Tool.csproj --configuration Release --no-build --output $Output
