using System;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http;
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
    private static Mutex? _mutex;
    private bool _isExiting;
    private bool _ownsMutex;
    private const string MutexName = "EasyBluetoothAudio-SingleInstance-Mutex";
    private const string PipeName = "EasyBluetoothAudio-SingleInstance-Pipe";

    /// <summary>
    /// Gets the application-wide <see cref="IServiceProvider"/> instance.
    /// </summary>
    public static IServiceProvider? ServiceProvider { get; private set; }

    /// <inheritdoc />
    protected override void OnStartup(StartupEventArgs e)
    {
        _mutex = new Mutex(true, MutexName, out _ownsMutex);

        if (!_ownsMutex)
        {
            _ = SignalExistingInstanceAsync();
            Current.Shutdown();
            return;
        }

        _ = StartPipeServerAsync();

        try
        {
            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);

            ServiceProvider = serviceCollection.BuildServiceProvider();

            var themeService = ServiceProvider.GetRequiredService<IThemeService>();
            themeService.Initialize();

            var soundService = ServiceProvider.GetRequiredService<ISoundService>();
            soundService.Initialize();

            var mainWindow = ServiceProvider.GetRequiredService<MainWindow>();
            var mainViewModel = ServiceProvider.GetRequiredService<MainViewModel>();

            mainWindow.DataContext = mainViewModel;

            mainWindow.TrayIcon.ShowBalloonTip("Easy Bluetooth Audio", "App started in system tray.", Hardcodet.Wpf.TaskbarNotification.BalloonIcon.Info);

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

            mainViewModel.RequestExit += () =>
            {
                _isExiting = true;
                Current.Shutdown();
            };

            this.SessionEnding += (s, ev) => _isExiting = true;

            mainWindow.Deactivated += (s, ev) =>
            {
                if (mainViewModel.IsSettingsOpen)
                {
                    mainViewModel.SettingsViewModel.CloseCommand.Execute(null);
                }

                mainWindow.Hide();
            };
            mainWindow.Closing += (s, ev) =>
            {
                if (!_isExiting)
                {
                    ev.Cancel = true;
                    mainWindow.Hide();
                }
            };

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
            Current.Shutdown();
        }
    }

    /// <summary>
    /// Gracefully shuts down the application for an update, ensuring the "closing" logic is bypassed.
    /// </summary>
    public void ShutdownForUpdate()
    {
        _isExiting = true;
        Current.Shutdown();
    }

    /// <inheritdoc />
    protected override void OnExit(ExitEventArgs e)
    {
        if (_ownsMutex)
        {
            _mutex?.ReleaseMutex();
        }

        _mutex?.Dispose();
        _mutex = null;
        base.OnExit(e);
    }

    /// <summary>
    /// Signals the existing running instance that a second launch was attempted.
    /// </summary>
    /// <returns>A task representing the asynchronous pipe operation.</returns>
    private static async Task SignalExistingInstanceAsync()
    {
        try
        {
            using var client = new NamedPipeClientStream(".", PipeName, PipeDirection.Out);
            await client.ConnectAsync(500);
            using var writer = new StreamWriter(client);
            await writer.WriteAsync("NOTIFY");
            await writer.FlushAsync();
        }
        catch
        {
            // If pipe communication fails, just ignore and let the instance close.
        }
    }

    /// <summary>
    /// Starts a named-pipe server that listens for signals from secondary application instances.
    /// </summary>
    /// <returns>A task representing the background pipe-server loop.</returns>
    private async Task StartPipeServerAsync()
    {
        while (true)
        {
            try
            {
                using var server = new NamedPipeServerStream(PipeName, PipeDirection.In);
                await server.WaitForConnectionAsync();
                using var reader = new StreamReader(server);
                var message = await reader.ReadToEndAsync();

                if (message == "NOTIFY")
                {
                    await Current.Dispatcher.InvokeAsync(() =>
                    {
                        var mainWindow = ServiceProvider?.GetService<MainWindow>();
                        mainWindow?.TrayIcon?.ShowBalloonTip("Easy Bluetooth Audio", "App is already running in the background!", Hardcodet.Wpf.TaskbarNotification.BalloonIcon.Info);
                    });
                }
            }
            catch
            {
                await Task.Delay(1000);
            }
        }
    }

    /// <summary>
    /// Configures the dependency injection container with all services, ViewModels, and views.
    /// </summary>
    /// <param name="services">The service collection to populate.</param>
    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<IMessenger>(WeakReferenceMessenger.Default);
        services.AddSingleton<IDispatcherService, DispatcherService>();
        services.AddSingleton<IAudioService, AudioService>();
        services.AddSingleton<IProcessService, ProcessService>();
        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddSingleton<IStartupService, StartupService>();
        services.AddSingleton<IThemeService, ThemeService>();
        services.AddSingleton<ISoundService, SoundService>();
        services.AddSingleton<HttpClient>();
        services.AddSingleton<IUpdateService, UpdateService>();
        services.AddTransient<SettingsViewModel>();
        services.AddSingleton<UpdateViewModel>();
        services.AddSingleton<MainViewModel>();
        services.AddSingleton<MainWindow>();
    }
}
