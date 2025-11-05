using System.Globalization;
using DatabookService.Application.Features.DatabookTypes;
using DatabookService.Application.Features.ApiKeys;
using DatabookService.Application.Interfaces;
using DatabookService.Infrastructure.Data;
using DatabookService.Infrastructure.Authentication;
using DatabookService.Infrastructure.Repositories;
using DatabookService.Infrastructure.Services;
using DatabookService.Web.EndpointsSettings;
using DatabookService.Web.Middleware;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Exceptions;
using DatabookService.Application.Interfaces.Repositories;
using DatabookService.Application.Features.DatabookContent;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console(formatProvider: CultureInfo.InvariantCulture)
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Services.AddEndpoints(typeof(Program).Assembly);
    builder.Services.AddDirectoryTypesFeature(); 
    builder.Services.AddDatabooksContentFeature();
    builder.Services.AddApiKeysFeature();

    // Add services to the container
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        var scheme = new Microsoft.OpenApi.Models.OpenApiSecurityScheme
        {
            Name = "X-API-Key",
            Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
            In = Microsoft.OpenApi.Models.ParameterLocation.Header,
            Reference = new Microsoft.OpenApi.Models.OpenApiReference
            {
                Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                Id = "ApiKey"
            },
            Scheme = "ApiKey",
            Description = "Provide API key via X-API-Key header or Authorization: ApiKey <key>"
        };
        c.AddSecurityDefinition("ApiKey", scheme);
        c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
        {
            [scheme] = new List<string>()
        });
    });
    
    builder.Services.AddSerilog((sp, lc) => lc
            .ReadFrom.Configuration(builder.Configuration)
            .ReadFrom.Services(sp)
            .Enrich.FromLogContext()
            .Enrich.WithExceptionDetails()
            .Enrich.WithProperty("ServiceName", "DatabookService")
            .WriteTo.Console(formatProvider: CultureInfo.InvariantCulture)
            .WriteTo.Seq("http://seq:80")
    );

    // Database
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

    // Repositories
    builder.Services.AddScoped<IDirectoryTypeRepository, DirectoryTypeRepository>();
    builder.Services.AddScoped<DatabookService.Application.Interfaces.Repositories.IApiKeyRepository, ApiKeyRepository>();
    builder.Services.AddScoped<DatabookService.Application.Interfaces.Repositories.IDirectoryCollectionRepository, DirectoryCollectionRepository>();

    // Services
    builder.Services.AddScoped<IDynamicTableService, DynamicTableService>(sp =>
        new DynamicTableService(sp.GetRequiredService<ApplicationDbContext>(), sp.GetRequiredService<IConfiguration>()));

    // Security services
    builder.Services.AddSingleton<DatabookService.Application.Interfaces.Security.IApiKeyHasher, DatabookService.Infrastructure.Security.ApiKeyHasherPbkdf2>();
    builder.Services.AddSingleton<DatabookService.Application.Interfaces.Security.IApiKeyGenerator, DatabookService.Infrastructure.Security.ApiKeyGenerator>();

    // Auth
    builder.Services
        .AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = ApiKeyAuthenticationHandler.Scheme;
            options.DefaultChallengeScheme = ApiKeyAuthenticationHandler.Scheme;
        })
        .AddScheme<ApiKeyAuthenticationOptions, ApiKeyAuthenticationHandler>(
            ApiKeyAuthenticationHandler.Scheme,
            _ => { });

    builder.Services.Configure<ApiKeyAuthenticationOptions>(builder.Configuration.GetSection("ApiKeyAuth"));

    builder.Services.AddAuthorization(options =>
    {
        options.AddPolicy("AdminOnly", policy =>
            policy.RequireAuthenticatedUser().RequireRole("Admin"));

        options.AddPolicy("Contributor", policy =>
            policy.RequireAuthenticatedUser().RequireRole("Admin", "User"));
    });

    // CORS
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowFrontend", policy =>
        {
            policy.WithOrigins("http://localhost:3000", "http://localhost", "http://localhost:80") // для разработки и Docker
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        });
    });

    var app = builder.Build();

    // Apply database migrations
    using (var scope = app.Services.CreateScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        try
        {
            dbContext.Database.Migrate();

            // Dev seed: create initial admin key if none exists
            if (app.Environment.IsDevelopment())
            {
                if (!dbContext.ApiKeys.Any())
                {
                    var generator = scope.ServiceProvider.GetRequiredService<DatabookService.Application.Interfaces.Security.IApiKeyGenerator>();
                    var hasher = scope.ServiceProvider.GetRequiredService<DatabookService.Application.Interfaces.Security.IApiKeyHasher>();
                    var plain = generator.Generate();
                    var hash = hasher.Hash(plain);
                    dbContext.ApiKeys.Add(new DatabookService.Domain.Entities.ApiKey("Dev Admin", hash, DatabookService.Domain.Enums.ApiKeyRole.Admin, null));
                    await dbContext.SaveChangesAsync();
                    var seedLogger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
                    seedLogger.LogWarning("Seeded DEV admin API key (showing once): {ApiKey}", plain);
                }
            }
        }
        catch (Exception ex)
        {
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
            logger.LogError(ex, "An error occurred while migrating the database.");
        }
    }

    // // Configure the HTTP request pipeline
    // if (app.Environment.IsDevelopment())
    // {
        app.UseSwagger();
        app.UseSwaggerUI();
    // }

    app.UseMiddleware<ErrorHandlingMiddleware>();

    app.UseAuthentication();
    app.UseAuthorization();

    app.UseCors("AllowFrontend");

    app.MapDirectoryTypesFeature();
    app.MapDatabooksContentFeature();
    app.MapApiKeysFeature();

    app.Run();

}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
