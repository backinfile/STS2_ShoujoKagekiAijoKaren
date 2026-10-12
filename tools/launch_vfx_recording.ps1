param(
    [ValidateSet('stable', 'beta')][string]$Environment = 'stable',
    [string]$Scenes = '',
    [string]$OutputRoot = '',
    [string]$UserSource = ''
)

# 全览与单段共用隔离录制流程，保留游戏进程音频和移牌验证。
$ErrorActionPreference = 'Stop'
$project = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
if (-not $OutputRoot) { $OutputRoot = Join-Path $project ('artifacts/vfx-catalog-' + [DateTime]::Now.ToString('yyyyMMdd')) }
Push-Location $project
try {
    if (-not (Test-Path -LiteralPath (Join-Path $OutputRoot "isolated/$Environment/game/SlayTheSpire2.exe"))) {
        $prepareArgs = @('tools/vfx/catalog/prepare.py', $Environment, '--output', $OutputRoot)
        if ($UserSource) { $prepareArgs += @('--user-source', $UserSource) }
        & python @prepareArgs
        if ($LASTEXITCODE -ne 0) { throw 'Recording environment preparation failed.' }
    }
    $recordArgs = @('-u', 'tools/vfx/catalog/record.py', $Environment, '--output', $OutputRoot)
    if ($Scenes) { $recordArgs += @('--scenes', $Scenes) }
    & python @recordArgs
    if ($LASTEXITCODE -ne 0) { throw 'VFX recording failed; see the isolated log and per-scene evidence.' }
}
finally { Pop-Location }
