using Xunit;
using XrmMockup.DataverseProxy.Contracts;
using XrmMockup.DataverseProxy.Tests.Fixtures;

namespace XrmMockup.DataverseProxy.Tests.Server;

/// <summary>
/// Tests for authentication token validation.
/// </summary>
[Trait("Category", "Unit")]
public class AuthenticationTests : ProxyServerTestBase
{
    [Fact]
    public async Task ValidToken_ProcessesRequest()
    {
        // Arrange
        using var client = await CreateConnectedClientAsync();
        var request = new ProxyRequest
        {
            RequestType = ProxyRequestType.Ping,
            AuthToken = AuthToken
        };

        // Act
        await client.SendRequestAsync(request);
        var response = await client.ReadResponseAsync();

        // Assert
        Assert.NotNull(response);
        Assert.True(response.Success, $"Expected success but got error: {response.ErrorMessage}");
    }

    [Fact]
    public async Task InvalidToken_ReturnsAuthFailure()
    {
        // Arrange
        using var client = await CreateConnectedClientAsync();
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
    public async Task NullToken_ReturnsAuthFailure()
    {
        // Arrange
        using var client = await CreateConnectedClientAsync();
        var request = new ProxyRequest
        {
            RequestType = ProxyRequestType.Ping,
            AuthToken = null
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
    public async Task EmptyToken_ReturnsAuthFailure()
    {
        // Arrange
        using var client = await CreateConnectedClientAsync();
        var request = new ProxyRequest
        {
            RequestType = ProxyRequestType.Ping,
            AuthToken = ""
        };

        // Act
        await client.SendRequestAsync(request);
        var response = await client.ReadResponseAsync();

        // Assert
        Assert.NotNull(response);
        Assert.False(response.Success);
        Assert.Equal("Authentication failed", response.ErrorMessage);
    }
}
