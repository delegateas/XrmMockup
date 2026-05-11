param(
    [string]$Output = "./nupkg",
    [string]$Configuration = "Release"
)

# Local pack script for XrmMockup packages
# Sets versions from changelogs and creates NuGet packages locally

# Set XrmMockup365 version from RELEASE_NOTES.md
./scripts/Set-VersionFromChangelog.ps1 -ChangelogPath ./RELEASE_NOTES.md -CsprojPath ./src/XrmMockup365/XrmMockup365.csproj

# Set MetadataGenerator version from CHANGELOG.md (shared via Directory.Build.props)
./scripts/Set-VersionFromChangelog.ps1 -ChangelogPath ./src/MetadataGen/MetadataGenerator.Tool/CHANGELOG.md -PropsPath ./src/MetadataGen/Directory.Build.props

# Build
dotnet build --configuration $Configuration

# Pack specific projects (not the entire solution to avoid legacy project errors)
dotnet pack ./src/XrmMockup365/XrmMockup365.csproj --configuration $Configuration --no-build --output $Output
dotnet pack ./src/MetadataGen/MetadataGenerator.Tool/MetadataGenerator.Tool.csproj --configuration $Configuration --no-build --output $Output
