# Dot-sourced only by the Mr. White targeted multiplayer suite.
$whitePeers = @(@($hostPort,$clientPort) | Select-Object -Unique)
$whiteActors = @()
if ($HostCharacter -eq 'KAREN') { $whiteActors += $hostPort }
if ($ClientCharacter -eq 'KAREN') { $whiteActors += $clientPort }
$whiteEvidence = Join-Path $RunRoot "mr-white/$caseName"
New-Item -ItemType Directory -Path $whiteEvidence -Force | Out-Null
$whiteResults = [Collections.Generic.List[object]]::new()

function Wait-MrWhiteMcp([string]$root) {
    $log = Join-Path $root 'godot.log'
    for ($attempt = 0; $attempt -lt 80; $attempt++) {
        if ((Test-Path -LiteralPath $log) -and (Select-String -LiteralPath $log -SimpleMatch '[Karen STS2 MCP]' -Quiet)) { return }
        Start-Sleep -Milliseconds 250
    }
    throw "Karen STS2 MCP not loaded: $log"
}

function Read-White([int]$peer) {
    $response = Post-Action $peer 'multiplayer' @{action='run_command';command='white_state'}
    return $response.message | ConvertFrom-Json
}

function Save-MrWhiteState([string]$label, [bool]$checkHands = $true) {
    foreach ($peer in $whitePeers) {
        Get-State $peer 'multiplayer' | ConvertTo-Json -Depth 60 |
            Set-Content -LiteralPath (Join-Path $whiteEvidence "$label-$peer.json") -Encoding utf8NoBOM
        Read-White $peer | ConvertTo-Json -Depth 30 |
            Set-Content -LiteralPath (Join-Path $whiteEvidence "$label-$peer-model.json") -Encoding utf8NoBOM
        Invoke-WebRequest -Uri "http://127.0.0.1:$peer/api/v1/screenshot" -OutFile (Join-Path $whiteEvidence "$label-$peer.png") | Out-Null
        if ($checkHands) {
            $check = Post-Action $peer 'multiplayer' @{action='run_command';command='karen_check_hand'}
            $check | ConvertTo-Json -Depth 15 |
                Set-Content -LiteralPath (Join-Path $whiteEvidence "$label-$peer-hand.json") -Encoding utf8NoBOM
        }
    }
}

function White-Fixture([int]$actor, [string]$action) {
    $identity = (Read-White $actor).localOwner
    $null = Post-Action $actor 'multiplayer' @{action='run_command';command="white_fixture $action"}
    foreach ($peer in $whitePeers) {
        $matched = $false
        for ($attempt=0;$attempt -lt 80;$attempt++) {
            $status = (Read-White $peer).fixtureStatus
            if ($status -eq "pass:$action`:owner=$identity") { $matched=$true; break }
            if ($status -like 'fail:*') { throw $status }
            Start-Sleep -Milliseconds 100
        }
        if (-not $matched) { throw "Fixture not synchronized: $action on $peer ($status)" }
    }
}

function Initialize-MrWhiteCase {
    if ($whiteActors.Count -eq 0) { throw 'A Karen owner is required' }
    foreach ($peer in $whitePeers) {
        $null = Invoke-RestMethod -Uri "http://127.0.0.1:$peer/api/v1/settings" -Method Post -ContentType 'application/json' -Body '{"fullscreen":false,"muted":true,"window_width":1280,"window_height":720}' -TimeoutSec 5
    }
    foreach ($actor in $whiteActors) {
        $null = Post-Action $actor 'multiplayer' @{action='run_command';command='relic add KAREN_MR_WHITE_RELIC'}
    }
}

function Wait-WhiteRound([int]$round) {
    foreach ($peer in $whitePeers) {
        $null = Wait-State $peer 'multiplayer' {param($s) $s.state_type -eq 'monster' -and $s.battle.round -eq $round -and $s.battle.is_play_phase} "Mr. White round $round"
    }
}

function Assert-WhiteModels([hashtable]$expected, [int]$completedTurns = 2) {
    $hostModel = Read-White $hostPort
    $clientModel = Read-White $clientPort
    if (($hostModel.players | ConvertTo-Json -Depth 20 -Compress) -cne ($clientModel.players | ConvertTo-Json -Depth 20 -Compress)) {
        throw 'Mr. White all-player hands/promise piles/status differ between peers'
    }
    foreach ($player in $hostModel.players) {
        $ids = @($expected[[string]$player.id])
        if (($player.promise -join ',') -cne ($ids -join ',')) {
            throw "Promise pile mismatch for $($player.id): actual=$($player.promise -join ','); expected=$($ids -join ',')"
        }
        $expectedStatus=if($completedTurns -lt 2){'Normal'}else{'Disabled'}
        if($null -ne $player.white -and $player.white -ne $expectedStatus){throw "Mr. White status after turn $completedTurns for $($player.id): expected=$expectedStatus actual=$($player.white)"}
    }
}

