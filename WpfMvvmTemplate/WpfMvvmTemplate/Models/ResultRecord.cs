using System.Collections.ObjectModel;

namespace WpfMvvmTemplate.Models;

/// <summary>
/// 결과 조회 화면의 시험 이력 레코드 모델입니다.
/// </summary>
public sealed class ResultRecord
{
    public long Id { get; set; }
    public required string TestNumber { get; init; }
    public required DateTime TestedAt { get; init; }
    public string TestedDate => TestedAt.ToString("yyyy-MM-dd");
    public required string OverallResult { get; init; } // "OK" | "NG"
    public string TesterName { get; init; } = "-";
    public string CarNumber { get; init; } = "-";
    public string FormationNumber { get; init; } = "-";
    public string SerialNumber { get; init; } = "-";

    public ObservableCollection<ResultDetailItem> Details { get; init; } = [];

    public int ItemCount => Details.Count;
    public int PassedCount => Details.Count(d => string.Equals(d.Result, "OK", StringComparison.OrdinalIgnoreCase));
}

/// <summary>
/// 결과 조회 화면의 상세 계측/점검 항목 모델입니다.
/// </summary>
public sealed class ResultDetailItem
{
    public int StepNo { get; init; }
    public required string ItemName { get; init; }
    public string Category { get; init; } = "-";
    public string StandardSpec { get; init; } = "-";
    public string MeasuredValue { get; init; } = "-";
    public required string Result { get; init; } // "OK" | "NG"
}
