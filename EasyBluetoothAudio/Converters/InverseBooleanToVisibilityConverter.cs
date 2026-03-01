using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace EasyBluetoothAudio.Converters;

/// <summary>
/// Converts a boolean value to a Visibility enumeration, returning Collapsed when true and Visible when false.
/// </summary>
public class InverseBooleanToVisibilityConverter : IValueConverter
{
    /// <inheritdoc />
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isTrue)
        {
            return isTrue ? Visibility.Collapsed : Visibility.Visible;
        }

        return Visibility.Visible;
    }

    /// <inheritdoc />
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is Visibility visibility)
        {
            return visibility != Visibility.Visible;
        }

        return false;
    }
}
