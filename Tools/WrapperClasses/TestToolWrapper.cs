using ModelContextProtocol.Server;
using System.ComponentModel;

namespace Tools.WrapperClasses;

/// <summary>
/// Instance-based wrapper for TestTool to fix JSON-RPC compatibility.
/// The MCP C# SDK has issues with static classes in JSON-RPC protocol.
/// </summary>
[McpServerToolType]
public class TestToolWrapper
{
    [McpServerTool(Name = "test_echo")]
    [Description("Simple test echo function")]
    public string Echo(
        [Description("Message to echo back")]
        string message)
    {
        // Delegate to the existing static implementation
        return TestTool.Echo(message);
    }
}
