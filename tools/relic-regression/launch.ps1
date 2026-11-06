param([ValidateSet('stable','beta')][string]$Branch = 'stable', [string]$UserRoot = '', [int]$ProfileId = 0)
$ErrorActionPreference = 'Stop'
$project = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$root = Join-Path $project "artifacts/dev-game/$Branch"
$game = Join-Path $root 'game'
$port = if ($Branch -eq 'stable') { 15657 } else { 15658 }
if (Get-NetTCPConnection -LocalPort $port -State Listen -ErrorAction SilentlyContinue) { throw "Port $port already occupied" }
if (@(Get-Process SlayTheSpire2 -ErrorAction SilentlyContinue | Where-Object Path -eq (Join-Path $game 'SlayTheSpire2.exe')).Count) {
    throw 'This isolated game is already running'
}
$configPath = Join-Path $game 'mods/KarenSTS2MCP.conf'
$config = Get-Content -LiteralPath $configPath -Raw -Encoding UTF8 | ConvertFrom-Json -AsHashtable
$config.port = $port
$config | ConvertTo-Json -Depth 16 | Set-Content -LiteralPath $configPath -Encoding UTF8
if ($UserRoot) {
    $UserRoot = [IO.Path]::GetFullPath($UserRoot)
    if (-not $UserRoot.StartsWith((Join-Path $project 'artifacts') + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) { throw 'UserRoot must be inside task artifacts' }
    if ($ProfileId -le 0) { throw 'A new UserRoot requires a positive ProfileId' }
    $settingsSource = Join-Path $root 'user/AppData/Roaming/SlayTheSpire2/default/1/settings.save'
    $settingsTarget = Join-Path $UserRoot "user/AppData/Roaming/SlayTheSpire2/default/$ProfileId/settings.save"
    New-Item -ItemType Directory -Path (Split-Path $settingsTarget -Parent) -Force | Out-Null
    $settings = Get-Content -LiteralPath $settingsSource -Raw -Encoding UTF8 | ConvertFrom-Json -AsHashtable
    $settings.limit_fps_in_background = $false
    $settings | ConvertTo-Json -Depth 60 | Set-Content -LiteralPath $settingsTarget -Encoding UTF8
    $root = $UserRoot
}
$variant = if ($Branch -eq 'stable') { 'Stable' } else { 'Beta' }
& dotnet build (Join-Path $PSScriptRoot 'KarenRelicRegression.csproj') "-p:Variant=$variant" --nologo -v:q -clp:ErrorsOnly
if ($LASTEXITCODE -ne 0) { throw 'Fixture build failed' }
Copy-Item -LiteralPath (Join-Path $project 'artifacts/ShoujoKagekiAijoKaren') -Destination (Join-Path $game 'mods') -Recurse -Force
$fixture = Join-Path $game 'mods/KarenRelicRegression'
New-Item -ItemType Directory -Path $fixture -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'bin/Debug/net9.0/KarenRelicRegression.dll') -Destination $fixture -Force
@{id='KarenRelicRegression';name='Karen Relic Regression';author='Codex';version='0.1.0';min_game_version='v0.107.1';has_dll=$true;has_pck=$false;affects_gameplay=$true;dependencies=@(@{id='ShoujoKagekiAijoKaren';min_version='v0.1.3'})} |
    ConvertTo-Json | Set-Content -LiteralPath (Join-Path $fixture 'KarenRelicRegression.json') -Encoding UTF8
$oldAppData = $env:APPDATA
$oldLocalData = $env:LOCALAPPDATA
try {
    $env:APPDATA = Join-Path $root 'user/AppData/Roaming'
    $env:LOCALAPPDATA = Join-Path $root 'user/AppData/Local'
    New-Item -ItemType Directory -Path $env:APPDATA,$env:LOCALAPPDATA -Force | Out-Null
    $log = Join-Path $root 'godot.log'
    $arguments = '--rendering-driver vulkan --force-steam=off --log-file "' + $log + '"'
    if ($ProfileId -gt 0) { $arguments += " --clientId=$ProfileId" }
    $process = Start-Process -FilePath (Join-Path $game 'SlayTheSpire2.exe') -WorkingDirectory $game -ArgumentList $arguments -WindowStyle Hidden -PassThru
    @{pid=$process.Id;port=$port;game=$game;log=$log} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $root 'relic-process.json') -Encoding UTF8
    Write-Host "Started $Branch PID=$($process.Id), MCP=$port"
} finally { $env:APPDATA=$oldAppData; $env:LOCALAPPDATA=$oldLocalData }
