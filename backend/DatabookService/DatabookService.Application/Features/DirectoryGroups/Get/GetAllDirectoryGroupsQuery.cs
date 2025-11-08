using DatabookService.Application.DTOs;
using DatabookService.Application.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

namespace DatabookService.Application.Features.DirectoryGroups.Get;

public class GetAllDirectoryGroupsQuery
{
    private readonly IDirectoryGroupRepository _repository;
    private readonly ILogger<GetAllDirectoryGroupsQuery> _logger;

    public GetAllDirectoryGroupsQuery(
        IDirectoryGroupRepository repository,
        ILogger<GetAllDirectoryGroupsQuery> logger)
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
