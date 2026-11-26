using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WpfMvvmTemplate.Models;
using WpfMvvmTemplate.Services;

namespace WpfMvvmTemplate.ViewModels;

/// <summary>
/// 홈 대시보드 화면의 ViewModel입니다.
/// </summary>
public sealed partial class HomeViewModel : ObservableObject
{
    private readonly INavigationService _navigation;
    private readonly ISettingsService _settingsService;
    private readonly IDialogService _dialogService;

    [ObservableProperty]
    private string _title = "시스템 제어 프로그램";

    [ObservableProperty]
    private string _subtitle = "W P F   M V V M   T E M P L A T E";

    public ObservableCollection<MenuCardItem> MenuItems { get; }

    public HomeViewModel(
        INavigationService navigation,
        ISettingsService settingsService,
        IDialogService dialogService)
    {
        _navigation = navigation;
        _settingsService = settingsService;
        _dialogService = dialogService;

        MenuItems =
        [
            new MenuCardItem
            {
                Title = "시 험",
                Subtitle = "Auto & Manual Test",
                Icon = FindIcon("IconGauge"),
                Command = OpenTestCommand
            },
            new MenuCardItem
            {
                Title = "결과 검색",
                Subtitle = "Test History",
                Icon = FindIcon("IconSearchChart"),
                Command = SearchResultCommand
            },
            new MenuCardItem
            {
                Title = "자가 진단",
                Subtitle = "System Check",
                Icon = FindIcon("IconChecklist"),
                Command = SelfDiagnosisCommand
            },
            new MenuCardItem
            {
                Title = "설 정",
                Subtitle = "Configuration",
                Icon = FindIcon("IconSettings"),
                Command = OpenSettingsCommand
            },
            new MenuCardItem
            {
                Title = "종 료",
                Subtitle = "Exit System",
                Icon = FindIcon("IconPower"),
                Command = ExitCommand
            }
        ];
    }

    private static Geometry? FindIcon(string resourceKey)
        => Application.Current?.TryFindResource(resourceKey) as Geometry;

    [RelayCommand]
    private void OpenTest()
    {
        _navigation.NavigateTo<TestViewModel>();
    }

    [RelayCommand]
    private void SearchResult()
    {
        _navigation.NavigateTo<SearchResultViewModel>();
    }

    [RelayCommand]
    private void SelfDiagnosis()
    {
        _navigation.OpenModal<SelfDiagnosisViewModel>();
    }

    [RelayCommand]
    private void OpenSettings()
    {
        if (_settingsService.IsPasswordEnabled)
        {
            _navigation.OpenModal<PasswordPromptViewModel>();
            return;
        }

        _navigation.NavigateTo<SettingsViewModel>();
    }

    [RelayCommand]
    private async Task ExitAsync()
    {
        var confirmed = await _dialogService.ShowConfirmAsync(
            "프로그램 종료",
            "프로그램을 종료하시겠습니까?",
            confirmText: "종료",
            cancelText: "취소",
            isDanger: true);

        if (confirmed)
        {
            Application.Current?.Shutdown();
        }
    }
}

