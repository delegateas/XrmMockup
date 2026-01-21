using System.IO;
using Xunit;
using DG.Tools.XrmMockup.Online;

namespace DG.XrmMockupTest.Online.ProcessManager
{
    /// <summary>
    /// Tests for DLL auto-detection logic in ProxyDllFinder.
    /// </summary>
    [Trait("Category", "Unit")]
    public class ProxyDllFinderTests
    {
        private const string ProxyDllName = "XrmMockup.DataverseProxy.dll";

        [Fact]
        public void ExplicitPath_ValidFile_ReturnsPath()
        {
            // Arrange
            var fs = new TestFileSystemHelper();
            var explicitPath = @"C:\custom\path\XrmMockup.DataverseProxy.dll";
            fs.AddFile(explicitPath);
            var finder = new ProxyDllFinder(fs);

            // Act
            var result = finder.FindProxyDll(explicitPath);

            // Assert
            Assert.Equal(explicitPath, result);
        }

        [Fact]
        public void ExplicitPath_FileNotExists_ThrowsFileNotFoundException()
        {
            // Arrange
            var fs = new TestFileSystemHelper();
            var explicitPath = @"C:\custom\path\XrmMockup.DataverseProxy.dll";
            // Don't add the file
            var finder = new ProxyDllFinder(fs);

            // Act & Assert
            var ex = Assert.Throws<FileNotFoundException>(() => finder.FindProxyDll(explicitPath));
            Assert.Contains(explicitPath, ex.Message);
        }

        [Fact]
        public void NuGet_FindsVersionSpecificDll()
        {
            // Arrange
            var fs = new TestFileSystemHelper();
            fs.AssemblyInformationalVersion = "1.2.3";
            fs.UserProfilePath = @"C:\Users\testuser";

            var expectedPath = @"C:\Users\testuser\.nuget\packages\xrmmockup365\1.2.3\tools\net8.0\XrmMockup.DataverseProxy.dll";
            fs.AddFile(expectedPath);
            fs.AddDirectory(@"C:\Users\testuser\.nuget\packages\xrmmockup365\1.2.3");
            fs.AddDirectory(@"C:\Users\testuser\.nuget\packages\xrmmockup365\1.2.3\tools\net8.0");

            var finder = new ProxyDllFinder(fs);

            // Act
            var result = finder.FindProxyDll();

            // Assert
            Assert.Equal(expectedPath, result);
        }

        [Fact]
        public void NuGet_UsesNuGetPackagesEnvVar()
        {
            // Arrange
            var fs = new TestFileSystemHelper();
            fs.AssemblyInformationalVersion = "1.2.3";
            fs.SetEnvironmentVariable("NUGET_PACKAGES", @"D:\custom\nuget");

            var expectedPath = @"D:\custom\nuget\xrmmockup365\1.2.3\tools\net8.0\XrmMockup.DataverseProxy.dll";
            fs.AddFile(expectedPath);
            fs.AddDirectory(@"D:\custom\nuget\xrmmockup365\1.2.3");
            fs.AddDirectory(@"D:\custom\nuget\xrmmockup365\1.2.3\tools\net8.0");

            var finder = new ProxyDllFinder(fs);

            // Act
            var result = finder.FindProxyDll();

            // Assert
            Assert.Equal(expectedPath, result);
        }

        [Fact]
        public void NuGet_FallsBackToUserProfile()
        {
            // Arrange
            var fs = new TestFileSystemHelper();
            fs.AssemblyInformationalVersion = "1.2.3";
            fs.UserProfilePath = @"C:\Users\testuser";
            // Don't set NUGET_PACKAGES env var

            var expectedPath = @"C:\Users\testuser\.nuget\packages\xrmmockup365\1.2.3\tools\net8.0\XrmMockup.DataverseProxy.dll";
            fs.AddFile(expectedPath);
            fs.AddDirectory(@"C:\Users\testuser\.nuget\packages\xrmmockup365\1.2.3");
            fs.AddDirectory(@"C:\Users\testuser\.nuget\packages\xrmmockup365\1.2.3\tools\net8.0");

            var finder = new ProxyDllFinder(fs);

            // Act
            var result = finder.FindProxyDll();

            // Assert
            Assert.Equal(expectedPath, result);
        }

