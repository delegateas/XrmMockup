using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using XrmMockup.DataverseProxy.Contracts;

namespace XrmMockup.DataverseProxy.Tests.Fixtures;

/// <summary>
/// Test helper for raw pipe message communication.
/// Allows sending valid/malformed messages for protocol-level testing.
/// </summary>
public class TestPipeClient : IDisposable
{
    private readonly NamedPipeClientStream _pipe;
    private bool _disposed;

    public TestPipeClient(string pipeName)
    {
        _pipe = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
    }

    public async Task ConnectAsync(int timeoutMs = 5000, CancellationToken cancellationToken = default)
    {
        await _pipe.ConnectAsync(timeoutMs, cancellationToken);
    }

    public bool IsConnected => _pipe.IsConnected;

    /// <summary>
    /// Sends a properly framed ProxyRequest message.
    /// </summary>
    public async Task SendRequestAsync(ProxyRequest request, CancellationToken cancellationToken = default)
    {
        var requestBytes = JsonSerializer.SerializeToUtf8Bytes(request);
        await SendFramedMessageAsync(requestBytes, cancellationToken);
    }

    /// <summary>
    /// Sends raw bytes with a 4-byte length prefix.
    /// </summary>
    public async Task SendFramedMessageAsync(byte[] messageBytes, CancellationToken cancellationToken = default)
    {
        var lengthBytes = BitConverter.GetBytes(messageBytes.Length);
        await _pipe.WriteAsync(lengthBytes, 0, 4, cancellationToken);
        await _pipe.WriteAsync(messageBytes, 0, messageBytes.Length, cancellationToken);
        await _pipe.FlushAsync(cancellationToken);
    }

    /// <summary>
    /// Sends a raw 4-byte length prefix without any message body.
    /// Useful for testing incomplete message handling.
    /// </summary>
    public async Task SendLengthOnlyAsync(int length, CancellationToken cancellationToken = default)
    {
        var lengthBytes = BitConverter.GetBytes(length);
        await _pipe.WriteAsync(lengthBytes, 0, 4, cancellationToken);
        await _pipe.FlushAsync(cancellationToken);
    }

    /// <summary>
    /// Sends raw bytes without any length prefix.
    /// </summary>
    public async Task SendRawBytesAsync(byte[] bytes, CancellationToken cancellationToken = default)
    {
        await _pipe.WriteAsync(bytes, 0, bytes.Length, cancellationToken);
        await _pipe.FlushAsync(cancellationToken);
    }

    /// <summary>
    /// Reads a ProxyResponse from the pipe.
    /// </summary>
    public async Task<ProxyResponse?> ReadResponseAsync(CancellationToken cancellationToken = default)
    {
        // Read length prefix
        var lengthBytes = new byte[4];
        var bytesRead = await _pipe.ReadAsync(lengthBytes, 0, 4, cancellationToken);
        if (bytesRead < 4)
        {
            return null;
        }

        var messageLength = BitConverter.ToInt32(lengthBytes, 0);
        if (messageLength <= 0 || messageLength > 100 * 1024 * 1024)
        {
            return null;
        }

        // Read message body
        var messageBytes = new byte[messageLength];
        var totalRead = 0;
        while (totalRead < messageLength)
        {
            bytesRead = await _pipe.ReadAsync(messageBytes, totalRead, messageLength - totalRead, cancellationToken);
            if (bytesRead == 0)
                break;
            totalRead += bytesRead;
        }

        if (totalRead < messageLength)
        {
            return null;
        }

        return JsonSerializer.Deserialize<ProxyResponse>(messageBytes);
    }

    /// <summary>
    /// Reads raw bytes from the pipe.
    /// </summary>
    public async Task<int> ReadRawBytesAsync(byte[] buffer, CancellationToken cancellationToken = default)
    {
        return await _pipe.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _pipe.Dispose();
    }
}
