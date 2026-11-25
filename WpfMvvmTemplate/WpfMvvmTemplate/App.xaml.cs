using System.Windows;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using WpfMvvmTemplate.Services;
using WpfMvvmTemplate.Services.Plc;
using WpfMvvmTemplate.ViewModels;
using WpfMvvmTemplate.Views;

namespace WpfMvvmTemplate;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private readonly IHost _host;

    public App()
    {
        DispatcherUnhandledException += OnDispatcherUnhandledException;

        _host = Host.CreateDefaultBuilder()
            .ConfigureServices((_, services) =>
            {
                // Database & Services
                services.AddSingleton<IDbConnectionFactory, SqliteDbContext>();
                services.AddSingleton<IResultRepository, SqliteResultRepository>();
                services.AddSingleton<INavigationService, NavigationService>();
                services.AddSingleton<IDialogService, DialogService>();
                services.AddSingleton<ISettingsService, SettingsService>();
                services.AddSingleton<IPlcService, MockPlcService>();

                // ViewModels
                services.AddSingleton<MainViewModel>();
                services.AddTransient<HomeViewModel>();
                services.AddTransient<SelfDiagnosisViewModel>();
                services.AddTransient<SearchResultViewModel>();
                services.AddTransient<PasswordPromptViewModel>();
                services.AddTransient<SettingsViewModel>();
                services.AddTransient<TestViewModel>();
#if DEBUG
                services.AddTransient<PlcTestViewModel>();
#endif

                // Views
                services.AddSingleton<MainWindow>();
            })
            .Build();
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        await _host.StartAsync();

        // SQLite DB 및 초기 테이블/설정 생성
        _host.Services.GetRequiredService<IDbConnectionFactory>().Initialize();

        var mainWindow = _host.Services.GetRequiredService<MainWindow>();
        mainWindow.DataContext = _host.Services.GetRequiredService<MainViewModel>();
        mainWindow.Show();

        base.OnStartup(e);
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        await _host.StopAsync();
        _host.Dispose();

        base.OnExit(e);
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        e.Handled = true;
        MessageBox.Show(
            $"예기치 않은 오류가 발생했습니다.\n\n{e.Exception.Message}",
            "오류",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
    }
}
