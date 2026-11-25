using CommunityToolkit.Mvvm.ComponentModel;
using WpfMvvmTemplate.Services;

namespace WpfMvvmTemplate.ViewModels;

/// <summary>
/// 메인 셸(MainWindow)의 ViewModel입니다.
/// </summary>
public sealed partial class MainViewModel : ObservableObject
{
    public INavigationService Navigation { get; }

    [ObservableProperty]
    private string _windowTitle = "WPF MVVM Template";

    public MainViewModel(INavigationService navigation)
    {
        Navigation = navigation;
        Navigation.NavigateTo<HomeViewModel>();
    }
}

