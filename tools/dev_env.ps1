param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('stable', 'beta')]
    [string]$Branch,

    [Parameter(Mandatory = $true)]
    [ValidateSet('Capture', 'Install', 'Launch', 'Smoke', 'Status')]
    [string]$Action,

    [string]$GameSource = 'D:\App\Stream\steamapps\common\Slay the Spire 2',
    [string]$BaseLibSource = 'D:\App\Stream\steamapps\workshop\content\2868840\3737335127\BaseLib',
    [string]$ExtraArguments = ''
)

$ErrorActionPreference = 'Stop'
$project = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$devRoot = [IO.Path]::GetFullPath((Join-Path $project 'artifacts\dev-game'))
$root = [IO.Path]::GetFullPath((Join-Path $devRoot $Branch))
$game = Join-Path $root 'game'
$exe = Join-Path $game 'SlayTheSpire2.exe'
$mods = Join-Path $game 'mods'
$roaming = Join-Path $root 'user\AppData\Roaming'
$local = Join-Path $root 'user\AppData\Local'
$settingsPath = Join-Path $roaming 'SlayTheSpire2\default\1\settings.save'
$log = Join-Path $root 'godot.log'
$expectedVersion = if ($Branch -eq 'stable') { 'v0.107.1' } else { 'v0.111.0' }
$variantName = if ($Branch -eq 'stable') { 'Stable' } else { 'Beta' }
$package = Join-Path $project 'artifacts\ShoujoKagekiAijoKaren'

if (-not $root.StartsWith($devRoot + [IO.Path]::DirectorySeparatorChar,
        [StringComparison]::OrdinalIgnoreCase)) {
    throw "Unsafe environment path: $root"
}

function Get-GameVersion([string]$directory) {
    $releaseInfo = Join-Path $directory 'release_info.json'
    if (-not (Test-Path -LiteralPath $releaseInfo)) { throw "Missing release_info.json: $directory" }
    return (Get-Content -LiteralPath $releaseInfo -Raw -Encoding UTF8 | ConvertFrom-Json).version
}

function Assert-GameVersion([string]$directory) {
    $actual = Get-GameVersion $directory
    if ($actual -ne $expectedVersion) {
        throw "$Branch needs $expectedVersion, but $directory contains $actual"
    }
}

function Get-RunningGame {
    $target = [IO.Path]::GetFullPath($exe)
    return @(Get-Process SlayTheSpire2 -ErrorAction SilentlyContinue |
        Where-Object { $_.Path -and $_.Path.Equals($target, [StringComparison]::OrdinalIgnoreCase) })
}

function Copy-Tree([string]$source, [string]$destination) {
    if (-not (Test-Path -LiteralPath $source -PathType Container)) { throw "Missing source: $source" }
    New-Item -ItemType Directory -Path $destination -Force | Out-Null
    & robocopy $source $destination /E /NFL /NDL /NP /R:1 /W:1 | Out-Null
    if ($LASTEXITCODE -ge 8) { throw "Copy failed ($LASTEXITCODE): $source" }
}

function Start-IsolatedGame([bool]$headless, [int]$quitAfter = 0) {
    Assert-GameVersion $game
    if (-not (Test-Path -LiteralPath $exe)) { throw "Missing game executable: $exe" }
    New-Item -ItemType Directory -Path $roaming,$local -Force | Out-Null

    # Godot resolves user:// from APPDATA on Windows. Disabling Steam also
    # prevents Workshop loading and cloud saves from touching other projects.
    $previousRoaming = $env:APPDATA
    $previousLocal = $env:LOCALAPPDATA
    try {
        $env:APPDATA = $roaming
        $env:LOCALAPPDATA = $local
        $arguments = '--rendering-driver vulkan --force-steam=off --log-file "' + $log + '"'
        if ($headless) { $arguments += " --headless --quit-after $quitAfter" }
        if ($ExtraArguments) { $arguments += " $ExtraArguments" }
        $startArgs = @{
            FilePath = $exe
            ArgumentList = $arguments
            WorkingDirectory = $game
            PassThru = $true
        }
        if ($headless) { $startArgs.WindowStyle = 'Hidden' }
        return Start-Process @startArgs
    }
    finally {
        $env:APPDATA = $previousRoaming
        $env:LOCALAPPDATA = $previousLocal
    }
}

