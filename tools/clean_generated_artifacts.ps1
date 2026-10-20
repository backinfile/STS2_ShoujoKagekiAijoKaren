#requires -Version 7.0
# Default is inspection only. -Execute requires approval through the calling host.
[CmdletBinding()]
param(
    [switch]$Execute,
    [string]$PlanPath = ''
)

$ErrorActionPreference = 'Stop'
$projectRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$artifactsRoot = Join-Path $projectRoot 'artifacts'
$worktreesRoot = [IO.Path]::GetFullPath((Join-Path $projectRoot '..\worktrees'))
if (-not $PlanPath) { $PlanPath = Join-Path $artifactsRoot 'cleanup-20261016\pending-cleanup.json' }
$PlanPath = [IO.Path]::GetFullPath($PlanPath)
$backupRoot = Split-Path -Parent $PlanPath
$plan = Get-Content -LiteralPath $PlanPath -Raw -Encoding UTF8 | ConvertFrom-Json -AsHashtable

function Test-InPath([string]$Path, [string]$Root) {
    $rootPath = [IO.Path]::GetFullPath($Root).TrimEnd('\', '/')
    return $Path.Equals($rootPath, [StringComparison]::OrdinalIgnoreCase) -or
        $Path.StartsWith($rootPath + '\', [StringComparison]::OrdinalIgnoreCase)
}

function Assert-NoAncestorLink([string]$Path) {
    $cursor = Get-Item -LiteralPath $Path -Force
    while ($cursor) {
        if ($cursor.Attributes -band [IO.FileAttributes]::ReparsePoint) {
            throw ('Refusing a linked cleanup path: ' + $cursor.FullName)
        }
        $cursor = if ($cursor -is [IO.FileInfo]) { $cursor.Directory } else { $cursor.Parent }
    }
}

if (-not (Test-InPath $backupRoot $artifactsRoot) -or $backupRoot -eq $artifactsRoot) {
    throw 'The plan and backup records must reside in a project artifacts subdirectory.'
}
Assert-NoAncestorLink $backupRoot
$protected = @((Join-Path $artifactsRoot 'dev-game'), $backupRoot) + @($plan.protectedWorktrees)
$registry = @(git -C $projectRoot worktree list --porcelain)
if ($LASTEXITCODE -ne 0) { throw 'Could not read the current worktree registry.' }
foreach ($line in $registry) {
    if ($line.StartsWith('worktree ')) {
        $registeredPath = [IO.Path]::GetFullPath($line.Substring(9))
        if ($registeredPath -ne $projectRoot) { $protected += $registeredPath }
    }
}
$protected = @($protected | Sort-Object -Unique)
$allowed = @($artifactsRoot, (Join-Path $projectRoot 'output'), $worktreesRoot)

function Assert-TargetScope([string]$Path) {
    if (-not @($allowed | Where-Object { (Test-InPath $Path $_) -and $Path -ne $_ }).Count) {
        throw ('Outside approved cleanup roots: ' + $Path)
    }
    foreach ($root in $protected) {
        if ((Test-InPath $Path $root) -or (Test-InPath ([IO.Path]::GetFullPath($root)) $Path)) {
            throw ('Overlaps a protected environment or backup: ' + $Path)
        }
    }
}

$targets = @(
    foreach ($item in @($plan.oldWorktreeRemnants | Sort-Object bytes)) {
        [pscustomobject]@{ Kind = 'OldWorktree'; Path = [IO.Path]::GetFullPath($item.path) }
    }
    foreach ($item in $plan.historicalGameCopies) {
        [pscustomobject]@{ Kind = 'HistoricalGame'; Path = [IO.Path]::GetFullPath($item.path) }
    }
    foreach ($item in $plan.historicalVideos) {
        [pscustomobject]@{ Kind = 'HistoricalVideo'; Path = [IO.Path]::GetFullPath($item.path) }
    }
    if ($plan.optionalPythonEnvironment) {
        [pscustomobject]@{ Kind = 'PythonEnvironment'; Path = [IO.Path]::GetFullPath($plan.optionalPythonEnvironment.path) }
    }
)
foreach ($target in $targets) { Assert-TargetScope $target.Path }
if (-not $Execute) {
    $targets | Format-Table Kind, Path -Wrap
    Write-Output 'Inspection only. No cleanup operation was executed.'
    return
}

# Fail closed if process inspection is unavailable; the host must approve escalation.
$processes = @(Get-CimInstance Win32_Process -ErrorAction Stop)
$backups = @{}
$indexPath = Join-Path $backupRoot 'source-backups.json'
foreach ($entry in (Get-Content -LiteralPath $indexPath -Raw -Encoding UTF8 | ConvertFrom-Json -AsHashtable)) {
    $signatures = @{}
    foreach ($key in $entry.signatures.Keys) { $signatures[$key.Replace('\', '/')] = $entry.signatures[$key] }
    $backups[[IO.Path]::GetFullPath($entry.path)] = @{
        Archive = $entry.archive; Hash = $entry.sha256; Signatures = $signatures
    }
}
$supplementPath = Join-Path $backupRoot 'supplemental-backups.json'
if (Test-Path -LiteralPath $supplementPath -PathType Leaf) {
    $supplement = Get-Content -LiteralPath $supplementPath -Raw -Encoding UTF8 | ConvertFrom-Json -AsHashtable
    foreach ($entry in $supplement.backups) {
        $signatures = @{}
        foreach ($signature in $entry.signatures) { $signatures[$signature.path.Replace('\', '/')] = $signature }
        $backups[[IO.Path]::GetFullPath($entry.path)] = @{
            Archive = $entry.archive; Hash = $entry.archiveSha256; Signatures = $signatures
        }
    }
}

function Read-TreeWithoutFollowingLinks([string]$Path) {
    $files = [Collections.Generic.List[object]]::new()
    $links = [Collections.Generic.List[object]]::new()
    $pending = [Collections.Generic.Stack[string]]::new()
    $pending.Push($Path)
    while ($pending.Count) {
        foreach ($child in Get-ChildItem -LiteralPath $pending.Pop() -Force -ErrorAction Stop) {
            if ($child.Attributes -band [IO.FileAttributes]::ReparsePoint) { $links.Add($child) }
            elseif ($child.PSIsContainer) { $pending.Push($child.FullName) }
            else { $files.Add($child) }
        }
    }
    return @{ Files = $files; Links = $links }
}

function Test-SourceFile([string]$RelativePath) {
    $parts = $RelativePath.Split('/')
    $generatedDirectories = @('.git', '.godot', '.vs', '.dotnet_home', '.claude', 'artifacts', 'output', 'bin', 'obj', '__pycache__', '.venv')
    if (@($parts | Where-Object { $_ -in $generatedDirectories }).Count) { return $false }
    $extension = [IO.Path]::GetExtension($RelativePath).ToLowerInvariant()
    if ($extension -in @('.dll', '.pdb', '.pyc', '.pck', '.tmp')) { return $false }
    if ($RelativePath -eq 'server.pid') { return $false }
    return $true
}

function Assert-SourceBackup([string]$Path, $Tree) {
    if (-not $Tree.Files.Count) { return }
    if (-not $backups.ContainsKey($Path)) { throw 'No indexed source backup exists.' }
    $backup = $backups[$Path]
    $archivePath = [IO.Path]::GetFullPath($backup.Archive)
    if (-not (Test-InPath $archivePath $backupRoot)) { throw 'Archive lies outside the backup directory.' }
    Assert-NoAncestorLink $archivePath
    if ((Get-FileHash -LiteralPath $archivePath -Algorithm SHA256).Hash -ne $backup.Hash) {
        throw 'Source archive checksum mismatch.'
    }
    foreach ($file in $Tree.Files) {
        $relativePath = $file.FullName.Substring($Path.Length).TrimStart('\', '/').Replace('\', '/')
        if ($backup.Signatures.ContainsKey($relativePath)) {
            $signature = $backup.Signatures[$relativePath]
            if ($file.Length -ne $signature.bytes -or
                (Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash -ne $signature.sha256) {
                throw ('Live file differs from its backup: ' + $relativePath)
            }
        } elseif (Test-SourceFile $relativePath) {
            throw ('Source file has no backup: ' + $relativePath)
        }
    }
}

$runRoot = Join-Path $backupRoot ('execution-' + (Get-Date -Format 'yyyyMMdd-HHmmss-fff'))
New-Item -ItemType Directory -Path $runRoot -ErrorAction Stop | Out-Null
$beforeFree = (Get-PSDrive D).Free
$primaryFiles = @(
    foreach ($version in @('stable', 'beta')) {
        foreach ($name in @('SlayTheSpire2.exe', 'SlayTheSpire2.pck')) {
            Get-FileHash -LiteralPath (Join-Path $artifactsRoot "dev-game\$version\game\$name") -Algorithm SHA256
        }
    }
)
[pscustomobject]@{
    StartedAt = (Get-Date).ToString('o'); PlanPath = $PlanPath
    PlanSha256 = (Get-FileHash -LiteralPath $PlanPath -Algorithm SHA256).Hash
    BeforeFreeBytes = $beforeFree; ProtectedRoots = $protected; PrimaryFiles = $primaryFiles
} | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $runRoot 'before.json') -Encoding UTF8
$results = [Collections.Generic.List[object]]::new()
$removedDirectories = [Collections.Generic.List[string]]::new()
Write-Output ('Execution journal: ' + $runRoot)

foreach ($target in $targets) {
    $path = $target.Path
    $status = 'Pending'; $detail = ''; $unlinked = @()
    try {
        Assert-TargetScope $path
        if (-not (Test-Path -LiteralPath $path)) {
            $covering = @($removedDirectories | Where-Object { Test-InPath $path $_ } | Select-Object -First 1)
            $status = if ($covering.Count) { 'CoveredByRemovedDirectory' } else { 'AlreadyMissing' }
            $detail = $covering -join ''
        } else {
            Assert-NoAncestorLink $path
            $processes = @(Get-CimInstance Win32_Process -ErrorAction Stop)
            $users = @($processes | Where-Object {
                ($_.ExecutablePath -and (Test-InPath $_.ExecutablePath $path)) -or
                ($_.CommandLine -and $_.CommandLine.IndexOf($path, [StringComparison]::OrdinalIgnoreCase) -ge 0)
            })
            if ($users.Count) { throw ('Path is in use by process IDs: ' + (($users.ProcessId) -join ',')) }
            $node = Get-Item -LiteralPath $path -Force
            if ($target.Kind -eq 'HistoricalVideo') {
                if ($node.PSIsContainer -or $node.Extension.ToLowerInvariant() -notin @('.mp4', '.avi', '.mkv', '.mov', '.webm')) {
                    throw 'Not an expected video file.'
                }
                Remove-Item -LiteralPath $path -Force -ErrorAction Stop
            } else {
                if (-not $node.PSIsContainer) { throw 'Not a directory.' }
                if ($target.Kind -eq 'HistoricalGame' -and
                    (-not (Test-Path -LiteralPath (Join-Path $path 'SlayTheSpire2.exe') -PathType Leaf) -or
                     -not (Test-Path -LiteralPath (Join-Path $path 'SlayTheSpire2.pck') -PathType Leaf))) {
                    throw 'Historical game-copy markers are missing.'
                }
                if ($target.Kind -eq 'PythonEnvironment' -and
                    ($node.Name -ne '.venv' -or -not (Test-Path -LiteralPath (Join-Path $path 'pyvenv.cfg') -PathType Leaf))) {
                    throw 'Python environment markers are missing.'
                }
                Write-Output ('Checking ' + $target.Kind + ': ' + $path)
                $tree = Read-TreeWithoutFollowingLinks $path
                if ($target.Kind -eq 'OldWorktree') { Assert-SourceBackup $path $tree }
                foreach ($link in $tree.Links) {
                    if (-not (Test-InPath $link.FullName $path) -or $link.FullName -eq $path) { throw 'Unexpected link path.' }
                    $current = Get-Item -LiteralPath $link.FullName -Force
                    if (-not ($current.Attributes -band [IO.FileAttributes]::ReparsePoint)) { throw 'Link changed during validation.' }
                    # Remove only the link itself; never recurse into its target.
                    Remove-Item -LiteralPath $link.FullName -Force -ErrorAction Stop
                    $unlinked += $link.FullName
                }
                Remove-Item -LiteralPath $path -Recurse -Force -ErrorAction Stop
                $removedDirectories.Add($path)
            }
            if (Test-Path -LiteralPath $path) { throw 'The path still exists after removal.' }
            $status = 'Removed'
        }
    } catch {
        $status = 'FailedOrSkipped'; $detail = $_.Exception.Message
        Write-Output ('Skipped: ' + $path + ' -- ' + $detail)
    }
    $entry = [pscustomobject]@{
        Time = (Get-Date).ToString('o'); Kind = $target.Kind; Path = $path
        Status = $status; Detail = $detail; LinksRemoved = @($unlinked)
    }
    $results.Add($entry)
    $entry | ConvertTo-Json -Depth 3 -Compress | Add-Content -LiteralPath (Join-Path $runRoot 'items.jsonl') -Encoding UTF8
    if (($results.Count % 25) -eq 0) { Write-Output ("Processed $($results.Count)/$($targets.Count) items") }
}

$primaryVerified = $true
foreach ($file in $primaryFiles) {
    if (-not (Test-Path -LiteralPath $file.Path -PathType Leaf) -or
        (Get-FileHash -LiteralPath $file.Path -Algorithm SHA256).Hash -ne $file.Hash) {
        $primaryVerified = $false
    }
}
$afterFree = (Get-PSDrive D).Free
$summary = [pscustomobject]@{
    CompletedAt = (Get-Date).ToString('o'); Mode = 'Execute'
    BeforeFreeBytes = $beforeFree; AfterFreeBytes = $afterFree
    ObservedNetFreeIncreaseBytes = $afterFree - $beforeFree
    PrimaryFilesVerified = $primaryVerified
    Counts = @($results | Group-Object Kind, Status | Select-Object Name, Count)
    Results = $results.ToArray()
}
$summary | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $runRoot 'result.json') -Encoding UTF8
$results | Export-Csv -LiteralPath (Join-Path $runRoot 'items.csv') -NoTypeInformation -Encoding UTF8
$summary.Counts | Format-Table -AutoSize
Write-Output ('Observed free-space increase: {0:N2} GiB' -f (($afterFree - $beforeFree) / 1GB))
Write-Output ('Protected primary files verified: ' + $primaryVerified)
Write-Output ('Report: ' + (Join-Path $runRoot 'result.json'))
if (-not $primaryVerified) { throw 'Protected primary file verification failed.' }
