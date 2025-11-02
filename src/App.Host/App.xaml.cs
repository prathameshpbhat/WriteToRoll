using App.Core.Services;
using App.Persistence.Services;
using App.UI.Controls;
using App.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Controls;

namespace App.Host
{
    public partial class AppEntryPoint : Application
    {
        private IServiceProvider? _serviceProvider;

        protected override void OnStartup(StartupEventArgs e)
        {
            var services = new ServiceCollection();

            // Register services
            services.AddSingleton<FountainParser>();
            services.AddSingleton<IScriptService, ScriptService>();

            // Register view models
            services.AddSingleton<MainViewModel>();

            // Register main window
            services.AddSingleton<MainWindow>();

            _serviceProvider = services.BuildServiceProvider();

            var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
            mainWindow.Show();
        }
    }
}
