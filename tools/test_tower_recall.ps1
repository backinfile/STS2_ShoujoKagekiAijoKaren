param(
    [ValidateSet('stable','beta')][string]$Environment='stable',
    [string]$GameDirectory='',
    [int]$McpPort=0,
    [ValidateSet('normal','fast')][string]$NativeSpeed='normal',
    [ValidateSet('vulkan','opengl3')][string]$RenderingDriver='vulkan',
    [switch]$SkipCardChecks,
    [switch]$InteractionOnly,
    [switch]$ResolutionOnly,
    [switch]$VfxShowcase,
    [switch]$ReuseFixture
)
$ErrorActionPreference='Stop'
$project=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$game=Join-Path $project "artifacts/dev-game/$Environment/game"
if ($GameDirectory) {$game=[IO.Path]::GetFullPath($GameDirectory)}
$exe=Join-Path $game 'SlayTheSpire2.exe'
$port=if ($Environment -eq 'stable') {15627} else {15628}
if ($McpPort) {$port=$McpPort}
$variant=if ($Environment -eq 'stable') {'Stable'} else {'Beta'}
$url="http://127.0.0.1:$port/api/v1/singleplayer"
$run=Join-Path $project ("artifacts/tower-recall/"+[DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss')+"-$Environment")
$plugin=Join-Path $game 'mods/KarenPromiseTransformTests'
$output=Join-Path $run 'fixture'
$process=$null
$recording=$false
$checks=[Collections.Generic.List[object]]::new()
if (Get-NetTCPConnection -LocalPort $port -State Listen -ErrorAction SilentlyContinue) { throw "Port $port occupied" }
if ((Test-Path -LiteralPath $plugin) -and @(Get-ChildItem -LiteralPath $plugin -Force).Count -gt 0 -and -not $ReuseFixture) { throw "Fixture already installed: $plugin" }
New-Item -ItemType Directory -Path $run,$plugin -Force | Out-Null
function State { Invoke-RestMethod "$url`?format=json" -TimeoutSec 5 }
function Post([hashtable]$action) {
    $result=Invoke-RestMethod $url -Method Post -ContentType 'application/json' -Body ($action|ConvertTo-Json -Compress) -TimeoutSec 20
    if ($result.status -ne 'ok') { throw "$($action|ConvertTo-Json -Compress): $($result|ConvertTo-Json -Compress -Depth 8)" }
    return $result
}
function Cmd([string]$command) { Post @{action='run_command';command=$command} }
function Wait-State([scriptblock]$condition) {
    for($i=0;$i -lt 100;$i++) { try { $s=State; if (& $condition $s) {return $s} } catch {}; Start-Sleep -Milliseconds 250 }
    throw 'State timeout'
}
function Shot([string]$name) {
    Invoke-WebRequest "http://127.0.0.1:$port/api/v1/screenshot" -OutFile (Join-Path $run "$name.png") -TimeoutSec 15 | Out-Null
    State | ConvertTo-Json -Depth 30 | Set-Content -Encoding UTF8 (Join-Path $run "$name.json")
}
function Probe { (Cmd 'tower_recall status').message | ConvertFrom-Json }
function Wait-Fixture {
    for($i=0;$i -lt 100;$i++) {
        $probe=Probe
        if ($probe.preparationError) {throw $probe.preparationError}
        if (-not $probe.preparing) {Start-Sleep -Milliseconds 500;return}
        Start-Sleep -Milliseconds 250
    }
    throw 'Fixture preparation timeout'
}
function Check([string]$name,[bool]$passed,[object]$detail) {
    $checks.Add(@{case=$name;passed=$passed;detail=$detail})
    $checks | ConvertTo-Json -Depth 12 | Set-Content -Encoding UTF8 (Join-Path $run 'checks.json')
    if (-not $passed) { throw "Check failed: $name $($detail|ConvertTo-Json -Compress)" }
}
function Check-Hand([string]$name) {
    $hand=(Cmd 'karen_check_hand').message|ConvertFrom-Json
    Check $name ($hand.modelCount -eq $hand.visualCount -and @($hand.missing).Count -eq 0 -and @($hand.unexpected).Count -eq 0 -and @($hand.unexpectedPileIcons).Count -eq 0) $hand
}
try {
    & dotnet build (Join-Path $PSScriptRoot 'promise-transform/KarenPromiseTransformTests.csproj') "-p:Variant=$variant" "-p:Game=$game" -o $output --nologo -v:q
    if ($LASTEXITCODE -ne 0) {throw 'Fixture build failed'}
    Copy-Item -LiteralPath (Join-Path $output 'KarenPromiseTransformTests.dll') -Destination $plugin
    @{id='KarenPromiseTransformTests';name='Promise recall test';author='Karen tests';version='1.0.0';has_dll=$true;has_pck=$false;affects_gameplay=$false;dependencies=@(@{id='ShoujoKagekiAijoKaren'})} | ConvertTo-Json -Depth 4 | Set-Content -Encoding UTF8 (Join-Path $plugin 'KarenPromiseTransformTests.json')
    $roaming=Join-Path $run 'user/AppData/Roaming'
    $local=Join-Path $run 'user/AppData/Local'
    $settings=Join-Path $roaming 'SlayTheSpire2/default/66041/settings.save'
    New-Item -ItemType Directory -Path (Split-Path $settings -Parent),$local -Force | Out-Null
    Copy-Item -LiteralPath (Join-Path $project "artifacts/dev-game/$Environment/user/AppData/Roaming/SlayTheSpire2/default/1/settings.save") -Destination $settings
    $oldRoaming=$env:APPDATA; $oldLocal=$env:LOCALAPPDATA
    try {
        $env:APPDATA=$roaming; $env:LOCALAPPDATA=$local
        $renderer=if($RenderingDriver -eq 'opengl3'){'--rendering-method gl_compatibility --rendering-driver opengl3'}else{'--rendering-driver vulkan'}
        $process=Start-Process -FilePath $exe -WorkingDirectory $game -WindowStyle Hidden -PassThru -ArgumentList "$renderer --force-steam=off --clientId=66041 --log-file `"$(Join-Path $run 'godot.log')`""
    } finally { $env:APPDATA=$oldRoaming; $env:LOCALAPPDATA=$oldLocal }
    $started=$false
    for($i=0;$i -lt 100;$i++) {
        if ((Test-Path (Join-Path $run 'godot.log')) -and (Select-String -LiteralPath (Join-Path $run 'godot.log') -Encoding UTF8 -Pattern '\[Karen STS2 MCP\].*server started' -Quiet)) {$started=$true;break}
        Start-Sleep -Milliseconds 250
    }
    if (-not $started) {throw 'Karen MCP did not start'}
    $null=Wait-State {param($s) $s.menu_screen -eq 'main' -and @($s.options) -contains 'singleplayer'}
    $null=Invoke-RestMethod "http://127.0.0.1:$port/api/v1/settings" -Method Post -ContentType 'application/json' -Body '{"fullscreen":false,"window_width":1280,"window_height":720,"muted":false}'
    $null=Wait-State {param($s) $s.menu_screen -eq 'main' -and @($s.options) -contains 'singleplayer'}
    foreach($option in @('singleplayer','KAREN','embark')) {$null=Post @{action='menu_select';option=$option}}
    if ((State).menu_screen -eq 'tutorial_prompt') {$null=Post @{action='menu_select';option='no'}}
    $null=Wait-State {param($s) $s.state_type -eq 'map'}
    # The map snapshot can become available before the travel animation unlocks.
    $entered=$false
    for($attempt=0;$attempt -lt 40;$attempt++) {
        $travel=Invoke-RestMethod $url -Method Post -ContentType 'application/json' -Body '{"action":"choose_map_node","index":0}' -TimeoutSec 10
        if($travel.status -eq 'ok'){$entered=$true;break}
        if($travel.error -notmatch 'Already traveling'){throw ($travel|ConvertTo-Json -Compress)}
        Start-Sleep -Milliseconds 250
    }
    if(-not $entered){throw 'Map travel did not unlock'}
    $null=Wait-State {param($s) $s.state_type -eq 'monster' -and $s.battle.is_play_phase}
    Start-Sleep -Milliseconds 1400 # Native opening draws finish after the play-phase flag appears.
    $null=Cmd "native_speed $NativeSpeed"
    $null=Cmd 'tower_recall prepare'
    Wait-Fixture
    if ($VfxShowcase) {
        $chapters=[Collections.Generic.List[object]]::new()
        $null=Invoke-RestMethod "http://127.0.0.1:$port/api/v1/recording/start" -Method Post -ContentType 'application/json' -Body '{"fps":30,"max_duration_seconds":120,"width":1280,"height":720,"audio":true}'
        $recording=$true
        $clock=[Diagnostics.Stopwatch]::StartNew()
        foreach($case in @('cancel','normal','repeat','small-flip','fast','instant','full','empty','void')) {
            $speed=if($case -in @('fast','instant')){$case}else{'normal'}
            $null=Cmd "tower_recall speed $speed"
            $mode=if($case -in @('full','empty','void')){$case}else{'prepare'}
            $null=Cmd "tower_recall $mode"
            Wait-Fixture
            if($case -eq 'small-flip'){$null=Cmd 'promise_transform small_flip'}
            Start-Sleep -Milliseconds 200
            Shot "$case-before"
            Check-Hand "$case-before-hand"
            $chapters.Add(@{case=$case;time=$clock.Elapsed.TotalSeconds})
            $null=Cmd 'tower_recall drag KAREN_TOWER_OF_PROMISE'
            Start-Sleep -Milliseconds 800
            if($case -eq 'cancel') {
                Shot 'cancel-held'
                $null=Cmd 'tower_recall cancel'
                Start-Sleep -Milliseconds 500
                $probe=Probe
                Check 'cancel-clears-preview' ($probe.alpha -le 0.01 -and $probe.active -eq 0 -and $probe.current -eq 0 -and $probe.crest -eq 0 -and $probe.promise -eq 3) $probe
            } else {
                $null=Cmd 'tower_recall play'
                Start-Sleep -Milliseconds 2300
                $probe=Probe
                $expected=if($case -eq 'empty'){0}elseif($case -eq 'full'){1}else{3}
                $visual=if($case -eq 'instant'){0}else{$expected}
                Check "$case-recall" ($probe.seen -eq $visual -and $probe.landed -eq $visual -and $probe.current -eq 0 -and $probe.crest -eq 0 -and $probe.active -eq 0 -and $probe.alpha -le 0.01) $probe
                Check "$case-model-result" ($probe.hand -eq $(if($case -eq 'full'){10}else{$expected}) -and $probe.promise -eq $(if($case -eq 'full'){2}else{0})) $probe
            }
            Shot "$case-after"
            Check-Hand "$case-after-hand"
            if($case -eq 'small-flip'){$null=Cmd 'promise_transform normal'}
        }
        $chapters.Add(@{case='end';time=$clock.Elapsed.TotalSeconds})
        $chapters | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $run 'chapters.json') -Encoding UTF8
        $null=Invoke-RestMethod "http://127.0.0.1:$port/api/v1/recording/stop" -Method Post
        for($i=0;$i -lt 100;$i++) {
            $rec=Invoke-RestMethod "http://127.0.0.1:$port/api/v1/recording"
            if($rec.status -in @('completed','failed')){break}
            Start-Sleep -Milliseconds 300
        }
        if($rec.status -ne 'completed' -or -not $rec.audio -or $rec.audio_source -ne 'game_process'){throw 'Game-process audio recording failed'}
        $rec | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath (Join-Path $run 'recording.json') -Encoding UTF8
        Copy-Item -LiteralPath $rec.output_path -Destination (Join-Path $run "tower-promise-$Environment.mp4")
        $recording=$false
        Write-Output "PASS VFX showcase $Environment : $($checks.Count) checks; $run"
        return
    }
    if ($ResolutionOnly) {
        foreach($id in @('KAREN_FALL','KAREN_DEFEND','KAREN_STRIKE')) {$null=Cmd "card $id Hand"}
        Start-Sleep -Milliseconds 700
        Shot 'resolution-before'
        $null=Invoke-RestMethod "http://127.0.0.1:$port/api/v1/recording/start" -Method Post -ContentType 'application/json' -Body '{"fps":30,"max_duration_seconds":60,"width":1280,"height":720,"audio":true}'
        $recording=$true
        Start-Sleep -Seconds 1
        $null=Cmd 'tower_recall drag KAREN_FALL'
        Start-Sleep -Seconds 2
        $null=Cmd 'tower_recall play'
        $null=Wait-State {param($s) $s.state_type -eq 'hand_select'}
        Start-Sleep -Seconds 3
        Shot 'fall-awaiting-choice'
        $probe=Probe
        Check 'fall-lit-through-choice' ($probe.alpha -ge 0.99 -and $probe.profile -eq 'Put' -and $probe.resolving -eq 1 -and $probe.playFrames -gt 60 -and $probe.playMinimumAlpha -ge 0.99) $probe
        $null=Post @{action='combat_select_card';card_index=1}
        if((State).state_type -eq 'hand_select') {$null=Post @{action='combat_confirm_selection'}}
        $null=Wait-State {param($s) $s.state_type -eq 'monster' -and $s.battle.is_play_phase}
        Start-Sleep -Seconds 2
        Shot 'fall-resolved'
        $probe=Probe
        Check 'fall-fades-after-resolution' ($probe.alpha -le 0.01 -and $probe.resolving -eq 0 -and $probe.promise -eq 4 -and $probe.playMinimumAlpha -ge 0.99) $probe
        Check-Hand 'hand-after-fall'
        $null=Cmd 'tower_recall drag KAREN_TOWER_OF_PROMISE'
        Start-Sleep -Seconds 3
        $null=Cmd 'tower_recall play'
        Start-Sleep -Seconds 3
        Shot 'tower-resolved'
        $probe=Probe
        Check 'tower-resolves-after-fall' ($probe.seen -eq 4 -and $probe.landed -eq 4 -and $probe.current -eq 0 -and $probe.alpha -le 0.01 -and $probe.resolving -eq 0 -and $probe.playFrames -gt 0 -and $probe.playMinimumAlpha -ge 0.99) $probe
        Check-Hand 'hand-after-tower'
        $null=Cmd 'tower_recall click'
        Start-Sleep -Seconds 2
        $probe=Probe
        Check 'tower-click-still-opens' ($probe.screen -eq 'NCardPileScreen') $probe
        $null=Cmd 'tower_recall close'
        Start-Sleep -Seconds 1
        $null=Invoke-RestMethod "http://127.0.0.1:$port/api/v1/recording/stop" -Method Post
        for($i=0;$i -lt 100;$i++) {
            $rec=Invoke-RestMethod "http://127.0.0.1:$port/api/v1/recording"
            if($rec.status -in @('completed','failed')){break}
            Start-Sleep -Milliseconds 300
        }
        if ($rec.status -ne 'completed' -or -not $rec.audio -or $rec.audio_source -ne 'game_process') {throw 'Recording failed'}
        Copy-Item -LiteralPath $rec.output_path -Destination (Join-Path $run "resolution-$Environment.mp4")
        $rec | ConvertTo-Json -Depth 10 | Set-Content -Encoding UTF8 (Join-Path $run 'recording.json')
        $recording=$false
        Write-Output "PASS resolution $Environment : $($checks.Count) checks; $run"
        return
    }
    if ($InteractionOnly) {
        $null=Cmd 'card KAREN_FALL Hand'
        Start-Sleep -Milliseconds 500
        Shot 'idle'
        foreach($id in @('KAREN_FALL','KAREN_TOWER_OF_PROMISE')) {
            $null=Cmd "tower_recall drag $id"
            foreach($sample in @(1,6)) {
                Start-Sleep -Seconds $(if($sample -eq 1){1}else{5})
                $probe=Probe
                Check "held-$id-$sample-seconds" ($probe.inCardPlay -and $probe.preview -eq $id -and $probe.alpha -ge 0.99) $probe
                Shot "held-$id-$sample"
            }
            $null=Cmd 'tower_recall cancel'
            Start-Sleep -Milliseconds 350
            $probe=Probe
            Check "cancel-$id" (-not $probe.inCardPlay -and $probe.alpha -le 0.01) $probe
        }
        $null=Cmd 'tower_recall drag KAREN_TOWER_OF_PROMISE'
        $null=Cmd 'tower_recall play'
        Start-Sleep -Seconds 3
        Shot 'played-after-hold'
        $probe=Probe
        Check 'tower-recall-after-held-preview' ($probe.seen -eq 3 -and $probe.landed -eq 3 -and $probe.current -eq 0 -and $probe.alpha -le 0.01) $probe
        Check-Hand 'hand-after-recall'
        $null=Cmd 'tower_recall prepare'
        Wait-Fixture
        $null=Cmd 'tower_recall drag KAREN_TOWER_OF_PROMISE'
        Start-Sleep -Milliseconds 300
        $null=Cmd 'tower_recall click'
        Start-Sleep -Seconds 2
        $probe=Probe
        Check 'tower-click-during-card-play-does-not-open' ($null -eq $probe.screen) $probe
        Shot 'tower-click-during-card-play'
        $null=Cmd 'tower_recall cancel'
        Check-Hand 'hand-after-card-play-click'
        $null=Cmd 'tower_recall prepare'
        Wait-Fixture
        $null=Cmd 'tower_recall click outside'
        Start-Sleep -Milliseconds 400
        $probe=Probe
        Check 'outside-tower-does-not-open' ($null -eq $probe.screen) $probe
        $null=Cmd 'tower_recall click'
        Start-Sleep -Milliseconds 500
        Shot 'tower-click'
        $probe=Probe
        Check 'tower-opens-promise-pile' ($probe.screen -eq 'NCardPileScreen' -and $probe.screenTitle -match '约定' -and $probe.promise -eq 3) $probe
        $null=Cmd 'tower_recall close'
        Start-Sleep -Milliseconds 350
        Check-Hand 'hand-after-pile-view'
        $null=Cmd 'tower_recall empty'
        Wait-Fixture
        $null=Cmd 'tower_recall click'
        Start-Sleep -Milliseconds 500
        $probe=Probe
        Check 'tower-opens-empty-promise-pile' ($probe.screen -eq 'NCardPileScreen' -and $probe.promise -eq 0) $probe
        $null=Cmd 'tower_recall close'
        Start-Sleep -Milliseconds 350
        $null=Cmd 'tower_recall void'
        Wait-Fixture
        $null=Cmd 'tower_recall click'
        Start-Sleep -Milliseconds 500
        Shot 'void-tower-click'
        $probe=Probe
        Check 'void-tower-opens-draw-pile' ($probe.screen -eq 'NCardPileScreen' -and $probe.screenTitle -match '抽牌') $probe
        $null=Cmd 'tower_recall close'
        Write-Output "PASS interaction $Environment : $($checks.Count) checks; $run"
        return
    }
    # Test every active-transfer card, plus power and non-transfer negative controls.
    $profiles=@{
        Put=@('FALL','WHY','OUR_PROMISE','MEET_AGAIN','YESTERGLOW','NO_HESITATE','STAR_GUIDE','COURAGE_STRIKE','DODGE','LANDING','NEW_SITUATION','NEW_DAY','REVUE_DUET','RUN','STRETCHING')
        Take=@('PROMISE_DRAW','ON_STAGE','DANCE','BRIDGE')
        Both=@('WHOS_PROMISE','EXCHANGE_FATE','RAPID')
        None=@('AQUARIUM_CARD','HOLY_STAR','BACK_TO_BACK_CARD','WAKE_UP','DEFEND')
    }
    if (-not $SkipCardChecks) { foreach($profile in $profiles.Keys) { foreach($name in $profiles[$profile]) {
        $id="KAREN_$name"
        $null=Cmd "card $id Hand"
        Start-Sleep -Milliseconds 450
        $null=Cmd "tower_recall drag $id"
        Start-Sleep -Milliseconds 350
        $probe=Probe
        Check "drag-$name" ($probe.inCardPlay -and (($profile -eq 'None' -and $null -eq $probe.preview) -or ($profile -ne 'None' -and $probe.preview -eq $id -and $probe.profile -eq $profile -and $probe.alpha -gt 0.9))) $probe
        if ($name -eq 'FALL') {Shot 'hover-ordinary'}
        $null=Cmd 'tower_recall cancel'
        Start-Sleep -Milliseconds 350
        $probe=Probe
        Check "cancel-$name" (-not $probe.inCardPlay -and $probe.alpha -le 0.01) $probe
        if ($name -eq 'WAKE_UP') {
            $null=Cmd "tower_recall upgrade $id"
            $null=Cmd "tower_recall drag $id"
            Start-Sleep -Milliseconds 350
            $probe=Probe
            Check 'drag-WAKE_UP-upgraded' ($probe.preview -eq $id -and $probe.profile -eq 'Take') $probe
            $null=Cmd 'tower_recall cancel'
            Start-Sleep -Milliseconds 350
        }
        $null=Cmd "tower_recall retire $id"
    }}}
    $null=Invoke-RestMethod "http://127.0.0.1:$port/api/v1/recording/start" -Method Post -ContentType 'application/json' -Body '{"fps":30,"max_duration_seconds":180,"width":1280,"height":720,"audio":true}'
    $recording=$true
    foreach($mode in @('prepare','empty','full','void')) {
        $null=Cmd "tower_recall $mode"
        Wait-Fixture
        Shot "$mode-before"
        $null=Cmd 'tower_recall drag KAREN_TOWER_OF_PROMISE'
        Start-Sleep -Seconds 1
        $probe=Probe
        Check "$mode-tower-hover" ($probe.preview -eq 'KAREN_TOWER_OF_PROMISE' -and $probe.profile -eq 'Tower') $probe
        Shot "$mode-hover"
        $null=Cmd 'tower_recall play'
        Start-Sleep -Seconds 3
        Shot "$mode-after"
        $null=Cmd 'karen_check_hand'
        $probe=Probe
        $expected=if($mode -eq 'empty'){0}elseif($mode -eq 'full'){1}else{3}
        Check "$mode-recall" ($probe.seen -eq $expected -and $probe.landed -eq $expected -and $probe.current -eq 0 -and $probe.active -eq 0 -and $probe.alpha -le 0.01) $probe
    }
    $null=Invoke-RestMethod "http://127.0.0.1:$port/api/v1/recording/stop" -Method Post
    for($i=0;$i -lt 100;$i++) {
        $rec=Invoke-RestMethod "http://127.0.0.1:$port/api/v1/recording"
        if($rec.status -in @('completed','failed')){break}
        Start-Sleep -Milliseconds 300
    }
    if ($rec.status -ne 'completed') {throw 'Recording failed'}
    Copy-Item -LiteralPath $rec.output_path -Destination (Join-Path $run "tower-recall-$Environment.mp4")
    $recording=$false
    Write-Output "PASS $Environment : $($checks.Count) checks; $run"
} finally {
    if ($recording) {try {$null=Invoke-RestMethod "http://127.0.0.1:$port/api/v1/recording/stop" -Method Post -TimeoutSec 5} catch {}}
    if ($process) {
        $running=Get-Process -Id $process.Id -ErrorAction SilentlyContinue
        if ($running -and $running.Path -and $running.Path.Equals($exe,[StringComparison]::OrdinalIgnoreCase)) {Stop-Process -Id $process.Id; $running.WaitForExit(5000)|Out-Null}
    }
    if (-not $ReuseFixture) { foreach($file in @('KarenPromiseTransformTests.dll','KarenPromiseTransformTests.json')) {
        $path=Join-Path $plugin $file
        if(Test-Path -LiteralPath $path){Remove-Item -LiteralPath $path -Force}
    }
    if (@(Get-ChildItem -LiteralPath $plugin -Force).Count -eq 0) {Remove-Item -LiteralPath $plugin}
    }
}
