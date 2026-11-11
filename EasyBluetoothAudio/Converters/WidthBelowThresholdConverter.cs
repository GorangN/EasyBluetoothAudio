using System.Globalization;
using System.Windows.Data;

namespace EasyBluetoothAudio.Converters;

/// <summary>
/// Selects the compact layout when measured content cannot hold a horizontal action group.
/// </summary>
public sealed class WidthBelowThresholdConverter : IValueConverter
{
    /// <summary>
    /// Compares a measured width with the threshold supplied by the view.
    /// </summary>
    /// <param name="value">The measured width in device-independent pixels.</param>
    /// <param name="targetType">The binding target type.</param>
    /// <param name="parameter">The invariant-culture width at which the layout changes.</param>
    /// <param name="culture">The binding culture.</param>
    /// <returns>Whether the available width is below the threshold.</returns>
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is double width && double.TryParse(parameter?.ToString(), CultureInfo.InvariantCulture, out var threshold)
            && width < threshold;
    }

    /// <summary>
    /// Leaves the read-only measurement binding unchanged.
    /// </summary>
    /// <param name="value">The target value.</param>
    /// <param name="targetType">The source type.</param>
    /// <param name="parameter">The supplied threshold.</param>
    /// <param name="culture">The binding culture.</param>
    /// <returns>The sentinel preventing a source update.</returns>
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return System.Windows.Data.Binding.DoNothing;
    }
}
