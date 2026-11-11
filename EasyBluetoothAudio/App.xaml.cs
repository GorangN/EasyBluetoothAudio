using System;
using System.IO;
using System.Diagnostics;
using System.IO.Pipes;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using System.Net.Http;
using EasyBluetoothAudio.Messages;
using EasyBluetoothAudio.Services;
using EasyBluetoothAudio.Services.Interfaces;
using EasyBluetoothAudio.ViewModels;
using EasyBluetoothAudio.Views;

namespace EasyBluetoothAudio;

/// <summary>
/// Application entry point responsible for dependency injection configuration and startup.
/// </summary>
public partial class App : System.Windows.Application
{
    private DispatcherTimer? _refreshTimer;
    private MainViewModel? _mainViewModel;
    private IConnectionDiagnostics? _connectionDiagnostics;
    private static Mutex? _mutex;
    private bool _isExiting;
    private bool _ownsMutex;
    private const string MutexName = "EasyBluetoothAudio-SingleInstance-Mutex";
    private const string PipeName = "EasyBluetoothAudio-SingleInstance-Pipe";

    /// <summary>
    /// Gets the application-wide <see cref="IServiceProvider"/> instance.
    /// </summary>
    public static IServiceProvider? ServiceProvider { get; private set; }

    /// <summary>
    /// Argument passed to a second instance launched with UAC elevation to apply the low-end hardware registry setting.
    /// </summary>
    internal const string ArgApplyLowEndMode = "--apply-low-end-mode";

    /// <summary>
    /// Argument passed to a second instance launched with UAC elevation to restore the default Bluetooth quality registry setting.
    /// </summary>
    internal const string ArgRestoreLowEndMode = "--restore-low-end-mode";


