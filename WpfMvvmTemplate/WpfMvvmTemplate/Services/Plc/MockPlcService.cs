using System.Collections.Concurrent;

namespace WpfMvvmTemplate.Services.Plc;

/// <summary>
/// 실제 PLC 장비 없이 UI 및 시험 흐름을 검증하기 위한 시뮬레이션 PLC 서비스입니다.
/// 실제 프로젝트에서는 LS산전 XGT(FEnet/Cnet) 또는 Modbus 구현체로 교체하세요.
/// </summary>
public sealed class MockPlcService : IPlcService
{
    private readonly ISettingsService _settingsService;
    private readonly ConcurrentDictionary<string, bool> _bits = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, short> _words = new(StringComparer.OrdinalIgnoreCase);

    public bool IsConnected { get; private set; }
    public string ConnectionSummary => _settingsService.PlcSettings.SummaryText;

    public event EventHandler<bool>? ConnectionStateChanged;
    public event EventHandler<string>? CommunicationLogged;

    public MockPlcService(ISettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    public async Task<bool> ConnectAsync(CancellationToken ct = default)
    {
        await Task.Delay(250, ct);
        IsConnected = true;
        Log($"[CONN] Mock PLC 연결 성공 ({ConnectionSummary})");
        ConnectionStateChanged?.Invoke(this, true);
        return true;
    }

    public Task DisconnectAsync()
    {
        IsConnected = false;
        Log("[DISC] Mock PLC 연결 해제");
        ConnectionStateChanged?.Invoke(this, false);
        return Task.CompletedTask;
    }

    public async Task<(bool IsOk, string Message)> CheckConnectionAsync(CancellationToken ct = default)
    {
        Log($"[PING] 연결 점검 시작 ({ConnectionSummary})");
        await Task.Delay(350, ct);

        if (!IsConnected)
        {
            await ConnectAsync(ct);
        }

        var msg = $"PLC 응답 정상 (Mock · {ConnectionSummary})";
        Log($"[PING] {msg}");
        return (true, msg);
    }

    public async Task<bool> ReadBitAsync(string address, CancellationToken ct = default)
    {
        EnsureConnected();
        var key = NormalizeAddress(address);
        Log($"[TX] ReadBit {key}");
        await Task.Delay(60, ct);

        var value = _bits.GetOrAdd(key, false);
        Log($"[RX] {key} = {(value ? "ON (1)" : "OFF (0)")}");
        return value;
    }

    public async Task WriteBitAsync(string address, bool value, CancellationToken ct = default)
    {
        EnsureConnected();
        var key = NormalizeAddress(address);
        Log($"[TX] WriteBit {key} <- {(value ? "ON (1)" : "OFF (0)")}");
        await Task.Delay(60, ct);

        _bits[key] = value;
        Log($"[RX] WriteBit {key} ACK");
    }

    public async Task<short> ReadWordAsync(string address, CancellationToken ct = default)
    {
        EnsureConnected();
        var key = NormalizeAddress(address);
        Log($"[TX] ReadWord {key}");
        await Task.Delay(60, ct);

        var value = _words.GetOrAdd(key, (short)0);
        Log($"[RX] {key} = {value} (0x{value:X4})");
        return value;
    }

    public async Task WriteWordAsync(string address, short value, CancellationToken ct = default)
    {
        EnsureConnected();
        var key = NormalizeAddress(address);
        Log($"[TX] WriteWord {key} <- {value} (0x{value:X4})");
        await Task.Delay(60, ct);

        _words[key] = value;
        Log($"[RX] WriteWord {key} ACK");
    }

    private void EnsureConnected()
    {
        if (!IsConnected)
            throw new InvalidOperationException("PLC가 연결되어 있지 않습니다. 먼저 연결을 실행하세요.");
    }

    private static string NormalizeAddress(string address)
        => string.IsNullOrWhiteSpace(address) ? "D0000" : address.Trim().ToUpperInvariant();

    private void Log(string message)
        => CommunicationLogged?.Invoke(this, $"[{DateTime.Now:HH:mm:ss.fff}] {message}");
}

