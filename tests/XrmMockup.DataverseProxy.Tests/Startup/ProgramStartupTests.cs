using System.Diagnostics;
using System.Reflection;
using Xunit;

namespace XrmMockup.DataverseProxy.Tests.Startup;

/// <summary>
/// Tests for Program.cs startup and CLI argument handling.
/// These tests verify that the proxy process starts correctly with various arguments.
/// </summary>
[Trait("Category", "Startup")]
public class ProgramStartupTests
{
    private static string GetProxyDllPath()
    {
        // Find the proxy DLL relative to the test assembly
        var testAssemblyPath = Assembly.GetExecutingAssembly().Location;
        var testDir = Path.GetDirectoryName(testAssemblyPath)!;

        // Navigate from test output to proxy output
        // tests/XrmMockup.DataverseProxy.Tests/bin/Debug/net8.0 -> src/XrmMockup.DataverseProxy/bin/Debug/net8.0
        var solutionRoot = Path.GetFullPath(Path.Combine(testDir, "..", "..", "..", "..", ".."));
        var proxyPath = Path.Combine(solutionRoot, "src", "XrmMockup.DataverseProxy", "bin", "Debug", "net8.0", "XrmMockup.DataverseProxy.dll");

        if (!File.Exists(proxyPath))
        {
            // Try Release configuration
            proxyPath = Path.Combine(solutionRoot, "src", "XrmMockup.DataverseProxy", "bin", "Release", "net8.0", "XrmMockup.DataverseProxy.dll");
        }

        return proxyPath;
    }

    [Fact]
    public async Task Startup_WithUrl_PassesUrlToDataverseOptions()
    {
        // Arrange
        var proxyPath = GetProxyDllPath();
        if (!File.Exists(proxyPath))
        {
            // Skip if proxy not built
            return;
        }

        var pipeName = $"XrmMockupStartupTest_{Guid.NewGuid():N}";
        var testUrl = "https://test-org.crm4.dynamics.com";

        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"\"{proxyPath}\" --url \"{testUrl}\" --pipe \"{pipeName}\"",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            RedirectStandardInput = true
        };

        using var process = new Process { StartInfo = startInfo };
        var stdout = new List<string>();
        var stderr = new List<string>();

        process.OutputDataReceived += (_, e) => { if (e.Data != null) stdout.Add(e.Data); };
        process.ErrorDataReceived += (_, e) => { if (e.Data != null) stderr.Add(e.Data); };

        // Act
        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        // Write auth token to stdin
        await process.StandardInput.WriteLineAsync("test-auth-token");
        process.StandardInput.Close();

        // Wait for process to exit (it will fail at authentication, which is expected)
        var exited = process.WaitForExit(30000);
        if (!exited)
        {
            process.Kill();
        }

        var allOutput = string.Join("\n", stdout.Concat(stderr));

        // Assert
        // The process should fail at authentication, NOT at "DataverseUrl must be provided"
        // This verifies that the URL was correctly passed to DataverseOptions
        Assert.DoesNotContain("DataverseUrl must be provided", allOutput);

        // It should show the URL in the startup message (proving the URL was received)
        Assert.Contains(testUrl, allOutput);
    }

    [Fact]
    public async Task Startup_WithoutUrl_AndWithoutMockData_FailsWithMissingUrlError()
    {
        // Arrange
        var proxyPath = GetProxyDllPath();
        if (!File.Exists(proxyPath))
        {
            return;
        }

        var pipeName = $"XrmMockupStartupTest_{Guid.NewGuid():N}";

        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"\"{proxyPath}\" --pipe \"{pipeName}\"",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            RedirectStandardInput = true
        };

        using var process = new Process { StartInfo = startInfo };
        var stderr = new List<string>();

        process.ErrorDataReceived += (_, e) => { if (e.Data != null) stderr.Add(e.Data); };

        // Act
        process.Start();
        process.BeginErrorReadLine();

        // Write auth token
        await process.StandardInput.WriteLineAsync("test-auth-token");
        process.StandardInput.Close();

        process.WaitForExit(10000);

        var errorOutput = string.Join("\n", stderr);

        // Assert - should fail with clear error about missing URL
        Assert.Contains("--url is required", errorOutput);
    }

    [Fact]
    public async Task Startup_WithMockDataFile_DoesNotRequireUrl()
    {
        // Arrange
        var proxyPath = GetProxyDllPath();
        if (!File.Exists(proxyPath))
        {
            return;
        }

        var pipeName = $"XrmMockupStartupTest_{Guid.NewGuid():N}";
        var tempFile = Path.GetTempFileName();

        try
        {
            // Create minimal mock data file
            await File.WriteAllTextAsync(tempFile, "{\"Entities\":[]}");

            var startInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"\"{proxyPath}\" --mock-data-file \"{tempFile}\" --pipe \"{pipeName}\"",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                RedirectStandardInput = true
            };

            using var process = new Process { StartInfo = startInfo };
            var stdout = new List<string>();

            process.OutputDataReceived += (_, e) => { if (e.Data != null) stdout.Add(e.Data); };

            // Act
            process.Start();
            process.BeginOutputReadLine();

            // Write auth token
            await process.StandardInput.WriteLineAsync("test-auth-token");
            process.StandardInput.Close();

            // Give it time to start
            await Task.Delay(2000);

            var isRunning = !process.HasExited;

            // Cleanup
            if (!process.HasExited)
            {
                process.Kill();
                process.WaitForExit(5000);
            }

            var output = string.Join("\n", stdout);

            // Assert - should start successfully in mock mode
            Assert.True(isRunning || output.Contains("mock mode"),
                $"Process should start in mock mode. Output: {output}");
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }
}
