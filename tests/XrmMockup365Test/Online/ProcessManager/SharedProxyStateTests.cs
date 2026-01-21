using System.Diagnostics;
using System.Linq;
using System.Threading;
using Xunit;
using DG.Tools.XrmMockup.Online;

namespace DG.XrmMockupTest.Online.ProcessManager
{
    /// <summary>
    /// Tests for SharedProxyState process lifecycle management.
    /// </summary>
    [Trait("Category", "Unit")]
    public class SharedProxyStateTests
    {
        [Fact]
        public void EnsureRunning_StartsNewProcess()
        {
            // Arrange
            var state = new SharedProxyState();
            var processStarted = false;
            string capturedToken = null;

            Process StartProcess(string url, string pipe, out string token)
            {
                processStarted = true;
                token = "test-token";
                capturedToken = token;
                // Return a dummy process (current process)
                return Process.GetCurrentProcess();
            }

            // Act
            state.EnsureRunning("http://test.crm.dynamics.com", "TestPipe", StartProcess);

            // Assert
            Assert.True(processStarted);
            Assert.Equal("test-token", state.AuthToken);
            Assert.Equal("test-token", capturedToken);
        }

        [Fact]
        public void EnsureRunning_ReusesRunningProcess()
        {
            // Arrange
            var state = new SharedProxyState();
            var startCount = 0;

            Process StartProcess(string url, string pipe, out string token)
            {
                startCount++;
                token = string.Format("token-{0}", startCount);
                return Process.GetCurrentProcess();
            }

            // Act - call EnsureRunning twice
            state.EnsureRunning("http://test.crm.dynamics.com", "TestPipe", StartProcess);
            state.EnsureRunning("http://test.crm.dynamics.com", "TestPipe", StartProcess);

            // Assert - process should only be started once
            Assert.Equal(1, startCount);
            Assert.Equal("token-1", state.AuthToken);
        }

        [Fact]
        public void ConcurrentEnsureRunning_ThreadSafe()
        {
            // Arrange
            var state = new SharedProxyState();
            var startCount = 0;
            var lockObj = new object();

            Process StartProcess(string url, string pipe, out string token)
            {
                lock (lockObj)
                {
                    startCount++;
                }
                token = "test-token";
                // Simulate some work
                Thread.Sleep(10);
                return Process.GetCurrentProcess();
            }

            // Act - start multiple threads calling EnsureRunning concurrently
            var threads = Enumerable.Range(0, 10).Select(_ =>
            {
                var t = new Thread(() =>
                {
                    state.EnsureRunning("http://test.crm.dynamics.com", "TestPipe", StartProcess);
                });
                t.Start();
                return t;
            }).ToList();

            foreach (var t in threads)
            {
                t.Join();
            }

            // Assert - process should only be started once despite concurrent access
            Assert.Equal(1, startCount);
        }

        [Fact]
        public void AuthToken_StoredAndRetrievable()
        {
            // Arrange
            var state = new SharedProxyState();
            var expectedToken = "my-secure-token-12345";

            Process StartProcess(string url, string pipe, out string token)
            {
                token = expectedToken;
                return Process.GetCurrentProcess();
            }

            // Act
            state.EnsureRunning("http://test.crm.dynamics.com", "TestPipe", StartProcess);

            // Assert
            Assert.Equal(expectedToken, state.AuthToken);
        }

        [Fact]
        public void AuthToken_NullBeforeProcessStarts()
        {
            // Arrange
            var state = new SharedProxyState();

            // Act & Assert
            Assert.Null(state.AuthToken);
        }
    }
}
