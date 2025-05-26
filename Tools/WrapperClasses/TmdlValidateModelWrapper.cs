using ModelContextProtocol.Server;
using ModelContextProtocol.Protocol;
using ModelContextProtocol;
using System.ComponentModel;

namespace Tools.WrapperClasses;

/// <summary>
/// Instance-based wrapper for TmdlValidateModelTools to fix JSON-RPC compatibility.
/// The MCP C# SDK has issues with static classes in JSON-RPC protocol.
/// </summary>
[McpServerToolType]
public class TmdlValidateModelWrapper
{    [McpServerTool(Name = "tmdl_validate_model")]
    [Description("Validate a TMDL model via Tabular Editor 2 (round-trip + BPA)")]
    public async Task<string> ValidateModel(
        [Description("Path to the TMDL model folder or file")]
        string tmdlPath,
        [Description("Optional path to BPA rules file")]
        string? rulesPath = null)
    {
        // Delegate to the existing static implementation (without progress and cancellation token)
        return await TmdlValidateModelTools.ValidateModel(tmdlPath, rulesPath, null, CancellationToken.None);
    }
}
