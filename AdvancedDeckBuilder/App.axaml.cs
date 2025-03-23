using AdvancedDeckBuilder.Services;
using AdvancedDeckBuilder.ViewModels;
using AdvancedDeckBuilder.Views;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace AdvancedDeckBuilder
{
    public partial class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = new MainWindow
                {
                    DataContext = new MainWindowViewModel(),
                };

                var services = new ServiceCollection();

                services.AddSingleton<IFilesService>(_ => new FilesService(desktop.MainWindow));
                services.AddSingleton<IAnalyzerTracker>(_ => new AnalyzerTracker());

                desktop.Exit += Desktop_Exit;

                Services = services.BuildServiceProvider();
            }

            base.OnFrameworkInitializationCompleted();
        }

        private void Desktop_Exit(object? sender, ControlledApplicationLifetimeExitEventArgs e)
        {
            var tracker = Current?.Services?.GetService<IAnalyzerTracker>();
            tracker?.CloseAllProcesses();
        }

        public new static App? Current => Application.Current as App;

        public IServiceProvider? Services { get; private set; }
    }
}