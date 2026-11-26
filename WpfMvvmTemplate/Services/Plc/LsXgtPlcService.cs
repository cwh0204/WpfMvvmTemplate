using System.Globalization;
using System.IO;
using System.Text;
using WpfMvvmTemplate.Models;
using WpfMvvmTemplate.Services.Communication;

namespace WpfMvvmTemplate.Services.Plc;

/// <summary>
/// LS산전 XGT PLC(이더넷 FEnet / 시리얼 Cnet) 통신 서비스 구현체입니다.
/// </summary>
public sealed class LsXgtPlcService : IPlcService, IDisposable
{
    private static readonly byte[] FenetCompanyId = "LSIS-XGT\0\0"u8.ToArray();
    private const byte FenetSourcePc = 0x33;
    private const ushort CmdRead = 0x0054;
    private const ushort CmdWrite = 0x0058;
    private const ushort DataTypeBit = 0x0000;
    private const ushort DataTypeWord = 0x0002;

    private const byte Enq = 0x05;
    private const byte Eot = 0x04;
    private const byte Ack = 0x06;
    private const byte Nak = 0x15;
    private const byte Etx = 0x03;

    private readonly ISettingsService _settingsService;
    private ICommunicationTransport? _transport;
    private PlcConnectionSettings? _activeSettings;
    private ushort _invokeId;

    public bool IsConnected => _transport?.IsOpen == true;
    public string ConnectionSummary => _settingsService.PlcSettings.SummaryText;

    public event EventHandler<bool>? ConnectionStateChanged;
    public event EventHandler<string>? CommunicationLogged;

    public LsXgtPlcService(ISettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    public async Task<bool> ConnectAsync(CancellationToken ct = default)
    {
        await DisconnectAsync();

        var settings = _settingsService.PlcSettings;
        _activeSettings = settings;
        _transport = settings.ConnectionType == PlcConnectionType.Ethernet
            ? new TcpTransport(settings)
            : new SerialTransport(settings);

        try
        {
            await _transport.OpenAsync(ct);
            Log($"[CONN] LS XGT PLC 연결 성공 ({settings.SummaryText})");
            ConnectionStateChanged?.Invoke(this, true);
            return true;
        }
        catch (Exception ex)
        {
            Log($"[ERR] 연결 실패 ({settings.SummaryText}): {ex.Message}");
            await DisconnectAsync();
            throw;
        }
    }

    public async Task DisconnectAsync()
    {
        if (_transport is not null)
        {
            await _transport.CloseAsync();
            _transport.Dispose();
            _transport = null;
            Log("[DISC] PLC 연결 해제");
            ConnectionStateChanged?.Invoke(this, false);
        }
    }

    public async Task<(bool IsOk, string Message)> CheckConnectionAsync(CancellationToken ct = default)
    {
        try
        {
            Log($"[PING] 연결 점검 시작 ({ConnectionSummary})");
            if (!IsConnected || HasSettingsChanged())
            {
                await ConnectAsync(ct);
            }

            // D0000 1워드 읽기로 실제 PLC 응답 검증
            await ReadWordAsync("D0000", ct);

            var msg = $"LS XGT PLC 응답 정상 ({ConnectionSummary})";
            Log($"[PING] {msg}");
            return (true, msg);
        }
        catch (Exception ex)
        {
            var msg = $"PLC 연결 실패: {ex.Message}";
            Log($"[PING] {msg}");
            return (false, msg);
        }
    }

    public async Task<bool> ReadBitAsync(string address, CancellationToken ct = default)
    {
        var transport = EnsureConnected();
        var variable = FormatVariableName(address, isBit: true);

        if (_activeSettings!.ConnectionType == PlcConnectionType.Ethernet)
        {
            var frame = BuildFenetReadFrame(variable, DataTypeBit);
            var response = await ExchangeBytesAsync(transport, frame, ct);
            var data = ParseFenetReadResponse(response);
            var bit = data.Length > 0 && data[0] != 0;
            Log($"[RX] {variable} = {(bit ? "ON (1)" : "OFF (0)")}");
            return bit;
        }
        else
        {
            var frame = BuildCnetReadFrame(_activeSettings.StationNo, variable);
            var response = await ExchangeAsciiAsync(transport, frame, ct);
            var hex = ParseCnetReadResponse(response);
            var bit = int.Parse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture) != 0;
            Log($"[RX] {variable} = {(bit ? "ON (1)" : "OFF (0)")}");
            return bit;
        }
    }

