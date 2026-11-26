using System.Data;

namespace WpfMvvmTemplate.Services;

/// <summary>
/// SQLite 데이터베이스 연결 생성 및 스키마 초기화를 담당합니다.
/// </summary>
public interface IDbConnectionFactory
{
    IDbConnection CreateConnection();
    void Initialize();
}

