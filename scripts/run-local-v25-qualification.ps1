[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$BricsCadDir,
    [string]$Profile = "",
    [string]$ArtifactDir = "",
    [string]$PythonPath = "",
    [switch]$SkipRuntime,
    [switch]$SkipScreenshot,
    [switch]$Package,
    [string]$ReleaseTag = "",
    [switch]$SignPackage,
    [ValidatePattern('^[0-9A-Fa-f]{40}$')]
    [string]$SigningCertThumbprint = "",
    [ValidatePattern('^https://')]
    [string]$TimestampUrl = ""
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
$packageDir = Join-Path $repoRoot "dist\QS3D-BricsCAD-V25"
$packageZip = Join-Path $repoRoot "dist\QS3D-BricsCAD-V25.zip"
$startedAt = [DateTime]::UtcNow
$sourceBuildCompleted = $false
$wpfSmokeCompleted = $false
$runtimeSmokeCompleted = $false
$packageCompleted = $false
$signingQualified = $false
$packageZipSha256 = ""
$normalizedSignerThumbprint = ""
$pythonExe = ""

function Test-PythonInterpreter {
    param([Parameter(Mandatory = $true)][string]$Candidate)
    try {
        & $Candidate -c "import sys; raise SystemExit(0 if sys.version_info.major == 3 else 1)" *> $null
        return $LASTEXITCODE -eq 0
    }
    catch {
        return $false
    }
}

function Resolve-PythonInterpreter {
    param([string]$RequestedPath)

    if (-not [string]::IsNullOrWhiteSpace($RequestedPath)) {
        $resolved = [IO.Path]::GetFullPath($RequestedPath)
        if (-not (Test-Path -LiteralPath $resolved -PathType Leaf) -or -not (Test-PythonInterpreter $resolved)) {
            throw "-PythonPath must point to a working Python 3 interpreter: $resolved"
        }
        return $resolved
    }

    $candidates = New-Object System.Collections.Generic.List[string]
    if (-not [string]::IsNullOrWhiteSpace($env:QS3D_PYTHON)) { $candidates.Add($env:QS3D_PYTHON) }
    foreach ($name in @("python.exe", "python3.exe", "py.exe")) {
        $command = Get-Command $name -CommandType Application -ErrorAction SilentlyContinue
        if ($null -ne $command -and -not [string]::IsNullOrWhiteSpace($command.Source)) { $candidates.Add($command.Source) }
    }
    if (-not [string]::IsNullOrWhiteSpace($env:USERPROFILE)) {
        $candidates.Add((Join-Path $env:USERPROFILE ".cache\codex-runtimes\codex-primary-runtime\dependencies\python\python.exe"))
    }
    if (-not [string]::IsNullOrWhiteSpace($env:LOCALAPPDATA)) {
        $programsRoot = Join-Path $env:LOCALAPPDATA "Programs\Python"
        if (Test-Path -LiteralPath $programsRoot -PathType Container) {
            foreach ($candidate in Get-ChildItem -LiteralPath $programsRoot -Recurse -Filter python.exe -File -ErrorAction SilentlyContinue) {
                $candidates.Add($candidate.FullName)
            }
        }
    }

    foreach ($candidate in $candidates | Select-Object -Unique) {
        if ([string]::IsNullOrWhiteSpace($candidate)) { continue }
        if ((Test-Path -LiteralPath $candidate -PathType Leaf) -and (Test-PythonInterpreter $candidate)) { return $candidate }
    }
    throw "A working Python 3 interpreter was not found. Install Python, set QS3D_PYTHON, or pass -PythonPath."
}

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

function Invoke-PythonChecked {
    param([Parameter(Mandatory = $true)][string[]]$Arguments)
    $previousPythonIoEncoding = $env:PYTHONIOENCODING
    try {
        $env:PYTHONIOENCODING = "utf-8"
        Invoke-ExternalChecked $pythonExe $Arguments
    }
    finally {
        $env:PYTHONIOENCODING = $previousPythonIoEncoding
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
    if ($SignPackage -and -not $Package) {
        throw "-SignPackage requires -Package so the exact packaged payload is signed and finalized."
    }
    if ($SignPackage -and $SkipRuntime) {
        throw "-SignPackage cannot be combined with -SkipRuntime. Signed release qualification requires the real licensed V25 runtime gate."
    }
    if ($SignPackage) {
        if ([string]::IsNullOrWhiteSpace($SigningCertThumbprint)) {
            throw "-SignPackage requires -SigningCertThumbprint with the 40-hex certificate thumbprint from Cert:\CurrentUser\My."
        }
        if ([string]::IsNullOrWhiteSpace($TimestampUrl)) {
            throw "-SignPackage requires -TimestampUrl using HTTPS."
        }
        $normalizedSignerThumbprint = $SigningCertThumbprint.Replace(' ', '').ToUpperInvariant()
        if ($normalizedSignerThumbprint -notmatch '^[0-9A-F]{40}$') {
            throw "Signing certificate thumbprint must contain exactly 40 hexadecimal characters."
        }
        if ($TimestampUrl -notmatch '^https://') {
            throw "Timestamp URL must use HTTPS."
        }
    }

    foreach ($command in @("git", "dotnet")) {
        if (-not (Get-Command $command -ErrorAction SilentlyContinue)) {
            throw "Required command is not available on PATH: $command"
        }
    }
    $pythonExe = Resolve-PythonInterpreter $PythonPath
    Write-Host "Python 3: $pythonExe"
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
            Invoke-PythonChecked @("scripts/preflight-ci-manual-only.py")
        }
        Invoke-QualificationStep "Generic source preflight" {
            Invoke-PythonChecked @("scripts/preflight.py")
        }
        Invoke-QualificationStep "Aggregate feature preflights" {
            Invoke-PythonChecked @("scripts/preflight-all.py")
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
            $sourceBuildCompleted = $true
        }
        finally {
            $env:BRICSCAD_V25_DIR = $oldBricsCadDir
        }

        Invoke-QualificationStep "Offline WPF theme / Workspace / RightPanel smoke" {
            & (Join-Path $PSScriptRoot "run-local-v25-wpf-smoke.ps1") `
                -BricsCadDir $BricsCadDir `
                -PluginPath $pluginDll
        }
        $wpfSmokeCompleted = $true

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
            $runtimeSmokeCompleted = $true
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
                    $env:RELEASE_TAG = $ReleaseTag
                    & (Join-Path $PSScriptRoot "package-v25.ps1")
                }
                finally {
                    $env:RELEASE_TAG = $oldReleaseTag
                }
                if (-not (Test-Path -LiteralPath $packageDir -PathType Container)) {
                    throw "Local package step completed without the expected package directory: $packageDir"
                }
                if (-not (Test-Path -LiteralPath $packageZip -PathType Leaf)) {
                    throw "Local package step completed without the expected ZIP: $packageZip"
                }
            }
            $packageCompleted = $true
        }

        if ($SignPackage) {
            $signedPayload = @(
                (Join-Path $packageDir "QS3D.BricsCAD.V25.dll"),
                (Join-Path $packageDir "QS3D.Core.dll"),
                (Join-Path $packageDir "install-v25-autoload.ps1"),
                (Join-Path $packageDir "uninstall-v25-autoload.ps1"),
                (Join-Path $packageDir "update-v25.ps1")
            )
            Invoke-QualificationStep "Authenticode sign packaged executable payload" {
                foreach ($path in $signedPayload) {
                    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
                        throw "Expected signable package payload is missing: $path"
                    }
                }
                & (Join-Path $PSScriptRoot "sign-v25.ps1") `
                    -Path $signedPayload `
                    -CertificateThumbprint $normalizedSignerThumbprint `
                    -TimestampServer $TimestampUrl `
                    -Confirm:$false
            }
            Invoke-QualificationStep "Verify Authenticode signer and trusted timestamp" {
                & (Join-Path $PSScriptRoot "verify-v25-signatures.ps1") `
                    -Path $signedPayload `
                    -ExpectedThumbprint $normalizedSignerThumbprint
            }
            Invoke-QualificationStep "Finalize signed package metadata / hashes / ZIP" {
                & (Join-Path $PSScriptRoot "finalize-v25-signed-package.ps1") `
                    -PackageDirectory $packageDir `
                    -PackageZip $packageZip `
                    -ExpectedSignerThumbprint $normalizedSignerThumbprint `
                    -Confirm:$false
                if (-not (Test-Path -LiteralPath $packageZip -PathType Leaf)) {
                    throw "Signed package finalization did not produce the expected ZIP: $packageZip"
                }
                $script:packageZipSha256 = (Get-FileHash -LiteralPath $packageZip -Algorithm SHA256).Hash.ToLowerInvariant()
            }
            $signingQualified = $true
        }
        elseif ($Package -and (Test-Path -LiteralPath $packageZip -PathType Leaf)) {
            $packageZipSha256 = (Get-FileHash -LiteralPath $packageZip -Algorithm SHA256).Hash.ToLowerInvariant()
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
    $automatedGateStatus = if ($null -eq $fatal) { "PASS" } else { "FAIL" }
    $sourceBuildStatus = if ($sourceBuildCompleted) { "PASS" } else { "FAIL_OR_INCOMPLETE" }
    $wpfSmokeStatus = if ($wpfSmokeCompleted) { "PASS" } else { "FAIL_OR_INCOMPLETE" }
    $runtimeSmokeStatus = if ($SkipRuntime) { "NOT_RUN" } elseif ($runtimeSmokeCompleted) { "PASS" } else { "FAIL_OR_INCOMPLETE" }
    $packageStatus = if (-not $Package) { "NOT_REQUESTED" } elseif ($packageCompleted) { "PASS" } else { "FAIL_OR_INCOMPLETE" }
    $signingStatus = if (-not $SignPackage) { "NOT_REQUESTED" } elseif ($signingQualified) { "PASS" } else { "FAIL_OR_INCOMPLETE" }
    $qualificationScope = if ($signingQualified) { "source-build+runtime-smoke+package+authenticode" } elseif ($runtimeSmokeCompleted -and $packageCompleted) { "source-build+runtime-smoke+package" } elseif ($runtimeSmokeCompleted) { "source-build+runtime-smoke" } elseif ($sourceBuildCompleted) { "source-build" } else { "incomplete" }
    $report = [ordered]@{
        schema = 3
        status = $automatedGateStatus
        automatedGateStatus = $automatedGateStatus
        sourceBuildStatus = $sourceBuildStatus
        wpfSmokeStatus = $wpfSmokeStatus
        runtimeSmokeStatus = $runtimeSmokeStatus
        packageStatus = $packageStatus
        signingStatus = $signingStatus
        fullInteractiveMatrixStatus = "NOT_RUN"
        customerReleaseQualified = $false
        qualificationScope = $qualificationScope
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
        packageQualified = [bool]$packageCompleted
        releaseTag = $ReleaseTag
        packageZip = if ($packageCompleted) { $packageZip } else { "" }
        packageZipSha256 = $packageZipSha256
        signingRequested = [bool]$SignPackage
        signingQualified = [bool]$signingQualified
        signerThumbprint = if ($SignPackage) { $normalizedSignerThumbprint } else { "" }
        timestampUrl = if ($SignPackage) { $TimestampUrl } else { "" }
        manualScenarioChecklist = "docs/LOCAL-V25-QUALIFICATION.md"
        steps = $steps.ToArray()
        error = if ($null -eq $fatal) { "" } else { $fatal.Exception.Message }
    }
    $report | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $reportPath -Encoding UTF8
    Write-Host ""
    Write-Host "Qualification evidence report: $reportPath"
}

if ($null -ne $fatal) {
    throw $fatal
}

Write-Host ""
if ($SkipRuntime) {
    Write-Host "AUTOMATED SOURCE/BUILD + OFFLINE WPF GATES PASS for exact SHA $headSha; licensed V25 runtime smoke NOT RUN."
}
elseif ($signingQualified) {
    Write-Host "AUTOMATED SOURCE/BUILD + OFFLINE WPF + LICENSED V25 RUNTIME + SIGNED/FINALIZED PACKAGE GATES PASS for exact SHA $headSha."
}
else {
    Write-Host "AUTOMATED SOURCE/BUILD + OFFLINE WPF + LICENSED V25 NETLOAD/RIBBON/PALETTE SMOKE PASS for exact SHA $headSha."
}
Write-Host "FULL INTERACTIVE/PRIVATE-DWG PRODUCT MATRIX: NOT RUN by this script."
Write-Host "This does not replace the manual/private-DWG scenario checklist in docs/LOCAL-V25-QUALIFICATION.md."
Write-Host "Customer release qualification remains false until docs/LOCAL-V25-QUALIFICATION.md is executed and recorded for the same SHA/package."
