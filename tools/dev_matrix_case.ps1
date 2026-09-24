param(
    [ValidateSet('stable','beta')][string]$Branch,
    [ValidateSet('KAREN','IRONCLAD')][string]$HostCharacter,
    [ValidateSet('KAREN','IRONCLAD')][string]$ClientCharacter,
    [int]$CaseId,
    [Parameter(Mandatory = $true)][string]$RunRoot,
    [switch]$ShineRegression,
    [switch]$GlobalMoveRegression,
    [switch]$PromiseRegression,
    [ValidateSet('host','client')][string]$ShineActor = 'host'
)

$ErrorActionPreference = 'Stop'
$project = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$RunRoot = [IO.Path]::GetFullPath($RunRoot)
$allowedRoot = [IO.Path]::GetFullPath((Join-Path $project 'artifacts\test-matrix'))
if (-not $RunRoot.StartsWith($allowedRoot + [IO.Path]::DirectorySeparatorChar,
        [StringComparison]::OrdinalIgnoreCase)) { throw "Unsafe run root: $RunRoot" }
$hostRoot = Join-Path $RunRoot "$Branch-host"
$clientRoot = Join-Path $RunRoot "$Branch-client"
$hostGame = Join-Path $project "artifacts/dev-game/$Branch/game"
$clientGame = Join-Path $clientRoot 'game'
$hostExe = Join-Path $hostGame 'SlayTheSpire2.exe'
$clientExe = Join-Path $clientGame 'SlayTheSpire2.exe'
$hostPort = if ($Branch -eq 'stable') { 15627 } else { 15628 }
$clientPort = if ($Branch -eq 'stable') { 15629 } else { 15630 }
$hostId = $CaseId * 10
$clientId = $hostId + 1
$hostProcess = $null
$clientProcess = $null

function Get-State([int]$port, [string]$endpoint = 'singleplayer') {
    Invoke-RestMethod -Uri "http://127.0.0.1:$port/api/v1/$endpoint`?format=json" -TimeoutSec 5
}

