using ModelContextProtocol.Server;
using ModelContextProtocol.Protocol;
using ModelContextProtocol;
using System.ComponentModel;

namespace Tools.WrapperClasses;

/// <summary>
/// Instance-based wrapper for TmdlLoadModelTool to fix JSON-RPC compatibility.
/// The MCP C# SDK has issues with static classes in JSON-RPC protocol.
/// </summary>
[McpServerToolType]
public class TmdlLoadModelToolWrapper
{
    [McpServerTool(Name = "tmdl_load_model")]
    [Description("Load an existing TMDL model and return a summary of its contents")]
    public Task<string> LoadModel(
        [Description("Path to the TMDL file to load")]
        string inputPath,
        [Description("Progress reporter for streaming load results")]
        IProgress<ProgressNotificationValue>? progress = null,
        CancellationToken cancellationToken = default)
    {
        // Delegate to the existing static implementation
        return TmdlLoadModelTool.LoadModel(inputPath, progress, cancellationToken);
    }
}