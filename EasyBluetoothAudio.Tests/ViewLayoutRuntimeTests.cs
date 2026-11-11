using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;
using EasyBluetoothAudio.ViewModels;
using EasyBluetoothAudio.Views;

namespace EasyBluetoothAudio.Tests;

/// <summary>
/// Exercises compiled source and settings layouts with realistic data at narrow widths and in every theme.
/// </summary>
[Collection("Window layout runtime")]
public class ViewLayoutRuntimeTests
{
    /// <summary>
    /// Verifies compact source selection, readable action labels, wrapping errors, settings, and focus styles in every theme.
    /// </summary>
    [Fact]
    public void Views_AllThemes_NarrowLayoutsReflowWithoutHorizontalScrolling()
    {
        StaTest.Run(() =>
        {
            var app = new App();
            app.InitializeComponent();
            try
            {
                VisualRenderingRegression.Verify(app);
                using var model = VisualPreviewFactory.Create();
                using var emptyModel = VisualPreviewFactory.Create(includeSources: false);
                foreach (var theme in new[] { "LightTheme", "DarkTheme", "HighContrastTheme" })
                {
                    app.Resources.MergedDictionaries[0] = new ResourceDictionary
                    {
                        Source = new Uri($"/EasyBluetoothAudio;component/Resources/{theme}.xaml", UriKind.Relative)
                    };
                    FooterAndTrayMenuRegression.Verify(app, theme);
                    foreach (var size in new[] { new Size(280, 180), new Size(338, 238), new Size(550, 490), new Size(850, 610) })
                    {
                        model.SelectedSource = model.Sources[0];
                        var sourceView = new BluetoothConfigView { DataContext = model };
                        Layout(sourceView, size);
                        Assert.Null(sourceView.FindName("SourceScrollViewer"));
                        VerifyScrollableView(new SettingsView { DataContext = model }, size,
                            expectVerticalScroll: size.Height < 610 ? true : null);
                    }

                    VerifySelectedSources(model);
                    VerifyEmptySelector(emptyModel);
                    VerifyExpandedSettingsAndScrollFallback(model);
                    VerifySourceRow(model.Sources[0], 280);
                    VerifySourceRow(model.Sources[1], 550);
                }

                var font = Assert.IsType<FontFamily>(app.FindResource("InterfaceFont"));
                Assert.True(new Typeface(font, FontStyles.Normal, FontWeights.Normal, FontStretches.Normal).TryGetGlyphTypeface(out var glyphs));
                Assert.Contains("Segoe UI", glyphs.FamilyNames.Values);
                Assert.IsType<Style>(app.FindResource("KeyboardFocusStyle"));
            }
            finally
            {
                app.Shutdown();
            }
        });
    }

    private static void VerifySelectedSources(MainViewModel model)
    {
        var view = new BluetoothConfigView { DataContext = model };
        foreach (var source in model.Sources)
        {
            model.SelectedSource = source;
            Layout(view, new Size(338, 238));
            var selector = Assert.IsType<ComboBox>(view.FindName("SourceSelector"));
            Assert.Same(model.Sources, selector.ItemsSource);
            Assert.Same(source, selector.SelectedItem);
            Assert.NotNull(selector.FocusVisualStyle);
            var sourceView = Assert.Single(Descendants<DeviceSourceView>(view));
            Assert.Same(source, sourceView.DataContext);
            var status = Assert.Single(Descendants<TextBlock>(sourceView), text => text.Text == source.StatusText);
            var actions = Assert.IsType<Grid>(sourceView.FindName("SourceActions"));
            Assert.True(status.TranslatePoint(new Point(), view).Y < actions.TranslatePoint(new Point(), view).Y);
            var add = Assert.Single(Descendants<Button>(view), button => Equals(button.Content, "Add device"));
            Assert.Same(model.OpenBluetoothSettingsCommand, add.Command);
            Assert.True(add.TranslatePoint(new Point(), view).Y >= actions.TranslatePoint(new Point(), view).Y + actions.ActualHeight);
            var label = Assert.Single(Descendants<TextBlock>(selector), text => text.Text == source.Name);
            Assert.Equal(TextTrimming.CharacterEllipsis, label.TextTrimming);
            Assert.Equal(TextWrapping.NoWrap, label.TextWrapping);
            Assert.True(label.ActualWidth <= selector.ActualWidth);

            if (!string.IsNullOrEmpty(source.ErrorText))
            {
                Assert.DoesNotContain(Descendants<TextBlock>(sourceView), text => text.Text == source.ErrorText);
                var error = Assert.IsType<TextBlock>(view.FindName("SourceFooter"));
                Assert.Equal(source.ErrorText, error.Text);
                Assert.Equal(TextWrapping.NoWrap, error.TextWrapping);
                Assert.Equal(error.Text, error.ToolTip);
                Assert.True(add.TranslatePoint(new Point(), view).Y + add.ActualHeight <= error.TranslatePoint(new Point(), view).Y);
                Assert.True(error.TranslatePoint(new Point(), view).Y + error.ActualHeight <= view.ActualHeight + 0.5);
            }
        }
    }

