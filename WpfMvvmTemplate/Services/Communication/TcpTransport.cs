using System.Net.Sockets;
using WpfMvvmTemplate.Models;

namespace WpfMvvmTemplate.Services.Communication;

/// <summary>
/// 이더넷(TCP/IP) 기반 통신 전송 구현체입니다. (LS XGT FEnet, Modbus TCP 등)
/// </summary>
public sealed class TcpTransport : ICommunicationTransport
{
    private readonly PlcConnectionSettings _settings;
    private readonly SemaphoreSlim _ioLock = new(1, 1);
    private TcpClient? _client;
    private NetworkStream? _stream;

    public bool IsOpen => _client?.Connected == true;

    public TcpTransport(PlcConnectionSettings settings)
    {
        _settings = settings;
    }

    public async Task OpenAsync(CancellationToken ct = default)
    {
        await CloseAsync();

        _client = new TcpClient();
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(_settings.TimeoutMs);

        await _client.ConnectAsync(_settings.IpAddress, _settings.Port, timeoutCts.Token);
        _stream = _client.GetStream();
        _stream.ReadTimeout = _settings.TimeoutMs;
        _stream.WriteTimeout = _settings.TimeoutMs;
    }

    public Task CloseAsync()
    {
        _stream?.Dispose();
        _stream = null;
        _client?.Dispose();
        _client = null;
        return Task.CompletedTask;
    }

    public async Task<byte[]> SendAndReceiveAsync(byte[] request, CancellationToken ct = default)
    {
        if (_stream is null || !IsOpen)
            throw new InvalidOperationException("TCP 연결이 열려 있지 않습니다.");

        await _ioLock.WaitAsync(ct);
        try
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeoutCts.CancelAfter(_settings.TimeoutMs);

            await _stream.WriteAsync(request, timeoutCts.Token);
            await _stream.FlushAsync(timeoutCts.Token);

            var buffer = new byte[4096];
            int bytesRead = await _stream.ReadAsync(buffer, timeoutCts.Token);
            return buffer[..bytesRead];
        }
        finally
        {
            _ioLock.Release();
        }
    }

    public void Dispose()
    {
        _stream?.Dispose();
        _client?.Dispose();
        _ioLock.Dispose();
    }
}

