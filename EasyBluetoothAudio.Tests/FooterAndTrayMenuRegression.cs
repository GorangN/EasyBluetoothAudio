using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using EasyBluetoothAudio.Views;

namespace EasyBluetoothAudio.Tests;

internal static class FooterAndTrayMenuRegression
{
    internal static void Verify(App app, string theme)
    {
        using var model = VisualPreviewFactory.Create();
        var errorView = new BluetoothConfigView { DataContext = model };
        var errorHost = new Border { Background = (Brush)app.FindResource("WindowBackgroundBrush"), Child = errorView };
        model.SelectedSource = model.Sources[2];
        Layout(errorHost, new Size(338, 238));
        Assert.Null(errorView.FindName("SourceScrollViewer"));
        var errorFooter = Assert.IsType<TextBlock>(errorView.FindName("SourceFooter"));
        Assert.Equal(model.SelectedSource.ErrorText, errorFooter.Text);
        Assert.False(string.IsNullOrEmpty(errorFooter.Text));
        AssertControlsFit(errorView, errorFooter);
        VisualRenderingRegression.Export(errorHost, $"error-footer-{theme}.png");
        model.UpdateStatusText = "Update available";
        model.DiscoveryError = "Couldn’t refresh devices. Check Bluetooth and try again.";
        model.IsRefreshing = true;
        Layout(errorHost, new Size(338, 238));
        Assert.Equal(model.SelectedSource.ErrorText, errorFooter.Text);
        model.SelectedSource = model.Sources[0];
        Layout(errorHost, new Size(338, 238));
        Assert.Equal(model.DiscoveryError, errorFooter.Text);
        AssertControlsFit(errorView, errorFooter);
        model.DiscoveryError = null;
        Layout(errorHost, new Size(338, 238));
        Assert.Equal("Refreshing devices…", errorFooter.Text);
        model.IsRefreshing = false;
        Layout(errorHost, new Size(338, 238));
        Assert.Equal(model.UpdateStatusText, errorFooter.Text);
        model.UpdateStatusText = "A new update is available. Restart to install.";
        var view = new BluetoothConfigView { DataContext = model };
        var host = new Border { Background = (Brush)app.FindResource("WindowBackgroundBrush"), Child = view };
        foreach (var size in new[] { new Size(272, 238), new Size(338, 238) })
        {
            Layout(host, size);
            var status = Assert.Single(Descendants<TextBlock>(view), text => text.Text == model.UpdateStatusText);
            var content = Assert.IsType<StackPanel>(view.FindName("SourceContent"));
            Assert.DoesNotContain(status, Descendants<TextBlock>(content));
            var top = status.TranslatePoint(new Point(), view).Y;
            Assert.True(top >= content.TranslatePoint(new Point(), view).Y + content.ActualHeight);
            Assert.True(top + status.ActualHeight <= size.Height);
            Assert.True(status.ActualHeight > 0);
            Assert.Equal(TextWrapping.NoWrap, status.TextWrapping);
            Assert.Equal(TextTrimming.CharacterEllipsis, status.TextTrimming);
            Assert.Equal(status.Text, status.ToolTip);
            AssertControlsFit(view, status);
            VisualRenderingRegression.Export(host, $"update-footer-{theme}-{size.Width:0}.png");
        }

        model.UpdateStatusText = string.Empty;
        Layout(host, new Size(338, 238));
        var footer = Assert.Single(Descendants<TextBlock>(view), text => DockPanel.GetDock(text) == Dock.Bottom);
        Assert.Equal(Visibility.Collapsed, footer.Visibility);

        var window = new MainWindow();
        try
        {
            Assert.Equal(ResizeMode.NoResize, window.ResizeMode);
            Assert.Equal(new Thickness(0), System.Windows.Shell.WindowChrome.GetWindowChrome(window).ResizeBorderThickness);
            var menu = window.TrayIcon.ContextMenu;
            Layout(menu, new Size(240, 180));
            var labels = Descendants<TextBlock>(menu).Where(text => !string.IsNullOrWhiteSpace(text.Text)).ToArray();
            Assert.NotEmpty(labels);
            Assert.All(labels, label => Assert.Equal(SystemColors.MenuTextColor,
                Assert.IsType<SolidColorBrush>(label.Foreground).Color));
            Assert.Equal(SystemColors.MenuTextColor, Assert.IsType<SolidColorBrush>(menu.Foreground).Color);
            VisualRenderingRegression.Export(menu, $"tray-menu-{theme}.png");

            var sourceItem = new MenuItem { Header = "Preview phone" };
            window.DevicesTrayMenuItem.Items.Add(sourceItem);
            Layout(sourceItem, new Size(200, 30));
            var sourceLabel = Assert.Single(Descendants<TextBlock>(sourceItem), text => text.Text == "Preview phone");
            Assert.Equal(SystemColors.MenuTextColor, Assert.IsType<SolidColorBrush>(sourceLabel.Foreground).Color);
            sourceItem.IsEnabled = false;
            sourceItem.UpdateLayout();
            Assert.Equal(sourceItem.Foreground, sourceLabel.Foreground);
        }
        finally
        {
            window.TrayIcon.Dispose();
            window.Close();
        }
    }

    private static void AssertControlsFit(BluetoothConfigView view, TextBlock footer)
    {
        var footerTop = footer.TranslatePoint(new Point(), view).Y;
        Assert.All(Descendants<Button>(view).Where(button => button.Visibility == Visibility.Visible), button =>
        {
            Assert.True(button.TranslatePoint(new Point(), view).Y + button.ActualHeight <= footerTop,
                $"{button.Content} must fit above the footer.");
        });
    }

    private static void Layout(FrameworkElement element, Size size)
    {
        element.Measure(size);
        element.Arrange(new Rect(size));
        element.UpdateLayout();
    }

    private static IEnumerable<T> Descendants<T>(DependencyObject parent) where T : DependencyObject
    {
        for (int index = 0; index < VisualTreeHelper.GetChildrenCount(parent); index++)
        {
            var child = VisualTreeHelper.GetChild(parent, index);
            if (child is T match)
            {
                yield return match;
            }

            foreach (var descendant in Descendants<T>(child))
            {
                yield return descendant;
            }
        }
    }
}
