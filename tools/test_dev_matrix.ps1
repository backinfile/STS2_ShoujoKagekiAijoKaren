param(
    [ValidateSet('smoke', 'solo', 'multiplayer', 'regression', 'promise', 'cross', 'full')]
    [string]$Suite = 'full',
    [switch]$UpdateMcp,
    [switch]$KeepClientGames
)

$ErrorActionPreference = 'Stop'
$project = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$runsRoot = [IO.Path]::GetFullPath((Join-Path $project 'artifacts\test-matrix'))
$runName = [DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss') + '-' + [guid]::NewGuid().ToString('N').Substring(0, 6)
$runRoot = [IO.Path]::GetFullPath((Join-Path $runsRoot $runName))
$logDir = Join-Path $runRoot 'logs'
New-Item -ItemType Directory -Path $logDir -Force | Out-Null
$results = [Collections.Generic.List[object]]::new()
$testId = 200

function Get-Port([string]$branch, [bool]$client = $false) {
    if ($branch -eq 'stable') { if ($client) { return 15629 }; return 15627 }
    if ($client) { return 15630 }; return 15628
}

function Get-State([int]$port, [string]$endpoint = 'singleplayer') {
    Invoke-RestMethod -Uri "http://127.0.0.1:$port/api/v1/$endpoint`?format=json" -TimeoutSec 5
}

function Wait-State([int]$port, [string]$endpoint, [scriptblock]$condition, [string]$label) {
    for ($attempt = 0; $attempt -lt 60; $attempt++) {
        try {
            $state = Get-State $port $endpoint
            if (& $condition $state) { return $state }
        }
        catch { }
        Start-Sleep -Milliseconds 500
    }
    throw "Timed out waiting for $label on port $port"
}

function Post-Action([int]$port, [string]$endpoint, [hashtable]$action) {
    $result = Invoke-RestMethod -Uri "http://127.0.0.1:$port/api/v1/$endpoint" -Method Post `
        -ContentType 'application/json' -Body ($action | ConvertTo-Json -Compress) -TimeoutSec 8
    if ($result.status -ne 'ok') {
        $state = Get-State $port $endpoint
        throw "Action $($action | ConvertTo-Json -Compress) failed on port $port (state=$($state.state_type), menu=$($state.menu_screen)): $($result | ConvertTo-Json -Compress)"
    }
    return $result
}

function Wait-NetworkFree {
    for ($attempt = 0; $attempt -lt 30; $attempt++) {
        $udp = Get-NetUDPEndpoint -LocalPort 33771 -ErrorAction SilentlyContinue
        $tcp = @(15627, 15628, 15629, 15630 | ForEach-Object {
            Get-NetTCPConnection -LocalPort $_ -State Listen -ErrorAction SilentlyContinue
        })
        if (-not $udp -and $tcp.Count -eq 0) { return }
        Start-Sleep -Milliseconds 500
    }
    throw 'A test game or another project is using the multiplayer/MCP ports'
}

function Start-Game([string]$game, [string]$userRoot, [int]$profileId, [string]$log, [string]$fastMp = '') {
    $exe = Join-Path $game 'SlayTheSpire2.exe'
    if (-not (Test-Path -LiteralPath $exe -PathType Leaf)) { throw "Missing game: $exe" }
    $oldRoaming = $env:APPDATA
    $oldLocal = $env:LOCALAPPDATA
    try {
        $env:APPDATA = Join-Path $userRoot 'user\AppData\Roaming'
        $env:LOCALAPPDATA = Join-Path $userRoot 'user\AppData\Local'
        New-Item -ItemType Directory -Path $env:APPDATA,$env:LOCALAPPDATA -Force | Out-Null
        $arguments = "--rendering-driver vulkan --force-steam=off --clientId=$profileId --log-file `"$log`""
        if ($fastMp) { $arguments += " --fastmp=$fastMp" }
        return Start-Process -FilePath $exe -WorkingDirectory $game -ArgumentList $arguments `
            -WindowStyle Hidden -PassThru
    }
    finally {
        $env:APPDATA = $oldRoaming
        $env:LOCALAPPDATA = $oldLocal
    }
}

function Stop-Verified([object]$process, [string]$expectedGame) {
    if ($null -eq $process) { return }
    $running = Get-Process -Id $process.Id -ErrorAction SilentlyContinue
    if ($null -eq $running) { return }
    $expected = [IO.Path]::GetFullPath((Join-Path $expectedGame 'SlayTheSpire2.exe'))
    if (-not $running.Path.Equals($expected, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to stop unexpected process: $($running.Path)"
    }
    Stop-Process -Id $process.Id
    $running.WaitForExit(5000) | Out-Null
}

function Prepare-Profile([string]$branch, [string]$userRoot, [int]$profileId) {
    $source = Join-Path $project "artifacts/dev-game/$branch/user/AppData/Roaming/SlayTheSpire2/default/1/settings.save"
    $destination = Join-Path $userRoot "user/AppData/Roaming/SlayTheSpire2/default/$profileId/settings.save"
    if (-not (Test-Path -LiteralPath $source -PathType Leaf)) { throw "Missing settings: $source" }
    New-Item -ItemType Directory -Path (Split-Path $destination -Parent) -Force | Out-Null
    Copy-Item -LiteralPath $source -Destination $destination -Force
}

function Test-Solo([string]$branch, [int]$caseId) {
    $port = Get-Port $branch
    $game = Join-Path $project "artifacts/dev-game/$branch/game"
    $root = Join-Path $runRoot "solo-$branch"
    $profileId = $caseId * 10
    $log = Join-Path $root 'godot.log'
    $process = $null
    Prepare-Profile $branch $root $profileId
    try {
        $process = Start-Game $game $root $profileId $log
        $null = Wait-State $port 'singleplayer' {
            param($s) $s.menu_screen -eq 'main' -and @($s.options) -contains 'singleplayer'
        } 'ready main menu'
        $null = Invoke-RestMethod -Uri "http://127.0.0.1:$port/api/v1/window/minimize" -Method Post -TimeoutSec 5
        $settings = Invoke-RestMethod -Uri "http://127.0.0.1:$port/api/v1/settings" -TimeoutSec 5
        if ($settings.status -ne 'ok' -or $null -eq $settings.fullscreen) {
            throw 'The latest MCP settings endpoint is unavailable'
        }
        $newMuted = -not [bool]$settings.muted
        $changed = Invoke-RestMethod -Uri "http://127.0.0.1:$port/api/v1/settings" -Method Post `
            -ContentType 'application/json' -Body (@{ muted=$newMuted } | ConvertTo-Json -Compress) -TimeoutSec 5
        if ($changed.status -ne 'ok' -or $changed.muted -ne $newMuted) {
            throw 'MCP settings update did not take effect'
        }
        $restored = Invoke-RestMethod -Uri "http://127.0.0.1:$port/api/v1/settings" -Method Post `
            -ContentType 'application/json' -Body (@{ muted=[bool]$settings.muted } | ConvertTo-Json -Compress) -TimeoutSec 5
        if ($restored.status -ne 'ok' -or $restored.muted -ne [bool]$settings.muted) {
            throw 'MCP settings update could not be restored'
        }
        $null = Post-Action $port 'singleplayer' @{ action='menu_select'; option='singleplayer' }
        $next = Wait-State $port 'singleplayer' {
            param($s) $s.menu_screen -in @('singleplayer', 'character_select')
        } 'solo character selection'
        if ($next.menu_screen -eq 'singleplayer') {
            $null = Post-Action $port 'singleplayer' @{ action='menu_select'; option='standard' }
            $null = Wait-State $port 'singleplayer' { param($s) $s.menu_screen -eq 'character_select' } 'character select'
        }
        $null = Post-Action $port 'singleplayer' @{ action='menu_select'; option='KAREN' }
        $null = Post-Action $port 'singleplayer' @{ action='menu_select'; option='embark' }
        $null = Wait-State $port 'singleplayer' { param($s) $s.menu_screen -eq 'tutorial_prompt' } 'tutorial prompt'
        $null = Post-Action $port 'singleplayer' @{ action='menu_select'; option='no' }
        $null = Wait-State $port 'singleplayer' { param($s) $s.state_type -eq 'map' } 'map'
        $null = Post-Action $port 'singleplayer' @{ action='choose_map_node'; index=0 }
        $before = Wait-State $port 'singleplayer' { param($s) $s.state_type -eq 'monster' -and $s.battle.is_play_phase } 'combat'
        $originalHp = $before.battle.enemies[0].hp
        $null = Post-Action $port 'singleplayer' @{ action='run_command'; command='damage 40' }
        $damaged = Wait-State $port 'singleplayer' { param($s) $s.battle.enemies[0].hp -lt $originalHp } 'damage command'
        $null = Post-Action $port 'singleplayer' @{ action='run_command'; command='card KAREN_STAR Hand' }
        $withStar = Wait-State $port 'singleplayer' { param($s) @($s.player.hand | Where-Object id -eq 'KAREN_STAR').Count -gt 0 } 'KarenStar'
        $star = $withStar.player.hand | Where-Object id -eq 'KAREN_STAR' | Select-Object -First 1
        if (-not $star.can_play) { throw 'KarenStar cannot be played' }
        $null = Post-Action $port 'singleplayer' @{ action='play_card'; card_index=$star.index }
        $null = Wait-State $port 'singleplayer' { param($s) $s.state_type -eq 'rewards' } 'rewards'

        $savePath = Join-Path $root "user/AppData/Roaming/SlayTheSpire2/default/$profileId/modded/profile1/saves/current_run.save"
        $save = Get-Content -LiteralPath $savePath -Raw -Encoding UTF8 | ConvertFrom-Json
        $shineCount = @($save.players[0].deck | Where-Object id -match 'KAREN_SHINE_STRIKE').Count
        if ($shineCount -ne 0) { throw "Shine Strike remains in deck: $shineCount" }
        $contents = Get-Content -LiteralPath $log -Raw -Encoding UTF8
        if (-not $contents.Contains("Card '闪耀打击' added to shine pile")) { throw 'Shine depletion was not logged' }
        if ($contents -match '(?m)^\[ERROR\]') { throw 'Game logged an error' }
        Copy-Item -LiteralPath $log -Destination (Join-Path $logDir "solo-$branch.log") -Force
        return [PSCustomObject]@{ case="solo-$branch"; result='pass'; game_version=$branch;
            settings_api=$true; settings_write_restored=$true;
            original_hp=$originalHp; damaged_hp=$damaged.battle.enemies[0].hp;
            shine_count_in_deck=$shineCount }
    }
    finally { Stop-Verified $process $game }
}

function Test-CrossVersion([string]$hostBranch, [string]$clientBranch, [int]$caseId) {
    $hostPort = Get-Port $hostBranch
    $clientPort = Get-Port $clientBranch $true
    $hostGame = Join-Path $project "artifacts/dev-game/$hostBranch/game"
    $clientGame = Join-Path $runRoot "$clientBranch-client/game"
    $hostRoot = Join-Path $runRoot "cross-$hostBranch-host"
    $clientRoot = Join-Path $runRoot "cross-$clientBranch-client"
    $hostLog = Join-Path $hostRoot 'godot.log'
    $clientLog = Join-Path $clientRoot 'godot.log'
    $hostId = $caseId * 10
    $clientId = $hostId + 1
    $hostProcess = $null
    $clientProcess = $null
    Prepare-Profile $hostBranch $hostRoot $hostId
    Prepare-Profile $clientBranch $clientRoot $clientId
    try {
        $hostProcess = Start-Game $hostGame $hostRoot $hostId $hostLog 'host_standard'
        $null = Wait-State $hostPort 'singleplayer' { param($s) $s.menu_screen -eq 'character_select' } 'cross-version host lobby'
        $clientProcess = Start-Game $clientGame $clientRoot $clientId $clientLog 'join'
        for ($attempt = 0; $attempt -lt 50; $attempt++) {
            if (Test-Path -LiteralPath $clientLog) {
                $clientText = Get-Content -LiteralPath $clientLog -Raw -Encoding UTF8
                if ($clientText.Contains('SwitchExpressionException')) { break }
            }
            Start-Sleep -Milliseconds 500
        }
        $hostText = Get-Content -LiteralPath $hostLog -Raw -Encoding UTF8
        $clientText = Get-Content -LiteralPath $clientLog -Raw -Encoding UTF8
        Copy-Item -LiteralPath $hostLog -Destination (Join-Path $logDir "cross-$hostBranch-$clientBranch-host.log") -Force
        Copy-Item -LiteralPath $clientLog -Destination (Join-Path $logDir "cross-$hostBranch-$clientBranch-client.log") -Force
        if (-not $hostText.Contains('HandshakeTimeout')) { throw 'Cross-version handshake result was not observed' }
        if ($clientText.Contains('SwitchExpressionException')) {
            throw 'Game NErrorPopup throws SwitchExpressionException instead of showing a normal version error'
        }
        return [PSCustomObject]@{ handshake_rejected=$true; client_error_popup_exception=$false }
    }
    finally {
        Stop-Verified $clientProcess $clientGame
        Stop-Verified $hostProcess $hostGame
    }
}

function Record-Case([string]$name, [scriptblock]$action) {
    Write-Output "RUN $name"
    try {
        $detail = & $action
        $result = [PSCustomObject]@{ case=$name; result='pass'; detail=$detail; error=$null }
    }
    catch {
        $result = [PSCustomObject]@{ case=$name; result='fail'; detail=$null; error=$_.Exception.Message }
    }
    $results.Add($result)
    Write-Output ($result | ConvertTo-Json -Compress -Depth 6)
}

function Prepare-ClientGames {
    foreach ($branch in @('stable', 'beta')) {
        $source = Join-Path $project "artifacts/dev-game/$branch/game"
        $client = Join-Path $runRoot "$branch-client/game"
        New-Item -ItemType Directory -Path $client -Force | Out-Null
        & robocopy $source $client /E /NFL /NDL /NP /R:1 /W:1 | Out-Null
        if ($LASTEXITCODE -ge 8) { throw "Could not copy $branch client game" }
        $config = Join-Path $client 'mods\KarenSTS2MCP.conf'
        $settings = Get-Content -LiteralPath $config -Raw -Encoding UTF8 | ConvertFrom-Json -AsHashtable
        $settings.port = Get-Port $branch $true
        [IO.File]::WriteAllText($config, ($settings | ConvertTo-Json -Compress -Depth 16),
            [Text.UTF8Encoding]::new($false))
    }
}

function Remove-ClientGames {
    if ($KeepClientGames) { return 'kept by request' }
    $deferred = [Collections.Generic.List[string]]::new()
    foreach ($branch in @('stable', 'beta')) {
        $client = [IO.Path]::GetFullPath((Join-Path $runRoot "$branch-client/game"))
        if (-not $client.StartsWith($runRoot + [IO.Path]::DirectorySeparatorChar,
                [StringComparison]::OrdinalIgnoreCase)) { throw "Unsafe cleanup path: $client" }
        for ($attempt = 0; $attempt -lt 10 -and (Test-Path -LiteralPath $client); $attempt++) {
            try { Remove-Item -LiteralPath $client -Recurse -Force -ErrorAction Stop }
            catch { Start-Sleep -Milliseconds 500 }
        }
        if (Test-Path -LiteralPath $client) { $deferred.Add($client) }
    }
    if ($deferred.Count -gt 0) { return "deferred: $($deferred -join ', ')" }
    return 'removed'
}

try {
    if ($UpdateMcp) {
        & (Join-Path $PSScriptRoot 'update_dev_mcp.ps1')
        if (-not $?) { throw 'MCP update failed' }
    }
    foreach ($branch in @('stable', 'beta')) {
        if (-not (Test-Path -LiteralPath (Join-Path $project "artifacts/mcp-$branch/source-info.json"))) {
            throw "No current MCP build for $branch; run with -UpdateMcp"
        }
    }
    if ($Suite -in @('smoke', 'full')) {
        foreach ($branch in @('stable', 'beta')) {
            Record-Case "smoke-$branch" {
                & (Join-Path $PSScriptRoot 'dev_env.ps1') -Branch $branch -Action Smoke
                if (-not $?) { throw "Smoke failed for $branch" }
            }
        }
    }
    if ($Suite -in @('solo', 'full')) {
        foreach ($branch in @('stable', 'beta')) {
            $testId++
            Record-Case "solo-$branch" { Test-Solo $branch $testId }
        }
    }
    if ($Suite -in @('multiplayer', 'regression', 'promise', 'cross', 'full')) {
        Prepare-ClientGames
    }
    if ($Suite -in @('multiplayer', 'full')) {
        foreach ($branch in @('stable', 'beta')) {
            foreach ($hostCharacter in @('KAREN', 'IRONCLAD')) {
                foreach ($clientCharacter in @('KAREN', 'IRONCLAD')) {
                    $testId++
                    $name = "mp-$branch-$hostCharacter-$clientCharacter"
                    Record-Case $name {
                        Wait-NetworkFree
                        $json = & (Join-Path $PSScriptRoot 'dev_matrix_case.ps1') -Branch $branch `
                            -HostCharacter $hostCharacter -ClientCharacter $clientCharacter `
                            -CaseId $testId -RunRoot $runRoot
                        if (-not $?) { throw "Case script failed: $name" }
                        $detail = $json | Select-Object -Last 1 | ConvertFrom-Json
                        if ($detail.result -ne 'pass') { throw "Case returned $($detail.result)" }
                        $detail
                    }
                }
            }
        }
    }
    if ($Suite -in @('regression', 'full')) {
        foreach ($branch in @('stable', 'beta')) {
            foreach ($scenario in @(
                    @{ Host='KAREN'; Client='KAREN'; Kind='shine'; Actor='host' },
                    @{ Host='KAREN'; Client='KAREN'; Kind='shine'; Actor='client' },
                    @{ Host='KAREN'; Client='IRONCLAD'; Kind='shine'; Actor='host' },
                    @{ Host='IRONCLAD'; Client='KAREN'; Kind='shine'; Actor='client' },
                    @{ Host='KAREN'; Client='IRONCLAD'; Kind='global' },
                    @{ Host='IRONCLAD'; Client='KAREN'; Kind='global' }
                )) {
                $testId++
                $scenarioSuffix = if ($scenario.Kind -eq 'global') { 'global-move' } else { "shine-$($scenario.Actor)" }
                $name = "mp-$branch-$($scenario.Host)-$($scenario.Client)-$scenarioSuffix"
                Record-Case $name {
                    Wait-NetworkFree
                    $arguments = @{
                        Branch = $branch
                        HostCharacter = $scenario.Host
                        ClientCharacter = $scenario.Client
                        CaseId = $testId
                        RunRoot = $runRoot
                    }
                    if ($scenario.Kind -eq 'shine') {
                        $arguments.ShineRegression = $true
                        $arguments.ShineActor = $scenario.Actor
                    }
                    else { $arguments.GlobalMoveRegression = $true }
                    $json = & (Join-Path $PSScriptRoot 'dev_matrix_case.ps1') @arguments
                    if (-not $?) { throw "Case script failed: $name" }
                    $detail = $json | Select-Object -Last 1 | ConvertFrom-Json
                    if ($detail.result -ne 'pass') { throw "Case returned $($detail.result)" }
                    $detail
                }
            }
        }
    }
    if ($Suite -in @('regression', 'promise', 'full')) {
        foreach ($branch in @('stable', 'beta')) {
            foreach ($characters in @(@('KAREN', 'KAREN'), @('KAREN', 'IRONCLAD'), @('IRONCLAD', 'KAREN'))) {
                $testId++
                Record-Case "mp-$branch-$($characters[0])-$($characters[1])-promise" {
                    Wait-NetworkFree
                    $json = & (Join-Path $PSScriptRoot 'dev_matrix_case.ps1') -Branch $branch `
                        -HostCharacter $characters[0] -ClientCharacter $characters[1] -CaseId $testId `
                        -RunRoot $runRoot -PromiseRegression
                    if (-not $?) { throw "Promise case failed for $branch" }
                    $detail = $json | Select-Object -Last 1 | ConvertFrom-Json
                    if ($detail.result -ne 'pass') { throw "Promise case returned $($detail.result)" }
                    $detail
                }
            }
        }
    }
    if ($Suite -eq 'cross') {
        foreach ($pair in @(@('stable', 'beta'), @('beta', 'stable'))) {
            $testId++
            $hostBranch = $pair[0]
            $clientBranch = $pair[1]
            Record-Case "cross-$hostBranch-$clientBranch" {
                Wait-NetworkFree
                Test-CrossVersion $hostBranch $clientBranch $testId
            }
        }
    }
}
finally {
    try { $cleanup = Remove-ClientGames }
    catch { $cleanup = "failed: $($_.Exception.Message)" }
    $report = [PSCustomObject]@{
        started_utc = $runName
        suite = $Suite
        passed = @($results | Where-Object result -eq 'pass').Count
        failed = @($results | Where-Object result -eq 'fail').Count
        client_copy_cleanup = $cleanup
        results = @($results)
        mcp_stable = if (Test-Path (Join-Path $project 'artifacts/mcp-stable/source-info.json')) {
            Get-Content -LiteralPath (Join-Path $project 'artifacts/mcp-stable/source-info.json') -Raw -Encoding UTF8 | ConvertFrom-Json
        } else { $null }
        mcp_beta = if (Test-Path (Join-Path $project 'artifacts/mcp-beta/source-info.json')) {
            Get-Content -LiteralPath (Join-Path $project 'artifacts/mcp-beta/source-info.json') -Raw -Encoding UTF8 | ConvertFrom-Json
        } else { $null }
    }
    $reportPath = Join-Path $runRoot 'report.json'
    $report | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath $reportPath -Encoding UTF8
    Write-Output "Report: $reportPath (passed=$($report.passed), failed=$($report.failed))"
    if ($cleanup -notin @('removed', 'kept by request')) { Write-Warning "Client game cleanup $cleanup" }
}

if ($results.Count -eq 0 -or @($results | Where-Object result -eq 'fail').Count -gt 0) { exit 1 }
