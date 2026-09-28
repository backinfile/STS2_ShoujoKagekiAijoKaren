param(
    [int]$ClientId = 66032,
    [ValidateSet('KAREN_DEFEND', 'KAREN_STRIKE')]
    [string]$SelectedId = 'KAREN_DEFEND',
    [ValidateSet('stable', 'beta')]
    [string]$Environment = 'beta',
    [switch]$Record,
    [switch]$OrbitPreview,
    [switch]$Showcase,
    [switch]$TransformChecks,
    [switch]$AllowPlacementFailure,
    [string]$GameDirectory = '',
    [int]$McpPort = 0
)

$ErrorActionPreference = 'Stop'
$project = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$root = Join-Path $project 'artifacts\test-promise-visuals'
$branch = if ($Environment -eq 'stable') { 'stable' } else { 'beta' }
$game = Join-Path $project "artifacts\dev-game\$branch\game"
if ($GameDirectory) { $game = [IO.Path]::GetFullPath($GameDirectory) }
$exe = Join-Path $game 'SlayTheSpire2.exe'
$run = Join-Path $root ([DateTime]::UtcNow.ToString("yyyyMMdd-HHmmss") + "-$Environment-" + [guid]::NewGuid().ToString("N").Substring(0,6))
$port = if ($Environment -eq 'stable') { 15627 } else { 15628 }
if ($McpPort) { $port = $McpPort }
$url = "http://127.0.0.1:$port/api/v1/singleplayer"
$process = $null
$recordingStarted = $false
$videoPath = $null
$placementFailures = @()

function Capture([string]$name) {
    Start-Sleep -Seconds 2
    Get-State | ConvertTo-Json -Depth 30 | Set-Content -Encoding UTF8 (Join-Path $run "$name.json")
    $path = Join-Path $run "$name.png"
    Invoke-WebRequest -Uri "http://127.0.0.1:$port/api/v1/screenshot" -OutFile $path -TimeoutSec 15 | Out-Null
}

if (Test-Path -LiteralPath $run) { throw "Run directory already exists: $run" }
if (Get-NetTCPConnection -LocalPort $port -State Listen -ErrorAction SilentlyContinue) {
    throw "MCP port $port is occupied"
}
$roaming = Join-Path $run 'user\AppData\Roaming'
$local = Join-Path $run 'user\AppData\Local'
$settings = Join-Path $roaming "SlayTheSpire2\default\$ClientId\settings.save"
New-Item -ItemType Directory -Path (Split-Path $settings -Parent),$local -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $project "artifacts\dev-game\$branch\user\AppData\Roaming\SlayTheSpire2\default\1\settings.save") -Destination $settings

function Get-State {
    Invoke-RestMethod -Uri "$url`?format=json" -TimeoutSec 5
}
function Wait-State([scriptblock]$accept, [string]$label) {
    for ($i = 0; $i -lt 100; $i++) {
        try {
            $state = Get-State
            if (& $accept $state) { return $state }
        }
        catch { }
        Start-Sleep -Milliseconds 250
    }
    throw "Timed out: $label"
}
function Post([hashtable]$body) {
    $result = Invoke-RestMethod -Uri $url -Method Post -ContentType 'application/json' -Body ($body | ConvertTo-Json -Compress) -TimeoutSec 12
    if ($result.status -ne 'ok') { throw "Action failed: $($body | ConvertTo-Json -Compress): $($result | ConvertTo-Json -Compress)" }
    return $result
}

