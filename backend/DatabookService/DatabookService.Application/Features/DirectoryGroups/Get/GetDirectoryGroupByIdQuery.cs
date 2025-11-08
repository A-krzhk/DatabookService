using DatabookService.Application.DTOs;
using DatabookService.Application.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

namespace DatabookService.Application.Features.DirectoryGroups.Get;

public class GetDirectoryGroupByIdQuery
{
    private readonly IDirectoryGroupRepository _repository;
    private readonly ILogger<GetDirectoryGroupByIdQuery> _logger;

    public GetDirectoryGroupByIdQuery(IDirectoryGroupRepository repository, ILogger<GetDirectoryGroupByIdQuery> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<DirectoryGroupDto?> ExecuteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation($"Getting directory group by id: {id}");
        var group = await _repository.GetByIdAsync(id, cancellationToken);
        if (group == null) return null;
        return new DirectoryGroupDto { Id = group.Id, Name = group.Name };
    }
}