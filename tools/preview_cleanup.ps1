# 清理预览：核对现有清单并生成报告，不删除、移动或执行清单中的文件。
[CmdletBinding()]
param(
    [string]$PlanPath = '',
    [string]$ReportDirectory = '',
    [switch]$VerifyArchiveHashes
)

$ErrorActionPreference = 'Stop'
$projectRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$worktreeRoot = [IO.Path]::GetFullPath((Join-Path $projectRoot '..\worktrees'))
$artifactsRoot = Join-Path $projectRoot 'artifacts'
if (-not $PlanPath) {
    $PlanPath = Join-Path $artifactsRoot 'cleanup-20261016\pending-cleanup.json'
}
$PlanPath = [IO.Path]::GetFullPath($PlanPath)
$planRoot = Split-Path -Parent $PlanPath
$plan = Get-Content -LiteralPath $PlanPath -Raw -Encoding UTF8 | ConvertFrom-Json

function Test-InPath([string]$Path, [string]$Root) {
    $normalizedRoot = [IO.Path]::GetFullPath($Root).TrimEnd('\', '/')
    return $Path.Equals($normalizedRoot, [StringComparison]::OrdinalIgnoreCase) -or
        $Path.StartsWith($normalizedRoot + '\', [StringComparison]::OrdinalIgnoreCase)
}

function Find-ReparseAncestor([string]$Path) {
    $cursor = Get-Item -LiteralPath $Path -Force
    while ($cursor) {
        if ($cursor.Attributes -band [IO.FileAttributes]::ReparsePoint) {
            return $cursor.FullName
        }
        if ($cursor -is [IO.FileInfo]) { $cursor = $cursor.Directory }
        else { $cursor = $cursor.Parent }
    }
    return $null
}

$allowedRoots = @($artifactsRoot, (Join-Path $projectRoot 'output'), $worktreeRoot)
$protectedRoots = @((Join-Path $artifactsRoot 'dev-game'), $planRoot)
$protectedRoots += @($plan.protectedWorktrees)
$worktrees = @(git -C $projectRoot worktree list --porcelain)
if ($LASTEXITCODE -ne 0) { throw 'Cannot read the current Git worktree registry.' }
foreach ($line in $worktrees) {
    if ($line.StartsWith('worktree ')) {
        $tree = [IO.Path]::GetFullPath($line.Substring(9))
        if ($tree -ne $projectRoot) { $protectedRoots += $tree }
    }
}
$protectedRoots = @($protectedRoots | Sort-Object -Unique)

$processScan = 'Complete'
try {
    $processes = @(Get-CimInstance Win32_Process)
    if (@($processes | Where-Object { -not $_.ExecutablePath }).Count) {
        $processScan = 'Partial: some process paths are unavailable'
    }
} catch {
    $processes = @()
    $processScan = 'Unavailable: ' + $_.Exception.Message
}

$archives = @{}
$archiveIndex = Join-Path $planRoot 'source-backups.json'
if (Test-Path -LiteralPath $archiveIndex -PathType Leaf) {
    # Windows PowerShell 5.1 emits a JSON array as one pipeline result.
    $archiveEntries = Get-Content -LiteralPath $archiveIndex -Raw -Encoding UTF8 | ConvertFrom-Json
    foreach ($entry in $archiveEntries) {
        $archives[[IO.Path]::GetFullPath($entry.path)] = $entry
    }
}
$archiveChecks = @{}
$candidates = @(
    foreach ($item in $plan.oldWorktreeRemnants) {
        [pscustomobject]@{ Kind = 'OldWorktree'; Item = $item }
    }
    foreach ($item in $plan.historicalGameCopies) {
        [pscustomobject]@{ Kind = 'HistoricalGame'; Item = $item }
    }
    foreach ($item in $plan.historicalVideos) {
        [pscustomobject]@{ Kind = 'HistoricalVideo'; Item = $item }
    }
    if ($plan.optionalPythonEnvironment) {
        [pscustomobject]@{ Kind = 'PythonEnvironment'; Item = $plan.optionalPythonEnvironment }
    }
)

$rows = foreach ($candidate in $candidates) {
    $path = [IO.Path]::GetFullPath($candidate.Item.path)
    $status = 'Review'
    $reason = 'Preview only; no deletion was performed or approved by this script.'
    $archiveStatus = ''
    $runningIds = @()
    $inAllowedRoot = @($allowedRoots | Where-Object {
        (Test-InPath $path $_) -and $path -ne $_
    }).Count -gt 0
    $touchesProtectedRoot = @($protectedRoots | Where-Object {
        (Test-InPath $path $_) -or (Test-InPath ([IO.Path]::GetFullPath($_)) $path)
    }).Count -gt 0

    if (-not $inAllowedRoot) {
        $status = 'OutsideScope'; $reason = 'Outside the listed artifact and worktree roots.'
    } elseif ($touchesProtectedRoot) {
        $status = 'Protected'; $reason = 'Touches a protected environment, worktree or backup directory.'
    } elseif (-not (Test-Path -LiteralPath $path)) {
        $status = 'Missing'; $reason = 'The listed path is no longer present.'
    } else {
        $link = Find-ReparseAncestor $path
        $runningIds = @($processes | Where-Object {
            ($_.ExecutablePath -and (Test-InPath $_.ExecutablePath $path)) -or
            ($_.CommandLine -and $_.CommandLine.IndexOf($path, [StringComparison]::OrdinalIgnoreCase) -ge 0)
        } | Select-Object -ExpandProperty ProcessId)
        if ($link) {
            $status = 'ReparsePoint'; $reason = 'Path passes through a junction or symbolic link: ' + $link
        } elseif ($runningIds.Count) {
            $status = 'InUse'; $reason = 'A running process refers to this path.'
        } elseif ($candidate.Kind -eq 'OldWorktree') {
            if (-not $archives.ContainsKey($path)) {
                $status = 'NeedsSourceBackup'; $reason = 'No indexed source archive was found.'
            } else {
                $entry = $archives[$path]
                $archivePath = [IO.Path]::GetFullPath($entry.archive)
                if (-not (Test-InPath $archivePath $planRoot)) {
                    $archiveStatus = 'OutsideBackupDirectory'
                } elseif (-not (Test-Path -LiteralPath $archivePath -PathType Leaf)) {
                    $archiveStatus = 'Missing'
                } elseif (-not $VerifyArchiveHashes) {
                    $archiveStatus = 'Present; hash not checked'
                } else {
                    if (-not $archiveChecks.ContainsKey($archivePath)) {
                        $archiveChecks[$archivePath] = (Get-FileHash -LiteralPath $archivePath -Algorithm SHA256).Hash
                    }
                    $archiveStatus = if ($archiveChecks[$archivePath] -eq $entry.sha256) { 'HashMatched' } else { 'HashMismatch' }
                }
                if ($archiveStatus -in @('Missing', 'HashMismatch', 'OutsideBackupDirectory')) {
                    $status = 'NeedsSourceBackup'; $reason = 'Source archive is missing or failed validation.'
                }
            }
        } elseif ($candidate.Kind -eq 'HistoricalGame') {
            if (-not (Test-Path -LiteralPath (Join-Path $path 'SlayTheSpire2.exe') -PathType Leaf) -or
                -not (Test-Path -LiteralPath (Join-Path $path 'SlayTheSpire2.pck') -PathType Leaf)) {
                $status = 'MarkersMissing'; $reason = 'The expected game-copy files are missing.'
            }
        }
    }
    [pscustomobject]@{
        Kind = $candidate.Kind
        Path = $path
        Status = $status
        Reason = $reason
        ListedLogicalBytes = $candidate.Item.bytes
        Archive = $archiveStatus
        ProcessIds = $runningIds -join ','
    }
}

if (-not $ReportDirectory) {
    $ReportDirectory = Join-Path $planRoot ('preview-' + (Get-Date -Format 'yyyyMMdd-HHmmss-fff'))
}
$ReportDirectory = [IO.Path]::GetFullPath($ReportDirectory)
if (-not (Test-InPath $ReportDirectory $artifactsRoot) -or $ReportDirectory -eq $artifactsRoot) {
    throw 'Reports must be written to a subdirectory of the project artifacts directory.'
}
if (Test-Path -LiteralPath $ReportDirectory) { throw 'Report directory already exists; choose a fresh path.' }
# Validate the nearest existing ancestor before creating any report directories.
# GetFullPath normalizes text but does not resolve junctions or symbolic links.
$reportAncestor = Split-Path -Parent $ReportDirectory
while ($reportAncestor -and -not (Test-Path -LiteralPath $reportAncestor)) {
    $reportAncestor = Split-Path -Parent $reportAncestor
}
if (-not $reportAncestor) { throw 'Cannot locate the report directory ancestor.' }
$reportLink = Find-ReparseAncestor $reportAncestor
if ($reportLink) { throw ('Refusing a linked report path: ' + $reportLink) }
New-Item -ItemType Directory -Path $ReportDirectory | Out-Null
$rows | Export-Csv -LiteralPath (Join-Path $ReportDirectory 'items.csv') -NoTypeInformation -Encoding UTF8
[pscustomobject]@{
    GeneratedAt = (Get-Date).ToString('o')
    Mode = 'PreviewOnly'
    PlanPath = $PlanPath
    SourcePlanDate = $plan.generatedAt
    ProcessScan = $processScan
    Notes = @(
        'No candidate was deleted, moved or executed.'
        'Listed sizes come from the input plan and may be stale.'
        'Hard links and overlapping candidates prevent adding sizes as reclaimable disk space.'
        'Ancestor links are detected; directory descendants are not traversed.'
        'An archive hash match does not prove that live source files have not changed since backup.'
        'Review status is not execution-policy approval.'
    )
    ProtectedRoots = $protectedRoots
    Items = @($rows)
} | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $ReportDirectory 'report.json') -Encoding UTF8
$rows | Group-Object Kind, Status | Select-Object Name, Count | Format-Table -AutoSize
Write-Output ('Preview report: ' + $ReportDirectory)
