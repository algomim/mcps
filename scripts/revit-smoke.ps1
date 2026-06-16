<#
.SYNOPSIS
  Runs a smoke check against a live revit-mcp HTTP endpoint.
.EXAMPLE
  ./scripts/revit-smoke.ps1 -BaseUrl http://127.0.0.1:48884
.EXAMPLE
  ./scripts/revit-smoke.ps1 -BaseUrl http://127.0.0.1:48884 -WritableElementId 12345 -ExportViewId 67890 -ExportFolder C:\tmp\revit-mcp-export
#>
param(
    [Parameter(Mandatory)]
    [string]$BaseUrl,

    [long]$WritableElementId,
    [long]$ExportViewId,
    [string]$ExportFolder
)

$ErrorActionPreference = "Stop"

$base = $BaseUrl.TrimEnd("/")
$mcpUrl = "$base/mcp"
$headers = @{
    Accept = "application/json"
    "Content-Type" = "application/json"
}
$nextId = 1

function Invoke-Mcp {
    param(
        [Parameter(Mandatory)][string]$Method,
        [object]$Params = @{}
    )

    $script:nextId += 1
    $body = @{
        jsonrpc = "2.0"
        id = $script:nextId
        method = $Method
        params = $Params
    } | ConvertTo-Json -Depth 20

    $response = Invoke-RestMethod -Method Post -Uri $mcpUrl -Headers $headers -Body $body
    if ($response.error) {
        throw "MCP $Method failed: $($response.error.message)"
    }

    return $response.result
}

function Invoke-Tool {
    param(
        [Parameter(Mandatory)][string]$Name,
        [object]$Arguments = @{}
    )

    return Invoke-Mcp -Method "tools/call" -Params @{
        name = $Name
        arguments = $Arguments
    }
}

$health = Invoke-RestMethod -Method Get -Uri "$base/health"
if ($health.status -ne "ok") {
    throw "Health check failed."
}

Invoke-Mcp -Method "initialize" -Params @{
    protocolVersion = "2025-06-18"
    capabilities = @{}
    clientInfo = @{ name = "revit-mcp-smoke"; version = "1.0.0" }
} | Out-Null

$tools = (Invoke-Mcp -Method "tools/list").tools
$toolNames = @($tools | ForEach-Object { $_.name })
$required = @(
    "document_get_info",
    "view_get_active",
    "selection_get",
    "model_list_warnings",
    "document_get_units",
    "element_move",
    "export_pdf"
)

foreach ($name in $required) {
    if ($toolNames -notcontains $name) {
        throw "Required smoke tool is missing: $name"
    }
}

Invoke-Tool -Name "document_get_info" | Out-Null
Invoke-Tool -Name "view_get_active" | Out-Null
Invoke-Tool -Name "selection_get" | Out-Null
Invoke-Tool -Name "model_list_warnings" | Out-Null
Invoke-Tool -Name "document_get_units" | Out-Null

if ($WritableElementId -gt 0) {
    Invoke-Tool -Name "element_move" -Arguments @{
        elementIds = @($WritableElementId)
        x = @(0)
        y = @(0)
        z = @(0)
    } | Out-Null
}

if ($ExportViewId -gt 0) {
    if ([string]::IsNullOrWhiteSpace($ExportFolder)) {
        throw "-ExportFolder is required when -ExportViewId is provided."
    }

    Invoke-Tool -Name "export_pdf" -Arguments @{
        fileNames = @("revit-mcp-smoke")
        viewIds = @($ExportViewId)
        folderPath = $ExportFolder
        combine = $true
    } | Out-Null
}

Write-Host "revit-mcp smoke passed: $base"
