using Microsoft.Extensions.Logging;

namespace FeborBack.Infrastructure.Data;

public class DatabaseInitializer
{
    private readonly ILogger<DatabaseInitializer> _logger;

    public DatabaseInitializer(ILogger<DatabaseInitializer> logger)
    {
        _logger = logger;
    }

    public Task InitializeAsync()
    {
        _logger.LogInformation("DatabaseInitializer: No hay migraciones pendientes");

        return Task.CompletedTask;
    }
}
