[CmdletBinding()]
param(
    [string]$Configuration = "Release"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")

$versionDocument = New-Object System.Xml.XmlDocument
$versionDocument.Load((Join-Path $repoRoot "Directory.Build.props"))
$versionNode = $versionDocument.SelectSingleNode("/Project/PropertyGroup/Version")
if ($null -eq $versionNode -or [string]::IsNullOrWhiteSpace($versionNode.InnerText)) {
    throw "Directory.Build.props does not contain Project/PropertyGroup/Version."
}

$version = $versionNode.InnerText.Trim()
$dist = Join-Path $repoRoot "dist"
$msiPath = Join-Path $dist "rhino-mcp-$version-yak-local.msi"
$utilExtension = Join-Path $env:USERPROFILE ".wix\extensions\WixToolset.Util.wixext\5.0.2\wixext5\WixToolset.Util.wixext.dll"

& powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot "build-rhino-yak.ps1") -Configuration $Configuration
if ($LASTEXITCODE -ne 0) {
    throw "Rhino Yak package build failed."
}

$yakPackage = Get-ChildItem -LiteralPath $dist -Filter "algomim-rhino-mcp-$version-rh8_*-win.yak" |
    Sort-Object LastWriteTime -Descending |
    Select-Object -First 1

if ($null -eq $yakPackage) {
    throw "Expected Rhino Yak package was not found under '$dist' for version $version."
}

$yakPath = $yakPackage.FullName

if (-not (Test-Path $utilExtension -PathType Leaf)) {
    throw "WiX Util extension was not found. Run: wix extension add WixToolset.Util.wixext/5.0.2 --global"
}

& wix build -arch x64 -ext $utilExtension -d "RhinoYakPackage=$yakPath" (Join-Path $repoRoot "installer\rhino-mcp.wxs") -o $msiPath
if ($LASTEXITCODE -ne 0) {
    throw "Rhino MSI build failed."
}

$hash = Get-FileHash $msiPath -Algorithm SHA256
"$($hash.Hash.ToLowerInvariant())  $(Split-Path $msiPath -Leaf)" | Set-Content -Encoding ascii "$msiPath.sha256"

Write-Host "Built Rhino Yak-backed MSI:"
Write-Host "  $msiPath"
Write-Host "  $msiPath.sha256"
