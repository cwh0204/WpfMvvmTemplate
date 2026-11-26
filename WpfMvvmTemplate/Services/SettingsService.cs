using System.IO.Ports;
using Dapper;
using WpfMvvmTemplate.Models;

namespace WpfMvvmTemplate.Services;

/// <summary>
/// SQLite + Dapper 기반으로 애플리케이션 설정과 비밀번호, PLC 통신 설정을 관리하는 서비스입니다.
/// </summary>
public sealed class SettingsService : ISettingsService
{
    private readonly IDbConnectionFactory _dbFactory;
    private readonly AppSettings _settings;

    public bool IsPasswordEnabled => _settings.IsSettingsPasswordEnabled;
    public PlcConnectionSettings PlcSettings => _settings.Plc;

    public SettingsService(IDbConnectionFactory dbFactory)
    {
        _dbFactory = dbFactory;
        _settings = LoadSettings();
    }

    public void SetPasswordEnabled(bool enabled)
    {
        using var connection = _dbFactory.CreateConnection();
        connection.Execute("""
            UPDATE AppSettings
            SET IsSettingsPasswordEnabled = @Enabled
            WHERE Id = 1;
            """,
            new { Enabled = enabled ? 1 : 0 });

        _settings.IsSettingsPasswordEnabled = enabled;
    }

    public bool VerifyPassword(string password)
    {
        if (string.IsNullOrEmpty(password))
            return false;

        return string.Equals(
            _settings.SettingsPasswordHash,
            SqliteDbContext.HashPassword(password),
            StringComparison.Ordinal);
    }

    public bool TryChangePassword(string currentPassword, string newPassword, out string errorMessage)
    {
        if (!VerifyPassword(currentPassword))
        {
            errorMessage = "현재 비밀번호가 일치하지 않습니다.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 4)
        {
            errorMessage = "새 비밀번호는 4자 이상 입력해 주세요.";
            return false;
        }

        var newHash = SqliteDbContext.HashPassword(newPassword);

        using var connection = _dbFactory.CreateConnection();
        connection.Execute("""
            UPDATE AppSettings
            SET SettingsPasswordHash = @Hash
            WHERE Id = 1;
            """,
            new { Hash = newHash });

        _settings.SettingsPasswordHash = newHash;
        errorMessage = string.Empty;
        return true;
    }

    public void SavePlcSettings(PlcConnectionSettings plcSettings)
    {
        using var connection = _dbFactory.CreateConnection();
        connection.Execute("""
            UPDATE AppSettings
            SET PlcConnectionType = @ConnectionType,
                PlcIpAddress = @IpAddress,
                PlcPort = @Port,
                PlcTimeoutMs = @TimeoutMs,
                PlcSerialPortName = @SerialPortName,
                PlcBaudRate = @BaudRate,
                PlcDataBits = @DataBits,
                PlcParity = @Parity,
                PlcStopBits = @StopBits,
                PlcStationNo = @StationNo
            WHERE Id = 1;
            """,
            new
            {
                ConnectionType = (int)plcSettings.ConnectionType,
                plcSettings.IpAddress,
                plcSettings.Port,
                plcSettings.TimeoutMs,
                plcSettings.SerialPortName,
                plcSettings.BaudRate,
                plcSettings.DataBits,
                Parity = (int)plcSettings.Parity,
                StopBits = (int)plcSettings.StopBits,
                plcSettings.StationNo
            });

        _settings.Plc = plcSettings;
    }

    private AppSettings LoadSettings()
    {
        using var connection = _dbFactory.CreateConnection();
        var row = connection.QuerySingle<AppSettingsRow>("""
            SELECT IsSettingsPasswordEnabled,
                   SettingsPasswordHash,
                   PlcConnectionType,
                   PlcIpAddress,
                   PlcPort,
                   PlcTimeoutMs,
                   PlcSerialPortName,
                   PlcBaudRate,
                   PlcDataBits,
                   PlcParity,
                   PlcStopBits,
                   PlcStationNo
            FROM AppSettings
            WHERE Id = 1;
            """);

        return new AppSettings
        {
            IsSettingsPasswordEnabled = row.IsSettingsPasswordEnabled != 0,
            SettingsPasswordHash = row.SettingsPasswordHash,
            Plc = new PlcConnectionSettings
            {
                ConnectionType = (PlcConnectionType)row.PlcConnectionType,
                IpAddress = row.PlcIpAddress,
                Port = row.PlcPort,
                TimeoutMs = row.PlcTimeoutMs,
                SerialPortName = row.PlcSerialPortName,
                BaudRate = row.PlcBaudRate,
                DataBits = row.PlcDataBits,
                Parity = (Parity)row.PlcParity,
                StopBits = (StopBits)row.PlcStopBits,
                StationNo = row.PlcStationNo
            }
        };
    }

    private sealed class AppSettingsRow
    {
        public int IsSettingsPasswordEnabled { get; init; }
        public string SettingsPasswordHash { get; init; } = string.Empty;
        public int PlcConnectionType { get; init; }
        public string PlcIpAddress { get; init; } = "192.168.0.10";
        public int PlcPort { get; init; } = 2004;
        public int PlcTimeoutMs { get; init; } = 1000;
        public string PlcSerialPortName { get; init; } = "COM1";
        public int PlcBaudRate { get; init; } = 9600;
        public int PlcDataBits { get; init; } = 8;
        public int PlcParity { get; init; }
        public int PlcStopBits { get; init; } = 1;
        public int PlcStationNo { get; init; }
    }
}
