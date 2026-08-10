[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$BricsCadDir,
    [string]$Profile = "",
    [string]$ArtifactDir = "",
    [switch]$SkipRuntime,
    [switch]$SkipScreenshot,
    [switch]$Package,
    [string]$ReleaseTag = ""
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($ArtifactDir)) {
    $ArtifactDir = Join-Path $repoRoot "artifacts\local-v25-qualification"
}
$ArtifactDir = [IO.Path]::GetFullPath($ArtifactDir)
$BricsCadDir = [IO.Path]::GetFullPath($BricsCadDir)
New-Item -ItemType Directory -Path $ArtifactDir -Force | Out-Null

$steps = New-Object System.Collections.Generic.List[object]
$fatal = $null
$headSha = ""
$branchName = ""
$pluginDll = Join-Path $repoRoot "src\QS3D.BricsCAD.V25\bin\x64\Release\net48\QS3D.BricsCAD.V25.dll"
$runtimeArtifacts = Join-Path $ArtifactDir "runtime"
$reportPath = Join-Path $ArtifactDir "qualification.json"
$startedAt = [DateTime]::UtcNow

function Invoke-ExternalChecked {
    param(
        [Parameter(Mandatory = $true)][string]$FilePath,
        [Parameter(Mandatory = $true)][string[]]$Arguments
    )
    & $FilePath @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "$FilePath exited with code $LASTEXITCODE."
    }
}

function Invoke-QualificationStep {
    param(
        [Parameter(Mandatory = $true)][string]$Name,
        [Parameter(Mandatory = $true)][scriptblock]$Action
    )
    $started = [DateTime]::UtcNow
    Write-Host ""
    Write-Host ("=== {0} ===" -f $Name)
    try {
        & $Action
        $steps.Add([pscustomobject]@{
            name = $Name
            status = "PASS"
            startedUtc = $started.ToString("O")
            completedUtc = [DateTime]::UtcNow.ToString("O")
        })
    }
    catch {
        $steps.Add([pscustomobject]@{
            name = $Name
            status = "FAIL"
            startedUtc = $started.ToString("O")
            completedUtc = [DateTime]::UtcNow.ToString("O")
            error = $_.Exception.Message
        })
        throw
    }
}

