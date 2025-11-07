using DatabookService.Application.Features.DatabookTypes.Create;
using DatabookService.Application.Features.DatabookTypes.GetAll;
using DatabookService.Application.Features.DatabookTypes.Update;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace DatabookService.Application.Features.DatabookTypes;

/// <summary>
/// Расширения для регистрации DirectoryTypes фичи
/// </summary>
public static class DirectoryTypesFeatureExtensions
{
    public static IServiceCollection AddDirectoryTypesFeature(this IServiceCollection services)
    {
        // Регистрация Commands и Queries
        services.AddScoped<CreateDirectoryTypeCommand>();
        services.AddScoped<GetAllDirectoryTypesQuery>();
        services.AddScoped<AddFieldCommand>();
        services.AddScoped<RemoveFieldCommand>();
        services.AddScoped<UpdateFieldCommand>();
        return services;
    }

    public static void MapDirectoryTypesFeature(this WebApplication app)
    {
        // Регистрация endpoints
        var createEndpoint = new CreateDirectoryTypeEndpoint();
        createEndpoint.MapEndpoint(app);

        var getAllEndpoint = new GetAllDirectoryTypesEndpoint();
        getAllEndpoint.MapEndpoint(app);
        
        var addFieldEndpoint = new AddFieldEndpoint();
        addFieldEndpoint.MapEndpoint(app);

        var removeFieldEndpoint = new RemoveFieldEndpoint();
        removeFieldEndpoint.MapEndpoint(app);

        var updateFieldEndpoint = new UpdateFieldEndpoint();
        updateFieldEndpoint.MapEndpoint(app);
    }
}