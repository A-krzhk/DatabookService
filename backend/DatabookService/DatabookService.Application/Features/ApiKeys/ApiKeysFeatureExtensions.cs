using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace DatabookService.Application.Features.ApiKeys;

public static class ApiKeysFeatureExtensions
{
    public static IServiceCollection AddApiKeysFeature(this IServiceCollection services)
    {
        services.AddScoped<Create.CreateApiKeyCommand>();
        services.AddScoped<List.ListApiKeysQuery>();
        services.AddScoped<Revoke.RevokeApiKeyCommand>();
        services.AddScoped<Rotate.RotateApiKeyCommand>();
        return services;
    }

    public static void MapApiKeysFeature(this WebApplication app)
    {
        new Create.CreateApiKeyEndpoint().MapEndpoint(app);
        new List.ListApiKeysEndpoint().MapEndpoint(app);
        new Revoke.RevokeApiKeyEndpoint().MapEndpoint(app);
        new Rotate.RotateApiKeyEndpoint().MapEndpoint(app);
    }
}


