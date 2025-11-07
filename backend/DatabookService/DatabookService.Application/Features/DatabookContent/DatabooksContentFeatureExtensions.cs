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
using DatabookService.Application.Features.DatabookContent.Delete;
using DatabookService.Application.Features.DatabookContent.Delete.BackgroundJobs;
using DatabookService.Application.Features.DatabookContent.Get;

namespace DatabookService.Application.Features.DatabookContent
{
    public static class DatabooksContentFeatureExtensions
    {
        public static IServiceCollection AddDatabooksContentFeature(this IServiceCollection services)
        {
            // Регистрация Commands и Queries
            services.AddScoped<CreateDatabookRecordCommand>();
            services.AddScoped<SoftDeleteDirectoryRecordCommand>();
            services.AddScoped<GetAllDatabookRecordsQuery>();
            services.AddScoped<GetDatabookRecordByIdQuery>();
            
            //Фоновые процессы
            services.AddHostedService<DirectoryRecordsCleanupService>();
            
            return services;
        }

        public static void MapDatabooksContentFeature(this WebApplication app)
        {
            // Регистрация endpoints
            var createEndpoint = new CreateDatabookRecordEndpoint();
            createEndpoint.MapEndpoint(app);
            
            var softDeleteDirectoryRecord = new SoftDeleteDirectoryRecordEndpoint();
            softDeleteDirectoryRecord.MapEndpoint(app);
            
            var getDatabookEndpoints = new GetDatabookEndpoints();
            getDatabookEndpoints.MapEndpoint(app);
        }
    }
}
