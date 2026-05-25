using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Monthoya.Core.Entities;
using Monthoya.Core.Services;
using Monthoya.Data.Dashboard;
using Monthoya.Data.RentalManagement;
using Monthoya.Data.Users;
using Microsoft.AspNetCore.Identity;

namespace Monthoya.Data;

public static class DependencyInjection
{
    public static IServiceCollection AddMonthoyaData(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration[$"{DatabaseOptions.SectionName}:{nameof(DatabaseOptions.ConnectionString)}"];

        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            services.AddDbContext<MonthoyaDbContext>(options => options.UseNpgsql(connectionString));
        }

        services.AddScoped<PasswordHasher<AppUser>>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IRentalManagementService, RentalManagementService>();
        services.AddScoped<IBoletoProvider, LocalBoletoProvider>();
        services.AddScoped<INfseProvider, ManualPortalNfseProvider>();

        return services;
    }
}
