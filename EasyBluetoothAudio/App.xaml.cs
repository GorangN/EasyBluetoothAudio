using System;
using System.Linq;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http;
using EasyBluetoothAudio.Core;
using EasyBluetoothAudio.Services;
using EasyBluetoothAudio.ViewModels;
using EasyBluetoothAudio.Views;

namespace EasyBluetoothAudio;

/// <summary>
/// Application entry point responsible for dependency injection configuration and startup.
/// </summary>
public partial class App : System.Windows.Application
{
    private DispatcherTimer? _refreshTimer;

    /// <summary>
    /// Gets the application-wide <see cref="IServiceProvider"/> instance.
    /// </summary>
    public static IServiceProvider? ServiceProvider { get; private set; }

    /// <inheritdoc />
    protected override void OnStartup(StartupEventArgs e)
    {
        try
        {
            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);

            ServiceProvider = serviceCollection.BuildServiceProvider();

            var mainWindow = ServiceProvider.GetRequiredService<MainWindow>();
            var mainViewModel = ServiceProvider.GetRequiredService<MainViewModel>();

            // Wire up the DataContext
            mainWindow.DataContext = mainViewModel;

            // Wire up tray icon startup notification
            mainWindow.TrayIcon.ShowBalloonTip("Easy Bluetooth Audio", "App started in system tray.", Hardcodet.Wpf.TaskbarNotification.BalloonIcon.Info);

            // Wire up ViewModel lifecycle requests
            mainViewModel.RequestShow += () =>
            {
                var mousePt = System.Windows.Forms.Cursor.Position;
                var screen = System.Windows.Forms.Screen.FromPoint(mousePt);
                var workArea = screen.WorkingArea;

                mainWindow.Left = workArea.Right - mainWindow.Width - 10;
                mainWindow.Top = workArea.Bottom - mainWindow.Height - 10;

                mainWindow.Show();
                mainWindow.WindowState = WindowState.Normal;
                mainWindow.Activate();
            };

            mainViewModel.RequestExit += () => System.Windows.Application.Current.Shutdown();

            // Wire up Window UI behaviors
            mainWindow.Deactivated += (s, ev) => mainWindow.Hide();
            mainWindow.Closing += (s, ev) =>
            {
                ev.Cancel = true;
                mainWindow.Hide();
            };

            // Wire up the UI periodic refresh timer
            _refreshTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
            _refreshTimer.Tick += async (s, ev) => await mainViewModel.RefreshDevicesAsync();

            mainWindow.IsVisibleChanged += (s, ev) =>
            {
                if (mainWindow.IsVisible)
                {
                    _refreshTimer.Start();
                    _ = mainViewModel.RefreshDevicesAsync();
                }
                else
                {
                    _refreshTimer.Stop();
                }
            };

            _ = mainViewModel.InitializeAsync();

            bool isSilent = e.Args.Any(arg => arg.Equals("--silent", StringComparison.OrdinalIgnoreCase));
            if (!isSilent)
            {
                mainWindow.Show();
            }

            base.OnStartup(e);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Startup Error: {ex.Message}\n\nStack: {ex.StackTrace}", "Easy Bluetooth Audio Error", MessageBoxButton.OK, MessageBoxImage.Error);
            System.Windows.Application.Current.Shutdown();
        }
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<IDispatcherService, DispatcherService>();
        services.AddSingleton<IAudioService, AudioService>();
        services.AddSingleton<IProcessService, ProcessService>();
        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddSingleton<IStartupService, StartupService>();
        services.AddSingleton<HttpClient>();
        services.AddSingleton<IUpdateService, UpdateService>();
        services.AddTransient<SettingsViewModel>();
        services.AddSingleton<UpdateViewModel>();
        services.AddSingleton<MainViewModel>();
        services.AddSingleton<MainWindow>();
    }
}
