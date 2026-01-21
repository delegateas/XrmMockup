using System.IO.Pipes;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk.Query;
using XrmMockup.DataverseProxy.Contracts;

namespace XrmMockup.DataverseProxy;

/// <summary>
/// Named pipe server that handles proxy requests from XrmMockup clients.
/// Supports multiple concurrent client connections for parallel test execution.
/// </summary>
/// <remarks>
/// Creates a new ProxyServer with a service factory.
/// </remarks>
public class ProxyServer(IDataverseServiceFactory serviceFactory, string pipeName, string authToken, ILogger<ProxyServer> logger)
{
    private readonly IDataverseServiceFactory _serviceFactory = serviceFactory ?? throw new ArgumentNullException(nameof(serviceFactory));
    private readonly string _pipeName = pipeName ?? throw new ArgumentNullException(nameof(pipeName));
    private readonly string _authToken = authToken ?? throw new ArgumentNullException(nameof(authToken));
    private readonly ILogger<ProxyServer> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly List<Task> _activeClients = [];
    private readonly object _clientsLock = new();

    /// <summary>
    /// Runs the proxy server, accepting connections and processing requests.
    /// Handles multiple clients concurrently for parallel test execution.
    /// </summary>
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting proxy server on pipe: {PipeName}", _pipeName);

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var pipeServer = new NamedPipeServerStream(
                    _pipeName,
                    PipeDirection.InOut,
                    NamedPipeServerStream.MaxAllowedServerInstances,
                    PipeTransmissionMode.Byte,
                    PipeOptions.Asynchronous);

                _logger.LogDebug("Waiting for client connection...");
                await pipeServer.WaitForConnectionAsync(cancellationToken);
                _logger.LogDebug("Client connected, spawning handler task");

