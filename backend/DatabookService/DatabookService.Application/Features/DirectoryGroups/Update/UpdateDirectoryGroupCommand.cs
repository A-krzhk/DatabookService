using DatabookService.Application.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

namespace DatabookService.Application.Features.DirectoryGroups.Update;

public class UpdateDirectoryGroupCommand
{
    private readonly IDirectoryGroupRepository _repository;
    private readonly ILogger<UpdateDirectoryGroupCommand> _logger;

    public UpdateDirectoryGroupCommand(
        IDirectoryGroupRepository repository,
        ILogger<UpdateDirectoryGroupCommand> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task ExecuteAsync(
        Guid id,
        string name,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating directory group {GroupId}", id);

        var group = await _repository.GetByIdAsync(id, cancellationToken);
        if (group == null)
            throw new InvalidOperationException($"Directory group with ID '{id}' not found");

        group.Update(name);
        await _repository.UpdateAsync(group, cancellationToken);

        _logger.LogInformation("Directory group {GroupId} updated successfully", id);
    }
}
