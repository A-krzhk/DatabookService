using DatabookService.Application.DTOs;
using DatabookService.Application.Interfaces.Repositories;
using DatabookService.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace DatabookService.Application.Features.DirectoryGroups.Create;

public class CreateDirectoryGroupCommand
{
    private readonly IDirectoryGroupRepository _repository;
    private readonly ILogger<CreateDirectoryGroupCommand> _logger;

    public CreateDirectoryGroupCommand(
        IDirectoryGroupRepository repository,
        ILogger<CreateDirectoryGroupCommand> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<DirectoryGroupDto> ExecuteAsync(
        CreateDirectoryGroupDto dto,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating Directory Group '{GroupName}'", dto.Name);

        var group = new DirectoryGroup(dto.Name);
        var created = await _repository.AddAsync(group, cancellationToken);

        _logger.LogInformation("Directory Group '{GroupName}' created successfully", dto.Name);

        return new DirectoryGroupDto { Id = created.Id, Name = created.Name };
    }
}
