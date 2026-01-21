using System;
using System.IO;

namespace DG.Tools.XrmMockup.Online
{
    /// <summary>
    /// Finds the XrmMockup.DataverseProxy.dll using various search strategies.
    /// </summary>
    internal class ProxyDllFinder
    {
        internal const string ProxyDllName = "XrmMockup.DataverseProxy.dll";

        private readonly IFileSystemHelper _fileSystem;

        public ProxyDllFinder() : this(new DefaultFileSystemHelper())
        {
        }

        public ProxyDllFinder(IFileSystemHelper fileSystem)
        {
            _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
        }

        /// <summary>
        /// Finds the proxy DLL using the search order:
        /// 1. Explicit path (if provided)
        /// 2. NuGet packages directory (version-specific)
        /// 3. Same directory as XrmMockup365.dll
        /// 4. tools/net8.0 subdirectory
        /// 5. Parent directories (for development builds)
        /// </summary>
        /// <param name="explicitProxyPath">Optional explicit path to the proxy DLL.</param>
        /// <returns>Full path to the proxy DLL.</returns>
        /// <exception cref="FileNotFoundException">Thrown if the proxy DLL cannot be found.</exception>
        public string FindProxyDll(string explicitProxyPath = null)
        {
            // 1. Use explicit path if provided
            if (!string.IsNullOrEmpty(explicitProxyPath))
            {
                if (!_fileSystem.FileExists(explicitProxyPath))
                    throw new FileNotFoundException(string.Format("Proxy DLL not found at: {0}", explicitProxyPath));
                return explicitProxyPath;
            }

            // 2. Check NuGet packages directory
            var nugetPath = FindInNuGetPackages();
            if (nugetPath != null)
            {
                return nugetPath;
            }

            // 3. Check relative to XrmMockup365.dll (for development)
            var assemblyLocation = _fileSystem.GetExecutingAssemblyLocation();
            var assemblyDir = Path.GetDirectoryName(assemblyLocation);
            if (assemblyDir != null)
            {
                // Try dll in same directory
                var dllPath = Path.Combine(assemblyDir, ProxyDllName);
                if (_fileSystem.FileExists(dllPath))
                {
                    return dllPath;
                }

                // Try in tools/net8.0 subdirectory (NuGet package structure)
                var toolsDllPath = Path.Combine(assemblyDir, "tools", "net8.0", ProxyDllName);
                if (_fileSystem.FileExists(toolsDllPath))
                {
                    return toolsDllPath;
                }

                // Try in parent directories (for development)
                var parentDir = _fileSystem.GetParentDirectory(assemblyDir);
                while (parentDir != null)
                {
                    var devPath = Path.Combine(parentDir, "XrmMockup.DataverseProxy", "bin", "Debug", "net8.0", ProxyDllName);
                    if (_fileSystem.FileExists(devPath))
                    {
                        return devPath;
                    }
                    devPath = Path.Combine(parentDir, "XrmMockup.DataverseProxy", "bin", "Release", "net8.0", ProxyDllName);
                    if (_fileSystem.FileExists(devPath))
                    {
                        return devPath;
                    }
                    parentDir = _fileSystem.GetParentDirectory(parentDir);
                }
            }

            throw new FileNotFoundException(
                string.Format("Could not find XrmMockup Dataverse Proxy DLL ({0}). " +
                "Configure OnlineEnvironment.proxyPath in XrmMockupSettings to specify the path explicitly.",
                ProxyDllName));
        }

        /// <summary>
        /// Attempts to find the proxy DLL in the NuGet packages directory.
        /// Uses the assembly's informational version to locate the version-specific path.
        /// </summary>
        /// <returns>Path to the proxy DLL, or null if not found.</returns>
        internal string FindInNuGetPackages()
        {
            // Get the version from the current assembly to find matching NuGet package
            var informationalVersion = _fileSystem.GetAssemblyInformationalVersion();

            // Strip any metadata suffix (e.g., "+abc123" or "-preview.1+abc123")
            if (!string.IsNullOrEmpty(informationalVersion))
            {
                var plusIndex = informationalVersion.IndexOf('+');
                if (plusIndex > 0)
                {
                    informationalVersion = informationalVersion.Substring(0, plusIndex);
                }
            }

            if (string.IsNullOrEmpty(informationalVersion))
                return null;

            // Check NUGET_PACKAGES environment variable first, then fall back to default location
            var nugetPackagesBase = _fileSystem.GetEnvironmentVariable("NUGET_PACKAGES");
            if (string.IsNullOrEmpty(nugetPackagesBase))
            {
                var userProfile = _fileSystem.GetUserProfilePath();
                nugetPackagesBase = Path.Combine(userProfile, ".nuget", "packages");
            }

            var versionDir = Path.Combine(nugetPackagesBase, "xrmmockup365", informationalVersion);
            if (!_fileSystem.DirectoryExists(versionDir))
                return null;

            var toolsDir = Path.Combine(versionDir, "tools", "net8.0");
            if (!_fileSystem.DirectoryExists(toolsDir))
                return null;

            var dllPath = Path.Combine(toolsDir, ProxyDllName);
            if (_fileSystem.FileExists(dllPath))
                return dllPath;

            return null;
        }
    }
}
