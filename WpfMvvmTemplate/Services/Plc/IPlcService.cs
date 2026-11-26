namespace WpfMvvmTemplate.Services.Plc;

/// <summary>
/// PLC 제어기 통신 추상화 인터페이스입니다. (LS산전 XGT FEnet/Cnet 또는 Modbus 등 교체 가능)
/// </summary>
public interface IPlcService
{
    bool IsConnected { get; }
    string ConnectionSummary { get; }

    event EventHandler<bool>? ConnectionStateChanged;
    event EventHandler<string>? CommunicationLogged;

    Task<bool> ConnectAsync(CancellationToken ct = default);
    Task DisconnectAsync();
    Task<(bool IsOk, string Message)> CheckConnectionAsync(CancellationToken ct = default);

    Task<bool> ReadBitAsync(string address, CancellationToken ct = default);
    Task WriteBitAsync(string address, bool value, CancellationToken ct = default);

    Task<short> ReadWordAsync(string address, CancellationToken ct = default);
    Task WriteWordAsync(string address, short value, CancellationToken ct = default);
}

