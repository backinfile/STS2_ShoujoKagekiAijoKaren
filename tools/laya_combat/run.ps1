# Examples: ./run.ps1 --inspect; ./run.ps1; ./run.ps1 --execute --loop
$ErrorActionPreference = 'Stop'
$env:PYTHONUTF8 = '1'
$venvPython = Join-Path $PSScriptRoot '.venv/Scripts/python.exe'
if (-not (Test-Path -LiteralPath $venvPython)) {
    $bundledPython = Join-Path $env:USERPROFILE '.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
    if (Test-Path -LiteralPath $bundledPython) {
        & $bundledPython -m venv (Join-Path $PSScriptRoot '.venv')
    } else {
        & py -3 -m venv (Join-Path $PSScriptRoot '.venv')
    }
    if ($LASTEXITCODE -ne 0) { throw 'Install Python 3.11+ and retry.' }
}
if ($args -contains '--install') {
    Write-Host 'Jev runtime ready; no third-party packages required.'
    exit 0
}
$jevKeyFile = Join-Path $PSScriptRoot '.secrets/typesafe.dpapi'
$jevLoadedKey = $false
$jevNeedsKey = -not ($args -contains '--inspect') -and -not ($args -contains '--help') -and -not ($args -contains '-h')
try {
    if ($jevNeedsKey -and -not $env:TYPESAFE_API_KEY -and (Test-Path -LiteralPath $jevKeyFile)) {
        $jevSecureKey = (Get-Content -LiteralPath $jevKeyFile -Raw -Encoding UTF8).Trim() | ConvertTo-SecureString
        $env:TYPESAFE_API_KEY = [System.Net.NetworkCredential]::new('', $jevSecureKey).Password
        $jevLoadedKey = $true
    }
    & $venvPython (Join-Path $PSScriptRoot 'combat.py') @args
    $jevExitCode = $LASTEXITCODE
} finally {
    if ($jevLoadedKey) { Remove-Item Env:TYPESAFE_API_KEY -ErrorAction SilentlyContinue }
}
exit $jevExitCode
