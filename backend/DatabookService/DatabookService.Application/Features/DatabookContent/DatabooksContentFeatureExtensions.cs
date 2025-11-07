using DatabookService.Application.Features.DatabookContent.Create;
using DatabookService.Application.Features.DatabookTypes.Create;
using DatabookService.Application.Features.DatabookTypes.GetAll;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabookService.Application.Features.DatabookContent
{
    public static class DatabooksContentFeatureExtensions
    {
        public static IServiceCollection AddDatabooksContentFeature(this IServiceCollection services)
        {
            // Регистрация Commands и Queries
            services.AddScoped<CreateDatabookRecordCommand>();

            return services;
        }

        public static void MapDatabooksContentFeature(this WebApplication app)
        {
            // Регистрация endpoints
            var createEndpoint = new CreateDatabookRecordEndpoint();
            createEndpoint.MapEndpoint(app);
        }
    }
}
