using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;

namespace WpfMvvmTemplate.Services;

/// <summary>
/// DI 컨테이너를 통해 ViewModel을 생성하고 화면/모달을 전환하는 내비게이션 서비스입니다.
/// </summary>
public sealed partial class NavigationService : ObservableObject, INavigationService
{
    private readonly IServiceProvider _serviceProvider;

    [ObservableProperty]
    private ObservableObject? _currentViewModel;

    [ObservableProperty]
    private ObservableObject? _currentModalViewModel;

    public NavigationService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public void NavigateTo<TViewModel>() where TViewModel : ObservableObject
    {
        CurrentModalViewModel = null;
        CurrentViewModel = _serviceProvider.GetRequiredService<TViewModel>();
    }

    public void OpenModal<TViewModel>() where TViewModel : ObservableObject
    {
        CurrentModalViewModel = _serviceProvider.GetRequiredService<TViewModel>();
    }

    public void OpenModal(ObservableObject viewModel)
    {
        CurrentModalViewModel = viewModel;
    }

    public void CloseModal()
    {
        CurrentModalViewModel = null;
    }
}
