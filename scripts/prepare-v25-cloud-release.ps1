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

$releaseRelevantPrefixes = @(
    'src/',
    'tests/',
    'scripts/'
)
$releaseRelevantExactPaths = @(
    'Directory.Build.props',
    'QS3D.sln',
    'QS3D.V26.sln',
    '.github/workflows/release-v25-cloud.yml',
    '.github/workflows/dispatch-v25-cloud-after-main-integration.yml'
)

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

function Test-ReleaseRelevantPath {
    param([Parameter(Mandatory = $true)][string]$Path)

    $normalized = $Path.Trim().Replace('\', '/')
    if ($normalized -in $releaseRelevantExactPaths) {
        return $true
    }
    foreach ($prefix in $releaseRelevantPrefixes) {
        if ($normalized.StartsWith($prefix, [StringComparison]::Ordinal)) {
            return $true
        }
    }
    return $false
}

function Get-RemoteMain {
    & git fetch --no-tags origin '+refs/heads/main:refs/remotes/origin/main'
    if ($LASTEXITCODE -ne 0) {
        throw 'Could not refresh origin/main before release preparation.'
    }
    $remoteMain = ([string](& git rev-parse --verify origin/main)).Trim().ToLowerInvariant()
    if ($LASTEXITCODE -ne 0 -or $remoteMain -notmatch '^[0-9a-f]{40}$') {
        throw 'Could not resolve origin/main before release preparation.'
    }
    return $remoteMain
}

function Get-ReleaseRelevantDriftPaths {
    param([Parameter(Mandatory = $true)][string]$TargetSha)

    if ([string]::Equals($TargetSha, $dispatch, [StringComparison]::OrdinalIgnoreCase)) {
        return @()
    }

    & git merge-base --is-ancestor $dispatch $TargetSha
    if ($LASTEXITCODE -ne 0) {
        throw "Dispatched source $dispatch is not an ancestor of current main $TargetSha. Refusing ambiguous release preparation."
    }

    $range = "${dispatch}..${TargetSha}"
    $paths = @(& git diff --name-only $range --)
    if ($LASTEXITCODE -ne 0) {
        throw "Could not inspect main drift between $dispatch and $TargetSha."
    }

    $relevant = @()
    foreach ($rawPath in $paths) {
        $path = ([string]$rawPath).Trim().Replace('\', '/')
        if (-not [string]::IsNullOrWhiteSpace($path) -and (Test-ReleaseRelevantPath -Path $path)) {
            $relevant += $path
        }
    }
    return @($relevant | Sort-Object -Unique)
}

function Assert-ReleaseBaseIsSafe {
    param([Parameter(Mandatory = $true)][string]$TargetSha)

    $relevant = @(Get-ReleaseRelevantDriftPaths -TargetSha $TargetSha)
    if ($relevant.Count -ne 0) {
        throw "main moved after dispatch with release-relevant changes. Dispatched=$dispatch current-origin/main=$TargetSha paths=$($relevant -join ', '). A newer release-relevant main push must own the next release."
    }
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

    & (Join-Path $PSScriptRoot 'validate-preview-release-sequence.ps1') -ReleaseTag $tag

    $allowed = @(
        'src/QS3D.BricsCAD.V25/QS3D.BricsCAD.V25.csproj',
        'src/QS3D.BricsCAD.V26/QS3D.BricsCAD.V26.csproj',
        'src/QS3D.Core/QS3D.Core.csproj'
    )

    & git config user.name 'github-actions[bot]'
    if ($LASTEXITCODE -ne 0) { throw 'Could not configure release commit author name.' }
    & git config user.email '41898282+github-actions[bot]@users.noreply.github.com'
    if ($LASTEXITCODE -ne 0) { throw 'Could not configure release commit author email.' }

    $maxAttempts = 12
    for ($attempt = 1; $attempt -le $maxAttempts; $attempt++) {
        $releaseBase = Get-RemoteMain
        Assert-ReleaseBaseIsSafe -TargetSha $releaseBase

        & git reset --hard
        if ($LASTEXITCODE -ne 0) {
            throw 'Could not reset release workspace before selecting the current safe main base.'
        }
        & git checkout --detach $releaseBase
        if ($LASTEXITCODE -ne 0) {
            throw "Could not move release preparation onto safe main base $releaseBase."
        }

        $baseStatus = @(Get-ReleaseStatusEntries)
        foreach ($entry in $baseStatus) {
            throw "Release base must be clean before version synchronization. Unexpected status '$($entry.State)' at $($entry.Path)."
        }

        if ($releaseBase -ne $dispatch) {
            Write-Host "main advanced only through non-release paths; release preparation is rebased safely from dispatched source $dispatch onto $releaseBase."
        }

        & (Join-Path $PSScriptRoot 'sync-preview-release-version.ps1') -ReleaseTag $tag
        if ($LASTEXITCODE -ne 0) {
            throw "Preview source identity synchronization failed for $tag."
        }

        & python (Join-Path $PSScriptRoot 'preflight-runtime-product-version-identity.py')
        if ($LASTEXITCODE -ne 0) {
            throw 'Runtime product-version identity preflight failed after synchronization.'
        }

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
            $latestMain = Get-RemoteMain
            Assert-ReleaseBaseIsSafe -TargetSha $latestMain
            if ($latestMain -ne $releaseBase) {
                Write-Host "main advanced through additional non-release paths while checking existing release identity; retrying on $latestMain."
                continue
            }
            Write-Host "Source identity already matches $tag on safe main base $releaseBase."
            Write-Output $releaseBase
            return
        }

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
        if ($LASTEXITCODE -ne 0 -or $releaseCommit -notmatch '^[0-9a-f]{40}$' -or $releaseCommit -eq $releaseBase) {
            throw 'Could not resolve the newly created release-preparation commit.'
        }
        $releaseParent = ([string](& git rev-parse --verify 'HEAD^')).Trim().ToLowerInvariant()
        if ($LASTEXITCODE -ne 0 -or $releaseParent -ne $releaseBase) {
            throw "Release-preparation commit parent mismatch. Expected safe base $releaseBase, got $releaseParent."
        }

        $postCommitStatus = @(Get-ReleaseStatusEntries)
        foreach ($entry in $postCommitStatus) {
            throw "Release-preparation working tree is not clean after commit: '$($entry.State)' at $($entry.Path)."
        }

        & git push origin 'HEAD:refs/heads/main'
        $pushExit = $LASTEXITCODE
        if ($pushExit -eq 0) {
            $pushedMain = Get-RemoteMain
            if ($pushedMain -ne $releaseCommit) {
                throw "Release-preparation push was not read back exactly. Expected $releaseCommit, got $pushedMain."
            }

            Write-Host "Prepared exact release source commit $releaseCommit for $tag from safe main base $releaseBase (trigger $dispatch)."
            Write-Output $releaseCommit
            return
        }

        $afterPushMain = Get-RemoteMain
        Assert-ReleaseBaseIsSafe -TargetSha $afterPushMain
        if ($afterPushMain -eq $releaseBase) {
            throw 'Could not fast-forward main with the release-preparation commit even though origin/main did not advance. Check repository write permissions or remote health.'
        }
        if ($attempt -ge $maxAttempts) {
            throw "main kept advancing through non-release paths during $maxAttempts safe release-preparation attempts. Retry from a fresh workflow run."
        }
        Write-Host "Release-preparation CAS lost to non-release main drift ($releaseBase -> $afterPushMain); retrying without overwriting concurrent work."
    }

    throw 'Release preparation exhausted its retry loop unexpectedly.'
}
finally {
    Pop-Location
}
