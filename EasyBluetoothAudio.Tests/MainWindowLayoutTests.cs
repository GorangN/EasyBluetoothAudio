using System.Globalization;
using System.IO;
using System.Xml.Linq;

namespace EasyBluetoothAudio.Tests;

/// <summary>
/// Guards the native window-interaction contract declared by the production XAML.
/// </summary>
public class MainWindowLayoutTests
{
    private static readonly XNamespace Presentation = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";

    /// <summary>
    /// Verifies that the borderless tray window retains native movement and resizing behavior.
    /// </summary>
    [Fact]
    public void MainWindow_ProvidesCaptionAndResizeBorders()
    {
        var window = LoadView("MainWindow.xaml");

        Assert.Equal("CanResize", window.Attribute("ResizeMode")?.Value);
        var chrome = Assert.Single(window.Descendants(Presentation + "WindowChrome"));
        Assert.True(double.Parse(chrome.Attribute("CaptionHeight")!.Value, CultureInfo.InvariantCulture) >= 44);
        Assert.True(double.Parse(chrome.Attribute("ResizeBorderThickness")!.Value, CultureInfo.InvariantCulture) > 0);
    }

    private static XElement LoadView(string name)
    {
        return XDocument.Load(Path.Combine(AppContext.BaseDirectory, "ViewMarkup", name)).Root!;
    }
}