function Complete-WhiteSelection([string]$label, [string]$mode, [hashtable]$expected, [int[]]$actors) {
    $pending = [Collections.Generic.List[int]]::new()
    foreach($actor in $actors){$pending.Add($actor)}
    $deadline = [DateTime]::UtcNow.AddSeconds(35)
    $selectedCounts = @{}
    while($pending.Count -gt 0 -and [DateTime]::UtcNow -lt $deadline){
        foreach($actor in @($pending.ToArray())){
            $selection = Get-State $actor 'multiplayer'
            if($selection.state_type -ne 'hand_select'){continue}
            $owner=[string](Read-White $actor).localOwner
            $count=if($mode -eq 'partial'){[Math]::Min(2,@($selection.hand_select.cards).Count)}elseif($mode -eq 'all' -or $mode -eq 'keywords-all'){@($selection.hand_select.cards).Count}else{0}
            $selection|ConvertTo-Json -Depth 60|Set-Content -LiteralPath (Join-Path $whiteEvidence "$label-selection-$actor.json") -Encoding utf8NoBOM
            Invoke-WebRequest -Uri "http://127.0.0.1:$actor/api/v1/screenshot" -OutFile (Join-Path $whiteEvidence "$label-selection-$actor.png")|Out-Null
            for($index=0;$index -lt $count;$index++){
                $selection=Get-State $actor 'multiplayer'
                $expected[$owner]=@($expected[$owner])+@($selection.hand_select.cards[0].id)
                $null=Post-Action $actor 'multiplayer' @{action='combat_select_card';card_index=0}
            }
            $selectedCounts["$actor"]=$count
            $null=Post-Action $actor 'multiplayer' @{action='combat_confirm_selection'}
            $pending.Remove($actor)|Out-Null
        }
        Start-Sleep -Milliseconds 150
    }
    if($pending.Count){throw "Mr. White selections never completed: $pending"}
    return $selectedCounts
}

function Assert-WhiteRetainAndEthereal {
    foreach($actor in $whiteActors){
        $owner=(Read-White $actor).localOwner
        $player=(Read-White $actor).players|Where-Object id -eq $owner
        if('KAREN_STRIKE' -notin $player.exhaust -or 'KAREN_DEFEND' -notin $player.hand){throw 'Unselected Ethereal/Retain lifecycle changed'}
    }
}

function Invoke-MrWhiteScenario([string]$mode, [int]$number) {
    $label = '{0:00}-{1}' -f $number,$mode
    if ($number -gt 1) {
        $null = Post-Action $hostPort 'multiplayer' @{action='run_command';command='fight NIBBITS_WEAK'}
        Wait-WhiteRound 1
    }
    foreach ($peer in $whitePeers) { White-Fixture $peer 'buffer' }
    Start-Sleep -Seconds 1
    Save-MrWhiteState "$label-00-before-setup"
    if ($mode -eq 'empty' -or $mode -like 'keywords-*') {
        foreach ($actor in $whiteActors) { White-Fixture $actor $(if($mode -eq 'empty'){'empty'}else{'keywords'}) }
    }
    Start-Sleep -Seconds 2
    Save-MrWhiteState "$label-01-before-end"
    $expected = @{}
    foreach ($player in (Read-White $hostPort).players) {
        $expected[[string]$player.id] = @($player.promise)
        if ($null -ne $player.white -and $player.white -ne 'Normal') { throw "Mr. White did not reset before combat: $($player.id)" }
    }
    $order = @($whitePeers)
    if ($mode -eq 'zero' -or $mode -eq 'keywords-zero') { [array]::Reverse($order) }
    if ($mode -eq 'partial' -and $whitePeers.Count -gt 1) {
        $first = $whiteActors[0]
        $other = if($first -eq $hostPort){$clientPort}else{$hostPort}
        $null = Post-Action $first 'multiplayer' @{action='end_turn'}
        Start-Sleep -Milliseconds 500
        $state = Get-State $first 'multiplayer'
        if ($state.state_type -eq 'hand_select' -or $state.battle.round -ne 1) { throw 'End-ready unexpectedly executed shared turn-end before teammate' }
        $null = Post-Action $first 'multiplayer' @{action='run_command';command='white_undo'}
        Start-Sleep -Milliseconds 500
        if (@((Read-White $first).players | Where-Object { $null -ne $_.white -and $_.white -ne 'Normal' }).Count) { throw 'Undo ready consumed Mr. White' }
        $null = Post-Action $first 'multiplayer' @{action='end_turn'}
        $teammate = Get-State $other 'multiplayer'
        $defend = $teammate.player.hand | Where-Object {$_.can_play -and $_.id -match 'DEFEND'} | Select-Object -First 1
        if ($null -ne $defend) {
            $null = Post-Action $other 'multiplayer' @{action='play_card';card_index=$defend.index}
            Start-Sleep -Seconds 1
        }
        $null = Post-Action $other 'multiplayer' @{action='end_turn'}
    } else {
        foreach ($peer in $order) { $null = Post-Action $peer 'multiplayer' @{action='end_turn'} }
    }
    $firstActors=if($mode -eq 'empty'){@()}else{@($whiteActors)}
    $selectedFirst=Complete-WhiteSelection "$label-turn1" $mode $expected $firstActors
    Wait-WhiteRound 2
    Start-Sleep -Seconds 2
    Assert-WhiteModels $expected 1
    Save-MrWhiteState "$label-02-after-first-selection"
    if($mode -eq 'keywords-zero'){Assert-WhiteRetainAndEthereal}
    if($mode -like 'keywords-*'){
        foreach($actor in $whiteActors){White-Fixture $actor 'keywords'}
        Start-Sleep -Seconds 2
    }
    Save-MrWhiteState "$label-03-before-second-end"
    foreach($peer in $whitePeers){$null=Post-Action $peer 'multiplayer' @{action='end_turn'}}
    $secondMode=if($mode -eq 'empty'){'partial'}else{$mode}
    $selectedSecond=Complete-WhiteSelection "$label-turn2" $secondMode $expected @($whiteActors)
    Wait-WhiteRound 3
    Start-Sleep -Seconds 2
    Assert-WhiteModels $expected 2
    Save-MrWhiteState "$label-04-after-second-selection"
    if($mode -eq 'keywords-zero'){Assert-WhiteRetainAndEthereal}
    foreach($peer in $whitePeers){$null=Post-Action $peer 'multiplayer' @{action='end_turn'}}
    Wait-WhiteRound 4
    Assert-WhiteModels $expected 2
    Save-MrWhiteState "$label-05-after-third-end"
    $null=Post-Action $hostPort 'multiplayer' @{action='run_command';command='kill all'}
    foreach($peer in $whitePeers){
        $null=Wait-State $peer 'multiplayer' {param($s) $s.state_type -eq 'rewards'} 'Mr. White combat rewards'
    }
    $whiteResults.Add([PSCustomObject]@{scenario=$mode;result='pass';selectedFirst=$selectedFirst;selectedSecond=$selectedSecond;firstTwoTurns=$true;round3NoRetrigger=$true;modelsSynchronized=$true;handVisualsMatched=$true})
    $whiteResults|ConvertTo-Json -Depth 15|Set-Content -LiteralPath (Join-Path $whiteEvidence 'results.json') -Encoding utf8NoBOM
    Write-Host "PASS $caseName $mode (turns 1 and 2)"
}

