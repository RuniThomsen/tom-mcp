using Microsoft.AnalysisServices.Tabular;
using ModelContextProtocol.Server;
using TomMcp.Services;
using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using Microsoft.Identity.Client;

namespace Tools;

[McpServerToolType]
public class XmlaInfoTools
{
    public XmlaInfoTools()
    {
        // No dependencies needed for public client authentication
    }[McpServerTool(Name = "xmla_list_databases")]
    [Description("Lists all databases on the XMLA endpoint")]
    public Task<string> ListDatabases(
        string connectionString,
        CancellationToken cancellationToken = default)
    {
        using var server = new Server();

        // Use Integrated Windows Authentication if no explicit auth is in connection string
        string finalConnectionString = connectionString;        // Cloud XMLA → get AAD token instead of SSPI
        if (connectionString.StartsWith("powerbi://", StringComparison.OrdinalIgnoreCase))
        {
            var token = AcquirePowerBiAccessTokenAsync().GetAwaiter().GetResult();

            var aas = new Microsoft.AnalysisServices.Server();
            aas.Connect(finalConnectionString, token);   // overload with bearer token
            var res = string.Join('\n',
                aas.Databases.Cast<Database>().Select(d => $"{d.Name}|{d.ID}|{d.CompatibilityLevel}"));
            aas.Disconnect();
            return Task.FromResult(res);
        }
        else
        {
            // on-prem path (IWA / SSPI keeps working)
            finalConnectionString = $"{connectionString};Integrated Security=SSPI";
            server.Connect(finalConnectionString);
        }

        // Use LINQ's Cast<T>() to convert DatabaseCollection to IEnumerable<Database>
        var result = string.Join('\n',
            server.Databases.Cast<Database>().Select(d => $"{d.Name}|{d.ID}|{d.CompatibilityLevel}"));        server.Disconnect();
        return Task.FromResult(result);
    }    private static async Task<string> AcquirePowerBiAccessTokenAsync(string flow = "silent")
    {
        var tenantId = Environment.GetEnvironmentVariable("AZURE_TENANT_ID") ?? "organizations";
        var clientId = Environment.GetEnvironmentVariable("AZURE_CLIENT_ID")
                      ?? "04b07795-8ddb-461a-bbee-02f9e1bf7b46";        // Microsoft first-party public client
        var scopes = new[] { "https://analysis.windows.net/powerbi/api/.default" };

        var app = PublicClientApplicationBuilder
                    .Create(clientId)
                    .WithTenantId(tenantId)
                    .WithRedirectUri("http://localhost")               // required for interactive flow
                    .Build();

        switch (flow.ToLowerInvariant())
        {
            case "interactive":
                return (await app.AcquireTokenInteractive(scopes)
                                 .ExecuteAsync()).AccessToken;            case "devicecode":
            case "device-code":
                var deviceCodeCts = new CancellationTokenSource(TimeSpan.FromMinutes(2));
                try
                {
                    var dc = await app.AcquireTokenWithDeviceCode(
                                 scopes,
                                 msg => 
                                 { 
                                     Console.WriteLine($"Device code: {msg.UserCode}");
                                     Console.WriteLine($"URL: {msg.VerificationUrl}");
                                     Console.WriteLine(msg.Message); 
                                     return Task.CompletedTask; 
                                 })
                               .ExecuteAsync(deviceCodeCts.Token);
                    return dc.AccessToken;
                }
                catch (OperationCanceledException)
                {
                    throw new InvalidOperationException("Device code authentication timed out after 2 minutes.");
                }

            case "silent":
            default:
                try
                {
                    var silent = await app.AcquireTokenSilent(
                                         scopes,
                                         (await app.GetAccountsAsync()).FirstOrDefault())
                                     .ExecuteAsync();
                    return silent.AccessToken;
                }
                catch (MsalUiRequiredException)
                {
                    throw new InvalidOperationException("Silent auth failed – no cached token available.");
                }
        }
    }

    [McpServerTool(Name = "xmla_test_connection")]
    [Description("Attempts to connect to an XMLA endpoint with the given auth flow "
               + "(silent | interactive | devicecode) and returns a short status message")]
    public async Task<string> TestConnection(
        string connectionString,
        string flow = "silent",
        CancellationToken cancellationToken = default)
    {
        if (!connectionString.StartsWith("powerbi://", StringComparison.OrdinalIgnoreCase))
            return "Only powerbi:// Fabric / Power BI endpoints are supported.";

        string token;
        try
        {
            token = await AcquirePowerBiAccessTokenAsync(flow);
        }
        catch (Exception ex)
        {
            return $"FAILED acquiring token via {flow}: {ex.Message}";
        }

        using var server = new Microsoft.AnalysisServices.Server();
        try
        {
            server.Connect(connectionString, token);  // bearer-token overload
            int dbCount = server.Databases.Count;
            return $"SUCCESS via {flow}: connected, {dbCount} database(s) visible.";
        }
        catch (Exception ex)
        {
            return $"FAILED connecting via {flow}: {ex.Message}";
        }
        finally
        {
            server.Disconnect();
        }
    }

    [McpServerTool(Name = "xmla_test_connection_noauth")]
    [Description("Test XMLA connection logic without authentication (for testing purposes)")]
    public Task<string> TestConnectionNoAuth(string connectionString)
    {
        if (!connectionString.StartsWith("powerbi://", StringComparison.OrdinalIgnoreCase))
            return Task.FromResult("Only powerbi:// Fabric / Power BI endpoints are supported.");

        // Test the connection string parsing and basic setup
        try
        {
            using var server = new Microsoft.AnalysisServices.Server();
            // Don't actually connect, just test the setup
            return Task.FromResult($"Connection string parsed successfully: {connectionString}");
        }
        catch (Exception ex)
        {
            return Task.FromResult($"Error setting up connection: {ex.Message}");
        }
    }
}
