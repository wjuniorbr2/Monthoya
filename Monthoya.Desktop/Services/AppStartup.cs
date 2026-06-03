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
            ShowConfigurationWindow("Configure a conexão com o PostgreSQL para iniciar o Monthoya.");
            return;
        }

        using var scope = services.CreateScope();
        var dbContext = scope.ServiceProvider.GetService<MonthoyaDbContext>();
        if (dbContext is null)
        {
            ShowConfigurationWindow("A conexão com o banco não foi registrada. Revise a configuração local.");
            return;
        }

        try
        {
            await dbContext.Database.MigrateAsync();
        }
        catch (Exception ex)
        {
            ShowConfigurationWindow($"Não foi possível conectar ou atualizar o banco de dados. Detalhes: {ex.Message}");
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

        var agenciaPerfilService = scope.ServiceProvider.GetRequiredService<IAgenciaPerfilService>();
        if (!await agenciaPerfilService.HasProfileAsync())
        {
            var agencyWindow = ActivatorUtilities.CreateInstance<AgencyProfileWindow>(scope.ServiceProvider, true);
            var agencyResult = agencyWindow.ShowDialog();
            if (agencyResult != true)
            {
                Application.Current.Shutdown();
                return;
            }
        }

        EnsureRuntimeStyleAliases();

        while (true)
        {
            var loginWindow = services.GetRequiredService<LoginWindow>();
            var loginResult = loginWindow.ShowDialog();
            if (loginResult != true || loginWindow.AuthenticatedUser is null)
            {
                Application.Current.Shutdown();
                return;
            }

            EnsureRuntimeStyleAliases();

            // Create a scope that will live for the lifetime of the shell window.
            var windowScope = services.CreateScope();
            var shellWindow = ActivatorUtilities.CreateInstance<ShellWindow>(windowScope.ServiceProvider, loginWindow.AuthenticatedUser);
            Application.Current.MainWindow = shellWindow;
            Application.Current.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            shellWindow.ShowDialog();

            // Dispose the window scope after the shell window closes so scoped services (DbContext, etc.) are cleaned up.
            windowScope.Dispose();

            if (shellWindow.IsLogoutRequested)
            {
                Application.Current.MainWindow = null;
                continue;
            }

            Application.Current.Shutdown();
            return;
        }
    }

    private static void EnsureRuntimeStyleAliases()
    {
        var resources = Application.Current.Resources;

        if (!resources.Contains("PrimaryButtonSmall") && resources["PrimaryButton"] is Style primaryButtonStyle)
        {
            resources["PrimaryButtonSmall"] = primaryButtonStyle;
        }

        if (!resources.Contains("IconToolButtonDanger") && resources["IconToolButton"] is Style iconToolButtonStyle)
        {
            resources["IconToolButtonDanger"] = iconToolButtonStyle;
        }
    }

    private void ShowConfigurationWindow(string message)
    {
        var window = services.GetRequiredService<ConfigurationWindow>();
        window.SetMessage(message);
        window.ShowDialog();
        Application.Current.Shutdown();
    }
}
