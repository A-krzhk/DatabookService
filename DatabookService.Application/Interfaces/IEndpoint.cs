using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace DatabookService.Application.Interfaces;

public interface IEndpoint
{
    /// <summary>
    /// Регистрирует endpoint в приложении
    /// </summary>
    void MapEndpoint(WebApplication app);
}