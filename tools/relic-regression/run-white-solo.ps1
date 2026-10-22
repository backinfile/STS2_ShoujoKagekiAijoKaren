param([Parameter(Mandatory=$true)][ValidateSet('stable','beta')][string]$Branch,[string]$RunRoot='')
$ErrorActionPreference='Stop'
$project=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
if(-not $RunRoot){$RunRoot=Join-Path $project ("artifacts/test-matrix/mr-white-two-turn-solo-"+[DateTime]::UtcNow.ToString("yyyyMMdd-HHmmss")+"/"+$Branch)}
$RunRoot=[IO.Path]::GetFullPath($RunRoot)
$allowedRoot=Join-Path $project 'artifacts/test-matrix'
if(-not $RunRoot.StartsWith($allowedRoot+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)){throw 'Unsafe solo evidence root'}
$game=Join-Path $project "artifacts/dev-game/$Branch/game"
$exe=Join-Path $game 'SlayTheSpire2.exe'
$port=if($Branch -eq 'stable'){15817}else{15818}
$profileId=if($Branch -eq 'stable'){9261}else{9262}
if(Get-NetTCPConnection -LocalPort $port -State Listen -ErrorAction SilentlyContinue){throw "Solo MCP port $port is occupied"}
if(@(Get-Process SlayTheSpire2 -ErrorAction SilentlyContinue|Where-Object Path -eq $exe).Count){throw 'This task game is already running'}
$configPath=Join-Path $game 'mods/KarenSTS2MCP.conf'
$configBytes=[IO.File]::ReadAllBytes($configPath)
$config=Get-Content -LiteralPath $configPath -Raw -Encoding UTF8|ConvertFrom-Json -AsHashtable
$config.port=$port
$settingsSource=Join-Path $project "artifacts/dev-game/$Branch/user/AppData/Roaming/SlayTheSpire2/default/1/settings.save"
$settingsTarget=Join-Path $RunRoot "user/AppData/Roaming/SlayTheSpire2/default/$profileId/settings.save"
New-Item -ItemType Directory -Path (Split-Path $settingsTarget -Parent),(Join-Path $RunRoot 'user/AppData/Local') -Force|Out-Null
Copy-Item -LiteralPath $settingsSource -Destination $settingsTarget -Force
$hostPort=$port
$clientPort=$port
$HostCharacter='KAREN'
$ClientCharacter='IRONCLAD'
$caseName="$Branch-solo-two-turn"
$hostRoot=$RunRoot
$clientRoot=$RunRoot
. (Join-Path $PSScriptRoot 'mr-white-actions.ps1')
function Get-State([int]$peer,[string]$endpoint='singleplayer'){
    Invoke-RestMethod -Uri "http://127.0.0.1:$peer/api/v1/singleplayer?format=json" -TimeoutSec 5
}
function Post-Action([int]$peer,[string]$endpoint,[hashtable]$action){
    $response=Invoke-RestMethod -Uri "http://127.0.0.1:$peer/api/v1/singleplayer" -Method Post -ContentType 'application/json' -Body ($action|ConvertTo-Json -Compress) -TimeoutSec 8
    [ordered]@{action=$action;response=$response}|ConvertTo-Json -Depth 20 -Compress|Add-Content -LiteralPath (Join-Path $RunRoot 'actions.jsonl') -Encoding utf8NoBOM
    if($response.status -ne 'ok'){
        Get-State $peer|ConvertTo-Json -Depth 60|Set-Content -LiteralPath (Join-Path $RunRoot 'failed-action-state.json') -Encoding utf8NoBOM
        throw ("Action "+($action|ConvertTo-Json -Compress)+": "+($response|ConvertTo-Json -Compress))
    }
    return $response
}
function Wait-State([int]$peer,[string]$endpoint,[scriptblock]$condition,[string]$label){
    for($attempt=0;$attempt -lt 60;$attempt++){
        try{$state=Get-State $peer;if(& $condition $state){return $state}}catch{}
        Start-Sleep -Milliseconds 500
    }
    throw "Timed out waiting for $label"
}
$process=$null
try{
    $config|ConvertTo-Json -Depth 16|Set-Content -LiteralPath $configPath -Encoding utf8NoBOM
    $previousRoaming=$env:APPDATA
    $previousLocal=$env:LOCALAPPDATA
    try{
        $env:APPDATA=Join-Path $RunRoot 'user/AppData/Roaming'
        $env:LOCALAPPDATA=Join-Path $RunRoot 'user/AppData/Local'
        $arguments='--rendering-driver vulkan --force-steam=off --clientId='+$profileId+' --log-file "'+(Join-Path $RunRoot 'godot.log')+'"'
        $process=Start-Process -FilePath $exe -WorkingDirectory $game -ArgumentList $arguments -WindowStyle Hidden -PassThru
    }finally{$env:APPDATA=$previousRoaming;$env:LOCALAPPDATA=$previousLocal}
    Wait-MrWhiteMcp $RunRoot
    $null=Wait-State $port 'singleplayer' {param($s)$s.menu_screen -eq 'main' -and 'singleplayer' -in @($s.options)} 'ready main menu'
    $null=Post-Action $port 'singleplayer' @{action='menu_select';option='singleplayer'}
    $menu=Wait-State $port 'singleplayer' {param($s)$s.menu_screen -in @('singleplayer','character_select')} 'singleplayer menu'
    if($menu.menu_screen -eq 'singleplayer'){$null=Post-Action $port 'singleplayer' @{action='menu_select';option='standard'}}
    $null=Wait-State $port 'singleplayer' {param($s)$s.menu_screen -eq 'character_select'} 'character selection'
    $null=Post-Action $port 'singleplayer' @{action='menu_select';option='KAREN'}
    $null=Post-Action $port 'singleplayer' @{action='menu_select';option='embark'}
    $entry=Wait-State $port 'singleplayer' {param($s)$s.state_type -eq 'map' -or $s.menu_screen -eq 'tutorial_prompt'} 'tutorial or map'
    if($entry.menu_screen -eq 'tutorial_prompt'){$null=Post-Action $port 'singleplayer' @{action='menu_select';option='no'}}
    $null=Wait-State $port 'singleplayer' {param($s)$s.state_type -eq 'map'} 'map'
    $null=Post-Action $port 'singleplayer' @{action='run_command';command='relic add KAREN_STAR_TIARA_RELIC'}
    Get-State $port|ConvertTo-Json -Depth 60|Set-Content -LiteralPath (Join-Path $RunRoot 'crown-description.json') -Encoding utf8NoBOM
    $null=Post-Action $port 'singleplayer' @{action='run_command';command='relic_art KAREN_STAR_TIARA_RELIC'}
    Start-Sleep -Milliseconds 750
    Invoke-WebRequest -Uri "http://127.0.0.1:$port/api/v1/screenshot" -OutFile (Join-Path $RunRoot 'crown-energy-icon.png')|Out-Null
    $null=Post-Action $port 'singleplayer' @{action='run_command';command='relic_art close'}
    $null=Post-Action $port 'singleplayer' @{action='run_command';command='relic remove KAREN_STAR_TIARA_RELIC'}
    Initialize-MrWhiteCase
    $null=Post-Action $port 'singleplayer' @{action='run_command';command='relic_art KAREN_MR_WHITE_RELIC'}
    Start-Sleep -Milliseconds 750
    Invoke-WebRequest -Uri "http://127.0.0.1:$port/api/v1/screenshot" -OutFile (Join-Path $RunRoot 'white-two-turn-description.png')|Out-Null
    $null=Post-Action $port 'singleplayer' @{action='run_command';command='relic_art close'}
    $null=Post-Action $port 'singleplayer' @{action='choose_map_node';index=0}
    Wait-WhiteRound 1
    Invoke-MrWhiteScenario 'partial' 1
    $logAudit=Test-MrWhiteLogs
    [PSCustomObject]@{case=$caseName;result='pass';firstTwoTurns=$true;thirdTurnNoTrigger=$true;handVisualsMatched=$true;startupWorkerPoolDiagnostics=$logAudit.startupWorkerPoolDiagnostics.Count}|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $RunRoot 'report.json') -Encoding utf8NoBOM
    Get-Content -LiteralPath (Join-Path $RunRoot 'report.json') -Raw -Encoding UTF8
}finally{
    if($null -ne $process){
        $running=Get-Process -Id $process.Id -ErrorAction SilentlyContinue
        if($null -ne $running){
            if(-not $running.Path.Equals($exe,[StringComparison]::OrdinalIgnoreCase)){throw 'Unexpected solo process path; preserve configuration for investigation'}
            Stop-Process -Id $process.Id
            $running.WaitForExit(5000)|Out-Null
        }
    }
    [IO.File]::WriteAllBytes($configPath,$configBytes)
}
