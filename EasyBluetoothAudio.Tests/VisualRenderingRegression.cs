using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shell;
using EasyBluetoothAudio.ViewModels;
using EasyBluetoothAudio.Views;
using Path = System.Windows.Shapes.Path;

namespace EasyBluetoothAudio.Tests;

internal static class VisualRenderingRegression
{
    internal static void Verify(App app)
    {
        foreach (var theme in new[] { "DarkTheme", "LightTheme", "HighContrastTheme" })
        {
            app.Resources.MergedDictionaries[0] = new ResourceDictionary
            {
                Source = new Uri($"/EasyBluetoothAudio;component/Resources/{theme}.xaml", UriKind.Relative)
            };
            var button = new Button { Content = "Add device", Style = (Style)app.FindResource("PrimaryButtonStyle") };
            Layout(button, new Size(160, 44));
            VerifyActionLabel(button);
            var customFontButton = new Button
            {
                Content = "Typeface",
                FontFamily = new FontFamily("Consolas"),
                Style = (Style)app.FindResource("SecondaryButtonStyle")
            };
            Layout(customFontButton, new Size(160, 44));
            VerifyActionLabel(customFontButton);

            using var model = VisualPreviewFactory.Create();
            model.SettingsViewModel.ThemeMode = theme == "LightTheme" ? EasyBluetoothAudio.Models.AppThemeMode.Light : EasyBluetoothAudio.Models.AppThemeMode.Dark;
            var shell = CreateShell(model);
            foreach (var size in new[] { new Size(386, 278), new Size(320, 236), new Size(600, 580) })
            {
                Layout(shell, size);
                VerifyVisibleLabels(shell);
                VerifyVectorHeaderIcons(shell);
                VerifyCompactToolbar(shell, model);
                Export(shell, $"sources-{theme}-{size.Width:0}.png");
            }

            model.Updater.UpdateAvailable = true;
            Layout(shell, new Size(320, 236));
            VerifyVectorHeaderIcons(shell);
            VerifyCompactToolbar(shell, model);
            Export(shell, $"sources-{theme}-320-update.png");
            model.Updater.UpdateAvailable = false;

            foreach (var source in model.Sources)
            {
                model.SelectedSource = source;
                Layout(shell, new Size(386, 278));
                VerifyVisibleLabels(shell);
                var sourceView = Assert.Single(Descendants<DeviceSourceView>(shell));
                Assert.Same(source, sourceView.DataContext);
                Export(shell, $"source-{theme}-{source.Id}.png");
            }
            model.SelectedSource = model.Sources[0];

            var narrowView = new BluetoothConfigView { DataContext = model };
            var narrowHost = new Border { Background = (Brush)app.FindResource("WindowBackgroundBrush"), Child = narrowView };
            Layout(narrowHost, new Size(272, 198));
            VerifyVisibleLabels(narrowView);
            Export(narrowHost, $"sources-{theme}-view-272.png");

            model.OpenSettingsCommand.Execute(null);
            Assert.True(model.IsSettingsOpen);
            Layout(shell, new Size(386, 278));
            VerifyVisibleLabels(shell);
            Export(shell, $"settings-{theme}-386.png");
            Layout(shell, new Size(386, 800));
            VerifyVisibleLabels(shell);
            Export(shell, $"settings-{theme}-386-expanded.png");
            var close = Assert.Single(Descendants<Button>(shell), button =>
                ReferenceEquals(button.Command, model.CloseOrHideCommand));
            Assert.Equal("Close settings", AutomationProperties.GetName(close));
            Assert.DoesNotContain(Descendants<Button>(shell), button => Equals(button.Content, "Back"));
            close.Command.Execute(null);
            Assert.False(model.IsSettingsOpen);
            Assert.Equal("Hide to system tray", model.CloseActionLabel);
            model.OpenSettingsCommand.Execute(null);
            model.OpenSettingsCommand.Execute(null);
            Assert.False(model.IsSettingsOpen);
        }
    }

