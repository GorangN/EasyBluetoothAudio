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

        // Subscribe to ViewModel requests
        viewModel.RequestShow += () => 
        { 
            Show(); 
            WindowState = WindowState.Normal; 
            Activate(); 
        };
        
        viewModel.RequestExit += () => Application.Current.Shutdown();
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