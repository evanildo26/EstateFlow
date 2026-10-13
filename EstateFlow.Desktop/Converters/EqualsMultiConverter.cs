using System.Globalization;
using System.Windows.Data;

namespace EstateFlow.Desktop.Converters;

// True when all bound values are equal, e.g. a sidebar item's screen name and
// the shell's CurrentScreen (to highlight the active page).
public class EqualsMultiConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object? parameter, CultureInfo culture) =>
        values.Length >= 2 &&
        values.Skip(1).All(v => Equals(v?.ToString(), values[0]?.ToString()));

    public object[] ConvertBack(object value, Type[] targetTypes, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
