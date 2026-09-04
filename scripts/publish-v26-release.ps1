[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

foreach ($name in @('GH_TOKEN','GITHUB_REPOSITORY','GITHUB_SHA','GITHUB_RUN_ID','GITHUB_RUN_ATTEMPT','RELEASE_TAG','RELEASE_RUN_RUNTIME','V26_RELEASE_REQUEST_PRERELEASE','V26_RELEASE_REQUEST_SIGN_PACKAGE')) {
    $value = [Environment]::GetEnvironmentVariable($name)
    if ([string]::IsNullOrWhiteSpace($value)) { throw "Required V26 publish environment variable is missing: $name" }
}
if ($env:GITHUB_SHA -notmatch '^[0-9a-fA-F]{40}$') { throw 'GITHUB_SHA must be one exact 40-hex commit identity.' }
if ($env:V26_RELEASE_REQUEST_PRERELEASE -notin @('true','false')) { throw 'V26_RELEASE_REQUEST_PRERELEASE must be true or false.' }
if ($env:V26_RELEASE_REQUEST_SIGN_PACKAGE -notin @('true','false')) { throw 'V26_RELEASE_REQUEST_SIGN_PACKAGE must be true or false.' }
if ($env:RELEASE_RUN_RUNTIME -notin @('true','false')) { throw 'RELEASE_RUN_RUNTIME must be true or false.' }

$headers = @{
  Authorization = "Bearer $env:GH_TOKEN"
  Accept = "application/vnd.github+json"
  "X-GitHub-Api-Version" = "2022-11-28"
  "User-Agent" = "QS3D-V26-Manual-Release"
}
$tagRef = "refs/tags/$env:RELEASE_TAG"

function Test-GitHubNotFound {
  param([Parameter(Mandatory = $true)]$ErrorRecord)
  $response = $ErrorRecord.Exception.Response
  if ($null -eq $response) { return $false }
  try { return ([int]$response.StatusCode -eq 404) }
  catch { return $false }
}

function Get-ExactReusableReleaseTag {
  $escapedTag = [Uri]::EscapeDataString($env:RELEASE_TAG)
  $uri = "https://api.github.com/repos/$env:GITHUB_REPOSITORY/git/ref/tags/$escapedTag"
  try { $snapshot = Invoke-RestMethod -Method Get -Uri $uri -Headers $headers }
  catch {
    if (Test-GitHubNotFound -ErrorRecord $_) { return $null }
    throw
  }
  if ($null -eq $snapshot -or -not [string]::Equals([string]$snapshot.ref, $tagRef, [StringComparison]::Ordinal)) {
    throw "V26 reusable release tag lookup returned a mismatched ref."
  }
  if ($null -eq $snapshot.object -or -not [string]::Equals([string]$snapshot.object.type, 'commit', [StringComparison]::Ordinal) -or -not [string]::Equals([string]$snapshot.object.sha, $env:GITHUB_SHA, [StringComparison]::OrdinalIgnoreCase)) {
    throw "Existing V26 release tag is annotated, moved, or not bound to the exact qualified workflow SHA."
  }
  return $snapshot
}

function Assert-RemoteReleaseTagTargetsWorkflowSha {
  $peeledRef = $tagRef + '^{}'
  $lines = @(git ls-remote --tags origin $tagRef $peeledRef)
  if ($LASTEXITCODE -ne 0) {
    throw "Failed to resolve remote V26 release tag $env:RELEASE_TAG from origin. Release remains a draft."
  }
  $exact = New-Object System.Collections.Generic.List[string]
  $peeled = New-Object System.Collections.Generic.List[string]
  foreach ($line in $lines) {
    if ($line -notmatch '^([0-9a-fA-F]{40})\s+(.+)$') { throw "Malformed git ls-remote output while resolving V26 release tag $env:RELEASE_TAG. Release remains a draft." }
    $sha = $Matches[1]
    $refName = $Matches[2]
    if ([string]::Equals($refName, $tagRef, [StringComparison]::Ordinal)) { $exact.Add($sha) }
    elseif ([string]::Equals($refName, $peeledRef, [StringComparison]::Ordinal)) { $peeled.Add($sha) }
    else { throw "Unexpected remote ref while resolving V26 release tag: $refName. Release remains a draft." }
  }
  if ($exact.Count -ne 1 -or $peeled.Count -ne 0) { throw "V26 release tag $env:RELEASE_TAG must resolve to exactly one lightweight remote tag identity. Release remains a draft." }
  if (-not [string]::Equals($exact[0], $env:GITHUB_SHA, [StringComparison]::OrdinalIgnoreCase)) { throw "V26 release tag $env:RELEASE_TAG targets $($exact[0]) instead of qualified workflow SHA $env:GITHUB_SHA. Release remains a draft." }
}

function Resolve-AmbiguousDraftCreate {
  param(
    [Parameter(Mandatory = $true)][string]$TransactionMarker,
    [Parameter(Mandatory = $true)][string]$ExpectedReleaseName,
    [Parameter(Mandatory = $true)][bool]$IsPrerelease
  )
  $maxPages = 20
  $matches = New-Object System.Collections.Generic.List[object]
  $repositoryReleasePrefix = "https://api.github.com/repos/$env:GITHUB_REPOSITORY/releases/"
  for ($page = 1; $page -le $maxPages; $page++) {
    $pageUri = "https://api.github.com/repos/$env:GITHUB_REPOSITORY/releases?per_page=100&page=$page"
    $releasePage = @(Invoke-RestMethod -Method Get -Uri $pageUri -Headers $headers)
    foreach ($ReleaseSnapshot in $releasePage) {
      $body = [string]$ReleaseSnapshot.body
      if ($body.IndexOf($TransactionMarker, [StringComparison]::Ordinal) -lt 0) { continue }
      if ($ReleaseSnapshot.draft -ne $true -or
          -not [string]::Equals([string]$ReleaseSnapshot.tag_name, $env:RELEASE_TAG, [StringComparison]::Ordinal) -or
          -not [string]::Equals([string]$ReleaseSnapshot.target_commitish, $env:GITHUB_SHA, [StringComparison]::OrdinalIgnoreCase) -or
          -not [string]::Equals([string]$ReleaseSnapshot.name, $ExpectedReleaseName, [StringComparison]::Ordinal) -or
          [bool]$ReleaseSnapshot.prerelease -ne $IsPrerelease -or
          [long]$ReleaseSnapshot.id -le 0 -or
          -not [string]::Equals([string]$ReleaseSnapshot.url, ($repositoryReleasePrefix + [long]$ReleaseSnapshot.id), [StringComparison]::Ordinal)) {
        throw "A matching V26 draft-create transaction marker exists on a moved, published, or identity-mismatched release; refusing acknowledgement recovery."
      }
      $matches.Add($ReleaseSnapshot)
    }
    if ($releasePage.Count -lt 100) { break }
    if ($page -eq $maxPages) { throw "V26 draft-create acknowledgement release enumeration exceeded $maxPages pages." }
  }
  if ($matches.Count -eq 0) { throw "No matching V26 draft-create transaction marker was found after ambiguous creation." }
  if ($matches.Count -ne 1) { throw "Expected exactly one matching V26 draft-create transaction marker; found $($matches.Count)." }
  return $matches[0]
}

function Assert-PublishedReleaseMatchesVerifiedTransaction {
  param(
    [Parameter(Mandatory = $true)]$ReleaseSnapshot,
    [Parameter(Mandatory = $true)][string]$ReleaseUri,
    [Parameter(Mandatory = $true)][long]$ReleaseId,
    [Parameter(Mandatory = $true)][string[]]$ExpectedAssets,
    [Parameter(Mandatory = $true)][hashtable]$VerifiedAssetIds,
    [Parameter(Mandatory = $true)][hashtable]$AdmittedAssets,
    [Parameter(Mandatory = $true)][bool]$IsPrerelease
  )
  if ($null -eq $ReleaseSnapshot -or [long]$ReleaseSnapshot.id -ne $ReleaseId) { throw "Published V26 release identity mismatch during acknowledgement reconciliation." }
  if (-not [string]::Equals([string]$ReleaseSnapshot.url, $ReleaseUri, [StringComparison]::Ordinal)) { throw "Published V26 release repository identity mismatch during acknowledgement reconciliation." }
  if ($ReleaseSnapshot.draft -ne $false) { throw "Published V26 release reconciliation expected draft=false." }
  if (-not [string]::Equals([string]$ReleaseSnapshot.tag_name, $env:RELEASE_TAG, [StringComparison]::Ordinal)) { throw "Published V26 release tag mismatch during acknowledgement reconciliation." }
  if (-not [string]::Equals([string]$ReleaseSnapshot.target_commitish, $env:GITHUB_SHA, [StringComparison]::OrdinalIgnoreCase)) { throw "Published V26 release target SHA mismatch during acknowledgement reconciliation." }
  if ([bool]$ReleaseSnapshot.prerelease -ne $IsPrerelease) { throw "Published V26 release prerelease-state mismatch during acknowledgement reconciliation." }
  Assert-RemoteReleaseTagTargetsWorkflowSha
  if (@($ReleaseSnapshot.assets).Count -ne $ExpectedAssets.Count) { throw "Published V26 release asset count mismatch during acknowledgement reconciliation." }
  if ($VerifiedAssetIds.Count -ne $ExpectedAssets.Count) { throw "Verified V26 release asset identity set is incomplete; refusing acknowledgement recovery." }
  foreach ($expectedAsset in $ExpectedAssets) {
    $matches = @($ReleaseSnapshot.assets | Where-Object { [string]$_.name -ceq $expectedAsset })
    if ($matches.Count -ne 1) { throw "Published V26 release asset identity is ambiguous for $expectedAsset." }
    if (-not $VerifiedAssetIds.ContainsKey($expectedAsset) -or -not $AdmittedAssets.ContainsKey($expectedAsset)) { throw "Verified V26 release asset identity mapping is missing for $expectedAsset." }
    $publishedAsset = $matches[0]
    $expectedAssetId = [long]$VerifiedAssetIds[$expectedAsset]
    if ($expectedAssetId -le 0 -or [long]$publishedAsset.id -ne $expectedAssetId) { throw "Verified V26 release asset identity mismatch for $expectedAsset." }
    $expectedLength = [int64]$AdmittedAssets[$expectedAsset].Length
    if ([int64]$publishedAsset.size -ne $expectedLength) { throw "Published V26 release asset size mismatch for $expectedAsset during acknowledgement reconciliation." }
  }
}

$isPrerelease = $env:V26_RELEASE_REQUEST_PRERELEASE -eq 'true'
$signPackage = $env:V26_RELEASE_REQUEST_SIGN_PACKAGE -eq 'true'
$runtimeState = if ($env:RELEASE_RUN_RUNTIME -eq 'true') {
  if ($signPackage) { 'passed required V26 runtime gate on the exact signed release payload' }
  else { 'passed V26 runtime gate on the unsigned prerelease payload' }
} else { 'runtime skipped only for explicit prerelease preview' }
$signingState = if ($signPackage) { 'Authenticode-signed/timestamped with V26-only update manifest' } else { 'unsigned prerelease; manual install only' }
$expectedReleaseName = "QS3D for BricsCAD V26 $env:RELEASE_TAG"
$draftTransactionMarker = "QS3D-DRAFT-CREATE-V26:$env:GITHUB_RUN_ID:$env:GITHUB_RUN_ATTEMPT:$([Guid]::NewGuid().ToString('N'))"
$releaseBody = "Owner-dispatched V26 release. Source/Core/V26 build gates completed. Runtime: $runtimeState. Package: $signingState. V25 assets are not accepted by this lane.`n`nTransaction-Marker: $draftTransactionMarker"
$request = @{
  tag_name = $env:RELEASE_TAG
  target_commitish = $env:GITHUB_SHA
  name = $expectedReleaseName
  body = $releaseBody
  draft = $true
  prerelease = $isPrerelease
  generate_release_notes = $true
} | ConvertTo-Json

$tagCreatedByThisRun = $false
$tagReadyForRelease = $false
$releaseId = [long]0
$release = $null
$releaseUri = $null
$expectedAssets = @()
$admittedAssets = @{}
$verifiedAssetIds = @{}
$publishPatchAttempted = $false

try {
  $tagRefUri = "https://api.github.com/repos/$env:GITHUB_REPOSITORY/git/refs"
  $existingTag = Get-ExactReusableReleaseTag
  if ($null -ne $existingTag) {
    Write-Host "Reusing exact V26 lightweight tag $env:RELEASE_TAG at workflow SHA without claiming deletion ownership."
    $tagReadyForRelease = $true
  }
  else {
    $tagCreateRequest = @{ ref = $tagRef; sha = $env:GITHUB_SHA } | ConvertTo-Json
    try {
      $createdTag = Invoke-RestMethod -Method Post -Uri $tagRefUri -Headers $headers -ContentType 'application/json' -Body $tagCreateRequest
      if ($null -eq $createdTag -or -not [string]::Equals([string]$createdTag.ref, $tagRef, [StringComparison]::Ordinal)) { throw "V26 release tag creation returned a mismatched ref; refusing transaction ownership." }
      if ($null -eq $createdTag.object -or -not [string]::Equals([string]$createdTag.object.type, 'commit', [StringComparison]::Ordinal) -or -not [string]::Equals([string]$createdTag.object.sha, $env:GITHUB_SHA, [StringComparison]::OrdinalIgnoreCase)) { throw "V26 release tag creation returned a non-commit or mismatched SHA; refusing transaction ownership." }
      $tagCreatedByThisRun = $true
      $tagReadyForRelease = $true
    }
    catch {
      $tagCreateError = $_
      $reconciledTag = Get-ExactReusableReleaseTag
      if ($null -eq $reconciledTag) { throw "V26 tag-create acknowledgement failed and the exact release tag is authoritatively absent. Original error: $($tagCreateError.Exception.Message)" }
      Write-Host "V26 tag-create acknowledgement was ambiguous, but the exact lightweight tag now exists at workflow SHA; reusing it without deletion ownership."
      $tagCreatedByThisRun = $false
      $tagReadyForRelease = $true
    }
  }
  Assert-RemoteReleaseTagTargetsWorkflowSha

  try {
    $release = Invoke-RestMethod -Method Post -Uri "https://api.github.com/repos/$env:GITHUB_REPOSITORY/releases" -Headers $headers -ContentType 'application/json' -Body $request
  }
  catch {
    $draftCreateError = $_
    try {
      $reconciledDraft = Resolve-AmbiguousDraftCreate -TransactionMarker $draftTransactionMarker -ExpectedReleaseName $expectedReleaseName -IsPrerelease:$isPrerelease
    }
    catch {
      throw "V26 draft-create acknowledgement reconciliation failed. Original create error: $($draftCreateError.Exception.Message) Reconciliation error: $($_.Exception.Message)"
    }
    Write-Host "V26 draft-create acknowledgement was ambiguous, but exactly one transaction-owned draft was recovered."
    $release = $reconciledDraft
  }
  if ($null -eq $release -or [long]$release.id -le 0) { throw "V26 draft creation returned no usable release identity." }
  $expectedReleaseUri = "https://api.github.com/repos/$env:GITHUB_REPOSITORY/releases/$([long]$release.id)"
  if ($release.draft -ne $true -or
      -not [string]::Equals([string]$release.url, $expectedReleaseUri, [StringComparison]::Ordinal) -or
      -not [string]::Equals([string]$release.tag_name, $env:RELEASE_TAG, [StringComparison]::Ordinal) -or
      -not [string]::Equals([string]$release.target_commitish, $env:GITHUB_SHA, [StringComparison]::OrdinalIgnoreCase) -or
      -not [string]::Equals([string]$release.name, $expectedReleaseName, [StringComparison]::Ordinal) -or
      [bool]$release.prerelease -ne $isPrerelease -or
      ([string]$release.body).IndexOf($draftTransactionMarker, [StringComparison]::Ordinal) -lt 0) {
    throw "Newly-created/reconciled V26 draft identity, state, or transaction marker mismatch."
  }
  $releaseId = [long]$release.id
  Assert-RemoteReleaseTagTargetsWorkflowSha

  $releaseAssets = @('dist\QS3D-BricsCAD-V26.zip', 'dist\QS3D-BricsCAD-V26.zip.sha256', 'dist\QS3D-BricsCAD-V26.provenance.json')
  if ($signPackage) { $releaseAssets += 'dist\QS3D-BricsCAD-V26.update.json' }
  $uploadBase = $release.upload_url -replace '\{\?name,label\}$', ''
  foreach ($assetPath in $releaseAssets) {
    $name = [IO.Path]::GetFileName($assetPath)
    if ($name -match 'V25') { throw "V25 release asset leaked into V26 publication: $name" }
    if ($admittedAssets.ContainsKey($name)) { throw "Duplicate local V26 release asset name: $name" }
    $contentType = if ($name.EndsWith('.zip')) { 'application/zip' } elseif ($name.EndsWith('.json')) { 'application/json' } else { 'text/plain' }
    $admittedAsset = & .\scripts\invoke-v26-held-release-upload.ps1 `
      -UploadBase $uploadBase `
      -Token $env:GH_TOKEN `
      -Path $assetPath `
      -ContentType $contentType
    if ($null -eq $admittedAsset -or -not [string]::Equals([string]$admittedAsset.Name, $name, [StringComparison]::Ordinal)) {
      throw "V26 held upload returned no matching admitted record for $name."
    }
    $admittedAssets[$name] = $admittedAsset
  }

  $releaseUri = "https://api.github.com/repos/$env:GITHUB_REPOSITORY/releases/$releaseId"
  $draftRelease = Invoke-RestMethod -Method Get -Uri $releaseUri -Headers $headers
  $expectedAssets = @('QS3D-BricsCAD-V26.zip', 'QS3D-BricsCAD-V26.zip.sha256', 'QS3D-BricsCAD-V26.provenance.json')
  if ($signPackage) { $expectedAssets += 'QS3D-BricsCAD-V26.update.json' }
  if (@($draftRelease.assets).Count -ne $expectedAssets.Count) { throw "Draft V26 release contains unexpected assets; refusing publication." }

  $assetDownloadHeaders = @{}
  foreach ($key in $headers.Keys) { $assetDownloadHeaders[$key] = $headers[$key] }
  $assetDownloadHeaders['Accept'] = 'application/octet-stream'
  $verificationWorkspace = & .\scripts\v26-release-verification-workspace.ps1 -Operation Create -TempRoot $env:RUNNER_TEMP
  try {
    foreach ($expectedAsset in $expectedAssets) {
      $matches = @($draftRelease.assets | Where-Object { [string]$_.name -ceq $expectedAsset })
      if ($matches.Count -ne 1) { throw "Expected exactly one V26 release asset named $expectedAsset; found $($matches.Count). Release remains a draft." }
      if (-not $admittedAssets.ContainsKey($expectedAsset)) { throw "Admitted V26 release asset mapping is missing for $expectedAsset. Release remains a draft." }
      $uploadedAsset = $matches[0]
      $uploadedAssetId = [long]$uploadedAsset.id
      if ($uploadedAssetId -le 0) { throw "Uploaded V26 release asset returned no usable identity for $expectedAsset. Release remains a draft." }
      $admittedAsset = $admittedAssets[$expectedAsset]
      if ([long]$admittedAsset.UploadedAssetId -ne $uploadedAssetId) { throw "Uploaded V26 release asset identity changed after held upload for $expectedAsset. Release remains a draft." }
      $expectedLength = [int64]$admittedAssets[$expectedAsset].Length
      $remoteLength = [int64]$uploadedAsset.size
      if ($remoteLength -ne $expectedLength) { throw "Uploaded V26 release asset size mismatch for $expectedAsset. Admitted=$expectedLength Remote=$remoteLength. Release remains a draft." }
      $childName = 'asset-' + [Guid]::NewGuid().ToString('N')
      $downloadedAsset = & .\scripts\v26-release-verification-workspace.ps1 -Operation Child -Workspace $verificationWorkspace -ChildName $childName
      Invoke-WebRequest -Method Get -Uri ([string]$uploadedAsset.url) -Headers $assetDownloadHeaders -OutFile $downloadedAsset -UseBasicParsing
      $expectedHash = [string]$admittedAssets[$expectedAsset].Sha256
      $remoteHash = (& .\scripts\verify-v26-held-file.ps1 -Operation Hash -Path $downloadedAsset).Trim()
      if (-not [string]::Equals($expectedHash, $remoteHash, [StringComparison]::OrdinalIgnoreCase)) { throw "Uploaded V26 release asset SHA-256 mismatch for $expectedAsset. Release remains a draft." }
      $verifiedAssetIds[$expectedAsset] = $uploadedAssetId
    }
  }
  finally {
    if (-not [string]::IsNullOrWhiteSpace($verificationWorkspace)) { & .\scripts\v26-release-verification-workspace.ps1 -Operation Cleanup -Workspace $verificationWorkspace }
  }

  Assert-RemoteReleaseTagTargetsWorkflowSha

  $finalMainResponse = Invoke-RestMethod -Method Get -Uri "https://api.github.com/repos/$env:GITHUB_REPOSITORY/commits/main" -Headers $headers
  $finalMain = ([string]$finalMainResponse.sha).Trim().ToLowerInvariant()
  if ($finalMain -notmatch '^[0-9a-f]{40}$') {
    throw "Protected main API returned an invalid final V26-release SHA: $finalMain"
  }
  $finalMainRef = 'refs/remotes/origin/qs3d-v26-release-final-main'
  & git fetch --no-tags --force origin "+refs/heads/main:$finalMainRef"
  if ($LASTEXITCODE -ne 0) {
    throw 'Could not fetch protected main for final V26 release admission.'
  }
  $fetchedFinalMain = ([string](& git rev-parse --verify $finalMainRef)).Trim().ToLowerInvariant()
  if ($LASTEXITCODE -ne 0 -or $fetchedFinalMain -notmatch '^[0-9a-f]{40}$') {
    throw 'Could not resolve fetched protected-main SHA for final V26 release admission.'
  }
  if ($fetchedFinalMain -ne $finalMain) {
    throw "Protected-main API/fetch identity mismatch during final V26 publication admission. API=$finalMain fetched=$fetchedFinalMain"
  }
  & git merge-base --is-ancestor $env:GITHUB_SHA $finalMain
  $finalAncestorStatus = $LASTEXITCODE
  if ($finalAncestorStatus -ne 0) {
    throw "V26 release workflow SHA is no longer an ancestor of protected main during final publication admission: $env:GITHUB_SHA"
  }
  $finalReleaseRelevantPaths = @(
    '.github/workflows/',
    'scripts/',
    'src/QS3D.BricsCAD.V25/',
    'src/QS3D.BricsCAD.V26/',
    'src/QS3D.Core/',
    'Directory.Build.props',
    'Directory.Build.targets',
    'VERSION',
    'installer/',
    'external/'
  )
  & git diff --quiet --no-ext-diff "$env:GITHUB_SHA..$finalMain" -- @finalReleaseRelevantPaths
  $finalReleaseDriftStatus = $LASTEXITCODE
  if ($finalReleaseDriftStatus -eq 1) {
    throw 'newer release-relevant protected main supersedes this V26 publication; refusing stale V26 publication'
  }
  if ($finalReleaseDriftStatus -ne 0) {
    throw "Could not classify protected-main V26 release drift. git diff exit=$finalReleaseDriftStatus"
  }
  if ($finalMain -ne $env:GITHUB_SHA.ToLowerInvariant()) {
    Write-Host 'main advanced only through non-release paths during final V26 publication admission; published provenance remains pinned to GITHUB_SHA.'
  }
  $confirmedMainResponse = Invoke-RestMethod -Method Get -Uri "https://api.github.com/repos/$env:GITHUB_REPOSITORY/commits/main" -Headers $headers
  $confirmedFinalMain = ([string]$confirmedMainResponse.sha).Trim().ToLowerInvariant()
  if ($confirmedFinalMain -notmatch '^[0-9a-f]{40}$') {
    throw "Protected main confirmation returned an invalid V26-release SHA: $confirmedFinalMain"
  }
  if ($confirmedFinalMain -ne $finalMain) {
    throw "Protected main moved during final V26 release admission. Before=$finalMain after=$confirmedFinalMain"
  }

  $publishPatchAttempted = $true
  $published = Invoke-RestMethod -Method Patch -Uri $releaseUri -Headers $headers -ContentType 'application/json' -Body (@{ draft = $false } | ConvertTo-Json)
  Assert-PublishedReleaseMatchesVerifiedTransaction `
    -ReleaseSnapshot $published `
    -ReleaseUri $releaseUri `
    -ReleaseId $releaseId `
    -ExpectedAssets $expectedAssets `
    -VerifiedAssetIds $verifiedAssetIds `
    -AdmittedAssets $admittedAssets `
    -IsPrerelease $isPrerelease
}
catch {
  $publicationError = $_
  if (-not $tagReadyForRelease) { throw }

  if ($releaseId -gt 0) {
    try {
      $reconciledRelease = Invoke-RestMethod -Method Get -Uri $releaseUri -Headers $headers
      if ($reconciledRelease.draft -eq $false) {
        if (-not $publishPatchAttempted) { throw "V26 release became published before this workflow attempted the final publish PATCH." }
        Assert-PublishedReleaseMatchesVerifiedTransaction `
          -ReleaseSnapshot $reconciledRelease `
          -ReleaseUri $releaseUri `
          -ReleaseId $releaseId `
          -ExpectedAssets $expectedAssets `
          -VerifiedAssetIds $verifiedAssetIds `
          -AdmittedAssets $admittedAssets `
          -IsPrerelease $isPrerelease
        Write-Host "V26 publish acknowledgement was ambiguous, but authoritative release state confirms the exact qualified release is already published; treating publication as committed."
        return
      }
      if ($reconciledRelease.draft -ne $true) { throw "V26 release draft state is ambiguous during acknowledgement reconciliation." }
    }
    catch {
      $reconciliationError = $_
      throw "V26 publication acknowledgement reconciliation failed. Original publication error: $($publicationError.Exception.Message) Reconciliation error: $($reconciliationError.Exception.Message) Manual cleanup is required before retry."
    }
  }

  try {
    & .\scripts\rollback-v26-draft-release.ps1 `
      -Repository $env:GITHUB_REPOSITORY `
      -ReleaseId $releaseId `
      -ReleaseTag $env:RELEASE_TAG `
      -WorkflowSha $env:GITHUB_SHA `
      -TagCreatedByThisRun $tagCreatedByThisRun `
      -Token $env:GH_TOKEN | Out-Null
  }
  catch {
    $rollbackError = $_
    throw "Automatic V26 draft rollback failed. Original publication error: $($publicationError.Exception.Message) Rollback error: $($rollbackError.Exception.Message) Manual cleanup is required before retry."
  }
  throw "V26 publication failed after exact release-tag admission: $($publicationError.Exception.Message) Automatic rollback completed; retry with the same tag is safe."
}