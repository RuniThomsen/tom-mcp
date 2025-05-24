using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace tom_mcp.Tools
{
    /// <summary>
    /// Business Process Automation validation tool
    /// </summary>
    public class BusinessProcessAutomation
    {
        private readonly ILogger<BusinessProcessAutomation>? _logger;

        public BusinessProcessAutomation(ILogger<BusinessProcessAutomation>? logger = null)
        {
            _logger = logger;
        }

        /// <summary>
        /// Validates files against business process automation rules
        /// </summary>
        /// <param name="filePath">Path to the file to validate</param>
        /// <param name="ruleSet">Rule set to apply for validation (optional)</param>
        /// <returns>Validation result with detailed findings</returns>
        public async Task<string> ValidateAsync(string filePath, string? ruleSet = null)
        {
            try
            {
                _logger?.LogInformation("Starting BPA validation for file: {FilePath} with ruleset: {RuleSet}", filePath, ruleSet ?? "default");

                // Validate input parameters
                if (string.IsNullOrWhiteSpace(filePath))
                {
                    return "ERROR: File path is required and cannot be empty.";
                }

                // Check if file exists
                if (!File.Exists(filePath))
                {
                    return $"ERROR: File not found at path: {filePath}";
                }

                var fileInfo = new FileInfo(filePath);
                var results = new List<string>();
                
                results.Add($"BPA Validation Report for: {filePath}");
                results.Add($"File Size: {fileInfo.Length} bytes");
                results.Add($"Last Modified: {fileInfo.LastWriteTime}");
                results.Add($"Rule Set: {ruleSet ?? "default"}");
                results.Add("");

                // Read file content for analysis
                string content;
                try
                {
                    content = await File.ReadAllTextAsync(filePath);
                }
                catch (Exception ex)
                {
                    return $"ERROR: Unable to read file content: {ex.Message}";
                }

                // Perform validation checks based on file type
                var extension = Path.GetExtension(filePath).ToLowerInvariant();
                
                switch (extension)
                {
                    case ".tmdl":
                        results.AddRange(ValidateTmdlFile(content, ruleSet));
                        break;
                    case ".json":
                        results.AddRange(ValidateJsonFile(content, ruleSet));
                        break;
                    case ".xml":
                        results.AddRange(ValidateXmlFile(content, ruleSet));
                        break;
                    case ".cs":
                        results.AddRange(ValidateCSharpFile(content, ruleSet));
                        break;
                    default:
                        results.AddRange(ValidateGenericFile(content, ruleSet));
                        break;
                }

                results.Add("");
                results.Add("Validation completed successfully.");
                
                var result = string.Join(Environment.NewLine, results);
                _logger?.LogInformation("BPA validation completed for: {FilePath}", filePath);
                
                return result;
            }
            catch (Exception ex)
            {
                var errorMessage = $"ERROR: BPA validation failed: {ex.Message}";
                _logger?.LogError(ex, "BPA validation error for file: {FilePath}", filePath);
                return errorMessage;
            }
        }

        private List<string> ValidateTmdlFile(string content, string? ruleSet)
        {
            var findings = new List<string>();
            findings.Add("TMDL File Validation:");

            // Check for required sections
            if (!content.Contains("model"))
            {
                findings.Add("  WARNING: No 'model' definition found");
            }
            else
            {
                findings.Add("  ✓ Model definition found");
            }

            if (!content.Contains("table"))
            {
                findings.Add("  WARNING: No 'table' definitions found");
            }
            else
            {
                findings.Add("  ✓ Table definitions found");
            }

            // Check for common issues
            var lines = content.Split('\n');
            var lineNumber = 0;
            foreach (var line in lines)
            {
                lineNumber++;
                if (line.Trim().EndsWith(",") && lineNumber == lines.Length)
                {
                    findings.Add($"  WARNING: Trailing comma on last line ({lineNumber})");
                }
            }

            // Rule set specific validations
            if (ruleSet == "strict")
            {
                if (!content.Contains("annotation"))
                {
                    findings.Add("  WARNING: No annotations found (strict mode)");
                }
            }

            return findings;
        }

        private List<string> ValidateJsonFile(string content, string? ruleSet)
        {
            var findings = new List<string>();
            findings.Add("JSON File Validation:");

            try
            {
                System.Text.Json.JsonDocument.Parse(content);
                findings.Add("  ✓ Valid JSON syntax");
            }
            catch (System.Text.Json.JsonException ex)
            {
                findings.Add($"  ERROR: Invalid JSON syntax: {ex.Message}");
            }

            return findings;
        }

        private List<string> ValidateXmlFile(string content, string? ruleSet)
        {
            var findings = new List<string>();
            findings.Add("XML File Validation:");

            try
            {
                var xmlDoc = new System.Xml.XmlDocument();
                xmlDoc.LoadXml(content);
                findings.Add("  ✓ Valid XML syntax");
            }
            catch (System.Xml.XmlException ex)
            {
                findings.Add($"  ERROR: Invalid XML syntax: {ex.Message}");
            }

            return findings;
        }

        private List<string> ValidateCSharpFile(string content, string? ruleSet)
        {
            var findings = new List<string>();
            findings.Add("C# File Validation:");

            // Basic syntax checks
            if (!content.Contains("namespace") && !content.Contains("class"))
            {
                findings.Add("  WARNING: No namespace or class declarations found");
            }

            if (content.Contains("TODO") || content.Contains("HACK") || content.Contains("FIXME"))
            {
                findings.Add("  WARNING: Code contains TODO/HACK/FIXME comments");
            }

            return findings;
        }

        private List<string> ValidateGenericFile(string content, string? ruleSet)
        {
            var findings = new List<string>();
            findings.Add("Generic File Validation:");

            findings.Add($"  ✓ File is readable ({content.Length} characters)");
            
            var lineCount = content.Split('\n').Length;
            findings.Add($"  ✓ Line count: {lineCount}");

            if (content.Length == 0)
            {
                findings.Add("  WARNING: File is empty");
            }

            return findings;
        }
    }
}
