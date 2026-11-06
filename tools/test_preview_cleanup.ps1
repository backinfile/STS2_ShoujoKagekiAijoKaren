# Run with both Windows PowerShell 5.1 and PowerShell 7.
[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$projectRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$fixtureRoot = Join-Path $projectRoot ('artifacts\cleanup-preview-regression\' + [Guid]::NewGuid().ToString('N'))
$planRoot = Join-Path $fixtureRoot 'plan'
$reportRoot = Join-Path $fixtureRoot 'report'
New-Item -ItemType Directory -Path $planRoot | Out-Null
Add-Type -AssemblyName System.IO.Compression, System.IO.Compression.FileSystem

$archiveEntries = @()
$candidates = @()
foreach ($name in @('first', 'second with spaces')) {
    $candidatePath = Join-Path $fixtureRoot ('candidates\' + $name)
    New-Item -ItemType Directory -Path $candidatePath -Force | Out-Null
    $archivePath = Join-Path $planRoot ($name + '-sources.zip')
    $zip = [IO.Compression.ZipFile]::Open($archivePath, [IO.Compression.ZipArchiveMode]::Create)
    $zip.Dispose()
    $archiveEntries += [pscustomobject]@{
        path = $candidatePath
        archive = $archivePath
        sha256 = (Get-FileHash -LiteralPath $archivePath -Algorithm SHA256).Hash
    }
    $candidates += [pscustomobject]@{ path = $candidatePath; bytes = 0 }
}
$archiveEntries | ConvertTo-Json -Depth 3 |
    Set-Content -LiteralPath (Join-Path $planRoot 'source-backups.json') -Encoding UTF8
[pscustomobject]@{
    generatedAt = (Get-Date).ToString('o')
    oldWorktreeRemnants = $candidates
    historicalGameCopies = @()
    historicalVideos = @()
    optionalPythonEnvironment = $null
    protectedWorktrees = @()
} | ConvertTo-Json -Depth 4 |
    Set-Content -LiteralPath (Join-Path $planRoot 'pending-cleanup.json') -Encoding UTF8

& (Join-Path $PSScriptRoot 'preview_cleanup.ps1') `
    -PlanPath (Join-Path $planRoot 'pending-cleanup.json') `
    -ReportDirectory $reportRoot -VerifyArchiveHashes | Out-Null
$report = Get-Content -LiteralPath (Join-Path $reportRoot 'report.json') -Raw -Encoding UTF8 | ConvertFrom-Json
if (@($report.Items).Count -ne 2) { throw 'Expected two individually indexed archive entries.' }
foreach ($item in $report.Items) {
    if ($item.Path -notin $candidates.path) { throw ('Unexpected candidate path: ' + $item.Path) }
    if ($item.Archive -ne 'HashMatched') { throw ('Archive lookup or verification failed: ' + $item.Path) }
    if ($item.Status -ne 'Review') { throw ('Unexpected candidate status: ' + $item.Status) }
    if (-not (Test-Path -LiteralPath $item.Path -PathType Container)) { throw 'Preview changed a candidate.' }
}
Write-Output ('PASS: two scalar archive paths and hashes; PowerShell ' + $PSVersionTable.PSVersion)
$failures = @()

# Each index entry must validate its own expected hash, even when archives are shared.
$archiveEntries[1].archive = $archiveEntries[0].archive
$archiveEntries[1].sha256 = '0' * 64
$archiveEntries | ConvertTo-Json -Depth 3 |
    Set-Content -LiteralPath (Join-Path $planRoot 'source-backups.json') -Encoding UTF8
$sharedReportRoot = Join-Path $fixtureRoot 'shared-archive-report'
& (Join-Path $PSScriptRoot 'preview_cleanup.ps1') `
    -PlanPath (Join-Path $planRoot 'pending-cleanup.json') `
    -ReportDirectory $sharedReportRoot -VerifyArchiveHashes | Out-Null
$sharedReport = Get-Content -LiteralPath (Join-Path $sharedReportRoot 'report.json') -Raw -Encoding UTF8 | ConvertFrom-Json
if ($sharedReport.Items[0].Archive -ne 'HashMatched' -or
    $sharedReport.Items[1].Archive -ne 'HashMismatch' -or
    $sharedReport.Items[1].Status -ne 'NeedsSourceBackup') {
    $failures += 'Shared archives reused another entry''s expected-hash result.'
} else { Write-Output 'PASS: shared archive hashes are compared per index entry.' }

# A lexical artifacts path must not redirect report writes through a junction.
$linkTarget = Join-Path $fixtureRoot 'link-target'
$linkPath = Join-Path $fixtureRoot 'linked-reports'
New-Item -ItemType Directory -Path $linkTarget | Out-Null
New-Item -ItemType Junction -Path $linkPath -Target $linkTarget | Out-Null
try {
    $rejected = $false
    try {
        & (Join-Path $PSScriptRoot 'preview_cleanup.ps1') `
            -PlanPath (Join-Path $planRoot 'pending-cleanup.json') `
            -ReportDirectory (Join-Path $linkPath 'new-report') | Out-Null
    } catch {
        if ($_.Exception.Message -notlike 'Refusing a linked report path:*') { throw }
        $rejected = $true
    }
    if (-not $rejected -or @(Get-ChildItem -LiteralPath $linkTarget -Force).Count) {
        $failures += 'A junction redirected report writes.'
    } else { Write-Output 'PASS: report ancestor junction is rejected before writing.' }
} finally {
    # Remove only the fixture junction, without recursing into the target.
    Remove-Item -LiteralPath $linkPath -Force
}
if ($failures.Count) { throw ($failures -join "`n") }
Write-Output ('Regression report: ' + $reportRoot)
