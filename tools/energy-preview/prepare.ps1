param(
    [Parameter(Mandatory=$true)][string]$GameSource,
    [Parameter(Mandatory=$true)][string]$McpSource,
    [int]$Port = 15637,
    [switch]$Launch
)
$ErrorActionPreference='Stop'
$project=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$artifactRoot=[IO.Path]::GetFullPath((Join-Path $project 'artifacts/energy-ui-live'))
$root=Join-Path $artifactRoot 'stable'
$game=Join-Path $root 'game'
if (-not $game.StartsWith($artifactRoot+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)) {throw 'Unsafe destination'}
if ((Get-Content -LiteralPath (Join-Path $GameSource 'release_info.json') -Encoding UTF8 -Raw|ConvertFrom-Json).version -ne 'v0.107.1') {throw 'Requires stable v0.107.1'}
if (Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction SilentlyContinue) {throw "Port $Port occupied"}
New-Item -ItemType Directory -Path $game,(Join-Path $game 'mods') -Force|Out-Null
foreach($file in Get-ChildItem -LiteralPath $GameSource -File){
 if($file.Extension -notin @('.exe','.pck','.dll','.json','.txt')){continue}
 $dest=Join-Path $game $file.Name
 if(-not(Test-Path -LiteralPath $dest)){New-Item -ItemType HardLink -Path $dest -Target $file.FullName|Out-Null}
}
$data=Join-Path $game 'data_sts2_windows_x86_64'
if(-not(Test-Path -LiteralPath $data)){New-Item -ItemType Junction -Path $data -Target (Join-Path $GameSource 'data_sts2_windows_x86_64')|Out-Null}
$mods=Join-Path $game 'mods'
foreach($name in @('BaseLib','ShoujoKagekiAijoKaren')){
 $dest=Join-Path $mods $name
 if(-not(Test-Path -LiteralPath $dest)){Copy-Item -LiteralPath (Join-Path $GameSource "mods/$name") -Destination $mods -Recurse}
}
foreach($name in @('KarenSTS2MCP.dll','KarenSTS2MCP.json')){
 if((Get-Item -LiteralPath (Join-Path $McpSource $name)).Length -lt 100){throw "Invalid MCP file $name"}
 Copy-Item -LiteralPath (Join-Path $McpSource $name) -Destination (Join-Path $mods $name) -Force
}
@{port=$Port;fullscreen=$false;muted=$true;skip_tutorial=$true;skip_first_prompt=$true;window_width=1280;window_height=720}|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $mods 'KarenSTS2MCP.conf') -Encoding UTF8
$preview=Join-Path $mods 'KarenEnergyPreview'
New-Item -ItemType Directory -Path $preview -Force|Out-Null
Copy-Item -LiteralPath (Join-Path $project 'artifacts/energy-ui-live/probe/KarenEnergyPreview.dll') -Destination $preview -Force
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'KarenEnergyPreview.json') -Destination $preview -Force
Copy-Item -LiteralPath (Join-Path $project 'docs/worktree-tasks/karen-energy-concepts/style-assets/a-faceted-crown.png') -Destination (Join-Path $preview 'crown.png') -Force
Copy-Item -LiteralPath (Join-Path $project 'docs/worktree-tasks/karen-energy-concepts/style-assets/d-faceted-tomato.png') -Destination (Join-Path $preview 'tomato.png') -Force
$assetDirectory=Join-Path $project 'docs/worktree-tasks/karen-energy-concepts/style-assets'
$assetMetadata=Get-Content -LiteralPath (Join-Path $assetDirectory 'assets.json') -Encoding UTF8 -Raw|ConvertFrom-Json
$styles=[ordered]@{}
foreach($entry in @(
 @{id='a';key='batch4-a-upper-pentagon'},
 @{id='b';key='batch4-b-lower-pentagon'},
 @{id='c';key='batch4-c-arc-triangle'},
 @{id='d';key='batch4-d-truncated-triangle'},
 @{id='p9';key='regular-nonagon'}
)){
 $asset=$assetMetadata.icons.($entry.key)
 if($null -eq $asset){throw ('Missing candidate metadata '+$entry.key)}
 Copy-Item -LiteralPath (Join-Path $assetDirectory $asset.file) -Destination (Join-Path $preview ($entry.id+'.png')) -Force
 $styles[$entry.id]=[ordered]@{key=$entry.key;file=($entry.id+'.png');x=$asset.x;y=$asset.y;width=$asset.width;height=$asset.height}
}
[IO.File]::WriteAllText((Join-Path $preview 'styles.json'),($styles|ConvertTo-Json -Depth 8),[Text.UTF8Encoding]::new($false))

$roaming=Join-Path $root 'user/AppData/Roaming'
$local=Join-Path $root 'user/AppData/Local'
New-Item -ItemType Directory -Path $roaming,$local -Force|Out-Null
$settingsDir=Join-Path $roaming 'SlayTheSpire2/default/1'
New-Item -ItemType Directory -Path $settingsDir -Force|Out-Null
$settingsSource=Join-Path (Split-Path $GameSource -Parent) 'user/AppData/Roaming/SlayTheSpire2/default/1/settings.save'
if((Test-Path -LiteralPath $settingsSource) -and -not(Test-Path -LiteralPath (Join-Path $settingsDir 'settings.save'))){
 $settings=Get-Content -LiteralPath $settingsSource -Encoding UTF8 -Raw|ConvertFrom-Json
 $settings.mod_settings=[PSCustomObject]@{mods_enabled=$true;mod_list=@()}
 [IO.File]::WriteAllText((Join-Path $settingsDir 'settings.save'),($settings|ConvertTo-Json -Depth 60),[Text.UTF8Encoding]::new($false))
}
if($Launch){
 $oldRoaming=$env:APPDATA;$oldLocal=$env:LOCALAPPDATA
 try{
  $env:APPDATA=$roaming;$env:LOCALAPPDATA=$local
  $arguments='--force-steam=off --rendering-driver vulkan --windowed --resolution 1280x720 --log-file "'+(Join-Path $root 'godot.log')+'"'
  $process=Start-Process -FilePath (Join-Path $game 'SlayTheSpire2.exe') -ArgumentList $arguments -WorkingDirectory $game -WindowStyle Hidden -PassThru
  [IO.File]::WriteAllText((Join-Path $root 'process.json'),(@{pid=$process.Id;exe=(Join-Path $game 'SlayTheSpire2.exe');port=$Port}|ConvertTo-Json),[Text.UTF8Encoding]::new($false))
  Write-Output ("Started isolated stable preview PID "+$process.Id+" on MCP "+$Port)
 }finally{$env:APPDATA=$oldRoaming;$env:LOCALAPPDATA=$oldLocal}
}
Write-Output $root
