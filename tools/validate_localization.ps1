#requires -Version 7.0
<#!
Validate the Chinese source and its English/Japanese translations.
Optionally use the installed game's SmartFormat.dll for parsing and branch smoke checks.
This does not launch the game or verify fonts/layout; custom numeric/icon formatters are
replaced only in smoke-check copies, never in localization files.
!#>
[CmdletBinding()]
param(
    [string]$RepositoryRoot = (Split-Path $PSScriptRoot -Parent),
    [string]$SmartFormatPath
)
$ErrorActionPreference = 'Stop'
$locRoot = Join-Path $RepositoryRoot 'ShoujoKagekiAijoKaren/localization'
$failures = [Collections.Generic.List[string]]::new()
$utf8 = [Text.UTF8Encoding]::new($false, $true)
$variablePattern = '\{(?<name>[A-Za-z_]\w*)(?::(?<formatter>[A-Za-z_]\w*)(?<options>\([^{}]*?\))?)?(?=[:}])'

function Read-LocTable([string]$Path) {
    $text = [IO.File]::ReadAllText($Path, $utf8)
    $document = [System.Text.Json.JsonDocument]::Parse($text)
    try {
        if ($document.RootElement.ValueKind -ne 'Object') { throw "$Path must be a JSON object" }
        $table = [Collections.Generic.Dictionary[string,string]]::new([StringComparer]::Ordinal)
        foreach ($property in $document.RootElement.EnumerateObject()) {
            if ($property.Value.ValueKind -ne 'String') { throw "$Path / $($property.Name) must be a string" }
            if (-not $table.TryAdd($property.Name, $property.Value.GetString())) {
                throw "$Path contains duplicate key $($property.Name)"
            }
        }
        return ,$table
    } finally { $document.Dispose() }
}

function Get-TemplateSignature([string]$Value) {
    # Compare every placeholder's selector, formatter, options and own branch count.
    # Word order can change; nested placeholders and branches must remain intact.
    $stack = [Collections.Generic.Stack[object]]::new()
    $parts = [Collections.Generic.List[string]]::new()
    for ($index = 0; $index -lt $Value.Length; $index++) {
        switch ($Value[$index]) {
            '{' { $stack.Push(@{ Start = $index; Bars = 0 }) }
            '|' { if ($stack.Count) { $stack.Peek().Bars++ } }
            '}' {
                if (-not $stack.Count) { throw 'Unmatched closing brace' }
                $node = $stack.Pop()
                $raw = $Value.Substring($node.Start, $index - $node.Start + 1)
                $match = [regex]::Match($raw, '^' + $variablePattern)
                if (-not $match.Success) { throw "Invalid placeholder: $raw" }
                if ($match.Groups['formatter'].Value -ne 'plural') {
                    $parts.Add($match.Value + ';branches=' + $node.Bars)
                }
            }
        }
    }
    if ($stack.Count) { throw 'Unclosed placeholder' }
    return (($parts | Sort-Object -CaseSensitive) -join "`n")
}

function Assert-Tags([string]$Value) {
    $stack = [Collections.Generic.Stack[string]]::new()
    foreach ($match in [regex]::Matches($Value, '\[(?<close>/)?(?<tag>[A-Za-z]+)\]')) {
        $tag = $match.Groups['tag'].Value
        if ($match.Groups['close'].Success) {
            if (-not $stack.Count -or $stack.Pop() -cne $tag) { throw "Mismatched BBCode tag $($match.Value)" }
        } else { $stack.Push($tag) }
    }
    if ($stack.Count) { throw 'Unclosed BBCode tag' }
}

