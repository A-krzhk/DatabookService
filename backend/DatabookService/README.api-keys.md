# API Keys Auth

- Header: `X-API-Key: <key>` or `Authorization: ApiKey <key>`
- Roles: `Admin`, `User`
- Admin can manage directory types and API keys; User can only fill data.

Config (`DatabookService.Api/appsettings.json` → `ApiKeyAuth`):
- `HeaderName`: header name to read
- `AllowAuthorizationHeader`: enable `Authorization: ApiKey <key>`
- `AuthorizationScheme`: auth header scheme name
- `KeyPrefix`: generated key prefix

Swagger: API Key security is defined; authorize by providing the header value.

Development seed: if no keys exist, a dev Admin key is created on startup and printed to logs once.



