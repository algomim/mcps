[CmdletBinding()]
param(
    [string]$Configuration = "Release",
    [string]$YakExe = "C:\Program Files\Rhino 8\System\Yak.exe"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$packageName = "algomim-rhino-mcp"
$project = Join-Path $repoRoot "src\hosts\rhino\Algomim.Rhino.Mcp.8\Algomim.Rhino.Mcp.8.csproj"
$output = Join-Path $repoRoot "src\hosts\rhino\Algomim.Rhino.Mcp.8\bin\$Configuration\net8.0-windows"
$staging = Join-Path $repoRoot "dist\rhino-yak"
$dist = Join-Path $repoRoot "dist"

if (-not (Test-Path $YakExe -PathType Leaf)) {
    throw "Yak.exe was not found at '$YakExe'. Install Rhino 8 or pass -YakExe."
}

$versionDocument = New-Object System.Xml.XmlDocument
$versionDocument.Load((Join-Path $repoRoot "Directory.Build.props"))
$versionNode = $versionDocument.SelectSingleNode("/Project/PropertyGroup/Version")
if ($null -eq $versionNode -or [string]::IsNullOrWhiteSpace($versionNode.InnerText)) {
    throw "Directory.Build.props does not contain Project/PropertyGroup/Version."
}

$version = $versionNode.InnerText.Trim()

& dotnet build $project -c $Configuration
if ($LASTEXITCODE -ne 0) {
    throw "Rhino plugin build failed."
}

if (-not (Test-Path (Join-Path $output "$packageName.rhp") -PathType Leaf)) {
    throw "Rhino plugin output was not found under '$output'."
}

New-Item -ItemType Directory -Force $dist | Out-Null
if (Test-Path $staging) {
    $resolvedStaging = (Resolve-Path $staging).Path
    $rootPath = $repoRoot.Path.TrimEnd('\')
    if (-not $resolvedStaging.StartsWith($rootPath, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to remove staging path outside the repository: $resolvedStaging"
    }

    Remove-Item -LiteralPath $staging -Recurse -Force
}

New-Item -ItemType Directory -Force $staging | Out-Null
$outputFiles = Get-ChildItem -LiteralPath $output -File |
    Where-Object {
        $_.Name -in @("$packageName.rhp", "$packageName.deps.json", "$packageName.runtimeconfig.json") -or
        $_.Extension.Equals(".dll", [System.StringComparison]::OrdinalIgnoreCase)
    }

foreach ($file in $outputFiles) {
    Copy-Item -LiteralPath $file.FullName -Destination $staging
}

$runtimeOutput = Join-Path $output "runtimes"
if (Test-Path $runtimeOutput -PathType Container) {
    Copy-Item -LiteralPath $runtimeOutput -Destination $staging -Recurse
}

$manifest = @"
---
name: $packageName
version: $version
authors:
- Algomim
description: Algomim MCP host skeleton for Rhino 8.
url: https://github.com/algomim/mcps
keywords:
- mcp
- rhino
- ai
- algomim
"@

Set-Content -LiteralPath (Join-Path $staging "manifest.yml") -Value $manifest -Encoding utf8

Push-Location $staging
try {
    & $YakExe build --platform win --version $version
    if ($LASTEXITCODE -ne 0) {
        throw "Yak package build failed."
    }
}
finally {
    Pop-Location
}

$package = Get-ChildItem -LiteralPath $staging -Filter "*.yak" | Sort-Object LastWriteTime -Descending | Select-Object -First 1
if ($null -eq $package) {
    throw "Yak package was not produced."
}

$target = Join-Path $dist $package.Name
Copy-Item -LiteralPath $package.FullName -Destination $target -Force

$hash = Get-FileHash $target -Algorithm SHA256
"$($hash.Hash.ToLowerInvariant())  $($package.Name)" | Set-Content -Encoding ascii "$target.sha256"

Write-Host "Built Rhino Yak package:"
Write-Host "  $target"
Write-Host "  $target.sha256"
