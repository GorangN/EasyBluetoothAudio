using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace EasyBluetoothAudio.Converters;

/// <summary>
/// Hides absent or blank optional feedback without reserving empty layout space.
/// </summary>
public class NullToCollapsedConverter : IValueConverter
{
    /// <inheritdoc />
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value == null || value is string text && string.IsNullOrWhiteSpace(text)
            ? Visibility.Collapsed : Visibility.Visible;
    }

    /// <inheritdoc />
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return System.Windows.Data.Binding.DoNothing;
    }
}
