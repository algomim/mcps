[CmdletBinding()]
param(
    [string]$Configuration = "Release"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$testsRoot = Join-Path $repoRoot "tests"

if (-not (Test-Path $testsRoot -PathType Container)) {
    throw "Tests directory was not found: $testsRoot"
}

$projects = Get-ChildItem -LiteralPath $testsRoot -Recurse -Filter "*.csproj" |
    Where-Object {
        $_.FullName -notmatch '\\(bin|obj)\\' -and
        $_.BaseName.EndsWith(".Tests", [System.StringComparison]::Ordinal)
    } |
    Sort-Object FullName

if ($projects.Count -eq 0) {
    throw "No host-neutral test projects were found under $testsRoot."
}

Write-Host "Running $($projects.Count) host-neutral test project(s)."

foreach ($project in $projects) {
    $rootPath = $repoRoot.Path.TrimEnd('\')
    $relative = $project.FullName
    if ($relative.StartsWith($rootPath, [System.StringComparison]::OrdinalIgnoreCase)) {
        $relative = $relative.Substring($rootPath.Length).TrimStart('\')
    }

    $relative = $relative.Replace('\', '/')
    Write-Host "Testing $relative"
    & dotnet test $project.FullName -c $Configuration
    if ($LASTEXITCODE -ne 0) {
        throw "Test project failed: $relative"
    }
}