        [Fact]
        public void NuGet_StripsVersionMetadataSuffix()
        {
            // Arrange
            var fs = new TestFileSystemHelper();
            fs.AssemblyInformationalVersion = "1.2.3+abc123"; // Version with metadata suffix
            fs.UserProfilePath = @"C:\Users\testuser";

            // Path should use version without the metadata suffix
            var expectedPath = @"C:\Users\testuser\.nuget\packages\xrmmockup365\1.2.3\tools\net8.0\XrmMockup.DataverseProxy.dll";
            fs.AddFile(expectedPath);
            fs.AddDirectory(@"C:\Users\testuser\.nuget\packages\xrmmockup365\1.2.3");
            fs.AddDirectory(@"C:\Users\testuser\.nuget\packages\xrmmockup365\1.2.3\tools\net8.0");

            var finder = new ProxyDllFinder(fs);

            // Act
            var result = finder.FindProxyDll();

            // Assert
            Assert.Equal(expectedPath, result);
        }

        [Fact]
        public void NuGet_ReturnsNullIfNoVersion()
        {
            // Arrange
            var fs = new TestFileSystemHelper();
            fs.AssemblyInformationalVersion = null; // No version

            var finder = new ProxyDllFinder(fs);

            // Act
            var result = finder.FindInNuGetPackages();

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void NuGet_ReturnsNullIfVersionDirMissing()
        {
            // Arrange
            var fs = new TestFileSystemHelper();
            fs.AssemblyInformationalVersion = "1.2.3";
            fs.UserProfilePath = @"C:\Users\testuser";
            // Don't create the version directory

            var finder = new ProxyDllFinder(fs);

            // Act
            var result = finder.FindInNuGetPackages();

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void AssemblyDir_FindsAdjacentDll()
        {
            // Arrange
            var fs = new TestFileSystemHelper();
            fs.AssemblyInformationalVersion = null; // Skip NuGet lookup
            fs.ExecutingAssemblyLocation = @"C:\app\XrmMockup365.dll";

            var expectedPath = @"C:\app\XrmMockup.DataverseProxy.dll";
            fs.AddFile(expectedPath);

            var finder = new ProxyDllFinder(fs);

            // Act
            var result = finder.FindProxyDll();

            // Assert
            Assert.Equal(expectedPath, result);
        }

        [Fact]
        public void ToolsSubdir_Searched()
        {
            // Arrange
            var fs = new TestFileSystemHelper();
            fs.AssemblyInformationalVersion = null; // Skip NuGet lookup
            fs.ExecutingAssemblyLocation = @"C:\app\lib\net8.0\XrmMockup365.dll";

            // Not in same directory, but in tools/net8.0 subdir
            var expectedPath = @"C:\app\lib\net8.0\tools\net8.0\XrmMockup.DataverseProxy.dll";
            fs.AddFile(expectedPath);

            var finder = new ProxyDllFinder(fs);

            // Act
            var result = finder.FindProxyDll();

            // Assert
            Assert.Equal(expectedPath, result);
        }

        [Fact]
        public void DevTree_FindsDebugBuild()
        {
            // Arrange
            var fs = new TestFileSystemHelper();
            fs.AssemblyInformationalVersion = null; // Skip NuGet lookup
            fs.ExecutingAssemblyLocation = @"C:\repos\XrmMockup\src\XrmMockup365\bin\Debug\net8.0\XrmMockup365.dll";

            // Proxy is in sibling project's bin directory
            var expectedPath = @"C:\repos\XrmMockup\src\XrmMockup.DataverseProxy\bin\Debug\net8.0\XrmMockup.DataverseProxy.dll";
            fs.AddFile(expectedPath);

            var finder = new ProxyDllFinder(fs);

            // Act
            var result = finder.FindProxyDll();

            // Assert
            Assert.Equal(expectedPath, result);
        }

        [Fact]
        public void DevTree_FindsReleaseBuild()
        {
            // Arrange
            var fs = new TestFileSystemHelper();
            fs.AssemblyInformationalVersion = null; // Skip NuGet lookup
            fs.ExecutingAssemblyLocation = @"C:\repos\XrmMockup\src\XrmMockup365\bin\Release\net8.0\XrmMockup365.dll";

            // Proxy is in sibling project's bin directory
            var expectedPath = @"C:\repos\XrmMockup\src\XrmMockup.DataverseProxy\bin\Release\net8.0\XrmMockup.DataverseProxy.dll";
            fs.AddFile(expectedPath);

            var finder = new ProxyDllFinder(fs);

            // Act
            var result = finder.FindProxyDll();

            // Assert
            Assert.Equal(expectedPath, result);
        }

        [Fact]
        public void NoProxyFound_ThrowsWithHelpfulMessage()
        {
            // Arrange
            var fs = new TestFileSystemHelper();
            fs.AssemblyInformationalVersion = null; // Skip NuGet lookup
            fs.ExecutingAssemblyLocation = @"C:\app\XrmMockup365.dll";
            // Don't add any proxy files

            var finder = new ProxyDllFinder(fs);

            // Act & Assert
            var ex = Assert.Throws<FileNotFoundException>(() => finder.FindProxyDll());
            Assert.Contains(ProxyDllName, ex.Message);
            Assert.Contains("proxyPath", ex.Message);
        }

        [Fact]
        public void SearchOrder_ExplicitPathTakesPrecedence()
        {
            // Arrange
            var fs = new TestFileSystemHelper();
            var explicitPath = @"C:\custom\XrmMockup.DataverseProxy.dll";
            var nugetPath = @"C:\Users\testuser\.nuget\packages\xrmmockup365\1.2.3\tools\net8.0\XrmMockup.DataverseProxy.dll";
            var adjacentPath = @"C:\app\XrmMockup.DataverseProxy.dll";

            fs.AssemblyInformationalVersion = "1.2.3";
            fs.UserProfilePath = @"C:\Users\testuser";
            fs.ExecutingAssemblyLocation = @"C:\app\XrmMockup365.dll";

            // Add all possible locations
            fs.AddFile(explicitPath);
            fs.AddFile(nugetPath);
            fs.AddFile(adjacentPath);
            fs.AddDirectory(@"C:\Users\testuser\.nuget\packages\xrmmockup365\1.2.3");
            fs.AddDirectory(@"C:\Users\testuser\.nuget\packages\xrmmockup365\1.2.3\tools\net8.0");

            var finder = new ProxyDllFinder(fs);

            // Act
            var result = finder.FindProxyDll(explicitPath);

            // Assert - explicit path should be returned
            Assert.Equal(explicitPath, result);
        }

        [Fact]
        public void SearchOrder_NuGetBeforeAssemblyDir()
        {
            // Arrange
            var fs = new TestFileSystemHelper();
            var nugetPath = @"C:\Users\testuser\.nuget\packages\xrmmockup365\1.2.3\tools\net8.0\XrmMockup.DataverseProxy.dll";
            var adjacentPath = @"C:\app\XrmMockup.DataverseProxy.dll";

            fs.AssemblyInformationalVersion = "1.2.3";
            fs.UserProfilePath = @"C:\Users\testuser";
            fs.ExecutingAssemblyLocation = @"C:\app\XrmMockup365.dll";

            // Add both locations
            fs.AddFile(nugetPath);
            fs.AddFile(adjacentPath);
            fs.AddDirectory(@"C:\Users\testuser\.nuget\packages\xrmmockup365\1.2.3");
            fs.AddDirectory(@"C:\Users\testuser\.nuget\packages\xrmmockup365\1.2.3\tools\net8.0");

            var finder = new ProxyDllFinder(fs);

            // Act
            var result = finder.FindProxyDll();

            // Assert - NuGet path should be returned (higher priority)
            Assert.Equal(nugetPath, result);
        }
    }
}
