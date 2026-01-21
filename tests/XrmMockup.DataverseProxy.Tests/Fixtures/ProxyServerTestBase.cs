using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.PowerPlatform.Dataverse.Client;
using NSubstitute;
using Xunit;

namespace XrmMockup.DataverseProxy.Tests.Fixtures;

/// <summary>
/// Base class for ProxyServer tests.
/// Provides a mock IOrganizationServiceAsync2 and starts the server on a unique pipe name.
/// </summary>
public abstract class ProxyServerTestBase : IAsyncLifetime
{
    protected IOrganizationServiceAsync2 MockOrganizationService { get; private set; } = null!;
    protected string PipeName { get; private set; } = null!;
    protected string AuthToken { get; } = "test-auth-token-12345";
    protected ProxyServer Server { get; private set; } = null!;
    protected CancellationTokenSource ServerCts { get; private set; } = null!;
    protected Task ServerTask { get; private set; } = null!;
    protected ILogger<ProxyServer> Logger { get; private set; } = null!;

    public virtual async Task InitializeAsync()
    {
        // Generate unique pipe name for this test
        PipeName = $"XrmMockupTest_{Guid.NewGuid():N}";

        // Create mock IOrganizationServiceAsync2
        MockOrganizationService = Substitute.For<IOrganizationServiceAsync2>();

        // Create logger (use NullLogger for quiet tests, or real logger for debugging)
        Logger = NullLogger<ProxyServer>.Instance;

        // Create and start the server using factory pattern
        var factory = new TestDataverseServiceFactory(MockOrganizationService);
        Server = new ProxyServer(factory, PipeName, AuthToken, Logger);
        ServerCts = new CancellationTokenSource();
        ServerTask = Server.RunAsync(ServerCts.Token);

        // Give the server a moment to start accepting connections
        await Task.Delay(50);
    }

    public virtual async Task DisposeAsync()
    {
        // Signal server to stop
        ServerCts.Cancel();

        // Wait for server to shut down (with timeout)
        try
        {
            await Task.WhenAny(ServerTask, Task.Delay(2000));
        }
        catch (OperationCanceledException)
        {
            // Expected
        }

        ServerCts.Dispose();
    }

    /// <summary>
    /// Creates a connected TestPipeClient for this test's server.
    /// </summary>
    protected async Task<TestPipeClient> CreateConnectedClientAsync(int timeoutMs = 5000)
    {
        var client = new TestPipeClient(PipeName);
        await client.ConnectAsync(timeoutMs);
        return client;
    }
}
