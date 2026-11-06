param(
    [string]$McpRepository = 'D:\Github\STS2_Mcp',
    [string]$GameRoot = '',
    [string]$OutputRoot = '',
    [switch]$LoopbackOnly,
    [switch]$SkipInstall
)

$ErrorActionPreference = 'Stop'
$project = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
if (-not $GameRoot) { $GameRoot = Join-Path $project 'artifacts\dev-game' }
if (-not $OutputRoot) { $OutputRoot = Join-Path $project 'artifacts' }
$McpRepository = [IO.Path]::GetFullPath($McpRepository)
$GameRoot = [IO.Path]::GetFullPath($GameRoot)
$OutputRoot = [IO.Path]::GetFullPath($OutputRoot)

foreach ($required in @('KarenSTS2MCP.csproj', 'mod_manifest.json',
        'McpMod.cs', 'McpMod.Actions.cs', 'McpMod.MultiplayerActions.cs')) {
    if (-not (Test-Path -LiteralPath (Join-Path $McpRepository $required) -PathType Leaf)) {
        throw "MCP source is incomplete: $required in $McpRepository"
    }
}
if (-not (Select-String -LiteralPath (Join-Path $McpRepository 'McpMod.Actions.cs') -Pattern 'ExecuteRunCommand' -Quiet -Encoding UTF8)) {
    throw 'This MCP source does not provide ExecuteRunCommand'
}

$revision = (& git -C $McpRepository rev-parse HEAD).Trim()
if ($LASTEXITCODE -ne 0) { throw "Cannot identify MCP source revision: $McpRepository" }
$dirty = @(git -C $McpRepository status --porcelain=v1).Count -gt 0
$sourceFiles = @(Get-ChildItem -LiteralPath $McpRepository -File -Filter '*.cs')
$buildRoot = Join-Path $OutputRoot ("mcp-build\" + [guid]::NewGuid().ToString('N'))

foreach ($branch in @('stable', 'beta')) {
    $game = Join-Path $GameRoot "$branch\game"
    $releaseInfo = Join-Path $game 'release_info.json'
    if (-not (Test-Path -LiteralPath $releaseInfo -PathType Leaf)) {
        throw "Missing game snapshot: $game"
    }
    $version = (Get-Content -LiteralPath $releaseInfo -Raw -Encoding UTF8 | ConvertFrom-Json).version
    $expected = if ($branch -eq 'stable') { 'v0.107.1' } else { 'v0.111.0' }
    if ($version -ne $expected) { throw "$branch snapshot is $version; expected $expected" }

    $sourceCopy = Join-Path $buildRoot $branch
    $output = Join-Path $sourceCopy 'out'
    $package = Join-Path $OutputRoot "mcp-$branch"
    New-Item -ItemType Directory -Path $sourceCopy,$package -Force | Out-Null
    foreach ($file in $sourceFiles) {
        Copy-Item -LiteralPath $file.FullName -Destination (Join-Path $sourceCopy $file.Name)
    }
    Copy-Item -LiteralPath (Join-Path $McpRepository 'KarenSTS2MCP.csproj') -Destination $sourceCopy

    # The upstream bridge exposes run_command for solo play. The batch tests
    # also need it for a networked Karen player; patch only this build copy.
    $multiplayerPath = Join-Path $sourceCopy 'McpMod.MultiplayerActions.cs'
    $multiplayer = [IO.File]::ReadAllText($multiplayerPath, [Text.Encoding]::UTF8)
    if ($multiplayer -notmatch '"run_command"\s*=>') {
        $marker = '            "end_turn" => ExecuteMultiplayerEndTurn(player),'
        if (-not $multiplayer.Contains($marker)) { throw 'MCP multiplayer action switch changed' }
        $multiplayer = $multiplayer.Replace($marker,
            "            `"run_command`" => ExecuteRunCommand(data),`n$marker")
        [IO.File]::WriteAllText($multiplayerPath, $multiplayer, [Text.UTF8Encoding]::new($false))
    }
    if ($LoopbackOnly) {
        Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'review-regression/LoopbackTransport.cs') `
            -Destination (Join-Path $sourceCopy 'KarenTestLoopbackTransport.cs')
    }
    $buildFiles = @(Get-ChildItem -LiteralPath $sourceCopy -File -Filter '*.cs')
    $hashLines = @($buildFiles | Sort-Object Name | ForEach-Object {
        $path = Join-Path $sourceCopy $_.Name
        "$($_.Name)=$((Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash)"
    })
    $hashLines += "KarenSTS2MCP.csproj=$((Get-FileHash -LiteralPath (Join-Path $sourceCopy 'KarenSTS2MCP.csproj') -Algorithm SHA256).Hash)"
    $hashLines += "mod_manifest.json=$((Get-FileHash -LiteralPath (Join-Path $McpRepository 'mod_manifest.json') -Algorithm SHA256).Hash)"
    $buildSourceHash = [Convert]::ToHexString(
        [Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes($hashLines -join "`n")))

    Write-Output "Building current MCP source for $branch $version"
    & dotnet build (Join-Path $sourceCopy 'KarenSTS2MCP.csproj') -c Release -o $output `
        "-p:STS2GameDir=$game" --nologo -v:q
    if ($LASTEXITCODE -ne 0) { throw "MCP build failed for $branch" }
    $dll = Join-Path $output 'KarenSTS2MCP.dll'
    if (-not (Test-Path -LiteralPath $dll -PathType Leaf)) { throw "Missing build output: $dll" }
    Copy-Item -LiteralPath $dll -Destination (Join-Path $package 'KarenSTS2MCP.dll') -Force
    Copy-Item -LiteralPath (Join-Path $McpRepository 'mod_manifest.json') `
        -Destination (Join-Path $package 'KarenSTS2MCP.json') -Force

    [PSCustomObject]@{
        repository = $McpRepository
        revision = $revision
        source_has_uncommitted_changes = $dirty
        build_source_sha256 = $buildSourceHash
        game_version = $version
        built_utc = [DateTime]::UtcNow.ToString('o')
        multiplayer_run_command_added_to_build_copy = $true
        loopback_only = [bool]$LoopbackOnly
    } | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $package 'source-info.json') -Encoding UTF8
    Write-Output "Built $package"
}

if (-not $SkipInstall) {
    foreach ($branch in @('stable', 'beta')) {
        & (Join-Path $PSScriptRoot 'dev_env.ps1') -Branch $branch -Action Install `
            -McpSource (Join-Path $OutputRoot "mcp-$branch")
        if (-not $?) { throw "MCP install failed for $branch" }
    }
}