                // Spawn a task to handle this client, allowing the main loop to accept more connections
                var clientTask = HandleClientAsync(pipeServer, cancellationToken);
                TrackClientTask(clientTask);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                _logger.LogInformation("Proxy server shutting down, waiting for active clients...");
                await WaitForActiveClientsAsync();
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error accepting client connection");
            }
        }
    }

    private void TrackClientTask(Task clientTask)
    {
        lock (_clientsLock)
        {
            // Remove completed tasks
            _activeClients.RemoveAll(t => t.IsCompleted);
            _activeClients.Add(clientTask);
        }
    }

    private async Task WaitForActiveClientsAsync()
    {
        Task[] tasksToWait;
        lock (_clientsLock)
        {
            tasksToWait = [.. _activeClients];
        }

        if (tasksToWait.Length > 0)
        {
            _logger.LogDebug("Waiting for {Count} active client(s) to complete", tasksToWait.Length);
            await Task.WhenAll(tasksToWait);
        }
    }

    private async Task HandleClientAsync(NamedPipeServerStream pipeServer, CancellationToken cancellationToken)
    {
        try
        {
            while (pipeServer.IsConnected && !cancellationToken.IsCancellationRequested)
            {
                // Read message length (4 bytes, little-endian)
                var lengthBuffer = new byte[4];
                var bytesRead = await pipeServer.ReadAsync(lengthBuffer.AsMemory(0, 4), cancellationToken);
                if (bytesRead == 0)
                {
                    _logger.LogDebug("Client disconnected");
                    break;
                }

                if (bytesRead < 4)
                {
                    _logger.LogWarning("Incomplete message length received");
                    continue;
                }

                var messageLength = BitConverter.ToInt32(lengthBuffer, 0);
                if (messageLength <= 0 || messageLength > 100 * 1024 * 1024) // Max 100MB
                {
                    _logger.LogWarning("Invalid message length: {Length}", messageLength);
                    continue;
                }

                // Read message body
                var messageBuffer = new byte[messageLength];
                var totalRead = 0;
                while (totalRead < messageLength)
                {
                    bytesRead = await pipeServer.ReadAsync(messageBuffer.AsMemory(totalRead, messageLength - totalRead), cancellationToken);
                    if (bytesRead == 0)
                        break;
                    totalRead += bytesRead;
                }

                if (totalRead < messageLength)
                {
                    _logger.LogWarning("Incomplete message received");
                    continue;
                }

                // Deserialize and process request
                var request = JsonSerializer.Deserialize<ProxyRequest>(messageBuffer);
                if (request is null)
                {
                    _logger.LogWarning("Failed to deserialize request");
                    continue;
                }
                var response = await ProcessRequestAsync(request);

                // Serialize and send response
                var responseBytes = JsonSerializer.SerializeToUtf8Bytes(response);
                var responseLength = BitConverter.GetBytes(responseBytes.Length);
                await pipeServer.WriteAsync(responseLength.AsMemory(0, 4), cancellationToken);
                await pipeServer.WriteAsync(responseBytes, cancellationToken);
                await pipeServer.FlushAsync(cancellationToken);

                // Handle shutdown request - note: with multiple clients, only this client's connection closes
                if (request.RequestType == ProxyRequestType.Shutdown)
                {
                    _logger.LogDebug("Client requested disconnect");
                    break;
                }
            }
        }
        catch (IOException)
        {
            _logger.LogDebug("Pipe broken, client disconnected");
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("Client handler cancelled");
        }
        finally
        {
            // Always dispose the pipe when client handling is done
            await pipeServer.DisposeAsync();
        }
    }

    private async Task<ProxyResponse> ProcessRequestAsync(ProxyRequest request)
    {
        // Validate authentication token using constant-time comparison to prevent timing attacks
        if (!ValidateAuthToken(request.AuthToken))
        {
            _logger.LogWarning("Invalid authentication token received");
            return new ProxyResponse { Success = false, ErrorMessage = "Authentication failed" };
        }

        try
        {
            return request.RequestType switch
            {
                ProxyRequestType.Ping => new ProxyResponse { Success = true },
                ProxyRequestType.Retrieve => await HandleRetrieveAsync(request.Payload),
                ProxyRequestType.RetrieveMultiple => await HandleRetrieveMultipleAsync(request.Payload),
                ProxyRequestType.Shutdown => new ProxyResponse { Success = true },
                _ => new ProxyResponse { Success = false, ErrorMessage = $"Unknown request type: {request.RequestType}" }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing {RequestType} request", request.RequestType);
            return new ProxyResponse
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    /// <summary>
    /// Validates the authentication token using constant-time comparison to prevent timing attacks.
    /// </summary>
    private bool ValidateAuthToken(string? token)
    {
        if (token == null)
            return false;

        var expectedBytes = Encoding.UTF8.GetBytes(_authToken);
        var actualBytes = Encoding.UTF8.GetBytes(token);

        // Use FixedTimeEquals for constant-time comparison
        return CryptographicOperations.FixedTimeEquals(expectedBytes, actualBytes);
    }

    private async Task<ProxyResponse> HandleRetrieveAsync(string payload)
    {
        if (string.IsNullOrEmpty(payload))
        {
            return new ProxyResponse { Success = false, ErrorMessage = "Empty payload" };
        }

        var retrieveRequest = JsonSerializer.Deserialize<ProxyRetrieveRequest>(payload);
        if (retrieveRequest is null)
        {
            return new ProxyResponse { Success = false, ErrorMessage = "Invalid retrieve request payload" };
        }
        _logger.LogDebug("Retrieve: {EntityName} {Id}", retrieveRequest.EntityName, retrieveRequest.Id);

        var columnSet = retrieveRequest.Columns == null
            ? new ColumnSet(true)
            : new ColumnSet(retrieveRequest.Columns);

        var service = _serviceFactory.CreateService();
        var entity = await service.RetrieveAsync(retrieveRequest.EntityName, retrieveRequest.Id, columnSet);
        var serializedEntity = EntitySerializationHelper.SerializeEntity(entity);

        return new ProxyResponse
        {
            Success = true,
            SerializedData = serializedEntity
        };
    }

    private async Task<ProxyResponse> HandleRetrieveMultipleAsync(string payload)
    {
        if (string.IsNullOrEmpty(payload))
        {
            return new ProxyResponse { Success = false, ErrorMessage = "Empty payload" };
        }

        var retrieveMultipleRequest = JsonSerializer.Deserialize<ProxyRetrieveMultipleRequest>(payload);
        if (retrieveMultipleRequest is null)
        {
            return new ProxyResponse { Success = false, ErrorMessage = "Invalid retrieve multiple request payload" };
        }

        var queryExpression = EntitySerializationHelper.DeserializeQueryExpression(retrieveMultipleRequest.SerializedQuery);
        _logger.LogDebug("RetrieveMultiple: {EntityName}", queryExpression.EntityName);

        var service = _serviceFactory.CreateService();
        var entityCollection = await service.RetrieveMultipleAsync(queryExpression);
        var serializedCollection = EntitySerializationHelper.SerializeEntityCollection(entityCollection);

        return new ProxyResponse
        {
            Success = true,
            SerializedData = serializedCollection
        };
    }
}
