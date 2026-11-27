using System.IO;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shell;
using CommunityToolkit.Mvvm.Messaging;
using EasyBluetoothAudio.Models;
using EasyBluetoothAudio.Services;
using EasyBluetoothAudio.Services.Interfaces;
using EasyBluetoothAudio.ViewModels;
using EasyBluetoothAudio.Views;
using Moq;
using Rectangle = System.Windows.Shapes.Rectangle;

namespace EasyBluetoothAudio.Tests;

internal static class RetroAppearanceRegression
{
    internal static void VerifyLiveSwitching(App app)
    {
        var directory = Directory.CreateTempSubdirectory("eba-retro-");
        try
        {
            ISettingsService settings = new SettingsService(directory.FullName);
            settings.Save(new AppSettings { AutoConnect = false, ThemeMode = AppThemeMode.Dark });
            var messenger = new WeakReferenceMessenger();
            IThemeService themeService = new ThemeService(messenger, settings);
            themeService.Initialize();
            using var model = VisualPreviewFactory.Create(settingsService: settings, messenger: messenger);
            var window = new MainWindow { DataContext = model };
            try
            {
                var shell = Assert.IsType<Border>(window.Content);
                var source = Assert.Single(Descendants<BluetoothConfigView>(shell));
                var picker = Assert.IsType<ComboBox>(source.FindName("SourceSelector"));
                var settingsView = Assert.IsType<SettingsView>(window.FindName("SettingsPage"));
                var themeChoice = Assert.IsType<ComboBox>(settingsView.FindName("ThemeChoice"));
                var originalBackground = Assert.IsType<SolidColorBrush>(window.Background).Color;
                var highContrast = SystemParameters.HighContrast;
                var size = new Size(window.Width, window.Height);

                foreach (var (mode, label, dictionary) in new[]
                {
                    (AppThemeMode.RetroDark, "Retro Dark", "RetroDarkTheme"),
                    (AppThemeMode.RetroLight, "Retro Light", "RetroLightTheme"),
                    (AppThemeMode.Dark, "Dark", "DarkTheme")
                })
                {
                    model.OpenSettingsCommand.ExecuteAsync(null).GetAwaiter().GetResult();
                    Layout(shell, size);
                    Assert.Equal(new[] { "Light", "Dark", "System", "Retro Dark", "Retro Light" },
                        themeChoice.Items.Cast<KeyValuePair<AppThemeMode, string>>().Select(choice => choice.Value));
                    var item = Assert.Single(themeChoice.Items.Cast<KeyValuePair<AppThemeMode, string>>(),
                        choice => choice.Key == mode);
                    Assert.Equal(label, item.Value);
                    themeChoice.SetCurrentValue(Selector.SelectedValueProperty, mode);
                    Layout(shell, size);
                    Assert.Equal(mode, themeChoice.SelectedValue);
                    Assert.Equal(mode, model.SettingsViewModel.ThemeMode);
                    Assert.Single(Descendants<TextBlock>(themeChoice), text => text.Text == label && VisualRenderingRegression.IsOffscreenVisible(text));
                    Assert.NotEqual(mode, settings.Load().ThemeMode);

                    model.CloseOrHideCommand.ExecuteAsync(null).GetAwaiter().GetResult();
                    Assert.False(model.IsSettingsOpen);
                    Assert.Equal(mode, settings.Load().ThemeMode);
                    Layout(shell, size);
                    var expectedDictionary = highContrast ? "HighContrastTheme" : dictionary;
                    Assert.EndsWith($"/{expectedDictionary}.xaml", app.Resources.MergedDictionaries[0].Source.OriginalString);
                    Assert.Same(picker, source.FindName("SourceSelector"));
                    Assert.Same(model.SelectedSource, picker.SelectedItem);
                    var retro = !highContrast && mode != AppThemeMode.Dark;
                    VerifyRenderedPicker(picker, model.SelectedSource!.Name, retro, mode == AppThemeMode.RetroLight);
                    VerifyComposition(source, model, retro);
                    Assert.Equal(new CornerRadius(retro ? 0 : 12), shell.CornerRadius);
                    Assert.Equal(shell.CornerRadius, WindowChrome.GetWindowChrome(window).CornerRadius);

                    if (retro)
                    {
                        Assert.Equal("Consolas", window.FontFamily.Source);
                        Assert.Contains(Descendants<TextBlock>(source), text =>
                            text.Text == "AUDIO SINK" && text.FontFamily.Source == "Consolas");
                        var background = Assert.IsType<SolidColorBrush>(window.Background);
                        Assert.Equal(mode == AppThemeMode.RetroDark ? Colors.Black : Color.FromRgb(245, 243, 226),
                            background.Color);
                        VerifyStripeBrush(background, Assert.IsAssignableFrom<TileBrush>(shell.Background));
                        var connect = Assert.Single(Descendants<Button>(source), button => Equals(button.Content, "CONNECT"));
                        Assert.Equal(mode == AppThemeMode.RetroDark ? Color.FromRgb(176, 176, 176) : Color.FromRgb(26, 26, 26),
                            Assert.IsType<SolidColorBrush>(connect.Background).Color);
                        Assert.Equal(mode == AppThemeMode.RetroDark ? Colors.Black : Colors.White,
                            Assert.IsType<SolidColorBrush>(connect.Foreground).Color);
                    }
                    else
                    {
                        Assert.Equal("Segoe UI", window.FontFamily.Source);
                        Assert.Equal(originalBackground, Assert.IsType<SolidColorBrush>(window.Background).Color);
                        Assert.Equal(Colors.Transparent, Assert.IsType<SolidColorBrush>(shell.Background).Color);
                    }
                }
            }
            finally
            {
                window.TrayIcon.Dispose();
                window.Close();
            }
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }

    internal static void VerifyComposition(BluetoothConfigView view, MainViewModel model, bool retro)
    {
        var frame = Assert.IsType<Border>(view.FindName("SourceFrame"));
        var label = Assert.IsType<TextBlock>(view.FindName("SourceLabel"));
        var footer = Assert.IsAssignableFrom<FrameworkElement>(view.FindName("RetroFooter"));
        var sourceViews = Descendants<DeviceSourceView>(view).Where(VisualRenderingRegression.IsOffscreenVisible).ToArray();
        if (!retro)
        {
            Assert.Equal(new Thickness(0), frame.BorderThickness);
            Assert.Equal(Visibility.Collapsed, label.Visibility);
            Assert.Equal(Visibility.Collapsed, footer.Visibility);
            if (model.SelectedSource != null)
            {
                var sourceView = Assert.Single(sourceViews);
                AssertAbove(FindStatus(sourceView), FindActions(sourceView), view);
            }

            return;
        }

        var picker = Assert.IsType<ComboBox>(view.FindName("SourceSelector"));
        var version = Assert.IsType<TextBlock>(view.FindName("SourceVersion"));
        var add = Assert.IsType<Button>(view.FindName("RetroAddDevice"));
        var rectangles = Descendants<Rectangle>(footer).ToArray();
        Assert.Equal(new[] { 20d, 10d, 5d }, rectangles.Select(rectangle => rectangle.Width));
        var accent = Assert.IsType<SolidColorBrush>(view.FindResource("RetroAccentBrush"));
        Assert.Equal(accent.Color, Assert.IsType<SolidColorBrush>(rectangles[0].Fill).Color);
        Assert.Equal(new Thickness(1), frame.BorderThickness);
        Assert.Equal(new CornerRadius(6), frame.CornerRadius);
        Assert.Equal("BLUETOOTH SOURCE", label.Text);
        Assert.Equal(model.Updater.AppVersion, version.Text);
        Assert.True(add.IsEnabled);
        Assert.Same(model.OpenBluetoothSettingsCommand, add.Command);
        var popupAdd = Assert.IsType<Button>(picker.Template.FindName("PopupAddDevice", picker));
        Assert.Same(model.OpenBluetoothSettingsCommand, popupAdd.Command);
        AssertAbove(label, picker, view);
        AssertAbove(frame, footer, view);
        Assert.True(Right(version, footer) <= Left(add, footer) + 0.5);
        Assert.True(Right(add, footer) <= Left(rectangles[0], footer) + 0.5);

        var device = Assert.Single(sourceViews);
        var actions = FindActions(device);
        var status = FindStatus(device);
        Assert.Same(model.SelectedSource, device.DataContext);
        AssertAbove(picker, actions, view);
        AssertAbove(actions, status, view);
        Assert.True(Bottom(status, view) <= Bottom(frame, view) + 0.5, "The status strip must fit inside the source frame.");
        Assert.True(Bottom(footer, view) <= view.ActualHeight + 0.5, "The version and Add device footer must fit inside the flyout.");
        VerifyRetroActions(device, model.SelectedSource);
    }

    internal static void VerifyRetroActionStates()
    {
        var pending = new TaskCompletionSource<AudioConnectionResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var audio = new Mock<IAudioService>();
        audio.Setup(service => service.ConnectBluetoothAudioAsync("retro-preview", It.IsAny<CancellationToken>()))
            .Returns(pending.Task);
        var dispatcher = new Mock<IDispatcherService>();
        dispatcher.Setup(service => service.Invoke(It.IsAny<Action>())).Callback<Action>(action => action());
        var device = new BluetoothDevice { Id = "retro-preview", Name = "Retro preview phone", IsConnected = true };
        using var model = new DeviceConnectionViewModel(device, audio.Object, dispatcher.Object, new WeakReferenceMessenger());
        var view = new DeviceSourceView { DataContext = model };
        var size = new Size(280, 100);

        Layout(view, size);
        Assert.True(model.ConnectCommand.CanExecute(null));
        Assert.False(model.DisconnectCommand.CanExecute(null));
        VerifyRetroActions(view, model);

        var connection = model.ConnectAsync();
        try
        {
            Assert.True(model.IsBusy);
            Layout(view, size);
            Assert.False(model.ConnectCommand.CanExecute(null));
            Assert.True(model.DisconnectCommand.CanExecute(null));
            VerifyRetroActions(view, model);
        }
        finally
        {
            model.Disconnect();
            pending.SetResult(AudioConnectionResult.Connected);
            connection.GetAwaiter().GetResult();
        }

        audio.Setup(service => service.ConnectBluetoothAudioAsync("retro-preview", It.IsAny<CancellationToken>()))
            .ReturnsAsync(AudioConnectionResult.Connected);
        model.ConnectAsync().GetAwaiter().GetResult();
        Assert.True(model.IsConnected);
        Layout(view, size);
        Assert.True(model.ReconnectCommand.CanExecute(null));
        Assert.True(model.DisconnectCommand.CanExecute(null));
        VerifyRetroActions(view, model);
    }

    private static void VerifyRetroActions(DeviceSourceView view, DeviceConnectionViewModel? source)
    {
        var actions = FindActions(view);
        var buttons = Descendants<Button>(actions).Where(VisualRenderingRegression.IsOffscreenVisible)
            .OrderBy(button => Left(button, actions)).ToArray();
        if (source == null)
        {
            Assert.Empty(buttons);
            return;
        }

        if (source.ShowReconnectActions)
        {
            Assert.Equal(2, buttons.Length);
            Assert.Equal("RECONNECT", buttons[0].Content);
            Assert.Equal("DISCONNECT", buttons[1].Content);
            Assert.Same(source.ReconnectCommand, buttons[0].Command);
            Assert.Same(source.DisconnectCommand, buttons[1].Command);
            Assert.InRange(Math.Abs(buttons[0].ActualWidth - buttons[1].ActualWidth), 0, 1);
            Assert.True(Right(buttons[0], actions) < Left(buttons[1], actions));
        }
        else
        {
            var button = Assert.Single(buttons);
            var caption = source.IsBusy ? "CANCEL" : source.ShowDisconnectAction ? "DISCONNECT" : "CONNECT";
            Assert.Equal(caption, button.Content);
            Assert.Same(source.ShowDisconnectAction ? source.DisconnectCommand : source.ConnectCommand, button.Command);
            Assert.InRange(Math.Abs(actions.ActualWidth - button.ActualWidth), 0, 1);
        }

        foreach (var button in buttons)
        {
            Assert.Equal(button.Command?.CanExecute(button.CommandParameter) == true, button.IsEnabled);
            Assert.NotNull(button.FocusVisualStyle);
            if (button.Command != null)
            {
                Assert.NotEmpty(AutomationProperties.GetName(button));
            }
        }
    }

    private static Border FindStatus(DeviceSourceView view) => Assert.IsType<Border>(view.FindName("SourceStatusStrip"));

    private static Grid FindActions(DeviceSourceView view) => Assert.IsType<Grid>(view.FindName("SourceActions"));

    private static void AssertAbove(FrameworkElement first, FrameworkElement second, FrameworkElement ancestor)
    {
        Assert.True(Bottom(first, ancestor) <= second.TranslatePoint(new Point(), ancestor).Y + 0.5,
            $"{first.Name} must end above {second.Name}.");
    }

    private static double Left(FrameworkElement element, FrameworkElement ancestor) =>
        element.TranslatePoint(new Point(), ancestor).X;

    private static double Right(FrameworkElement element, FrameworkElement ancestor) =>
        Left(element, ancestor) + element.ActualWidth;

    private static double Bottom(FrameworkElement element, FrameworkElement ancestor) =>
        element.TranslatePoint(new Point(0, element.ActualHeight), ancestor).Y;

    private static void VerifyRenderedPicker(ComboBox picker, string sourceName, bool retro, bool light)
    {
        var label = Assert.Single(Descendants<TextBlock>(picker), text => text.Text == sourceName);
        var toggle = Assert.Single(Descendants<ToggleButton>(picker));
        var border = Assert.IsType<Border>(toggle.Template.FindName("SelectorBorder", toggle));
        Assert.IsType<Popup>(picker.Template.FindName("PART_Popup", picker));
        Assert.NotNull(picker.FocusVisualStyle);
        Assert.Equal(new CornerRadius(retro ? 0 : 6), border.CornerRadius);
        Assert.Equal(TextTrimming.CharacterEllipsis, label.TextTrimming);
        if (retro)
        {
            var background = light ? Colors.White : Color.FromRgb(204, 207, 0);
            var accent = light ? Color.FromRgb(184, 160, 0) : background;
            Assert.Equal(background, Assert.IsType<SolidColorBrush>(border.Background).Color);
            Assert.Equal(accent, Assert.IsType<SolidColorBrush>(border.BorderBrush).Color);
            Assert.Equal(new Thickness(2), border.BorderThickness);
            VerifyRetroPickerLabel(label, light);

            var row = new ComboBoxItem
            {
                Content = picker.SelectedItem,
                ContentTemplate = picker.ItemTemplate,
                Style = picker.ItemContainerStyle
            };
            Layout(row, new Size(320, 44));
            Assert.Equal(background, Assert.IsType<SolidColorBrush>(row.Background).Color);
            VerifyRetroPickerLabel(Assert.Single(Descendants<TextBlock>(row), text => text.Text == sourceName), light);
        }
    }

    private static void VerifyRetroPickerLabel(TextBlock label, bool light)
    {
        Assert.Equal(light ? Color.FromRgb(26, 26, 26) : Colors.Black, Assert.IsType<SolidColorBrush>(label.Foreground).Color);
        Assert.Equal(20, label.FontSize);
        Assert.Equal("Segoe UI", label.FontFamily.Source);
    }

    private static void VerifyStripeBrush(Brush background, TileBrush brush)
    {
        Assert.Equal(TileMode.Tile, brush.TileMode);
        Assert.Equal(BrushMappingMode.Absolute, brush.ViewportUnits);
        Assert.Equal(new Size(10, 10), brush.Viewport.Size);
        var surface = new Border { Background = background, Child = new Border { Background = brush } };
        Layout(surface, new Size(20, 20));
        var bitmap = new RenderTargetBitmap(20, 20, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(surface);
        var pixels = new byte[20 * 20 * 4];
        bitmap.CopyPixels(pixels, 20 * 4, 0);
        var samples = pixels.Chunk(4).ToArray();
        Assert.All(samples, pixel => Assert.Equal(byte.MaxValue, pixel[3]));
        Assert.True(samples.Select(pixel => pixel[0]).Distinct().Count() > 1);
    }

    private static void Layout(FrameworkElement element, Size size)
    {
        element.Measure(size);
        element.Arrange(new Rect(size));
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
