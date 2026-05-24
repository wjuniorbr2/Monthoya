using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Monthoya.Data;

public sealed class DesignTimeMonthoyaDbContextFactory : IDesignTimeDbContextFactory<MonthoyaDbContext>
{
    public MonthoyaDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<MonthoyaDbContext>()
            .UseNpgsql("Host=localhost;Port=5432;Database=monthoya_design_time;Username=postgres")
            .Options;

        return new MonthoyaDbContext(options);
    }
}
