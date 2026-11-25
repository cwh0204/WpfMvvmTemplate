using System.Windows.Input;
using System.Windows.Media;

namespace WpfMvvmTemplate.Models;

/// <summary>
/// 홈 화면 메뉴 카드 항목 모델입니다.
/// </summary>
public sealed class MenuCardItem
{
    public required string Title { get; init; }
    public string Subtitle { get; init; } = string.Empty;
    public Geometry? Icon { get; init; }
    public required ICommand Command { get; init; }
}
