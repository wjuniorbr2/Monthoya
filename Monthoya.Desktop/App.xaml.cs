using System.Windows;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Monthoya.Data;
using Monthoya.Desktop.Services;
using Monthoya.Desktop.Views;

namespace Monthoya.Desktop;

public partial class App : Application
{
    private IHost? _host;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _host = Host.CreateDefaultBuilder(e.Args)
            .ConfigureAppConfiguration(configuration =>
            {
                configuration.SetBasePath(AppContext.BaseDirectory);
                configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
                configuration.AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true);
                configuration.AddUserSecrets<App>(optional: true);
                configuration.AddEnvironmentVariables("MONTHOYA_");
            })
            .ConfigureServices((context, services) =>
            {
                services.AddMonthoyaData(context.Configuration);
                services.AddSingleton<AppStartup>();
                services.AddTransient<ConfigurationWindow>();
                services.AddTransient<SetupAdminWindow>();
                services.AddTransient<LoginWindow>();
                services.AddTransient<ShellWindow>();
            })
            .Build();

        await _host.StartAsync();
        await _host.Services.GetRequiredService<AppStartup>().RunAsync();
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        if (_host is not null)
        {
            await _host.StopAsync(TimeSpan.FromSeconds(5));
            _host.Dispose();
        }

        base.OnExit(e);
    }
}
