namespace WpfMvvmTemplate.Services;

/// <summary>
/// 커스텀 모달 알림 및 확인 다이얼로그를 표시하는 서비스 인터페이스입니다.
/// </summary>
public interface IDialogService
{
    Task ShowAlertAsync(string title, string message, string confirmText = "확인");
    Task<bool> ShowConfirmAsync(string title, string message, string confirmText = "확인", string cancelText = "취소", bool isDanger = false);
}

