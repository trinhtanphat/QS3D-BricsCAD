param(
    [Parameter(Mandatory = $true)][string]$BricsCadDir,
    [Parameter(Mandatory = $true)][string]$PluginDll,
    [Parameter(Mandatory = $true)][string]$DrawingCopy,
    [Parameter(Mandatory = $true)][string]$Profile,
    [Parameter(Mandatory = $true)][string]$ArtifactDir,
    [Parameter(Mandatory = $true)][switch]$ConfirmDisposableCopy,
    [ValidateRange(60, 1200)][int]$StartupTimeoutSeconds = 360
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$windowInteropPath = Join-Path $PSScriptRoot "bricscad-runner-window-interop.ps1"
if (-not (Test-Path -LiteralPath $windowInteropPath -PathType Leaf)) {
    throw "Curtain P11 runner window interop helper is missing."
}
. $windowInteropPath

function Read-Qs3dMarker {
    param([Parameter(Mandatory = $true)][string]$Path)
    $marker = @{}
    foreach ($line in Get-Content -LiteralPath $Path) {
        if ([string]::IsNullOrWhiteSpace($line)) { continue }
        $separator = $line.IndexOf('=')
        if ($separator -le 0) { throw "Malformed Curtain P11 marker line." }
        $key = $line.Substring(0, $separator).Trim()
        $value = $line.Substring($separator + 1).Trim()
        if ($marker.ContainsKey($key)) { throw "Duplicate Curtain P11 marker key: $key" }
        $marker[$key] = $value
    }
    return $marker
}

function Require-Qs3dValue {
    param(
        [Parameter(Mandatory = $true)]$Marker,
        [Parameter(Mandatory = $true)][string]$Key,
        [Parameter(Mandatory = $true)][string]$Expected
    )
    if (-not $Marker.ContainsKey($Key)) { throw "Curtain P11 marker is missing '$Key'." }
    if (-not [string]::Equals([string]$Marker[$Key], $Expected, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Curtain P11 marker '$Key' did not match its expected value."
    }
}

function Read-PositiveMarkerInt {
    param([Parameter(Mandatory = $true)]$Marker, [Parameter(Mandatory = $true)][string]$Key)
    if (-not $Marker.ContainsKey($Key)) { throw "Curtain P11 marker is missing '$Key'." }
    [int]$value = 0
    if (-not [int]::TryParse([string]$Marker[$Key], [Globalization.NumberStyles]::None, [Globalization.CultureInfo]::InvariantCulture, [ref]$value) -or $value -le 0) {
        throw "Curtain P11 marker '$Key' is not a positive invariant integer."
    }
    return $value
}

function Read-NonNegativeMarkerLong {
    param([Parameter(Mandatory = $true)]$Marker, [Parameter(Mandatory = $true)][string]$Key)
    if (-not $Marker.ContainsKey($Key)) { throw "Curtain P11 marker is missing '$Key'." }
    [long]$value = 0
    if (-not [long]::TryParse([string]$Marker[$Key], [Globalization.NumberStyles]::None, [Globalization.CultureInfo]::InvariantCulture, [ref]$value) -or $value -lt 0) {
        throw "Curtain P11 marker '$Key' is not a non-negative invariant integer."
    }
    return $value
}

function Restore-EnvironmentValue {
    param([Parameter(Mandatory = $true)][string]$Name, [AllowNull()][string]$Value)
    if ($null -eq $Value) { Remove-Item -LiteralPath ("Env:" + $Name) -ErrorAction SilentlyContinue }
    else { Set-Item -LiteralPath ("Env:" + $Name) -Value $Value }
}

function Stop-Qs3dLaunchedProcess {
    param([AllowNull()][Diagnostics.Process]$Process)
    if ($null -eq $Process) { return }
    $Process.Refresh()
    if (-not $Process.HasExited) {
        Stop-Process -Id $Process.Id -Force -ErrorAction Stop
        $Process.WaitForExit(15000) | Out-Null
        $Process.Refresh()
    }
    if (-not $Process.HasExited) { throw "Launched Curtain P11 BricsCAD process did not exit." }
}

function Wait-Qs3dMarker {
    param(
        [Parameter(Mandatory = $true)][Diagnostics.Process]$Process,
        [Parameter(Mandatory = $true)][string]$ExpectedPath,
        [Parameter(Mandatory = $true)][string]$FailurePath,
        [Parameter(Mandatory = $true)][DateTime]$Deadline
    )
    $dismissed = 0
    while ((Get-Date) -lt $Deadline) {
        if (Test-Path -LiteralPath $ExpectedPath -PathType Leaf) { return $dismissed }
        if (Test-Path -LiteralPath $FailurePath -PathType Leaf) { return $dismissed }
        $dismissed += Close-Qs3dProxyInformationDialog -Process $Process
        $Process.Refresh()
        if ($Process.HasExited) { throw "BricsCAD exited before the Curtain P11 marker." }
        Start-Sleep -Milliseconds 500
    }
    throw "Timed out waiting for the Curtain P11 marker."
}

function Remove-ExactFile {
    param([Parameter(Mandatory = $true)][string]$Path)
    if (Test-Path -LiteralPath $Path) { Remove-Item -LiteralPath $Path -Force -ErrorAction Stop }
    if (Test-Path -LiteralPath $Path) { throw "Curtain P11 exact private-file cleanup failed." }
}

if ([Environment]::OSVersion.Platform -ne [PlatformID]::Win32NT) { throw "Curtain P11 qualification requires Windows." }
if (-not [Environment]::UserInteractive) { throw "Curtain P11 qualification requires an interactive Windows session." }
if (-not $ConfirmDisposableCopy) { throw "Pass -ConfirmDisposableCopy only for a disposable synthetic drawing copy." }
if ([string]::IsNullOrWhiteSpace($Profile)) { throw "Curtain P11 qualification requires an initialized BricsCAD profile." }

$repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot ".."))
$BricsCadDir = [IO.Path]::GetFullPath($BricsCadDir)
$PluginDll = [IO.Path]::GetFullPath($PluginDll)
$DrawingCopy = [IO.Path]::GetFullPath($DrawingCopy)
$ArtifactDir = [IO.Path]::GetFullPath($ArtifactDir)
if (-not [IO.Path]::GetFileName($DrawingCopy).EndsWith(".curtain-undo-reopen-probe-copy.dwg", [StringComparison]::OrdinalIgnoreCase)) {
    throw "DrawingCopy must use the guarded '*.curtain-undo-reopen-probe-copy.dwg' suffix."
}
if ($DrawingCopy.StartsWith($repoRoot + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
    throw "DrawingCopy must be an ordinary disposable copy outside the repository."
}
if ($ArtifactDir.StartsWith($repoRoot + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
    throw "ArtifactDir must stay outside the repository."
}

$bricscadExe = Join-Path $BricsCadDir "bricscad.exe"
$coreDll = Join-Path (Split-Path -Parent $PluginDll) "QS3D.Core.dll"
foreach ($required in @($bricscadExe, $PluginDll, $coreDll, $DrawingCopy)) {
    if (-not (Test-Path -LiteralPath $required -PathType Leaf)) { throw "Required Curtain P11 input is missing." }
}
$expectedPlugin = [IO.Path]::GetFullPath((Join-Path $repoRoot "src\QS3D.BricsCAD.V25\bin\x64\Release\net48\QS3D.BricsCAD.V25.dll"))
if (-not [string]::Equals($PluginDll, $expectedPlugin, [StringComparison]::OrdinalIgnoreCase)) {
    throw "PluginDll must be the exact repository x64 Release V25 build output."
}

$git = Get-Command git -CommandType Application -ErrorAction Stop | Select-Object -First 1
if ($null -eq $git -or [string]::IsNullOrWhiteSpace($git.Source)) { throw "Git executable is unavailable." }
$gitOutput = @(& $git.Source -C $repoRoot rev-parse HEAD 2>$null)
$gitExitCode = $LASTEXITCODE
if ($gitExitCode -ne 0 -or $gitOutput.Count -ne 1) { throw "Cannot resolve the exact Curtain P11 Git candidate SHA." }
$gitHead = ([string]$gitOutput[0]).Trim().ToLowerInvariant()
if ($gitHead -notmatch '^[0-9a-f]{40}$') { throw "Curtain P11 Git candidate SHA is invalid." }
$gitStatus = @(& $git.Source -C $repoRoot status --porcelain=v1 --untracked-files=all 2>$null)
$gitStatusExitCode = $LASTEXITCODE
if ($gitStatusExitCode -ne 0) { throw "Cannot inspect the Curtain P11 candidate worktree." }
if ($gitStatus.Count -ne 0) { throw "Curtain P11 qualification requires a clean exact-SHA worktree." }
$expectedAssemblyRevision = "+" + $gitHead
foreach ($assemblyPath in @($PluginDll, $coreDll)) {
    $productVersion = [string](Get-Item -LiteralPath $assemblyPath).VersionInfo.ProductVersion
    if (-not $productVersion.EndsWith($expectedAssemblyRevision, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Curtain P11 assembly was not built from the exact Git candidate SHA."
    }
}
if (@(Get-Process -Name "bricscad" -ErrorAction SilentlyContinue).Count -gt 0) {
    throw "Close existing BricsCAD processes before starting isolated Curtain P11 qualification."
}

$projectSidecar = [IO.Path]::ChangeExtension($DrawingCopy, ".qsdb")
$sidecarBackup = $projectSidecar + ".bak"
$sidecarLock = $projectSidecar + ".lock"
$drawingLock = [IO.Path]::ChangeExtension($DrawingCopy, ".dwl")
$drawingLock2 = [IO.Path]::ChangeExtension($DrawingCopy, ".dwl2")
foreach ($privateInput in @($projectSidecar, $sidecarBackup, $sidecarLock, $drawingLock, $drawingLock2)) {
    if (Test-Path -LiteralPath $privateInput) { throw "Curtain P11 disposable copy has pre-existing private state." }
}

if (Test-Path -LiteralPath $ArtifactDir) {
    if (@(Get-ChildItem -LiteralPath $ArtifactDir -Force).Count -ne 0) { throw "ArtifactDir must be empty." }
}
else { New-Item -ItemType Directory -Path $ArtifactDir | Out-Null }

$resultPath = Join-Path $ArtifactDir "curtain-panel-undo-reopen-result.txt"
$phasePath = Join-Path $ArtifactDir "curtain-panel-undo-reopen-session1.txt"
$scriptOnePath = Join-Path $ArtifactDir "curtain-panel-undo-reopen-session1.scr"
$scriptTwoPath = Join-Path $ArtifactDir "curtain-panel-undo-reopen-session2.scr"
$metadataPath = Join-Path $ArtifactDir "curtain-panel-undo-reopen-metadata.json"
$originalCopyPath = Join-Path $ArtifactDir "curtain-panel-undo-reopen-original.private.dwg"
foreach ($output in @($resultPath, $phasePath, $scriptOnePath, $scriptTwoPath, $metadataPath, $originalCopyPath)) {
    if (Test-Path -LiteralPath $output) { throw "Curtain P11 output already exists." }
}

$drawingHashBefore = (Get-FileHash -LiteralPath $DrawingCopy -Algorithm SHA256).Hash.ToUpperInvariant()
$pluginHash = (Get-FileHash -LiteralPath $PluginDll -Algorithm SHA256).Hash.ToUpperInvariant()
Copy-Item -LiteralPath $DrawingCopy -Destination $originalCopyPath -ErrorAction Stop
if (-not [string]::Equals((Get-FileHash -LiteralPath $originalCopyPath -Algorithm SHA256).Hash, $drawingHashBefore, [StringComparison]::OrdinalIgnoreCase)) {
    throw "Curtain P11 private restore copy hash mismatch."
}

$nonce = [Guid]::NewGuid().ToString("N")
$environmentNames = @(
    "QS3D_CURTAIN_P11_RESULT",
    "QS3D_CURTAIN_P11_PHASE_RESULT",
    "QS3D_CURTAIN_P11_NONCE",
    "QS3D_CURTAIN_P11_EXPECTED_HOSTS",
    "QS3D_CURTAIN_P11_EXPECTED_FRAMES",
    "QS3D_CURTAIN_P11_EXPECTED_PANELS",
    "QS3D_CURTAIN_P11_UNDO_COHERENT",
    "QS3D_CURTAIN_P11_REDO_COHERENT"
)
$oldEnvironment = @{}
foreach ($name in $environmentNames) { $oldEnvironment[$name] = [Environment]::GetEnvironmentVariable($name, "Process") }
$processOne = $null
$processTwo = $null
$proxyInformationDialogsDismissed = 0
$startedAt = Get-Date
$qualificationError = $null
$cleanupError = $null
$processCleanupVerified = $false
$phaseMarker = $null
$finalMarker = $null
$savedDrawingHash = ""
$rebuiltDrawingHash = ""

try {
    $env:QS3D_CURTAIN_P11_RESULT = $resultPath
    $env:QS3D_CURTAIN_P11_PHASE_RESULT = $phasePath
    $env:QS3D_CURTAIN_P11_NONCE = $nonce

    $scriptOne = @(
        "FILEDIA", "0",
        "CMDECHO", "1",
        "TILEMODE", "1",
        "INSUNITS", "4",
        "UCS", "W",
        "NETLOAD", ('"' + $PluginDll + '"'),
        "QS3DDRAWGLASSWALL",
        "0,0", "5000,0", "",
        "QS3DCURTAINP11PREPARE",
        "QS3DCURTAIN3D",
        "QS3DCURTAINP11BASELINE",
        "_.UNDO", "1",
        "QS3DCURTAINP11CHECKUNDO",
        "_.REDO",
        "QS3DCURTAINP11CHECKREDO",
        "QS3DSAVE",
        "_.QSAVE",
        "QS3DCURTAINP11SESSION1"
    )
    Set-Content -LiteralPath $scriptOnePath -Value $scriptOne -Encoding ASCII
    $argumentsOne = '"' + $DrawingCopy + '" /P "' + $Profile + '" /B "' + $scriptOnePath + '"'
    $processOne = Start-Process -FilePath $bricscadExe -ArgumentList $argumentsOne -PassThru -WindowStyle Hidden -WorkingDirectory $ArtifactDir
    $proxyInformationDialogsDismissed += Wait-Qs3dMarker -Process $processOne -ExpectedPath $phasePath -FailurePath $resultPath -Deadline (Get-Date).AddSeconds($StartupTimeoutSeconds)
    if (-not (Test-Path -LiteralPath $phasePath -PathType Leaf)) {
        $failed = Read-Qs3dMarker -Path $resultPath
        $phase = if ($failed.ContainsKey("failure_phase")) { [string]$failed["failure_phase"] } else { "session1" }
        $code = if ($failed.ContainsKey("failure_code")) { [string]$failed["failure_code"] } else { "STATE_REJECTED" }
        throw "Curtain P11 session one failed: $phase/$code"
    }
    $phaseMarker = Read-Qs3dMarker -Path $phasePath
    Require-Qs3dValue -Marker $phaseMarker -Key "status" -Expected "PASS"
    Require-Qs3dValue -Marker $phaseMarker -Key "command" -Expected "QS3DCURTAINP11SESSION1"
    Require-Qs3dValue -Marker $phaseMarker -Key "nonce" -Expected $nonce
    Require-Qs3dValue -Marker $phaseMarker -Key "schema" -Expected "QS3D_CURTAIN_PANEL_UNDO_REOPEN_RUNTIME_V1"
    Require-Qs3dValue -Marker $phaseMarker -Key "health_issue_count" -Expected "0"
    Require-Qs3dValue -Marker $phaseMarker -Key "source_preserved" -Expected "true"
    Require-Qs3dValue -Marker $phaseMarker -Key "sentinel_preserved" -Expected "true"
    $hostCount = Read-PositiveMarkerInt -Marker $phaseMarker -Key "host_solid_count"
    $frameCount = Read-PositiveMarkerInt -Marker $phaseMarker -Key "frame_solid_count"
    $panelCount = Read-PositiveMarkerInt -Marker $phaseMarker -Key "panel_solid_count"
    $null = Read-NonNegativeMarkerLong -Marker $phaseMarker -Key "change_version"

    Stop-Qs3dLaunchedProcess -Process $processOne
    Remove-ExactFile -Path $scriptOnePath
    if (-not (Test-Path -LiteralPath $projectSidecar -PathType Leaf)) { throw "Curtain P11 session one did not persist the QSDB sidecar." }
    $savedDrawingHash = (Get-FileHash -LiteralPath $DrawingCopy -Algorithm SHA256).Hash.ToUpperInvariant()
    if ([string]::Equals($savedDrawingHash, $drawingHashBefore, [StringComparison]::Ordinal)) {
        throw "Curtain P11 session one did not save the DWG."
    }

    $env:QS3D_CURTAIN_P11_EXPECTED_HOSTS = $hostCount.ToString([Globalization.CultureInfo]::InvariantCulture)
    $env:QS3D_CURTAIN_P11_EXPECTED_FRAMES = $frameCount.ToString([Globalization.CultureInfo]::InvariantCulture)
    $env:QS3D_CURTAIN_P11_EXPECTED_PANELS = $panelCount.ToString([Globalization.CultureInfo]::InvariantCulture)
    $env:QS3D_CURTAIN_P11_UNDO_COHERENT = [string]$phaseMarker["undo_coherent"]
    $env:QS3D_CURTAIN_P11_REDO_COHERENT = [string]$phaseMarker["redo_coherent"]

    $scriptTwo = @(
        "FILEDIA", "0",
        "CMDECHO", "1",
        "TILEMODE", "1",
        "INSUNITS", "4",
        "UCS", "W",
        "NETLOAD", ('"' + $PluginDll + '"'),
        "QS3DCURTAINP11REOPEN",
        "QS3DCURTAIN3D",
        "QS3DCURTAINP11AFTERREBUILD",
        "QS3DSAVE",
        "_.QSAVE",
        "QS3DCURTAINP11COMPLETE"
    )
    Set-Content -LiteralPath $scriptTwoPath -Value $scriptTwo -Encoding ASCII
    $argumentsTwo = '"' + $DrawingCopy + '" /P "' + $Profile + '" /B "' + $scriptTwoPath + '"'
    $processTwo = Start-Process -FilePath $bricscadExe -ArgumentList $argumentsTwo -PassThru -WindowStyle Hidden -WorkingDirectory $ArtifactDir
    $proxyInformationDialogsDismissed += Wait-Qs3dMarker -Process $processTwo -ExpectedPath $resultPath -FailurePath $resultPath -Deadline (Get-Date).AddSeconds($StartupTimeoutSeconds)
    $finalMarker = Read-Qs3dMarker -Path $resultPath
    Require-Qs3dValue -Marker $finalMarker -Key "command" -Expected "QS3DCURTAINP11COMPLETE"
    Require-Qs3dValue -Marker $finalMarker -Key "nonce" -Expected $nonce
    Require-Qs3dValue -Marker $finalMarker -Key "schema" -Expected "QS3D_CURTAIN_PANEL_UNDO_REOPEN_RUNTIME_V1"
    Require-Qs3dValue -Marker $finalMarker -Key "production_local002_qualified" -Expected "false"

    Stop-Qs3dLaunchedProcess -Process $processTwo
    Remove-ExactFile -Path $scriptTwoPath
    if (-not (Test-Path -LiteralPath $projectSidecar -PathType Leaf)) { throw "Curtain P11 rebuild did not retain the QSDB sidecar." }
    $rebuiltDrawingHash = (Get-FileHash -LiteralPath $DrawingCopy -Algorithm SHA256).Hash.ToUpperInvariant()

    if ([string]::Equals([string]$finalMarker["status"], "PASS", [StringComparison]::OrdinalIgnoreCase)) {
        foreach ($key in @("undo_coherent", "redo_coherent", "reopen_coherent", "rebuild_coherent", "source_preserved", "sentinel_preserved", "old_generated_removed", "new_generated_disjoint", "rebuild_counts_stable", "p11_qualified")) {
            Require-Qs3dValue -Marker $finalMarker -Key $key -Expected "true"
        }
        Require-Qs3dValue -Marker $finalMarker -Key "health_issue_count" -Expected "0"
        $reopenedHostCount = Read-PositiveMarkerInt -Marker $finalMarker -Key "reopened_host_count"
        $reopenedFrameCount = Read-PositiveMarkerInt -Marker $finalMarker -Key "reopened_frame_count"
        $reopenedPanelCount = Read-PositiveMarkerInt -Marker $finalMarker -Key "reopened_panel_count"
        $rebuiltHostCount = Read-PositiveMarkerInt -Marker $finalMarker -Key "rebuilt_host_count"
        $rebuiltFrameCount = Read-PositiveMarkerInt -Marker $finalMarker -Key "rebuilt_frame_count"
        $rebuiltPanelCount = Read-PositiveMarkerInt -Marker $finalMarker -Key "rebuilt_panel_count"
        $null = Read-NonNegativeMarkerLong -Marker $finalMarker -Key "reopened_change_version"
        $null = Read-NonNegativeMarkerLong -Marker $finalMarker -Key "rebuilt_change_version"
        if ($reopenedHostCount -ne $hostCount -or $reopenedFrameCount -ne $frameCount -or $reopenedPanelCount -ne $panelCount) {
            throw "Curtain P11 cold-reopen counts changed."
        }
        if ($rebuiltHostCount -ne $reopenedHostCount -or $rebuiltFrameCount -ne $reopenedFrameCount -or $rebuiltPanelCount -ne $reopenedPanelCount) {
            throw "Curtain P11 rebuild counts changed for unchanged source/configuration."
        }
    }
    else {
        $phase = if ($finalMarker.ContainsKey("failure_phase")) { [string]$finalMarker["failure_phase"] } else { "unknown" }
        $code = if ($finalMarker.ContainsKey("failure_code")) { [string]$finalMarker["failure_code"] } else { "STATE_REJECTED" }
        throw "Curtain P11 licensed result failed: $phase/$code"
    }
}
catch {
    $qualificationError = $_
}
finally {
    $processesStopped = $true
    foreach ($launched in @($processOne, $processTwo)) {
        try { Stop-Qs3dLaunchedProcess -Process $launched }
        catch {
            $processesStopped = $false
            if ($null -eq $cleanupError) { $cleanupError = $_ }
        }
    }
    if ($processesStopped) {
        foreach ($scriptPath in @($scriptOnePath, $scriptTwoPath)) {
            try { Remove-ExactFile -Path $scriptPath }
            catch { if ($null -eq $cleanupError) { $cleanupError = $_ } }
        }
        foreach ($privatePath in @($projectSidecar, $sidecarBackup, $sidecarLock, $drawingLock, $drawingLock2)) {
            try { Remove-ExactFile -Path $privatePath }
            catch { if ($null -eq $cleanupError) { $cleanupError = $_ } }
        }
        if (Test-Path -LiteralPath $originalCopyPath -PathType Leaf) {
            try {
                Copy-Item -LiteralPath $originalCopyPath -Destination $DrawingCopy -Force -ErrorAction Stop
                $restoredHash = (Get-FileHash -LiteralPath $DrawingCopy -Algorithm SHA256).Hash.ToUpperInvariant()
                if (-not [string]::Equals($restoredHash, $drawingHashBefore, [StringComparison]::Ordinal)) {
                    throw "Curtain P11 disposable drawing restore hash mismatch."
                }
                Remove-ExactFile -Path $originalCopyPath
            }
            catch { if ($null -eq $cleanupError) { $cleanupError = $_ } }
        }
        try {
            if (@(Get-Process -Name "bricscad" -ErrorAction SilentlyContinue).Count -gt 0) {
                throw "Curtain P11 cleanup left a BricsCAD process."
            }
            $processCleanupVerified = $true
        }
        catch { if ($null -eq $cleanupError) { $cleanupError = $_ } }
    }
    foreach ($name in $environmentNames) { Restore-EnvironmentValue -Name $name -Value $oldEnvironment[$name] }
}

$markerForMetadata = if ($null -ne $finalMarker) { $finalMarker } elseif ($null -ne $phaseMarker) { $phaseMarker } else { @{} }
$metadataStatus = if ($null -eq $qualificationError -and $null -eq $cleanupError) { "PASS" } else { "FAIL" }
$metadata = [ordered]@{
    status = $metadataStatus
    git_sha = $gitHead
    started_at = $startedAt.ToUniversalTime().ToString("O")
    completed_at = (Get-Date).ToUniversalTime().ToString("O")
    bricscad_file_version = (Get-Item -LiteralPath $bricscadExe).VersionInfo.FileVersion
    plugin_sha256 = $pluginHash
    drawing_copy_sha256_before = $drawingHashBefore
    drawing_copy_sha256_saved = $savedDrawingHash
    drawing_copy_sha256_rebuilt = $rebuiltDrawingHash
    drawing_copy_sha256_restored = (Get-FileHash -LiteralPath $DrawingCopy -Algorithm SHA256).Hash.ToUpperInvariant()
    process_cleanup_verified = $processCleanupVerified
    script_cleanup_verified = (-not (Test-Path -LiteralPath $scriptOnePath) -and -not (Test-Path -LiteralPath $scriptTwoPath))
    sidecar_cleanup_verified = (-not (Test-Path -LiteralPath $projectSidecar) -and -not (Test-Path -LiteralPath $sidecarBackup) -and -not (Test-Path -LiteralPath $sidecarLock))
    drawing_lock_cleanup_verified = (-not (Test-Path -LiteralPath $drawingLock) -and -not (Test-Path -LiteralPath $drawingLock2))
    drawing_restore_verified = [string]::Equals((Get-FileHash -LiteralPath $DrawingCopy -Algorithm SHA256).Hash, $drawingHashBefore, [StringComparison]::OrdinalIgnoreCase)
    proxy_information_dialogs_dismissed = $proxyInformationDialogsDismissed
    marker = $markerForMetadata
}
$metadata | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $metadataPath -Encoding UTF8

if ($null -ne $cleanupError) { throw $cleanupError }
if ($null -ne $qualificationError) { throw $qualificationError }

Write-Host "QS3D BricsCAD V25 Curtain P11 Undo/save-reopen/rebuild PASS"
Write-Host "Marker: $resultPath"
Write-Host "Metadata: $metadataPath"