try {
    if ([Environment]::OSVersion.Platform -ne [PlatformID]::Win32NT) {
        throw "Local V25 qualification requires Windows."
    }
    if (-not [Environment]::UserInteractive -and -not $SkipRuntime) {
        throw "Interactive Windows is required unless -SkipRuntime is explicitly used."
    }

    foreach ($command in @("git", "python", "dotnet")) {
        if (-not (Get-Command $command -ErrorAction SilentlyContinue)) {
            throw "Required command is not available on PATH: $command"
        }
    }
    foreach ($name in @("bricscad.exe", "BrxMgd.dll", "TD_Mgd.dll")) {
        $path = Join-Path $BricsCadDir $name
        if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
            throw "Required BricsCAD V25 runtime file is missing: $path"
        }
    }

    Push-Location $repoRoot
    try {
        Invoke-QualificationStep "Exact Git SHA / clean tree" {
            $script:headSha = (& git rev-parse HEAD).Trim()
            if ($LASTEXITCODE -ne 0 -or $script:headSha -notmatch '^[0-9a-fA-F]{40}$') {
                throw "Could not resolve an exact Git HEAD SHA."
            }
            $script:branchName = (& git rev-parse --abbrev-ref HEAD).Trim()
            if ($LASTEXITCODE -ne 0) { throw "Could not resolve the current Git branch." }
            $dirty = @(& git status --porcelain)
            if ($LASTEXITCODE -ne 0) { throw "git status failed." }
            if ($dirty.Count -gt 0) {
                throw "Working tree is dirty. Qualification must run against an exact reproducible SHA."
            }
            Write-Host "HEAD: $script:headSha"
            Write-Host "Branch: $script:branchName"
        }

        Invoke-QualificationStep "Manual-only CI policy" {
            Invoke-ExternalChecked "python" @("scripts/preflight-ci-manual-only.py")
        }
        Invoke-QualificationStep "Generic source preflight" {
            Invoke-ExternalChecked "python" @("scripts/preflight.py")
        }
        Invoke-QualificationStep "Aggregate feature preflights" {
            Invoke-ExternalChecked "python" @("scripts/preflight-all.py")
        }
        Invoke-QualificationStep "Core Release build" {
            Invoke-ExternalChecked "dotnet" @("build", "src/QS3D.Core/QS3D.Core.csproj", "-c", "Release")
        }
        Invoke-QualificationStep "Core deterministic smoke suite" {
            Invoke-ExternalChecked "dotnet" @("run", "--project", "tests/QS3D.Core.SmokeTests/QS3D.Core.SmokeTests.csproj", "-c", "Release")
        }

        $oldBricsCadDir = $env:BRICSCAD_V25_DIR
        try {
            $env:BRICSCAD_V25_DIR = $BricsCadDir
            Invoke-QualificationStep "BricsCAD V25 adapter Release build" {
                Invoke-ExternalChecked "dotnet" @("build", "src/QS3D.BricsCAD.V25/QS3D.BricsCAD.V25.csproj", "-c", "Release", "-p:Platform=x64")
                if (-not (Test-Path -LiteralPath $pluginDll -PathType Leaf)) {
                    throw "Expected V25 plugin output is missing: $pluginDll"
                }
            }
        }
        finally {
            $env:BRICSCAD_V25_DIR = $oldBricsCadDir
        }

        Invoke-QualificationStep "WPF theme resource smoke" {
            & (Join-Path $PSScriptRoot "test-wpf-theme-runtime.ps1")
        }
        Invoke-QualificationStep "WPF Workspace / RightPanel layout smoke" {
            & (Join-Path $PSScriptRoot "test-wpf-palettes-runtime.ps1") `
                -PluginPath $pluginDll `
                -BricscadDirectory $BricsCadDir
        }

        if (-not $SkipRuntime) {
            Invoke-QualificationStep "Licensed V25 NETLOAD / Ribbon / Palette runtime probe" {
                $runtimeArgs = @{
                    BricsCadDir = $BricsCadDir
                    PluginDll = $pluginDll
                    Profile = $Profile
                    ArtifactDir = $runtimeArtifacts
                    SkipScreenshot = [bool]$SkipScreenshot
                }
                & (Join-Path $PSScriptRoot "test-bricscad-v25-runtime.ps1") @runtimeArgs
            }
        }
        else {
            $steps.Add([pscustomobject]@{
                name = "Licensed V25 NETLOAD / Ribbon / Palette runtime probe"
                status = "SKIPPED"
                startedUtc = [DateTime]::UtcNow.ToString("O")
                completedUtc = [DateTime]::UtcNow.ToString("O")
                error = "Explicit -SkipRuntime; this result cannot qualify a customer release."
            })
        }

        if ($Package) {
            Invoke-QualificationStep "Build local V25 package" {
                if ([string]::IsNullOrWhiteSpace($ReleaseTag)) {
                    throw "-Package requires -ReleaseTag so package-v25.ps1 can enforce exact product SemVer binding."
                }
                $oldReleaseTag = $env:RELEASE_TAG
                try {
                    $env:RELEASE_TAG = $ReleaseTag.Trim()
                    & (Join-Path $PSScriptRoot "package-v25.ps1")
                }
                finally {
                    $env:RELEASE_TAG = $oldReleaseTag
                }
                $zip = Join-Path $repoRoot "dist\QS3D-BricsCAD-V25.zip"
                if (-not (Test-Path -LiteralPath $zip -PathType Leaf)) {
                    throw "Local package step completed without the expected ZIP: $zip"
                }
            }
        }
    }
    finally {
        Pop-Location
    }
}
catch {
    $fatal = $_
}
finally {
    $pluginHash = ""
    if (Test-Path -LiteralPath $pluginDll -PathType Leaf) {
        try { $pluginHash = (Get-FileHash -LiteralPath $pluginDll -Algorithm SHA256).Hash.ToLowerInvariant() }
        catch { $pluginHash = "" }
    }
    $runtimeMetadata = Join-Path $runtimeArtifacts "runtime-metadata.json"
    $status = if ($null -eq $fatal) { "PASS" } else { "FAIL" }
    $report = [ordered]@{
        schema = 1
        status = $status
        exactSha = $headSha
        branch = $branchName
        startedUtc = $startedAt.ToString("O")
        completedUtc = [DateTime]::UtcNow.ToString("O")
        runnerUser = [Environment]::UserName
        interactive = [Environment]::UserInteractive
        bricsCadDir = $BricsCadDir
        pluginDll = $pluginDll
        pluginSha256 = $pluginHash
        runtimeSkipped = [bool]$SkipRuntime
        runtimeMetadata = if (Test-Path -LiteralPath $runtimeMetadata -PathType Leaf) { $runtimeMetadata } else { "" }
        packageRequested = [bool]$Package
        releaseTag = $ReleaseTag
        manualScenarioChecklist = "docs/LOCAL-V25-QUALIFICATION.md"
        steps = @($steps)
        error = if ($null -eq $fatal) { "" } else { $fatal.Exception.Message }
    }
    $report | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $reportPath -Encoding UTF8
    Write-Host ""
    Write-Host "Qualification report: $reportPath"
}

if ($null -ne $fatal) {
    throw $fatal
}

Write-Host ""
Write-Host "AUTOMATED LOCAL V25 QUALIFICATION PASS for exact SHA $headSha"
Write-Host "This does not replace the manual/private-DWG scenario checklist in docs/LOCAL-V25-QUALIFICATION.md."
