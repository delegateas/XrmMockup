using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using NSubstitute;
using Xunit;
using XrmMockup.DataverseProxy.Contracts;
using XrmMockup.DataverseProxy.Tests.Fixtures;

namespace XrmMockup.DataverseProxy.Tests.Integration;

/// <summary>
/// End-to-end integration tests using a mock IOrganizationServiceAsync2 as the backend.
/// Tests the full proxy communication flow.
/// </summary>
[Trait("Category", "Integration")]
public class ProxyIntegrationTests : IAsyncLifetime
{
    private IOrganizationServiceAsync2 _mockOrganizationService = null!;
    private string _pipeName = null!;
    private string _authToken = null!;
    private ProxyServer _server = null!;
    private CancellationTokenSource _serverCts = null!;
    private Task _serverTask = null!;

    public async Task InitializeAsync()
    {
        _pipeName = $"XrmMockupIntegration_{Guid.NewGuid():N}";
        _authToken = "integration-test-token";
        _mockOrganizationService = Substitute.For<IOrganizationServiceAsync2>();
        var factory = new TestDataverseServiceFactory(_mockOrganizationService);
        _server = new ProxyServer(factory, _pipeName, _authToken, NullLogger<ProxyServer>.Instance);
        _serverCts = new CancellationTokenSource();
        _serverTask = _server.RunAsync(_serverCts.Token);

        await Task.Delay(100); // Give server time to start
    }

    public async Task DisposeAsync()
    {
        _serverCts.Cancel();
        try
        {
            await Task.WhenAny(_serverTask, Task.Delay(2000));
        }
        catch (OperationCanceledException) { }
        _serverCts.Dispose();
    }

    [Fact]
    public async Task Retrieve_ThroughProxy_ReturnsEntity()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        var expectedEntity = new Entity("account", entityId)
        {
            ["name"] = "Test Account",
            ["accountnumber"] = "ACC-001"
        };

        _mockOrganizationService.RetrieveAsync("account", entityId, Arg.Any<ColumnSet>())
            .Returns(Task.FromResult(expectedEntity));

        using var client = new TestPipeClient(_pipeName);
        await client.ConnectAsync();

        var retrieveRequest = new ProxyRetrieveRequest
        {
            EntityName = "account",
            Id = entityId,
            Columns = new[] { "name", "accountnumber" }
        };

        var request = new ProxyRequest
        {
            RequestType = ProxyRequestType.Retrieve,
            AuthToken = _authToken,
            Payload = JsonSerializer.Serialize(retrieveRequest)
        };

        // Act
        await client.SendRequestAsync(request);
        var response = await client.ReadResponseAsync();

        // Assert
        Assert.NotNull(response);
        Assert.True(response.Success, $"Expected success but got error: {response.ErrorMessage}");
        Assert.NotNull(response.SerializedData);

