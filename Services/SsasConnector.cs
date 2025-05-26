using Microsoft.AnalysisServices.AdomdClient;
using Microsoft.Extensions.Configuration;

namespace TomMcp.Services;

public sealed class SsasConnector
{
    private readonly AuthManager _auth;
    private readonly string _endpoint;

    public SsasConnector(AuthManager auth, IConfiguration cfg)
    {
        _auth = auth;
        _endpoint = cfg["FABRIC_XMLA_ENDPOINT"] ?? throw new InvalidOperationException("FABRIC_XMLA_ENDPOINT not configured");
    }    public async Task<AdomdConnection> GetConnectionAsync()
    {
        var token = await _auth.GetAccessTokenAsync();
        var cs = $"Data Source={_endpoint};User ID=;Password={token};" +
                 "Persist Security Info=True;Impersonation Level=Impersonate";
        var conn = new AdomdConnection(cs);
        conn.Open();
        return conn;
    }
}
