param(
    [Parameter(Mandatory = $true)][string]$BricsCadDir,
    [Parameter(Mandatory = $true)][string]$PluginDll,
    [Parameter(Mandatory = $true)][string]$DrawingCopy,
    [Parameter(Mandatory = $true)][string]$Profile,
    [Parameter(Mandatory = $true)][string]$ArtifactDir,
    [Parameter(Mandatory = $true)][switch]$ConfirmDisposableCopy,
    [ValidateRange(60, 1200)][int]$StartupTimeoutSeconds = 420
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$windowInteropPath = Join-Path $PSScriptRoot "bricscad-runner-window-interop.ps1"
if (-not (Test-Path -LiteralPath $windowInteropPath -PathType Leaf)) { throw "Curtain P08 runner window interop helper is missing." }
. $windowInteropPath

function Read-Qs3dMarker {
    param([Parameter(Mandatory = $true)][string]$Path)
    $marker = @{}
    foreach ($line in Get-Content -LiteralPath $Path) {
        if ([string]::IsNullOrWhiteSpace($line)) { continue }
        $separator = $line.IndexOf('=')
        if ($separator -le 0) { throw "Malformed Curtain P08 marker line." }
        $key = $line.Substring(0, $separator).Trim()
        $value = $line.Substring($separator + 1).Trim()
        if ($marker.ContainsKey($key)) { throw "Duplicate Curtain P08 marker key." }
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
    if (-not $Marker.ContainsKey($Key)) { throw "Curtain P08 marker is missing a required key." }
    if (-not [string]::Equals([string]$Marker[$Key], $Expected, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Curtain P08 marker contains an unexpected required value."
    }
}

function Read-Qs3dAllowedValue {
    param(
        [Parameter(Mandatory = $true)]$Marker,
        [Parameter(Mandatory = $true)][string]$Key,
        [Parameter(Mandatory = $true)][string[]]$Allowed
    )
    if (-not $Marker.ContainsKey($Key)) { throw "Curtain P08 marker is missing a diagnostic key." }
    $value = [string]$Marker[$Key]
    foreach ($candidate in $Allowed) {
        if ([string]::Equals($value, $candidate, [StringComparison]::Ordinal)) { return $value }
    }
    throw "Curtain P08 marker diagnostic token is not allowlisted."
}

function Read-PositiveMarkerInt {
    param([Parameter(Mandatory = $true)]$Marker, [Parameter(Mandatory = $true)][string]$Key)
    if (-not $Marker.ContainsKey($Key)) { throw "Curtain P08 marker is missing a positive-count key." }
    [int]$value = 0
    if (-not [int]::TryParse([string]$Marker[$Key], [Globalization.NumberStyles]::None, [Globalization.CultureInfo]::InvariantCulture, [ref]$value) -or $value -le 0) {
        throw "Curtain P08 marker count is not a positive invariant integer."
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
        $Process.WaitForExit(10000) | Out-Null
        $Process.Refresh()
    }
    if (-not $Process.HasExited) { throw "Launched BricsCAD Curtain P08 process did not exit." }
}

function Remove-Qs3dDrawingLocks {
    param([Parameter(Mandatory = $true)][string[]]$Paths)
    foreach ($path in $Paths) {
        if (Test-Path -LiteralPath $path) { Remove-Item -LiteralPath $path -Force -ErrorAction Stop }
        if (Test-Path -LiteralPath $path) { throw "Curtain P08 drawing-lock cleanup failed." }
    }
}

if ([Environment]::OSVersion.Platform -ne [PlatformID]::Win32NT) { throw "Curtain P08 runtime qualification requires Windows." }
if (-not [Environment]::UserInteractive) { throw "Curtain P08 runtime qualification requires an interactive Windows session." }
if (-not $ConfirmDisposableCopy) { throw "Pass -ConfirmDisposableCopy only for a disposable synthetic drawing copy." }
if ([string]::IsNullOrWhiteSpace($Profile)) { throw "Curtain P08 runtime qualification requires an initialized BricsCAD profile." }

$repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot ".."))
$BricsCadDir = [IO.Path]::GetFullPath($BricsCadDir)
$PluginDll = [IO.Path]::GetFullPath($PluginDll)
$DrawingCopy = [IO.Path]::GetFullPath($DrawingCopy)
$ArtifactDir = [IO.Path]::GetFullPath($ArtifactDir)
if (-not [IO.Path]::GetFileName($DrawingCopy).EndsWith(".curtain-atomic-failure-probe-copy.dwg", [StringComparison]::OrdinalIgnoreCase)) {
    throw "DrawingCopy must use the guarded '*.curtain-atomic-failure-probe-copy.dwg' suffix."
}

$bricscadExe = Join-Path $BricsCadDir "bricscad.exe"
$coreDll = Join-Path (Split-Path -Parent $PluginDll) "QS3D.Core.dll"
foreach ($required in @($bricscadExe, $PluginDll, $coreDll, $DrawingCopy)) {
    if (-not (Test-Path -LiteralPath $required -PathType Leaf)) { throw "A required Curtain P08 runtime input is missing." }
}
$expectedPlugin = [IO.Path]::GetFullPath((Join-Path $repoRoot "src\QS3D.BricsCAD.V25\bin\x64\Release\net48\QS3D.BricsCAD.V25.dll"))
if (-not [string]::Equals($PluginDll, $expectedPlugin, [StringComparison]::OrdinalIgnoreCase)) {
    throw "PluginDll must be the exact repository x64 Release V25 build output."
}
if ([string]::Equals($ArtifactDir, $repoRoot, [StringComparison]::OrdinalIgnoreCase) -or
    $ArtifactDir.StartsWith($repoRoot + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
    throw "ArtifactDir must stay outside the repository."
}

$git = Get-Command git -CommandType Application -ErrorAction Stop | Select-Object -First 1
if ($null -eq $git -or [string]::IsNullOrWhiteSpace($git.Source)) { throw "Git executable is unavailable." }
$gitHead = (& $git.Source -C $repoRoot rev-parse HEAD).Trim()
if ($LASTEXITCODE -ne 0 -or $gitHead -notmatch '^[0-9a-f]{40}$') { throw "Cannot resolve the exact Git candidate SHA." }
$gitStatus = @(& $git.Source -C $repoRoot status --porcelain --untracked-files=normal)
if ($LASTEXITCODE -ne 0) { throw "Cannot inspect the Git candidate worktree." }
if ($gitStatus.Count -ne 0) { throw "Curtain P08 runtime qualification requires a clean exact-SHA worktree." }
if (@(Get-Process -Name "bricscad" -ErrorAction SilentlyContinue).Count -gt 0) {
    throw "Close existing BricsCAD processes before starting the isolated Curtain P08 runtime probe."
}

$projectSidecar = [IO.Path]::ChangeExtension($DrawingCopy, ".qsdb")
$drawingBackup = [IO.Path]::ChangeExtension($DrawingCopy, ".bak")
$drawingLocks = @([IO.Path]::ChangeExtension($DrawingCopy, ".dwl"), [IO.Path]::ChangeExtension($DrawingCopy, ".dwl2"))
foreach ($forbiddenInput in @($projectSidecar, ($projectSidecar + ".bak"), $drawingBackup) + $drawingLocks) {
    if (Test-Path -LiteralPath $forbiddenInput) { throw "The disposable Curtain P08 copy has a pre-existing sidecar or backup." }
}
if (Test-Path -LiteralPath $ArtifactDir) {
    if (@(Get-ChildItem -LiteralPath $ArtifactDir -Force).Count -ne 0) { throw "ArtifactDir must be empty." }
}
else { New-Item -ItemType Directory -Path $ArtifactDir | Out-Null }

$resultPath = Join-Path $ArtifactDir "curtain-panel-atomic-failure-runtime-result.txt"
$scriptPath = Join-Path $ArtifactDir "curtain-panel-atomic-failure-runtime.scr"
$metadataPath = Join-Path $ArtifactDir "curtain-panel-atomic-failure-runtime-metadata.json"
foreach ($output in @($resultPath, $scriptPath, $metadataPath)) {
    if (Test-Path -LiteralPath $output) { throw "Curtain P08 runtime output must not already exist." }
}

$drawingHashBefore = (Get-FileHash -LiteralPath $DrawingCopy -Algorithm SHA256).Hash.ToUpperInvariant()
$pluginHash = (Get-FileHash -LiteralPath $PluginDll -Algorithm SHA256).Hash.ToUpperInvariant()
$nonce = [Guid]::NewGuid().ToString("N")
$oldResult = [Environment]::GetEnvironmentVariable("QS3D_CURTAIN_PANEL_P08_RESULT", "Process")
$oldNonce = [Environment]::GetEnvironmentVariable("QS3D_CURTAIN_PANEL_P08_NONCE", "Process")
$process = $null
$proxyInformationDialogsDismissed = 0
$startedAt = Get-Date

try {
    $env:QS3D_CURTAIN_PANEL_P08_RESULT = $resultPath
    $env:QS3D_CURTAIN_PANEL_P08_NONCE = $nonce
    $script = @(
        "FILEDIA", "0",
        "CMDECHO", "1",
        "TILEMODE", "1",
        "INSUNITS", "4",
        "UCS", "W",
        "NETLOAD", ('"' + $PluginDll + '"'),
        "QS3DCURTAINP08SEEDLINE",
        "QS3DGLASSWALL",
        "QS3DDRAWGLASSWALL", "0,10000", "4000,10000", "4000,13000", "",
        "QS3DCURTAINP08PREPARE",
        "QS3DCURTAIN3D",
        "QS3DCURTAINP08BASELINE",
        "QS3DCURTAINP08ARM",
        "QS3DCURTAIN3D",
        "QS3DCURTAINP08VERIFY",
        "QS3DCURTAINP08ARM",
        "QS3DCURTAIN3D",
        "QS3DCURTAINP08VERIFY",
        "QS3DCURTAINP08ARM",
        "QS3DCURTAIN3D",
        "QS3DCURTAINP08VERIFY",
        "QS3DCURTAINP08ARM",
        "QS3DCURTAIN3D",
        "QS3DCURTAINP08VERIFY",
        "QS3DCURTAINP08ARM",
        "QS3DCURTAIN3D",
        "QS3DCURTAINP08VERIFY",
        "QS3DCURTAINP08ARM",
        "QS3DCURTAIN3D",
        "QS3DCURTAINP08VERIFY",
        "QS3DCURTAINP08ARM",
        "QS3DCURTAIN3D",
        "QS3DCURTAINP08VERIFY",
        "QS3DCURTAINP08VALID",
        "QS3DCURTAIN3D",
        "QS3DCURTAINP08PROBE"
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
        if ($process.HasExited) { throw "BricsCAD exited before the Curtain P08 marker." }
        Start-Sleep -Milliseconds 500
    }
    if (-not (Test-Path -LiteralPath $resultPath -PathType Leaf)) { throw "Timed out waiting for the synthetic Curtain P08 atomic-failure matrix." }

    $marker = Read-Qs3dMarker -Path $resultPath
    $diagnosticFailure = $false
    $failurePhase = ""
    $failureCode = ""
    $finalPanelCount = 0
    if ($marker.ContainsKey("status") -and [string]::Equals([string]$marker["status"], "FAIL", [StringComparison]::Ordinal)) {
        $failurePhases = @(
            "PROBE_AUTH", "SEED_LINE", "PREPARE_BASELINE", "VERIFY_BASELINE", "ARM_FAILURE",
            "VERIFY_FAILURE_ROLLBACK", "PREPARE_VALID_REPLACEMENT", "VERIFY_VALID_REPLACEMENT", "RESULT_PUBLISH"
        )
        $failureCodes = @(
            "STATE_REJECTED", "DATA_REJECTED", "IO_REJECTED", "OVERFLOW_REJECTED", "UNEXPECTED_REJECTED",
            "OWNER_STALE_REJECTED", "OWNER_METADATA_REJECTED", "OWNER_OUTPUT_REJECTED",
            "OWNER_OUTPUT_NOT_LIVE", "OWNER_HEALTH_REJECTED", "OWNERSHIP_OVERLAP_REJECTED",
            "LINE_SOURCE_METADATA_REJECTED", "LINE_HOST_METADATA_REJECTED", "LINE_FRAME_METADATA_REJECTED", "LINE_PANEL_METADATA_REJECTED",
            "PATH_SOURCE_METADATA_REJECTED", "PATH_HOST_METADATA_REJECTED", "PATH_FRAME_METADATA_REJECTED", "PATH_PANEL_METADATA_REJECTED"
        )
        $failureKeys = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
        foreach ($key in @(
            "status", "command", "nonce", "schema", "qualification_boundary",
            "production_local002_qualified", "error_code", "failure_phase", "failure_code"
        )) { [void]$failureKeys.Add($key) }
        foreach ($key in $marker.Keys) {
            if (-not $failureKeys.Contains([string]$key)) { throw "Curtain P08 FAIL marker contains a non-contract field." }
        }
        Require-Qs3dValue -Marker $marker -Key "command" -Expected "QS3DCURTAINP08PROBE"
        Require-Qs3dValue -Marker $marker -Key "nonce" -Expected $nonce
        Require-Qs3dValue -Marker $marker -Key "schema" -Expected "QS3D_CURTAIN_PANEL_ATOMIC_FAILURE_RUNTIME_V1"
        Require-Qs3dValue -Marker $marker -Key "qualification_boundary" -Expected "LOCAL_002_P08_ONLY"
        Require-Qs3dValue -Marker $marker -Key "production_local002_qualified" -Expected "false"
        Require-Qs3dValue -Marker $marker -Key "error_code" -Expected "CURTAIN_PANEL_ATOMIC_FAILURE_RUNTIME_FAILED"
        $failurePhase = Read-Qs3dAllowedValue -Marker $marker -Key "failure_phase" -Allowed $failurePhases
        $failureCode = Read-Qs3dAllowedValue -Marker $marker -Key "failure_code" -Allowed $failureCodes
        $diagnosticFailure = $true
    }
    else {
        Require-Qs3dValue -Marker $marker -Key "status" -Expected "PASS"
        Require-Qs3dValue -Marker $marker -Key "command" -Expected "QS3DCURTAINP08PROBE"
        Require-Qs3dValue -Marker $marker -Key "process" -Expected "bricscad"
        Require-Qs3dValue -Marker $marker -Key "nonce" -Expected $nonce
        Require-Qs3dValue -Marker $marker -Key "schema" -Expected "QS3D_CURTAIN_PANEL_ATOMIC_FAILURE_RUNTIME_V1"
        Require-Qs3dValue -Marker $marker -Key "qualification_boundary" -Expected "LOCAL_002_P08_ONLY"
        Require-Qs3dValue -Marker $marker -Key "production_local002_qualified" -Expected "false"
        Require-Qs3dValue -Marker $marker -Key "is_64bit" -Expected "true"
        Require-Qs3dValue -Marker $marker -Key "legacy_no_level" -Expected "true"
        Require-Qs3dValue -Marker $marker -Key "mixed_line_path" -Expected "true"
        Require-Qs3dValue -Marker $marker -Key "injected_phase_count" -Expected "7"
        foreach ($key in @(
            "semantic_regeneration_rollback", "line_host_rollback", "path_host_rollback",
            "line_frame_rollback", "path_frame_rollback", "line_panel_rollback", "path_panel_rollback",
            "whole_batch_native_preserved", "whole_batch_semantic_preserved", "source_geometry_preserved",
            "valid_replacement_succeeded", "valid_old_sets_removed", "valid_new_sets_complete"
        )) { Require-Qs3dValue -Marker $marker -Key $key -Expected "true" }
        Require-Qs3dValue -Marker $marker -Key "health_issue_count" -Expected "0"
        $baselinePanelCount = Read-PositiveMarkerInt -Marker $marker -Key "baseline_generated_count"
        $finalPanelCount = Read-PositiveMarkerInt -Marker $marker -Key "valid_generated_count"
        if ($baselinePanelCount -le 6 -or $finalPanelCount -le 6) { throw "Curtain P08 generated counts are unexpectedly small." }
    }

    Stop-Qs3dLaunchedProcess -Process $process
    Remove-Qs3dDrawingLocks -Paths $drawingLocks
    if (@(Get-Process -Name "bricscad" -ErrorAction SilentlyContinue).Count -ne 0) { throw "Curtain P08 process cleanup is incomplete." }
    if (Test-Path -LiteralPath $scriptPath) { Remove-Item -LiteralPath $scriptPath -Force -ErrorAction Stop }
    if (Test-Path -LiteralPath $scriptPath) { throw "Curtain P08 runtime script cleanup failed." }
    $drawingHashAfter = (Get-FileHash -LiteralPath $DrawingCopy -Algorithm SHA256).Hash.ToUpperInvariant()
    if (-not [string]::Equals($drawingHashBefore, $drawingHashAfter, [StringComparison]::Ordinal)) { throw "The disposable Curtain P08 drawing was written unexpectedly." }
    foreach ($forbiddenOutput in @($projectSidecar, ($projectSidecar + ".bak"), $drawingBackup)) {
        if (Test-Path -LiteralPath $forbiddenOutput) { throw "Curtain P08 runtime probe persisted an unexpected sidecar or backup." }
    }
    if ($diagnosticFailure) { throw "Curtain P08 probe failed at sanitized phase '$failurePhase' with code '$failureCode'; cleanup was verified." }

    $metadata = [ordered]@{
        status = "PASS"
        git_sha = $gitHead
        started_at = $startedAt.ToUniversalTime().ToString("O")
        completed_at = (Get-Date).ToUniversalTime().ToString("O")
        bricscad_file_version = (Get-Item -LiteralPath $bricscadExe).VersionInfo.FileVersion
        plugin_sha256 = $pluginHash
        drawing_copy_sha256_before = $drawingHashBefore
        drawing_copy_sha256_after = $drawingHashAfter
        process_cleanup_verified = $true
        script_cleanup_verified = $true
        drawing_lock_cleanup_verified = $true
        sidecar_absent_verified = $true
        backup_absent_verified = $true
        proxy_information_dialogs_dismissed = $proxyInformationDialogsDismissed
        marker = $marker
    }
    $metadata | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $metadataPath -Encoding UTF8
    Write-Host "QS3D BricsCAD V25 Curtain-panel P08 seven-boundary atomic-failure runtime PASS"
    Write-Host "Final valid generated objects: $finalPanelCount"
    Write-Host "Marker and sanitized metadata written to the requested artifact directory."
}
finally {
    try {
        Stop-Qs3dLaunchedProcess -Process $process
        Remove-Qs3dDrawingLocks -Paths $drawingLocks
        if (Test-Path -LiteralPath $scriptPath) { Remove-Item -LiteralPath $scriptPath -Force -ErrorAction Stop }
        if (Test-Path -LiteralPath $scriptPath) { throw "Curtain P08 runtime script cleanup failed." }
    }
    finally {
        Restore-EnvironmentValue -Name "QS3D_CURTAIN_PANEL_P08_RESULT" -Value $oldResult
        Restore-EnvironmentValue -Name "QS3D_CURTAIN_PANEL_P08_NONCE" -Value $oldNonce
    }
}


