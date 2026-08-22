param(
    [Parameter(Mandatory = $true)]
    [string]$ReleaseTag
)

$ErrorActionPreference = 'Stop'
$tag = $ReleaseTag.Trim()
$pattern = '^v(?<major>(?:0|[1-9][0-9]*))\.(?<minor>(?:0|[1-9][0-9]*))\.(?<patch>(?:0|[1-9][0-9]*))-preview\.(?<ordinal>[1-9][0-9]*)$'
$match = [regex]::Match($tag, $pattern, [System.Text.RegularExpressions.RegexOptions]::CultureInvariant)
if (-not $match.Success) {
    throw "ReleaseTag must use the exact preview shape v<major>.<minor>.<patch>-preview.<n>. Got: $ReleaseTag"
}

$requestedOrdinal = [long]0
if (-not [long]::TryParse(
        $match.Groups['ordinal'].Value,
        [System.Globalization.NumberStyles]::None,
        [System.Globalization.CultureInfo]::InvariantCulture,
        [ref]$requestedOrdinal)) {
    throw "ReleaseTag preview ordinal is outside the supported Int64 range: $ReleaseTag"
}

$major = $match.Groups['major'].Value
$minor = $match.Groups['minor'].Value
$patch = $match.Groups['patch'].Value
$seriesPrefix = "v$major.$minor.$patch-preview."

& git fetch --force --tags origin
if ($LASTEXITCODE -ne 0) {
    throw 'Could not refresh Git tags before preview release sequence validation.'
}

$seriesTags = @(& git tag --list "$seriesPrefix*")
if ($LASTEXITCODE -ne 0) {
    throw "Could not enumerate existing preview tags for series $seriesPrefix"
}

$foundAny = $false
$maxOrdinal = [long]0
foreach ($rawSeriesTag in $seriesTags) {
    $seriesTag = ([string]$rawSeriesTag).Trim()
    if ([string]::IsNullOrWhiteSpace($seriesTag)) {
        throw "Git returned an empty matching-series tag for $seriesPrefix"
    }

    $candidate = [regex]::Match($seriesTag, $pattern, [System.Text.RegularExpressions.RegexOptions]::CultureInvariant)
    if (-not $candidate.Success -or
        -not [string]::Equals($candidate.Groups['major'].Value, $major, [StringComparison]::Ordinal) -or
        -not [string]::Equals($candidate.Groups['minor'].Value, $minor, [StringComparison]::Ordinal) -or
        -not [string]::Equals($candidate.Groups['patch'].Value, $patch, [StringComparison]::Ordinal)) {
        throw "Matching-series Git tag is not canonical and blocks safe preview ordinal derivation: $seriesTag"
    }

    $candidateOrdinal = [long]0
    if (-not [long]::TryParse(
            $candidate.Groups['ordinal'].Value,
            [System.Globalization.NumberStyles]::None,
            [System.Globalization.CultureInfo]::InvariantCulture,
            [ref]$candidateOrdinal)) {
        throw "Matching-series Git tag ordinal is outside the supported Int64 range: $seriesTag"
    }

    $foundAny = $true
    if ($candidateOrdinal -gt $maxOrdinal) {
        $maxOrdinal = $candidateOrdinal
    }
}

if ($foundAny -and $maxOrdinal -eq [long]::MaxValue) {
    throw "Preview series $seriesPrefix exhausted the supported Int64 ordinal range. Start a new release base version."
}

if (-not $foundAny) {
    if ($requestedOrdinal -ne [long]1) {
        throw "ReleaseTag must start a new preview series at ordinal 1. Expected ${seriesPrefix}1 because no prior tag exists; got $tag."
    }
}
elseif ($requestedOrdinal -le $maxOrdinal) {
    throw "ReleaseTag preview ordinal must be greater than the highest published ordinal for its exact series. Highest published is $maxOrdinal; got $requestedOrdinal in $tag."
}

Write-Host "Preview release sequence validated: $tag is newer than published history for $seriesPrefix"
