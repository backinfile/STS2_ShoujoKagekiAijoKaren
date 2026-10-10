param(
    [ValidateSet('stable','beta')][string]$Environment='beta',
    [Parameter(Mandatory=$true)][string]$GameLog,
    [switch]$Record,
    [switch]$FinishRefill
)
# Run against an isolated Karen combat already in Void with four stored cards.
# Requires the disposable tools/promise-transform plugin (void_orbit_check).
$ErrorActionPreference='Stop'
$project=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$port=if($Environment -eq 'stable'){15627}else{15628}
if(-not (Select-String -LiteralPath $GameLog -Pattern '\[Karen STS2 MCP\].*server started' -Quiet -Encoding UTF8)) {
    throw 'KarenSTS2MCP startup is not confirmed'
}
$url="http://127.0.0.1:$port/api/v1/singleplayer"
$root=Join-Path $project ('artifacts/void-orbit-checks/'+[DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss')+'-'+$Environment)
New-Item -ItemType Directory -Path $root -Force | Out-Null
$checks=[Collections.Generic.List[object]]::new()
$recordingStarted=$false
function State { Invoke-RestMethod -Uri "$url`?format=json" -TimeoutSec 5 }
function Post([hashtable]$body) {
    $reply=Invoke-RestMethod -Uri $url -Method Post -ContentType 'application/json' -Body ($body|ConvertTo-Json -Compress) -TimeoutSec 15
    if($reply.status -ne 'ok'){throw ($reply|ConvertTo-Json -Compress)}
    return $reply
}
function Snapshot([string]$name) {
    State|ConvertTo-Json -Depth 30|Set-Content -LiteralPath (Join-Path $root "$name.json") -Encoding UTF8
    Invoke-WebRequest -Uri "http://127.0.0.1:$port/api/v1/screenshot" -OutFile (Join-Path $root "$name.png") -TimeoutSec 15
}
function Check([string]$name,[int]$count) {
    # Power hooks can publish the new model cards before queued entry VFX start.
    # Wait for the visible stars as well as the authoritative count.
    $ready=$false
    for($attempt=0;$attempt -lt 80;$attempt++){
        $orbit=Invoke-RestMethod -Uri $url -Method Post -ContentType 'application/json' -Body '{"action":"run_command","command":"void_orbit_check"}' -TimeoutSec 10
        if($orbit.status -eq 'ok' -and ($orbit.message|ConvertFrom-Json).modelCount -eq $count){$ready=$true;break}
        Start-Sleep -Milliseconds 150
    }
    if($ready){
        Start-Sleep -Milliseconds 650 # Finish the last 0.48-second replenishment flight.
        $orbit=Post @{action='run_command';command='void_orbit_check'}
    }
    Snapshot $name
    if(-not $ready){throw "$name did not settle: $($orbit|ConvertTo-Json -Compress)"}
    $data=$orbit.message|ConvertFrom-Json
    if($data.modelCount -ne $count){throw "$name expected $count cards, found $($data.modelCount)"}
    $hand=Post @{action='run_command';command='karen_check_hand'}
    $checks.Add([pscustomobject]@{name=$name;expected=$count;orbit=$data;hand=($hand.message|ConvertFrom-Json)})
    $checks|ConvertTo-Json -Depth 12|Set-Content -LiteralPath (Join-Path $root 'checks.json') -Encoding UTF8
    Write-Output "$name : $count card stars"
}
function Play([string]$id) {
    Snapshot "before-$id"
    $null=Post @{action='run_command';command="card $id Hand"}
    Start-Sleep -Milliseconds 750
    $s=State
    $card=$s.player.hand|Where-Object id -eq $id|Select-Object -Last 1
    if($null -eq $card){throw "No $id in hand"}
    $null=Post @{action='play_card';card_index=$card.index}
}
try {
    $s=State
    $expectedInitial=if($FinishRefill){10}else{4}
    if($s.state_type -ne 'monster' -or $s.player.draw_pile_count -ne $expectedInitial){throw "Start in Void with $expectedInitial Draw cards"}
    if($Record){
        $null=Invoke-RestMethod -Uri "http://127.0.0.1:$port/api/v1/recording/start" -Method Post -ContentType 'application/json' -Body '{"fps":30,"max_duration_seconds":120,"width":1280,"height":720}'
        $recordingStarted=$true
    }
    $null=Post @{action='run_command';command='energy 99'}
    if(-not $FinishRefill){
    Check 'initial-four' 4
    $first=$checks[0].orbit.positions
    Start-Sleep -Seconds 1
    $second=((Post @{action='run_command';command='void_orbit_check'}).message|ConvertFrom-Json).positions
    if(($first|ConvertTo-Json -Compress) -eq ($second|ConvertTo-Json -Compress)){throw 'Orbit positions did not change'}

    Snapshot 'before-draw-one'
    $null=Post @{action='run_command';command='draw 1'}
    Check 'after-draw-one' 3
    Play 'KAREN_DODGE'
    Check 'after-dodge' 4
    Play 'KAREN_FALL'
    for($i=0;$i -lt 60;$i++){
        $s=State
        if($s.state_type -eq 'hand_select'){break}
        Start-Sleep -Milliseconds 100
    }
    if($s.state_type -ne 'hand_select'){throw 'Fall selection missing'}
    $selected=$s.hand_select.cards|Select-Object -First 1
    $null=Post @{action='combat_select_card';card_index=$selected.index}
    if((State).state_type -eq 'hand_select'){$null=Post @{action='combat_confirm_selection'}}
    Check 'after-fall' 5
    Play 'KAREN_TOWER_OF_PROMISE'
    Check 'after-tower-empty' 0

    # Make room for a controlled Star Guide batch; use actual card effects below.
    for($i=0;$i -lt 15;$i++){
        $s=State
        if(@($s.player.hand).Count -eq 0){break}
        $id=$s.player.hand[0].id
        $null=Post @{action='run_command';command="remove_card $id Hand"}
        Start-Sleep -Milliseconds 250
    }
    for($i=0;$i -lt 2;$i++){$null=Post @{action='run_command';command='card KAREN_SHINE_STRIKE Hand'};Start-Sleep -Milliseconds 600}
    Play 'KAREN_STAR_GUIDE'
    Check 'after-star-guide' 2
    Play 'KAREN_BURN'
    Check 'after-burn' 2
    Play 'KAREN_PAST_AND_FUTURE'
    Check 'after-past-and-future' 2
    Play 'KAREN_STAGE_REPRODUCE'
    }
    Check 'after-reproduction' 10
    Snapshot 'before-refill-draw'
    $null=Post @{action='run_command';command='draw 1'}
    Check 'after-refill-draw' 10
    [pscustomobject]@{environment=$Environment;passed=$true;checks=$checks.Count;gameLog=$GameLog;run=$root}|
        ConvertTo-Json|Set-Content -LiteralPath (Join-Path $root 'report.json') -Encoding UTF8
} finally {
    if($recordingStarted){
        $null=Invoke-RestMethod -Uri "http://127.0.0.1:$port/api/v1/recording/stop" -Method Post
        for($i=0;$i -lt 80;$i++){
            $recording=Invoke-RestMethod -Uri "http://127.0.0.1:$port/api/v1/recording"
            if($recording.status -in @('completed','failed')){break}
            Start-Sleep -Milliseconds 250
        }
        $recording|ConvertTo-Json -Depth 8|Set-Content -LiteralPath (Join-Path $root 'recording.json') -Encoding UTF8
        if($recording.status -eq 'completed'){Copy-Item -LiteralPath $recording.output_path -Destination (Join-Path $root 'cards.mp4')}
    }
    Write-Output "Evidence: $root"
}
