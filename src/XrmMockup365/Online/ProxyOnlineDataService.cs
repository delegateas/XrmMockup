using System;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text.Json;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using XrmMockup.DataverseProxy.Contracts;

namespace DG.Tools.XrmMockup.Online
{
    /// <summary>
    /// Implementation of IOnlineDataService that communicates with an out-of-process
    /// DataverseProxy via named pipes.
    /// </summary>
    internal class ProxyOnlineDataService : IOnlineDataService
    {
        private readonly ProxyProcessManager _processManager;
        private NamedPipeClientStream _pipeClient;
        private bool _disposed;

        public ProxyOnlineDataService(string environmentUrl, string proxyPath = null)
        {
            EnvironmentUrl = environmentUrl ?? throw new ArgumentNullException(nameof(environmentUrl));
            _processManager = new ProxyProcessManager(environmentUrl, proxyPath);
        }

        public string EnvironmentUrl { get; }

        public bool IsConnected
        {
            get
            {
                try
                {
                    EnsureConnected();
                    return _pipeClient?.IsConnected == true;
                }
                catch
                {
                    return false;
                }
            }
        }

        public Entity Retrieve(string entityName, Guid id, ColumnSet columnSet)
        {
            if (entityName == null) throw new ArgumentNullException(nameof(entityName));
            if (columnSet == null) throw new ArgumentNullException(nameof(columnSet));

            var retrieveRequest = new ProxyRetrieveRequest
            {
                EntityName = entityName,
                Id = id,
                Columns = columnSet.AllColumns ? null : columnSet.Columns.ToArray()
            };

            var request = new ProxyRequest
            {
                RequestType = ProxyRequestType.Retrieve,
                Payload = JsonSerializer.Serialize(retrieveRequest)
            };

            var response = SendRequest(request);

            if (!response.Success)
            {
                throw new InvalidOperationException(string.Format("Retrieve failed: {0}", response.ErrorMessage));
            }

            if (response.SerializedData == null)
            {
                throw new InvalidOperationException("Retrieve returned no data");
            }

            return EntitySerializationHelper.DeserializeEntity(response.SerializedData);
        }

        public EntityCollection RetrieveMultiple(QueryExpression query)
        {
            if (query == null) throw new ArgumentNullException(nameof(query));

            var serializedQuery = EntitySerializationHelper.SerializeQueryExpression(query);
            var retrieveMultipleRequest = new ProxyRetrieveMultipleRequest
            {
                SerializedQuery = serializedQuery
            };

            var request = new ProxyRequest
            {
                RequestType = ProxyRequestType.RetrieveMultiple,
                Payload = JsonSerializer.Serialize(retrieveMultipleRequest)
            };

            var response = SendRequest(request);

            if (!response.Success)
            {
                throw new InvalidOperationException(string.Format("RetrieveMultiple failed: {0}", response.ErrorMessage));
            }

            if (response.SerializedData == null)
            {
                return new EntityCollection();
            }

            return EntitySerializationHelper.DeserializeEntityCollection(response.SerializedData);
        }

        private ProxyResponse SendRequest(ProxyRequest request)
        {
            EnsureConnected();

            try
            {
                // Include authentication token in request
                request.AuthToken = _processManager.AuthToken;

                var stream = _pipeClient;

                // Serialize request
                var requestBytes = JsonSerializer.SerializeToUtf8Bytes(request);

                // Send message length + message
                var lengthBytes = BitConverter.GetBytes(requestBytes.Length);
                stream.Write(lengthBytes, 0, 4);
                stream.Write(requestBytes, 0, requestBytes.Length);
                stream.Flush();

                // Read response length
                var responseLengthBytes = new byte[4];
                var bytesRead = stream.Read(responseLengthBytes, 0, 4);
                if (bytesRead < 4)
                {
                    throw new IOException("Failed to read response length");
                }

                var responseLength = BitConverter.ToInt32(responseLengthBytes, 0);
                if (responseLength <= 0 || responseLength > 100 * 1024 * 1024)
                {
                    throw new IOException(string.Format("Invalid response length: {0}", responseLength));
                }

                // Read response body
                var responseBytes = new byte[responseLength];
                var totalRead = 0;
                while (totalRead < responseLength)
                {
                    bytesRead = stream.Read(responseBytes, totalRead, responseLength - totalRead);
                    if (bytesRead == 0)
                        break;
                    totalRead += bytesRead;
                }

                if (totalRead < responseLength)
                {
                    throw new IOException("Incomplete response received");
                }

                var response = JsonSerializer.Deserialize<ProxyResponse>(responseBytes);
                if (response == null)
                {
                    throw new InvalidOperationException("Failed to deserialize proxy response");
                }
                return response;
            }
            catch (IOException)
            {
                // Communication error - mark proxy as unhealthy and invalidate connection
                _processManager.MarkUnhealthy();
                _pipeClient?.Dispose();
                _pipeClient = null;
                throw;
            }
        }

