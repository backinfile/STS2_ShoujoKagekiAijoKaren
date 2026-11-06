param([string]$ClientGamesRoot = '', [string]$RunRoot = '')
$ErrorActionPreference='Stop'
$project=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
if(-not $ClientGamesRoot){$ClientGamesRoot=Join-Path $project 'artifacts/test-matrix/mr-white-client-cache'}
if(-not $RunRoot){$RunRoot=Join-Path $project ('artifacts/test-matrix/mr-white-two-turn-'+[DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss'))}
$runRoot=[IO.Path]::GetFullPath($RunRoot)
$allowedRoot=Join-Path $project 'artifacts/test-matrix'
if(-not $runRoot.StartsWith($allowedRoot+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)){throw 'RunRoot must be inside task test-matrix artifacts'}
New-Item -ItemType Directory -Path $runRoot -Force|Out-Null
$results=[Collections.Generic.List[object]]::new()
$cases=@(
    @{branch='stable';host='KAREN';client='IRONCLAD';name='stable-host';id=9251},
    @{branch='stable';host='IRONCLAD';client='KAREN';name='stable-client';id=9252},
    @{branch='stable';host='KAREN';client='KAREN';name='stable-both';id=9253},
    @{branch='beta';host='KAREN';client='IRONCLAD';name='beta-host';id=9254},
    @{branch='beta';host='IRONCLAD';client='KAREN';name='beta-client';id=9255},
    @{branch='beta';host='KAREN';client='KAREN';name='beta-both';id=9256}
)
foreach($case in $cases){
    $caseRoot=Join-Path $runRoot $case.name
    Write-Host "RUN $($case.name)"
    $output=@(& pwsh -NoProfile -File (Join-Path $project 'tools/dev_matrix_case.ps1') -Branch $case.branch -HostCharacter $case.host -ClientCharacter $case.client -CaseId $case.id -RunRoot $caseRoot -ClientGamesRoot $ClientGamesRoot -MrWhiteRegression 2>&1)
    $exitCode=$LASTEXITCODE
    $output|ForEach-Object{Write-Host $_}
    $output|Out-String|Set-Content -LiteralPath (Join-Path $runRoot "$($case.name)-console.txt") -Encoding utf8NoBOM
    $results.Add([PSCustomObject]@{case=$case.name;result=if($exitCode -eq 0){'pass'}else{'fail'};exitCode=$exitCode;root=[IO.Path]::GetRelativePath($project,$caseRoot)})
    $results|ConvertTo-Json -Depth 12|Set-Content -LiteralPath (Join-Path $runRoot 'report.json') -Encoding utf8NoBOM
}
$failed=@($results|Where-Object result -ne 'pass').Count
Write-Host "Completed two-turn Mr. White cases: $($results.Count-$failed) passed, $failed failed."
if($failed){exit 1}
