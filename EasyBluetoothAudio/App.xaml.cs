using System.Linq;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using EasyBluetoothAudio.Services;
using EasyBluetoothAudio.ViewModels;
using EasyBluetoothAudio.Views;

namespace EasyBluetoothAudio;

/// <summary>
/// Interaction logic for App.xaml. Handles application startup and Dependency Injection configuration.
/// </summary>
public partial class App : System.Windows.Application
{
    /// <summary>
    /// Gets the current <see cref="IServiceProvider"/> instance.
    /// </summary>
    public static IServiceProvider? ServiceProvider { get; private set; }

    /// <inheritdoc />
    protected override void OnStartup(StartupEventArgs e)
    {
        var serviceCollection = new ServiceCollection();
        ConfigureServices(serviceCollection);

        ServiceProvider = serviceCollection.BuildServiceProvider();

        var mainWindow = ServiceProvider.GetRequiredService<MainWindow>();
        
        // Show window if manually launched (not via --silent flag)
        bool isSilent = e.Args.Any(arg => arg.Equals("--silent", StringComparison.OrdinalIgnoreCase));
        if (!isSilent)
        {
            mainWindow.Show();
        }
        
        base.OnStartup(e);
    }

    private void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<IAudioService, AudioService>();
        services.AddSingleton<MainViewModel>();
        // Register MainWindow as Singleton since it's the primary window
        services.AddSingleton<MainWindow>();
    }
}