try {
    $oldRoaming = $env:APPDATA
    $oldLocal = $env:LOCALAPPDATA
    try {
        $env:APPDATA = $roaming
        $env:LOCALAPPDATA = $local
        $arguments = "--rendering-driver vulkan --force-steam=off --clientId=$ClientId --log-file `"$(Join-Path $run 'godot.log')`""
        $process = Start-Process -FilePath $exe -WorkingDirectory $game -PassThru -WindowStyle Hidden -ArgumentList $arguments
    }
    finally {
        $env:APPDATA = $oldRoaming
        $env:LOCALAPPDATA = $oldLocal
    }

    $null = Wait-State { param($s) $s.menu_screen -eq 'main' -and @($s.options) -contains 'singleplayer' } 'main menu'
    $null = Invoke-RestMethod -Uri "http://127.0.0.1:$port/api/v1/window/minimize" -Method Post -TimeoutSec 5
    $null = Post @{ action='menu_select'; option='singleplayer' }
    $null = Post @{ action='menu_select'; option='KAREN' }
    $null = Post @{ action='menu_select'; option='embark' }
    $state = Get-State
    if ($state.menu_screen -eq 'tutorial_prompt') { $null = Post @{ action='menu_select'; option='no' } }
    $null = Wait-State { param($s) $s.state_type -eq 'map' } 'map'
    $null = Post @{ action='choose_map_node'; index=0 }
    $null = Wait-State { param($s) $s.state_type -eq 'monster' -and $s.battle.is_play_phase } 'combat'

    if ($TransformChecks) {
        foreach ($pose in @('normal','small','large','body_flip','small_flip','visual_flip','rotate','stretch','offset','normal')) {
            $null = Post @{ action='run_command'; command="promise_transform $pose" }
            Capture "transform-$pose"
            $check = Invoke-RestMethod -Uri $url -Method Post -ContentType 'application/json' -Body '{"action":"run_command","command":"promise_transform check"}'
            $check | ConvertTo-Json -Depth 10 | Set-Content -Encoding UTF8 (Join-Path $run "transform-$pose-check.json")
            Write-Output "$pose : $($check | ConvertTo-Json -Compress)"
            if ($check.status -ne 'ok') {
                $placementFailures += $pose
                if (-not $AllowPlacementFailure) { throw "Transform failed: $pose" }
            }
        }
        $null = Post @{ action='run_command'; command='promise_transform small_flip' }
        Start-Sleep -Milliseconds 500
    }
    if ($Record) {
        $null = Invoke-RestMethod -Uri "http://127.0.0.1:$port/api/v1/recording/start" -Method Post -ContentType 'application/json' -Body '{"fps":30,"max_duration_seconds":180,"width":1280,"height":720}'
        $recordingStarted = $true
    }
    $null = Post @{ action='run_command'; command="card $SelectedId Hand" }
    $null = Post @{ action='run_command'; command='card KAREN_FALL Hand' }
    $state = Wait-State { param($s) @($s.player.hand | Where-Object id -eq 'KAREN_FALL').Count -gt 0 } 'Fall in hand'
    $before = @($state.player.hand | Where-Object id -eq $SelectedId).Count
    Capture 'before-fall'
    $fall = $state.player.hand | Where-Object id -eq 'KAREN_FALL' | Select-Object -Last 1
    $null = Post @{ action='play_card'; card_index=$fall.index }
    $selection = Wait-State { param($s) $s.state_type -eq 'hand_select' } 'Fall selection'
    Capture 'fall-selection'
    $selected = $selection.hand_select.cards | Where-Object id -eq $SelectedId | Select-Object -Last 1
    if ($null -eq $selected) { throw "Selected card unavailable: $SelectedId" }
    $null = Post @{ action='combat_select_card'; card_index=$selected.index }
    $selection = Get-State
    if ($selection.state_type -eq 'hand_select') { $null = Post @{ action='combat_confirm_selection' } }
    $state = Wait-State { param($s) $s.state_type -eq 'monster' -and $s.battle.is_play_phase } 'selection completion'
    Capture 'after-fall'
    if ($Record) { Start-Sleep -Seconds 5 } # Let the idle VFX read clearly in the preview.
    $state = Get-State
    $afterFall = @($state.player.hand | Where-Object id -eq $SelectedId).Count
    $null = Post @{ action='run_command'; command='karen_check_hand' }
    $null = Post @{ action='run_command'; command='card KAREN_TOWER_OF_PROMISE Hand' }
    $state = Wait-State { param($s) @($s.player.hand | Where-Object id -eq 'KAREN_TOWER_OF_PROMISE').Count -gt 0 } 'Tower in hand'
    Start-Sleep -Milliseconds 650
    $tower = $state.player.hand | Where-Object id -eq 'KAREN_TOWER_OF_PROMISE' | Select-Object -Last 1
    $null = Post @{ action='play_card'; card_index=$tower.index }
    $state = Wait-State { param($s) $s.state_type -eq 'monster' -and $s.battle.is_play_phase } 'Tower completion'
    Capture 'after-tower'
    $state = Get-State
    $afterTower = @($state.player.hand | Where-Object id -eq $SelectedId).Count
    $null = Post @{ action='run_command'; command='karen_check_hand' }

    $restoredCard = $state.player.hand | Where-Object id -eq $SelectedId | Select-Object -First 1
    if ($SelectedId -eq 'KAREN_DEFEND') {
        $null = Post @{ action='play_card'; card_index=$restoredCard.index }
        Capture 'after-play-returned-card'
        $null = Post @{ action='run_command'; command='karen_check_hand' }
    }
    if ($OrbitPreview) {
        # A short visual sample for separate inventory stars, without the full mode showcase.
        $null = Post @{ action='run_command'; command='energy 20' }
        for ($i = 0; $i -lt 4; $i++) {
            $null = Post @{ action='run_command'; command='card KAREN_DODGE Hand' }
            $state = Wait-State { param($s) @($s.player.hand | Where-Object id -eq 'KAREN_DODGE').Count -gt 0 } 'orbit Dodge'
            Start-Sleep -Milliseconds 650
            $card = $state.player.hand | Where-Object id -eq 'KAREN_DODGE' | Select-Object -Last 1
            $null = Post @{ action='play_card'; card_index=$card.index }
            Start-Sleep -Milliseconds 700
        }
        Capture 'orbit-four'
        $null = Post @{ action='run_command'; command='karen_check_hand' }
        if ($Record) { Start-Sleep -Seconds 4 }
        $null = Post @{ action='run_command'; command='card KAREN_TOWER_OF_PROMISE Hand' }
        $state = Wait-State { param($s) @($s.player.hand | Where-Object id -eq 'KAREN_TOWER_OF_PROMISE').Count -gt 0 } 'orbit Tower'
        Start-Sleep -Milliseconds 650
        $card = $state.player.hand | Where-Object id -eq 'KAREN_TOWER_OF_PROMISE' | Select-Object -Last 1
        $null = Post @{ action='play_card'; card_index=$card.index }
        Capture 'orbit-return'
        $null = Post @{ action='run_command'; command='karen_check_hand' }
    }
    if ($Showcase) {
        # Use actual card effects: token creation, more than ten stored cards, bulk retrieval.
        for ($i = 0; $i -lt 13; $i++) {
            $null = Post @{ action='run_command'; command='card KAREN_DODGE Hand' }
            $state = Wait-State { param($s) @($s.player.hand | Where-Object id -eq 'KAREN_DODGE').Count -gt 0 } 'Dodge'
            Start-Sleep -Milliseconds 650 # Console creation returns before the native hand-entry tween.
            $null = Post @{ action='run_command'; command='karen_check_hand' }
            $card = $state.player.hand | Where-Object id -eq 'KAREN_DODGE' | Select-Object -Last 1
            $null = Post @{ action='play_card'; card_index=$card.index }
            Start-Sleep -Milliseconds 700
        }
        Capture 'showcase-thirteen'
        $null = Post @{ action='run_command'; command='karen_check_promise_pile KAREN_CONFRONT' }
        $null = Post @{ action='run_command'; command='karen_check_hand' }
        $null = Post @{ action='run_command'; command='card KAREN_TOWER_OF_PROMISE Hand' }
        $state = Wait-State { param($s) @($s.player.hand | Where-Object id -eq 'KAREN_TOWER_OF_PROMISE').Count -gt 0 } 'bulk Tower'
        Start-Sleep -Milliseconds 650
        $card = $state.player.hand | Where-Object id -eq 'KAREN_TOWER_OF_PROMISE' | Select-Object -Last 1
        $null = Post @{ action='play_card'; card_index=$card.index }
        Capture 'showcase-bulk-return'
        $null = Post @{ action='run_command'; command='karen_check_hand' }
        $null = Post @{ action='run_command'; command='energy 20' }
        foreach ($modeCard in @('KAREN_BURN','KAREN_PAST_AND_FUTURE','KAREN_STAGE_REPRODUCE','KAREN_VOID')) {
            $state = Get-State
            if (@($state.player.hand).Count -ge 10) {
                $id = $state.player.hand[0].id
                $null = Post @{ action='run_command'; command="remove_card $id Hand" }
                Start-Sleep -Milliseconds 700
            }
            $null = Post @{ action='run_command'; command="card $modeCard Hand" }
            $state = Wait-State { param($s) @($s.player.hand | Where-Object id -eq $modeCard).Count -gt 0 } 'mode card'
            Start-Sleep -Milliseconds 650
            $card = $state.player.hand | Where-Object id -eq $modeCard | Select-Object -Last 1
            $null = Post @{ action='play_card'; card_index=$card.index }
            if ($modeCard -eq 'KAREN_STAGE_REPRODUCE') {
                $ready = $false
                for ($j = 0; $j -lt 60; $j++) {
                    $check = Invoke-RestMethod -Uri $url -Method Post -ContentType 'application/json' -Body '{"action":"run_command","command":"karen_check_promise_pile KAREN_CONTINUE"}'
                    if ($check.status -eq 'ok') { $ready = $true; break }
                    Start-Sleep -Milliseconds 500
                }
                if (-not $ready) { throw 'Infinite replenishment did not finish' }
            }
            Capture "showcase-$modeCard"
            $null = Post @{ action='run_command'; command='karen_check_hand' }
        }
    }
    if ($recordingStarted) {
        $null = Invoke-RestMethod -Uri "http://127.0.0.1:$port/api/v1/recording/stop" -Method Post
        for ($i = 0; $i -lt 60; $i++) {
            $recording = Invoke-RestMethod -Uri "http://127.0.0.1:$port/api/v1/recording"
            if ($recording.status -in @('completed','failed')) { break }
            Start-Sleep -Milliseconds 500
        }
        $recording | ConvertTo-Json -Depth 10 | Set-Content -Encoding UTF8 (Join-Path $run 'recording.json')
        if ($recording.status -ne 'completed') { throw "Recording failed: $($recording | ConvertTo-Json -Compress)" }
        $videoPath = Join-Path $run "promise-constellation-$Environment.mp4"
        Copy-Item -LiteralPath $recording.output_path -Destination $videoPath
        $recordingStarted = $false
    }
    $result = [PSCustomObject]@{
        video = $videoPath
        result = if ($afterFall -eq $before - 1 -and $afterTower -eq $before -and $placementFailures.Count -eq 0) { 'pass' } else { 'fail' }
        placementFailures = $placementFailures
        selected = $SelectedId
        countBefore = $before
        countAfterFall = $afterFall
        countAfterTower = $afterTower
        log = Join-Path $run 'godot.log'
    }
    $result | ConvertTo-Json -Compress
    $result | ConvertTo-Json | Set-Content -Encoding UTF8 (Join-Path $run 'report.json')
    if ($result.result -ne 'pass') { exit 1 }
}
finally {
    if ($recordingStarted) {
        try {
            $null = Invoke-RestMethod -Uri "http://127.0.0.1:$port/api/v1/recording/stop" -Method Post -TimeoutSec 5
            for ($j = 0; $j -lt 40; $j++) {
                $rec = Invoke-RestMethod -Uri "http://127.0.0.1:$port/api/v1/recording" -TimeoutSec 5
                if ($rec.status -in @('completed','failed')) { break }
                Start-Sleep -Milliseconds 500
            }
            $rec | ConvertTo-Json -Depth 10 | Set-Content -Encoding UTF8 (Join-Path $run 'recording-interrupted.json')
            if ($rec.status -eq 'completed') { Copy-Item -LiteralPath $rec.output_path -Destination (Join-Path $run 'interrupted.mp4') }
        } catch { Write-Warning "Recording cleanup: $_" }
    }
    if ($process) {
        $running = Get-Process -Id $process.Id -ErrorAction SilentlyContinue
        if ($running) {
            if (-not $running.Path.Equals($exe,[StringComparison]::OrdinalIgnoreCase)) { throw 'Unexpected process path; not stopping it' }
            Stop-Process -Id $process.Id
            $running.WaitForExit(5000) | Out-Null
        }
    }
}
