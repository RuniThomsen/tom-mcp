# TMDL Validation Tool

This tool integrates with Tabular Editor 2 CLI to perform model validation on TMDL files.

## Features

- **TOM Round-trip Check**: Loads the model into TOM and verifies it can be serialized and deserialized without issues
- **Best Practice Analysis**: Runs the Tabular Editor BPA rules against the model
- **Server-Sent Events**: Streams validation results line-by-line via SSE for real-time feedback

## Installation

1. Run `setup-te2.ps1` to prepare the installation directory
2. Download Tabular Editor 2 Portable from [GitHub releases](https://github.com/TabularEditor/TabularEditor/releases/latest)
3. Extract the downloaded ZIP to `./bin/te2/` directory

## Usage

The tool can be called via the MCP server interface:

```json
{
  "toolName": "tmdl_validate_model",
  "args": ["--path", "c:/path/to/model/definition", "--rules", "c:/optional/path/to/rules.json"]
}
```

- `--path`: Required. Path to the TMDL model folder or file
- `--rules`: Optional. Path to custom BPA rules file

## Direct Testing

You can test the tool directly via the HTTP API:

```
GET /test-validate
```

This will run the validation on a predefined model path and return results as server-sent events.
