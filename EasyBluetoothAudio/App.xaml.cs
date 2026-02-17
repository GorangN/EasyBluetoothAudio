using Microsoft.Extensions.DependencyInjection;
using EasyBluetoothAudio.Services;
using EasyBluetoothAudio.ViewModels;
using System.Windows;

namespace EasyBluetoothAudio
{
    public partial class App : Application
    {
        public static IServiceProvider? ServiceProvider { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);

            ServiceProvider = serviceCollection.BuildServiceProvider();

            var mainWindow = ServiceProvider.GetRequiredService<MainWindow>();
            mainWindow.Show();

            base.OnStartup(e);
        }

        private void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<IAudioService, AudioService>();
            services.AddSingleton<MainViewModel>();
            services.AddTransient<MainWindow>();
        }
    }
}
