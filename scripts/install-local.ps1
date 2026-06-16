<#
.SYNOPSIS
  Installs a locally-built revit-mcp into the per-user Revit Addins folder for quick testing.
.EXAMPLE
  ./scripts/install-local.ps1 -Version 2025
#>
param(
    [Parameter(Mandatory)]
    [ValidateSet("2025", "2026", "2027")]
    [string]$Version,

    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"

$tfm = if ($Version -eq "2027") { "net10.0-windows" } else { "net8.0-windows" }
$root = Split-Path $PSScriptRoot -Parent
$project = Join-Path $root "src\hosts\revit\Algomim.Revit.Mcp.$Version\Algomim.Revit.Mcp.$Version.csproj"
$output = Join-Path $root "src\hosts\revit\Algomim.Revit.Mcp.$Version\bin\$Configuration\$tfm"
$dllSource = Join-Path $output "Algomim.Revit.Mcp.$Version.dll"
$addinSource = Join-Path $output "Algomim.Revit.Mcp.$Version.addin"

if (-not (Test-Path $project -PathType Leaf)) {
    throw "Project file not found: $project"
}

if (-not (Test-Path $output)) {
    throw "Build output not found: $output`nRun: dotnet build $project -c $Configuration"
}

if (-not (Test-Path $dllSource -PathType Leaf)) {
    throw "Plugin DLL not found: $dllSource`nRun: dotnet build $project -c $Configuration"
}

if (-not (Test-Path $addinSource -PathType Leaf)) {
    throw "Addin manifest not found: $addinSource`nRun: dotnet build $project -c $Configuration"
}

if ([string]::IsNullOrWhiteSpace($env:APPDATA)) {
    throw "APPDATA is not set; cannot resolve the per-user Revit Addins folder."
}

$addins = Join-Path $env:APPDATA "Autodesk\Revit\Addins\$Version"
$pluginDir = Join-Path $addins "revit-mcp"

New-Item -ItemType Directory -Force -Path $pluginDir | Out-Null

# Plugin DLL + its dependencies go in the subfolder; the .addin (referencing revit-mcp\...dll) sits at the Addins root.
Get-ChildItem -LiteralPath $pluginDir -Force | Remove-Item -Recurse -Force
Copy-Item -Path (Join-Path $output "*") -Destination $pluginDir -Recurse -Force -Exclude "*.addin"
Copy-Item -Path $addinSource -Destination $addins -Force

Write-Host "Installed revit-mcp for Revit $Version -> $addins"
Write-Host "Restart Revit, then click Algomim > revit-mcp > Connect."
