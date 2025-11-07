using DatabookService.Application.Interfaces.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DatabookService.Application.Features.DatabookContent.Delete.BackgroundJobs;

public class DirectoryRecordsCleanupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DirectoryRecordsCleanupService> _logger;

    public DirectoryRecordsCleanupService(IServiceProvider serviceProvider, ILogger<DirectoryRecordsCleanupService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("DirectoryRecordsCleanupService started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var repository = scope.ServiceProvider.GetRequiredService<IDirectoryContentRepository>();

                var expiredRecords = await repository.GetSoftDeletedOlderThanAsync(TimeSpan.FromDays(30), stoppingToken);

                _logger.LogInformation("Found {Count} expired soft-deleted records", expiredRecords.Count);

                foreach (var (directoryTypeId, recordId, tableName) in expiredRecords)
                {
                    await repository.DeletePhysicallyAsync(directoryTypeId, recordId, tableName, stoppingToken);
                }

                _logger.LogInformation("Cleanup complete. Deleted {Count} records", expiredRecords.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while cleaning up directory records");
            }

            // Проверка раз в сутки
            await Task.Delay(TimeSpan.FromDays(1), stoppingToken);
        }
    }
}