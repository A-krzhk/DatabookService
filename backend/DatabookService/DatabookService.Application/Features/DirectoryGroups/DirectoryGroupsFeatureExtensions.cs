using DatabookService.Application.Features.DirectoryGroups.Create;
using DatabookService.Application.Features.DirectoryGroups.Delete;
using DatabookService.Application.Features.DirectoryGroups.Get;
using DatabookService.Application.Features.DirectoryGroups.Update;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace DatabookService.Application.Features.DirectoryGroups;

/// <summary>
/// Extensions for registering and mapping the DirectoryGroups feature.
/// </summary>
public static class DirectoryGroupsFeatureExtensions
{
    public static IServiceCollection AddDirectoryGroupsFeature(this IServiceCollection services)
    {
        // Register Commands and Queries
        services.AddScoped<CreateDirectoryGroupCommand>();
        services.AddScoped<GetAllDirectoryGroupsQuery>();
        services.AddScoped<GetDirectoryGroupByIdQuery>();
        services.AddScoped<UpdateDirectoryGroupCommand>();
        services.AddScoped<DeleteDirectoryGroupCommand>();

        return services;
    }

    public static void MapDirectoryGroupsFeature(this WebApplication app)
    {
        // Register endpoints
        var createEndpoint = new CreateDirectoryGroupEndpoint();
        createEndpoint.MapEndpoint(app);

        var getAllEndpoint = new GetAllDirectoryGroupsEndpoint();
        getAllEndpoint.MapEndpoint(app);

        var getByIdEndpoint = new GetDirectoryGroupByIdEndpoint();
        getByIdEndpoint.MapEndpoint(app);

        var updateEndpoint = new UpdateDirectoryGroupEndpoint();
        updateEndpoint.MapEndpoint(app);

        var deleteEndpoint = new DeleteDirectoryGroupEndpoint();
        deleteEndpoint.MapEndpoint(app);
    }
}