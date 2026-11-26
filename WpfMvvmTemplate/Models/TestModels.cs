using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace WpfMvvmTemplate.Models;

/// <summary>
/// 시험 항목 선택 트리의 하위 항목 모델입니다.
/// </summary>
public sealed partial class TestSubItem : ObservableObject
{
    private readonly Action? _onSelectionChanged;

    public required string CheckBoxTitle { get; init; }
    public required string ItemName { get; init; }
    public string Category { get; init; } = "-";
    public string StandardSpec { get; init; } = "-";

    [ObservableProperty]
    private bool _isSelected = true;

    public TestSubItem(Action? onSelectionChanged = null)
    {
        _onSelectionChanged = onSelectionChanged;
    }

    partial void OnIsSelectedChanged(bool value)
    {
        _onSelectionChanged?.Invoke();
    }
}

/// <summary>
/// 삼중 상태(전체/부분/해제)를 지원하는 상위 시험 항목 그룹 모델입니다.
/// </summary>
public sealed partial class TestSelectionGroup : ObservableObject
{
    private readonly Action? _onSelectionChanged;
    private bool _isSyncing;

    public required string Title { get; init; }
    public ObservableCollection<TestSubItem> SubItems { get; } = [];
    public bool HasSubItems => SubItems.Count > 0;

    [ObservableProperty]
    private bool? _isChecked = true;

    public TestSelectionGroup(Action? onSelectionChanged = null)
    {
        _onSelectionChanged = onSelectionChanged;
    }

    public void AddSubItem(string checkBoxTitle, string itemName, string category, string standardSpec, bool isSelected = true)
    {
        var sub = new TestSubItem(() =>
        {
            SyncFromSubItems();
            _onSelectionChanged?.Invoke();
        })
        {
            CheckBoxTitle = checkBoxTitle,
            ItemName = itemName,
            Category = category,
            StandardSpec = standardSpec,
            IsSelected = isSelected
        };

        SubItems.Add(sub);
        SyncFromSubItems();
    }

    partial void OnIsCheckedChanged(bool? value)
    {
        if (_isSyncing)
            return;

        // 삼중 상태에서 사용자가 직접 클릭해 null이 되면 전체 해제로 전환
        bool target = value ?? false;
        _isSyncing = true;
        foreach (var sub in SubItems)
        {
            sub.IsSelected = target;
        }
        IsChecked = target;
        _isSyncing = false;

        _onSelectionChanged?.Invoke();
    }

    private void SyncFromSubItems()
    {
        if (_isSyncing || SubItems.Count == 0)
            return;

        _isSyncing = true;
        int selectedCount = SubItems.Count(s => s.IsSelected);
        IsChecked = selectedCount == 0
            ? false
            : selectedCount == SubItems.Count
                ? true
                : null;
        _isSyncing = false;
    }
}

/// <summary>
/// 시험 화면 하단 DataGrid에 표시되는 실시간 스텝 결과 행 모델입니다.
/// </summary>
public sealed partial class TestExecutionItem : ObservableObject
{
    public int StepNo { get; init; }
    public required string ItemName { get; init; }
    public string Category { get; init; } = "-";
    public string StandardSpec { get; init; } = "-";

    [ObservableProperty]
    private string _measuredValue = "대기 중";

    [ObservableProperty]
    private string _result = "대기"; // "대기" | "진행중" | "OK" | "NG"
}

