using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WpfMvvmTemplate.Services;

namespace WpfMvvmTemplate.ViewModels;

/// <summary>
/// 설정 화면 진입 전 비밀번호를 확인하는 모달 ViewModel입니다.
/// </summary>
public sealed partial class PasswordPromptViewModel : ObservableObject
{
    private readonly INavigationService _navigation;
    private readonly ISettingsService _settingsService;

    [ObservableProperty]
    private string _password = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    private string _errorMessage = string.Empty;

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    public PasswordPromptViewModel(INavigationService navigation, ISettingsService settingsService)
    {
        _navigation = navigation;
        _settingsService = settingsService;
    }

    partial void OnPasswordChanged(string value)
    {
        if (HasError && !string.IsNullOrEmpty(value))
        {
            ErrorMessage = string.Empty;
        }
    }

    [RelayCommand]
    private void Confirm()
    {
        if (_settingsService.VerifyPassword(Password))
        {
            _navigation.NavigateTo<SettingsViewModel>();
            return;
        }

        ErrorMessage = "비밀번호가 일치하지 않습니다.";
        Password = string.Empty;
    }

    [RelayCommand]
    private void Cancel()
    {
        _navigation.CloseModal();
    }
}

