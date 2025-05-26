using ModelContextProtocol.Server;
using System.ComponentModel;

namespace Tools.WrapperClasses;

[McpServerToolType]
public class XmlaInfoWrapper
{
    private readonly XmlaInfoTools _tools;

    public XmlaInfoWrapper()
    {
        _tools = new XmlaInfoTools();
    }

    [McpServerTool(Name = "xmla_list_databases")]
    [Description("Lists all databases on the XMLA endpoint")]
    public Task<string> ListDatabases(
        string connectionString,
        CancellationToken cancellationToken = default)
    {
        return _tools.ListDatabases(connectionString, cancellationToken);
    }

    [McpServerTool(Name = "xmla_test_connection")]
    [Description("Attempts to connect to an XMLA endpoint with the given auth flow (silent | interactive | devicecode) and returns a short status message")]
    public Task<string> TestConnection(
        string connectionString,
        string flow = "silent",
        CancellationToken cancellationToken = default)
    {
        return _tools.TestConnection(connectionString, flow, cancellationToken);
    }

    [McpServerTool(Name = "xmla_test_connection_noauth")]
    [Description("Test XMLA connection logic without authentication (for testing purposes)")]
    public Task<string> TestConnectionNoAuth(string connectionString)
    {
        return _tools.TestConnectionNoAuth(connectionString);
    }
}
