# Dot-sourced by dev_matrix_case.ps1 only for the seven-relic suite.
$relicActors = @()
if ($HostCharacter -eq 'KAREN') { $relicActors += $hostPort }
if ($ClientCharacter -eq 'KAREN') { $relicActors += $clientPort }
$relicEvidence = Join-Path $RunRoot "seven-relics/$caseName"
New-Item -ItemType Directory -Path $relicEvidence -Force | Out-Null

function Invoke-RelicFixture([string]$name, [int]$actorPort) {
    $identity = Post-Action $actorPort 'multiplayer' @{ action='run_command'; command='relic_fixture_status owner' }
    $actorId = $identity.message
    $expected = "pass:$name`:owner=$actorId"
    $null = Post-Action $actorPort 'multiplayer' @{ action='run_command'; command="relic_fixture $name" }
    foreach ($peer in @($hostPort, $clientPort)) {
        $passed = $false
        for ($attempt = 0; $attempt -lt 100; $attempt++) {
            $result = Post-Action $peer 'multiplayer' @{ action='run_command'; command='relic_fixture_status' }
            if ($result.message -eq $expected) { $passed = $true; break }
            if ($result.message -like 'fail:*') { throw "Relic fixture on $peer : $($result.message)" }
            Start-Sleep -Milliseconds 200
        }
        if (-not $passed) { throw "Relic fixture $name did not finish on peer $peer" }
        $result | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $relicEvidence "$actorPort-$name-$peer.json") -Encoding UTF8
    }
}

function Save-RelicState([string]$label, [bool]$checkHand = $true) {
    Start-Sleep -Seconds 2
    foreach ($peer in @($hostPort, $clientPort)) {
        Get-State $peer 'multiplayer' | ConvertTo-Json -Depth 60 |
            Set-Content -LiteralPath (Join-Path $relicEvidence "$label-$peer.json") -Encoding UTF8
        Invoke-WebRequest -Uri "http://127.0.0.1:$peer/api/v1/screenshot" -OutFile (Join-Path $relicEvidence "$label-$peer.png") | Out-Null
        if ($checkHand) {
            $result = Post-Action $peer 'multiplayer' @{ action='run_command'; command='karen_check_hand' }
            $result | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $relicEvidence "$label-$peer-hand.json") -Encoding UTF8
        }
    }
}

