param(
    [string]$CorePackage = $env:SHOUJO_CORE_PACKAGE,
    [string]$StableGameDir = $env:SHOUJO_STABLE_GAME,
    [string]$BetaGameDir = $env:SHOUJO_BETA_GAME,
    [string]$MegaDot = 'D:\Godot\megadot-4.5.1-m.12-windows-x86_64-llvm-editor-csharp\MegaDot_v4.5.1-stable_mono_win64.exe',
    [ValidateSet('Desktop', 'Android')][string]$Target = 'Desktop',
    [string]$MainGameDir = $env:SHOUJO_MAIN_GAME
)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'core-package.ps1')
$core = Get-CorePackage $CorePackage
$project = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
if (-not $BetaGameDir) { $BetaGameDir = Join-Path $project 'artifacts/dev-game/beta/game' }
$BetaGameDir = [IO.Path]::GetFullPath($BetaGameDir)
$betaRelease = Join-Path $BetaGameDir 'release_info.json'
if (-not (Test-Path -LiteralPath $betaRelease)) { throw 'Set -BetaGameDir or SHOUJO_BETA_GAME to the supported beta game.' }
$actualBeta = (Get-Content -LiteralPath $betaRelease -Raw -Encoding UTF8 | ConvertFrom-Json).version.TrimStart('v')
if ($actualBeta -ne $core.Bundle.targets.beta.minimumGameVersion.TrimStart('v')) { throw 'Beta game does not match the selected Core package.' }
if ($StableGameDir) { $StableGameDir = [IO.Path]::GetFullPath($StableGameDir) }
$variants = [ordered]@{ Stable = $StableGameDir }
if ($Target -eq 'Desktop') {
    if (-not $core.Bundle.targets.main) { throw 'The desktop package requires Core with a Main runtime.' }
    if (-not $MainGameDir) { $MainGameDir = Join-Path $project 'artifacts/dev-game/main/game' }
    $MainGameDir = [IO.Path]::GetFullPath($MainGameDir)
    $mainRelease = Join-Path $MainGameDir 'release_info.json'
    if (-not (Test-Path -LiteralPath $mainRelease)) { throw 'Set -MainGameDir or SHOUJO_MAIN_GAME to the v0.111.1 game.' }
    $actualMain = (Get-Content -LiteralPath $mainRelease -Raw -Encoding UTF8 | ConvertFrom-Json).version.TrimStart('v')
    if ($actualMain -ne '0.111.1') { throw "Main reference game is v$actualMain; expected v0.111.1." }
    $variants.Add('Main', $MainGameDir)
}
$variants.Add('Beta', $BetaGameDir)

$modId = 'ShoujoKagekiAijoKaren'
$artifactRoot = [IO.Path]::GetFullPath((Join-Path $project 'artifacts'))
$packageRoot = if ($Target -eq 'Android') { Join-Path $artifactRoot 'android' } else { $artifactRoot }
$exportPreset = if ($Target -eq 'Android') { 'Android Mod Resources' } else { 'Windows Desktop' }
$modDir = [IO.Path]::GetFullPath((Join-Path $packageRoot $modId))
# Reference extracts may contain native resource UIDs; exclude them before Godot scans.
New-Item -ItemType Directory -Force -Path $packageRoot | Out-Null
New-Item -ItemType File -Force -Path (Join-Path $artifactRoot '.gdignore') | Out-Null
if (-not (Test-Path -LiteralPath $MegaDot)) {
    throw "MegaDot missing: $MegaDot"
}
if (-not (Test-Path -LiteralPath (Join-Path $project 'export_presets.cfg'))) {
    throw 'export_presets.cfg is required to export the PCK.'
}
if (-not $modDir.StartsWith($packageRoot + [IO.Path]::DirectorySeparatorChar,
        [StringComparison]::OrdinalIgnoreCase)) {
    throw "Unsafe package path: $modDir"
}

if (Test-Path -LiteralPath $modDir) {
    Remove-Item -LiteralPath $modDir -Recurse -Force
}
New-Item -ItemType Directory -Force -Path (Join-Path $modDir 'lib') | Out-Null

function Build-OrThrow([string]$projectPath, [string]$configuration, [string[]]$extraArgs) {
    & dotnet build $projectPath -c $configuration --nologo -v:q -clp:ErrorsOnly @extraArgs
    if ($LASTEXITCODE -ne 0) { throw "Build failed: $projectPath ($configuration)" }
}

$modProject = Join-Path $project ($modId + '.csproj')
$referenceMode = if ($StableGameDir) { 'local' } else { 'package' }
$commonArgs = @("-p:Sts2ReferenceMode=$referenceMode", "-p:CoreApiPath=$($core.ApiPath)")
if ($StableGameDir) { $commonArgs += "-p:Sts2GameDir=$StableGameDir" }
$commonArgs += if ($Target -eq 'Android') { '-p:KarenAndroidBuild=true' } else { '-p:KarenAndroidBuild=false' }
$godotOutput = Join-Path $project '.godot/mono/temp/bin/ExportRelease'

