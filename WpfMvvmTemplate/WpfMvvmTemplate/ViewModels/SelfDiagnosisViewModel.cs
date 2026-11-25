using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WpfMvvmTemplate.Models;
using WpfMvvmTemplate.Services;
using WpfMvvmTemplate.Services.Plc;

namespace WpfMvvmTemplate.ViewModels;

/// <summary>
/// 시스템 자가 진단 모달의 ViewModel입니다.
/// </summary>
public sealed partial class SelfDiagnosisViewModel : ObservableObject
{
    private readonly INavigationService _navigation;
    private readonly IPlcService _plcService;

    [ObservableProperty]
    private string _detailMessage = "전체 진단 시작을 누르거나, 개별 항목을 클릭해 진단을 실행하세요.";

    public ObservableCollection<DiagnosisItem> Items { get; }

    public SelfDiagnosisViewModel(
        INavigationService navigation,
        IPlcService plcService,
        ISettingsService settingsService)
    {
        _navigation = navigation;
        _plcService = plcService;

        Items =
        [
            new DiagnosisItem
            {
                Title = "PLC 제어기 진단",
                TargetInfo = settingsService.PlcSettings.SummaryText,
                Icon = FindIcon("IconPlcChip"),
                RunCommand = DiagnosePlcCommand
            }
        ];
    }

    private static Geometry? FindIcon(string resourceKey)
        => Application.Current?.TryFindResource(resourceKey) as Geometry;

    [RelayCommand]
    private async Task DiagnosePlcAsync()
    {
        var plcItem = Items[0];
        await RunItemAsync(plcItem, () => _plcService.CheckConnectionAsync());
    }

    [RelayCommand]
    private async Task StartDiagnosisAsync()
    {
        DetailMessage = "전체 시스템 자가 진단을 진행하고 있습니다...";

        foreach (var item in Items)
        {
            await item.RunCommand.ExecuteAsync(null);
        }

        var errorCount = Items.Count(i => i.Status == DiagnosisStatus.Error);
        DetailMessage = errorCount == 0
            ? "모든 점검 항목이 정상입니다."
            : $"{errorCount}개 항목에서 오류가 감지되었습니다.";
    }

    [RelayCommand]
    private void Close()
    {
        _navigation.CloseModal();
    }

    private async Task RunItemAsync(DiagnosisItem item, Func<Task<(bool IsOk, string Message)>> checkFunc)
    {
        item.Status = DiagnosisStatus.Checking;
        item.SubMessage = "연결 상태를 확인하고 있습니다...";
        DetailMessage = $"{item.Title}을(를) 진행하고 있습니다...";

        try
        {
            var (isOk, message) = await checkFunc();
            item.Status = isOk ? DiagnosisStatus.Normal : DiagnosisStatus.Error;
            item.SubMessage = message;
            DetailMessage = isOk
                ? $"{item.Title} 완료: 정상"
                : $"{item.Title} 실패: {message}";
        }
        catch (Exception ex)
        {
            item.Status = DiagnosisStatus.Error;
            item.SubMessage = ex.Message;
            DetailMessage = $"{item.Title} 중 오류가 발생했습니다.";
        }
    }
}