    private static FrameworkElement CreateShell(object model)
    {
        var window = new MainWindow { DataContext = model };
        try
        {
            var content = Assert.IsAssignableFrom<FrameworkElement>(window.Content);
            window.Content = null;
            var shell = new Border
            {
                DataContext = model,
                Background = window.Background,
                Child = content,
                UseLayoutRounding = window.UseLayoutRounding,
                SnapsToDevicePixels = window.SnapsToDevicePixels
            };
            TextElement.SetForeground(shell, window.Foreground);
            TextElement.SetFontFamily(shell, window.FontFamily);
            TextOptions.SetTextFormattingMode(shell, TextOptions.GetTextFormattingMode(window));
            return shell;
        }
        finally
        {
            window.TrayIcon.Dispose();
            window.Close();
        }
    }

    private static void VerifyVisibleLabels(DependencyObject root)
    {
        var buttons = Descendants<Button>(root).Where(IsOffscreenVisible).Where(button => button.Content is string).ToArray();
        Assert.NotEmpty(buttons);
        Assert.All(buttons, VerifyActionLabel);
    }

    private static void VerifyActionLabel(Button button)
    {
        var content = Assert.IsType<string>(button.Content);
        var label = Assert.Single(Descendants<TextBlock>(button), text => text.Text == content);
        Assert.Equal(BrushColor(button.Foreground), BrushColor(label.Foreground));
        Assert.Equal(button.FontFamily.Source, label.FontFamily.Source);
        Assert.Equal(button.FontWeight, label.FontWeight);
        Assert.Equal(TextWrapping.NoWrap, label.TextWrapping);
        var text = new FormattedText(content, CultureInfo.InvariantCulture, label.FlowDirection,
            new Typeface(label.FontFamily, label.FontStyle, label.FontWeight, label.FontStretch),
            label.FontSize, label.Foreground, null, TextOptions.GetTextFormattingMode(label),
            VisualTreeHelper.GetDpi(label).PixelsPerDip);
        var presenter = FindAncestor<ContentPresenter>(label);
        Assert.NotNull(presenter);
        Assert.True(text.WidthIncludingTrailingWhitespace <= presenter.ActualWidth + 0.5,
            $"'{content}' needs {text.WidthIncludingTrailingWhitespace:0.0} DIPs but has {presenter.ActualWidth:0.0}.");
        Assert.True(label.ActualHeight <= text.Height + 0.5,
            $"'{content}' must remain a single readable line.");
    }

    private static void VerifyVectorHeaderIcons(DependencyObject shell)
    {
        var names = new List<string> { "Refresh audio sources", "Settings", "Hide to system tray" };
        if (Descendants<Button>(shell).Any(button => AutomationProperties.GetName(button) == "Install available update" && IsOffscreenVisible(button)))
        {
            names.Add("Install available update");
        }

        foreach (var name in names)
        {
            var button = Assert.Single(Descendants<Button>(shell), candidate => AutomationProperties.GetName(candidate) == name);
            var icon = Assert.IsAssignableFrom<FrameworkElement>(button.Content);
            var presenter = FindAncestor<ContentPresenter>(icon);
            Assert.NotNull(presenter);
            Assert.True(icon.ActualWidth <= presenter.ActualWidth + 0.5,
                $"'{name}' icon needs {icon.ActualWidth:0.0} DIPs but has {presenter.ActualWidth:0.0}.");
            Assert.True(icon.ActualHeight <= presenter.ActualHeight + 0.5,
                $"'{name}' icon needs {icon.ActualHeight:0.0} DIPs but has {presenter.ActualHeight:0.0}.");
            var paths = Descendants<Path>(button).ToArray();
            Assert.NotEmpty(paths);
            Assert.All(paths, path =>
            {
                Assert.NotNull(path.Data);
                Assert.False(path.Data.Bounds.IsEmpty);
                Assert.True(path.StrokeThickness > 0);
            });
        }

        Assert.DoesNotContain(Descendants<TextBlock>(shell).Where(IsOffscreenVisible),
            label => label.Text.Any(character => char.GetUnicodeCategory(character) == UnicodeCategory.PrivateUse));
    }

