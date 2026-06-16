[CmdletBinding()]
param(
    [string]$Version,
    [switch]$Check
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")

function Normalize-Version {
    param([Parameter(Mandatory = $true)][string]$Value)

    $normalized = $Value.Trim()
    if ($normalized.StartsWith("v", [System.StringComparison]::OrdinalIgnoreCase)) {
        $normalized = $normalized.Substring(1)
    }

    if ($normalized -notmatch '^\d+\.\d+\.\d+$') {
        throw "Version must be SemVer core form X.Y.Z, got '$Value'."
    }

    return $normalized
}

function Get-Root-Version {
    $path = Join-Path $repoRoot "Directory.Build.props"
    $document = New-Object System.Xml.XmlDocument
    $document.PreserveWhitespace = $true
    $document.Load($path)

    $node = $document.SelectSingleNode("/Project/PropertyGroup/Version")
    if ($null -eq $node -or [string]::IsNullOrWhiteSpace($node.InnerText)) {
        throw "Directory.Build.props does not contain Project/PropertyGroup/Version."
    }

    return Normalize-Version $node.InnerText
}

if ([string]::IsNullOrWhiteSpace($Version)) {
    $Version = Get-Root-Version
}
else {
    $Version = Normalize-Version $Version
}

$msiVersion = "$Version.0"
$issues = New-Object System.Collections.Generic.List[string]
$updated = New-Object System.Collections.Generic.List[string]

function Get-Display-Path {
    param([Parameter(Mandatory = $true)][string]$Path)

    $root = $repoRoot.Path.TrimEnd('\')
    $resolved = (Resolve-Path $Path).Path
    if ($resolved.StartsWith($root, [System.StringComparison]::OrdinalIgnoreCase)) {
        return $resolved.Substring($root.Length).TrimStart('\').Replace('\', '/')
    }

    return $resolved.Replace('\', '/')
}

function Select-Node {
    param(
        [Parameter(Mandatory = $true)][System.Xml.XmlDocument]$Document,
        [Parameter(Mandatory = $true)][string]$XPath
    )

    if ([string]::IsNullOrWhiteSpace($Document.DocumentElement.NamespaceURI)) {
        return $Document.SelectSingleNode($XPath)
    }

    $namespaceManager = New-Object System.Xml.XmlNamespaceManager($Document.NameTable)
    $namespaceManager.AddNamespace("w", $Document.DocumentElement.NamespaceURI)
    return $Document.SelectSingleNode($XPath, $namespaceManager)
}

function Save-Xml {
    param(
        [Parameter(Mandatory = $true)][System.Xml.XmlDocument]$Document,
        [Parameter(Mandatory = $true)][string]$Path
    )

    $settings = New-Object System.Xml.XmlWriterSettings
    $settings.Encoding = New-Object System.Text.UTF8Encoding($false)
    $settings.Indent = $false
    $settings.NewLineChars = "`r`n"

    $writer = [System.Xml.XmlWriter]::Create($Path, $settings)
    try {
        $Document.Save($writer)
    }
    finally {
        $writer.Dispose()
    }
}

function Assert-Or-Update-Element {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$XPath,
        [Parameter(Mandatory = $true)][string]$Expected
    )

    $fullPath = Join-Path $repoRoot $Path
    $document = New-Object System.Xml.XmlDocument
    $document.PreserveWhitespace = $true
    $document.Load($fullPath)

    $node = Select-Node $document $XPath
    if ($null -eq $node) {
        throw "Could not find '$XPath' in '$Path'."
    }

    if ($node.InnerText -eq $Expected) {
        return
    }

    $actual = $node.InnerText
    if ($Check) {
        $issues.Add("${Path}: expected '$Expected', found '$actual'.")
        return
    }

    $node.InnerText = $Expected
    Save-Xml $document $fullPath
    $updated.Add($Path)
}

function Assert-Or-Update-Attribute {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$XPath,
        [Parameter(Mandatory = $true)][string]$Attribute,
        [Parameter(Mandatory = $true)][string]$Expected
    )

    $fullPath = Join-Path $repoRoot $Path
    $document = New-Object System.Xml.XmlDocument
    $document.PreserveWhitespace = $true
    $document.Load($fullPath)

    $node = Select-Node $document $XPath
    if ($null -eq $node) {
        throw "Could not find '$XPath' in '$Path'."
    }

    $actual = $node.GetAttribute($Attribute)
    if ($actual -eq $Expected) {
        return
    }

    if ($Check) {
        $issues.Add("${Path}: expected @$Attribute='$Expected', found '$actual'.")
        return
    }

    $node.SetAttribute($Attribute, $Expected)
    Save-Xml $document $fullPath
    $updated.Add($Path)
}

Assert-Or-Update-Element "Directory.Build.props" "/Project/PropertyGroup/Version" $Version

Assert-Or-Update-Attribute "installer/revit-mcp.wxs" "/w:Wix/w:Package" "Version" $msiVersion
Assert-Or-Update-Attribute "installer/autocad-mcp.wxs" "/w:Wix/w:Package" "Version" $msiVersion

$autoCadManifestPaths = @(
    "installer/hosts/autocad/PackageContents.xml",
    "src/hosts/autocad/Algomim.AutoCad.Mcp.2025/PackageContents.xml",
    "src/hosts/autocad/Algomim.AutoCad.Mcp.2026/PackageContents.xml",
    "src/hosts/autocad/Algomim.AutoCad.Mcp.2027/PackageContents.xml"
)

foreach ($path in $autoCadManifestPaths) {
    Assert-Or-Update-Attribute $path "/ApplicationPackage" "AppVersion" $Version
}

if ($issues.Count -gt 0) {
    $message = "Version metadata is out of sync with ${Version}:`n" + ($issues -join "`n")
    throw $message
}

if ($updated.Count -gt 0) {
    $uniqueUpdates = $updated | Sort-Object -Unique
    Write-Host "Synced version metadata to ${Version}:"
    foreach ($path in $uniqueUpdates) {
        Write-Host "  - $(Get-Display-Path (Join-Path $repoRoot $path))"
    }
}
else {
    Write-Host "Version metadata is in sync with $Version."
}
