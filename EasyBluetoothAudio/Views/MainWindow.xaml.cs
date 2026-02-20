using System;
using System.Windows;
using System.Windows.Threading;
using EasyBluetoothAudio.ViewModels;

namespace EasyBluetoothAudio.Views;

/// <summary>
/// Main application window that hosts the tray icon and positions itself near the system tray.
/// </summary>
public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;
    private readonly DispatcherTimer _refreshTimer;

    /// <summary>
    /// Initializes a new instance of the <see cref="MainWindow"/> class.
    /// </summary>
    /// <param name="viewModel">The view model injected via dependency injection.</param>
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = _viewModel;

        TrayIcon.ShowBalloonTip("Easy Bluetooth Audio", "App started in system tray.", Hardcodet.Wpf.TaskbarNotification.BalloonIcon.Info);

        _viewModel.RequestShow += () =>
        {
            UpdatePosition();
            Show();
            WindowState = WindowState.Normal;
            Activate();
        };

        _viewModel.RequestExit += () => System.Windows.Application.Current.Shutdown();



        Deactivated += (s, e) => Hide();

        _refreshTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
        _refreshTimer.Tick += async (s, e) => await _viewModel.RefreshDevicesAsync();

        IsVisibleChanged += (s, e) =>
        {
            if (IsVisible)
            {
                _refreshTimer.Start();
                _ = _viewModel.RefreshDevicesAsync();
            }
            else
            {
                _refreshTimer.Stop();
            }
        };
    }

    /// <summary>
    /// Positions the window at the bottom-right corner of the working area on the current screen.
    /// </summary>
    private void UpdatePosition()
    {
        var mousePt = System.Windows.Forms.Cursor.Position;
        var screen = System.Windows.Forms.Screen.FromPoint(mousePt);
        var workArea = screen.WorkingArea;

        Left = workArea.Right - Width - 10;
        Top = workArea.Bottom - Height - 10;
    }

    /// <summary>
    /// Intercepts the close event and hides the window to the system tray instead.
    /// </summary>
    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        e.Cancel = true;
        Hide();
        base.OnClosing(e);
    }
}