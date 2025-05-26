using Microsoft.Extensions.Configuration;
using Microsoft.Identity.Client;

namespace TomMcp.Services;

public sealed class AuthManager
{
    private readonly IConfidentialClientApplication _app;
    private AuthenticationResult? _cache;

    public AuthManager(IConfiguration cfg)
    {
        var tenantId = cfg["AZURE_TENANT_ID"] ?? throw new InvalidOperationException("AZURE_TENANT_ID not configured");
        var clientId = cfg["AZURE_CLIENT_ID"] ?? throw new InvalidOperationException("AZURE_CLIENT_ID not configured");
        var clientSecret = cfg["AZURE_CLIENT_SECRET"] ?? throw new InvalidOperationException("AZURE_CLIENT_SECRET not configured");
        
        _app = ConfidentialClientApplicationBuilder
            .Create(clientId)
            .WithClientSecret(clientSecret)
            .WithAuthority(AzureCloudInstance.AzurePublic, tenantId)
            .Build();
    }

    public async Task<string> GetAccessTokenAsync()
    {
        if (_cache is { ExpiresOn: var exp } && exp > DateTimeOffset.UtcNow.AddMinutes(5))
            return _cache.AccessToken;

        _cache = await _app
            .AcquireTokenForClient(new[] { "https://analysis.windows.net/powerbi/api/.default" })
            .ExecuteAsync();

        return _cache.AccessToken;
    }

    public string? AccessToken => _cache?.AccessToken;
}