    public async Task WriteBitAsync(string address, bool value, CancellationToken ct = default)
    {
        var transport = EnsureConnected();
        var variable = FormatVariableName(address, isBit: true);

        if (_activeSettings!.ConnectionType == PlcConnectionType.Ethernet)
        {
            var payload = new[] { (byte)(value ? 0x01 : 0x00) };
            var frame = BuildFenetWriteFrame(variable, DataTypeBit, payload);
            var response = await ExchangeBytesAsync(transport, frame, ct);
            ValidateFenetWriteResponse(response);
        }
        else
        {
            var hexData = value ? "01" : "00";
            var frame = BuildCnetWriteFrame(_activeSettings.StationNo, variable, hexData);
            var response = await ExchangeAsciiAsync(transport, frame, ct);
            ValidateCnetWriteResponse(response);
        }

        Log($"[RX] WriteBit {variable} <- {(value ? "ON (1)" : "OFF (0)")} ACK");
    }

    public async Task<short> ReadWordAsync(string address, CancellationToken ct = default)
    {
        var transport = EnsureConnected();
        var variable = FormatVariableName(address, isBit: false);

        if (_activeSettings!.ConnectionType == PlcConnectionType.Ethernet)
        {
            var frame = BuildFenetReadFrame(variable, DataTypeWord);
            var response = await ExchangeBytesAsync(transport, frame, ct);
            var data = ParseFenetReadResponse(response);
            if (data.Length < 2)
                throw new InvalidOperationException("FEnet 워드 응답 길이가 올바르지 않습니다.");

            short word = BitConverter.ToInt16(data, 0);
            Log($"[RX] {variable} = {word} (0x{word:X4})");
            return word;
        }
        else
        {
            var frame = BuildCnetReadFrame(_activeSettings.StationNo, variable);
            var response = await ExchangeAsciiAsync(transport, frame, ct);
            var hex = ParseCnetReadResponse(response);
            short word = unchecked((short)ushort.Parse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture));
            Log($"[RX] {variable} = {word} (0x{word:X4})");
            return word;
        }
    }

    public async Task WriteWordAsync(string address, short value, CancellationToken ct = default)
    {
        var transport = EnsureConnected();
        var variable = FormatVariableName(address, isBit: false);

        if (_activeSettings!.ConnectionType == PlcConnectionType.Ethernet)
        {
            var payload = BitConverter.GetBytes(value);
            var frame = BuildFenetWriteFrame(variable, DataTypeWord, payload);
            var response = await ExchangeBytesAsync(transport, frame, ct);
            ValidateFenetWriteResponse(response);
        }
        else
        {
            var hexData = $"{unchecked((ushort)value):X4}";
            var frame = BuildCnetWriteFrame(_activeSettings.StationNo, variable, hexData);
            var response = await ExchangeAsciiAsync(transport, frame, ct);
            ValidateCnetWriteResponse(response);
        }

        Log($"[RX] WriteWord {variable} <- {value} (0x{value:X4}) ACK");
    }

    public void Dispose()
    {
        _transport?.Dispose();
        _transport = null;
    }

    private ICommunicationTransport EnsureConnected()
    {
        if (_transport is null || !IsConnected || _activeSettings is null)
            throw new InvalidOperationException("PLC가 연결되어 있지 않습니다. 먼저 연결을 실행하세요.");

        return _transport;
    }

    private bool HasSettingsChanged()
    {
        if (_activeSettings is null)
            return true;

        var current = _settingsService.PlcSettings;
        return !string.Equals(_activeSettings.SummaryText, current.SummaryText, StringComparison.Ordinal)
            || _activeSettings.ConnectionType != current.ConnectionType
            || _activeSettings.TimeoutMs != current.TimeoutMs;
    }

    private async Task<byte[]> ExchangeBytesAsync(ICommunicationTransport transport, byte[] frame, CancellationToken ct)
    {
        Log($"[TX] {BitConverter.ToString(frame).Replace("-", " ")}");
        var response = await transport.SendAndReceiveAsync(frame, ct);
        if (response.Length == 0)
            throw new TimeoutException("PLC로부터 응답이 없습니다.");
        return response;
    }

    private async Task<byte[]> ExchangeAsciiAsync(ICommunicationTransport transport, byte[] frame, CancellationToken ct)
    {
        Log($"[TX] {FormatAsciiFrame(frame)}");
        var response = await transport.SendAndReceiveAsync(frame, ct);
        if (response.Length == 0)
            throw new TimeoutException("PLC로부터 응답이 없습니다.");
        Log($"[RX] {FormatAsciiFrame(response)}");
        return response;
    }

    private static string FormatVariableName(string address, bool isBit)
    {
        var trimmed = string.IsNullOrWhiteSpace(address) ? (isBit ? "M0000" : "D0000") : address.Trim().ToUpperInvariant();
        if (trimmed.StartsWith('%'))
            return trimmed;

        char device = trimmed[0];
        string offset = trimmed.Length > 1 ? trimmed[1..] : "0";
        char typeChar = isBit ? 'X' : 'W';
        return $"%{device}{typeChar}{offset}";
    }

    // ==================== LS XGT FEnet (Ethernet TCP) ====================

    private byte[] BuildFenetReadFrame(string variableName, ushort dataType)
    {
        var nameBytes = Encoding.ASCII.GetBytes(variableName);
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);

        // Instruction body: Cmd(2) + DataType(2) + Reserved(2) + BlockCount(2) + NameLen(2) + Name
        ushort bodyLength = (ushort)(10 + nameBytes.Length);
        WriteFenetHeader(bw, bodyLength);

        bw.Write(CmdRead);
        bw.Write(dataType);
        bw.Write((ushort)0);
        bw.Write((ushort)1);
        bw.Write((ushort)nameBytes.Length);
        bw.Write(nameBytes);

        return ms.ToArray();
    }

    private byte[] BuildFenetWriteFrame(string variableName, ushort dataType, byte[] data)
    {
        var nameBytes = Encoding.ASCII.GetBytes(variableName);
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);

        // Instruction body: Cmd(2) + DataType(2) + Reserved(2) + BlockCount(2) + NameLen(2) + Name + DataLen(2) + Data
        ushort bodyLength = (ushort)(12 + nameBytes.Length + data.Length);
        WriteFenetHeader(bw, bodyLength);

        bw.Write(CmdWrite);
        bw.Write(dataType);
        bw.Write((ushort)0);
        bw.Write((ushort)1);
        bw.Write((ushort)nameBytes.Length);
        bw.Write(nameBytes);
        bw.Write((ushort)data.Length);
        bw.Write(data);

        return ms.ToArray();
    }

    private void WriteFenetHeader(BinaryWriter bw, ushort bodyLength)
    {
        bw.Write(FenetCompanyId);          // 0..9: "LSIS-XGT\0\0"
        bw.Write((ushort)0);               // 10..11: PLC Info
        bw.Write((byte)0x00);              // 12: CPU Info
        bw.Write(FenetSourcePc);           // 13: Source (PC -> PLC)
        bw.Write(++_invokeId);             // 14..15: Invoke ID
        bw.Write(bodyLength);              // 16..17: Body Length
        bw.Write((byte)0x00);              // 18: FEnet Position
        bw.Write((byte)0x00);              // 19: Reserved2 (BCC)
    }

    private static byte[] ParseFenetReadResponse(byte[] response)
    {
        if (response.Length < 30)
            throw new InvalidOperationException($"FEnet 응답 길이가 부족합니다. ({response.Length} bytes)");

        ushort errorStatus = BitConverter.ToUInt16(response, 26);
        if (errorStatus != 0)
        {
            ushort errorCode = response.Length >= 30 ? BitConverter.ToUInt16(response, 28) : errorStatus;
            throw new InvalidOperationException($"FEnet PLC 오류 응답 (Code: 0x{errorCode:X4})");
        }

        ushort dataLength = BitConverter.ToUInt16(response, 30);
        if (response.Length < 32 + dataLength)
            throw new InvalidOperationException("FEnet 응답 데이터 영역이 잘렸습니다.");

        return response[32..(32 + dataLength)];
    }

    private static void ValidateFenetWriteResponse(byte[] response)
    {
        if (response.Length < 28)
            throw new InvalidOperationException($"FEnet 쓰기 응답 길이가 부족합니다. ({response.Length} bytes)");

        ushort errorStatus = BitConverter.ToUInt16(response, 26);
        if (errorStatus != 0)
        {
            ushort errorCode = response.Length >= 30 ? BitConverter.ToUInt16(response, 28) : errorStatus;
            throw new InvalidOperationException($"FEnet PLC 쓰기 오류 (Code: 0x{errorCode:X4})");
        }
    }

    // ==================== LS XGT Cnet (Serial RS-232 / RS-485) ====================

    private static byte[] BuildCnetReadFrame(int stationNo, string variableName)
    {
        var body = $"{stationNo:X2}RSS01{variableName.Length:X2}{variableName}";
        return WrapCnetFrame(body);
    }

    private static byte[] BuildCnetWriteFrame(int stationNo, string variableName, string hexData)
    {
        var body = $"{stationNo:X2}WSS01{variableName.Length:X2}{variableName}{hexData}";
        return WrapCnetFrame(body);
    }

    private static byte[] WrapCnetFrame(string asciiBody)
    {
        var bodyBytes = Encoding.ASCII.GetBytes(asciiBody);
        var frame = new byte[bodyBytes.Length + 2];
        frame[0] = Enq;
        Buffer.BlockCopy(bodyBytes, 0, frame, 1, bodyBytes.Length);
        frame[^1] = Eot;
        return frame;
    }

    private static string ParseCnetReadResponse(byte[] response)
    {
        var text = ValidateCnetAck(response);
        // Format: {Station:2}{RSS:3}{BlockCount:2}{ByteCount:2}{HexData...}
        if (text.Length < 9)
            throw new InvalidOperationException($"Cnet 읽기 응답 형식이 올바르지 않습니다: {text}");

        int byteCount = int.Parse(text[7..9], NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        int hexLength = byteCount * 2;
        if (text.Length < 9 + hexLength)
            throw new InvalidOperationException($"Cnet 읽기 데이터 길이가 부족합니다: {text}");

        return text[9..(9 + hexLength)];
    }

    private static void ValidateCnetWriteResponse(byte[] response)
    {
        _ = ValidateCnetAck(response);
    }

    private static string ValidateCnetAck(byte[] response)
    {
        if (response.Length < 4)
            throw new InvalidOperationException("Cnet 응답이 너무 짧습니다.");

        if (response[0] == Nak)
        {
            var errBody = Encoding.ASCII.GetString(response, 1, response.Length - 2);
            throw new InvalidOperationException($"Cnet PLC NAK 오류: {errBody}");
        }

        if (response[0] != Ack || response[^1] != Etx)
            throw new InvalidOperationException("Cnet ACK/ETX 프레임 형식이 아닙니다.");

        return Encoding.ASCII.GetString(response, 1, response.Length - 2);
    }

    private static string FormatAsciiFrame(byte[] frame)
    {
        var sb = new StringBuilder();
        foreach (byte b in frame)
        {
            sb.Append(b switch
            {
                Enq => "<ENQ>",
                Eot => "<EOT>",
                Ack => "<ACK>",
                Nak => "<NAK>",
                Etx => "<ETX>",
                _ => ((char)b).ToString()
            });
        }
        return sb.ToString();
    }

    private void Log(string message)
        => CommunicationLogged?.Invoke(this, $"[{DateTime.Now:HH:mm:ss.fff}] {message}");
}
