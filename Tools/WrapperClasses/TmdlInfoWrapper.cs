using ModelContextProtocol.Server;
using System.ComponentModel;

namespace Tools.WrapperClasses;

/// <summary>
/// Instance-based wrapper for TmdlInfoTools to fix JSON-RPC compatibility.
/// The MCP C# SDK has issues with static classes in JSON-RPC protocol.
/// </summary>
[McpServerToolType]
public class TmdlInfoWrapper
{    [McpServerTool(Name = "tmdl_list_tables")]
    [Description("Lists all tables in a TMDL file")]
    public string ListTables(
        [Description("Path to the TMDL model folder or file")]
        string tmdlPath)
    {
        // Delegate to the existing static implementation
        return TmdlInfoTools.ListTables(tmdlPath, CancellationToken.None);
    }

    [McpServerTool(Name = "tmdl_list_measures")]
    [Description("Lists all measures in a specific table inside a TMDL file")]
    public string ListMeasures(
        [Description("Path to the TMDL model folder or file")]        string tmdlPath,
        [Description("Name of the table to list measures from")]
        string table)
    {
        // Delegate to the existing static implementation
        return TmdlInfoTools.ListMeasures(tmdlPath, table, CancellationToken.None);
    }
}
