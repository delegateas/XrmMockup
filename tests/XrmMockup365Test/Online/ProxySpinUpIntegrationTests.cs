using System;
using System.IO;
using System.Text.Json;
using DG.Tools.XrmMockup.Online;
using DG.XrmFramework.BusinessDomain.ServiceContext;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Xunit;
using XrmMockup.DataverseProxy.Contracts;

namespace DG.XrmMockupTest.Online
{
    /// <summary>
    /// Integration tests verifying the actual proxy spin-up and communication.
    /// Tests that XrmMockup can locate, start, and communicate with the DataverseProxy.
    /// Works on both net462 and net8.0 frameworks (proxy runs via dotnet CLI out-of-process).
    /// </summary>
    public class ProxySpinUpIntegrationTests : IDisposable
    {
        private readonly string _tempDir;
        private readonly string _mockDataFilePath;

        public ProxySpinUpIntegrationTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), string.Format("XrmMockupProxyTest_{0:N}", Guid.NewGuid()));
            Directory.CreateDirectory(_tempDir);
            _mockDataFilePath = Path.Combine(_tempDir, "mock-data.json");
        }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(_tempDir))
                {
                    Directory.Delete(_tempDir, true);
                }
            }
            catch
            {
                // Ignore cleanup errors
            }
        }

        [SkippableFact]
        public void ProxyDllFinder_FindsProxyDll()
        {
            // Arrange
            var finder = new ProxyDllFinder();

            // Act & Assert - Should not throw
            // This test verifies that the proxy DLL can be found in the development environment
            try
            {
                var path = finder.FindProxyDll();
                Assert.NotNull(path);
                Assert.True(File.Exists(path), string.Format("Proxy DLL should exist at: {0}", path));
                Assert.EndsWith("XrmMockup.DataverseProxy.dll", path);
            }
            catch (FileNotFoundException ex)
            {
                // If running in CI or without the proxy built, skip this test
                // The proxy DLL may not be available in all build configurations
                Skip.If(true, string.Format("Proxy DLL not found - test skipped: {0}", ex.Message));
            }
        }

        [Fact]
        public void ProxyProcessManager_GeneratesPipeNameDeterministically()
        {
            // Arrange
            var url1 = "https://org1.crm.dynamics.com";
            var url2 = "https://org2.crm.dynamics.com";

            // Act
            var pipeName1a = ProxyProcessManager.GeneratePipeName(url1);
            var pipeName1b = ProxyProcessManager.GeneratePipeName(url1);
            var pipeName2 = ProxyProcessManager.GeneratePipeName(url2);

            // Assert
            Assert.Equal(pipeName1a, pipeName1b); // Same URL should produce same pipe name
            Assert.NotEqual(pipeName1a, pipeName2); // Different URLs should produce different pipe names
            Assert.StartsWith("XrmMockupProxy_", pipeName1a);
        }

        [SkippableFact]
        public void ProxyProcessManager_StartsProcess_WithMockData()
        {
            // Arrange - Create mock data file with a test account
            var testAccountId = Guid.NewGuid();
            var testAccount = new Entity(Account.EntityLogicalName, testAccountId);
            testAccount["name"] = "Mock Test Account";
            testAccount["accountnumber"] = "MOCK-001";

            CreateMockDataFile(testAccount);

            // Find the proxy DLL path
            var finder = new ProxyDllFinder();
            string proxyPath;
            try
            {
                proxyPath = finder.FindProxyDll();
            }
            catch (FileNotFoundException ex)
            {
                // Skip test if proxy DLL not found
                Skip.If(true, string.Format("Proxy DLL not found - test skipped: {0}", ex.Message));
                return;
            }

            // Act - Start proxy with mock data
            var pipeName = string.Format("XrmMockupProxyTest_{0:N}", Guid.NewGuid());
            var authToken = Guid.NewGuid().ToString("N");

            using (var process = StartProxyWithMockData(proxyPath, pipeName, authToken))
            {
                // Assert - Process should be running
                Assert.NotNull(process);
                Assert.False(process.HasExited, "Proxy process should still be running");

                // Cleanup
                process.Kill();
                process.WaitForExit(5000);
            }
        }

        [SkippableFact]
        public void ProxyOnlineDataService_ConnectsToProxy_WithMockData()
        {
            // Arrange - Create mock data file with a test account
            var testAccountId = Guid.NewGuid();
            var testAccount = new Entity(Account.EntityLogicalName, testAccountId);
            testAccount["name"] = "Proxy Connection Test Account";
            testAccount["accountnumber"] = "PROXY-CONN-001";

            CreateMockDataFile(testAccount);

            // Find and start proxy
            var finder = new ProxyDllFinder();
            string proxyPath;
            try
            {
                proxyPath = finder.FindProxyDll();
            }
            catch (FileNotFoundException ex)
            {
                Skip.If(true, string.Format("Proxy DLL not found - test skipped: {0}", ex.Message));
                return;
            }

            var pipeName = string.Format("XrmMockupProxyTest_{0:N}", Guid.NewGuid());
            var authToken = Guid.NewGuid().ToString("N");

            using (var process = StartProxyWithMockData(proxyPath, pipeName, authToken))
            {
                Assert.False(process.HasExited, "Proxy should be running");

                // Act - Create a direct pipe client to test connection
                using (var pipeClient = new System.IO.Pipes.NamedPipeClientStream(".", pipeName, System.IO.Pipes.PipeDirection.InOut))
                {
                    pipeClient.Connect(10000);

                    // Send ping request
                    var pingRequest = new ProxyRequest
                    {
                        RequestType = ProxyRequestType.Ping,
                        AuthToken = authToken
                    };
                    var pingBytes = JsonSerializer.SerializeToUtf8Bytes(pingRequest);
                    var lengthBytes = BitConverter.GetBytes(pingBytes.Length);

                    pipeClient.Write(lengthBytes, 0, 4);
                    pipeClient.Write(pingBytes, 0, pingBytes.Length);
                    pipeClient.Flush();

                    // Read ping response
                    var responseLengthBytes = new byte[4];
                    pipeClient.Read(responseLengthBytes, 0, 4);
                    var responseLength = BitConverter.ToInt32(responseLengthBytes, 0);
                    var responseBytes = new byte[responseLength];
                    pipeClient.Read(responseBytes, 0, responseLength);

                    var response = JsonSerializer.Deserialize<ProxyResponse>(responseBytes);

                    // Assert
                    Assert.NotNull(response);
                    Assert.True(response.Success, "Ping should succeed");
                }

                // Cleanup
                process.Kill();
                process.WaitForExit(5000);
            }
        }

        [SkippableFact]
        public void ProxyOnlineDataService_Retrieve_ReturnsMockData()
        {
            // Arrange - Create mock data file with a test account
            var testAccountId = Guid.NewGuid();
            var testAccount = new Entity(Account.EntityLogicalName, testAccountId);
            testAccount["name"] = "Retrieve Test Account";
            testAccount["accountnumber"] = "RETRIEVE-001";

            CreateMockDataFile(testAccount);

            // Find and start proxy
            var finder = new ProxyDllFinder();
            string proxyPath;
            try
            {
                proxyPath = finder.FindProxyDll();
            }
            catch (FileNotFoundException ex)
            {
                Skip.If(true, string.Format("Proxy DLL not found - test skipped: {0}", ex.Message));
                return;
            }

            var pipeName = string.Format("XrmMockupProxyTest_{0:N}", Guid.NewGuid());
            var authToken = Guid.NewGuid().ToString("N");

            using (var process = StartProxyWithMockData(proxyPath, pipeName, authToken))
            {
                Assert.False(process.HasExited, "Proxy should be running");

                // Act - Send retrieve request via pipe
                using (var pipeClient = new System.IO.Pipes.NamedPipeClientStream(".", pipeName, System.IO.Pipes.PipeDirection.InOut))
                {
                    pipeClient.Connect(10000);

                    var retrievePayload = new ProxyRetrieveRequest
                    {
                        EntityName = Account.EntityLogicalName,
                        Id = testAccountId,
                        Columns = null // All columns
                    };

                    var retrieveRequest = new ProxyRequest
                    {
                        RequestType = ProxyRequestType.Retrieve,
                        AuthToken = authToken,
                        Payload = JsonSerializer.Serialize(retrievePayload)
                    };

                    var requestBytes = JsonSerializer.SerializeToUtf8Bytes(retrieveRequest);
                    var lengthBytes = BitConverter.GetBytes(requestBytes.Length);

                    pipeClient.Write(lengthBytes, 0, 4);
                    pipeClient.Write(requestBytes, 0, requestBytes.Length);
                    pipeClient.Flush();

                    // Read response
                    var responseLengthBytes = new byte[4];
                    pipeClient.Read(responseLengthBytes, 0, 4);
                    var responseLength = BitConverter.ToInt32(responseLengthBytes, 0);
                    var responseBytes = new byte[responseLength];
                    var totalRead = 0;
                    while (totalRead < responseLength)
                    {
                        totalRead += pipeClient.Read(responseBytes, totalRead, responseLength - totalRead);
                    }

                    var response = JsonSerializer.Deserialize<ProxyResponse>(responseBytes);

                    // Assert
                    Assert.NotNull(response);
                    Assert.True(response.Success, response.ErrorMessage);
                    Assert.NotNull(response.SerializedData);

                    var retrievedEntity = EntitySerializationHelper.DeserializeEntity(response.SerializedData);
                    Assert.Equal(testAccountId, retrievedEntity.Id);
                    Assert.Equal("Retrieve Test Account", retrievedEntity.GetAttributeValue<string>("name"));
                }

                // Cleanup
                process.Kill();
                process.WaitForExit(5000);
            }
        }

        [SkippableFact]
        public void ProxyOnlineDataService_RetrieveMultiple_ReturnsMockData()
        {
            // Arrange - Create mock data file with multiple test accounts
            var testAccount1 = new Entity(Account.EntityLogicalName, Guid.NewGuid());
            testAccount1["name"] = "Multi Test Account 1";
            testAccount1["accountnumber"] = "MULTI-001";

            var testAccount2 = new Entity(Account.EntityLogicalName, Guid.NewGuid());
            testAccount2["name"] = "Multi Test Account 2";
            testAccount2["accountnumber"] = "MULTI-002";

            CreateMockDataFile(testAccount1, testAccount2);

            // Find and start proxy
            var finder = new ProxyDllFinder();
            string proxyPath;
            try
            {
                proxyPath = finder.FindProxyDll();
            }
            catch (FileNotFoundException ex)
            {
                Skip.If(true, string.Format("Proxy DLL not found - test skipped: {0}", ex.Message));
                return;
            }

            var pipeName = string.Format("XrmMockupProxyTest_{0:N}", Guid.NewGuid());
            var authToken = Guid.NewGuid().ToString("N");

            using (var process = StartProxyWithMockData(proxyPath, pipeName, authToken))
            {
                Assert.False(process.HasExited, "Proxy should be running");

                // Act - Send retrieve multiple request via pipe
                using (var pipeClient = new System.IO.Pipes.NamedPipeClientStream(".", pipeName, System.IO.Pipes.PipeDirection.InOut))
                {
                    pipeClient.Connect(10000);

                    var query = new QueryExpression(Account.EntityLogicalName)
                    {
                        ColumnSet = new ColumnSet("name", "accountnumber")
                    };

                    var serializedQuery = EntitySerializationHelper.SerializeQueryExpression(query);
                    var retrieveMultiplePayload = new ProxyRetrieveMultipleRequest
                    {
                        SerializedQuery = serializedQuery
                    };

                    var retrieveMultipleRequest = new ProxyRequest
                    {
                        RequestType = ProxyRequestType.RetrieveMultiple,
                        AuthToken = authToken,
                        Payload = JsonSerializer.Serialize(retrieveMultiplePayload)
                    };

                    var requestBytes = JsonSerializer.SerializeToUtf8Bytes(retrieveMultipleRequest);
                    var lengthBytes = BitConverter.GetBytes(requestBytes.Length);

                    pipeClient.Write(lengthBytes, 0, 4);
                    pipeClient.Write(requestBytes, 0, requestBytes.Length);
                    pipeClient.Flush();

                    // Read response
                    var responseLengthBytes = new byte[4];
                    pipeClient.Read(responseLengthBytes, 0, 4);
                    var responseLength = BitConverter.ToInt32(responseLengthBytes, 0);
                    var responseBytes = new byte[responseLength];
                    var totalRead = 0;
                    while (totalRead < responseLength)
                    {
                        totalRead += pipeClient.Read(responseBytes, totalRead, responseLength - totalRead);
                    }

                    var response = JsonSerializer.Deserialize<ProxyResponse>(responseBytes);

                    // Assert
                    Assert.NotNull(response);
                    Assert.True(response.Success, response.ErrorMessage);
                    Assert.NotNull(response.SerializedData);

                    var collection = EntitySerializationHelper.DeserializeEntityCollection(response.SerializedData);
                    Assert.Equal(2, collection.Entities.Count);
                }

                // Cleanup
                process.Kill();
                process.WaitForExit(5000);
            }
        }

        private void CreateMockDataFile(params Entity[] entities)
        {
            var mockData = new MockDataFile
            {
                Entities = new System.Collections.Generic.List<byte[]>()
            };

            foreach (var entity in entities)
            {
                mockData.Entities.Add(EntitySerializationHelper.SerializeEntity(entity));
            }

            var json = JsonSerializer.Serialize(mockData);
            File.WriteAllText(_mockDataFilePath, json);
        }

        private System.Diagnostics.Process StartProxyWithMockData(string proxyPath, string pipeName, string authToken)
        {
            var startInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "dotnet",
                // Token is passed via stdin for security - not visible in process listings
                Arguments = string.Format("\"{0}\" --mock-data-file \"{1}\" --pipe \"{2}\"", proxyPath, _mockDataFilePath, pipeName),
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                RedirectStandardInput = true
            };

            var process = new System.Diagnostics.Process { StartInfo = startInfo };
            process.Start();

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
                    var error = process.StandardError.ReadToEnd();
                    var output = process.StandardOutput.ReadToEnd();
                    throw new InvalidOperationException(string.Format("Proxy process exited. Exit code: {0}, Error: {1}, Output: {2}", process.ExitCode, error, output));
                }

                try
                {
                    using (var testClient = new System.IO.Pipes.NamedPipeClientStream(".", pipeName, System.IO.Pipes.PipeDirection.InOut))
                    {
                        testClient.Connect(500);
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

                System.Threading.Thread.Sleep(pollInterval);
                elapsed += pollInterval;
            }

            // Timeout reached
            if (process.HasExited)
            {
                var error = process.StandardError.ReadToEnd();
                throw new InvalidOperationException(string.Format("Proxy process exited. Error: {0}", error));
            }

            throw new TimeoutException("Proxy process did not become available within timeout");
        }
    }
}
