using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace WpfMvvmTemplate.Models;

/// <summary>
/// 자가 진단 항목의 상태를 나타냅니다.
/// </summary>
public enum DiagnosisStatus
{
    Idle,
    Checking,
    Normal,
    Error
}

/// <summary>
/// 자가 진단 모달의 개별 점검 항목 모델입니다.
/// </summary>
public sealed partial class DiagnosisItem : ObservableObject
{
    public required string Title { get; init; }
    public string TargetInfo { get; init; } = string.Empty;
    public Geometry? Icon { get; init; }
    public required IAsyncRelayCommand RunCommand { get; init; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StatusText))]
    private DiagnosisStatus _status = DiagnosisStatus.Idle;

    [ObservableProperty]
    private string _subMessage = "점검 대기 중";

    public string StatusText => Status switch
    {
        DiagnosisStatus.Idle => "대기",
        DiagnosisStatus.Checking => "점검중",
        DiagnosisStatus.Normal => "정상",
        DiagnosisStatus.Error => "오류",
        _ => "대기"
    };
}
