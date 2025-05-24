# TOM-MCP (Tabular Object Model - Model Context Protocol)

A Model Context Protocol (MCP) server that provides tools for reading and editing Power BI semantic models via the Tabular Object Model (TOM) v19+. This server exposes TMDL manipulation capabilities to Visual Studio Code Agent mode and other MCP-compatible clients.

## Overview

TOM-MCP bridges the gap between Power BI's Tabular Object Model and modern AI-assisted development tools by implementing the Model Context Protocol. It enables AI assistants to understand, analyze, and modify Power BI semantic models programmatically.

## Features

### 📊 Model Analysis
- **List Tables**: Enumerate all tables in a TMDL model
- **List Measures**: List all measures in a specific table
- **Detect Unused Columns**: Find columns not referenced by any measure or calculated column

### ✏️ Model Manipulation
- **Add/Update Measures**: Create new measures or modify existing ones
- **Rename Objects**: Rename tables, columns, or measures with automatic DAX reference updates
- **Format Model**: Deterministically format TMDL files for consistent Git diffs

### ✅ Validation
- **Validate Model**: Comprehensive model validation using Tabular Editor 2
  - Structural validation (TOM round-trip)
  - Best Practice Analyzer (BPA) checks
  - Progress streaming for long-running validations

### 🔧 Utilities
- **Diff TMDL**: Stream Git-style unified diffs between TMDL files
- **List Tools**: Discover available tools in the MCP server

## Installation

### Prerequisites
- .NET 8.0 or later
- Tabular Editor 2 (for validation features)
- Git (for diff functionality)

### Setup

1. Clone the repository:
```bash
git clone https://github.com/yourusername/tom-mcp.git
cd tom-mcp
```

2. Build the project:
```bash
dotnet build
```

3. Configure Tabular Editor 2 (for validation):
```bash
# Run the setup script
.\setup-te2.ps1
```

## Configuration

### MCP Configuration (for VS Code)

Add to your `.vscode/mcp.json`:

```json
{
  "servers": {
    "tom-mcp": {
      "command": "dotnet",
      "args": [
        "C:\\repos\\tom-mcp\\bin\\Release\\net9.0\\tom-mcp.dll"
      ],
      "env": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      },
      "type": "stdio"
    }
  }
}
```

## Available Tools

### Model Information
- `tmdl_list_tables` - Lists all tables in a TMDL model
- `tmdl_list_measures` - Lists measures in a specific table
- `tmdl_detect_unused_columns` - Finds unreferenced columns

### Model Modification
- `addMeasure` - Adds or updates a measure
- `tmdl_rename_object` - Renames objects and updates references
- `tmdl_format_model` - Formats model for consistent version control

### Validation
- `tmdl_validate_model` - Comprehensive model validation with BPA

### Utilities
- `diff_tmdl` - Compares two TMDL files
- `list_tools` - Lists all available tools

## Usage Examples

### Validating a Model
```javascript
// In VS Code with GitHub Copilot
use #tmdl_validate_model to validate the model at "C:/MyModel/definition"
```

### Adding a Measure
```javascript
use #addMeasure to add a measure named "Total Sales" with formula "SUM(Sales[Amount])" to table "Measures" in "C:/MyModel/definition"
```

### Renaming a Column
```javascript
use #tmdl_rename_object to rename column "CustomerID" to "Customer ID" in table "Sales" at "C:/MyModel/definition"
```

## Architecture

- **Transport**: Supports both stdio (for VS Code) and HTTP modes
- **Framework**: Built on MCP SDK 0.3.1-alpha
- **Dependencies**: 
  - Microsoft.AnalysisServices.Tabular (TOM)
  - Tabular Editor 2 (for validation)

## Development

### Running in Debug Mode
```bash
dotnet run -- --stdio
```

### Running Tests
```bash
# HTTP test endpoints available at:
# http://localhost:5000/test-echo
# http://localhost:5000/test-tmdl
# http://localhost:5000/test-validate
```

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Submit a pull request

## License

[Your License Here]

## Acknowledgments

- Built with [Model Context Protocol](https://github.com/modelcontextprotocol)
- Powered by [Tabular Object Model](https://docs.microsoft.com/en-us/analysis-services/tom/introduction-to-the-tabular-object-model-tom-in-analysis-services-amo)
- Validation by [Tabular Editor](https://tabulareditor.com/)
