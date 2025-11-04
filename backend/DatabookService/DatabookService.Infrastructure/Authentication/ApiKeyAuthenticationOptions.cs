using Microsoft.AspNetCore.Authentication;

namespace DatabookService.Infrastructure.Authentication;

public class ApiKeyAuthenticationOptions : AuthenticationSchemeOptions
{
    public string HeaderName { get; set; } = "X-API-Key";
    public bool AllowAuthorizationHeader { get; set; } = true;
    public string AuthorizationScheme { get; set; } = "ApiKey";
}



