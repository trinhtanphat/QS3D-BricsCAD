param(
    [Parameter(Mandatory = $true)]
    [string]$ReleaseTag,

    [Parameter(Mandatory = $true)]
    [string]$DispatchSha
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$tag = $ReleaseTag.Trim()
$dispatch = $DispatchSha.Trim().ToLowerInvariant()

if ($tag -notmatch '^v(?:0|[1-9][0-9]*)\.(?:0|[1-9][0-9]*)\.(?:0|[1-9][0-9]*)-preview\.[1-9][0-9]*$') {
    throw "ReleaseTag must use the exact preview shape v<major>.<minor>.<patch>-preview.<n>. Got: $ReleaseTag"
}
if ($dispatch -notmatch '^[0-9a-f]{40}$') {
    throw "DispatchSha must be one exact 40-hex commit. Got: $DispatchSha"
}

function Get-ReleaseStatusEntries {
    $lines = @(& git status --porcelain=v1 --untracked-files=all -- . ':(exclude).nuget/packages/**')
    if ($LASTEXITCODE -ne 0) {
        throw 'Could not inspect release-preparation Git status.'
    }

    $entries = @()
    foreach ($rawLine in $lines) {
        $line = [string]$rawLine
        if ($line.Length -lt 4) {
            throw "Could not parse release-preparation Git status: $line"
        }
        $state = $line.Substring(0, 2)
        $path = $line.Substring(3).Trim().Replace('\', '/')
        if ([string]::IsNullOrWhiteSpace($path) -or $path.StartsWith('"')) {
            throw "Release-preparation Git status contains an unsupported path representation: $line"
        }
        $entries += [pscustomobject]@{
            State = $state
            Path = $path
        }
    }
    return @($entries)
}

Push-Location $root
try {
    $head = ([string](& git rev-parse --verify HEAD)).Trim().ToLowerInvariant()
    if ($LASTEXITCODE -ne 0 -or $head -ne $dispatch) {
        throw "Release preparation must start from the dispatched commit. Expected $dispatch, got $head."
    }

    $initialStatus = @(Get-ReleaseStatusEntries)
    foreach ($entry in $initialStatus) {
        throw "Release preparation must start from a clean checkout/index. Unexpected status '$($entry.State)' at $($entry.Path)."
    }

    & (Join-Path $PSScriptRoot 'sync-preview-release-version.ps1') -ReleaseTag $tag
    if ($LASTEXITCODE -ne 0) {
        throw "Preview source identity synchronization failed for $tag."
    }

    & python (Join-Path $PSScriptRoot 'preflight-runtime-product-version-identity.py')
    if ($LASTEXITCODE -ne 0) {
        throw 'Runtime product-version identity preflight failed after synchronization.'
    }

    $allowed = @(
        'src/QS3D.BricsCAD.V25/QS3D.BricsCAD.V25.csproj',
        'src/QS3D.BricsCAD.V26/QS3D.BricsCAD.V26.csproj',
        'src/QS3D.Core/QS3D.Core.csproj'
    )
    $status = @(Get-ReleaseStatusEntries)
    $changed = @()
    foreach ($entry in $status) {
        if ($entry.State -ne ' M') {
            throw "Preview synchronization produced an unexpected Git status '$($entry.State)' at $($entry.Path)."
        }
        if ($entry.Path -notin $allowed) {
            throw "Release preparation touched an unexpected path: $($entry.Path)"
        }
        $changed += $entry.Path
    }
    $changed = @($changed | Sort-Object -Unique)

    & git diff --check
    if ($LASTEXITCODE -ne 0) {
        throw 'Release-preparation diff failed git diff --check.'
    }

    if ($changed.Count -eq 0) {
        Write-Host "Source identity already matches $tag; immutable release commit remains $dispatch even if main advances later."
        Write-Output $dispatch
        return
    }

    & git fetch --no-tags origin main
    if ($LASTEXITCODE -ne 0) {
        throw 'Could not refresh origin/main before release preparation.'
    }
    $remoteMain = ([string](& git rev-parse --verify origin/main)).Trim().ToLowerInvariant()
    if ($LASTEXITCODE -ne 0 -or $remoteMain -notmatch '^[0-9a-f]{40}$') {
        throw 'Could not resolve origin/main before release preparation.'
    }
    if ($remoteMain -ne $dispatch) {
        throw "main moved after this workflow was dispatched. Dispatched=$dispatch current-origin/main=$remoteMain. Start a fresh release run instead of overwriting concurrent work."
    }

    & git config user.name 'github-actions[bot]'
    if ($LASTEXITCODE -ne 0) { throw 'Could not configure release commit author name.' }
    & git config user.email '41898282+github-actions[bot]@users.noreply.github.com'
    if ($LASTEXITCODE -ne 0) { throw 'Could not configure release commit author email.' }

    & git add -- @allowed
    if ($LASTEXITCODE -ne 0) {
        throw 'Could not stage synchronized release identity files.'
    }
    $staged = @(& git diff --cached --name-only --)
    if ($LASTEXITCODE -ne 0) {
        throw 'Could not inspect staged release-preparation files.'
    }
    $staged = @($staged | ForEach-Object { ([string]$_).Trim().Replace('\', '/') } | Where-Object { $_ } | Sort-Object -Unique)
    $missingStaged = @($changed | Where-Object { $_ -notin $staged })
    $unexpectedStaged = @($staged | Where-Object { $_ -notin $changed })
    if ($missingStaged.Count -ne 0 -or $unexpectedStaged.Count -ne 0) {
        throw 'Staged release-preparation file set does not exactly match the validated source changes.'
    }
    foreach ($path in $staged) {
        if ($path -notin $allowed) {
            throw "Unexpected staged release-preparation path: $path"
        }
    }

    $postStageStatus = @(Get-ReleaseStatusEntries)
    foreach ($entry in $postStageStatus) {
        if ($entry.State -ne 'M ' -or $entry.Path -notin $staged) {
            throw "Release-preparation working tree changed after staging: '$($entry.State)' at $($entry.Path)."
        }
    }
    & git diff --cached --check
    if ($LASTEXITCODE -ne 0) {
        throw 'Staged release-preparation diff failed git diff --cached --check.'
    }

    & git commit -m "chore(release): prepare $tag"
    if ($LASTEXITCODE -ne 0) {
        throw "Could not create the release-preparation commit for $tag."
    }
    $releaseCommit = ([string](& git rev-parse --verify HEAD)).Trim().ToLowerInvariant()
    if ($LASTEXITCODE -ne 0 -or $releaseCommit -notmatch '^[0-9a-f]{40}$' -or $releaseCommit -eq $dispatch) {
        throw 'Could not resolve the newly created release-preparation commit.'
    }

    $postCommitStatus = @(Get-ReleaseStatusEntries)
    foreach ($entry in $postCommitStatus) {
        throw "Release-preparation working tree is not clean after commit: '$($entry.State)' at $($entry.Path)."
    }

    & git push origin 'HEAD:refs/heads/main'
    if ($LASTEXITCODE -ne 0) {
        throw 'Could not fast-forward main with the release-preparation commit. Concurrent work may have landed; start a fresh release run.'
    }

    & git fetch --no-tags origin main
    if ($LASTEXITCODE -ne 0) {
        throw 'Could not verify origin/main after release-preparation push.'
    }
    $pushedMain = ([string](& git rev-parse --verify origin/main)).Trim().ToLowerInvariant()
    if ($pushedMain -ne $releaseCommit) {
        throw "Release-preparation push was not read back exactly. Expected $releaseCommit, got $pushedMain."
    }

    Write-Host "Prepared exact release source commit $releaseCommit for $tag."
    Write-Output $releaseCommit
}
finally {
    Pop-Location
}