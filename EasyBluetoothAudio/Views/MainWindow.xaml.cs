using System.Windows;
using EasyBluetoothAudio.ViewModels;

namespace EasyBluetoothAudio.Views;

/// <summary>
/// Interaction logic for MainWindow.xaml.
/// </summary>
public partial class MainWindow : Window
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MainWindow"/> class.
    /// </summary>
    /// <param name="viewModel">The view model injected via Dependency Injection.</param>
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;

        // Notify user that app started in tray
        TrayIcon.ShowBalloonTip("Easy Bluetooth Audio", "App started in system tray.", Hardcodet.Wpf.TaskbarNotification.BalloonIcon.Info);

        // Subscribe to ViewModel requests
        viewModel.RequestShow += () => 
        { 
            UpdatePosition();
            Show(); 
            WindowState = WindowState.Normal; 
            Activate(); 
        };
        
        viewModel.RequestExit += () => System.Windows.Application.Current.Shutdown();

        // Light Dismiss: Hide window when it loses focus
        Deactivated += (s, e) => Hide();
    }

    /// <summary>
    /// Calculates the position of the window to appear at the bottom-right corner of the working area
    /// on the screen where the mouse cursor is currently located.
    /// </summary>
    private void UpdatePosition()
    {
        // Use the mouse position to find the target screen
        var mousePt = System.Windows.Forms.Cursor.Position;
        var screen = System.Windows.Forms.Screen.FromPoint(mousePt);
        var workArea = screen.WorkingArea;
        
        // Horizontal: Snap to right edge of the target screen
        Left = workArea.Right - Width - 10;
        
        // Vertical: Snap to bottom edge of the target screen
        Top = workArea.Bottom - Height - 10;
    }

    /// <summary>
    /// Prevents the window from closing and hides it to the tray instead.
    /// </summary>
    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        e.Cancel = true;
        Hide();
        base.OnClosing(e);
    }
}