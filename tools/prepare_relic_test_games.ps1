param([string]$SourceRoot = 'D:\Godot\Proj\STS2_ShoujoKagekiAijoKaren\artifacts\dev-game')
$ErrorActionPreference = 'Stop'
$project = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
foreach ($branch in @('stable','beta')) {
    $source = Join-Path $SourceRoot "$branch/game"
    $destination = Join-Path $project "artifacts/dev-game/$branch/game"
    New-Item -ItemType Directory -Path $destination -Force | Out-Null
    # Only immutable game files are hard-linked. Mods and user data are separate copies.
    Get-ChildItem -LiteralPath $source -Recurse -File | Where-Object {
        -not $_.FullName.Substring($source.Length + 1).StartsWith('mods\')
    } | ForEach-Object {
        $relative = $_.FullName.Substring($source.Length + 1)
        $target = Join-Path $destination $relative
        New-Item -ItemType Directory -Path (Split-Path $target) -Force | Out-Null
        if (-not (Test-Path -LiteralPath $target)) {
            New-Item -ItemType HardLink -Path $target -Target $_.FullName | Out-Null
        }
    }
    $mods = Join-Path $destination 'mods'
    New-Item -ItemType Directory -Path $mods -Force | Out-Null
    foreach ($mod in @('BaseLib','KarenSTS2MCP.dll','KarenSTS2MCP.json')) {
        Copy-Item -LiteralPath (Join-Path $source "mods/$mod") -Destination $mods -Recurse -Force
    }
    $port = if ($branch -eq 'stable') { 15657 } else { 15658 }
    @{port=$port;fullscreen=$false;muted=$true;skip_tutorial=$true;skip_first_prompt=$true;window_width=1280;window_height=720} |
        ConvertTo-Json | Set-Content -LiteralPath (Join-Path $mods 'KarenSTS2MCP.conf') -Encoding UTF8
    $settingsSource = Join-Path $SourceRoot "$branch/user/AppData/Roaming/SlayTheSpire2/default/1/settings.save"
    $settingsTarget = Join-Path $project "artifacts/dev-game/$branch/user/AppData/Roaming/SlayTheSpire2/default/1/settings.save"
    New-Item -ItemType Directory -Path (Split-Path $settingsTarget) -Force | Out-Null
    Copy-Item -LiteralPath $settingsSource -Destination $settingsTarget -Force
    Write-Host "$branch isolated on port $port"
}
