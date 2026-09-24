param(
    [switch]$SkipMcpBuild,
    [ValidateSet('stable','beta')][string[]]$Environment = @('stable','beta'),
    [string]$ClientGamesRoot = ''
)
$ErrorActionPreference = 'Stop'
$project = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$runRoot = Join-Path $project ('artifacts/test-matrix/review-' + [DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss'))
New-Item -ItemType Directory -Path $runRoot -Force | Out-Null
if (-not $ClientGamesRoot) { $ClientGamesRoot = Join-Path $project 'artifacts/test-matrix/review-client-cache' }
$ClientGamesRoot = [IO.Path]::GetFullPath($ClientGamesRoot)
$allowedClients = [IO.Path]::GetFullPath((Join-Path $project 'artifacts/test-matrix'))
if (-not $ClientGamesRoot.StartsWith($allowedClients + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) { throw 'Unsafe client cache' }
$results = @()
try {
    if (-not $SkipMcpBuild) {
        & (Join-Path $PSScriptRoot 'update_dev_mcp.ps1') -LoopbackOnly
        if (-not $?) { throw 'Loopback MCP build failed' }
    }
    foreach ($branch in $Environment) {
        $variant = if ($branch -eq 'stable') { 'Stable' } else { 'Beta' }
        $game = Join-Path $project "artifacts/dev-game/$branch/game"
        $output = Join-Path $project "artifacts/review-plugin/$branch"
        & dotnet build (Join-Path $PSScriptRoot 'review-regression/KarenReviewRegression.csproj') "-p:Variant=$variant" -o $output --nologo -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Fixture build failed' }
        $plugin = Join-Path $game 'mods/KarenReviewRegression'
        New-Item -ItemType Directory -Path $plugin -Force | Out-Null
        Copy-Item -LiteralPath (Join-Path $output 'KarenReviewRegression.dll') -Destination $plugin -Force
        @{
            id='KarenReviewRegression';name='Karen review regression fixtures';author='Karen tests';version='1.0.0';
            has_dll=$true;has_pck=$false;affects_gameplay=$true;min_game_version='v0.107.1';
            dependencies=@(@{id='ShoujoKagekiAijoKaren'})
        } | ConvertTo-Json -Depth 4 | Set-Content -Encoding UTF8 (Join-Path $plugin 'KarenReviewRegression.json')
        $client = Join-Path $ClientGamesRoot "$branch-client/game"
        & robocopy $game $client /E /NFL /NDL /NP /R:1 /W:1 | Out-Null
        if ($LASTEXITCODE -ge 8) { throw 'Client copy failed' }
        $config = Join-Path $client 'mods/KarenSTS2MCP.conf'
        $settings = Get-Content -LiteralPath $config -Raw -Encoding UTF8 | ConvertFrom-Json -AsHashtable
        $settings.port = if ($branch -eq 'stable') {15629} else {15630}
        $settings | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath $config -Encoding UTF8
        foreach ($characters in @(@('KAREN','IRONCLAD'),@('IRONCLAD','KAREN'))) {
            try {
                $caseId = if ($characters[0] -eq 'KAREN') {701} else {702}
                $json = & (Join-Path $PSScriptRoot 'dev_matrix_case.ps1') -Branch $branch -HostCharacter $characters[0] -ClientCharacter $characters[1] -CaseId $caseId -RunRoot $runRoot -ReviewRegression -ClientGamesRoot $ClientGamesRoot
                if (-not $?) { throw 'Review case failed' }
                $result = $json | Select-Object -Last 1 | ConvertFrom-Json
                if ($result.result -ne 'pass') { throw 'Review case did not pass' }
                $results += $result
            } catch {
                $results += [PSCustomObject]@{case="$branch-$($characters -join '-')";result='fail';error=$_.Exception.Message}
            }
            $results[-1] | ConvertTo-Json -Compress -Depth 5
        }
    }
} finally {
    $results | ConvertTo-Json -Depth 8 | Set-Content -Encoding UTF8 (Join-Path $runRoot 'report.json')
    foreach ($branch in $Environment) {
        $game = Join-Path $project "artifacts/dev-game/$branch/game"
        $plugin = [IO.Path]::GetFullPath((Join-Path $game 'mods/KarenReviewRegression'))
        if (-not $plugin.StartsWith($project + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) { throw 'Unsafe fixture cleanup' }
        if (Test-Path -LiteralPath $plugin) { Remove-Item -LiteralPath $plugin -Recurse -Force }
    }
    $results | ConvertTo-Json -Depth 8 | Set-Content -Encoding UTF8 (Join-Path $runRoot 'report.json')
    Write-Output "Report: $runRoot/report.json"
}
if (@($results | Where-Object result -eq 'fail').Count -gt 0 -or $results.Count -ne (2 * $Environment.Count)) { exit 1 }
