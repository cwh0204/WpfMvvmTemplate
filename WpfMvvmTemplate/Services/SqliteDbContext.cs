using System.Data;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Dapper;
using Microsoft.Data.Sqlite;

namespace WpfMvvmTemplate.Services;

/// <summary>
/// SQLite + Dapper용 연결 팩토리 및 초기 테이블 생성기입니다.
/// </summary>
public sealed class SqliteDbContext : IDbConnectionFactory
{
    private const string DefaultPassword = "0000";
    private readonly string _connectionString;

    public SqliteDbContext()
    {
        var dbPath = Path.Combine(AppContext.BaseDirectory, "app.db");
        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = dbPath,
            ForeignKeys = true
        }.ToString();
    }

    public IDbConnection CreateConnection()
    {
        var connection = new SqliteConnection(_connectionString);
        connection.Open();
        return connection;
    }

    public void Initialize()
    {
        using var connection = CreateConnection();

        connection.Execute("""
            CREATE TABLE IF NOT EXISTS AppSettings (
                Id INTEGER PRIMARY KEY CHECK (Id = 1),
                IsSettingsPasswordEnabled INTEGER NOT NULL,
                SettingsPasswordHash TEXT NOT NULL,
                PlcConnectionType INTEGER NOT NULL DEFAULT 0,
                PlcIpAddress TEXT NOT NULL DEFAULT '192.168.0.10',
                PlcPort INTEGER NOT NULL DEFAULT 2004,
                PlcTimeoutMs INTEGER NOT NULL DEFAULT 1000,
                PlcSerialPortName TEXT NOT NULL DEFAULT 'COM1',
                PlcBaudRate INTEGER NOT NULL DEFAULT 9600,
                PlcDataBits INTEGER NOT NULL DEFAULT 8,
                PlcParity INTEGER NOT NULL DEFAULT 0,
                PlcStopBits INTEGER NOT NULL DEFAULT 1,
                PlcStationNo INTEGER NOT NULL DEFAULT 0
            );

            CREATE TABLE IF NOT EXISTS TestRecords (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                TestNumber TEXT NOT NULL,
                TestedAt TEXT NOT NULL,
                OverallResult TEXT NOT NULL,
                TesterName TEXT NOT NULL,
                CarNumber TEXT NOT NULL,
                FormationNumber TEXT NOT NULL,
                SerialNumber TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS TestResultDetails (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                RecordId INTEGER NOT NULL,
                StepNo INTEGER NOT NULL,
                ItemName TEXT NOT NULL,
                Category TEXT NOT NULL,
                StandardSpec TEXT NOT NULL,
                MeasuredValue TEXT NOT NULL,
                Result TEXT NOT NULL,
                FOREIGN KEY (RecordId) REFERENCES TestRecords(Id) ON DELETE CASCADE
            );
            """);

        EnsureAppSettingsColumns(connection);

        var exists = connection.ExecuteScalar<int>("SELECT COUNT(1) FROM AppSettings WHERE Id = 1;");
        if (exists == 0)
        {
            connection.Execute("""
                INSERT INTO AppSettings (Id, IsSettingsPasswordEnabled, SettingsPasswordHash)
                VALUES (1, 1, @Hash);
                """,
                new { Hash = HashPassword(DefaultPassword) });
        }
    }

    private static void EnsureAppSettingsColumns(IDbConnection connection)
    {
        var existingColumns = connection
            .Query<string>("SELECT name FROM pragma_table_info('AppSettings');")
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var requiredColumns = new (string Name, string Definition)[]
        {
            ("PlcConnectionType", "INTEGER NOT NULL DEFAULT 0"),
            ("PlcIpAddress", "TEXT NOT NULL DEFAULT '192.168.0.10'"),
            ("PlcPort", "INTEGER NOT NULL DEFAULT 2004"),
            ("PlcTimeoutMs", "INTEGER NOT NULL DEFAULT 1000"),
            ("PlcSerialPortName", "TEXT NOT NULL DEFAULT 'COM1'"),
            ("PlcBaudRate", "INTEGER NOT NULL DEFAULT 9600"),
            ("PlcDataBits", "INTEGER NOT NULL DEFAULT 8"),
            ("PlcParity", "INTEGER NOT NULL DEFAULT 0"),
            ("PlcStopBits", "INTEGER NOT NULL DEFAULT 1"),
            ("PlcStationNo", "INTEGER NOT NULL DEFAULT 0")
        };

        foreach (var (name, definition) in requiredColumns)
        {
            if (!existingColumns.Contains(name))
            {
                connection.Execute($"ALTER TABLE AppSettings ADD COLUMN {name} {definition};");
            }
        }
    }

    public static string HashPassword(string password)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
        return Convert.ToHexString(bytes);
    }
}
