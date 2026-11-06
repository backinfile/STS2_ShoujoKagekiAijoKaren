param(
    [ValidateSet('stable','beta')][string]$Environment = 'beta',
    [string]$GameDirectory = '',
    [int]$McpPort = 0,
    [switch]$AllowPlacementFailure
)
$ErrorActionPreference = 'Stop'
$project = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
if (-not $GameDirectory) { $GameDirectory = Join-Path $project "artifacts/dev-game/$Environment/game" }
$GameDirectory = [IO.Path]::GetFullPath($GameDirectory)
if (-not $McpPort) { $McpPort = if ($Environment -eq 'stable') {15627} else {15628} }
if (Get-NetTCPConnection -LocalPort $McpPort -State Listen -ErrorAction SilentlyContinue) { throw "MCP port $McpPort is occupied" }
$variant = if ($Environment -eq 'stable') {'Stable'} else {'Beta'}
$output = Join-Path $project "artifacts/transform-plugin/$Environment"
$plugin = Join-Path $GameDirectory 'mods/KarenPromiseTransformTests'
if (Test-Path -LiteralPath $plugin) { throw "Fixture directory already exists: $plugin" }
& dotnet build (Join-Path $PSScriptRoot 'promise-transform/KarenPromiseTransformTests.csproj') "-p:Variant=$variant" -o $output --nologo -v:q
if ($LASTEXITCODE -ne 0) { throw 'Transform fixture build failed' }
New-Item -ItemType Directory -Path $plugin | Out-Null
try {
    Copy-Item -LiteralPath (Join-Path $output 'KarenPromiseTransformTests.dll') -Destination $plugin
    @{
        id='KarenPromiseTransformTests';name='Promise transform test';author='Karen tests';version='1.0.0';
        has_dll=$true;has_pck=$false;affects_gameplay=$false;dependencies=@(@{id='ShoujoKagekiAijoKaren'})
    } | ConvertTo-Json -Depth 4 | Set-Content -Encoding UTF8 (Join-Path $plugin 'KarenPromiseTransformTests.json')
    & (Join-Path $PSScriptRoot 'test_promise_visuals.ps1') -Environment $Environment -GameDirectory $GameDirectory -McpPort $McpPort -TransformChecks -AllowPlacementFailure:$AllowPlacementFailure
    if (-not $?) { throw 'Promise transform regression failed' }
} finally {
    # Delete only the two files installed here; never recursively remove game paths.
    foreach ($file in @('KarenPromiseTransformTests.dll','KarenPromiseTransformTests.json')) {
        $path = Join-Path $plugin $file
        if (Test-Path -LiteralPath $path) { Remove-Item -LiteralPath $path -Force }
    }
    if (@(Get-ChildItem -LiteralPath $plugin -Force).Count -eq 0) { Remove-Item -LiteralPath $plugin }
}
