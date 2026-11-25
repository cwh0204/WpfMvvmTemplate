using WpfMvvmTemplate.Models;

namespace WpfMvvmTemplate.Services;

/// <summary>
/// 애플리케이션 설정 저장 및 비밀번호 검증/변경을 담당하는 서비스 인터페이스입니다.
/// </summary>
public interface ISettingsService
{
    bool IsPasswordEnabled { get; }
    PlcConnectionSettings PlcSettings { get; }

    void SetPasswordEnabled(bool enabled);
    bool VerifyPassword(string password);
    bool TryChangePassword(string currentPassword, string newPassword, out string errorMessage);
    void SavePlcSettings(PlcConnectionSettings plcSettings);
}