        private void EnsureConnected()
        {
            if (_pipeClient?.IsConnected == true)
                return;

            // Ensure proxy process is running
            _processManager.EnsureRunning();

            // Dispose any existing client before creating new one
            _pipeClient?.Dispose();
            _pipeClient = null;

            // Create client in local variable first, only assign to field on complete success
            var newClient = new NamedPipeClientStream(
                ".",
                _processManager.PipeName,
                PipeDirection.InOut,
                PipeOptions.None);

            try
            {
                newClient.Connect(timeout: 30000); // 30 second timeout

                // Send a ping to verify connection
                var pingRequest = new ProxyRequest
                {
                    RequestType = ProxyRequestType.Ping,
                    AuthToken = _processManager.AuthToken
                };
                var pingBytes = JsonSerializer.SerializeToUtf8Bytes(pingRequest);
                var lengthBytes = BitConverter.GetBytes(pingBytes.Length);

                newClient.Write(lengthBytes, 0, 4);
                newClient.Write(pingBytes, 0, pingBytes.Length);
                newClient.Flush();

                // Read ping response
                var responseLengthBytes = new byte[4];
                var bytesRead = newClient.Read(responseLengthBytes, 0, 4);
                if (bytesRead < 4)
                {
                    throw new IOException("Failed to read ping response length");
                }

                var responseLength = BitConverter.ToInt32(responseLengthBytes, 0);
                var responseBytes = new byte[responseLength];
                var totalRead = 0;
                while (totalRead < responseLength)
                {
                    bytesRead = newClient.Read(responseBytes, totalRead, responseLength - totalRead);
                    if (bytesRead == 0)
                        break;
                    totalRead += bytesRead;
                }

                if (totalRead < responseLength)
                {
                    throw new IOException("Incomplete ping response received");
                }

                var response = JsonSerializer.Deserialize<ProxyResponse>(responseBytes);
                if (response == null)
                {
                    throw new InvalidOperationException("Failed to deserialize ping response");
                }
                if (!response.Success)
                {
                    throw new InvalidOperationException(string.Format("Ping failed: {0}", response.ErrorMessage));
                }

                // Only assign to field on complete success
                _pipeClient = newClient;
            }
            catch
            {
                // Dispose on any failure to prevent resource leak
                newClient.Dispose();

                // Mark proxy as unhealthy so it gets restarted on next attempt
                _processManager.MarkUnhealthy();

                throw;
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;

            try
            {
                // Send shutdown request
                if (_pipeClient?.IsConnected == true)
                {
                    var shutdownRequest = new ProxyRequest
                    {
                        RequestType = ProxyRequestType.Shutdown,
                        AuthToken = _processManager.AuthToken
                    };
                    var shutdownBytes = JsonSerializer.SerializeToUtf8Bytes(shutdownRequest);
                    var lengthBytes = BitConverter.GetBytes(shutdownBytes.Length);

                    _pipeClient.Write(lengthBytes, 0, 4);
                    _pipeClient.Write(shutdownBytes, 0, shutdownBytes.Length);
                    _pipeClient.Flush();
                }
            }
            catch
            {
                // Ignore errors during shutdown
            }

            _pipeClient?.Dispose();
            _processManager.Dispose();
        }
    }
}
