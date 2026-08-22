param(
    [Parameter(Mandatory = $true)][string]$BricsCadDir,
    [Parameter(Mandatory = $true)][string]$PluginDll,
    [Parameter(Mandatory = $true)][string]$DrawingCopy,
    [Parameter(Mandatory = $true)][string]$Profile,
    [Parameter(Mandatory = $true)][string]$ArtifactDir,
    [Parameter(Mandatory = $true)][ValidatePattern("^[0-9a-fA-F]{40}$")][string]$ExpectedSourceSha,
    [Parameter(Mandatory = $true)][switch]$ConfirmDisposableCopy,
    [ValidateRange(30, 900)][int]$StartupTimeoutSeconds = 240
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$windowInteropPath = Join-Path $PSScriptRoot "bricscad-runner-window-interop.ps1"
if (-not (Test-Path -LiteralPath $windowInteropPath -PathType Leaf)) {
    throw "Level Z runner window interop helper is missing: $windowInteropPath"
}
. $windowInteropPath

function Read-Qs3dLevelMarker {
    param([Parameter(Mandatory = $true)][string]$Path)
    $marker = @{}
    foreach ($line in Get-Content -LiteralPath $Path) {
        if ([string]::IsNullOrWhiteSpace($line)) { continue }
        $separator = $line.IndexOf('=')
        if ($separator -le 0) { throw "Malformed Level Z marker line: $line" }
        $key = $line.Substring(0, $separator).Trim()
        $value = $line.Substring($separator + 1).Trim()
        if ($marker.ContainsKey($key)) { throw "Duplicate Level Z marker key: $key" }
        $marker[$key] = $value
    }
    return $marker
}

function Require-Qs3dLevelValue {
    param(
        [Parameter(Mandatory = $true)]$Marker,
        [Parameter(Mandatory = $true)][string]$Key,
        [Parameter(Mandatory = $true)][string]$Expected
    )
    if (-not $Marker.ContainsKey($Key)) { throw "Level Z marker is missing '$Key'." }
    if (-not [string]::Equals([string]$Marker[$Key], $Expected, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Level Z marker '$Key' expected '$Expected' but was '$($Marker[$Key])'."
    }
}

function Require-Qs3dLevelNumber {
    param(
        [Parameter(Mandatory = $true)]$Marker,
        [Parameter(Mandatory = $true)][string]$Key,
        [Parameter(Mandatory = $true)][double]$Expected
    )
    if (-not $Marker.ContainsKey($Key)) { throw "Level Z marker is missing '$Key'." }
    [double]$actual = 0
    if (-not [double]::TryParse([string]$Marker[$Key], [Globalization.NumberStyles]::Float, [Globalization.CultureInfo]::InvariantCulture, [ref]$actual) -or
        [double]::IsNaN($actual) -or [double]::IsInfinity($actual)) {
        throw "Level Z marker '$Key' is not a finite invariant number."
    }
    $tolerance = [Math]::Max(0.0000001, [Math]::Max([Math]::Abs($Expected), [Math]::Abs($actual)) * 0.0000001)
    if ([Math]::Abs($Expected - $actual) -gt $tolerance) {
        throw "Level Z marker '$Key' expected '$Expected' but was '$actual'."
    }
}

function Read-PositiveLevelInt {
    param([Parameter(Mandatory = $true)]$Marker, [Parameter(Mandatory = $true)][string]$Key)
    if (-not $Marker.ContainsKey($Key)) { throw "Level Z marker is missing '$Key'." }
    [int]$value = 0
    if (-not [int]::TryParse([string]$Marker[$Key], [Globalization.NumberStyles]::None, [Globalization.CultureInfo]::InvariantCulture, [ref]$value) -or $value -le 0) {
        throw "Level Z marker '$Key' is not a positive invariant integer."
    }
    return $value
}

function Read-NonNegativeLevelInt {
    param([Parameter(Mandatory = $true)]$Marker, [Parameter(Mandatory = $true)][string]$Key)
    if (-not $Marker.ContainsKey($Key)) { return }
    [int]$value = 0
    if (-not [int]::TryParse([string]$Marker[$Key], [Globalization.NumberStyles]::None, [Globalization.CultureInfo]::InvariantCulture, [ref]$value) -or $value -lt 0) {
        throw "Level Z sanitized failure count '$Key' is invalid."
    }
}

function Require-Qs3dLevelFailure {
    param([Parameter(Mandatory = $true)]$Marker)
    Require-Qs3dLevelValue -Marker $Marker -Key "command" -Expected "QS3DLEVELZPROBE"
    $allowedCodes = @(
        "LEVEL_Z_RUNTIME_CONTEXT_FAILED", "LEVEL_Z_RUNTIME_SOURCE_FAILED", "LEVEL_Z_RUNTIME_HOST_BUILD_FAILED",
        "LEVEL_Z_RUNTIME_OPENING_FAILED", "LEVEL_Z_RUNTIME_CURTAIN_FRAME_BUILD_FAILED",
        "LEVEL_Z_RUNTIME_CURTAIN_PANEL_BUILD_FAILED", "LEVEL_Z_RUNTIME_CURTAIN_RANGE_FAILED",
        "LEVEL_Z_RUNTIME_CURTAIN_MODE_FAILED", "LEVEL_Z_RUNTIME_REBAR_FAILED",
        "LEVEL_Z_RUNTIME_LEVEL_EDIT_FAILED", "LEVEL_Z_RUNTIME_MARKER_FAILED"
    )
    $failureCode = [string]$Marker["error_code"]
    if (-not ($allowedCodes -contains $failureCode)) { throw "Level Z marker has an invalid sanitized failure code." }
    if ($failureCode -ne "LEVEL_Z_RUNTIME_REBAR_FAILED") { throw "Level Z runtime probe reported sanitized failure '$failureCode'." }

    $allowedStages = @(
        "longitudinal_build", "longitudinal_count", "stirrup_build", "stirrup_count",
        "longitudinal_range_read", "stirrup_range_read", "longitudinal_containment",
        "stirrup_containment", "complete"
    )
    if (-not $Marker.ContainsKey("rebar_stage") -or -not ($allowedStages -contains [string]$Marker["rebar_stage"])) {
        throw "Level Z marker has an invalid sanitized rebar stage."
    }
    foreach ($key in @("observed_beam_rebar_count", "observed_beam_stirrup_element_count", "observed_beam_stirrup_count")) {
        Read-NonNegativeLevelInt -Marker $Marker -Key $key
    }
    foreach ($key in @("observed_rebar_min_z_m", "observed_rebar_max_z_m", "observed_stirrup_min_z_m", "observed_stirrup_max_z_m")) {
        if ($Marker.ContainsKey($key)) {
            [double]$number = 0
            if (-not [double]::TryParse([string]$Marker[$key], [Globalization.NumberStyles]::Float, [Globalization.CultureInfo]::InvariantCulture, [ref]$number) -or
                [double]::IsNaN($number) -or [double]::IsInfinity($number)) {
                throw "Level Z marker contains an invalid sanitized Z range."
            }
        }
    }
    foreach ($key in @("exception_type", "exception_target", "exception_hresult")) {
        if (-not $Marker.ContainsKey($key)) { throw "Level Z marker is missing sanitized exception classification." }
    }
    if ([string]$Marker["exception_type"] -notmatch '^[A-Za-z0-9_.+`]+$' -or
        [string]$Marker["exception_target"] -notmatch '^[A-Za-z0-9_.<>+`]*$' -or
        [string]$Marker["exception_hresult"] -notmatch '^0x[0-9A-F]{8}$') {
        throw "Level Z marker contains an invalid sanitized exception classification."
    }
    throw "Level Z runtime probe reported sanitized rebar failure at stage '$([string]$Marker["rebar_stage"])'."
}

function Restore-EnvironmentValue {
    param([Parameter(Mandatory = $true)][string]$Name, [AllowNull()][string]$Value)
    if ($null -eq $Value) { Remove-Item -LiteralPath ("Env:" + $Name) -ErrorAction SilentlyContinue }
    else { Set-Item -LiteralPath ("Env:" + $Name) -Value $Value }
}

function Stop-Qs3dLevelProcess {
    param([AllowNull()][Diagnostics.Process]$Process)
    if ($null -eq $Process) { return }
    try {
        $Process.Refresh()
        if (-not $Process.HasExited) {
            Stop-Process -Id $Process.Id -Force -ErrorAction SilentlyContinue
            $Process.WaitForExit(10000) | Out-Null
        }
    }
    catch { }
}

function Restore-Qs3dLevelDrawingAndPrivateState {
    param(
        [Parameter(Mandatory = $true)][string]$scriptPath,
        [Parameter(Mandatory = $true)][string]$projectSidecar,
        [Parameter(Mandatory = $true)][string]$DrawingCopy,
        [Parameter(Mandatory = $true)][string]$drawingBackupPath
    )

    if (Test-Path -LiteralPath $scriptPath -PathType Leaf) {
        Remove-Item -LiteralPath $scriptPath -Force -ErrorAction SilentlyContinue
    }
    foreach ($privatePath in @(
        $projectSidecar,
        ($projectSidecar + ".bak"),
        [IO.Path]::ChangeExtension($DrawingCopy, ".bak"),
        [IO.Path]::ChangeExtension($DrawingCopy, ".dwl"),
        [IO.Path]::ChangeExtension($DrawingCopy, ".dwl2")
    )) {
        if (Test-Path -LiteralPath $privatePath -PathType Leaf) {
            Remove-Item -LiteralPath $privatePath -Force -ErrorAction SilentlyContinue
        }
    }
    if (Test-Path -LiteralPath $drawingBackupPath -PathType Leaf) {
        Copy-Item -LiteralPath $drawingBackupPath -Destination $DrawingCopy -Force -ErrorAction Stop
        Remove-Item -LiteralPath $drawingBackupPath -Force -ErrorAction SilentlyContinue
    }
}

if ([Environment]::OSVersion.Platform -ne [PlatformID]::Win32NT) { throw "Level Z runtime qualification requires Windows." }
if (-not [Environment]::UserInteractive) { throw "Level Z runtime qualification requires an interactive Windows session." }
if (-not $ConfirmDisposableCopy) { throw "Pass -ConfirmDisposableCopy only for a disposable synthetic drawing copy." }

$BricsCadDir = [IO.Path]::GetFullPath($BricsCadDir)
$PluginDll = [IO.Path]::GetFullPath($PluginDll)
$DrawingCopy = [IO.Path]::GetFullPath($DrawingCopy)
$ArtifactDir = [IO.Path]::GetFullPath($ArtifactDir)
$repoRoot = [IO.Path]::GetFullPath((Split-Path -Parent $PSScriptRoot))
$sourceShaOutput = @(& git -C $repoRoot rev-parse --verify HEAD 2>$null)
$sourceShaExitCode = $LASTEXITCODE
$resolvedSourceSha = if ($sourceShaOutput.Count -eq 1) { [string]$sourceShaOutput[0] } else { "" }
$resolvedSourceSha = $resolvedSourceSha.Trim()
if ($sourceShaExitCode -ne 0 -or
    $sourceShaOutput.Count -ne 1 -or
    $resolvedSourceSha -notmatch "^[0-9a-fA-F]{40}$" -or
    -not [string]::Equals($resolvedSourceSha, $ExpectedSourceSha, [StringComparison]::OrdinalIgnoreCase)) {
    throw "ExpectedSourceSha does not match the current repository HEAD. Expected=$ExpectedSourceSha Actual=$resolvedSourceSha"
}
$worktreeStatus = @(& git -C $repoRoot status --porcelain=v1 --untracked-files=all 2>$null)
$worktreeExitCode = $LASTEXITCODE
if ($worktreeExitCode -ne 0) { throw "Unable to verify the repository worktree for exact-SHA Level Z qualification." }
if ($worktreeStatus.Count -gt 0) { throw "Exact-SHA Level Z qualification requires a clean repository worktree." }
$ExpectedSourceSha = $resolvedSourceSha.ToLowerInvariant()
if (-not [IO.Path]::GetFileName($DrawingCopy).EndsWith(".level-z-probe-copy.dwg", [StringComparison]::OrdinalIgnoreCase)) {
    throw "DrawingCopy must use the guarded '*.level-z-probe-copy.dwg' suffix."
}

$bricscadExe = Join-Path $BricsCadDir "bricscad.exe"
$coreDll = Join-Path (Split-Path -Parent $PluginDll) "QS3D.Core.dll"
foreach ($required in @($bricscadExe, $PluginDll, $coreDll, $DrawingCopy)) {
    if (-not (Test-Path -LiteralPath $required -PathType Leaf)) { throw "Required Level Z runtime input is missing: $required" }
}
$expectedAssemblyRevision = "+" + $ExpectedSourceSha
foreach ($assemblyPath in @($PluginDll, $coreDll)) {
    $productVersion = [string](Get-Item -LiteralPath $assemblyPath).VersionInfo.ProductVersion
    if (-not $productVersion.EndsWith($expectedAssemblyRevision, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Assembly was not built from ExpectedSourceSha: $assemblyPath"
    }
}
if (@(Get-Process -Name "bricscad" -ErrorAction SilentlyContinue).Count -gt 0) {
    throw "Close existing BricsCAD processes before starting the isolated Level Z runtime probe."
}

$projectSidecar = [IO.Path]::ChangeExtension($DrawingCopy, ".qsdb")
if ((Test-Path -LiteralPath $projectSidecar) -or (Test-Path -LiteralPath ($projectSidecar + ".bak"))) {
    throw "The disposable Level Z drawing copy must not have a pre-existing QS3D sidecar."
}

New-Item -ItemType Directory -Force -Path $ArtifactDir | Out-Null
$resultPath = Join-Path $ArtifactDir "level-z-runtime-result.txt"
$scriptPath = Join-Path $ArtifactDir "level-z-runtime.scr"
$metadataPath = Join-Path $ArtifactDir "level-z-runtime-metadata.json"
$drawingBackupPath = Join-Path $ArtifactDir "level-z-original.dwg"
foreach ($output in @($resultPath, $scriptPath, $metadataPath, $drawingBackupPath)) {
    if (Test-Path -LiteralPath $output) { throw "Level Z runtime output must not already exist: $output" }
}

$drawingHashBefore = (Get-FileHash -LiteralPath $DrawingCopy -Algorithm SHA256).Hash.ToUpperInvariant()
Copy-Item -LiteralPath $DrawingCopy -Destination $drawingBackupPath -ErrorAction Stop
$pluginHash = (Get-FileHash -LiteralPath $PluginDll -Algorithm SHA256).Hash.ToUpperInvariant()
$nonce = [Guid]::NewGuid().ToString("N")
$oldResult = [Environment]::GetEnvironmentVariable("QS3D_LEVEL_Z_RESULT", "Process")
$oldNonce = [Environment]::GetEnvironmentVariable("QS3D_LEVEL_Z_NONCE", "Process")
$oldSourceSha = [Environment]::GetEnvironmentVariable("QS3D_LEVEL_Z_SOURCE_SHA", "Process")
$process = $null
$proxyInformationDialogsDismissed = 0
$gracefulExit = $false
$startedAt = Get-Date

try {
    $env:QS3D_LEVEL_Z_RESULT = $resultPath
    $env:QS3D_LEVEL_Z_NONCE = $nonce
    $env:QS3D_LEVEL_Z_SOURCE_SHA = $ExpectedSourceSha
    $script = @(
        "FILEDIA", "0",
        "CMDECHO", "1",
        "TILEMODE", "1",
        "INSUNITS", "4",
        "UCS", "W",
        "NETLOAD", ('"' + $PluginDll + '"'),
        "QS3DLEVELZPROBE",
        "_.QUIT", "_N"
    )
    Set-Content -LiteralPath $scriptPath -Value $script -Encoding ASCII

    $argumentParts = New-Object System.Collections.Generic.List[string]
    $argumentParts.Add('"' + $DrawingCopy + '"')
    $argumentParts.Add('/P')
    $argumentParts.Add('"' + $Profile + '"')
    $argumentParts.Add('/B')
    $argumentParts.Add('"' + $scriptPath + '"')
    $process = Start-Process -FilePath $bricscadExe -ArgumentList ([string]::Join(' ', $argumentParts)) -PassThru -WindowStyle Hidden -WorkingDirectory $ArtifactDir

    $deadline = (Get-Date).AddSeconds($StartupTimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        if (Test-Path -LiteralPath $resultPath -PathType Leaf) { break }
        $proxyInformationDialogsDismissed += Close-Qs3dProxyInformationDialog -Process $process
        $process.Refresh()
        if ($process.HasExited) { throw "BricsCAD exited before the Level Z marker. ExitCode=$($process.ExitCode)" }
        Start-Sleep -Milliseconds 500
    }
    if (-not (Test-Path -LiteralPath $resultPath -PathType Leaf)) {
        throw "Timed out waiting for QS3DLEVELZPROBE after $StartupTimeoutSeconds seconds."
    }

    $process.Refresh()
    if (-not $process.HasExited) {
        $gracefulExit = $process.WaitForExit(15000)
    }
    else {
        $gracefulExit = $true
    }

    $marker = Read-Qs3dLevelMarker -Path $resultPath
    if ([string]::Equals([string]$marker["status"], "FAIL", [StringComparison]::OrdinalIgnoreCase)) {
        Require-Qs3dLevelFailure -Marker $marker
    }
    Require-Qs3dLevelValue -Marker $marker -Key "status" -Expected "PASS"
    Require-Qs3dLevelValue -Marker $marker -Key "command" -Expected "QS3DLEVELZPROBE"
    Require-Qs3dLevelValue -Marker $marker -Key "process" -Expected "bricscad"
    Require-Qs3dLevelValue -Marker $marker -Key "nonce" -Expected $nonce
    Require-Qs3dLevelValue -Marker $marker -Key "source_sha" -Expected $ExpectedSourceSha
    Require-Qs3dLevelValue -Marker $marker -Key "schema" -Expected "QS3D_LEVEL_Z_RUNTIME_V1"
    Require-Qs3dLevelValue -Marker $marker -Key "is_64bit" -Expected "true"
    Require-Qs3dLevelValue -Marker $marker -Key "physical_opening_volume_reduced" -Expected "true"
    Require-Qs3dLevelValue -Marker $marker -Key "level_edit_invalidation" -Expected "true"
    Require-Qs3dLevelValue -Marker $marker -Key "top_only_fail_closed" -Expected "true"
    Require-Qs3dLevelValue -Marker $marker -Key "level_health_issue_count_before_edit" -Expected "0"
    Require-Qs3dLevelNumber -Marker $marker -Key "legacy_wall_bottom_m" -Expected 1.2
    Require-Qs3dLevelNumber -Marker $marker -Key "legacy_wall_top_m" -Expected 3.7
    Require-Qs3dLevelNumber -Marker $marker -Key "bounded_wall_bottom_m" -Expected 3.1
    Require-Qs3dLevelNumber -Marker $marker -Key "bounded_wall_top_m" -Expected 6.8
    Require-Qs3dLevelNumber -Marker $marker -Key "bottom_beam_bottom_m" -Expected 3.25
    Require-Qs3dLevelNumber -Marker $marker -Key "bottom_beam_top_m" -Expected 3.85
    if (-not $gracefulExit) { throw "BricsCAD did not exit gracefully after the Level Z marker." }

    $frameCount = Read-PositiveLevelInt -Marker $marker -Key "curtain_frame_count"
    $panelCount = Read-PositiveLevelInt -Marker $marker -Key "curtain_panel_count"
    $rebarCount = Read-PositiveLevelInt -Marker $marker -Key "beam_rebar_count"
    $stirrupCount = Read-PositiveLevelInt -Marker $marker -Key "beam_stirrup_count"
    $staleCount = Read-PositiveLevelInt -Marker $marker -Key "stale_snapshot_count_after_edit"
    if ($rebarCount -ne 4) { throw "Level Z runtime expected exactly four Beam longitudinal bars." }

    Stop-Qs3dLevelProcess -Process $process
    Restore-Qs3dLevelDrawingAndPrivateState -ScriptPath $scriptPath -ProjectSidecar $projectSidecar -DrawingCopy $DrawingCopy -DrawingBackupPath $drawingBackupPath
    $processCleanupVerified = @(Get-Process -Name "bricscad" -ErrorAction SilentlyContinue).Count -eq 0
    if (-not $processCleanupVerified) { throw "Level Z runtime left a BricsCAD process after cleanup." }
    $scriptCleanupVerified = -not (Test-Path -LiteralPath $scriptPath)
    if (-not $scriptCleanupVerified) { throw "Level Z runtime left its private script after cleanup." }
    $privateStateCleanupVerified = -not (
        (Test-Path -LiteralPath $projectSidecar) -or
        (Test-Path -LiteralPath ($projectSidecar + ".bak")) -or
        (Test-Path -LiteralPath ([IO.Path]::ChangeExtension($DrawingCopy, ".bak"))) -or
        (Test-Path -LiteralPath ([IO.Path]::ChangeExtension($DrawingCopy, ".dwl"))) -or
        (Test-Path -LiteralPath ([IO.Path]::ChangeExtension($DrawingCopy, ".dwl2")))
    )
    if (-not $privateStateCleanupVerified) { throw "Level Z runtime left private drawing state after cleanup." }
    $drawingHashAfter = (Get-FileHash -LiteralPath $DrawingCopy -Algorithm SHA256).Hash.ToUpperInvariant()
    $drawingRestoreVerified = [string]::Equals($drawingHashBefore, $drawingHashAfter, [StringComparison]::Ordinal)
    if (-not $drawingRestoreVerified) {
        throw "The disposable Level Z drawing was written unexpectedly."
    }

    $metadata = [ordered]@{
        status = "PASS"
        source_sha = $ExpectedSourceSha
        started_at = $startedAt.ToUniversalTime().ToString("O")
        completed_at = (Get-Date).ToUniversalTime().ToString("O")
        profile = $Profile
        bricscad_file_version = (Get-Item -LiteralPath $bricscadExe).VersionInfo.FileVersion
        plugin_sha256 = $pluginHash
        drawing_copy_sha256_before = $drawingHashBefore
        drawing_copy_sha256_after = $drawingHashAfter
        proxy_information_dialogs_dismissed = $proxyInformationDialogsDismissed
        graceful_exit = $gracefulExit
        process_cleanup_verified = $processCleanupVerified
        script_cleanup_verified = $scriptCleanupVerified
        private_state_cleanup_verified = $privateStateCleanupVerified
        drawing_restore_verified = $drawingRestoreVerified
        curtain_frame_count = $frameCount
        curtain_panel_count = $panelCount
        beam_rebar_count = $rebarCount
        beam_stirrup_count = $stirrupCount
        stale_snapshot_count_after_edit = $staleCount
        marker = $marker
    }
    $metadata | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $metadataPath -Encoding UTF8

    Write-Host "QS3D BricsCAD V25 Level Z runtime PASS"
    Write-Host "Curtain frames: $frameCount; panels: $panelCount; Beam bars: $rebarCount; stirrups: $stirrupCount"
    Write-Host "Marker: $resultPath"
    Write-Host "Metadata: $metadataPath"
}
finally {
    Stop-Qs3dLevelProcess -Process $process
    Restore-Qs3dLevelDrawingAndPrivateState -ScriptPath $scriptPath -ProjectSidecar $projectSidecar -DrawingCopy $DrawingCopy -DrawingBackupPath $drawingBackupPath
    Restore-EnvironmentValue -Name "QS3D_LEVEL_Z_RESULT" -Value $oldResult
    Restore-EnvironmentValue -Name "QS3D_LEVEL_Z_NONCE" -Value $oldNonce
    Restore-EnvironmentValue -Name "QS3D_LEVEL_Z_SOURCE_SHA" -Value $oldSourceSha
}
