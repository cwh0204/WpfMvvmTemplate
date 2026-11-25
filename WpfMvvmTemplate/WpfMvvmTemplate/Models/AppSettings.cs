namespace WpfMvvmTemplate.Models;

/// <summary>
/// 애플리케이션 환경 설정 모델입니다.
/// 추후 필요한 설정 항목을 이 클래스에 추가하세요.
/// </summary>
public sealed class AppSettings
{
    public bool IsSettingsPasswordEnabled { get; set; } = true;
    public string SettingsPasswordHash { get; set; } = string.Empty;
    public PlcConnectionSettings Plc { get; set; } = new();
}