    private static void VerifyCompactToolbar(FrameworkElement shell, MainViewModel model)
    {
        var settings = Assert.Single(Descendants<Button>(shell), button => ReferenceEquals(button.Command, model.OpenSettingsCommand));
        var close = Assert.Single(Descendants<Button>(shell), button => ReferenceEquals(button.Command, model.CloseOrHideCommand));
        var toolbar = Assert.IsType<StackPanel>(VisualTreeHelper.GetParent(settings));
        Assert.Same(toolbar, VisualTreeHelper.GetParent(close));
        Assert.Same(close, toolbar.Children[toolbar.Children.IndexOf(settings) + 1]);
        Assert.True(close.TranslatePoint(new Point(), shell).X >= settings.TranslatePoint(new Point(), shell).X + settings.ActualWidth);
        Assert.True(WindowChrome.GetIsHitTestVisibleInChrome(close));
        Assert.NotNull(close.FocusVisualStyle);
        var heading = Assert.Single(Descendants<TextBlock>(shell), text => text.Text == "Audio sink" && IsOffscreenVisible(text));
        var measured = new FormattedText(heading.Text, CultureInfo.InvariantCulture, heading.FlowDirection,
            new Typeface(heading.FontFamily, heading.FontStyle, heading.FontWeight, heading.FontStretch),
            heading.FontSize, heading.Foreground, null, TextOptions.GetTextFormattingMode(heading),
            VisualTreeHelper.GetDpi(heading).PixelsPerDip);
        Assert.True(heading.TranslatePoint(new Point(), shell).X + measured.WidthIncludingTrailingWhitespace + 8
            <= toolbar.TranslatePoint(new Point(), shell).X, "The heading must retain a gap before the toolbar.");
        Assert.True(toolbar.TranslatePoint(new Point(), shell).X + toolbar.ActualWidth <= shell.ActualWidth);
    }

    private static bool IsOffscreenVisible(FrameworkElement element)
    {
        for (DependencyObject? current = element; current != null; current = VisualTreeHelper.GetParent(current))
        {
            if (current is UIElement visual && visual.Visibility != Visibility.Visible)
            {
                return false;
            }
        }

        return true;
    }

    private static T? FindAncestor<T>(DependencyObject element) where T : DependencyObject
    {
        for (var parent = VisualTreeHelper.GetParent(element); parent != null; parent = VisualTreeHelper.GetParent(parent))
        {
            if (parent is T match)
            {
                return match;
            }
        }

        return null;
    }

    private static void Export(FrameworkElement element, string fileName)
    {
        var directory = Environment.GetEnvironmentVariable("EBA_RENDER_OUTPUT_DIRECTORY");
        if (string.IsNullOrWhiteSpace(directory))
        {
            return;
        }

        Directory.CreateDirectory(directory);
        var bitmap = new RenderTargetBitmap((int)Math.Ceiling(element.ActualWidth),
            (int)Math.Ceiling(element.ActualHeight), 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(element);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var output = File.Create(System.IO.Path.Combine(directory, fileName));
        encoder.Save(output);
    }

    private static Color BrushColor(Brush brush)
    {
        return Assert.IsType<SolidColorBrush>(brush).Color;
    }

    private static void Layout(FrameworkElement element, Size size)
    {
        element.Measure(size);
        element.Arrange(new Rect(new Point(), size));
        element.UpdateLayout();
        element.UpdateLayout();
    }

    private static IEnumerable<T> Descendants<T>(DependencyObject parent) where T : DependencyObject
    {
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(parent); index++)
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
