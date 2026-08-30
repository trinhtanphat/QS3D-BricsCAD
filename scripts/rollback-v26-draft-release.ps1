[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$Repository,
    [Parameter(Mandatory = $true)][long]$ReleaseId,
    [Parameter(Mandatory = $true)][string]$ReleaseTag,
    [Parameter(Mandatory = $true)][string]$WorkflowSha,
    [Parameter(Mandatory = $true)][bool]$TagWasAbsentBeforeCreate,
    [Parameter(Mandatory = $true)][string]$Token
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if ($Repository -notmatch '^[A-Za-z0-9_.-]+/[A-Za-z0-9_.-]+$') {
    throw "Repository must be owner/name: $Repository"
}
if ($ReleaseId -le 0) { throw 'ReleaseId must be positive.' }
if ($ReleaseTag -notmatch '^v[0-9A-Za-z.+-]+$') { throw "Unexpected V26 release tag: $ReleaseTag" }
if ($WorkflowSha -notmatch '^[0-9a-fA-F]{40}$') { throw 'WorkflowSha must be a full 40-hex commit SHA.' }
if (-not $TagWasAbsentBeforeCreate) {
    throw 'Rollback requires proof that the release tag was absent immediately before this transaction created the draft.'
}
if ([string]::IsNullOrWhiteSpace($Token)) { throw 'GitHub token is required for bounded draft rollback.' }

$headers = @{
    Authorization = "Bearer $Token"
    Accept = 'application/vnd.github+json'
    'X-GitHub-Api-Version' = '2022-11-28'
    'User-Agent' = 'QS3D-V26-Draft-Rollback'
}
$releaseUri = "https://api.github.com/repos/$Repository/releases/$ReleaseId"
$release = Invoke-RestMethod -Method Get -Uri $releaseUri -Headers $headers
if ([long]$release.id -ne $ReleaseId) {
    throw "Release identity mismatch for $ReleaseId; refusing destructive rollback."
}
if (-not [string]::Equals([string]$release.url, $releaseUri, [StringComparison]::Ordinal)) {
    throw "Release repository identity mismatch for $ReleaseId; refusing destructive rollback."
}
if ($release.draft -ne $true) {
    throw "Release $ReleaseId is not a draft; refusing destructive rollback."
}
if (-not [string]::Equals([string]$release.tag_name, $ReleaseTag, [StringComparison]::Ordinal)) {
    throw "Release $ReleaseId tag mismatch; refusing destructive rollback."
}

function Resolve-ExactRemoteTagSha {
    $tagRef = "refs/tags/$ReleaseTag"
    $peeledRef = $tagRef + '^{}'
    $lines = @(git ls-remote --tags origin $tagRef $peeledRef)
    if ($LASTEXITCODE -ne 0) { throw "Failed to resolve remote tag $ReleaseTag during rollback." }
    $exact = New-Object System.Collections.Generic.List[string]
    $peeled = New-Object System.Collections.Generic.List[string]
    foreach ($line in $lines) {
        if ($line -notmatch '^([0-9a-fA-F]{40})\s+(.+)$') { throw "Malformed ls-remote output for $ReleaseTag." }
        $sha = $Matches[1]
        $refName = $Matches[2]
        if ([string]::Equals($refName, $tagRef, [StringComparison]::Ordinal)) { $exact.Add($sha) }
        elseif ([string]::Equals($refName, $peeledRef, [StringComparison]::Ordinal)) { $peeled.Add($sha) }
        else { throw "Unexpected remote ref during rollback: $refName" }
    }
    if ($exact.Count -ne 1 -or $peeled.Count -gt 1) {
        throw "Remote tag $ReleaseTag is absent or ambiguous; refusing destructive rollback."
    }
    return $(if ($peeled.Count -eq 1) { $peeled[0] } else { $exact[0] })
}

$resolvedBefore = Resolve-ExactRemoteTagSha
if (-not [string]::Equals($resolvedBefore, $WorkflowSha, [StringComparison]::OrdinalIgnoreCase)) {
    throw "Remote tag $ReleaseTag moved to $resolvedBefore; refusing destructive rollback."
}

Invoke-RestMethod -Method Delete -Uri $releaseUri -Headers $headers | Out-Null

$tagReleaseUri = "https://api.github.com/repos/$Repository/releases/tags/$([Uri]::EscapeDataString($ReleaseTag))"
try {
    $remainingRelease = Invoke-RestMethod -Method Get -Uri $tagReleaseUri -Headers $headers
    if ($null -ne $remainingRelease) {
        throw "A release still owns tag $ReleaseTag after draft deletion; refusing tag deletion."
    }
}
catch {
    $status = $null
    if ($_.Exception.Response -and $_.Exception.Response.StatusCode) { $status = [int]$_.Exception.Response.StatusCode }
    if ($status -ne 404) { throw }
}

$resolvedAfter = Resolve-ExactRemoteTagSha
if (-not [string]::Equals($resolvedAfter, $WorkflowSha, [StringComparison]::OrdinalIgnoreCase)) {
    throw "Remote tag $ReleaseTag changed during rollback; refusing tag deletion."
}

$tagRefUri = "https://api.github.com/repos/$Repository/git/refs/tags/$([Uri]::EscapeDataString($ReleaseTag))"
Invoke-RestMethod -Method Delete -Uri $tagRefUri -Headers $headers | Out-Null

[pscustomobject]@{
    ReleaseId = $ReleaseId
    ReleaseTag = $ReleaseTag
    WorkflowSha = $WorkflowSha.ToLowerInvariant()
    TagWasAbsentBeforeCreate = $true
    DraftDeleted = $true
    TagDeleted = $true
}