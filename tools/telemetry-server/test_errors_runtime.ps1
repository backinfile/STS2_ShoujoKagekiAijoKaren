param(
    [ValidateSet('stable','beta')][string]$Branch = 'stable',
    [Parameter(Mandatory=$true)][string]$BuildRoot,
    [string]$PackageRoot
)
$ErrorActionPreference = 'Stop'
$project = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$build = [IO.Path]::GetFullPath((Join-Path $project $BuildRoot))
$source = Join-Path $project "artifacts/dev-game/$Branch/game"
$run = Join-Path $build ("runtime/$Branch-" + [guid]::NewGuid().ToString('N').Substring(0,8))
$game = Join-Path $run 'game'
$mods = Join-Path $game 'mods'
$variant = if ($Branch -eq 'stable') {'Stable'} else {'Beta'}
$port = if ($Branch -eq 'stable') {15927} else {15928}
if (Get-NetTCPConnection -LocalPort $port -State Listen -ErrorAction SilentlyContinue) { throw 'Probe port already in use' }
New-Item -ItemType Directory -Path $mods -Force | Out-Null
# Share only immutable game files. Each run owns its mods, config, queues, logs and saves.
foreach ($item in Get-ChildItem -LiteralPath $source) {
    if ($item.Name -eq 'mods') { continue }
    $destination = Join-Path $game $item.Name
    if ($item.PSIsContainer) { New-Item -ItemType Junction -Path $destination -Target $item.FullName | Out-Null }
    else { New-Item -ItemType HardLink -Path $destination -Target $item.FullName | Out-Null }
}
New-Item -ItemType Junction -Path (Join-Path $mods 'BaseLib') -Target (Join-Path $source 'mods/BaseLib') | Out-Null
$karen = Join-Path $mods 'ShoujoKagekiAijoKaren'
if ($PackageRoot) {
    $package = [IO.Path]::GetFullPath((Join-Path $project $PackageRoot))
    foreach ($required in @('ShoujoKagekiAijoKaren.dll','ShoujoKagekiAijoKaren.pck','ShoujoKagekiAijoKaren.json','lib/ShoujoKagekiAijoKaren.Stable.dll','lib/ShoujoKagekiAijoKaren.Beta.dll')) {
        if (!(Test-Path -LiteralPath (Join-Path $package $required))) { throw "Incomplete release package: $required" }
    }
    Copy-Item -LiteralPath $package -Destination $karen -Recurse
} else {
New-Item -ItemType Directory -Path (Join-Path $karen 'lib') -Force | Out-Null
foreach ($name in @('ShoujoKagekiAijoKaren.dll','ShoujoKagekiAijoKaren.pck')) {
    New-Item -ItemType HardLink -Path (Join-Path $karen $name) -Target (Join-Path $source "mods/ShoujoKagekiAijoKaren/$name") | Out-Null
}
Copy-Item -LiteralPath (Join-Path $project 'ShoujoKagekiAijoKaren.json') -Destination $karen
Copy-Item -LiteralPath (Join-Path $build "bin/$Branch/ShoujoKagekiAijoKaren.$variant.dll") -Destination (Join-Path $karen 'lib')
}
Get-ChildItem -LiteralPath $karen -File -Recurse | ForEach-Object {
    @{path=$_.FullName.Substring($karen.Length + 1).Replace('\','/'); sha256=(Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant()}
} | ConvertTo-Json | Set-Content (Join-Path $run 'package-sha256.json') -Encoding UTF8
foreach ($name in @('KarenSTS2MCP.dll','KarenSTS2MCP.json')) { Copy-Item -LiteralPath (Join-Path $source "mods/$name") -Destination $mods }
@{port=$port;muted=$true;fullscreen=$false;skip_tutorial=$true;skip_first_prompt=$true;window_width=640;window_height=360} | ConvertTo-Json | Set-Content (Join-Path $mods 'KarenSTS2MCP.conf') -Encoding UTF8
Copy-Item -LiteralPath (Join-Path $build "probe/$Branch/KarenErrorProbe.dll") -Destination $mods
@{id='KarenErrorProbe';name='Karen Error Probe';author='Local test';version='1.0.0';has_dll=$true;has_pck=$false;affects_gameplay=$false;dependencies=@(@{id='ShoujoKagekiAijoKaren';min_version='v0.1.0'})} | ConvertTo-Json -Depth 4 | Set-Content (Join-Path $mods 'KarenErrorProbe.json') -Encoding UTF8
$roaming = Join-Path $run 'user/AppData/Roaming'
$local = Join-Path $run 'user/AppData/Local'
$userData = Join-Path $roaming 'SlayTheSpire2'
New-Item -ItemType Directory -Path (Join-Path $userData 'mod_configs'),$local -Force | Out-Null
'{"EnableDataUpload":"False","EnableErrorUpload":"False","ShowSplashScreen":"False"}' | Set-Content (Join-Path $userData 'mod_configs/ShoujoKagekiAijoKaren.cfg') -Encoding UTF8
$settings = Get-Content -LiteralPath (Join-Path $project "artifacts/dev-game/$Branch/user/AppData/Roaming/SlayTheSpire2/default/1/settings.save") -Raw -Encoding UTF8 | ConvertFrom-Json
$settings.mod_settings = [PSCustomObject]@{mods_enabled=$true;mod_list=@()}
$settings.fullscreen=$false; $settings.volume_master=0; $settings.seen_ea_disclaimer=$true; $settings.skip_intro_logo=$true
New-Item -ItemType Directory -Path (Join-Path $userData 'default/1') -Force | Out-Null
$settings | ConvertTo-Json -Depth 30 | Set-Content (Join-Path $userData 'default/1/settings.save') -Encoding UTF8
$log = Join-Path $run 'godot.log'
$originalRoaming = $env:APPDATA
$originalLocal = $env:LOCALAPPDATA
$process = $null
try {
    $env:APPDATA=$roaming; $env:LOCALAPPDATA=$local
    $arguments='--rendering-driver vulkan --force-steam=off --audio-driver Dummy --log-file "' + $log + '"'
    $process=Start-Process -FilePath (Join-Path $game 'SlayTheSpire2.exe') -WorkingDirectory $game -ArgumentList $arguments -WindowStyle Hidden -PassThru
    $env:APPDATA=$originalRoaming; $env:LOCALAPPDATA=$originalLocal
    $result=Join-Path $userData 'error-probe-result.json'
    $deadline=[DateTime]::UtcNow.AddSeconds(60)
    do {
        Start-Sleep -Milliseconds 500
        if ($process.HasExited) { throw "Probe game exited; see $log" }
        $mcpReady=(Test-Path $log) -and (Select-String -LiteralPath $log -SimpleMatch '[Karen STS2 MCP]' -Quiet)
    } until (((Test-Path $result) -and $mcpReady) -or [DateTime]::UtcNow -gt $deadline)
    if (!(Test-Path $result) -or !$mcpReady) { throw "Probe timed out; see $log" }
    $probe=Get-Content -LiteralPath $result -Raw -Encoding UTF8 | ConvertFrom-Json
    if (!$probe.passed) { throw $probe.detail }
    $state=Invoke-RestMethod "http://127.0.0.1:$port/api/v1/singleplayer?format=json" -TimeoutSec 5
    Invoke-RestMethod "http://127.0.0.1:$port/api/v1/window/minimize" -Method Post -TimeoutSec 5 | Out-Null
    Write-Output "PASS $Branch actual game callbacks, dedup, redaction, disabled setting; MCP=$($state.state_type)"
    Write-Output "Evidence: $run"
}
finally {
    $env:APPDATA=$originalRoaming; $env:LOCALAPPDATA=$originalLocal
    if ($process -and !$process.HasExited) { Stop-Process -Id $process.Id -Force }
}
