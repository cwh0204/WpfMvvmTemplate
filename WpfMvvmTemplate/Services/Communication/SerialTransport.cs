using System.IO.Ports;
using WpfMvvmTemplate.Models;

namespace WpfMvvmTemplate.Services.Communication;

/// <summary>
/// 시리얼(RS-232 / RS-485) 기반 통신 전송 구현체입니다. (LS XGT Cnet, Modbus RTU 등)
/// </summary>
public sealed class SerialTransport : ICommunicationTransport
{
    private readonly PlcConnectionSettings _settings;
    private readonly SemaphoreSlim _ioLock = new(1, 1);
    private SerialPort? _port;

    public bool IsOpen => _port?.IsOpen == true;

    public SerialTransport(PlcConnectionSettings settings)
    {
        _settings = settings;
    }

    public Task OpenAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        CloseAsync();

        _port = new SerialPort(
            _settings.SerialPortName,
            _settings.BaudRate,
            _settings.Parity,
            _settings.DataBits,
            _settings.StopBits)
        {
            ReadTimeout = _settings.TimeoutMs,
            WriteTimeout = _settings.TimeoutMs
        };

        _port.Open();
        return Task.CompletedTask;
    }

    public Task CloseAsync()
    {
        if (_port is not null)
        {
            if (_port.IsOpen)
                _port.Close();
            _port.Dispose();
            _port = null;
        }
        return Task.CompletedTask;
    }

    public async Task<byte[]> SendAndReceiveAsync(byte[] request, CancellationToken ct = default)
    {
        if (_port is null || !IsOpen)
            throw new InvalidOperationException("시리얼 포트가 열려 있지 않습니다.");

        await _ioLock.WaitAsync(ct);
        try
        {
            return await Task.Run(() =>
            {
                ct.ThrowIfCancellationRequested();
                _port.DiscardInBuffer();
                _port.Write(request, 0, request.Length);

                // 첫 바이트 수신 대기 후 버퍼 일괄 읽기
                var buffer = new byte[4096];
                int first = _port.ReadByte();
                if (first < 0)
                    return Array.Empty<byte>();

                buffer[0] = (byte)first;
                int total = 1;

                Thread.Sleep(20);
                while (_port.BytesToRead > 0 && total < buffer.Length)
                {
                    int count = _port.Read(buffer, total, Math.Min(_port.BytesToRead, buffer.Length - total));
                    total += count;
                }

                return buffer[..total];
            }, ct);
        }
        finally
        {
            _ioLock.Release();
        }
    }

    public void Dispose()
    {
        if (_port is not null)
        {
            if (_port.IsOpen)
                _port.Close();
            _port.Dispose();
        }
        _ioLock.Dispose();
    }
}

