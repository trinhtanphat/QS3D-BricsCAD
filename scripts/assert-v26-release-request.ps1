[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$ReleaseTag,

    [Parameter(Mandatory = $true)]
    [bool]$Prerelease,

    [Parameter(Mandatory = $true)]
    [bool]$RunRuntime,

    [Parameter(Mandatory = $true)]
    [bool]$SignPackage
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Convert-ToStrictSemVerTag {
    param([Parameter(Mandatory = $true)][string]$Value)

    if ([string]::IsNullOrWhiteSpace($Value)) {
        throw 'V26 release_tag is required.'
    }
    if (-not [string]::Equals($Value, $Value.Trim(), [StringComparison]::Ordinal)) {
        throw 'V26 release_tag must not contain leading or trailing whitespace.'
    }

    $match = [regex]::Match(
        $Value,
        '^v(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)(?:-([0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*))?(?:\+([0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*))?$',
        [Text.RegularExpressions.RegexOptions]::CultureInvariant)
    if (-not $match.Success) {
        throw "V26 release_tag must be canonical strict SemVer prefixed by v: $Value"
    }

    if ($match.Groups[4].Success) {
        foreach ($identifier in $match.Groups[4].Value.Split('.')) {
            if ($identifier -match '^[0-9]+$' -and $identifier.Length -gt 1 -and $identifier[0] -eq '0') {
                throw "V26 release_tag has a numeric prerelease identifier with a leading zero: $Value"
            }
        }
    }

    return [pscustomobject]@{
        ReleaseTag = $Value
        VersionText = $Value.Substring(1)
        IsPrerelease = $match.Groups[4].Success
        PrereleaseText = if ($match.Groups[4].Success) { $match.Groups[4].Value } else { '' }
        BuildMetadata = if ($match.Groups[5].Success) { $match.Groups[5].Value } else { '' }
    }
}

$parsed = Convert-ToStrictSemVerTag -Value $ReleaseTag
if ($Prerelease -ne $parsed.IsPrerelease) {
    throw "prerelease input must match the validated V26 release_tag prerelease state."
}
if (-not $parsed.IsPrerelease -and -not $RunRuntime) {
    throw 'Stable V26 release requires run_runtime=true.'
}
if (-not $parsed.IsPrerelease -and -not $SignPackage) {
    throw 'Stable V26 release requires sign_package=true.'
}

[pscustomobject]@{
    ReleaseTag = $parsed.ReleaseTag
    VersionText = $parsed.VersionText
    IsPrerelease = $parsed.IsPrerelease
    PrereleaseText = $parsed.PrereleaseText
    BuildMetadata = $parsed.BuildMetadata
    RunRuntime = $RunRuntime
    SignPackage = $SignPackage
}
