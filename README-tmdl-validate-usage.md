# Using the TMDL Validation Tool

This guide provides instructions for using the `tmdl_validate_model` tool in the tom-mcp server.

## What This Tool Does

The `tmdl_validate_model` tool performs two types of validation on TMDL models:

1. **TOM Round-trip Validation**: Loads the model into the Tabular Object Model (TOM) and verifies it can be serialized/deserialized without issues.
2. **Best Practice Analysis (BPA)**: Uses Tabular Editor 2 CLI to scan the model for best practice violations.

## Prerequisites

1. Tabular Editor 2 must be installed at: `C:\Program Files (x86)\Tabular Editor\TabularEditor.exe`
2. Run `setup-te2.ps1` to verify the installation is correct

## Testing the Tool

### Option 1: Using the Direct Endpoint (Recommended)

The simplest way to test this tool is using the direct endpoint:

```
GET http://localhost:5000/test-validate
```

This will run the validation on a predefined model path and return results as server-sent events.

Curl example:
```
curl -N http://localhost:5000/test-validate
```

PowerShell example:
```powershell
Invoke-WebRequest -Uri "http://localhost:5000/test-validate" -Method Get
```

### Option 2: Running the Tool Directly

You can also run the tool directly from the command line:

```powershell
# Start the server first (in a separate window)
pwsh -ExecutionPolicy Bypass -File c:\repos\Start-TomMcp.ps1

# Then test the tool using the test script
pwsh -ExecutionPolicy Bypass -File c:\repos\test-tmdl-validate.ps1 -ModelPath "c:\path\to\your\model.tmdl"
```

## Common Issues

1. **Tabular Editor Not Found**: Make sure Tabular Editor 2 is installed at the expected path.
2. **BPA Errors**: The Best Practice Analyzer may fail if:
   - The model has structural issues
   - No BPA rules file is specified (default rules will be used)
   - Tabular Editor fails to process the model

## Using Custom BPA Rules

You can provide a custom BPA rules file to the tool:

```powershell
pwsh -ExecutionPolicy Bypass -File c:\repos\test-tmdl-validate.ps1 -ModelPath "c:\path\to\model.tmdl" -RulesPath "c:\path\to\rules.json"
```

## Output Format

The tool provides SSE (Server-Sent Events) output with validation results like:

```
data: Starting validation of model at [timestamp]
data: Performing TOM round-trip validation...
data: ✅ TMDL structure OK - loaded X tables
data: ✅ Round-trip validation successful
data: Running Best Practice Analyzer...
data: ... [BPA results here]
```

## Note on MCP JSON-RPC Protocol

While the tool is registered with the MCP server, calling it through the standard MCP JSON-RPC protocol at `/mcptools` requires specific client-side integration. The direct endpoint at `/test-validate` provides easier testing access.
