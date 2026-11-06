param([Parameter(Mandatory = $true)][string]$ClientGamesRoot)
$ErrorActionPreference = 'Stop'
$project = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$allowedRoot = Join-Path $project 'artifacts/test-matrix'
$ClientGamesRoot = [IO.Path]::GetFullPath($ClientGamesRoot)
if (-not $ClientGamesRoot.StartsWith($allowedRoot + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
    throw 'Client games must belong to this worktree test matrix'
}
$runRoot = Join-Path $allowedRoot ('seven-relics-' + [DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss'))
New-Item -ItemType Directory -Path $runRoot -Force | Out-Null
$results = [Collections.Generic.List[object]]::new()
foreach ($branch in @('stable','beta')) {
    $variant = if ($branch -eq 'stable') { 'Stable' } else { 'Beta' }
    $output = Join-Path $project "artifacts/relic-mp-fixture/$branch"
    & dotnet build (Join-Path $PSScriptRoot 'KarenRelicRegression.csproj') "-p:Variant=$variant" -o $output --nologo -v:q -clp:ErrorsOnly
    if ($LASTEXITCODE -ne 0) { throw "Fixture build failed: $branch" }
    foreach ($game in @((Join-Path $project "artifacts/dev-game/$branch/game"), (Join-Path $ClientGamesRoot "$branch-client/game"))) {
        $expectedExe = Join-Path $game 'SlayTheSpire2.exe'
        if (@(Get-Process SlayTheSpire2 -ErrorAction SilentlyContinue | Where-Object Path -eq $expectedExe).Count) {
            throw "Test game is still running: $expectedExe"
        }
        $fixture = Join-Path $game 'mods/KarenRelicRegression'
        New-Item -ItemType Directory -Path $fixture -Force | Out-Null
        Copy-Item -LiteralPath (Join-Path $output 'KarenRelicRegression.dll') -Destination $fixture -Force
        @{id='KarenRelicRegression';name='Karen Relic Regression';author='Codex';version='0.1.0';min_game_version='v0.107.1';has_dll=$true;has_pck=$false;affects_gameplay=$true;dependencies=@(@{id='ShoujoKagekiAijoKaren';min_version='v0.1.3'})} |
            ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $fixture 'KarenRelicRegression.json') -Encoding UTF8
    }
}
$caseId = 800
foreach ($branch in @('stable','beta')) {
    foreach ($characters in @(@('KAREN','KAREN'), @('KAREN','IRONCLAD'), @('IRONCLAD','KAREN'))) {
        $caseId++
        $name = "seven-$branch-$($characters[0])-$($characters[1])"
        Write-Output "RUN $name"
        try {
            $json = & (Join-Path $project 'tools/dev_matrix_case.ps1') -Branch $branch -HostCharacter $characters[0] `
                -ClientCharacter $characters[1] -CaseId $caseId -RunRoot $runRoot -ClientGamesRoot $ClientGamesRoot -SevenRelicRegression
            $detail = $json | Select-Object -Last 1 | ConvertFrom-Json
            if ($detail.result -ne 'pass') { throw 'Case did not pass' }
            $result = @{case=$name;result='pass';detail=$detail;error=$null}
        } catch { $result = @{case=$name;result='fail';detail=$null;error=$_.Exception.Message} }
        $results.Add($result)
        Write-Output ($result | ConvertTo-Json -Depth 10 -Compress)
        @{passed=@($results | Where-Object result -eq 'pass').Count;failed=@($results | Where-Object result -eq 'fail').Count;results=@($results)} |
            ConvertTo-Json -Depth 16 | Set-Content -LiteralPath (Join-Path $runRoot 'report.json') -Encoding UTF8
    }
}
Write-Output "Report: $runRoot/report.json"
if (@($results | Where-Object result -eq 'fail').Count) { exit 1 }
