using System.Windows;

namespace EasyBluetoothAudio.Views;

/// <summary>
/// Main application window that hosts the tray icon and positions itself near the system tray.
/// </summary>
public partial class MainWindow : Window
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MainWindow"/> class.
    /// </summary>
    public MainWindow()
    {
        InitializeComponent();
    }
}