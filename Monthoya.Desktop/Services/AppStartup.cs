using System.Windows;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Monthoya.Core.Services;
using Monthoya.Data;
using Monthoya.Desktop.Views;

namespace Monthoya.Desktop.Services;

public sealed class AppStartup(
    IServiceProvider services,
    IConfiguration configuration)
{
    public async Task RunAsync()
    {
        var connectionString = configuration[$"{DatabaseOptions.SectionName}:{nameof(DatabaseOptions.ConnectionString)}"];
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            ShowConfigurationWindow("Configure a conexao com o PostgreSQL para iniciar o Monthoya.");
            return;
        }

        using var scope = services.CreateScope();
        var dbContext = scope.ServiceProvider.GetService<MonthoyaDbContext>();
        if (dbContext is null)
        {
            ShowConfigurationWindow("A conexao com o banco nao foi registrada. Revise a configuracao local.");
            return;
        }

        try
        {
            await dbContext.Database.MigrateAsync();
        }
        catch (Exception ex)
        {
            ShowConfigurationWindow($"Nao foi possivel conectar ou atualizar o banco de dados. Detalhes: {ex.Message}");
            return;
        }

        var userService = scope.ServiceProvider.GetRequiredService<IUserService>();
        if (!await userService.HasAnyUsersAsync())
        {
            var setupWindow = services.GetRequiredService<SetupAdminWindow>();
            var setupResult = setupWindow.ShowDialog();
            if (setupResult != true)
            {
                Application.Current.Shutdown();
                return;
            }
        }

        var loginWindow = services.GetRequiredService<LoginWindow>();
        var loginResult = loginWindow.ShowDialog();
        if (loginResult != true || loginWindow.AuthenticatedUser is null)
        {
            Application.Current.Shutdown();
            return;
        }

        var shellWindow = ActivatorUtilities.CreateInstance<ShellWindow>(services, loginWindow.AuthenticatedUser);
        Application.Current.MainWindow = shellWindow;
        Application.Current.ShutdownMode = ShutdownMode.OnMainWindowClose;
        shellWindow.Show();
    }

    private void ShowConfigurationWindow(string message)
    {
        var window = services.GetRequiredService<ConfigurationWindow>();
        window.SetMessage(message);
        window.ShowDialog();
        Application.Current.Shutdown();
    }
}
