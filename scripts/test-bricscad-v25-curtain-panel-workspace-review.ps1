param(
    [Parameter(Mandatory = $true)][string]$BricsCadDir,
    [Parameter(Mandatory = $true)][string]$PluginDll,
    [Parameter(Mandatory = $true)][string]$DrawingCopy,
    [Parameter(Mandatory = $true)][string]$Profile,
    [Parameter(Mandatory = $true)][string]$ArtifactDir,
    [Parameter(Mandatory = $true)][switch]$ConfirmDisposableCopy,
    [ValidateRange(60, 1200)][int]$StartupTimeoutSeconds = 600
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$windowInteropPath = Join-Path $PSScriptRoot "bricscad-runner-window-interop.ps1"
if (-not (Test-Path -LiteralPath $windowInteropPath -PathType Leaf)) {
    throw "Curtain P10 runner window interop helper is missing."
}
. $windowInteropPath

function Read-Qs3dMarker {
    param([Parameter(Mandatory = $true)][string]$Path)
    $marker = @{}
    foreach ($line in Get-Content -LiteralPath $Path) {
        if ([string]::IsNullOrWhiteSpace($line)) { continue }
        $separator = $line.IndexOf('=')
        if ($separator -le 0) { throw "Malformed Curtain P10 marker line." }
        $key = $line.Substring(0, $separator).Trim()
        $value = $line.Substring($separator + 1).Trim()
        if ($marker.ContainsKey($key)) { throw "Duplicate Curtain P10 marker key: $key" }
        $marker[$key] = $value
    }
    return $marker
}

function Read-Qs3dProgressPhase {
    param([Parameter(Mandatory = $true)][string]$Path)
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) { return $null }
    $lines = @(Get-Content -LiteralPath $Path)
    if ($lines.Count -ne 1) {
        throw "Malformed Curtain P10 progress marker."
    }
    $match = [regex]::Match([string]$lines[0], '^phase=([a-z_]+)$')
    if (-not $match.Success) { throw "Malformed Curtain P10 progress marker." }
    $phase = [string]$match.Groups[1].Value
    if ($phase -notin @(
        "plugin_loaded", "direct_draw_complete", "source_selection_prepared", "curtain_build_complete",
        "panel_selected", "workspace_opened", "workspace_inspected", "workspace_verified",
        "health_all_opened", "health_verified", "release_check_opened")) {
        throw "Curtain P10 progress marker has an unknown phase."
    }
    return $phase
}

