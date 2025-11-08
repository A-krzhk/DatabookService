using DatabookService.Application.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

namespace DatabookService.Application.Features.DirectoryGroups.Delete;

public class DeleteDirectoryGroupCommand
{
    private readonly IDirectoryGroupRepository _repository;
    private readonly ILogger<DeleteDirectoryGroupCommand> _logger;

    public DeleteDirectoryGroupCommand(
        IDirectoryGroupRepository repository,
        ILogger<DeleteDirectoryGroupCommand> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task ExecuteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting directory group {GroupId}", id);

        var group = await _repository.GetByIdAsync(id, cancellationToken);
        if (group == null)
            throw new InvalidOperationException($"Directory group with ID '{id}' not found");

        if (group.Name == "Без группы")
            throw new InvalidOperationException("Default group cannot be deleted");

        await _repository.DeleteAsync(id, cancellationToken);

        _logger.LogInformation("Directory group {GroupId} deleted successfully", id);
    }
}
