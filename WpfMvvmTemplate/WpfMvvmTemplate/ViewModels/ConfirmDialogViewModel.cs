using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace WpfMvvmTemplate.ViewModels;

/// <summary>
/// 공용 확인/알림 모달의 ViewModel입니다.
/// </summary>
public sealed partial class ConfirmDialogViewModel : ObservableObject
{
    private readonly Action<bool> _onClose;

    public string Title { get; }
    public string Message { get; }
    public string ConfirmText { get; }
    public string CancelText { get; }
    public bool IsConfirmMode { get; }
    public bool IsDanger { get; }

    public ConfirmDialogViewModel(
        string title,
        string message,
        string confirmText,
        string cancelText,
        bool isConfirmMode,
        bool isDanger,
        Action<bool> onClose)
    {
        Title = title;
        Message = message;
        ConfirmText = confirmText;
        CancelText = cancelText;
        IsConfirmMode = isConfirmMode;
        IsDanger = isDanger;
        _onClose = onClose;
    }

    [RelayCommand]
    private void Confirm() => _onClose(true);

    [RelayCommand]
    private void Cancel() => _onClose(false);
}

