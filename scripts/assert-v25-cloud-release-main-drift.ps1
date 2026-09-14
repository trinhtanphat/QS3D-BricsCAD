param(
    [Parameter(Mandatory = $true)]
    [string]$SourceSha,

    [Parameter(Mandatory = $true)]
    [string]$CurrentMainSha
)

$ErrorActionPreference = 'Stop'

$SourceSha = $SourceSha.Trim().ToLowerInvariant()
$CurrentMainSha = $CurrentMainSha.Trim().ToLowerInvariant()
if ($SourceSha -notmatch '^[0-9a-f]{40}$') {
    throw 'V25 release drift admission requires an exact 40-hex source SHA.'
}
if ($CurrentMainSha -notmatch '^[0-9a-f]{40}$') {
    throw 'V25 release drift admission requires an exact 40-hex protected-main SHA.'
}

$releaseRelevantPathspecs = @(
    'src/'
    'tests/'
    'scripts/'
    'samples/generated/'
    'external/QS3D-Platform'
    '.gitmodules'
    'Directory.Build.props'
    'QS3D.sln'
    'QS3D.V26.sln'
    '.github/workflows/release-v25-cloud.yml'
    '.github/workflows/dispatch-v25-cloud-after-main-integration.yml'
)

$previousErrorActionPreference = $ErrorActionPreference
try {
    $ErrorActionPreference = 'Continue'
    & git diff --quiet --no-ext-diff "$SourceSha..$CurrentMainSha" -- @releaseRelevantPathspecs
    $releaseDriftStatus = $LASTEXITCODE
}
finally {
    $ErrorActionPreference = $previousErrorActionPreference
}

if ($releaseDriftStatus -eq 1) {
    throw 'Protected main advanced with release-relevant changes after V25 source admission; refusing stale release publication.'
}
if ($releaseDriftStatus -ne 0) {
    throw "Could not inspect release-relevant protected-main drift; git diff exit $releaseDriftStatus."
}

if ($SourceSha -ne $CurrentMainSha) {
    Write-Host "Protected main advanced only through non-release paths; V25 release remains pinned to admitted SOURCE_SHA=$SourceSha currentMain=$CurrentMainSha."
}