$parser = $null
if ($SmartFormatPath) {
    Add-Type -Path (Resolve-Path $SmartFormatPath).Path
    $parser = [SmartFormat.SmartFormatter]::new().Parser
}
$hashes = Read-LocTable (Join-Path $RepositoryRoot 'docs/localization/source-hashes.json')
$seenHashes = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
$sourceFiles = @(Get-ChildItem (Join-Path $locRoot 'zhs') -Filter '*.json' | Sort-Object Name)
foreach ($language in 'eng', 'jpn') {
    $names = @(Get-ChildItem (Join-Path $locRoot $language) -Filter '*.json' | ForEach-Object Name | Sort-Object)
    if (($names -join '|') -cne (($sourceFiles.Name | Sort-Object) -join '|')) {
        $failures.Add("$language has missing or extra localization files")
    }
}
$entryCount = 0
$renderCount = 0
foreach ($file in $sourceFiles) {
    $source = Read-LocTable $file.FullName
    $entryCount += $source.Count
    foreach ($key in $source.Keys) {
        $hashKey = "$($file.Name)/$key"
        [void]$seenHashes.Add($hashKey)
        $digest = [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($utf8.GetBytes($source[$key]))).ToLowerInvariant()
        if (-not $hashes.ContainsKey($hashKey) -or $hashes[$hashKey] -cne $digest) {
            $failures.Add("Chinese source changed; review both translations and refresh hash: $hashKey")
        }
    }
    foreach ($language in 'eng', 'jpn') {
        $targetPath = Join-Path $locRoot "$language/$($file.Name)"
        if (-not (Test-Path $targetPath)) { continue }
        $target = Read-LocTable $targetPath
        foreach ($key in $target.Keys) {
            if (-not $source.ContainsKey($key)) { $failures.Add("Extra key: $language/$($file.Name)/$key") }
        }
        foreach ($key in $source.Keys) {
            $label = "$language/$($file.Name)/$key"
            if (-not $target.ContainsKey($key)) { $failures.Add("Missing key: $label"); continue }
            $value = $target[$key]
            try {
                if ([string]::IsNullOrWhiteSpace($source[$key]) -ne [string]::IsNullOrWhiteSpace($value)) {
                    throw 'Empty/nonempty value differs from Chinese source'
                }
                if ((Get-TemplateSignature $source[$key]) -cne (Get-TemplateSignature $value)) {
                    throw 'Placeholder selectors, formatters or branch counts differ from Chinese source'
                }
                $sourceVars = @([regex]::Matches($source[$key], $variablePattern) | ForEach-Object { $_.Groups['name'].Value })
                foreach ($match in [regex]::Matches($value, $variablePattern)) {
                    if ($sourceVars -cnotcontains $match.Groups['name'].Value) { throw "Unknown variable: $($match.Value)" }
                }
                Assert-Tags $value
                $sourceTags = @([regex]::Matches($source[$key], '\[([A-Za-z]+)\]') | ForEach-Object Value | Sort-Object -Unique)
                $targetTags = @([regex]::Matches($value, '\[([A-Za-z]+)\]') | ForEach-Object Value | Sort-Object -Unique)
                if (($sourceTags -join '|') -cne ($targetTags -join '|')) { throw 'BBCode tag types differ from Chinese source' }
                if ($value.Contains('\n')) { throw 'Literal backslash-n instead of a newline' }
                if ($value.Contains([char]0xFFFD)) { throw 'Unicode replacement character found' }
                if ($language -eq 'eng' -and $value -match '[\u3400-\u9fff]|\(s\)') { throw 'Chinese residue or unresolved (s) suffix' }
                if ($parser) {
                    [void]$parser.ParseFormat($source[$key])
                    [void]$parser.ParseFormat($value)
                    # Exercise nested branches and plurals with the game's real formatter.
                    # Rendering of custom diff/icon formatters and layout requires the game.
                    $probe = $value -replace ':diff\(\)', '' -replace ':energyIcons\([^)]*\)', '' -replace ':show:', ':choose(True):'
                    foreach ($number in 0, 1, 2) {
                        foreach ($upgraded in $false, $true) {
                            foreach ($combat in $false, $true) {
                                $values = [Collections.Generic.Dictionary[string,object]]::new()
                                foreach ($name in $sourceVars) { $values[$name] = $number }
                                $values['IfUpgraded'] = $upgraded
                                $values['InCombat'] = $combat
                                $culture = [Globalization.CultureInfo]::GetCultureInfo($(if ($language -eq 'eng') { 'en' } else { 'ja' }))
                                $rendered = [SmartFormat.Smart]::Format($culture, $probe, [object[]]@($values))
                                if ($rendered -match '[{}]') { throw "Unresolved placeholder: $rendered" }
                                Assert-Tags $rendered
                                $renderCount++
                            }
                        }
                    }
                }
            } catch { $failures.Add("${label}: $($_.Exception.Message)") }
        }
    }
}
foreach ($key in $hashes.Keys) {
    if (-not $seenHashes.Contains($key)) { $failures.Add("Stale source hash: $key") }
}
if ($failures.Count) {
    $failures | ForEach-Object { Write-Host "FAIL: $_" }
    throw "$($failures.Count) localization validation failure(s)."
}
Write-Host "PASS: $($sourceFiles.Count) tables, $entryCount Chinese source entries, $($entryCount * 2) translations."
Write-Host 'PASS: UTF-8/JSON, unique keys, source hashes, placeholders/branches, BBCode and English residue checks.'
if ($parser) { Write-Host "PASS: original SmartFormat parsing and $renderCount branch/plural smoke renders (numeric/icon stubs)." }
else { Write-Host 'SmartFormat parsing/render checks skipped; supply -SmartFormatPath to enable them.' }
