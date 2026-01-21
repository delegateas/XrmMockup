using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace DG.Tools.XrmMockup.Online
{
    /// <summary>
    /// Manages the lifecycle of XrmMockup.DataverseProxy processes.
    /// Uses a shared proxy per environment URL to support parallel test execution.
    /// </summary>
    internal class ProxyProcessManager : IDisposable
    {
        // Static registry of proxy processes by environment URL
        private static readonly ConcurrentDictionary<string, SharedProxyState> _sharedProxies =
            new ConcurrentDictionary<string, SharedProxyState>(StringComparer.OrdinalIgnoreCase);

        private static bool _cleanupRegistered;
        private static readonly object _cleanupLock = new object();

        private static void EnsureCleanupRegistered()
        {
            if (_cleanupRegistered)
                return;

            lock (_cleanupLock)
            {
                if (_cleanupRegistered)
                    return;

                // Register cleanup handler for when the process exits
                AppDomain.CurrentDomain.ProcessExit += (sender, e) => ShutdownAllProxies();

                // Also handle domain unload (relevant for test frameworks)
                AppDomain.CurrentDomain.DomainUnload += (sender, e) => ShutdownAllProxies();

                _cleanupRegistered = true;
            }
        }

        /// <summary>
        /// Shuts down all running proxy processes. Called automatically on process exit.
        /// </summary>
        public static void ShutdownAllProxies()
        {
            foreach (var kvp in _sharedProxies)
            {
                kvp.Value.ForceShutdown();
            }
            _sharedProxies.Clear();
        }

        private readonly string _environmentUrl;
        private readonly string _explicitProxyPath;
        private readonly string _pipeName;
        private readonly SharedProxyState _sharedState;
        private readonly ProxyDllFinder _dllFinder;

        public ProxyProcessManager(string environmentUrl, string explicitProxyPath = null)
            : this(environmentUrl, explicitProxyPath, new ProxyDllFinder())
        {
        }

        internal ProxyProcessManager(string environmentUrl, string explicitProxyPath, ProxyDllFinder dllFinder)
        {
            _environmentUrl = environmentUrl ?? throw new ArgumentNullException(nameof(environmentUrl));
            _explicitProxyPath = explicitProxyPath;
            _dllFinder = dllFinder ?? throw new ArgumentNullException(nameof(dllFinder));
            _pipeName = GeneratePipeName(environmentUrl);

            // Ensure cleanup handlers are registered
            EnsureCleanupRegistered();

            // Get or create shared state for this environment URL
            _sharedState = _sharedProxies.GetOrAdd(_environmentUrl, _ => new SharedProxyState());
        }

        /// <summary>
        /// Gets the named pipe name used for communication.
        /// </summary>
        public string PipeName => _pipeName;

        /// <summary>
        /// Gets the authentication token for the proxy.
        /// </summary>
        public string AuthToken => _sharedState.AuthToken;

        /// <summary>
        /// Ensures the proxy process is running.
        /// Thread-safe for parallel test execution.
        /// </summary>
        public void EnsureRunning()
        {
            _sharedState.EnsureRunning(_environmentUrl, _pipeName, StartProxyProcess);
        }

        /// <summary>
        /// Marks the proxy as unhealthy, forcing a restart on the next EnsureRunning call.
        /// Call this when connection errors indicate the proxy is in a bad state.
        /// </summary>
        public void MarkUnhealthy()
        {
            _sharedState.MarkUnhealthy();
        }

        private Process StartProxyProcess(string environmentUrl, string pipeName, out string authToken)
        {
            var proxyPath = _dllFinder.FindProxyDll(_explicitProxyPath);

            // Generate cryptographically secure random token (256 bits = 32 bytes)
            var tokenBytes = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(tokenBytes);
            }
            authToken = Convert.ToBase64String(tokenBytes);

            var startInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = string.Format("\"{0}\" --url \"{1}\" --pipe \"{2}\"", proxyPath, environmentUrl, pipeName),
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                RedirectStandardInput = true
            };

            var process = new Process { StartInfo = startInfo };

            // Capture stdout and stderr asynchronously to avoid deadlocks and ensure error messages are captured
            var stdout = new StringBuilder();
            var stderr = new StringBuilder();
            process.OutputDataReceived += (sender, e) => { if (e.Data != null) stdout.AppendLine(e.Data); };
            process.ErrorDataReceived += (sender, e) => { if (e.Data != null) stderr.AppendLine(e.Data); };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            // Write token to stdin immediately after start (secure - not visible in process listings)
            process.StandardInput.WriteLine(authToken);
            process.StandardInput.Close();

            // Wait for proxy to start by polling for pipe availability
            var timeout = TimeSpan.FromSeconds(30);
            var pollInterval = TimeSpan.FromMilliseconds(100);
            var elapsed = TimeSpan.Zero;

            while (elapsed < timeout)
            {
                if (process.HasExited)
                {
                    throw new InvalidOperationException(FormatProcessError(stderr, stdout));
                }

                try
                {
                    using (var testClient = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.None))
                    {
                        testClient.Connect(500); // 500ms connection timeout
                        return process; // Pipe is available, proxy is ready
                    }
                }
                catch (TimeoutException)
                {
                    // Pipe not ready yet, continue polling
                }
                catch (IOException)
                {
                    // Pipe not ready yet, continue polling
                }

                Thread.Sleep(pollInterval);
                elapsed += pollInterval;
            }

            // Timeout reached - check one more time if process is still running
            if (process.HasExited)
            {
                throw new InvalidOperationException(FormatProcessError(stderr, stdout));
            }

            throw new TimeoutException("Proxy process did not become available within timeout");
        }

        private static string FormatProcessError(StringBuilder stderr, StringBuilder stdout)
        {
            var errorText = stderr.ToString().Trim();
            var outputText = stdout.ToString().Trim();

            if (!string.IsNullOrEmpty(errorText) && !string.IsNullOrEmpty(outputText))
            {
                return string.Format("Proxy process exited.\nStderr: {0}\nStdout: {1}", errorText, outputText);
            }
            if (!string.IsNullOrEmpty(errorText))
            {
                return string.Format("Proxy process exited. Error: {0}", errorText);
            }
            if (!string.IsNullOrEmpty(outputText))
            {
                return string.Format("Proxy process exited. Output: {0}", outputText);
            }
            return "Proxy process exited with no output.";
        }

        internal static string GeneratePipeName(string environmentUrl)
        {
            // Generate a deterministic pipe name based on the environment URL.
            // Uses SHA256 to ensure the hash is stable across processes/restarts.
            // All XrmMockup instances targeting the same URL share the same proxy process.
            using (var sha256 = SHA256.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(environmentUrl.ToLowerInvariant());
                var hash = sha256.ComputeHash(bytes);
                // Use first 8 bytes as hex (16 chars) for reasonable uniqueness
                var hashPrefix = BitConverter.ToString(hash, 0, 8).Replace("-", "");
                return string.Format("XrmMockupProxy_{0}", hashPrefix);
            }
        }

        public void Dispose()
        {
            // No-op: Proxy lifecycle is managed at the process level, not per-instance.
            // The proxy stays alive for the duration of the test run and is cleaned up
            // by ProcessExit/DomainUnload handlers, or restarted if marked unhealthy.
        }
    }

    /// <summary>
    /// Delegate for starting proxy process with token output.
    /// </summary>
    internal delegate Process StartProxyDelegate(string environmentUrl, string pipeName, out string authToken);

    /// <summary>
    /// Thread-safe shared state for a proxy process.
    /// Proxy lives for the duration of the test run
    /// </summary>
    internal class SharedProxyState
    {
        private readonly object _lock = new object();
        private Process _process;
        private string _authToken;
        private bool _markedUnhealthy;

        /// <summary>
        /// Gets the authentication token for the proxy.
        /// </summary>
        public string AuthToken
        {
            get
            {
                lock (_lock)
                {
                    return _authToken;
                }
            }
        }

        /// <summary>
        /// Marks the proxy as unhealthy, forcing a restart on next EnsureRunning call.
        /// Call this when connection errors indicate the proxy is in a bad state.
        /// </summary>
        public void MarkUnhealthy()
        {
            lock (_lock)
            {
                _markedUnhealthy = true;
            }
        }

        /// <summary>
        /// Ensures the proxy process is running. Thread-safe.
        /// </summary>
        public void EnsureRunning(string environmentUrl, string pipeName, StartProxyDelegate startProcess)
        {
            lock (_lock)
            {
                // Check if process is healthy (running and not marked unhealthy)
                if (_process != null && !_process.HasExited && !_markedUnhealthy)
                    return;

                // Process not running, exited, or marked unhealthy - need to (re)start
                if (_process != null)
                {
                    try
                    {
                        if (!_process.HasExited)
                        {
                            _process.Kill();
                            _process.WaitForExit(5000);
                        }
                    }
                    catch { }

                    try { _process.Dispose(); } catch { }
                    _process = null;
                }

                _markedUnhealthy = false;
                _process = startProcess(environmentUrl, pipeName, out _authToken);
            }
        }

        /// <summary>
        /// Shuts down the proxy process. Called during process exit cleanup.
        /// </summary>
        public void ForceShutdown()
        {
            lock (_lock)
            {
                if (_process == null)
                    return;

                try
                {
                    if (!_process.HasExited)
                    {
                        _process.Kill();
                        _process.WaitForExit(5000);
                    }
                }
                catch
                {
                    // Ignore errors during shutdown
                }

                try { _process.Dispose(); } catch { }
                _process = null;
            }
        }
    }
}
