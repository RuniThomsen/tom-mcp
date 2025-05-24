using System.ComponentModel;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using tom_mcp.Tools;

namespace Tools.WrapperClasses
{    /// <summary>
    /// MCP wrapper for Business Process Automation validation
    /// </summary>
    [McpServerToolType]
    public class BpaValidateWrapper
    {
        private readonly BusinessProcessAutomation _bpaValidator;
        private readonly ILogger<BpaValidateWrapper>? _logger;

        public BpaValidateWrapper(ILogger<BpaValidateWrapper>? logger = null)
        {
            _logger = logger;
            _bpaValidator = new BusinessProcessAutomation(null); // BPA has its own logger setup
        }

        /// <summary>
        /// Validates a file using Business Process Automation rules
        /// </summary>
        /// <param name="filePath">Path to the file to validate</param>
        /// <param name="ruleSet">Optional rule set to apply (default, strict, etc.)</param>
        /// <returns>Validation results and findings</returns>
        [McpServerTool(Name = "bpa_validate")]
        [Description("Validates a file using Business Process Automation (BPA) rules. Checks for compliance with business process standards and identifies potential issues.")]
        public async Task<string> BpaValidateAsync(
            [Description("Path to the file to validate")] string filePath,
            [Description("Rule set to apply for validation (optional: default, strict)")] string? ruleSet = null)
        {
            _logger?.LogInformation("BPA validation requested for: {FilePath} with ruleset: {RuleSet}", filePath, ruleSet ?? "default");
            
            return await _bpaValidator.ValidateAsync(filePath, ruleSet);
        }
    }
}
