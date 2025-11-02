
using DatabookService.Application.Commands;
using DatabookService.Application.Interfaces;
using DatabookService.Application.Queries;
using DatabookService.Application.Services;
using DatabookService.Infrastructure.Data;
using DatabookService.Infrastructure.Repositories;
using DatabookService.Web.EndpointSettings;
using DatabookService.Web.Middleware;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Database
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Repositories
builder.Services.AddScoped<IDirectoryTypeRepository, DirectoryTypeRepository>();

// Services
builder.Services.AddScoped<IDynamicTableService, DynamicTableService>(sp =>
    new DynamicTableService(sp.GetRequiredService<ApplicationDbContext>()));

// Commands and Queries
builder.Services.AddScoped<CreateDirectoryTypeCommand>();
builder.Services.AddScoped<GetAllDirectoryTypesQuery>();

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<ErrorHandlingMiddleware>();

app.UseCors("AllowAll");

// Map endpoints
app.MapDirectoryTypeEndpoints();

app.Run();