function Enable-IsolatedMods {
    if (-not (Test-Path -LiteralPath $settingsPath)) {
        $process = Start-IsolatedGame $true 300
        $process.WaitForExit()
        if ($process.ExitCode -ne 0) { throw "Initial game startup failed: $($process.ExitCode)" }
    }
    if (-not (Test-Path -LiteralPath $settingsPath)) {
        throw "Game did not create settings: $settingsPath"
    }
    $settings = Get-Content -LiteralPath $settingsPath -Raw -Encoding UTF8 | ConvertFrom-Json
    $settings.mod_settings = [PSCustomObject]@{ mods_enabled = $true; mod_list = @() }
    [IO.File]::WriteAllText($settingsPath, ($settings | ConvertTo-Json -Depth 60),
        [Text.UTF8Encoding]::new($false))
}

function Install-Mods {
    Assert-GameVersion $game
    if ((Get-RunningGame).Count -gt 0) { throw "$Branch game is running; close it before updating mods" }
    foreach ($required in @('ShoujoKagekiAijoKaren.dll', 'ShoujoKagekiAijoKaren.pck',
            'ShoujoKagekiAijoKaren.json', 'lib\ShoujoKagekiAijoKaren.Stable.dll',
            'lib\ShoujoKagekiAijoKaren.Beta.dll')) {
        if (-not (Test-Path -LiteralPath (Join-Path $package $required))) {
            throw "Build the complete package first; missing $required"
        }
    }
    Copy-Tree $BaseLibSource (Join-Path $mods 'BaseLib')
    Copy-Tree $package (Join-Path $mods 'ShoujoKagekiAijoKaren')
    Write-Output "Installed BaseLib and Karen into $mods"
}

switch ($Action) {
    'Capture' {
        Assert-GameVersion $GameSource
        if (Test-Path -LiteralPath $game) { throw "Snapshot already exists: $game" }
        New-Item -ItemType Directory -Path $game -Force | Out-Null
        $excludedMods = @((Join-Path $GameSource 'mods'), (Join-Path $GameSource 'mods_STEAMTEST'))
        & robocopy $GameSource $game /E /XD $excludedMods /NFL /NDL /NP /R:1 /W:1 | Out-Null
        if ($LASTEXITCODE -ge 8) { throw "Game copy failed: $LASTEXITCODE" }
        Assert-GameVersion $game
        Enable-IsolatedMods
        Install-Mods
        Write-Output "Captured $Branch $expectedVersion at $game"
    }
    'Install' { Install-Mods }
    'Launch' {
        Assert-GameVersion $game
        if ((Get-RunningGame).Count -gt 0) { throw "$Branch game is already running" }
        if (-not (Test-Path -LiteralPath (Join-Path $mods 'ShoujoKagekiAijoKaren\ShoujoKagekiAijoKaren.dll'))) {
            throw "Karen is not installed in $mods; run Install first"
        }
        $process = Start-IsolatedGame $false
        Write-Output "Started $Branch $expectedVersion (PID $($process.Id)); log: $log"
    }
    'Smoke' {
        if ((Get-RunningGame).Count -gt 0) { throw "$Branch game is already running" }
        $process = Start-IsolatedGame $true 900
        $process.WaitForExit()
        if ($process.ExitCode -ne 0) { throw "$Branch game exited with $($process.ExitCode)" }
        $contents = Get-Content -LiteralPath $log -Raw -Encoding UTF8
        if (-not $contents.Contains("[KarenLoader] Loaded $variantName implementation")) {
            throw "Karen $variantName did not initialize; inspect $log"
        }
        if (-not $contents.Contains('ModelDb.AllCharacters patched. containsKaren=True')) {
            throw "Karen is missing from the character list; inspect $log"
        }
        $expectedUserData = (Join-Path $roaming 'SlayTheSpire2').Replace('\', '/')
        if (-not $contents.Contains('Steam initialization skipped') -or
            -not $contents.Contains("User Data Directory: $expectedUserData")) {
            throw "Steam or user data was not isolated; inspect $log"
        }
        if ($contents.Contains('Encountered error on game startup') -or
            $contents.Contains('Exception thrown when calling mod initializer')) {
            throw "Game startup reported an error; inspect $log"
        }
        Write-Output "$Branch $expectedVersion smoke check passed; log: $log"
    }
    'Status' {
        if (Test-Path -LiteralPath $game) { Assert-GameVersion $game }
        Write-Output "$Branch expected=$expectedVersion game=$game"
        Write-Output "mods=$mods"
        Write-Output "userData=$(Join-Path $roaming 'SlayTheSpire2')"
        Write-Output "running=$((Get-RunningGame).Count)"
    }
}
