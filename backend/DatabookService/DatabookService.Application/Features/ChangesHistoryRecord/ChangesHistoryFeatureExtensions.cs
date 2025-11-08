using DatabookService.Application.Features.ChangesHistoryRecord.Get;
using DatabookService.Application.Features.DatabookContent.Create;
using DatabookService.Application.Features.DatabookContent.Delete;
using DatabookService.Application.Features.DatabookContent.Delete.BackgroundJobs;
using DatabookService.Application.Features.DatabookContent.Get;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabookService.Application.Features.ChangesHistoryRecord
{
    public static class ChangesHistoryFeatureExtensions
    {
        public static IServiceCollection AddChangesHistoryFeature(this IServiceCollection services)
        {
            // Регистрация Commands и Queries
            services.AddScoped<GetHistoryByDirectoryTypeQuery>();

            return services;
        }

        public static void MapChangesHistoryFeature(this WebApplication app)
        {
            // Регистрация endpoints
            var createEndpoint = new GetHistoryByDirectoryTypeEndpoint();
            createEndpoint.MapEndpoint(app);
        }
    }
}
