using WpfMvvmTemplate.ViewModels;

namespace WpfMvvmTemplate.Services;

/// <summary>
/// INavigationService의 모달 레이어를 사용해 커스텀 확인/알림 다이얼로그를 띄우는 서비스입니다.
/// </summary>
public sealed class DialogService : IDialogService
{
    private readonly INavigationService _navigation;

    public DialogService(INavigationService navigation)
    {
        _navigation = navigation;
    }

    public Task ShowAlertAsync(string title, string message, string confirmText = "확인")
    {
        var tcs = new TaskCompletionSource<bool>();
        var vm = new ConfirmDialogViewModel(
            title: title,
            message: message,
            confirmText: confirmText,
            cancelText: string.Empty,
            isConfirmMode: false,
            isDanger: false,
            onClose: result =>
            {
                _navigation.CloseModal();
                tcs.TrySetResult(result);
            });

        _navigation.OpenModal(vm);
        return tcs.Task;
    }

    public Task<bool> ShowConfirmAsync(string title, string message, string confirmText = "확인", string cancelText = "취소", bool isDanger = false)
    {
        var tcs = new TaskCompletionSource<bool>();
        var vm = new ConfirmDialogViewModel(
            title: title,
            message: message,
            confirmText: confirmText,
            cancelText: cancelText,
            isConfirmMode: true,
            isDanger: isDanger,
            onClose: result =>
            {
                _navigation.CloseModal();
                tcs.TrySetResult(result);
            });

        _navigation.OpenModal(vm);
        return tcs.Task;
    }
}

