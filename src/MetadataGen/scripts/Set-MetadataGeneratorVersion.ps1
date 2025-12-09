# Sets the <Version> property in MetadataGenerator projects to the latest version found in CHANGELOG.md
# Usage: .\Set-MetadataGeneratorVersion.ps1

param(
    [string]$ChangelogPath = "$PSScriptRoot\..\MetadataGenerator.Tool\CHANGELOG.md"
)

$csprojPaths = @(
    "$PSScriptRoot\..\MetadataGenerator.Tool\MetadataGenerator.Tool.csproj",
    "$PSScriptRoot\..\MetadataGenerator.Core\MetadataGenerator.Core.csproj"
    "$PSScriptRoot\..\MetadataGenerator.Context\MetadataGenerator.Context.csproj"
)

$changelog = Get-Content $ChangelogPath
# Match lines like: ### v1.0.0-preview.1 - 9 December 2025
$regex = '^### v?([0-9]+\.[0-9]+\.[0-9]+(-[A-Za-z0-9\-\.]+)?)'

$versionLine = $changelog | Where-Object { $_ -match $regex } | Select-Object -First 1
if ($versionLine -match $regex) {
    $version = $matches[1]
    Write-Host "Detected version: $version"

    foreach ($csprojPath in $csprojPaths) {
        $resolved = Resolve-Path $csprojPath
        [xml]$xml = Get-Content $resolved

        # First try to find a PropertyGroup with an existing Version element
        $propertyGroup = $xml.Project.PropertyGroup | Where-Object { $_.Version } | Select-Object -First 1

        # If not found, use the first PropertyGroup
        if (-not $propertyGroup) {
            $propertyGroup = $xml.Project.PropertyGroup | Select-Object -First 1
        }

        $versionNode = $propertyGroup.SelectSingleNode('Version')
        if (-not $versionNode) {
            $versionElement = $xml.CreateElement('Version')
            $versionElement.InnerText = $version
            $propertyGroup.AppendChild($versionElement) | Out-Null
        } else {
            $versionNode.InnerText = $version
        }

        $xml.Save($resolved)
        Write-Host "Updated $resolved with version $version"
    }
} else {
    Write-Error 'No version found in CHANGELOG.md'
    exit 1
}
