using System.Text.Json;
using Xunit;
using XrmMockup.DataverseProxy.Contracts;
using XrmMockup.DataverseProxy.Tests.Fixtures;

namespace XrmMockup.DataverseProxy.Tests.Server;

/// <summary>
/// Tests for request processing logic.
/// </summary>
[Trait("Category", "Unit")]
public class RequestHandlerTests : ProxyServerTestBase
{
    [Fact]
    public async Task Ping_ReturnsSuccess()
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
        Assert.True(response.Success);
        Assert.Null(response.ErrorMessage);
    }

    [Fact]
    public async Task Retrieve_EmptyPayload_ReturnsError()
    {
        // Arrange
        using var client = await CreateConnectedClientAsync();
        var request = new ProxyRequest
        {
            RequestType = ProxyRequestType.Retrieve,
            AuthToken = AuthToken,
            Payload = null
        };

        // Act
        await client.SendRequestAsync(request);
        var response = await client.ReadResponseAsync();

        // Assert
        Assert.NotNull(response);
        Assert.False(response.Success);
        Assert.Equal("Empty payload", response.ErrorMessage);
    }

    [Fact]
    public async Task Retrieve_EmptyStringPayload_ReturnsError()
    {
        // Arrange
        using var client = await CreateConnectedClientAsync();
        var request = new ProxyRequest
        {
            RequestType = ProxyRequestType.Retrieve,
            AuthToken = AuthToken,
            Payload = ""
        };

        // Act
        await client.SendRequestAsync(request);
        var response = await client.ReadResponseAsync();

        // Assert
        Assert.NotNull(response);
        Assert.False(response.Success);
        Assert.Equal("Empty payload", response.ErrorMessage);
    }

    [Fact]
    public async Task RetrieveMultiple_EmptyPayload_ReturnsError()
    {
        // Arrange
        using var client = await CreateConnectedClientAsync();
        var request = new ProxyRequest
        {
            RequestType = ProxyRequestType.RetrieveMultiple,
            AuthToken = AuthToken,
            Payload = null
        };

        // Act
        await client.SendRequestAsync(request);
        var response = await client.ReadResponseAsync();

        // Assert
        Assert.NotNull(response);
        Assert.False(response.Success);
        Assert.Equal("Empty payload", response.ErrorMessage);
    }

    [Fact]
    public async Task RetrieveMultiple_EmptyStringPayload_ReturnsError()
    {
        // Arrange
        using var client = await CreateConnectedClientAsync();
        var request = new ProxyRequest
        {
            RequestType = ProxyRequestType.RetrieveMultiple,
            AuthToken = AuthToken,
            Payload = ""
        };

        // Act
        await client.SendRequestAsync(request);
        var response = await client.ReadResponseAsync();

        // Assert
        Assert.NotNull(response);
        Assert.False(response.Success);
        Assert.Equal("Empty payload", response.ErrorMessage);
    }

    [Fact]
    public async Task Shutdown_ReturnsSuccessAndCloses()
    {
        // Arrange
        using var client = await CreateConnectedClientAsync();
        var request = new ProxyRequest
        {
            RequestType = ProxyRequestType.Shutdown,
            AuthToken = AuthToken
        };

        // Act
        await client.SendRequestAsync(request);
        var response = await client.ReadResponseAsync();

        // Assert
        Assert.NotNull(response);
        Assert.True(response.Success);

        // Give server time to close the connection
        await Task.Delay(100);

        // Verify the connection is closed by trying to read (should get 0 bytes or exception)
        var buffer = new byte[4];
        var bytesRead = await client.ReadRawBytesAsync(buffer);
        Assert.Equal(0, bytesRead);
    }

    [Fact]
    public async Task UnknownRequestType_ReturnsError()
    {
        // Arrange
        using var client = await CreateConnectedClientAsync();
        var request = new ProxyRequest
        {
            RequestType = (ProxyRequestType)99, // Unknown type
            AuthToken = AuthToken
        };

        // Act
        await client.SendRequestAsync(request);
        var response = await client.ReadResponseAsync();

        // Assert
        Assert.NotNull(response);
        Assert.False(response.Success);
        Assert.Contains("Unknown request type", response.ErrorMessage);
    }

    [Fact]
    public async Task Retrieve_InvalidPayloadJson_ReturnsError()
    {
        // Arrange
        using var client = await CreateConnectedClientAsync();
        var request = new ProxyRequest
        {
            RequestType = ProxyRequestType.Retrieve,
            AuthToken = AuthToken,
            Payload = "not valid json {"
        };

        // Act
        await client.SendRequestAsync(request);
        var response = await client.ReadResponseAsync();

        // Assert
        Assert.NotNull(response);
        Assert.False(response.Success);
        // Error message will contain JSON parsing exception details
        Assert.NotNull(response.ErrorMessage);
    }
}
