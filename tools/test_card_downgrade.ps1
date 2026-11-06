param(
    [ValidateSet('stable', 'beta')][string[]]$Environment = @('stable', 'beta')
)

$ErrorActionPreference = 'Stop'
$project = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$runRoot = Join-Path $project ('artifacts/test-matrix/downgrade-' + [DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss'))
New-Item -ItemType Directory -Path $runRoot -Force | Out-Null
$results = @()

foreach ($branch in $Environment) {
    $variant = if ($branch -eq 'stable') { 'Stable' } else { 'Beta' }
    $port = if ($branch -eq 'stable') { 15627 } else { 15628 }
    $game = Join-Path $project "artifacts/dev-game/$branch/game"
    $exe = Join-Path $game 'SlayTheSpire2.exe'
    $plugin = Join-Path $game 'mods/KarenReviewRegression'
    $root = Join-Path $runRoot $branch
    $log = Join-Path $root 'godot.log'
    $url = "http://127.0.0.1:$port/api/v1/singleplayer"
    $process = $null
    $installed = $false

    function Get-State { Invoke-RestMethod -Uri "$url`?format=json" -TimeoutSec 5 }
    function Wait-State([scriptblock]$accept, [string]$label) {
        for ($i = 0; $i -lt 100; $i++) {
            try { $state = Get-State; if (& $accept $state) { return $state } } catch { }
            Start-Sleep -Milliseconds 250
        }
        throw "Timed out: $label"
    }
    function Post([hashtable]$body) {
        $result = Invoke-RestMethod -Uri $url -Method Post -ContentType 'application/json' -Body ($body | ConvertTo-Json -Compress) -TimeoutSec 12
        if ($result.status -ne 'ok') { throw ($result | ConvertTo-Json -Compress) }
        return $result
    }

    try {
        if (Get-NetTCPConnection -LocalPort $port -State Listen -ErrorAction SilentlyContinue) { throw "MCP port $port is occupied" }
        if (Test-Path -LiteralPath $plugin) { throw "Fixture directory already exists: $plugin" }
        $output = Join-Path $project "artifacts/review-plugin/$branch"
        & dotnet build (Join-Path $PSScriptRoot 'review-regression/KarenReviewRegression.csproj') "-p:Variant=$variant" -o $output --nologo -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Fixture build failed' }
        New-Item -ItemType Directory -Path $plugin -Force | Out-Null
        $installed = $true
        Copy-Item -LiteralPath (Join-Path $output 'KarenReviewRegression.dll') -Destination $plugin
        @{
            id='KarenReviewRegression'; name='Karen review regression fixtures'; author='Karen tests'; version='1.0.0';
            has_dll=$true; has_pck=$false; affects_gameplay=$true; min_game_version='v0.107.1';
            dependencies=@(@{id='ShoujoKagekiAijoKaren'})
        } | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $plugin 'KarenReviewRegression.json') -Encoding UTF8

        $profileId = 8100
        $roaming = Join-Path $root 'user/AppData/Roaming'
        $local = Join-Path $root 'user/AppData/Local'
        $settings = Join-Path $roaming "SlayTheSpire2/default/$profileId/settings.save"
        New-Item -ItemType Directory -Path (Split-Path $settings -Parent),$local -Force | Out-Null
        Copy-Item -LiteralPath (Join-Path $project "artifacts/dev-game/$branch/user/AppData/Roaming/SlayTheSpire2/default/1/settings.save") -Destination $settings
        $oldRoaming = $env:APPDATA
        $oldLocal = $env:LOCALAPPDATA
        try {
            $env:APPDATA = $roaming
            $env:LOCALAPPDATA = $local
            $process = Start-Process -FilePath $exe -WorkingDirectory $game -WindowStyle Hidden -PassThru -ArgumentList "--rendering-driver vulkan --force-steam=off --clientId=$profileId --log-file `"$log`""
        } finally { $env:APPDATA = $oldRoaming; $env:LOCALAPPDATA = $oldLocal }
        $loaded = $false
        for ($i = 0; $i -lt 100; $i++) {
            if ((Test-Path -LiteralPath $log) -and (Select-String -LiteralPath $log -SimpleMatch '[Karen STS2 MCP] v0.1.0 server started' -Quiet)) { $loaded = $true; break }
            Start-Sleep -Milliseconds 250
        }
        if (-not $loaded) { throw 'KarenSTS2MCP did not load' }
        $null = Wait-State { param($s) $s.menu_screen -eq 'main' -and @($s.options) -contains 'singleplayer' } 'main menu'
        $null = Invoke-RestMethod -Uri "http://127.0.0.1:$port/api/v1/settings" -Method Post -ContentType 'application/json' -Body '{"muted":true}'
        $null = Invoke-RestMethod -Uri "http://127.0.0.1:$port/api/v1/window/minimize" -Method Post
        $null = Post @{action='menu_select'; option='singleplayer'}
        $state = Wait-State { param($s) $s.menu_screen -in @('singleplayer', 'character_select') } 'character menu'
        if ($state.menu_screen -eq 'singleplayer') { $null = Post @{action='menu_select'; option='standard'} }
        $null = Post @{action='menu_select'; option='KAREN'}
        $null = Post @{action='menu_select'; option='embark'}
        $state = Wait-State { param($s) $s.menu_screen -eq 'tutorial_prompt' -or $s.state_type -eq 'map' } 'run start'
        if ($state.menu_screen -eq 'tutorial_prompt') { $null = Post @{action='menu_select'; option='no'} }
        $null = Wait-State { param($s) $s.state_type -eq 'map' } 'map'
        $null = Post @{action='choose_map_node'; index=0}
        $state = Wait-State { param($s) $s.state_type -eq 'monster' -and $s.battle.is_play_phase } 'combat'
        $state | ConvertTo-Json -Depth 30 | Set-Content -LiteralPath (Join-Path $root 'combat.json') -Encoding UTF8
        $null = Post @{action='run_command'; command='review_fixture downgrade'}
        $passed = $false
        for ($i = 0; $i -lt 40; $i++) {
            $result = Post @{action='run_command'; command='review_status'}
            if ($result.message -eq 'pass:downgrade') { $passed = $true; break }
            Start-Sleep -Milliseconds 250
        }
        if (-not $passed) { throw 'Downgrade fixture timed out' }
        $errors = @(Select-String -LiteralPath $log -Pattern '^\[ERROR\]|^ERROR:|Unhandled exception')
        if ($errors.Count -gt 0) { throw "Game logged $($errors.Count) errors; see $log" }
        $results += @{environment=$branch; result='pass'; message=$result.message; log=$log}
    } catch {
        $results += @{environment=$branch; result='fail'; message=$_.Exception.Message; log=$log}
    } finally {
        if ($null -ne $process) {
            $running = Get-Process -Id $process.Id -ErrorAction SilentlyContinue
            if ($running) {
                if (-not $running.Path.Equals($exe, [StringComparison]::OrdinalIgnoreCase)) { throw 'Unexpected game process' }
                Stop-Process -Id $process.Id
                $running.WaitForExit(5000) | Out-Null
            }
        }
        if ($installed) {
            $resolved = [IO.Path]::GetFullPath($plugin)
            if ($resolved -ne [IO.Path]::GetFullPath((Join-Path $game 'mods/KarenReviewRegression'))) { throw 'Unsafe fixture cleanup' }
            Remove-Item -LiteralPath $resolved -Recurse -Force
        }
    }
    $results[-1] | ConvertTo-Json -Compress
}
$results | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $runRoot 'report.json') -Encoding UTF8
Write-Output "Report: $runRoot/report.json"
if (@($results | Where-Object result -eq 'fail').Count -gt 0) { exit 1 }
