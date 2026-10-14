param(
    [string]$Directory = (Join-Path $PSScriptRoot '../artifacts'),
    [ValidateRange(1024, 65535)][int]$Port = 1765,
    [string]$Page = 'starlight-duet-design-20261014/index.html'
)

$ErrorActionPreference = 'Stop'
$previewRoot = (Resolve-Path -LiteralPath $Directory).Path
$pageFile = Join-Path $previewRoot $Page
$expectedPage = [IO.File]::ReadAllText($pageFile, [Text.Encoding]::UTF8)
$previewUrl = "http://127.0.0.1:$Port/$Page"

function Test-PreviewPage {
    try {
        $response = Invoke-WebRequest -Uri $previewUrl -UseBasicParsing -TimeoutSec 2
        return $response.StatusCode -eq 200 -and $response.Content -ceq $expectedPage
    } catch {
        return $false
    }
}

$listener = Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction SilentlyContinue
if ($listener) {
    if (Test-PreviewPage) {
        Write-Output "Preview already running: $previewUrl"
        return
    }
    throw "Port $Port is occupied but the expected preview is unavailable. Inspect its process before restarting."
}

$pythonExecutable = (Get-Command python.exe -ErrorAction Stop).Source
$stdoutLog = Join-Path $previewRoot "preview-server-$Port.stdout.log"
$stderrLog = Join-Path $previewRoot "preview-server-$Port.stderr.log"
# File-backed standard streams survive the terminal/tool session ending.
$server = Start-Process -FilePath $pythonExecutable `
    -ArgumentList @('-u', '-m', 'http.server', "$Port", '--bind', '127.0.0.1', '--directory', ('"' + $previewRoot + '"')) `
    -WorkingDirectory $previewRoot -WindowStyle Hidden `
    -RedirectStandardOutput $stdoutLog -RedirectStandardError $stderrLog -PassThru

for ($attempt = 0; $attempt -lt 30; $attempt++) {
    if (Test-PreviewPage) {
        Write-Output "Preview ready: $previewUrl (launcher PID $($server.Id))"
        return
    }
    if ($server.HasExited) { break }
    Start-Sleep -Milliseconds 100
}
throw "Preview did not become ready. Inspect $stderrLog."
