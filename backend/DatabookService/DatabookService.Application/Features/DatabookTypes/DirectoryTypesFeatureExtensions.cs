using DatabookService.Application.Features.DatabookTypes.Create;
using DatabookService.Application.Features.DatabookTypes.GetAll;
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

        return services;
    }

    public static void MapDirectoryTypesFeature(this WebApplication app)
    {
        // Регистрация endpoints
        var createEndpoint = new CreateDirectoryTypeEndpoint();
        createEndpoint.MapEndpoint(app);

        var getAllEndpoint = new GetAllDirectoryTypesEndpoint();
        getAllEndpoint.MapEndpoint(app);
    }
}