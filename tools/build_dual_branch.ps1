param([string]$MegaDot = 'D:\Godot\megadot-4.5.1-m.12-windows-x86_64-llvm-editor-csharp\MegaDot_v4.5.1-stable_mono_win64.exe')

$ErrorActionPreference = 'Stop'
$project = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$modId = 'ShoujoKagekiAijoKaren'
$packageRoot = [IO.Path]::GetFullPath((Join-Path $project 'artifacts'))
$modDir = [IO.Path]::GetFullPath((Join-Path $packageRoot $modId))
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
$commonArgs = @('-p:Sts2ReferenceMode=package')
$godotOutput = Join-Path $project '.godot/mono/temp/bin/ExportRelease'

# Build the original project identity first so Godot can export the shared assets.
Build-OrThrow $modProject 'ExportRelease' ($commonArgs + @('-p:Sts2Api=stable'))
Push-Location $project
$previousReferenceMode = $env:Sts2ReferenceMode
$previousApi = $env:Sts2Api
try {
    # Godot invokes dotnet publish during export; keep it on the same stable
    # reference assemblies used by the preceding build.
    $env:Sts2ReferenceMode = 'package'
    $env:Sts2Api = 'stable'
    & $MegaDot --headless --export-pack 'Windows Desktop' (Join-Path $modDir ($modId + '.pck'))
    if ($LASTEXITCODE -ne 0) { throw 'PCK export failed.' }
}
finally {
    $env:Sts2ReferenceMode = $previousReferenceMode
    $env:Sts2Api = $previousApi
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

foreach ($variant in @('Stable', 'Beta')) {
    $api = $variant.ToLowerInvariant()
    $assemblyName = "$modId.$variant"
    Build-OrThrow $modProject 'ExportRelease' ($commonArgs + @("-p:Sts2Api=$api", "-p:AssemblyName=$assemblyName"))
    $source = Join-Path $godotOutput ($assemblyName + '.dll')
    if (-not (Test-Path -LiteralPath $source)) { throw "Missing variant DLL: $source" }
    Copy-Item -LiteralPath $source -Destination (Join-Path $modDir "lib/$assemblyName.dll")
}

Build-OrThrow (Join-Path $project 'loader/KarenModLoader.csproj') 'Release' $commonArgs
Copy-Item -LiteralPath (Join-Path $project 'loader/bin/Release/net9.0/ShoujoKagekiAijoKaren.dll') -Destination (Join-Path $modDir "$modId.dll")
Copy-Item -LiteralPath (Join-Path $project "$modId.json") -Destination $modDir

foreach ($required in @("$modId.dll", "$modId.pck", "$modId.json", "lib/$modId.Stable.dll", "lib/$modId.Beta.dll")) {
    if (-not (Test-Path -LiteralPath (Join-Path $modDir $required))) {
        throw "Incomplete package: $required"
    }
}
Write-Host "Dual-branch package: $modDir"