    /// <inheritdoc />
    protected override void OnStartup(StartupEventArgs e)
    {
        // Handle elevated helper arguments before any normal startup logic.
        // When launched with these args the process acts as a minimal registry writer and exits immediately,
        // bypassing the single-instance mutex and all UI initialisation.
        if (e.Args.Contains(ArgApplyLowEndMode))
        {
            RunElevatedApply();
            return;
        }

        if (e.Args.Contains(ArgRestoreLowEndMode))
        {
            RunElevatedRestore();
            return;
        }

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
            _connectionDiagnostics = ServiceProvider.GetRequiredService<IConnectionDiagnostics>();
            _connectionDiagnostics.Initialize();

            var themeService = ServiceProvider.GetRequiredService<IThemeService>();
            themeService.Initialize();

            var soundService = ServiceProvider.GetRequiredService<ISoundService>();
            soundService.Initialize();

            var mainWindow = ServiceProvider.GetRequiredService<MainWindow>();
            var mainViewModel = ServiceProvider.GetRequiredService<MainViewModel>();
            _mainViewModel = mainViewModel;
            var windowPlacementService = ServiceProvider.GetRequiredService<IWindowPlacementService>();

            mainWindow.DataContext = mainViewModel;
            mainViewModel.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(MainViewModel.IsSettingsOpen))
                {
                    windowPlacementService.SetSettingsExpanded(mainWindow, mainViewModel.IsSettingsOpen);
                }
            };
            mainWindow.OpenTrayMenuItem.Click += (_, _) =>
            {
                if (mainViewModel.OpenCommand.CanExecute(null))
                {
                    mainViewModel.OpenCommand.Execute(null);
                }
            };
            mainWindow.ReconnectTrayMenuItem.Visibility = Visibility.Collapsed;
            mainWindow.DisconnectAllTrayMenuItem.Click += (_, _) => mainViewModel.DisconnectAllCommand.Execute(null);
            mainWindow.TrayIcon.ContextMenu.Opened += (_, _) =>
            {
                mainWindow.DevicesTrayMenuItem.Items.Clear();
                foreach (var source in mainViewModel.Sources)
                {
                    var deviceMenu = new System.Windows.Controls.MenuItem { Header = source.Name };
                    deviceMenu.Items.Add(new System.Windows.Controls.MenuItem { Header = "Connect", Command = source.ConnectCommand });
                    deviceMenu.Items.Add(new System.Windows.Controls.MenuItem { Header = "Reconnect", Command = source.ReconnectCommand });
                    deviceMenu.Items.Add(new System.Windows.Controls.MenuItem { Header = "Disconnect", Command = source.DisconnectCommand });
                    mainWindow.DevicesTrayMenuItem.Items.Add(deviceMenu);
                }
                mainWindow.DisconnectAllTrayMenuItem.IsEnabled = mainViewModel.DisconnectAllCommand.CanExecute(null);
            };
            mainWindow.ExitTrayMenuItem.Click += (_, _) =>
            {
                if (mainViewModel.ExitCommand.CanExecute(null))
                {
                    mainViewModel.ExitCommand.Execute(null);
                }
            };

            mainWindow.TrayIcon.ShowBalloonTip("Easy Bluetooth Audio", "App started in system tray.", Hardcodet.Wpf.TaskbarNotification.BalloonIcon.Info);

            WeakReferenceMessenger.Default.Register<ShowBalloonRequestedMessage>(this, (_, msg) =>
                Current.Dispatcher.InvokeAsync(() =>
                    mainWindow.TrayIcon?.ShowBalloonTip(msg.Value.Title, msg.Value.Body, msg.Value.Icon)));

            mainViewModel.RequestShow += () =>
            {
                mainWindow.WindowState = WindowState.Normal;
                windowPlacementService.SetSettingsExpanded(mainWindow, false, animate: false);
                windowPlacementService.PositionNearCursor(mainWindow);
                if (mainViewModel.IsSettingsOpen)
                {
                    windowPlacementService.SetSettingsExpanded(mainWindow, true, animate: false);
                }
                mainWindow.Show();
                mainWindow.Activate();
            };
            mainViewModel.RequestHide += () =>
            {
                windowPlacementService.SetSettingsExpanded(mainWindow, false, animate: false);
                mainWindow.Hide();
            };
            mainViewModel.RequestActivate += () => mainWindow.Activate();

            mainViewModel.RequestExit += () =>
            {
                Debug.WriteLine("[App] RequestExit received from tray.");
                _isExiting = true;
                _refreshTimer?.Stop();
                if (!mainWindow.TrayIcon.IsDisposed)
                {
                    mainWindow.TrayIcon.Dispose();
                }

                mainWindow.Close();
                Current.Shutdown();
            };

            this.SessionEnding += (s, ev) => _isExiting = true;

            mainWindow.Deactivated += (s, ev) =>
            {
                if (mainViewModel.IsExternalInteractionActive || mainViewModel.SettingsViewModel.IsApplyingQualityChange
                    || System.Windows.Input.Mouse.Captured != null || mainWindow.TrayIcon.ContextMenu.IsOpen)
                {
                    return;
                }
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
                    mainViewModel.CloseOrHideCommand.Execute(null);
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
                mainViewModel.OpenCommand.Execute(null);
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
        Debug.WriteLine($"[App] OnExit code={e.ApplicationExitCode}");

        // Dispose the audio service to release the AudioPlaybackConnection and its
        // associated mixer endpoints (render + capture) before the process exits.
        // Without this, the virtual audio endpoints linger in the Windows Volume Mixer
        // and require a reboot or driver reset to disappear.
        _refreshTimer?.Stop();
        _mainViewModel?.Dispose();
        ServiceProvider?.GetService<IAudioService>()?.Dispose();
        _connectionDiagnostics?.Dispose();

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
    /// Writes the low-bandwidth SBC bitpool values to the registry and exits.
    /// Called only when the process is launched with <see cref="ArgApplyLowEndMode"/> under UAC elevation.
    /// </summary>
    private static void RunElevatedApply()
    {
        try
        {
            using var key = Registry.LocalMachine.CreateSubKey(BluetoothQualityService.AvdtpSbcKeyPath, writable: true);
            if (key == null)
            {
                Environment.Exit(1);
                return;
            }

            key.SetValue("MaximumBitpool", BluetoothQualityService.LowBandwidthBitpool, RegistryValueKind.DWord);
            key.SetValue("DefaultBitpool", BluetoothQualityService.LowBandwidthBitpool, RegistryValueKind.DWord);
            Environment.Exit(0);
        }
        catch
        {
            Environment.Exit(1);
        }
    }

    /// <summary>
    /// Deletes the SBC bitpool registry key to restore Windows defaults and exits.
    /// Called only when the process is launched with <see cref="ArgRestoreLowEndMode"/> under UAC elevation.
    /// </summary>
    private static void RunElevatedRestore()
    {
        try
        {
            Registry.LocalMachine.DeleteSubKey(BluetoothQualityService.AvdtpSbcKeyPath, throwOnMissingSubKey: false);
            Environment.Exit(0);
        }
        catch
        {
            Environment.Exit(1);
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
        services.AddSingleton<IConnectionDiagnostics, ConnectionDiagnostics>();
        services.AddSingleton<IAudioPlaybackConnectionFactory, AudioPlaybackConnectionFactory>();
        services.AddSingleton<IAudioService, AudioService>();
        services.AddSingleton<Func<Models.BluetoothDevice, DeviceConnectionViewModel>>(provider => device =>
            new DeviceConnectionViewModel(device, provider.GetRequiredService<IAudioService>(),
                provider.GetRequiredService<IDispatcherService>(), provider.GetRequiredService<IMessenger>()));
        services.AddSingleton<IDevicePickerService, DevicePickerService>();
        services.AddSingleton<IWindowPlacementService, WindowPlacementService>();
        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddSingleton<IStartupService, StartupService>();
        services.AddSingleton<IThemeService, ThemeService>();
        services.AddSingleton<ISoundService, SoundService>();
        services.AddSingleton<IBluetoothQualityService, BluetoothQualityService>();
        services.AddSingleton<HttpClient>();
        services.AddSingleton<IUpdateService, UpdateService>();
        services.AddTransient<SettingsViewModel>();
        services.AddSingleton<UpdateViewModel>();
        services.AddSingleton<MainViewModel>();
        services.AddSingleton<MainWindow>();
    }
}
