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
    /// Verifies that the borderless tray window retains native movement at a fixed user-facing size.
    /// </summary>
    [Fact]
    public void MainWindow_ProvidesCaptionWithoutManualResizing()
    {
        var window = LoadView("MainWindow.xaml");

        Assert.Equal("NoResize", window.Attribute("ResizeMode")?.Value);
        Assert.Equal("386", window.Attribute("Width")?.Value);
        Assert.Equal("278", window.Attribute("Height")?.Value);
        Assert.Equal("320", window.Attribute("MinWidth")?.Value);
        Assert.Equal("236", window.Attribute("MinHeight")?.Value);
        Assert.NotEqual("True", window.Attribute("AllowsTransparency")?.Value);
        var chrome = Assert.Single(window.Descendants(Presentation + "WindowChrome"));
        Assert.Equal(44, double.Parse(chrome.Attribute("CaptionHeight")!.Value, CultureInfo.InvariantCulture));
        Assert.Equal("1", chrome.Attribute("GlassFrameThickness")?.Value);
        Assert.Equal("0", chrome.Attribute("CornerRadius")?.Value);
        Assert.Equal("0", chrome.Attribute("ResizeBorderThickness")?.Value);
    }

    private static XElement LoadView(string name)
    {
        return XDocument.Load(Path.Combine(AppContext.BaseDirectory, "ViewMarkup", name)).Root!;
    }

    /// <summary>Header X and Escape share context-aware close behavior rather than hiding preferences.</summary>
    [Fact]
    public void MainWindow_HeaderCloseUsesPageAwareCommand()
    {
        var window = LoadView("MainWindow.xaml");
        var closeButton = Assert.Single(window.Descendants(Presentation + "Button"),
            button => button.Attribute("ToolTip")?.Value == "{Binding CloseActionLabel}");
        Assert.Equal("{Binding CloseOrHideCommand}", closeButton.Attribute("Command")?.Value);
        var settingsButton = Assert.Single(window.Descendants(Presentation + "Button"),
            button => button.Attribute("Command")?.Value == "{Binding OpenSettingsCommand}");
        Assert.Same(settingsButton.Parent, closeButton.Parent);
        Assert.Same(closeButton, settingsButton.ElementsAfterSelf().First());
        Assert.Single(window.Descendants(Presentation + "Button"),
            button => button.Attribute("Command")?.Value == "{Binding CloseOrHideCommand}");
        Assert.DoesNotContain(window.Descendants(Presentation + "Button"),
            button => button.Attribute("ToolTip")?.Value == "Minimize to tray");
        Assert.DoesNotContain(window.Descendants(Presentation + "TextBlock"),
            text => text.Attribute("Text")?.Value is "EasyBluetoothAudio" or "Easy Bluetooth Audio");
        Assert.Contains(window.Descendants(Presentation + "KeyBinding"),
            binding => binding.Attribute("Key")?.Value == "Escape"
                && binding.Attribute("Command")?.Value == "{Binding CloseOrHideCommand}");
    }
}
