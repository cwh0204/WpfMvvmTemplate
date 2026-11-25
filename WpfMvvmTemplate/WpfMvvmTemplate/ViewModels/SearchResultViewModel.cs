using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WpfMvvmTemplate.Models;
using WpfMvvmTemplate.Services;

namespace WpfMvvmTemplate.ViewModels;

/// <summary>
/// 결과 조회(Master-Detail) 화면의 ViewModel입니다.
/// </summary>
public sealed partial class SearchResultViewModel : ObservableObject
{
    private readonly INavigationService _navigation;
    private readonly IResultRepository _resultRepository;
    private readonly IDialogService _dialogService;

    [ObservableProperty]
    private DateTime? _startDate = DateTime.Today.AddMonths(-1);

    [ObservableProperty]
    private DateTime? _endDate = DateTime.Today;

    [ObservableProperty]
    private string _searchKeyword = string.Empty;

    [ObservableProperty]
    private string _selectedJudgmentFilter = "전체";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(DeleteRecordCommand))]
    [NotifyCanExecuteChangedFor(nameof(PrintCommand))]
    private ResultRecord? _selectedRecord;

    public IReadOnlyList<string> JudgmentOptions { get; } = ["전체", "OK", "NG"];

    public ObservableCollection<ResultRecord> FilteredRecords { get; } = [];

    public SearchResultViewModel(
        INavigationService navigation,
        IResultRepository resultRepository,
        IDialogService dialogService)
    {
        _navigation = navigation;
        _resultRepository = resultRepository;
        _dialogService = dialogService;

        _ = SearchAsync();
    }

    private bool HasSelectedRecord => SelectedRecord is not null;

    [RelayCommand]
    private async Task SearchAsync()
    {
        var records = await _resultRepository.SearchAsync(
            StartDate,
            EndDate,
            SearchKeyword,
            SelectedJudgmentFilter);

        FilteredRecords.Clear();
        foreach (var record in records)
        {
            FilteredRecords.Add(record);
        }

        SelectedRecord = FilteredRecords.FirstOrDefault();
    }

    [RelayCommand]
    private async Task ResetFilterAsync()
    {
        StartDate = DateTime.Today.AddMonths(-1);
        EndDate = DateTime.Today;
        SearchKeyword = string.Empty;
        SelectedJudgmentFilter = "전체";
        await SearchAsync();
    }

    [RelayCommand(CanExecute = nameof(HasSelectedRecord))]
    private async Task DeleteRecordAsync()
    {
        if (SelectedRecord is null)
            return;

        var target = SelectedRecord;
        var confirmed = await _dialogService.ShowConfirmAsync(
            "이력 삭제",
            $"'{target.TestNumber}' 시험 이력을 삭제하시겠습니까?",
            confirmText: "삭제",
            cancelText: "취소",
            isDanger: true);

        if (!confirmed)
            return;

        await _resultRepository.DeleteAsync(target.Id);
        FilteredRecords.Remove(target);
        SelectedRecord = FilteredRecords.FirstOrDefault();
    }

    [RelayCommand(CanExecute = nameof(HasSelectedRecord))]
    private async Task PrintAsync()
    {
        if (SelectedRecord is null)
            return;

        // TODO: 실제 리포트/PDF 출력 서비스 호출로 교체
        await _dialogService.ShowAlertAsync("인쇄 안내", $"'{SelectedRecord.TestNumber}' 성적서 출력 기능은 프로젝트에 맞게 연결하세요.");
    }

    [RelayCommand]
    private void ReturnToMain()
    {
        _navigation.NavigateTo<HomeViewModel>();
    }
}
