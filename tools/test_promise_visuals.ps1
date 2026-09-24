param(
    [int]$ClientId = 66032,
    [ValidateSet('KAREN_DEFEND', 'KAREN_STRIKE')]
    [string]$SelectedId = 'KAREN_DEFEND',
    [ValidateSet('stable', 'beta')]
    [string]$Environment = 'beta'
)

$ErrorActionPreference = 'Stop'
$project = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$root = Join-Path $project 'artifacts\test-promise-visuals'
$branch = if ($Environment -eq 'stable') { 'stable' } else { 'beta' }
$game = Join-Path $project "artifacts\dev-game\$branch\game"
$exe = Join-Path $game 'SlayTheSpire2.exe'
$run = Join-Path $root ([DateTime]::UtcNow.ToString("yyyyMMdd-HHmmss") + "-$Environment-" + [guid]::NewGuid().ToString("N").Substring(0,6))
$port = if ($Environment -eq 'stable') { 15627 } else { 15628 }
$url = "http://127.0.0.1:$port/api/v1/singleplayer"
$process = $null

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
    $state = Get-State
    $afterFall = @($state.player.hand | Where-Object id -eq $SelectedId).Count
    $null = Post @{ action='run_command'; command='karen_check_hand' }
    $null = Post @{ action='run_command'; command='card KAREN_TOWER_OF_PROMISE Hand' }
    $state = Wait-State { param($s) @($s.player.hand | Where-Object id -eq 'KAREN_TOWER_OF_PROMISE').Count -gt 0 } 'Tower in hand'
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
    $result = [PSCustomObject]@{
        result = if ($afterFall -eq $before - 1 -and $afterTower -eq $before) { 'pass' } else { 'fail' }
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
    if ($process) {
        $running = Get-Process -Id $process.Id -ErrorAction SilentlyContinue
        if ($running) {
            if (-not $running.Path.Equals($exe,[StringComparison]::OrdinalIgnoreCase)) { throw 'Unexpected process path; not stopping it' }
            Stop-Process -Id $process.Id
            $running.WaitForExit(5000) | Out-Null
        }
    }
}
