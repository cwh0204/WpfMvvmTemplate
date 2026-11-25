using System.IO.Ports;

namespace WpfMvvmTemplate.Models;

/// <summary>
/// PLC 물리적 연결 방식을 나타냅니다.
/// </summary>
public enum PlcConnectionType
{
    Ethernet,
    Serial
}

/// <summary>
/// PLC 통신(이더넷 / 시리얼 RS-232·485) 설정 모델입니다.
/// </summary>
public sealed class PlcConnectionSettings
{
    public PlcConnectionType ConnectionType { get; set; } = PlcConnectionType.Ethernet;
    public int TimeoutMs { get; set; } = 1000;

    // 이더넷 설정 (LS XGT FEnet 기본: 2004 / Modbus TCP: 502)
    public string IpAddress { get; set; } = "192.168.0.10";
    public int Port { get; set; } = 2004;

    // 시리얼 설정 (RS-232 / RS-485)
    public string SerialPortName { get; set; } = "COM1";
    public int BaudRate { get; set; } = 9600;
    public int DataBits { get; set; } = 8;
    public Parity Parity { get; set; } = Parity.None;
    public StopBits StopBits { get; set; } = StopBits.One;
    public int StationNo { get; set; } = 0;

    public string SummaryText => ConnectionType == PlcConnectionType.Ethernet
        ? $"{IpAddress}:{Port}"
        : $"{SerialPortName} · {BaudRate}bps (국번 {StationNo})";
}

