using System;
using System.Globalization;
using System.Windows.Data;

namespace EasyBluetoothAudio.Core;

/// <summary>
/// Converts an enum value to a boolean for use with radio button bindings.
/// The <c>ConverterParameter</c> must be the string name of the enum member to match.
/// </summary>
public class EnumToBoolConverter : IValueConverter
{
    /// <inheritdoc />
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value?.ToString() == parameter?.ToString();
    }

    /// <inheritdoc />
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is true && parameter != null)
        {
            return Enum.Parse(targetType, parameter.ToString()!);
        }

        return System.Windows.Data.Binding.DoNothing;
    }
}