function Wait-State([int]$port, [string]$endpoint, [scriptblock]$condition, [string]$label) {
    for ($attempt = 0; $attempt -lt 45; $attempt++) {
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
    $result = Invoke-RestMethod -Uri "http://127.0.0.1:$port/api/v1/$endpoint" -Method Post -ContentType 'application/json' -Body ($action | ConvertTo-Json -Compress) -TimeoutSec 8
    if ($result.status -ne 'ok') { throw "Action failed on $port : $($result | ConvertTo-Json -Compress)" }
    return $result
}

function Prepare-Profile([string]$branch, [string]$role, [int]$profileId) {
    $targetRoot = if ($role -eq 'host') { $hostRoot } else { $clientRoot }
    $source = Join-Path $project "artifacts/dev-game/$branch/user/AppData/Roaming/SlayTheSpire2/default/1/settings.save"
    $destination = Join-Path $targetRoot "user/AppData/Roaming/SlayTheSpire2/default/$profileId/settings.save"
    New-Item -ItemType Directory -Path (Split-Path $destination -Parent),(Join-Path $targetRoot 'user/AppData/Local') -Force | Out-Null
    Copy-Item -LiteralPath $source -Destination $destination -Force
}

function Start-Game([string]$game, [string]$targetRoot, [string]$mode, [int]$profileId) {
    $oldRoaming = $env:APPDATA
    $oldLocal = $env:LOCALAPPDATA
    try {
        $env:APPDATA = Join-Path $targetRoot 'user/AppData/Roaming'
        $env:LOCALAPPDATA = Join-Path $targetRoot 'user/AppData/Local'
        $log = Join-Path $targetRoot 'godot.log'
        $arguments = "--rendering-driver vulkan --force-steam=off --fastmp=$mode --clientId=$profileId --log-file `"$log`""
        Start-Process -FilePath (Join-Path $game 'SlayTheSpire2.exe') -WorkingDirectory $game -ArgumentList $arguments -WindowStyle Hidden -PassThru
    }
    finally {
        $env:APPDATA = $oldRoaming
        $env:LOCALAPPDATA = $oldLocal
    }
}

function Stop-Verified([object]$process, [string]$expectedPath) {
    if ($null -eq $process) { return }
    $running = Get-Process -Id $process.Id -ErrorAction SilentlyContinue
    if ($null -eq $running) { return }
    if (-not $running.Path.Equals($expectedPath, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Unexpected process path: $($running.Path)"
    }
    Stop-Process -Id $process.Id
}

if (Get-NetUDPEndpoint -LocalPort 33771 -ErrorAction SilentlyContinue) { throw 'Multiplayer host port 33771 is occupied' }
foreach ($port in @($hostPort, $clientPort)) {
    if (Get-NetTCPConnection -LocalPort $port -State Listen -ErrorAction SilentlyContinue) {
        throw "MCP port $port is occupied"
    }
}
Prepare-Profile $Branch 'host' $hostId
Prepare-Profile $Branch 'client' $clientId
$caseName = "$Branch-$HostCharacter-$ClientCharacter"
if ($ShineRegression) { $caseName += "-shine-regression-$ShineActor" }
if ($GlobalMoveRegression) { $caseName += '-global-move-regression' }
if ($PromiseRegression) { $caseName += '-promise-regression' }

try {
    $hostProcess = Start-Game $hostGame $hostRoot 'host_standard' $hostId
    $null = Wait-State $hostPort 'singleplayer' { param($s) $s.menu_screen -eq 'character_select' } 'host lobby'
    $null = Invoke-RestMethod -Uri "http://127.0.0.1:$hostPort/api/v1/window/minimize" -Method Post -TimeoutSec 5
    $clientProcess = Start-Game $clientGame $clientRoot 'join' $clientId
    $null = Wait-State $clientPort 'singleplayer' { param($s) $s.lobby.player_count -eq 2 } 'joined lobby'
    $null = Invoke-RestMethod -Uri "http://127.0.0.1:$clientPort/api/v1/window/minimize" -Method Post -TimeoutSec 5

    $null = Post-Action $hostPort 'singleplayer' @{ action='menu_select'; option=$HostCharacter }
    $null = Post-Action $clientPort 'singleplayer' @{ action='menu_select'; option=$ClientCharacter }
    $null = Wait-State $hostPort 'singleplayer' {
        param($s)
        @($s.lobby.players | Where-Object { $_.is_local -and $_.character_id -eq $HostCharacter }).Count -eq 1 -and
            @($s.options | Where-Object { $_.name -eq 'embark' -and $_.enabled }).Count -eq 1
    } 'host character selection'
    $null = Wait-State $clientPort 'singleplayer' {
        param($s)
        @($s.lobby.players | Where-Object { $_.is_local -and $_.character_id -eq $ClientCharacter }).Count -eq 1 -and
            @($s.options | Where-Object { $_.name -eq 'embark' -and $_.enabled }).Count -eq 1
    } 'client character selection'
    $null = Post-Action $hostPort 'singleplayer' @{ action='menu_select'; option='embark' }
    $null = Post-Action $clientPort 'singleplayer' @{ action='menu_select'; option='embark' }
    foreach ($port in @($hostPort, $clientPort)) {
        $s = Wait-State $port 'singleplayer' { param($state) $state.menu_screen -eq 'tutorial_prompt' } 'tutorial prompt'
        $null = Post-Action $port 'singleplayer' @{ action='menu_select'; option='no' }
    }
    foreach ($port in @($hostPort, $clientPort)) {
        $null = Wait-State $port 'multiplayer' { param($s) $s.state_type -eq 'map' } 'map'
    }
    foreach ($port in @($hostPort, $clientPort)) {
        $null = Post-Action $port 'multiplayer' @{ action='choose_map_node'; index=0 }
    }
    foreach ($port in @($hostPort, $clientPort)) {
        $null = Wait-State $port 'multiplayer' { param($s) $s.state_type -eq 'monster' -and $s.battle.is_play_phase } 'first combat'
    }

    if ($ShineRegression) {
        if (($ShineActor -eq 'host' -and $HostCharacter -ne 'KAREN') -or
            ($ShineActor -eq 'client' -and $ClientCharacter -ne 'KAREN')) {
            throw 'Shine regression requires Karen as the acting player'
        }
        $actorPort = if ($ShineActor -eq 'host') { $hostPort } else { $clientPort }
        $otherPort = if ($ShineActor -eq 'host') { $clientPort } else { $hostPort }
        $actorRoot = if ($ShineActor -eq 'host') { $hostRoot } else { $clientRoot }
        $before = Get-State $actorPort 'multiplayer'
        $originalHp = $before.battle.enemies[0].hp
        $null = Post-Action $actorPort 'multiplayer' @{ action='run_command'; command='damage 40' }
        $damaged = Wait-State $actorPort 'multiplayer' { param($s) $s.battle.enemies[0].hp -lt $originalHp } 'damage command'
        $otherDamaged = Wait-State $otherPort 'multiplayer' { param($s) $s.battle.enemies[0].hp -eq $damaged.battle.enemies[0].hp } 'damage synchronization'
        $null = Post-Action $actorPort 'multiplayer' @{ action='run_command'; command='card KAREN_STAR Hand' }
        $withStar = Wait-State $actorPort 'multiplayer' { param($s) @($s.player.hand | Where-Object id -eq 'KAREN_STAR').Count -gt 0 } 'KarenStar in actor hand'
        $star = $withStar.player.hand | Where-Object id -eq 'KAREN_STAR' | Select-Object -First 1
        if (-not $star.can_play) { throw 'KarenStar is not playable' }
        $null = Post-Action $actorPort 'multiplayer' @{ action='play_card'; card_index=$star.index }
        foreach ($port in @($hostPort, $clientPort)) {
            $null = Wait-State $port 'multiplayer' { param($s) $s.state_type -eq 'rewards' } 'combat rewards'
        }
        $hostSave = Join-Path $hostRoot "user/AppData/Roaming/SlayTheSpire2/default/$hostId/modded/profile1/saves/current_run_mp.save"
        $saved = Get-Content -LiteralPath $hostSave -Raw -Encoding UTF8 | ConvertFrom-Json
        $savedActor = if ($ShineActor -eq 'host') { $saved.players[0] } else { $saved.players | Where-Object net_id -eq $clientId | Select-Object -First 1 }
        if ($null -eq $savedActor) { throw "Actor $ShineActor missing from host save" }
        $shineCount = @($savedActor.deck | Where-Object id -match 'KAREN_SHINE_STRIKE').Count
        if ($shineCount -ne 0) { throw "ShineStrike still in saved $ShineActor deck: $shineCount" }
        $logDir = Join-Path $RunRoot 'logs'
        New-Item -ItemType Directory -Path $logDir -Force | Out-Null
        $actorLog = ''
        for ($attempt = 0; $attempt -lt 20; $attempt++) {
            $actorLog = Get-Content -LiteralPath (Join-Path $actorRoot 'godot.log') -Raw -Encoding UTF8
            if ($actorLog.Contains("Card '闪耀打击' added to shine pile")) { break }
            Start-Sleep -Milliseconds 250
        }
        if (-not $actorLog.Contains("Card '闪耀打击' added to shine pile")) { throw "Shine depletion not logged on $ShineActor" }
        Copy-Item -LiteralPath (Join-Path $hostRoot 'godot.log') -Destination (Join-Path $logDir "mp-$caseName-host.log") -Force
        Copy-Item -LiteralPath (Join-Path $clientRoot 'godot.log') -Destination (Join-Path $logDir "mp-$caseName-client.log") -Force
        [PSCustomObject]@{case=$caseName;result='pass';originalHp=$originalHp;damagedHp=$damaged.battle.enemies[0].hp;actorDeckShineCount=$shineCount;bothAtRewards=$true} | ConvertTo-Json -Compress
        return
    }

    if ($GlobalMoveRegression) {
        if ($HostCharacter -ne 'KAREN' -and $ClientCharacter -ne 'KAREN') {
            throw 'Global move regression requires one Karen player'
        }
        $actorPort = if ($HostCharacter -eq 'KAREN') { $hostPort } else { $clientPort }
        $actorRoot = if ($HostCharacter -eq 'KAREN') { $hostRoot } else { $clientRoot }
        $before = Get-State $actorPort 'multiplayer'
        $enemyId = $before.battle.enemies[0].entity_id
        $originalHp = $before.battle.enemies[0].hp
        $null = Post-Action $actorPort 'multiplayer' @{ action='run_command'; command='card KAREN_SPIN Hand' }
        $withSpin = Wait-State $actorPort 'multiplayer' { param($s) @($s.player.hand | Where-Object id -eq 'KAREN_SPIN').Count -gt 0 } 'KarenSpin in hand'
        $spin = $withSpin.player.hand | Where-Object id -eq 'KAREN_SPIN' | Select-Object -First 1
        if (-not $spin.can_play) { throw 'KarenSpin is not playable' }
        $null = Post-Action $actorPort 'multiplayer' @{ action='play_card'; card_index=$spin.index; target=$enemyId }
        $actorAfter = Wait-State $actorPort 'multiplayer' { param($s) $s.battle.enemies[0].hp -lt $originalHp } 'KarenSpin damage'
        $otherPort = if ($actorPort -eq $hostPort) { $clientPort } else { $hostPort }
        $otherAfter = Wait-State $otherPort 'multiplayer' { param($s) $s.battle.enemies[0].hp -eq $actorAfter.battle.enemies[0].hp } 'KarenSpin damage sync'
        foreach ($port in @($hostPort, $clientPort)) {
            $null = Post-Action $port 'multiplayer' @{ action='end_turn' }
        }
        foreach ($port in @($hostPort, $clientPort)) {
            $null = Wait-State $port 'multiplayer' { param($s) $s.battle.round -ge 2 -and $s.battle.is_play_phase } 'round 2 after KarenSpin'
        }
        $hostLog = Get-Content -LiteralPath (Join-Path $hostRoot 'godot.log') -Raw -Encoding UTF8
        $clientLog = Get-Content -LiteralPath (Join-Path $clientRoot 'godot.log') -Raw -Encoding UTF8
        foreach ($log in @($hostLog, $clientLog)) {
            if (-not $log.Contains('[KarenSpin] 牌堆移动: None -> Hand')) { throw 'KarenSpin entry callback missing' }
            if (-not $log.Contains('[KarenSpin] 牌堆移动: Hand -> Discard')) { throw 'KarenSpin play-to-discard callback missing' }
        }
        $logDir = Join-Path $RunRoot 'logs'
        New-Item -ItemType Directory -Path $logDir -Force | Out-Null
        Copy-Item -LiteralPath (Join-Path $hostRoot 'godot.log') -Destination (Join-Path $logDir "mp-$caseName-host.log") -Force
        Copy-Item -LiteralPath (Join-Path $clientRoot 'godot.log') -Destination (Join-Path $logDir "mp-$caseName-client.log") -Force
        $errors = @(Select-String -Path (Join-Path $logDir "mp-$caseName-host.log"),(Join-Path $logDir "mp-$caseName-client.log") -Pattern '^\[ERROR\]')
        if ($errors.Count -gt 0) { throw "Game logged $($errors.Count) errors" }
        [PSCustomObject]@{case=$caseName;result='pass';originalHp=$originalHp;enemyHp=$actorAfter.battle.enemies[0].hp;bothAtRound2=$true;errorCount=$errors.Count} | ConvertTo-Json -Compress
        return
    }

    if ($PromiseRegression) {
        $karenPorts = @()
        if ($HostCharacter -eq 'KAREN') { $karenPorts += $hostPort }
        if ($ClientCharacter -eq 'KAREN') { $karenPorts += $clientPort }
        if ($karenPorts.Count -eq 0) {
            throw 'Promise regression requires at least one Karen player'
        }
        foreach ($port in $karenPorts) {
            $null = Post-Action $port 'multiplayer' @{ action='run_command'; command='card KAREN_DEFEND Hand' }
            $null = Post-Action $port 'multiplayer' @{ action='run_command'; command='card KAREN_FALL Hand' }
            $withFall = Wait-State $port 'multiplayer' {
                param($s) @($s.player.hand | Where-Object id -eq 'KAREN_FALL').Count -gt 0
            } 'KarenFall in hand'
            $fall = $withFall.player.hand | Where-Object id -eq 'KAREN_FALL' | Select-Object -Last 1
            $defendBefore = @($withFall.player.hand | Where-Object id -eq 'KAREN_DEFEND').Count
            $null = Post-Action $port 'multiplayer' @{ action='play_card'; card_index=$fall.index }
            $selection = Wait-State $port 'multiplayer' { param($s) $s.state_type -eq 'hand_select' } 'promise card selection'
            $defend = $selection.hand_select.cards | Where-Object id -eq 'KAREN_DEFEND' | Select-Object -Last 1
            if ($null -eq $defend) { throw 'No KarenDefend can be selected for PromisePile' }
            $null = Post-Action $port 'multiplayer' @{ action='combat_select_card'; card_index=$defend.index }
            $selectionAfter = Get-State $port 'multiplayer'
            if ($selectionAfter.state_type -eq 'hand_select') {
                $null = Post-Action $port 'multiplayer' @{ action='combat_confirm_selection' }
            }
            $null = Wait-State $port 'multiplayer' {
                param($s) $s.state_type -eq 'monster' -and $s.battle.is_play_phase
            } 'promise selection completion'
            $screenshotDir = Join-Path $RunRoot 'logs'
            New-Item -ItemType Directory -Path $screenshotDir -Force | Out-Null
            $side = if ($port -eq $hostPort) { 'host' } else { 'client' }
            Start-Sleep -Milliseconds 500
            Invoke-WebRequest -Uri "http://127.0.0.1:$port/api/v1/screenshot" `
                -OutFile (Join-Path $screenshotDir "mp-$caseName-$side-after-fall.png") -TimeoutSec 15 | Out-Null
            $null = Post-Action $port 'multiplayer' @{ action='run_command'; command='karen_check_hand' }
            $afterFall = Get-State $port 'multiplayer'
            if (@($afterFall.player.hand | Where-Object id -eq 'KAREN_DEFEND').Count -ne $defendBefore - 1) {
                throw 'KarenFall did not remove exactly one Defend from hand'
            }
            $null = Post-Action $port 'multiplayer' @{ action='run_command'; command='card KAREN_TOWER_OF_PROMISE Hand' }
            $withTower = Wait-State $port 'multiplayer' {
                param($s) @($s.player.hand | Where-Object id -eq 'KAREN_TOWER_OF_PROMISE').Count -gt 0
            } 'KarenTowerOfPromise in hand'
            $tower = $withTower.player.hand | Where-Object id -eq 'KAREN_TOWER_OF_PROMISE' | Select-Object -Last 1
            $null = Post-Action $port 'multiplayer' @{ action='play_card'; card_index=$tower.index }
            $null = Wait-State $port 'multiplayer' {
                param($s) $s.state_type -eq 'monster' -and $s.battle.is_play_phase
            } 'promise draw completion'
            Start-Sleep -Seconds 2
            Invoke-WebRequest -Uri "http://127.0.0.1:$port/api/v1/screenshot" `
                -OutFile (Join-Path $screenshotDir "mp-$caseName-$side-after-tower.png") -TimeoutSec 15 | Out-Null
            $afterTower = Get-State $port 'multiplayer'
            if (@($afterTower.player.hand | Where-Object id -eq 'KAREN_DEFEND').Count -ne $defendBefore) {
                throw 'Tower did not return the promised Defend to hand'
            }
            foreach ($checkPort in @($hostPort, $clientPort)) {
                $null = Post-Action $checkPort 'multiplayer' @{ action='run_command'; command='karen_check_hand' }
            }
        }
        foreach ($port in @($hostPort, $clientPort)) {
            $null = Post-Action $port 'multiplayer' @{ action='end_turn' }
        }
        foreach ($port in @($hostPort, $clientPort)) {
            $null = Wait-State $port 'multiplayer' {
                param($s) $s.battle.round -ge 2 -and $s.battle.is_play_phase
            } 'round 2 after promise draw'
        }
        $logDir = Join-Path $RunRoot 'logs'
        New-Item -ItemType Directory -Path $logDir -Force | Out-Null
        foreach ($pair in @(@($hostRoot, 'host'), @($clientRoot, 'client'))) {
            $log = Get-Content -LiteralPath (Join-Path $pair[0] 'godot.log') -Raw -Encoding UTF8
            if (-not $log.Contains('DrawFromPromisePileAsync')) { throw "PromisePile draw not logged for $($pair[1])" }
            if ($log -match '(?m)^\[ERROR\]') { throw "Game logged an error for $($pair[1])" }
            Copy-Item -LiteralPath (Join-Path $pair[0] 'godot.log') `
                -Destination (Join-Path $logDir "mp-$caseName-$($pair[1]).log") -Force
        }
        [PSCustomObject]@{
            case=$caseName; result='pass'; bothAtRound2=$true; promiseDrawLogged=$true;
            karenActors=$karenPorts.Count; handVisualsMatched=$true; cardCountsRestored=$true; errorCount=0
        } | ConvertTo-Json -Compress
        return
    }

    $actions = @()
    foreach ($port in @($hostPort, $clientPort)) {
        $s = Get-State $port 'multiplayer'
        $card = $s.player.hand | Where-Object { $_.can_play -and $_.target_type -eq 'AnyEnemy' } | Select-Object -First 1
        if ($null -eq $card) {
            $card = $s.player.hand | Where-Object { $_.can_play -and $_.id -match 'DEFEND' } | Select-Object -First 1
        }
        if ($null -eq $card) { throw "No simple playable card on port $port" }
        $action = @{ action='play_card'; card_index=$card.index }
        if ($card.target_type -eq 'AnyEnemy') { $action.target = $s.battle.enemies[0].entity_id }
        $null = Post-Action $port 'multiplayer' $action
        $actions += "$port : $($card.id)"
        Start-Sleep -Seconds 2
    }
    $hostState = Get-State $hostPort 'multiplayer'
    $clientState = Get-State $clientPort 'multiplayer'
    if ($hostState.battle.enemies[0].hp -ne $clientState.battle.enemies[0].hp) {
        throw 'Enemy HP differs between host and client'
    }
    foreach ($port in @($hostPort, $clientPort)) {
        $null = Post-Action $port 'multiplayer' @{ action='end_turn' }
    }
    foreach ($port in @($hostPort, $clientPort)) {
        $null = Wait-State $port 'multiplayer' { param($s) $s.battle.round -ge 2 -and $s.battle.is_play_phase } 'round 2'
    }

    $logDir = Join-Path $RunRoot 'logs'
    New-Item -ItemType Directory -Path $logDir -Force | Out-Null
    Copy-Item -LiteralPath (Join-Path $hostRoot 'godot.log') -Destination (Join-Path $logDir "mp-$caseName-host.log") -Force
    Copy-Item -LiteralPath (Join-Path $clientRoot 'godot.log') -Destination (Join-Path $logDir "mp-$caseName-client.log") -Force
    $errors = @(Select-String -Path (Join-Path $logDir "mp-$caseName-host.log"),(Join-Path $logDir "mp-$caseName-client.log") -Pattern '^\[ERROR\]')
    [PSCustomObject]@{case=$caseName;result=if($errors.Count -eq 0){'pass'}else{'log_errors'};enemyHp=$hostState.battle.enemies[0].hp;actions=$actions;round=2;errorCount=$errors.Count} | ConvertTo-Json -Compress
}
finally {
    Stop-Verified $clientProcess $clientExe
    Stop-Verified $hostProcess $hostExe
}
