param([ValidateSet('stable','beta')][string]$Branch='stable',[int]$Port=0,[switch]$Launch)
$ErrorActionPreference='Stop'
$project=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
if(-not $Port){$Port=if($Branch -eq 'stable'){15637}else{15638}}
if(Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction SilentlyContinue){throw "MCP port $Port is occupied"}
$source=Join-Path $project "artifacts/dev-game/$Branch/game"
$root=Join-Path $project "artifacts/energy-demo/$Branch"
$game=Join-Path $root 'game'
$mods=Join-Path $game 'mods'
New-Item -ItemType Directory -Path $mods -Force|Out-Null
foreach($file in Get-ChildItem -LiteralPath $source -File){
 $destination=Join-Path $game $file.Name
 if(-not(Test-Path -LiteralPath $destination)){New-Item -ItemType HardLink -Path $destination -Target $file.FullName|Out-Null}
}
$data=Join-Path $game 'data_sts2_windows_x86_64'
if(-not(Test-Path -LiteralPath $data)){New-Item -ItemType Junction -Path $data -Target (Join-Path $source 'data_sts2_windows_x86_64')|Out-Null}
foreach($name in @('BaseLib','ShoujoKagekiAijoKaren')){
 Copy-Item -LiteralPath (Join-Path $source "mods/$name") -Destination $mods -Recurse -Force
}
foreach($name in @('KarenSTS2MCP.dll','KarenSTS2MCP.json')){Copy-Item -LiteralPath (Join-Path $project "artifacts/mcp-$Branch/$name") -Destination $mods -Force}
@{port=$Port;fullscreen=$false;muted=$true;skip_tutorial=$true;skip_first_prompt=$true;window_width=1280;window_height=720}|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $mods 'KarenSTS2MCP.conf') -Encoding UTF8
Copy-Item -LiteralPath (Join-Path $project "artifacts/energy-demo/audit-$Branch/KarenEnergyDemoAudit.dll") -Destination $mods -Force
@{id='KarenEnergyDemoAudit';name='Karen Energy Demo Audit';author='Development';version='1.0.0';has_dll=$true;has_pck=$false;affects_gameplay=$false;description='Read-only production energy resource audit, no texture override'}|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $mods 'KarenEnergyDemoAudit.json') -Encoding UTF8
$override=@"
[application]
run/max_fps=60
[display]
window/size/mode=0
window/size/no_focus=true
window/size/window_width_override=1280
window/size/window_height_override=720
"@
[IO.File]::WriteAllText((Join-Path $game 'override.cfg'),$override,[Text.UTF8Encoding]::new($false))
$profile=if($Branch -eq 'stable'){90371}else{90372}
$roaming=Join-Path $root 'user/AppData/Roaming';$local=Join-Path $root 'user/AppData/Local'
$settingsDir=Join-Path $roaming "SlayTheSpire2/default/$profile"
New-Item -ItemType Directory -Path $settingsDir,$local -Force|Out-Null
if(-not(Test-Path -LiteralPath (Join-Path $settingsDir 'settings.save'))){Copy-Item -LiteralPath (Join-Path $project "artifacts/dev-game/$Branch/user/AppData/Roaming/SlayTheSpire2/default/1/settings.save") -Destination (Join-Path $settingsDir 'settings.save')}
$settingsPath=Join-Path $settingsDir 'settings.save'
$settings=Get-Content -LiteralPath $settingsPath -Encoding UTF8 -Raw|ConvertFrom-Json
$settings.limit_fps_in_background=$false
$settings.fps_limit=60
[IO.File]::WriteAllText($settingsPath,($settings|ConvertTo-Json -Depth 60),[Text.UTF8Encoding]::new($false))
if($Launch){
 $oldRoaming=$env:APPDATA;$oldLocal=$env:LOCALAPPDATA
 try{
  $env:APPDATA=$roaming;$env:LOCALAPPDATA=$local
  $arguments="--force-steam=off --rendering-driver vulkan --windowed --resolution 1280x720 --clientId=$profile --log-file `"$(Join-Path $root 'godot.log')`""
  $process=Start-Process -FilePath (Join-Path $game 'SlayTheSpire2.exe') -ArgumentList $arguments -WorkingDirectory $game -WindowStyle Hidden -PassThru
  [IO.File]::WriteAllText((Join-Path $root 'process.json'),(@{pid=$process.Id;exe=(Join-Path $game 'SlayTheSpire2.exe');port=$Port;profile=$profile}|ConvertTo-Json),[Text.UTF8Encoding]::new($false))
  Write-Output "Started production $Branch energy demonstration PID $($process.Id), MCP $Port"
 }finally{$env:APPDATA=$oldRoaming;$env:LOCALAPPDATA=$oldLocal}
}
Write-Output $root