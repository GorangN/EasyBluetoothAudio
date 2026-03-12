using System.Globalization;
using System.Windows;
using EasyBluetoothAudio.Converters;

namespace EasyBluetoothAudio.Tests.Converters;

public class InverseBooleanToVisibilityConverterTests
{
    private readonly InverseBooleanToVisibilityConverter _converter;

    public InverseBooleanToVisibilityConverterTests()
    {
        _converter = new InverseBooleanToVisibilityConverter();
    }

    [Fact]
    public void Convert_WhenValueIsTrue_ReturnsCollapsed()
    {
        var result = _converter.Convert(true, typeof(Visibility), null!, CultureInfo.InvariantCulture);

        Assert.Equal(Visibility.Collapsed, result);
    }

    [Fact]
    public void Convert_WhenValueIsFalse_ReturnsVisible()
    {
        var result = _converter.Convert(false, typeof(Visibility), null!, CultureInfo.InvariantCulture);

        Assert.Equal(Visibility.Visible, result);
    }

    [Fact]
    public void Convert_WhenValueIsNotBoolean_ReturnsVisible()
    {
        var result = _converter.Convert("not a boolean", typeof(Visibility), null!, CultureInfo.InvariantCulture);

        Assert.Equal(Visibility.Visible, result);
    }

    [Fact]
    public void Convert_WhenValueIsNull_ReturnsVisible()
    {
        var result = _converter.Convert(null!, typeof(Visibility), null!, CultureInfo.InvariantCulture);

        Assert.Equal(Visibility.Visible, result);
    }

    [Fact]
    public void ConvertBack_WhenValueIsCollapsed_ReturnsTrue()
    {
        var result = _converter.ConvertBack(Visibility.Collapsed, typeof(bool), null!, CultureInfo.InvariantCulture);

        Assert.True((bool)result);
    }

    [Fact]
    public void ConvertBack_WhenValueIsHidden_ReturnsTrue()
    {
        var result = _converter.ConvertBack(Visibility.Hidden, typeof(bool), null!, CultureInfo.InvariantCulture);

        Assert.True((bool)result);
    }

    [Fact]
    public void ConvertBack_WhenValueIsVisible_ReturnsFalse()
    {
        var result = _converter.ConvertBack(Visibility.Visible, typeof(bool), null!, CultureInfo.InvariantCulture);

        Assert.False((bool)result);
    }

    [Fact]
    public void ConvertBack_WhenValueIsNotVisibility_ReturnsFalse()
    {
        var result = _converter.ConvertBack("not visibility", typeof(bool), null!, CultureInfo.InvariantCulture);

        Assert.False((bool)result);
    }

    [Fact]
    public void ConvertBack_WhenValueIsNull_ReturnsFalse()
    {
        var result = _converter.ConvertBack(null!, typeof(bool), null!, CultureInfo.InvariantCulture);

        Assert.False((bool)result);
    }
}
