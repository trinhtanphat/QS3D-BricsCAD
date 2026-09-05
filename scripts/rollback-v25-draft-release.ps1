[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$Repository,
    [Parameter(Mandatory = $true)][long]$ReleaseId,
    [Parameter(Mandatory = $true)][string]$ReleaseTag,
    [Parameter(Mandatory = $true)][string]$WorkflowSha,
    [Parameter(Mandatory = $true)][bool]$TagCreatedByThisRun,
    [Parameter(Mandatory = $true)][string]$Token
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if ($Repository -notmatch '^[A-Za-z0-9_.-]+/[A-Za-z0-9_.-]+$') { throw "Repository must be owner/name: $Repository" }
if ($ReleaseId -lt 0) { throw 'ReleaseId must be zero or positive.' }
if ($ReleaseTag -notmatch '^v[0-9A-Za-z.+-]+$') { throw "Unexpected V25 release tag: $ReleaseTag" }
if ($WorkflowSha -notmatch '^[0-9a-fA-F]{40}$') { throw 'WorkflowSha must be a full 40-hex commit SHA.' }
if ([string]::IsNullOrWhiteSpace($Token)) { throw 'GitHub token is required for bounded draft rollback.' }

$headers = @{
    Authorization = "Bearer $Token"
    Accept = 'application/vnd.github+json'
    'X-GitHub-Api-Version' = '2022-11-28'
    'User-Agent' = 'QS3D-V25-Draft-Rollback'
}

function Test-GitHubNotFound {
    param([Parameter(Mandatory = $true)]$ErrorRecord)
    $response = $ErrorRecord.Exception.Response
    if ($null -eq $response) { return $false }
    try { return ([int]$response.StatusCode -eq 404) }
    catch { return $false }
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
    if ($exact.Count -ne 1 -or $peeled.Count -gt 1) { throw "Remote tag $ReleaseTag is absent or ambiguous; refusing destructive rollback." }
    return $(if ($peeled.Count -eq 1) { $peeled[0] } else { $exact[0] })
}

function Assert-NoReleaseOwnsTag {
    $maxPages = 100
    for ($page = 1; $page -le $maxPages; $page++) {
        $listUri = "https://api.github.com/repos/$Repository/releases?per_page=100&page=$page"
        $releases = @(Invoke-RestMethod -Method Get -Uri $listUri -Headers $headers)
        foreach ($candidate in $releases) {
            if ([string]::Equals([string]$candidate.tag_name, $ReleaseTag, [StringComparison]::Ordinal)) {
                throw "A release still owns tag $ReleaseTag; refusing rollback completion."
            }
        }
        if ($releases.Count -lt 100) { return }
    }
    throw "Release enumeration exceeded $maxPages pages while checking tag $ReleaseTag; refusing rollback completion."
}

function Assert-DraftDeleteCommittedAfterError {
    param([Parameter(Mandatory = $true)]$DeleteError, [Parameter(Mandatory = $true)][string]$ReleaseUri)
    $remainingRelease = $null
    try { $remainingRelease = Invoke-RestMethod -Method Get -Uri $ReleaseUri -Headers $headers }
    catch {
        if (Test-GitHubNotFound -ErrorRecord $_) {
            Write-Host "V25 draft DELETE acknowledgement was ambiguous, but the exact release is authoritatively absent; treating draft deletion as committed."
            return
        }
        throw "Unable to reconcile V25 draft DELETE acknowledgement. Delete error: $($DeleteError.Exception.Message) Reconciliation error: $($_.Exception.Message)"
    }
    if ($null -eq $remainingRelease -or [long]$remainingRelease.id -ne $ReleaseId) { throw "V25 draft DELETE reconciliation returned a mismatched release identity; refusing to assume deletion." }
    if (-not [string]::Equals([string]$remainingRelease.url, $ReleaseUri, [StringComparison]::Ordinal)) { throw "V25 draft DELETE reconciliation returned a mismatched repository identity; refusing to assume deletion." }
    if ($remainingRelease.draft -ne $true) { throw "V25 draft DELETE reconciliation found release $ReleaseId but it is no longer an owned draft; refusing to assume deletion." }
    if (-not [string]::Equals([string]$remainingRelease.tag_name, $ReleaseTag, [StringComparison]::Ordinal)) { throw "V25 draft DELETE reconciliation found a mismatched release tag; refusing to assume deletion." }
    throw "Exact owned V25 draft $ReleaseId still exists after DELETE error; refusing to assume deletion. Original error: $($DeleteError.Exception.Message)"
}

$resolvedBefore = Resolve-ExactRemoteTagSha
if (-not [string]::Equals($resolvedBefore, $WorkflowSha, [StringComparison]::OrdinalIgnoreCase)) { throw "Remote tag $ReleaseTag moved to $resolvedBefore; refusing destructive rollback." }

if ($ReleaseId -gt 0) {
    $releaseUri = "https://api.github.com/repos/$Repository/releases/$ReleaseId"
    $release = Invoke-RestMethod -Method Get -Uri $releaseUri -Headers $headers
    if ([long]$release.id -ne $ReleaseId) { throw "Release identity mismatch for $ReleaseId; refusing destructive rollback." }
    if (-not [string]::Equals([string]$release.url, $releaseUri, [StringComparison]::Ordinal)) { throw "Release repository identity mismatch for $ReleaseId; refusing destructive rollback." }
    if ($release.draft -ne $true) { throw "Release $ReleaseId is not a draft; refusing destructive rollback." }
    if (-not [string]::Equals([string]$release.tag_name, $ReleaseTag, [StringComparison]::Ordinal)) { throw "Release $ReleaseId tag mismatch; refusing destructive rollback." }
    try { Invoke-RestMethod -Method Delete -Uri $releaseUri -Headers $headers | Out-Null }
    catch { Assert-DraftDeleteCommittedAfterError -DeleteError $_ -ReleaseUri $releaseUri }
}

# An ambiguous draft POST can commit without returning an id. Exhaustive release-owner
# enumeration therefore remains mandatory before claiming rollback completion.
Assert-NoReleaseOwnsTag

# Preserve the exact tag for retry regardless of whether this run originally created it.
# Deleting a reusable exact tag after release-owner enumeration leaves a TOCTOU window in
# which another actor can attach/create a release against that tag before destructive DELETE.
$resolvedPreserved = Resolve-ExactRemoteTagSha
if (-not [string]::Equals($resolvedPreserved, $WorkflowSha, [StringComparison]::OrdinalIgnoreCase)) { throw "V25 release tag $ReleaseTag changed during draft rollback; refusing to claim restart safety." }
Write-Host "Preserving exact V25 tag $ReleaseTag at $($WorkflowSha.ToLowerInvariant()) for safe retry."

[pscustomobject]@{
    ReleaseId = $ReleaseId
    ReleaseTag = $ReleaseTag
    WorkflowSha = $WorkflowSha.ToLowerInvariant()
    TagCreatedByThisRun = $TagCreatedByThisRun
    DraftDeleted = ($ReleaseId -gt 0)
    TagDeleted = $false
}
