# Setup script for tmdl_validate_model tool
# This script checks for Tabular Editor 2 installation and verifies it's accessible

# Check if Tabular Editor is installed in the expected location
$teExePath = "C:\Program Files (x86)\Tabular Editor\TabularEditor.exe"
if (Test-Path $teExePath) {
    Write-Host "✅ Tabular Editor 2 detected at $teExePath"
    exit 0
}

Write-Host @"
⚠️ Tabular Editor 2 is not installed in the expected location:
   $teExePath

To enable the 'tmdl_validate_model' tool, please:

1. Download and install Tabular Editor 2 from:
   https://github.com/TabularEditor/TabularEditor/releases/latest

2. Install it to the default location:
   C:\Program Files (x86)\Tabular Editor\

Once completed, the 'tmdl_validate_model' tool will function properly.
"@

# Create a README file with installation instructions
$readmePath = Join-Path $PSScriptRoot "TABULAR-EDITOR-README.txt"
@"
TABULAR EDITOR 2 INSTALLATION INSTRUCTIONS

The tmdl_validate_model tool requires Tabular Editor 2 to function.

Please install Tabular Editor 2 from:
https://github.com/TabularEditor/TabularEditor/releases/latest

Install it to the default location:
C:\Program Files (x86)\Tabular Editor\TabularEditor.exe

Once complete, the tmdl_validate_model tool will be able to perform:
- TOM round-trip validation
- Best practice analyzer scans
"@ | Out-File -FilePath $readmePath -Encoding utf8

Write-Host "Created instructions file at $readmePath"
