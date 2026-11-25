using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WpfMvvmTemplate.Models;
using WpfMvvmTemplate.Services;
using WpfMvvmTemplate.Services.Plc;

namespace WpfMvvmTemplate.ViewModels;

/// <summary>
/// 장비 시험(자동 시험 / 매뉴얼 시험) 화면의 ViewModel입니다.
/// </summary>
public sealed partial class TestViewModel : ObservableObject
{
    private readonly INavigationService _navigation;
    private readonly IResultRepository _resultRepository;
    private readonly IDialogService _dialogService;
    private readonly IPlcService _plcService;
    private CancellationTokenSource? _testCts;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsManualMode))]
    private bool _isAutoMode = true;

    public bool IsManualMode => !IsAutoMode;

    [ObservableProperty]
    private bool _isDeviceConnected;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartAutoTestCommand))]
    [NotifyCanExecuteChangedFor(nameof(StopAutoTestCommand))]
    [NotifyCanExecuteChangedFor(nameof(ReturnToMainCommand))]
    [NotifyCanExecuteChangedFor(nameof(SwitchToAutoCommand))]
    [NotifyCanExecuteChangedFor(nameof(SwitchToManualCommand))]
    private bool _isRunning;

    // 필수 공통 시험 정보
    [ObservableProperty]
    private string _testNumber = $"TEST-{DateTime.Now:yyyyMMdd}-001";

    [ObservableProperty]
    private string _testerName = string.Empty;

    [ObservableProperty]
    private string _carNumber = string.Empty;

    [ObservableProperty]
    private string _formationNumber = string.Empty;

    [ObservableProperty]
    private string _serialNumber = string.Empty;

    // 실시간 계측 대시보드 상태
    [ObservableProperty]
    private string _activeStepTitle = "시험 대기 중";

    [ObservableProperty]
    private string _activeCategoryBadge = "READY";

    [ObservableProperty]
    private string _statusMessage = "시험 정보를 입력하고 시험 시작 버튼을 누르세요.";

    [ObservableProperty]
    private string _activeLiveMetric = "-";

    // 장비별 실시간 센서 예시 값 (프로젝트에 맞게 교체)
    [ObservableProperty]
    private double _sensor1Value;

    [ObservableProperty]
    private double _sensor2Value;

    [ObservableProperty]
    private double _sensor3Value;

    [ObservableProperty]
    private double _sensor4Value;

    public ObservableCollection<TestSelectionGroup> TestSelectionGroups { get; } = [];
    public ObservableCollection<TestExecutionItem> SelectedTestItems { get; } = [];

    [ObservableProperty]
    private TestExecutionItem? _selectedTestItem;

    public TestViewModel(
        INavigationService navigation,
        IResultRepository resultRepository,
        IDialogService dialogService,
        IPlcService plcService)
    {
        _navigation = navigation;
        _resultRepository = resultRepository;
        _dialogService = dialogService;
        _plcService = plcService;

        _isDeviceConnected = _plcService.IsConnected;
        _plcService.ConnectionStateChanged += OnPlcConnectionChanged;

        InitializeSampleTestGroups();
        RebuildSelectedTestItems();
    }

    private void OnPlcConnectionChanged(object? sender, bool connected)
    {
        Application.Current?.Dispatcher.Invoke(() => IsDeviceConnected = connected);
    }

    private bool CanSwitchMode => !IsRunning;
    private bool CanStartTest => !IsRunning && SelectedTestItems.Count > 0;
    private bool CanReturnToMain => !IsRunning;

    [RelayCommand(CanExecute = nameof(CanSwitchMode))]
    private void SwitchToAuto() => IsAutoMode = true;

    [RelayCommand(CanExecute = nameof(CanSwitchMode))]
    private void SwitchToManual() => IsAutoMode = false;

    [RelayCommand(CanExecute = nameof(CanStartTest))]
    private async Task StartAutoTestAsync()
    {
        IsRunning = true;
        _testCts = new CancellationTokenSource();
        var token = _testCts.Token;

        foreach (var item in SelectedTestItems)
        {
            item.MeasuredValue = "대기 중";
            item.Result = "대기";
        }

        try
        {
            foreach (var item in SelectedTestItems)
            {
                token.ThrowIfCancellationRequested();

                SelectedTestItem = item;
                ActiveStepTitle = $"[{item.StepNo}] {item.ItemName}";
                ActiveCategoryBadge = item.Category;
                StatusMessage = $"{item.ItemName} ({item.Category}) 시험을 진행하고 있습니다...";
                item.Result = "진행중";
                item.MeasuredValue = "계측 중...";

                // TODO: 실제 장비 제어 및 계측 서비스 호출로 교체
                await Task.Delay(700, token);

                item.MeasuredValue = "정상 범위";
                item.Result = "OK";
                ActiveLiveMetric = item.MeasuredValue;
            }

            bool isAllPassed = SelectedTestItems.All(i => string.Equals(i.Result, "OK", StringComparison.OrdinalIgnoreCase));
            var record = new ResultRecord
            {
                TestNumber = string.IsNullOrWhiteSpace(TestNumber) ? $"TEST-{DateTime.Now:yyyyMMdd-HHmmss}" : TestNumber.Trim(),
                TestedAt = DateTime.Now,
                OverallResult = isAllPassed ? "OK" : "NG",
                TesterName = string.IsNullOrWhiteSpace(TesterName) ? "-" : TesterName.Trim(),
                CarNumber = string.IsNullOrWhiteSpace(CarNumber) ? "-" : CarNumber.Trim(),
                FormationNumber = string.IsNullOrWhiteSpace(FormationNumber) ? "-" : FormationNumber.Trim(),
                SerialNumber = string.IsNullOrWhiteSpace(SerialNumber) ? "-" : SerialNumber.Trim(),
                Details = new ObservableCollection<ResultDetailItem>(
                    SelectedTestItems.Select(i => new ResultDetailItem
                    {
                        StepNo = i.StepNo,
                        ItemName = i.ItemName,
                        Category = i.Category,
                        StandardSpec = i.StandardSpec,
                        MeasuredValue = i.MeasuredValue,
                        Result = i.Result
                    }))
            };

            await _resultRepository.SaveAsync(record);

            ActiveStepTitle = "전체 시험 완료";
            ActiveCategoryBadge = "DONE";
            StatusMessage = "선택된 모든 시험 항목이 완료되어 DB에 저장되었습니다.";
            await _dialogService.ShowAlertAsync("시험 완료", $"시험 결과({record.TestNumber})가 저장되었습니다.");
        }
        catch (OperationCanceledException)
        {
            ActiveStepTitle = "시험 중단됨";
            ActiveCategoryBadge = "STOP";
            StatusMessage = "사용자에 의해 시험이 중단되었습니다.";
        }
        finally
        {
            _testCts.Dispose();
            _testCts = null;
            IsRunning = false;
        }
    }

    [RelayCommand(CanExecute = nameof(IsRunning))]
    private void StopAutoTest()
    {
        _testCts?.Cancel();
    }

    [RelayCommand(CanExecute = nameof(CanReturnToMain))]
    private void ReturnToMain()
    {
        _plcService.ConnectionStateChanged -= OnPlcConnectionChanged;
        _navigation.NavigateTo<HomeViewModel>();
    }

    private void InitializeSampleTestGroups()
    {
        // TODO: 각 시험기 프로젝트의 실제 시험 스텝 구성으로 교체
        var group1 = new TestSelectionGroup(RebuildSelectedTestItems) { Title = "1. 기본 동작 시험" };
        group1.AddSubItem("1차 점검", "기본 동작 확인", "1차", "4.8 ~ 5.2 V");
        group1.AddSubItem("2차 점검", "기본 동작 확인", "2차", "4.8 ~ 5.2 V");

        var group2 = new TestSelectionGroup(RebuildSelectedTestItems) { Title = "2. 응답 및 성능 시험" };
        group2.AddSubItem("응답 시간", "응답 시간 점검", "성능", "≤ 100 ms");
        group2.AddSubItem("내압 유지", "내압 시험", "안전", "0.50 ± 0.05 MPa");

        TestSelectionGroups.Add(group1);
        TestSelectionGroups.Add(group2);
    }

    private void RebuildSelectedTestItems()
    {
        SelectedTestItems.Clear();
        int stepNo = 1;

        foreach (var group in TestSelectionGroups)
        {
            foreach (var sub in group.SubItems.Where(s => s.IsSelected))
            {
                SelectedTestItems.Add(new TestExecutionItem
                {
                    StepNo = stepNo++,
                    ItemName = sub.ItemName,
                    Category = sub.Category,
                    StandardSpec = sub.StandardSpec
                });
            }
        }

        StartAutoTestCommand.NotifyCanExecuteChanged();
    }
}