    private static void VerifyEmptySelector(MainViewModel model)
    {
        var view = new BluetoothConfigView { DataContext = model };
        Layout(view, new Size(338, 238));
        var selector = Assert.IsType<ComboBox>(view.FindName("SourceSelector"));
        Assert.Empty(selector.Items);
        Assert.Contains(Descendants<TextBlock>(selector), text => text.Text == "No devices found" && text.Visibility == Visibility.Visible);
        Assert.DoesNotContain(Descendants<DeviceSourceView>(view), IsVisibleInTree);
        Assert.Contains(Descendants<Button>(view), button => Equals(button.Content, "Add device") && button.IsEnabled);
    }

    private static void VerifyExpandedSettingsAndScrollFallback(MainViewModel model)
    {
        var view = new SettingsView { DataContext = model };
        Layout(view, new Size(338, 238));
        var scroll = Assert.IsType<ScrollViewer>(view.FindName("SettingsScrollViewer"));
        var compactViewport = scroll.ViewportHeight;
        var compactOverflow = scroll.ScrollableHeight;
        var compactOptions = VisibleSettingOptions();
        Assert.True(compactOverflow > 0);

        Layout(view, new Size(338, 760));
        Assert.True(scroll.ViewportHeight > compactViewport);
        Assert.True(scroll.ScrollableHeight < compactOverflow);
        Assert.True(VisibleSettingOptions() > compactOptions);

        Layout(view, new Size(338, 238));
        scroll.ScrollToBottom();
        view.UpdateLayout();
        Assert.True(scroll.VerticalOffset > 0);
        Assert.Equal(scroll.ScrollableHeight, scroll.VerticalOffset);
        var close = Assert.Single(Descendants<Button>(view), button => Equals(button.Content, "Close settings"));
        Assert.True(close.TranslatePoint(new Point(), view).Y + close.ActualHeight <= view.ActualHeight);

        Layout(view, new Size(338, 2000));
        Assert.Equal(0, scroll.ScrollableHeight);

        int VisibleSettingOptions()
        {
            return Descendants<CheckBox>(view).Count(option =>
            {
                var top = option.TranslatePoint(new Point(), scroll).Y;
                return top >= 0 && top + option.ActualHeight <= scroll.ViewportHeight;
            });
        }
    }

    private static void VerifyScrollableView(UserControl view, Size size, bool? expectVerticalScroll)
    {
        Layout(view, size);
        var scroll = Assert.Single(Descendants<ScrollViewer>(view), item => item.Style == view.FindResource("CustomScrollViewerStyle"));
        Assert.Equal(0, scroll.ScrollableWidth);
        Assert.Equal(ScrollBarVisibility.Disabled, scroll.HorizontalScrollBarVisibility);
        if (expectVerticalScroll == true)
        {
            Assert.True(scroll.ScrollableHeight > 0);
            scroll.ScrollToBottom();
            view.UpdateLayout();
            Assert.True(scroll.VerticalOffset > 0);
        }
        else if (expectVerticalScroll == false)
        {
            Assert.Equal(0, scroll.ScrollableHeight);
        }

        Assert.All(Descendants<Button>(view).Where(button => button.Visibility == Visibility.Visible), button =>
        {
            Assert.True(button.ActualWidth >= 44);
            Assert.True(button.ActualHeight >= 44);
            Assert.NotNull(button.FocusVisualStyle);
        });
    }

    private static void VerifySourceRow(DeviceConnectionViewModel model, double width)
    {
        var row = new DeviceSourceView { DataContext = model };
        Layout(row, new Size(width, 220));
        var actions = Assert.IsType<Grid>(row.FindName("SourceActions"));
        Assert.Equal(width, actions.ActualWidth);
        var visibleButtons = Descendants<Button>(row).Where(button => button.Visibility == Visibility.Visible).ToArray();
        Assert.Equal(model.IsConnected ? 2 : 1, visibleButtons.Length);
        Assert.All(visibleButtons, button =>
        {
            Assert.NotEmpty(AutomationProperties.GetName(button));
            Assert.True(button.IsTabStop);
            Assert.True(button.IsHitTestVisible);
            Assert.NotNull(button.Command);
            Assert.Equal(model.IsConnected ? (width - 8) / 2 : width, button.ActualWidth);
        });
        Assert.All(Descendants<TextBlock>(row).Where(text => text.Visibility == Visibility.Visible), text =>
        {
            Assert.True(text.ActualWidth <= width);
        });
    }

    private static void Layout(FrameworkElement view, Size size)
    {
        view.Measure(size);
        view.Arrange(new Rect(new Point(), size));
        view.UpdateLayout();
        view.UpdateLayout();
    }

    private static bool IsVisibleInTree(FrameworkElement element)
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
