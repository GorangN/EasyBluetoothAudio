using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace EasyBluetoothAudio.Converters;

/// <summary>
/// Converts a <see langword="null"/> value to <see cref="Visibility.Collapsed"/>
/// and any non-null value to <see cref="Visibility.Visible"/>.
/// </summary>
public class NullToCollapsedConverter : IValueConverter
{
    /// <inheritdoc />
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value == null ? Visibility.Collapsed : Visibility.Visible;
    }

    /// <inheritdoc />
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return System.Windows.Data.Binding.DoNothing;
    }
}