        await _mockOrganizationService.Received(1).RetrieveAsync("account", entityId, Arg.Any<ColumnSet>());
    }

    [Fact]
    public async Task RetrieveMultiple_ThroughProxy_ReturnsCollection()
    {
        // Arrange
        var entity1 = new Entity("account", Guid.NewGuid()) { ["name"] = "Account 1" };
        var entity2 = new Entity("account", Guid.NewGuid()) { ["name"] = "Account 2" };
        var expectedCollection = new EntityCollection(new List<Entity> { entity1, entity2 });

        _mockOrganizationService.RetrieveMultipleAsync(Arg.Any<QueryExpression>())
            .Returns(Task.FromResult(expectedCollection));

        using var client = new TestPipeClient(_pipeName);
        await client.ConnectAsync();

        // Create a QueryExpression and serialize it
        var query = new QueryExpression("account")
        {
            ColumnSet = new ColumnSet("name")
        };
        var serializedQuery = EntitySerializationHelper.SerializeQueryExpression(query);

        var retrieveMultipleRequest = new ProxyRetrieveMultipleRequest
        {
            SerializedQuery = serializedQuery
        };

        var request = new ProxyRequest
        {
            RequestType = ProxyRequestType.RetrieveMultiple,
            AuthToken = _authToken,
            Payload = JsonSerializer.Serialize(retrieveMultipleRequest)
        };

        // Act
        await client.SendRequestAsync(request);
        var response = await client.ReadResponseAsync();

        // Assert
        Assert.NotNull(response);
        Assert.True(response.Success, $"Expected success but got error: {response.ErrorMessage}");
        Assert.NotNull(response.SerializedData);

        await _mockOrganizationService.Received(1).RetrieveMultipleAsync(Arg.Any<QueryExpression>());
    }

    [Fact]
    public async Task InvalidToken_ThroughProxy_Rejected()
    {
        // Arrange
        using var client = new TestPipeClient(_pipeName);
        await client.ConnectAsync();

        var request = new ProxyRequest
        {
            RequestType = ProxyRequestType.Ping,
            AuthToken = "wrong-token"
        };

        // Act
        await client.SendRequestAsync(request);
        var response = await client.ReadResponseAsync();

        // Assert
        Assert.NotNull(response);
        Assert.False(response.Success);
        Assert.Equal("Authentication failed", response.ErrorMessage);
    }

    [Fact]
    public async Task MultipleClients_ConcurrentRequests_AllProcessed()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        var expectedEntity = new Entity("account", entityId) { ["name"] = "Test Account" };

        _mockOrganizationService.RetrieveAsync("account", entityId, Arg.Any<ColumnSet>())
            .Returns(Task.FromResult(expectedEntity));

        var clientCount = 5;
        var tasks = new List<Task<bool>>();

        // Act - create multiple clients and send requests concurrently
        for (int i = 0; i < clientCount; i++)
        {
            tasks.Add(Task.Run(async () =>
            {
                using var client = new TestPipeClient(_pipeName);
                await client.ConnectAsync();

                var retrieveRequest = new ProxyRetrieveRequest
                {
                    EntityName = "account",
                    Id = entityId,
                    Columns = null // All columns
                };

                var request = new ProxyRequest
                {
                    RequestType = ProxyRequestType.Retrieve,
                    AuthToken = _authToken,
                    Payload = JsonSerializer.Serialize(retrieveRequest)
                };

                await client.SendRequestAsync(request);
                var response = await client.ReadResponseAsync();

                return response?.Success ?? false;
            }));
        }

        var results = await Task.WhenAll(tasks);

        // Assert - all requests should succeed
        Assert.All(results, success => Assert.True(success));
    }

    [Fact]
    public async Task Retrieve_ServiceClientThrows_ReturnsErrorResponse()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        _mockOrganizationService.RetrieveAsync("account", entityId, Arg.Any<ColumnSet>())
            .Returns<Entity>(x => throw new InvalidOperationException("Service unavailable"));

        using var client = new TestPipeClient(_pipeName);
        await client.ConnectAsync();

        var retrieveRequest = new ProxyRetrieveRequest
        {
            EntityName = "account",
            Id = entityId,
            Columns = null
        };

        var request = new ProxyRequest
        {
            RequestType = ProxyRequestType.Retrieve,
            AuthToken = _authToken,
            Payload = JsonSerializer.Serialize(retrieveRequest)
        };

        // Act
        await client.SendRequestAsync(request);
        var response = await client.ReadResponseAsync();

        // Assert
        Assert.NotNull(response);
        Assert.False(response.Success);
        Assert.Contains("Service unavailable", response.ErrorMessage);
    }

    [Fact]
    public async Task FullRoundTrip_PingShutdown()
    {
        // Arrange
        using var client = new TestPipeClient(_pipeName);
        await client.ConnectAsync();

        // Act - send ping
        var pingRequest = new ProxyRequest
        {
            RequestType = ProxyRequestType.Ping,
            AuthToken = _authToken
        };
        await client.SendRequestAsync(pingRequest);
        var pingResponse = await client.ReadResponseAsync();

        // Assert ping
        Assert.NotNull(pingResponse);
        Assert.True(pingResponse.Success);

        // Act - send shutdown
        var shutdownRequest = new ProxyRequest
        {
            RequestType = ProxyRequestType.Shutdown,
            AuthToken = _authToken
        };
        await client.SendRequestAsync(shutdownRequest);
        var shutdownResponse = await client.ReadResponseAsync();

        // Assert shutdown
        Assert.NotNull(shutdownResponse);
        Assert.True(shutdownResponse.Success);
    }
}
