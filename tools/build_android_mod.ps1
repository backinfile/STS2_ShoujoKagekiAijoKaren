param(
    [string]$MegaDot,
    [string]$BaseLibDirectory
)

$ErrorActionPreference = 'Stop'
$project = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$modId = 'ShoujoKagekiAijoKaren'
$androidRoot = Join-Path $project 'artifacts/android'
$argsForBuild = @{ Target = 'Android' }
if ($MegaDot) { $argsForBuild.MegaDot = $MegaDot }
& (Join-Path $PSScriptRoot 'build_dual_branch.ps1') @argsForBuild

$modDir = Join-Path $androidRoot $modId
$manifest = Get-Content -LiteralPath (Join-Path $modDir "$modId.json") -Encoding UTF8 -Raw | ConvertFrom-Json
$includedDirs = @($modDir)
if ($BaseLibDirectory) {
    $source = (Resolve-Path -LiteralPath $BaseLibDirectory).Path
    $baseManifest = Get-Content -LiteralPath (Join-Path $source 'BaseLib.json') -Encoding UTF8 -Raw | ConvertFrom-Json
    if ($baseManifest.id -ne 'BaseLib' -or [version]$baseManifest.version.TrimStart('v') -lt [version]'3.2.1') {
        throw 'BaseLib 3.2.1 or newer is required; use a version matching the game and Android compatibility pack.'
    }
    $baseDir = Join-Path $androidRoot 'BaseLib'
    New-Item -ItemType Directory -Path $baseDir -Force | Out-Null
    foreach ($name in @('BaseLib.dll', 'BaseLib.json', 'BaseLib.pck')) {
        Copy-Item -LiteralPath (Join-Path $source $name) -Destination (Join-Path $baseDir $name)
    }
    $includedDirs += $baseDir
}

# Validate before publishing the ZIP; no original game DLLs/native libraries are bundled.
$validation = Join-Path $androidRoot 'validation.json'
& python (Join-Path $PSScriptRoot 'android/validate_package.py') $modDir --output $validation
if ($LASTEXITCODE -ne 0) { throw 'Android package validation failed.' }
if ($BaseLibDirectory) {
    & python (Join-Path $PSScriptRoot 'android/validate_package.py') --assembly (Join-Path $baseDir 'BaseLib.dll')
    if ($LASTEXITCODE -ne 0) { throw 'Bundled BaseLib is not AnyCPU IL.' }
}

$zipPath = Join-Path $androidRoot "$modId-Android-$($manifest.version).zip"
Compress-Archive -LiteralPath $includedDirs -DestinationPath $zipPath -Force
$receipt = [ordered]@{
    target = 'Android Mono / ARM64 launcher'
    supported_game_versions = @('0.107.1', '0.111.0')
    mod_version = $manifest.version
    baselib_bundled = [bool]$BaseLibDirectory
    zip_sha256 = (Get-FileHash -LiteralPath $zipPath -Algorithm SHA256).Hash.ToLowerInvariant()
    validation = 'validation.json'
    android_device_tested = $false
}
$receipt | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $androidRoot 'package-info.json') -Encoding UTF8
Write-Host "Android import ZIP: $zipPath"
