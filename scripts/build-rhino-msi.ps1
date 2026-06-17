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
$msiPath = Join-Path $dist "rhino-mcp-$version.msi"
$utilExtensionRoot = Join-Path $env:USERPROFILE ".wix\extensions\WixToolset.Util.wixext"
$uiExtensionRoot = Join-Path $env:USERPROFILE ".wix\extensions\WixToolset.UI.wixext"

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

$utilExtension = Get-ChildItem -LiteralPath $utilExtensionRoot -Recurse -Filter "WixToolset.Util.wixext.dll" -ErrorAction SilentlyContinue |
    Sort-Object FullName -Descending |
    Select-Object -First 1

if ($null -eq $utilExtension) {
    throw "WiX Util extension was not found. Run: wix extension add WixToolset.Util.wixext --global"
}

$uiExtension = Get-ChildItem -LiteralPath $uiExtensionRoot -Recurse -Filter "WixToolset.UI.wixext.dll" -ErrorAction SilentlyContinue |
    Sort-Object FullName -Descending |
    Select-Object -First 1

if ($null -eq $uiExtension) {
    throw "WiX UI extension was not found. Run: wix extension add WixToolset.UI.wixext --global"
}

$wixVersion = (& wix --version).Trim()
$wixArgs = @()
if ($wixVersion -match '^(\d+)\.' -and [int]$Matches[1] -ge 7) {
    $wixArgs += @("-acceptEula", "wix7")
}

& wix build @wixArgs -arch x64 -ext $utilExtension.FullName -ext $uiExtension.FullName -d "RhinoYakPackage=$yakPath" (Join-Path $repoRoot "installer\rhino-mcp.wxs") -o $msiPath
if ($LASTEXITCODE -ne 0) {
    throw "Rhino MSI build failed."
}

$hash = Get-FileHash $msiPath -Algorithm SHA256
"$($hash.Hash.ToLowerInvariant())  $(Split-Path $msiPath -Leaf)" | Set-Content -Encoding ascii "$msiPath.sha256"

Write-Host "Built Rhino Yak-backed MSI:"
Write-Host "  $msiPath"
Write-Host "  $msiPath.sha256"
