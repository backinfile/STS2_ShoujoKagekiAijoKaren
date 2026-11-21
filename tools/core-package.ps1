function Get-CorePackage([string]$Path) {
    if ([string]::IsNullOrWhiteSpace($Path)) {
        throw 'Set -CorePackage or SHOUJO_CORE_PACKAGE to an official Core distribution directory.'
    }
    $root = (Resolve-Path -LiteralPath $Path).Path
    if (-not (Test-Path -LiteralPath (Join-Path $root 'ShoujoKagekiCore.json') -PathType Leaf)) {
        throw 'Select a ShoujoKagekiCore 2.0.0 or newer distribution directory.'
    }
    $checked = & (Join-Path $root 'sdk/validate-package.ps1') -CorePackage $root
    return [pscustomobject]@{
        Root = $checked.Root
        Bundle = $checked.Bundle
        ApiPath = Join-Path $checked.Root $checked.Bundle.apiAssembly
    }
}

function Copy-CoreRolePackage($Core, [string]$RoleDirectory, [string]$PackageRoot, [string]$RoleId) {
    $packageRootPath = [IO.Path]::GetFullPath($PackageRoot)
    $destination = [IO.Path]::GetFullPath((Join-Path $packageRootPath 'ShoujoKagekiCore'))
    if (-not $destination.StartsWith($packageRootPath + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase) -or
        $Core.Root.Equals($destination, [StringComparison]::OrdinalIgnoreCase) -or
        $Core.Root.StartsWith($destination + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
        throw 'The source Core package must be separate from the build destination.'
    }
    if (Test-Path -LiteralPath $destination) { Remove-Item -LiteralPath $destination -Recurse -Force }
    & (Join-Path $Core.Root 'sdk/pack-character.ps1') -CorePackage $Core.Root -CharacterDirectory $RoleDirectory -OutputDirectory $packageRootPath -ModId $RoleId -RequiredFeatures @('shine-rewards@1', 'player-choices@1', 'card-picking@1', 'shine@1', 'card-options@1', 'card-snapshots@1', 'effect-registration@1', 'movement-pipeline@1', 'card-copies@1', 'presentation@1', 'temporary-music@1')
}

function Preserve-LegacyCoreBuild([string]$Directory, [string]$ArtifactRoot) {
    $root = [IO.Path]::GetFullPath($ArtifactRoot)
    $old = [IO.Path]::GetFullPath((Join-Path $Directory 'ShoujoKagekiAijoCore'))
    if (-not (Test-Path -LiteralPath $old)) { return }
    $legacy = Get-Item -LiteralPath $old
    if (-not $legacy.FullName.StartsWith($root + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase) -or
        -not $legacy.PSIsContainer -or $legacy.Attributes.HasFlag([IO.FileAttributes]::ReparsePoint)) {
        throw 'Legacy Core build is outside the artifacts directory or is not a regular directory.'
    }
    $backup = Join-Path $root ('Core-1.x-' + [Guid]::NewGuid().ToString('N'))
    [IO.Directory]::Move($legacy.FullName, $backup)
}