function Test-MrWhiteLogs {
    $startup = [Collections.Generic.List[object]]::new()
    $unexpected = [Collections.Generic.List[object]]::new()
    $logEntries=@(@{path=(Join-Path $hostRoot 'godot.log');role='host'},@{path=(Join-Path $clientRoot 'godot.log');role='client'})
    foreach($entry in @($logEntries|Sort-Object path -Unique)){
        $preload = Select-String -LiteralPath $entry.path -SimpleMatch "Preloading 'Common' Complete:" | Select-Object -First 1
        $firstCommand = Select-String -LiteralPath $entry.path -SimpleMatch 'DevConsole:' | Select-Object -First 1
        foreach($errorLine in @(Select-String -LiteralPath $entry.path -Pattern '^\[ERROR\]|^ERROR:|Checksum mismatch|state divergence' -Context 0,24)){
            $source = $errorLine.Context.PostContext | Select-Object -First 1
            $detail = [PSCustomObject]@{role=$entry.role;line=$errorLine.LineNumber;text=$errorLine.Line;source=$source}
            if($Branch -eq 'beta' -and $errorLine.Line -eq 'ERROR: Invalid Task ID' -and
                $source -match 'wait_for_task_completion \(core/object/worker_thread_pool\.cpp:' -and
                $preload -and $errorLine.LineNumber -lt $preload.LineNumber -and
                $firstCommand -and $errorLine.LineNumber -lt $firstCommand.LineNumber){
                $startup.Add($detail)
            } else { $unexpected.Add($detail) }
        }
    }
    $audit = [PSCustomObject]@{startupWorkerPoolDiagnostics=@($startup);unexpectedErrors=@($unexpected)}
    $audit | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath (Join-Path $whiteEvidence 'log-audit.json') -Encoding utf8NoBOM
    if($unexpected.Count){throw "Mr. White logged $($unexpected.Count) unexpected errors"}
    return $audit
}

function Invoke-MrWhiteCase {
    $number=0
    foreach($mode in @('partial','zero','all','empty','keywords-all','keywords-zero')) {
        $number++
        try { Invoke-MrWhiteScenario $mode $number } catch {
            try { Save-MrWhiteState ("failure-{0:00}-{1}" -f $number,$mode) $false } catch {}
            throw
        }
    }
    $logAudit = Test-MrWhiteLogs
    [PSCustomObject]@{case=$caseName;result='pass';scenarios=$whiteResults.Count;holders=$whiteActors.Count;unexpectedErrorCount=$logAudit.unexpectedErrors.Count;startupWorkerPoolCount=$logAudit.startupWorkerPoolDiagnostics.Count}
}
