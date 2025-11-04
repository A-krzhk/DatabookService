using System.Globalization;
using DatabookService.Application.Features.DatabookTypes;
using DatabookService.Application.Interfaces;
using DatabookService.Infrastructure.Data;
using DatabookService.Infrastructure.Repositories;
using DatabookService.Infrastructure.Services;
using DatabookService.Web.EndpointsSettings;
using DatabookService.Web.Middleware;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Exceptions;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console(formatProvider: CultureInfo.InvariantCulture)
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Services.AddEndpoints(typeof(Program).Assembly);
    builder.Services.AddDirectoryTypesFeature();

    // Add services to the container
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();
    
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

    // Services
    builder.Services.AddScoped<IDynamicTableService, DynamicTableService>(sp =>
        new DynamicTableService(sp.GetRequiredService<ApplicationDbContext>()));

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

    app.UseCors("AllowFrontend");

    app.MapDirectoryTypesFeature();

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