function Initialize-SevenRelicCase {
    foreach ($peer in @($hostPort, $clientPort)) {
        $null = Invoke-RestMethod -Uri "http://127.0.0.1:$peer/api/v1/settings" -Method Post -ContentType 'application/json' `
            -Body '{"fullscreen":false,"muted":true,"window_width":1280,"window_height":720}' -TimeoutSec 5
    }
    foreach ($actor in $relicActors) {
        Invoke-RelicFixture 'setup' $actor
        Invoke-RelicFixture 'tiara' $actor
    }
    Save-RelicState '00-obtained' $false
}

function End-RelicRound([bool]$selectWhite) {
    foreach ($peer in @($hostPort, $clientPort)) {
        $null = Post-Action $peer 'multiplayer' @{ action='end_turn' }
    }
    if ($selectWhite) {
        $pending = [Collections.Generic.List[int]]::new()
        foreach ($actor in $relicActors) { $pending.Add($actor) }
        $deadline = [DateTime]::UtcNow.AddSeconds(40)
        while ($pending.Count -gt 0 -and [DateTime]::UtcNow -lt $deadline) {
            foreach ($peer in @($pending.ToArray())) {
                $selection = Get-State $peer 'multiplayer'
                if ($selection.state_type -ne 'hand_select') { continue }
                if (@($selection.hand_select.cards).Count -lt 2) { throw 'Mr. White needs two selectable cards' }
                $selection | ConvertTo-Json -Depth 60 | Set-Content -LiteralPath (Join-Path $relicEvidence "white-selection-$peer.json") -Encoding UTF8
                $null = Post-Action $peer 'multiplayer' @{ action='combat_select_card'; card_index=0 }
                $null = Post-Action $peer 'multiplayer' @{ action='combat_select_card'; card_index=0 }
                $null = Post-Action $peer 'multiplayer' @{ action='combat_confirm_selection' }
                $pending.Remove($peer) | Out-Null
            }
            Start-Sleep -Milliseconds 250
        }
        if ($pending.Count) { throw 'Mr. White owner selections did not complete' }
    }
}

function Invoke-SevenRelicCase {
    foreach ($actor in $relicActors) { Invoke-RelicFixture 'mp-start' $actor }
    foreach ($peer in @($hostPort, $clientPort)) {
        if ($peer -notin $relicActors) {
            $s = Get-State $peer 'multiplayer'
            if ($s.player.energy -ne 3 -or @($s.player.relics | Where-Object id -eq 'KAREN_LOCK_RELIC').Count -gt 0) {
                throw 'Tiara affected the vanilla teammate'
            }
            Invoke-RelicFixture 'buffer' $peer
        }
    }
    Save-RelicState '01-before-tower'
    foreach ($actor in $relicActors) {
        Invoke-RelicFixture 'mp-tower-prepare' $actor
        Play-RelicCard $actor 'KAREN_TOWER_OF_PROMISE'
        Invoke-RelicFixture 'mp-tower-check' $actor
        Save-RelicState "02-after-tower-$actor"
        Invoke-RelicFixture 'mp-cat-prepare' $actor
        foreach ($id in @('KAREN_SHINE_STRIKE','KAREN_SHINE_STRIKE','KAREN_DRINK_WATER','KAREN_WE_KOWN_TOKEN')) {
            Play-RelicCard $actor $id
        }
        Invoke-RelicFixture 'mp-cat-check' $actor
    }
    Save-RelicState '03-before-white'
    End-RelicRound $true
    foreach ($peer in @($hostPort, $clientPort)) {
        $null = Wait-State $peer 'multiplayer' { param($s) $s.battle.round -eq 2 -and $s.battle.is_play_phase } 'round 2'
    }
    Save-RelicState '04-after-white'
    foreach ($actor in $relicActors) {
        Invoke-RelicFixture 'mp-white' $actor
        Invoke-RelicFixture 'water2' $actor
    }
    foreach ($round in @(3,4)) {
        End-RelicRound $false
        foreach ($peer in @($hostPort, $clientPort)) {
            $null = Wait-State $peer 'multiplayer' { param($s) $s.battle.round -eq $round -and $s.battle.is_play_phase } "round $round"
        }
        foreach ($actor in $relicActors) { Invoke-RelicFixture "water$round" $actor }
    }
    Save-RelicState '05-round4'
    $null = Post-Action $hostPort 'multiplayer' @{ action='run_command'; command='kill all' }
    foreach ($peer in @($hostPort, $clientPort)) {
        $s = Wait-State $peer 'multiplayer' { param($s) $s.state_type -eq 'rewards' } 'combat rewards'
        $count = @($s.rewards.items | Where-Object type -eq 'special_card').Count
        $expected = if ($peer -in $relicActors) { 2 } else { 0 }
        if ($count -ne $expected) { throw "Baseball Cat reward ownership: $peer has $count, expected $expected" }
    }
    Save-RelicState '06-rewards' $false
    $errors = @(Select-String -LiteralPath (Join-Path $hostRoot 'godot.log'),(Join-Path $clientRoot 'godot.log') -Pattern '^\[ERROR\]')
    if ($errors.Count) { throw 'Game logged an error during seven-relic multiplayer case' }
    [PSCustomObject]@{ case=$caseName; result='pass'; karenActors=$relicActors.Count; allSevenRelics=$true;
        bothPeersValidated=$true; replayTempStrength=10; replayBlock=0; rounds=4; whiteCards=2; rewardsPerKaren=2;
        personalStateIsolated=$true; handVisualsMatched=$true; errorCount=0 }
}

function Play-RelicCard([int]$actor, [string]$id) {
    Start-Sleep -Milliseconds 750
    $s = Get-State $actor 'multiplayer'
    $card = $s.player.hand | Where-Object { $_.id -eq $id -and $_.can_play } | Select-Object -Last 1
    if ($null -eq $card) { throw "No playable $id on $actor" }
    $count = @($s.player.hand | Where-Object id -eq $id).Count
    $action = @{ action='play_card'; card_index=$card.index }
    if ($card.target_type -eq 'AnyEnemy') { $action.target = $s.battle.enemies[0].entity_id }
    $null = Post-Action $actor 'multiplayer' $action
    $null = Wait-State $actor 'multiplayer' { param($x)
        $x.state_type -eq 'monster' -and @($x.player.hand | Where-Object id -eq $id).Count -eq $count - 1
    } "native $id play"
    Start-Sleep -Seconds 2
}
