using Monthoya.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddMonthoyaData(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapGet("/health", () => Results.Ok(new
{
    application = "Monthoya.Api",
    status = "Healthy"
}))
.WithName("HealthCheck");

app.MapGet("/health/database", async (
    IConfiguration configuration,
    IServiceProvider services,
    CancellationToken cancellationToken) =>
{
    var connectionString = configuration[$"{DatabaseOptions.SectionName}:{nameof(DatabaseOptions.ConnectionString)}"];

    if (string.IsNullOrWhiteSpace(connectionString))
    {
        return Results.Problem(
            title: "Database connection is not configured.",
            detail: "Set Database:ConnectionString with user secrets, environment variables, or a local development settings file.",
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }

    await using var scope = services.CreateAsyncScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<MonthoyaDbContext>();
    var canConnect = await dbContext.Database.CanConnectAsync(cancellationToken);

    return canConnect
        ? Results.Ok(new { database = "PostgreSQL", status = "Connected" })
        : Results.Problem(
            title: "Database connection failed.",
            statusCode: StatusCodes.Status503ServiceUnavailable);
})
.WithName("DatabaseHealthCheck");

app.Run();