# Build the original project identity first so Godot can export the shared assets.
Build-OrThrow $modProject 'ExportRelease' ($commonArgs + @('-p:Sts2Api=stable'))
Push-Location $project
$previousReferenceMode = $env:Sts2ReferenceMode
$previousGameDir = $env:Sts2GameDir
$previousApi = $env:Sts2Api
$previousAndroidBuild = $env:KarenAndroidBuild
$previousCoreApi = $env:CoreApiPath
try {
    # Godot invokes dotnet publish during export; keep it on the same stable
    # reference assemblies used by the preceding build.
    $env:CoreApiPath = $core.ApiPath
    $env:Sts2ReferenceMode = $referenceMode
    if ($StableGameDir) { $env:Sts2GameDir = $StableGameDir }
    $env:Sts2Api = 'stable'
    $env:KarenAndroidBuild = if ($Target -eq 'Android') { 'true' } else { 'false' }
    & $MegaDot --headless --export-pack $exportPreset (Join-Path $modDir ($modId + '.pck'))
    if ($LASTEXITCODE -ne 0) { throw 'PCK export failed.' }
}
finally {
    $env:Sts2ReferenceMode = $previousReferenceMode
    $env:Sts2GameDir = $previousGameDir
    $env:Sts2Api = $previousApi
    $env:KarenAndroidBuild = $previousAndroidBuild
    $env:CoreApiPath = $previousCoreApi
    Pop-Location
}

# MegaDot can return before its exporter finishes writing the pack.
$pckPath = Join-Path $modDir ($modId + '.pck')
$deadline = [DateTime]::UtcNow.AddMinutes(2)
$previousSize = -1L
$stableChecks = 0
while ([DateTime]::UtcNow -lt $deadline) {
    if (Test-Path -LiteralPath $pckPath) {
        $size = (Get-Item -LiteralPath $pckPath).Length
        if ($size -gt 1MB -and $size -eq $previousSize) { $stableChecks++ } else { $stableChecks = 0 }
        if ($stableChecks -ge 3) { break }
        $previousSize = $size
    }
    Start-Sleep -Milliseconds 500
}
if ($stableChecks -lt 3) { throw 'PCK export did not finish.' }

foreach ($variant in $variants.Keys) {
    $api = $variant.ToLowerInvariant()
    $assemblyName = "$modId.$variant"
    $variantArgs = $commonArgs
    if ($variant -ne 'Stable') {
        $variantArgs = @($commonArgs | Where-Object { $_ -notlike '-p:Sts2ReferenceMode=*' -and $_ -notlike '-p:Sts2GameDir=*' })
        $variantArgs += '-p:Sts2ReferenceMode=local', "-p:Sts2GameDir=$($variants[$variant])"
    }
    Build-OrThrow $modProject 'ExportRelease' ($variantArgs + @("-p:Sts2Api=$api", "-p:AssemblyName=$assemblyName"))
    $source = Join-Path $godotOutput ($assemblyName + '.dll')
    if (-not (Test-Path -LiteralPath $source)) { throw "Missing variant DLL: $source" }
    Copy-Item -LiteralPath $source -Destination (Join-Path $modDir "lib/$assemblyName.dll")
}

Build-OrThrow (Join-Path $project 'loader/KarenModLoader.csproj') 'Release' $commonArgs
Copy-Item -LiteralPath (Join-Path $project 'loader/bin/Release/net9.0/ShoujoKagekiAijoKaren.dll') -Destination (Join-Path $modDir "$modId.dll")
Copy-Item -LiteralPath (Join-Path $project "$modId.json") -Destination $modDir

$variantFiles = @($variants.Keys | ForEach-Object { "lib/$modId.$_.dll" })
foreach ($required in (@("$modId.dll", "$modId.pck", "$modId.json") + $variantFiles)) {
    if (-not (Test-Path -LiteralPath (Join-Path $modDir $required))) {
        throw "Incomplete package: $required"
    }
}
Write-Host "$Target package ($($variants.Keys -join ', ')): $modDir"

Copy-CoreRolePackage $core $modDir $packageRoot $modId
if ($Target -eq 'Desktop') {
    $workshopRoot = Join-Path $artifactRoot 'workshop'
    New-Item -ItemType Directory -Force -Path $workshopRoot | Out-Null
    foreach ($name in @($modId, 'ShoujoKagekiAijoCore')) {
        $destination = [IO.Path]::GetFullPath((Join-Path $workshopRoot $name))
        if (-not $destination.StartsWith($workshopRoot + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
            throw 'Unsafe workshop package path.'
        }
        if (Test-Path -LiteralPath $destination) { Remove-Item -LiteralPath $destination -Recurse -Force }
        Copy-Item -LiteralPath (Join-Path $packageRoot $name) -Destination $destination -Recurse
    }
    $vdf = Get-Content -LiteralPath (Join-Path $project 'workshop_item.vdf') -Raw -Encoding UTF8
    $vdf = $vdf.Replace('<workshop-content>', $workshopRoot.Replace('\', '\\'))
    $vdf = $vdf.Replace('<preview-image>', (Join-Path $project 'workshop_preview.jpg').Replace('\', '\\'))
    [IO.File]::WriteAllText((Join-Path $artifactRoot 'workshop_item.vdf'), $vdf, [Text.UTF8Encoding]::new($false))
}
