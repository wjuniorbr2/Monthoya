using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Monthoya.Core.Entities;
using Monthoya.Core.Integrations;
using Monthoya.Core.Services;
using Monthoya.Data.Dashboard;
using Monthoya.Data.Documents;
using Monthoya.Data.RentalManagement;
using Monthoya.Data.Settings;
using Monthoya.Data.Storage;
using Monthoya.Data.Users;

namespace Monthoya.Data;

public static class DependencyInjection
{
    public static IServiceCollection AddMonthoyaData(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration[$"{DatabaseOptions.SectionName}:{nameof(DatabaseOptions.ConnectionString)}"];

        services.AddScoped<PessoaDocumentoOcrAutofillSaveChangesInterceptor>();

        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            services.AddDbContext<MonthoyaDbContext>((serviceProvider, options) => options
                .UseNpgsql(connectionString)
                .AddInterceptors(serviceProvider.GetRequiredService<PessoaDocumentoOcrAutofillSaveChangesInterceptor>()));
        }

        services.AddScoped<PasswordHasher<AppUser>>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IUserPasswordService, UserPasswordService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<RentalManagementService>();
        services.AddScoped<IRentalManagementService, ChavesSafeRentalManagementService>();
        services.AddScoped<IAgenciaPerfilService, AgenciaPerfilService>();
        services.AddScoped<IFileStorageService, ConfiguredFileStorageService>();
        services.AddScoped<IDocumentOcrService, LocalDocumentOcrService>();
        services.AddScoped<IBoletoProvider, LocalBoletoProvider>();
        services.AddScoped<INfseProvider, ManualPortalNfseProvider>();

        return services;
    }
}