function Require-Qs3dValue {
    param(
        [Parameter(Mandatory = $true)]$Marker,
        [Parameter(Mandatory = $true)][string]$Key,
        [Parameter(Mandatory = $true)][string]$Expected
    )
    if (-not $Marker.ContainsKey($Key)) { throw "Curtain P10 marker is missing '$Key'." }
    if (-not [string]::Equals([string]$Marker[$Key], $Expected, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Curtain P10 marker '$Key' did not match its expected value."
    }
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
    if (-not $Process.HasExited) { throw "Launched Curtain P10 BricsCAD process did not exit." }
}

function Find-Qs3dHandoffProcess {
    param(
        [Parameter(Mandatory = $true)][int]$LauncherId,
        [Parameter(Mandatory = $true)][string]$ExpectedExecutable
    )
    $records = @(Get-CimInstance -ClassName Win32_Process -Filter ("Name = 'bricscad.exe' AND ParentProcessId = " + $LauncherId))
    $matches = New-Object System.Collections.Generic.List[Diagnostics.Process]
    foreach ($record in $records) {
        $candidatePath = [string]$record.ExecutablePath
        if ([string]::IsNullOrWhiteSpace($candidatePath)) { continue }
        $candidatePath = [IO.Path]::GetFullPath($candidatePath)
        if (-not [string]::Equals($candidatePath, $ExpectedExecutable, [StringComparison]::OrdinalIgnoreCase)) { continue }
        $candidate = Get-Process -Id ([int]$record.ProcessId) -ErrorAction SilentlyContinue
        if ($null -ne $candidate) { $matches.Add($candidate) }
    }
    if ($matches.Count -gt 1) { throw "Curtain P10 launcher produced an ambiguous BricsCAD process handoff." }
    if ($matches.Count -eq 1) { return $matches[0] }
    return $null
}

function Wait-Qs3dHandoffProcess {
    param(
        [Parameter(Mandatory = $true)][int]$LauncherId,
        [Parameter(Mandatory = $true)][string]$ExpectedExecutable,
        [Parameter(Mandatory = $true)][DateTime]$Deadline
    )
    $handoffDeadline = (Get-Date).AddSeconds(30)
    if ($handoffDeadline -gt $Deadline) { $handoffDeadline = $Deadline }
    while ((Get-Date) -lt $handoffDeadline) {
        $candidate = Find-Qs3dHandoffProcess -LauncherId $LauncherId -ExpectedExecutable $ExpectedExecutable
        if ($null -ne $candidate) { return $candidate }
        Start-Sleep -Milliseconds 250
    }
    throw "BricsCAD launcher exited without an exact child-process handoff or Curtain P10 marker."
}

function Wait-Qs3dMarker {
    param(
        [Parameter(Mandatory = $true)][ref]$Process,
        [Parameter(Mandatory = $true)][ref]$HandoffCount,
        [Parameter(Mandatory = $true)][string]$ExpectedExecutable,
        [Parameter(Mandatory = $true)][string]$ResultPath,
        [Parameter(Mandatory = $true)][DateTime]$Deadline
    )
    $dismissed = 0
    [Diagnostics.Process]$current = $Process.Value
    $launcherId = $current.Id
    $handoffAdopted = $false
    while ((Get-Date) -lt $Deadline) {
        if (Test-Path -LiteralPath $ResultPath -PathType Leaf) { return $dismissed }
        $dismissed += Close-Qs3dProxyInformationDialog -Process $current
        $current.Refresh()
        if ($current.HasExited) {
            if ($handoffAdopted) { throw "BricsCAD exited before the Curtain P10 marker." }
            $current = Wait-Qs3dHandoffProcess -LauncherId $launcherId -ExpectedExecutable $ExpectedExecutable -Deadline $Deadline
            $Process.Value = $current
            $HandoffCount.Value = [int]$HandoffCount.Value + 1
            $handoffAdopted = $true
        }
        Start-Sleep -Milliseconds 500
    }
    throw "Timed out waiting for the Curtain P10 marker."
}

function Remove-ExactFile {
    param([Parameter(Mandatory = $true)][string]$Path)
    if (Test-Path -LiteralPath $Path) { Remove-Item -LiteralPath $Path -Force -ErrorAction Stop }
    if (Test-Path -LiteralPath $Path) { throw "Curtain P10 exact private-file cleanup failed." }
}

if ([Environment]::OSVersion.Platform -ne [PlatformID]::Win32NT) { throw "Curtain P10 qualification requires Windows." }
if (-not [Environment]::UserInteractive) { throw "Curtain P10 qualification requires an interactive Windows session." }
if (-not $ConfirmDisposableCopy) { throw "Pass -ConfirmDisposableCopy only for a disposable synthetic drawing copy." }
if ([string]::IsNullOrWhiteSpace($Profile)) { throw "Curtain P10 qualification requires an initialized BricsCAD profile." }

$repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot ".."))
$BricsCadDir = [IO.Path]::GetFullPath($BricsCadDir)
$PluginDll = [IO.Path]::GetFullPath($PluginDll)
$DrawingCopy = [IO.Path]::GetFullPath($DrawingCopy)
$ArtifactDir = [IO.Path]::GetFullPath($ArtifactDir)
if (-not [IO.Path]::GetFileName($DrawingCopy).EndsWith(".curtain-workspace-review-probe-copy.dwg", [StringComparison]::OrdinalIgnoreCase)) {
    throw "DrawingCopy must use the guarded '*.curtain-workspace-review-probe-copy.dwg' suffix."
}
if ($DrawingCopy.StartsWith($repoRoot + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
    throw "DrawingCopy must be an ordinary disposable copy outside the repository."
}
if ($ArtifactDir.StartsWith($repoRoot + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
    throw "ArtifactDir must stay outside the repository."
}

$bricscadExe = [IO.Path]::GetFullPath((Join-Path $BricsCadDir "bricscad.exe"))
$coreDll = Join-Path (Split-Path -Parent $PluginDll) "QS3D.Core.dll"
foreach ($required in @($bricscadExe, $PluginDll, $coreDll, $DrawingCopy)) {
    if (-not (Test-Path -LiteralPath $required -PathType Leaf)) { throw "Required Curtain P10 input is missing." }
}
$expectedPlugin = [IO.Path]::GetFullPath((Join-Path $repoRoot "src\QS3D.BricsCAD.V25\bin\x64\Release\net48\QS3D.BricsCAD.V25.dll"))
if (-not [string]::Equals($PluginDll, $expectedPlugin, [StringComparison]::OrdinalIgnoreCase)) {
    throw "PluginDll must be the exact repository x64 Release V25 build output."
}

$git = Get-Command git -CommandType Application -ErrorAction Stop | Select-Object -First 1
$gitOutput = @(& $git.Source -C $repoRoot rev-parse HEAD 2>$null)
$gitExitCode = $LASTEXITCODE
if ($gitExitCode -ne 0 -or $gitOutput.Count -ne 1) { throw "Cannot resolve the exact Curtain P10 Git candidate SHA." }
$gitHead = ([string]$gitOutput[0]).Trim().ToLowerInvariant()
if ($gitHead -notmatch '^[0-9a-f]{40}$') { throw "Curtain P10 Git candidate SHA is invalid." }
$gitStatus = @(& $git.Source -C $repoRoot status --porcelain=v1 --untracked-files=all 2>$null)
$gitStatusExitCode = $LASTEXITCODE
if ($gitStatusExitCode -ne 0) { throw "Cannot inspect the Curtain P10 candidate worktree." }
if ($gitStatus.Count -ne 0) { throw "Curtain P10 qualification requires a clean exact-SHA worktree." }
$expectedAssemblyRevision = "+" + $gitHead
foreach ($assemblyPath in @($PluginDll, $coreDll)) {
    $productVersion = [string](Get-Item -LiteralPath $assemblyPath).VersionInfo.ProductVersion
    if (-not $productVersion.EndsWith($expectedAssemblyRevision, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Curtain P10 assembly was not built from the exact Git candidate SHA."
    }
}
if (@(Get-Process -Name "bricscad" -ErrorAction SilentlyContinue).Count -gt 0) {
    throw "Close existing BricsCAD processes before isolated Curtain P10 qualification."
}

$projectSidecar = [IO.Path]::ChangeExtension($DrawingCopy, ".qsdb")
$privateFiles = @(
    $projectSidecar,
    $projectSidecar + ".bak",
    $projectSidecar + ".lock",
    [IO.Path]::ChangeExtension($DrawingCopy, ".dwl"),
    [IO.Path]::ChangeExtension($DrawingCopy, ".dwl2"),
    [IO.Path]::ChangeExtension($DrawingCopy, ".bak")
)
foreach ($privateInput in $privateFiles) {
    if (Test-Path -LiteralPath $privateInput) { throw "Curtain P10 disposable copy has pre-existing private state." }
}
if (Test-Path -LiteralPath $ArtifactDir) {
    if (@(Get-ChildItem -LiteralPath $ArtifactDir -Force).Count -ne 0) { throw "ArtifactDir must be empty." }
}
else { New-Item -ItemType Directory -Path $ArtifactDir | Out-Null }

$resultPath = Join-Path $ArtifactDir "curtain-panel-workspace-review-result.txt"
$progressPath = Join-Path $ArtifactDir "curtain-panel-workspace-review-progress.txt"
$prepareResultPath = Join-Path $ArtifactDir "curtain-panel-runtime-result.txt"
$scriptPath = Join-Path $ArtifactDir "curtain-panel-workspace-review.private.scr"
$metadataPath = Join-Path $ArtifactDir "curtain-panel-workspace-review-metadata.json"
$originalCopyPath = Join-Path $ArtifactDir "curtain-panel-workspace-review-original.private.dwg"
$uiLayoutBackupPath = Join-Path $ArtifactDir "curtain-panel-workspace-review-ui-layout.private.txt"
$uiLayoutPath = Join-Path ([Environment]::GetFolderPath([Environment+SpecialFolder]::LocalApplicationData)) "QS3D\BricsCAD-V25\ui-layout-v1.txt"
foreach ($output in @($resultPath, $progressPath, $prepareResultPath, $scriptPath, $metadataPath, $originalCopyPath, $uiLayoutBackupPath)) {
    if (Test-Path -LiteralPath $output) { throw "Curtain P10 output already exists." }
}
$runPrivateFiles = @($privateFiles) + @($prepareResultPath)

$drawingHashBefore = (Get-FileHash -LiteralPath $DrawingCopy -Algorithm SHA256).Hash.ToUpperInvariant()
$pluginHash = (Get-FileHash -LiteralPath $PluginDll -Algorithm SHA256).Hash.ToUpperInvariant()
Copy-Item -LiteralPath $DrawingCopy -Destination $originalCopyPath -ErrorAction Stop
if (-not [string]::Equals((Get-FileHash -LiteralPath $originalCopyPath -Algorithm SHA256).Hash, $drawingHashBefore, [StringComparison]::OrdinalIgnoreCase)) {
    throw "Curtain P10 private restore copy hash mismatch."
}
$uiLayoutExisted = Test-Path -LiteralPath $uiLayoutPath -PathType Leaf
$uiLayoutHashBefore = if ($uiLayoutExisted) {
    (Get-FileHash -LiteralPath $uiLayoutPath -Algorithm SHA256).Hash.ToUpperInvariant()
} else { "" }
if ($uiLayoutExisted) {
    Copy-Item -LiteralPath $uiLayoutPath -Destination $uiLayoutBackupPath -ErrorAction Stop
    if (-not [string]::Equals(
        (Get-FileHash -LiteralPath $uiLayoutBackupPath -Algorithm SHA256).Hash,
        $uiLayoutHashBefore,
        [StringComparison]::OrdinalIgnoreCase)) {
        throw "Curtain P10 private UI-layout backup hash mismatch."
    }
}

$nonce = [Guid]::NewGuid().ToString("N")
$oldResult = [Environment]::GetEnvironmentVariable("QS3D_CURTAIN_P10_RESULT", "Process")
$oldNonce = [Environment]::GetEnvironmentVariable("QS3D_CURTAIN_P10_NONCE", "Process")
$oldProgress = [Environment]::GetEnvironmentVariable("QS3D_CURTAIN_P10_PROGRESS", "Process")
$oldPrepareResult = [Environment]::GetEnvironmentVariable("QS3D_CURTAIN_PANEL_RESULT", "Process")
$oldPrepareNonce = [Environment]::GetEnvironmentVariable("QS3D_CURTAIN_PANEL_NONCE", "Process")
$process = $null
$launcherId = 0
$launcherHandoffs = 0
$proxyDialogsDismissed = 0
$startedAt = Get-Date
$qualificationError = $null
$cleanupError = $null
$marker = $null
$processCleanupVerified = $false
$scriptCleanupVerified = $false
$privateStateCleanupVerified = $false
$drawingRestoreVerified = $false
$uiLayoutRestoreVerified = $false
$lastProgressPhase = $null

try {
    $env:QS3D_CURTAIN_P10_RESULT = $resultPath
    $env:QS3D_CURTAIN_P10_NONCE = $nonce
    $env:QS3D_CURTAIN_P10_PROGRESS = $progressPath
    $env:QS3D_CURTAIN_PANEL_RESULT = $prepareResultPath
    $env:QS3D_CURTAIN_PANEL_NONCE = $nonce
    $script = @(
        "FILEDIA", "0",
        "CMDECHO", "1",
        "TILEMODE", "1",
        "INSUNITS", "4",
        "UCS", "W",
        "NETLOAD", ('"' + $PluginDll + '"'),
        "QS3DCURTAINP10PROGRESSLOAD",
        "QS3DDRAWGLASSWALL",
        "0,0", "5000,0", "",
        "QS3DCURTAINP10PROGRESSDRAW",
        "QS3DCURTAINPANELPREPARE",
        "QS3DCURTAINP10PROGRESSPREPARE",
        "QS3DCURTAIN3D",
        "QS3DCURTAINP10PROGRESSBUILD",
        "QS3DCURTAINP10SELECT",
        "QS3DCURTAINP10PROGRESSSELECT",
        "QS3D",
        "QS3DCURTAINP10PROGRESSWORKSPACE",
        "QS3DINSPECT",
        "QS3DCURTAINP10PROGRESSINSPECT",
        "QS3DCURTAINP10CHECKWORKSPACE",
        "QS3DCURTAINP10PROGRESSREVIEW",
        "QS3DHEALTHALL",
        "QS3DCURTAINP10PROGRESSHEALTH",
        "QS3DCURTAINP10CHECKHEALTH",
        "QS3DCURTAINP10PROGRESSHEALTHCHECK",
        "QS3DRELEASECHECK",
        "QS3DCURTAINP10PROGRESSRELEASE",
        "QS3DCURTAINP10COMPLETE",
        "QS3DHIDE",
        "_.QUIT", "_Y"
    )
    [IO.File]::WriteAllLines($scriptPath, $script, [Text.Encoding]::ASCII)
    $arguments = '"' + $DrawingCopy + '" /P "' + $Profile + '" /B "' + $scriptPath + '"'
    $process = Start-Process -FilePath $bricscadExe -ArgumentList $arguments -PassThru -WindowStyle Hidden -WorkingDirectory $ArtifactDir
    $launcherId = $process.Id
    $deadline = (Get-Date).AddSeconds($StartupTimeoutSeconds)
    $proxyDialogsDismissed += Wait-Qs3dMarker -Process ([ref]$process) -HandoffCount ([ref]$launcherHandoffs) -ExpectedExecutable $bricscadExe -ResultPath $resultPath -Deadline $deadline
    $marker = Read-Qs3dMarker -Path $resultPath
    if (-not $marker.ContainsKey("status") -or -not [string]::Equals([string]$marker.status, "PASS", [StringComparison]::OrdinalIgnoreCase)) {
        $phase = if ($marker.ContainsKey("failure_phase")) { [string]$marker.failure_phase } else { "UNKNOWN" }
        $code = if ($marker.ContainsKey("failure_code")) { [string]$marker.failure_code } else { "UNKNOWN" }
        throw "Curtain P10 licensed result failed: $phase/$code"
    }
    Require-Qs3dValue -Marker $marker -Key "command" -Expected "QS3DCURTAINP10COMPLETE"
    Require-Qs3dValue -Marker $marker -Key "process" -Expected "bricscad"
    Require-Qs3dValue -Marker $marker -Key "nonce" -Expected $nonce
    Require-Qs3dValue -Marker $marker -Key "schema" -Expected "QS3D_CURTAIN_PANEL_WORKSPACE_REVIEW_RUNTIME_V1"
    Require-Qs3dValue -Marker $marker -Key "qualification_boundary" -Expected "LOCAL_002_P10_ONLY"
    Require-Qs3dValue -Marker $marker -Key "production_local002_qualified" -Expected "false"
    Require-Qs3dValue -Marker $marker -Key "p10_qualified" -Expected "true"
    foreach ($key in @(
        "is_64bit", "owner_category_glasswall", "family_review_match", "instance_scope_active",
        "health_all_ready", "release_check_ready", "project_unchanged", "source_preserved", "panel_live")) {
        Require-Qs3dValue -Marker $marker -Key $key -Expected "true"
    }
    foreach ($key in @("selected_panel_count", "canonical_owner_count")) {
        Require-Qs3dValue -Marker $marker -Key $key -Expected "1"
    }
    Require-Qs3dValue -Marker $marker -Key "health_issue_count" -Expected "0"
}
catch { $qualificationError = $_ }
finally {
    try { Stop-Qs3dLaunchedProcess -Process $process; $processCleanupVerified = $true }
    catch { if ($null -eq $cleanupError) { $cleanupError = $_ } }
    try { Remove-ExactFile -Path $scriptPath; $scriptCleanupVerified = $true }
    catch { if ($null -eq $cleanupError) { $cleanupError = $_ } }
    foreach ($privateFile in $runPrivateFiles) {
        try { Remove-ExactFile -Path $privateFile }
        catch { if ($null -eq $cleanupError) { $cleanupError = $_ } }
    }
    try {
        Copy-Item -LiteralPath $originalCopyPath -Destination $DrawingCopy -Force -ErrorAction Stop
        $drawingRestoreVerified = [string]::Equals(
            (Get-FileHash -LiteralPath $DrawingCopy -Algorithm SHA256).Hash,
            $drawingHashBefore,
            [StringComparison]::OrdinalIgnoreCase)
        if (-not $drawingRestoreVerified) { throw "Curtain P10 drawing restore hash mismatch." }
        Remove-ExactFile -Path $originalCopyPath
    }
    catch { if ($null -eq $cleanupError) { $cleanupError = $_ } }
    try {
        if ($uiLayoutExisted) {
            Copy-Item -LiteralPath $uiLayoutBackupPath -Destination $uiLayoutPath -Force -ErrorAction Stop
            Remove-ExactFile -Path $uiLayoutBackupPath
        }
        else {
            Remove-ExactFile -Path $uiLayoutPath
        }
        $uiLayoutRestoreVerified = if ($uiLayoutExisted) {
            (Test-Path -LiteralPath $uiLayoutPath -PathType Leaf) -and
            [string]::Equals(
                (Get-FileHash -LiteralPath $uiLayoutPath -Algorithm SHA256).Hash,
                $uiLayoutHashBefore,
                [StringComparison]::OrdinalIgnoreCase)
        } else {
            -not (Test-Path -LiteralPath $uiLayoutPath)
        }
        if (-not $uiLayoutRestoreVerified) { throw "Curtain P10 UI-layout restore failed." }
    }
    catch { if ($null -eq $cleanupError) { $cleanupError = $_ } }
    try {
        if (@(Get-Process -Name "bricscad" -ErrorAction SilentlyContinue).Count -gt 0) {
            throw "Curtain P10 cleanup left a BricsCAD process."
        }
        $privateStateCleanupVerified = @($runPrivateFiles | Where-Object { Test-Path -LiteralPath $_ }).Count -eq 0
        if (-not $privateStateCleanupVerified) { throw "Curtain P10 private-state cleanup failed." }
    }
    catch { if ($null -eq $cleanupError) { $cleanupError = $_ } }
    Restore-EnvironmentValue -Name "QS3D_CURTAIN_P10_RESULT" -Value $oldResult
    Restore-EnvironmentValue -Name "QS3D_CURTAIN_P10_NONCE" -Value $oldNonce
    Restore-EnvironmentValue -Name "QS3D_CURTAIN_P10_PROGRESS" -Value $oldProgress
    Restore-EnvironmentValue -Name "QS3D_CURTAIN_PANEL_RESULT" -Value $oldPrepareResult
    Restore-EnvironmentValue -Name "QS3D_CURTAIN_PANEL_NONCE" -Value $oldPrepareNonce
}

try { $lastProgressPhase = Read-Qs3dProgressPhase -Path $progressPath }
catch { if ($null -eq $cleanupError) { $cleanupError = $_ } }

$metadata = [ordered]@{
    status = if ($null -eq $qualificationError -and $null -eq $cleanupError) { "PASS" } else { "FAIL" }
    git_sha = $gitHead
    started_at = $startedAt.ToUniversalTime().ToString("O")
    completed_at = (Get-Date).ToUniversalTime().ToString("O")
    bricscad_file_version = (Get-Item -LiteralPath $bricscadExe).VersionInfo.FileVersion
    plugin_sha256 = $pluginHash
    drawing_copy_sha256_before = $drawingHashBefore
    drawing_copy_sha256_restored = (Get-FileHash -LiteralPath $DrawingCopy -Algorithm SHA256).Hash.ToUpperInvariant()
    process_cleanup_verified = $processCleanupVerified
    script_cleanup_verified = $scriptCleanupVerified
    private_state_cleanup_verified = $privateStateCleanupVerified
    drawing_restore_verified = $drawingRestoreVerified
    ui_layout_restore_verified = $uiLayoutRestoreVerified
    proxy_information_dialogs_dismissed = $proxyDialogsDismissed
    launcher_handoffs = $launcherHandoffs
    last_progress_phase = $lastProgressPhase
    marker = $marker
}
$metadata | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $metadataPath -Encoding UTF8

if ($null -ne $cleanupError) { throw $cleanupError }
if ($null -ne $qualificationError) { throw $qualificationError }

Write-Host "QS3D BricsCAD V25 Curtain P10 Workspace review PASS"
Write-Host "Marker: $resultPath"
Write-Host "Metadata: $metadataPath"
