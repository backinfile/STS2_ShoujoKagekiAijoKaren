# Windows DPAPI: decryptable only by the current Windows account on this machine.
$ErrorActionPreference = 'Stop'
$secretDir = Join-Path $PSScriptRoot '.secrets'
New-Item -ItemType Directory -Force -Path $secretDir | Out-Null
$jevSecureKey = Read-Host 'TypeSafe API key' -AsSecureString
$jevSecureKey | ConvertFrom-SecureString | Set-Content -LiteralPath (Join-Path $secretDir 'typesafe.dpapi') -Encoding UTF8
Write-Host 'Encrypted TypeSafe key saved locally.'
