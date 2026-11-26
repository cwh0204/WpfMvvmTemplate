using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace WpfMvvmTemplate.Services;

/// <summary>
/// ViewModel 간 화면 전환 및 모달 오버레이를 담당하는 내비게이션 서비스 인터페이스입니다.
/// </summary>
public interface INavigationService : INotifyPropertyChanged
{
    ObservableObject? CurrentViewModel { get; }
    ObservableObject? CurrentModalViewModel { get; }

    void NavigateTo<TViewModel>() where TViewModel : ObservableObject;
    void OpenModal<TViewModel>() where TViewModel : ObservableObject;
    void OpenModal(ObservableObject viewModel);
    void CloseModal();
}
