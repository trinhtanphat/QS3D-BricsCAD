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

# Keep release-drift admission pathname-safe. Do not parse line-oriented
# `git diff --name-only` output: Git may quote/escape hostile-but-valid pathnames,
# and embedded newlines cannot be represented safely as one PowerShell line.
# Instead ask Git itself whether any path in the owned release surface differs.
$releaseRelevantPathspecs = @(
    'src/',
    'tests/',
    'scripts/',
    'external/QS3D-Platform',
    '.gitmodules',
    'Directory.Build.props',
    'QS3D.sln',
    'QS3D.V26.sln',
    '.github/workflows/release-v25-cloud.yml',
    '.github/workflows/dispatch-v25-cloud-after-main-integration.yml'
)

$workspaceVersionPaths = @(
    'src/QS3D.BricsCAD.V25/QS3D.BricsCAD.V25.csproj',
    'src/QS3D.BricsCAD.V26/QS3D.BricsCAD.V26.csproj',
    'src/QS3D.Core/QS3D.Core.csproj'
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

function Test-ReleaseRelevantDrift {
    param([Parameter(Mandatory = $true)][string]$TargetSha)

    if ([string]::Equals($TargetSha, $dispatch, [StringComparison]::OrdinalIgnoreCase)) {
        return $false
    }

    & git merge-base --is-ancestor $dispatch $TargetSha
    if ($LASTEXITCODE -ne 0) {
        throw "Dispatched source $dispatch is not an ancestor of current main $TargetSha. Refusing ambiguous release preparation."
    }

    $range = "${dispatch}..${TargetSha}"
    & git diff --quiet --no-ext-diff $range -- @releaseRelevantPathspecs
    $diffExit = $LASTEXITCODE
    if ($diffExit -eq 0) {
        return $false
    }
    if ($diffExit -eq 1) {
        return $true
    }
    throw "Could not inspect release-relevant main drift between $dispatch and $TargetSha (git diff exit $diffExit)."
}

function Assert-ReleaseBaseIsSafe {
    param([Parameter(Mandatory = $true)][string]$TargetSha)

    if (Test-ReleaseRelevantDrift -TargetSha $TargetSha) {
        throw "main moved after dispatch with release-relevant changes. Dispatched=$dispatch current-origin/main=$TargetSha. A newer release-relevant main push must own the next release."
    }
}

function Set-ProjectVersionValue {
    param(
        [Parameter(Mandatory = $true)][string]$Name,
        [Parameter(Mandatory = $true)][string]$Value,
        [Parameter(Mandatory = $true)][ref]$Content,
        [Parameter(Mandatory = $true)][string]$ProjectPath
    )

    $escapedName = [regex]::Escape($Name)
    $pattern = "(?s)(<$escapedName>)[^<]*(</$escapedName>)"
    $current = [string]$Content.Value
    $matches = [regex]::Matches($current, $pattern)
    if ($matches.Count -ne 1) {
        throw "Expected exactly one <$Name> element in '$ProjectPath', found $($matches.Count)."
    }

    $match = $matches[0]
    $replacement = $match.Groups[1].Value + $Value + $match.Groups[2].Value
    $Content.Value = $current.Substring(0, $match.Index) + $replacement + $current.Substring($match.Index + $match.Length)
}

function Set-WorkspaceProductVersion {
    param([Parameter(Mandatory = $true)][string]$ReleaseTagValue)

    $tag = $ReleaseTagValue
    $tagMatch = [regex]::Match($tag, '^v(?<major>[0-9]+)\.(?<minor>[0-9]+)\.(?<patch>[0-9]+)-preview\.(?<ordinal>[1-9][0-9]*)$')
    if (-not $tagMatch.Success) {
        throw "Could not derive workspace ProductVersion from release tag '$tag'."
    }

    $productVersion = $tag.Substring(1)
    $fileVersion = "$($tagMatch.Groups['major'].Value).$($tagMatch.Groups['minor'].Value).$($tagMatch.Groups['patch'].Value).$($tagMatch.Groups['ordinal'].Value)"

    foreach ($relativePath in $workspaceVersionPaths) {
        if (-not (Test-Path -LiteralPath $relativePath -PathType Leaf)) {
            throw "Could not locate workspace project version source: $relativePath"
        }

        $content = [System.IO.File]::ReadAllText($relativePath)
        Set-ProjectVersionValue -Name 'Version' -Value $productVersion -Content ([ref]$content) -ProjectPath $relativePath
        Set-ProjectVersionValue -Name 'FileVersion' -Value $fileVersion -Content ([ref]$content) -ProjectPath $relativePath
        Set-ProjectVersionValue -Name 'InformationalVersion' -Value $productVersion -Content ([ref]$content) -ProjectPath $relativePath
        [System.IO.File]::WriteAllText($relativePath, $content, [System.Text.UTF8Encoding]::new($false))
    }
}

function Get-CheckedOutProductVersion {
    $projectPath = Join-Path $root 'src/QS3D.BricsCAD.V25/QS3D.BricsCAD.V25.csproj'
    if (-not (Test-Path -LiteralPath $projectPath -PathType Leaf)) {
        throw "Could not locate checked-out V25 project version source: $projectPath"
    }

    [xml]$projectXml = Get-Content -LiteralPath $projectPath -Raw
    $values = @(
        $projectXml.Project.PropertyGroup |
            ForEach-Object { $_.Version } |
            Where-Object { $null -ne $_ } |
            ForEach-Object { ([string]$_).Trim() } |
            Where-Object { -not [string]::IsNullOrWhiteSpace($_) } |
            Sort-Object -Unique
    )
    if ($values.Count -ne 1) {
        throw "Checked-out V25 project must contain exactly one unambiguous Version value. Found $($values.Count)."
    }
    return [string]$values[0]
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
            throw "Release base must be clean before workspace version synchronization. Unexpected status '$($entry.State)' at $($entry.Path)."
        }

        if ($releaseBase -ne $dispatch) {
            Write-Host "main advanced only through non-release paths; release preparation is rebased safely from dispatched source $dispatch onto $releaseBase."
        }

        & python (Join-Path $PSScriptRoot 'preflight-runtime-product-version-identity.py')
        if ($LASTEXITCODE -ne 0) {
            throw 'Runtime product-version identity preflight failed for committed release source.'
        }

        Set-WorkspaceProductVersion -ReleaseTagValue $tag

        & python (Join-Path $PSScriptRoot 'preflight-runtime-product-version-identity.py')
        if ($LASTEXITCODE -ne 0) {
            throw 'Runtime product-version identity preflight failed after workspace synchronization.'
        }

        $expectedProductVersion = $tag.Substring(1)
        $checkedOutProductVersion = Get-CheckedOutProductVersion
        if (-not [string]::Equals($checkedOutProductVersion, $expectedProductVersion, [StringComparison]::Ordinal)) {
            throw "Workspace ProductVersion '$checkedOutProductVersion' does not match requested release identity '$expectedProductVersion'."
        }
        Write-Host "Workspace ProductVersion '$checkedOutProductVersion' matches requested release tag '$tag' at protected-main source $releaseBase."

        & git diff --check
        if ($LASTEXITCODE -ne 0) {
            throw 'Release-preparation diff failed git diff --check.'
        }

        $workspaceHead = ([string](& git rev-parse --verify HEAD)).Trim().ToLowerInvariant()
        if ($LASTEXITCODE -ne 0 -or $workspaceHead -ne $releaseBase) {
            throw "Release workspace HEAD must remain the protected-main source commit. Expected $releaseBase, got $workspaceHead."
        }

        $finalStatus = @(Get-ReleaseStatusEntries)
        if ($finalStatus.Count -ne $workspaceVersionPaths.Count) {
            throw 'Workspace version synchronization did not produce exactly three bounded project modifications.'
        }
        foreach ($entry in $finalStatus) {
            if ($entry.State -ne ' M' -or -not ($workspaceVersionPaths -contains $entry.Path)) {
                throw "Unexpected release-preparation workspace change '$($entry.State)' at $($entry.Path)."
            }
        }
        foreach ($relativePath in $workspaceVersionPaths) {
            if (-not ($finalStatus.Path -contains $relativePath)) {
                throw "Workspace version synchronization did not modify required project identity source: $relativePath"
            }
        }

        $latestMain = Get-RemoteMain
        Assert-ReleaseBaseIsSafe -TargetSha $latestMain
        if ($latestMain -ne $releaseBase) {
            if ($attempt -ge $maxAttempts) {
                throw "main kept advancing through non-release paths during $maxAttempts protected-main release-preparation attempts. Retry from a fresh workflow run."
            }
            Write-Host "main advanced through additional non-release paths while validating release source ($releaseBase -> $latestMain); retrying without writing main."
            continue
        }

        Write-Host "Release source identity $tag is synchronized only in the bounded workspace on protected-main source $releaseBase."
        Write-Host 'No commit, push, branch-protection bypass, or protected-main mutation was performed by release preparation.'
        Write-Output $releaseBase
        return
    }

    throw 'Release preparation exhausted its retry loop unexpectedly.'
}
finally {
    Pop-Location
}
