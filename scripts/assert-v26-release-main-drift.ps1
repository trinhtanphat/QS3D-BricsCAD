[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidatePattern('^[0-9a-fA-F]{40}$')]
    [string]$SourceSha,

    [Parameter(Mandatory = $true)]
    [ValidatePattern('^[0-9a-fA-F]{40}$')]
    [string]$CurrentMainSha
)

$ErrorActionPreference = 'Stop'
$source = $SourceSha.Trim().ToLowerInvariant()
$currentMain = $CurrentMainSha.Trim().ToLowerInvariant()

$releaseRelevantPathspecs = @(
    'src/',
    'tests/',
    'scripts/',
    'samples/generated/',
    'external/QS3D-Platform',
    '.gitmodules',
    'Directory.Build.props',
    'QS3D.sln',
    'QS3D.V26.sln',
    '.github/workflows/release-v26.yml'
)

& git diff --quiet --no-ext-diff "$source..$currentMain" -- @releaseRelevantPathspecs
$releaseDriftStatus = $LASTEXITCODE
if ($releaseDriftStatus -eq 1) {
    throw "Protected main contains V26 release-relevant drift after the pinned release source. Refusing stale release source=$source currentMain=$currentMain."
}
if ($releaseDriftStatus -ne 0) {
    throw "Could not classify V26 release-relevant protected-main drift. git diff exit=$releaseDriftStatus source=$source currentMain=$currentMain."
}

[PSCustomObject]@{
    SourceSha = $source
    CurrentMainSha = $currentMain
    ReleaseRelevantDrift = $false
}
