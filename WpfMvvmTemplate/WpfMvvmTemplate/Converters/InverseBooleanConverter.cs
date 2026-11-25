using System.Globalization;
using System.Windows.Data;

namespace WpfMvvmTemplate.Converters;

/// <summary>
/// bool 값을 반전시키는 공용 XAML 컨버터입니다.
/// </summary>
public sealed class InverseBooleanConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is bool b ? !b : value;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => value is bool b ? !b : value;
}

