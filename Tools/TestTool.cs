using System.ComponentModel;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace Tools;

[McpServerToolType]
public static class TestTool
{
    [McpServerTool(Name = "test_echo")]
    [Description("Simple test echo function")]
    public static string Echo(string message)
    {
        return $"Echo: {message}";
    }
}
