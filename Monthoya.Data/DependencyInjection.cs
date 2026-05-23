using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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

        return services;
    }
}
