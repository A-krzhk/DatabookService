using DatabookService.Application.DTOs;
using DatabookService.Application.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

namespace DatabookService.Application.Features.DirectoryGroups.GetAll;

public class GetAllDirectoryGroupsCommand
{
    private readonly IDirectoryGroupRepository _repository;
    private readonly ILogger<GetAllDirectoryGroupsCommand> _logger;

    public GetAllDirectoryGroupsCommand(
        IDirectoryGroupRepository repository,
        ILogger<GetAllDirectoryGroupsCommand> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<List<DirectoryGroupDto>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting all directory groups");

        var groups = await _repository.GetAllAsync(cancellationToken);

        return groups.Select(g => new DirectoryGroupDto { Id = g.Id, Name = g.Name }).ToList();
    }
}
