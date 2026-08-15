param(
    [Parameter(Mandatory = $true)][string]$BricsCadDir,
    [Parameter(Mandatory = $true)][string]$PluginDll,
    [Parameter(Mandatory = $true)][string]$DrawingCopy,
    [Parameter(Mandatory = $true)][string]$Profile,
    [Parameter(Mandatory = $true)][string]$ArtifactDir,
    [Parameter(Mandatory = $true)][string]$ExpectedSourceSha,
    [Parameter(Mandatory = $true)][switch]$ConfirmDisposableCopy,
    [ValidateRange(60, 1200)][int]$StartupTimeoutSeconds = 360
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$windowInteropPath = Join-Path $PSScriptRoot "bricscad-runner-window-interop.ps1"
if (-not (Test-Path -LiteralPath $windowInteropPath -PathType Leaf)) {
    throw "Level lifecycle runner window interop helper is missing."
}
. $windowInteropPath

function Read-Qs3dMarker {
    param([Parameter(Mandatory = $true)][string]$Path)
    $marker = @{}
    foreach ($line in Get-Content -LiteralPath $Path) {
        if ([string]::IsNullOrWhiteSpace($line)) { continue }
        $separator = $line.IndexOf('=')
        if ($separator -le 0) { throw "Malformed Level lifecycle marker line." }
        $key = $line.Substring(0, $separator).Trim()
        $value = $line.Substring($separator + 1).Trim()
        if ($marker.ContainsKey($key)) { throw "Duplicate Level lifecycle marker key." }
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
    if (-not $Marker.ContainsKey($Key)) { throw "Level lifecycle marker is missing '$Key'." }
    if (-not [string]::Equals([string]$Marker[$Key], $Expected, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Level lifecycle marker '$Key' did not match."
    }
}

function Read-Qs3dBoolean {
    param([Parameter(Mandatory = $true)]$Marker, [Parameter(Mandatory = $true)][string]$Key)
    if (-not $Marker.ContainsKey($Key)) { throw "Level lifecycle marker is missing '$Key'." }
    $value = [string]$Marker[$Key]
    if ([string]::Equals($value, "true", [StringComparison]::OrdinalIgnoreCase)) { return "true" }
    if ([string]::Equals($value, "false", [StringComparison]::OrdinalIgnoreCase)) { return "false" }
    throw "Level lifecycle marker '$Key' is not a sanitized boolean."
}

function Read-PositiveMarkerInt {
    param([Parameter(Mandatory = $true)]$Marker, [Parameter(Mandatory = $true)][string]$Key)
    if (-not $Marker.ContainsKey($Key)) { throw "Level lifecycle marker is missing '$Key'." }
    [int]$value = 0
    if (-not [int]::TryParse([string]$Marker[$Key], [Globalization.NumberStyles]::None, [Globalization.CultureInfo]::InvariantCulture, [ref]$value) -or $value -le 0) {
        throw "Level lifecycle marker '$Key' is not a positive integer."
    }
    return $value
}

function Read-NonNegativeMarkerLong {
    param([Parameter(Mandatory = $true)]$Marker, [Parameter(Mandatory = $true)][string]$Key)
    if (-not $Marker.ContainsKey($Key)) { throw "Level lifecycle marker is missing '$Key'." }
    [long]$value = 0
    if (-not [long]::TryParse([string]$Marker[$Key], [Globalization.NumberStyles]::None, [Globalization.CultureInfo]::InvariantCulture, [ref]$value) -or $value -lt 0) {
        throw "Level lifecycle marker '$Key' is not a non-negative integer."
    }
    return $value
}

function Read-FiniteMarkerDouble {
    param([Parameter(Mandatory = $true)]$Marker, [Parameter(Mandatory = $true)][string]$Key)
    if (-not $Marker.ContainsKey($Key)) { throw "Level lifecycle marker is missing '$Key'." }
    [double]$value = 0
    if (-not [double]::TryParse([string]$Marker[$Key], [Globalization.NumberStyles]::Float, [Globalization.CultureInfo]::InvariantCulture, [ref]$value) -or
        [double]::IsNaN($value) -or [double]::IsInfinity($value)) {
        throw "Level lifecycle marker '$Key' is not finite."
    }
    return $value
}

function Require-Near {
    param([Parameter(Mandatory = $true)][double]$Expected, [Parameter(Mandatory = $true)][double]$Actual, [Parameter(Mandatory = $true)][string]$Label)
    $tolerance = [Math]::Max(0.0000001, [Math]::Max([Math]::Abs($Expected), [Math]::Abs($Actual)) * 0.0000001)
    if ([Math]::Abs($Expected - $Actual) -gt $tolerance) { throw "Level lifecycle $Label did not match." }
}

function Read-UndoFailureCode {
    param([Parameter(Mandatory = $true)]$Marker)
    if (-not $Marker.ContainsKey("undo_failure_code")) { throw "Curtain P11 marker is missing its Undo code." }
    $code = [string]$Marker["undo_failure_code"]
    foreach ($allowed in @("NONE", "UNDO_AFTER_GENERATED_STILL_PRESENT", "UNDO_NATIVE_REMOVED_SEMANTIC_NOT_RESTORED", "UNDO_SOURCE_SENTINEL_DRIFT")) {
        if ([string]::Equals($code, $allowed, [StringComparison]::Ordinal)) { return $code }
    }
    throw "Curtain P11 marker Undo code is not allowlisted."
}

function Restore-EnvironmentValue {
    param([Parameter(Mandatory = $true)][string]$Name, [AllowNull()][string]$Value)
    if ($null -eq $Value) { Remove-Item -LiteralPath ("Env:" + $Name) -ErrorAction SilentlyContinue }
    else { Set-Item -LiteralPath ("Env:" + $Name) -Value $Value }
}

function Stop-Qs3dOwnedProcess {
    param([AllowNull()][Diagnostics.Process]$Process)
    if ($null -eq $Process) { return }
    $Process.Refresh()
    if (-not $Process.HasExited) {
        Stop-Process -Id $Process.Id -Force -ErrorAction Stop
        $Process.WaitForExit(15000) | Out-Null
        $Process.Refresh()
    }
    if (-not $Process.HasExited) { throw "Test-owned Level lifecycle BricsCAD process did not exit." }
}

function Wait-Qs3dMarkerSet {
    param(
        [Parameter(Mandatory = $true)][Diagnostics.Process]$Process,
        [Parameter(Mandatory = $true)][string[]]$ExpectedPaths,
        [Parameter(Mandatory = $true)][string[]]$FailurePaths,
        [Parameter(Mandatory = $true)][DateTime]$Deadline
    )
    $dismissed = 0
    while ((Get-Date) -lt $Deadline) {
        $allExpected = $true
        foreach ($path in $ExpectedPaths) {
            if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { $allExpected = $false; break }
        }
        if ($allExpected) { return $dismissed }
        foreach ($path in $FailurePaths) {
            if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { continue }
            $failed = Read-Qs3dMarker -Path $path
            if ($failed.ContainsKey("status") -and [string]::Equals([string]$failed["status"], "FAIL", [StringComparison]::OrdinalIgnoreCase)) {
                return $dismissed
            }
        }
        $dismissed += Close-Qs3dProxyInformationDialog -Process $Process
        $Process.Refresh()
        if ($Process.HasExited) { throw "BricsCAD exited before the Level lifecycle marker set completed." }
        Start-Sleep -Milliseconds 500
    }
    throw "Timed out waiting for the Level lifecycle marker set."
}

function Remove-ExactFile {
    param([Parameter(Mandatory = $true)][string]$Path)
    if (Test-Path -LiteralPath $Path) { Remove-Item -LiteralPath $Path -Force -ErrorAction Stop }
    if (Test-Path -LiteralPath $Path) { throw "Level lifecycle exact private-file cleanup failed." }
}

if ([Environment]::OSVersion.Platform -ne [PlatformID]::Win32NT) { throw "Level lifecycle qualification requires Windows." }
if (-not [Environment]::UserInteractive) { throw "Level lifecycle qualification requires an interactive Windows session." }
if (-not $ConfirmDisposableCopy) { throw "Pass -ConfirmDisposableCopy only for a synthetic disposable drawing." }
if ([string]::IsNullOrWhiteSpace($Profile)) { throw "Level lifecycle qualification requires an initialized profile." }

$repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot ".."))
$BricsCadDir = [IO.Path]::GetFullPath($BricsCadDir)
$PluginDll = [IO.Path]::GetFullPath($PluginDll)
$DrawingCopy = [IO.Path]::GetFullPath($DrawingCopy)
$ArtifactDir = [IO.Path]::GetFullPath($ArtifactDir)
$ExpectedSourceSha = $ExpectedSourceSha.Trim().ToLowerInvariant()
if ($ExpectedSourceSha -notmatch '^[0-9a-f]{40}$') { throw "ExpectedSourceSha must be a full Git SHA." }
if (-not [IO.Path]::GetFileName($DrawingCopy).EndsWith(".level-z-lifecycle-probe-copy.dwg", [StringComparison]::OrdinalIgnoreCase)) {
    throw "DrawingCopy must use the guarded '*.level-z-lifecycle-probe-copy.dwg' suffix."
}
if ($DrawingCopy.StartsWith($repoRoot + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
    throw "DrawingCopy must stay outside the repository."
}
if ($ArtifactDir.StartsWith($repoRoot + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
    throw "ArtifactDir must stay outside the repository."
}

$bricscadExe = Join-Path $BricsCadDir "bricscad.exe"
$coreDll = Join-Path (Split-Path -Parent $PluginDll) "QS3D.Core.dll"
foreach ($required in @($bricscadExe, $PluginDll, $coreDll, $DrawingCopy)) {
    if (-not (Test-Path -LiteralPath $required -PathType Leaf)) { throw "Required Level lifecycle input is missing." }
}
$expectedPlugin = [IO.Path]::GetFullPath((Join-Path $repoRoot "src\QS3D.BricsCAD.V25\bin\x64\Release\net48\QS3D.BricsCAD.V25.dll"))
if (-not [string]::Equals($PluginDll, $expectedPlugin, [StringComparison]::OrdinalIgnoreCase)) {
    throw "PluginDll must be the exact repository V25 x64 Release output."
}

$git = Get-Command git -CommandType Application -ErrorAction Stop | Select-Object -First 1
if ($null -eq $git -or [string]::IsNullOrWhiteSpace($git.Source)) { throw "Git executable is unavailable." }
$gitOutput = @(& $git.Source -C $repoRoot rev-parse HEAD 2>$null)
$gitExitCode = $LASTEXITCODE
if ($gitExitCode -ne 0 -or $gitOutput.Count -ne 1) { throw "Cannot resolve the Level lifecycle Git SHA." }
$gitHead = ([string]$gitOutput[0]).Trim().ToLowerInvariant()
if (-not [string]::Equals($gitHead, $ExpectedSourceSha, [StringComparison]::Ordinal)) {
    throw "ExpectedSourceSha does not match the worktree HEAD."
}
$gitStatus = @(& $git.Source -C $repoRoot status --porcelain=v1 --untracked-files=all 2>$null)
$gitStatusExitCode = $LASTEXITCODE
if ($gitStatusExitCode -ne 0) { throw "Cannot inspect the Level lifecycle worktree." }
if ($gitStatus.Count -ne 0) { throw "Level lifecycle qualification requires a clean committed worktree." }
$expectedAssemblyRevision = "+" + $ExpectedSourceSha
foreach ($assemblyPath in @($PluginDll, $coreDll)) {
    $productVersion = [string](Get-Item -LiteralPath $assemblyPath).VersionInfo.ProductVersion
    if (-not $productVersion.EndsWith($expectedAssemblyRevision, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Level lifecycle assembly was not built from ExpectedSourceSha."
    }
}
if (@(Get-Process -Name "bricscad" -ErrorAction SilentlyContinue).Count -gt 0) {
    throw "Close all BricsCAD processes before isolated Level lifecycle qualification."
}

$projectSidecar = [IO.Path]::ChangeExtension($DrawingCopy, ".qsdb")
$sidecarBackup = $projectSidecar + ".bak"
$sidecarLock = $projectSidecar + ".lock"
$drawingLock = [IO.Path]::ChangeExtension($DrawingCopy, ".dwl")
$drawingLock2 = [IO.Path]::ChangeExtension($DrawingCopy, ".dwl2")
$drawingBackup = [IO.Path]::ChangeExtension($DrawingCopy, ".bak")
foreach ($privateInput in @($projectSidecar, $sidecarBackup, $sidecarLock, $drawingLock, $drawingLock2, $drawingBackup)) {
    if (Test-Path -LiteralPath $privateInput) { throw "Level lifecycle disposable copy has pre-existing private state." }
}

if (Test-Path -LiteralPath $ArtifactDir) {
    if (@(Get-ChildItem -LiteralPath $ArtifactDir -Force).Count -ne 0) { throw "ArtifactDir must be empty." }
}
else { New-Item -ItemType Directory -Path $ArtifactDir | Out-Null }

$levelResultPath = Join-Path $ArtifactDir "level-z-lifecycle-result.txt"
$levelPhasePath = Join-Path $ArtifactDir "level-z-lifecycle-session1.txt"
$p11ResultPath = Join-Path $ArtifactDir "curtain-panel-undo-reopen-result.txt"
$p11PhasePath = Join-Path $ArtifactDir "curtain-panel-undo-reopen-session1.txt"
$scriptOnePath = Join-Path $ArtifactDir "level-z-lifecycle-session1.scr"
$scriptTwoPath = Join-Path $ArtifactDir "level-z-lifecycle-session2.scr"
$metadataPath = Join-Path $ArtifactDir "level-z-lifecycle-metadata.json"
$originalCopyPath = Join-Path $ArtifactDir "level-z-lifecycle-original.private.dwg"
foreach ($output in @($levelResultPath, $levelPhasePath, $p11ResultPath, $p11PhasePath, $scriptOnePath, $scriptTwoPath, $metadataPath, $originalCopyPath)) {
    if (Test-Path -LiteralPath $output) { throw "Level lifecycle output already exists." }
}

$drawingHashBefore = (Get-FileHash -LiteralPath $DrawingCopy -Algorithm SHA256).Hash.ToUpperInvariant()
$pluginHash = (Get-FileHash -LiteralPath $PluginDll -Algorithm SHA256).Hash.ToUpperInvariant()
$coreHash = (Get-FileHash -LiteralPath $coreDll -Algorithm SHA256).Hash.ToUpperInvariant()
Copy-Item -LiteralPath $DrawingCopy -Destination $originalCopyPath -ErrorAction Stop
if (-not [string]::Equals((Get-FileHash -LiteralPath $originalCopyPath -Algorithm SHA256).Hash, $drawingHashBefore, [StringComparison]::OrdinalIgnoreCase)) {
    throw "Level lifecycle private restore copy hash mismatch."
}

$nonce = [Guid]::NewGuid().ToString("N")
$environmentNames = @(
    "QS3D_LEVEL_Z_LIFECYCLE_RESULT",
    "QS3D_LEVEL_Z_LIFECYCLE_PHASE_RESULT",
    "QS3D_LEVEL_Z_LIFECYCLE_NONCE",
    "QS3D_LEVEL_Z_LIFECYCLE_SOURCE_SHA",
    "QS3D_LEVEL_Z_LIFECYCLE_EXPECTED_HOSTS",
    "QS3D_LEVEL_Z_LIFECYCLE_EXPECTED_FRAMES",
    "QS3D_LEVEL_Z_LIFECYCLE_EXPECTED_PANELS",
    "QS3D_CURTAIN_P11_RESULT",
    "QS3D_CURTAIN_P11_PHASE_RESULT",
    "QS3D_CURTAIN_P11_NONCE",
    "QS3D_CURTAIN_P11_EXPECTED_HOSTS",
    "QS3D_CURTAIN_P11_EXPECTED_FRAMES",
    "QS3D_CURTAIN_P11_EXPECTED_PANELS",
    "QS3D_CURTAIN_P11_UNDO_COHERENT",
    "QS3D_CURTAIN_P11_REDO_COHERENT",
    "QS3D_CURTAIN_P11_UNDO_AFTER_GENERATED_ABSENT",
    "QS3D_CURTAIN_P11_UNDO_SEMANTIC_BEFORE_RESTORED",
    "QS3D_CURTAIN_P11_UNDO_SOURCE_SENTINEL_PRESERVED",
    "QS3D_CURTAIN_P11_UNDO_FAILURE_CODE"
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
$levelPhaseMarker = $null
$levelFinalMarker = $null
$p11PhaseMarker = $null
$p11FinalMarker = $null
$savedDrawingHash = ""
$rebuiltDrawingHash = ""

try {
    $env:QS3D_LEVEL_Z_LIFECYCLE_RESULT = $levelResultPath
    $env:QS3D_LEVEL_Z_LIFECYCLE_PHASE_RESULT = $levelPhasePath
    $env:QS3D_LEVEL_Z_LIFECYCLE_NONCE = $nonce
    $env:QS3D_LEVEL_Z_LIFECYCLE_SOURCE_SHA = $ExpectedSourceSha
    $env:QS3D_CURTAIN_P11_RESULT = $p11ResultPath
    $env:QS3D_CURTAIN_P11_PHASE_RESULT = $p11PhasePath
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
        "QS3DLEVELZLIFECYCLECONFIGURE",
        "QS3DCURTAINP11PREPARE",
        "QS3DCURTAINP11SELECT",
        "_.UNDO", "_Mark",
        "QS3DCURTAIN3D", "P", "",
        "QS3DCURTAINP11BASELINE",
        "QS3DLEVELZLIFECYCLEBASELINE",
        "_.UNDO", "_Back",
        "QS3DCURTAINP11CHECKUNDO",
        "QS3DLEVELZLIFECYCLECHECKUNDO",
        "QS3DCURTAINP11SELECT",
        "_.UNDO", "_Begin",
        "QS3DCURTAIN3D", "P", "",
        "QS3DCURTAINP11BASELINE",
        "QS3DLEVELZLIFECYCLEBASELINE",
        "_.UNDO", "_End",
        "_.U",
        "_.REDO",
        "QS3DCURTAINP11CHECKREDO",
        "QS3DLEVELZLIFECYCLECHECKREDO",
        "QS3DSAVE",
        "_.QSAVE",
        "QS3DCURTAINP11SESSION1",
        "QS3DLEVELZLIFECYCLESESSION1"
    )
    Set-Content -LiteralPath $scriptOnePath -Value $scriptOne -Encoding ASCII
    $argumentsOne = '"' + $DrawingCopy + '" /P "' + $Profile + '" /B "' + $scriptOnePath + '"'
    $processOne = Start-Process -FilePath $bricscadExe -ArgumentList $argumentsOne -PassThru -WindowStyle Hidden -WorkingDirectory $ArtifactDir
    $proxyInformationDialogsDismissed += Wait-Qs3dMarkerSet -Process $processOne -ExpectedPaths @($p11PhasePath, $levelPhasePath) -FailurePaths @($p11ResultPath, $levelResultPath) -Deadline (Get-Date).AddSeconds($StartupTimeoutSeconds)
    if (-not (Test-Path -LiteralPath $p11PhasePath -PathType Leaf) -or -not (Test-Path -LiteralPath $levelPhasePath -PathType Leaf)) {
        throw "Level lifecycle session one returned a sanitized failure marker."
    }

    $p11PhaseMarker = Read-Qs3dMarker -Path $p11PhasePath
    Require-Qs3dValue -Marker $p11PhaseMarker -Key "status" -Expected "PASS"
    Require-Qs3dValue -Marker $p11PhaseMarker -Key "command" -Expected "QS3DCURTAINP11SESSION1"
    Require-Qs3dValue -Marker $p11PhaseMarker -Key "nonce" -Expected $nonce
    Require-Qs3dValue -Marker $p11PhaseMarker -Key "schema" -Expected "QS3D_CURTAIN_PANEL_UNDO_REOPEN_RUNTIME_V1"
    Require-Qs3dValue -Marker $p11PhaseMarker -Key "undo_coherent" -Expected "true"
    Require-Qs3dValue -Marker $p11PhaseMarker -Key "redo_coherent" -Expected "true"
    Require-Qs3dValue -Marker $p11PhaseMarker -Key "undo_after_generated_absent" -Expected "true"
    Require-Qs3dValue -Marker $p11PhaseMarker -Key "undo_semantic_before_restored" -Expected "true"
    Require-Qs3dValue -Marker $p11PhaseMarker -Key "undo_source_sentinel_preserved" -Expected "true"
    Require-Qs3dValue -Marker $p11PhaseMarker -Key "health_issue_count" -Expected "0"
    $undoFailureCode = Read-UndoFailureCode -Marker $p11PhaseMarker
    if (-not [string]::Equals($undoFailureCode, "NONE", [StringComparison]::Ordinal)) { throw "Curtain P11 Undo branch failed." }
    $hostCount = Read-PositiveMarkerInt -Marker $p11PhaseMarker -Key "host_solid_count"
    $frameCount = Read-PositiveMarkerInt -Marker $p11PhaseMarker -Key "frame_solid_count"
    $panelCount = Read-PositiveMarkerInt -Marker $p11PhaseMarker -Key "panel_solid_count"
    $null = Read-NonNegativeMarkerLong -Marker $p11PhaseMarker -Key "change_version"

    $levelPhaseMarker = Read-Qs3dMarker -Path $levelPhasePath
    Require-Qs3dValue -Marker $levelPhaseMarker -Key "status" -Expected "PASS"
    Require-Qs3dValue -Marker $levelPhaseMarker -Key "command" -Expected "QS3DLEVELZLIFECYCLESESSION1"
    Require-Qs3dValue -Marker $levelPhaseMarker -Key "nonce" -Expected $nonce
    Require-Qs3dValue -Marker $levelPhaseMarker -Key "source_sha" -Expected $ExpectedSourceSha
    Require-Qs3dValue -Marker $levelPhaseMarker -Key "schema" -Expected "QS3D_LEVEL_Z_LIFECYCLE_RUNTIME_V1"
    Require-Qs3dValue -Marker $levelPhaseMarker -Key "production_local003_qualified" -Expected "false"
    Require-Qs3dValue -Marker $levelPhaseMarker -Key "native_drawing_unit" -Expected "Millimeter"
    foreach ($key in @("undo_level_config_preserved", "undo_prebuild_host_restored", "undo_generated_after_absent", "redo_level_output_coherent")) {
        Require-Qs3dValue -Marker $levelPhaseMarker -Key $key -Expected "true"
    }
    Require-Qs3dValue -Marker $levelPhaseMarker -Key "level_health_issue_count" -Expected "0"
    if ((Read-PositiveMarkerInt -Marker $levelPhaseMarker -Key "host_solid_count") -ne $hostCount -or
        (Read-PositiveMarkerInt -Marker $levelPhaseMarker -Key "frame_solid_count") -ne $frameCount -or
        (Read-PositiveMarkerInt -Marker $levelPhaseMarker -Key "panel_solid_count") -ne $panelCount) {
        throw "Level lifecycle and Curtain P11 session-one counts differ."
    }
    Require-Near -Expected 3.1 -Actual (Read-FiniteMarkerDouble -Marker $levelPhaseMarker -Key "bounded_host_bottom_m") -Label "session-one bottom"
    Require-Near -Expected 6.8 -Actual (Read-FiniteMarkerDouble -Marker $levelPhaseMarker -Key "bounded_host_top_m") -Label "session-one top"

    Stop-Qs3dOwnedProcess -Process $processOne
    Remove-ExactFile -Path $scriptOnePath
    if (-not (Test-Path -LiteralPath $projectSidecar -PathType Leaf)) { throw "Level lifecycle session one did not persist a QSDB sidecar." }
    $savedDrawingHash = (Get-FileHash -LiteralPath $DrawingCopy -Algorithm SHA256).Hash.ToUpperInvariant()
    if ([string]::Equals($savedDrawingHash, $drawingHashBefore, [StringComparison]::Ordinal)) {
        throw "Level lifecycle session one did not save the DWG."
    }

    $env:QS3D_LEVEL_Z_LIFECYCLE_EXPECTED_HOSTS = $hostCount.ToString([Globalization.CultureInfo]::InvariantCulture)
    $env:QS3D_LEVEL_Z_LIFECYCLE_EXPECTED_FRAMES = $frameCount.ToString([Globalization.CultureInfo]::InvariantCulture)
    $env:QS3D_LEVEL_Z_LIFECYCLE_EXPECTED_PANELS = $panelCount.ToString([Globalization.CultureInfo]::InvariantCulture)
    $env:QS3D_CURTAIN_P11_EXPECTED_HOSTS = $hostCount.ToString([Globalization.CultureInfo]::InvariantCulture)
    $env:QS3D_CURTAIN_P11_EXPECTED_FRAMES = $frameCount.ToString([Globalization.CultureInfo]::InvariantCulture)
    $env:QS3D_CURTAIN_P11_EXPECTED_PANELS = $panelCount.ToString([Globalization.CultureInfo]::InvariantCulture)
    $env:QS3D_CURTAIN_P11_UNDO_COHERENT = "true"
    $env:QS3D_CURTAIN_P11_REDO_COHERENT = "true"
    $env:QS3D_CURTAIN_P11_UNDO_AFTER_GENERATED_ABSENT = "true"
    $env:QS3D_CURTAIN_P11_UNDO_SEMANTIC_BEFORE_RESTORED = "true"
    $env:QS3D_CURTAIN_P11_UNDO_SOURCE_SENTINEL_PRESERVED = "true"
    $env:QS3D_CURTAIN_P11_UNDO_FAILURE_CODE = "NONE"

    $scriptTwo = @(
        "FILEDIA", "0",
        "CMDECHO", "1",
        "TILEMODE", "1",
        "INSUNITS", "4",
        "UCS", "W",
        "NETLOAD", ('"' + $PluginDll + '"'),
        "QS3DCURTAINP11REOPEN",
        "QS3DLEVELZLIFECYCLEREOPEN",
        "QS3DCURTAINP11SELECT",
        "QS3DCURTAIN3D", "P", "",
        "QS3DCURTAINP11AFTERREBUILD",
        "QS3DLEVELZLIFECYCLEAFTERREBUILD",
        "QS3DSAVE",
        "_.QSAVE",
        "QS3DCURTAINP11COMPLETE",
        "QS3DLEVELZLIFECYCLECOMPLETE"
    )
    Set-Content -LiteralPath $scriptTwoPath -Value $scriptTwo -Encoding ASCII
    $argumentsTwo = '"' + $DrawingCopy + '" /P "' + $Profile + '" /B "' + $scriptTwoPath + '"'
    $processTwo = Start-Process -FilePath $bricscadExe -ArgumentList $argumentsTwo -PassThru -WindowStyle Hidden -WorkingDirectory $ArtifactDir
    $proxyInformationDialogsDismissed += Wait-Qs3dMarkerSet -Process $processTwo -ExpectedPaths @($p11ResultPath, $levelResultPath) -FailurePaths @($p11ResultPath, $levelResultPath) -Deadline (Get-Date).AddSeconds($StartupTimeoutSeconds)
    if (-not (Test-Path -LiteralPath $p11ResultPath -PathType Leaf) -or -not (Test-Path -LiteralPath $levelResultPath -PathType Leaf)) {
        throw "Level lifecycle session two did not publish both final markers."
    }

    $p11FinalMarker = Read-Qs3dMarker -Path $p11ResultPath
    Require-Qs3dValue -Marker $p11FinalMarker -Key "status" -Expected "PASS"
    Require-Qs3dValue -Marker $p11FinalMarker -Key "command" -Expected "QS3DCURTAINP11COMPLETE"
    Require-Qs3dValue -Marker $p11FinalMarker -Key "nonce" -Expected $nonce
    Require-Qs3dValue -Marker $p11FinalMarker -Key "schema" -Expected "QS3D_CURTAIN_PANEL_UNDO_REOPEN_RUNTIME_V1"
    Require-Qs3dValue -Marker $p11FinalMarker -Key "production_local002_qualified" -Expected "false"
    foreach ($key in @("undo_coherent", "redo_coherent", "undo_after_generated_absent", "undo_semantic_before_restored", "undo_source_sentinel_preserved", "reopen_coherent", "rebuild_coherent", "source_preserved", "sentinel_preserved", "old_generated_removed", "new_generated_disjoint", "rebuild_counts_stable", "p11_qualified")) {
        Require-Qs3dValue -Marker $p11FinalMarker -Key $key -Expected "true"
    }
    Require-Qs3dValue -Marker $p11FinalMarker -Key "undo_failure_code" -Expected "NONE"
    Require-Qs3dValue -Marker $p11FinalMarker -Key "health_issue_count" -Expected "0"

    $levelFinalMarker = Read-Qs3dMarker -Path $levelResultPath
    Require-Qs3dValue -Marker $levelFinalMarker -Key "status" -Expected "PASS"
    Require-Qs3dValue -Marker $levelFinalMarker -Key "command" -Expected "QS3DLEVELZLIFECYCLECOMPLETE"
    Require-Qs3dValue -Marker $levelFinalMarker -Key "nonce" -Expected $nonce
    Require-Qs3dValue -Marker $levelFinalMarker -Key "source_sha" -Expected $ExpectedSourceSha
    Require-Qs3dValue -Marker $levelFinalMarker -Key "schema" -Expected "QS3D_LEVEL_Z_LIFECYCLE_RUNTIME_V1"
    Require-Qs3dValue -Marker $levelFinalMarker -Key "production_local003_qualified" -Expected "false"
    Require-Qs3dValue -Marker $levelFinalMarker -Key "native_drawing_unit" -Expected "Millimeter"
    foreach ($key in @("reopen_level_config_coherent", "reopen_level_output_coherent", "rebuild_level_output_coherent", "old_generated_removed", "new_generated_disjoint", "rebuild_counts_stable", "level_lifecycle_qualified")) {
        Require-Qs3dValue -Marker $levelFinalMarker -Key $key -Expected "true"
    }
    Require-Qs3dValue -Marker $levelFinalMarker -Key "level_health_issue_count" -Expected "0"
    Require-Near -Expected 3.1 -Actual (Read-FiniteMarkerDouble -Marker $levelFinalMarker -Key "bounded_host_bottom_m") -Label "final bottom"
    Require-Near -Expected 6.8 -Actual (Read-FiniteMarkerDouble -Marker $levelFinalMarker -Key "bounded_host_top_m") -Label "final top"
    foreach ($prefix in @("reopened", "rebuilt")) {
        if ((Read-PositiveMarkerInt -Marker $levelFinalMarker -Key ($prefix + "_host_count")) -ne $hostCount -or
            (Read-PositiveMarkerInt -Marker $levelFinalMarker -Key ($prefix + "_frame_count")) -ne $frameCount -or
            (Read-PositiveMarkerInt -Marker $levelFinalMarker -Key ($prefix + "_panel_count")) -ne $panelCount) {
            throw "Level lifecycle final counts changed."
        }
    }

    Stop-Qs3dOwnedProcess -Process $processTwo
    Remove-ExactFile -Path $scriptTwoPath
    if (-not (Test-Path -LiteralPath $projectSidecar -PathType Leaf)) { throw "Level lifecycle rebuild lost the QSDB sidecar." }
    $rebuiltDrawingHash = (Get-FileHash -LiteralPath $DrawingCopy -Algorithm SHA256).Hash.ToUpperInvariant()
}
catch {
    $qualificationError = $_
}
finally {
    $processesStopped = $true
    foreach ($launched in @($processOne, $processTwo)) {
        try { Stop-Qs3dOwnedProcess -Process $launched }
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
        foreach ($privatePath in @($projectSidecar, $sidecarBackup, $sidecarLock, $drawingLock, $drawingLock2, $drawingBackup)) {
            try { Remove-ExactFile -Path $privatePath }
            catch { if ($null -eq $cleanupError) { $cleanupError = $_ } }
        }
        if (Test-Path -LiteralPath $originalCopyPath -PathType Leaf) {
            try {
                Copy-Item -LiteralPath $originalCopyPath -Destination $DrawingCopy -Force -ErrorAction Stop
                if (-not [string]::Equals((Get-FileHash -LiteralPath $DrawingCopy -Algorithm SHA256).Hash, $drawingHashBefore, [StringComparison]::OrdinalIgnoreCase)) {
                    throw "Level lifecycle disposable drawing restore hash mismatch."
                }
                Remove-ExactFile -Path $originalCopyPath
            }
            catch { if ($null -eq $cleanupError) { $cleanupError = $_ } }
        }
        try {
            if (@(Get-Process -Name "bricscad" -ErrorAction SilentlyContinue).Count -gt 0) {
                throw "Level lifecycle cleanup left a BricsCAD process."
            }
            $processCleanupVerified = $true
        }
        catch { if ($null -eq $cleanupError) { $cleanupError = $_ } }
    }
    foreach ($name in $environmentNames) { Restore-EnvironmentValue -Name $name -Value $oldEnvironment[$name] }
}

$metadataStatus = if ($null -eq $qualificationError -and $null -eq $cleanupError) { "PASS" } else { "FAIL" }
$metadata = [ordered]@{
    status = $metadataStatus
    git_sha = $gitHead
    started_at = $startedAt.ToUniversalTime().ToString("O")
    completed_at = (Get-Date).ToUniversalTime().ToString("O")
    bricscad_file_version = (Get-Item -LiteralPath $bricscadExe).VersionInfo.FileVersion
    plugin_sha256 = $pluginHash
    core_sha256 = $coreHash
    drawing_copy_sha256_before = $drawingHashBefore
    drawing_copy_sha256_saved = $savedDrawingHash
    drawing_copy_sha256_rebuilt = $rebuiltDrawingHash
    drawing_copy_sha256_restored = (Get-FileHash -LiteralPath $DrawingCopy -Algorithm SHA256).Hash.ToUpperInvariant()
    process_cleanup_verified = $processCleanupVerified
    script_cleanup_verified = (-not (Test-Path -LiteralPath $scriptOnePath) -and -not (Test-Path -LiteralPath $scriptTwoPath))
    sidecar_cleanup_verified = (-not (Test-Path -LiteralPath $projectSidecar) -and -not (Test-Path -LiteralPath $sidecarBackup) -and -not (Test-Path -LiteralPath $sidecarLock))
    drawing_lock_cleanup_verified = (-not (Test-Path -LiteralPath $drawingLock) -and -not (Test-Path -LiteralPath $drawingLock2))
    drawing_backup_cleanup_verified = (-not (Test-Path -LiteralPath $drawingBackup))
    drawing_restore_verified = [string]::Equals((Get-FileHash -LiteralPath $DrawingCopy -Algorithm SHA256).Hash, $drawingHashBefore, [StringComparison]::OrdinalIgnoreCase)
    proxy_information_dialogs_dismissed = $proxyInformationDialogsDismissed
    marker = [ordered]@{
        curtain_p11_session1 = if ($null -ne $p11PhaseMarker) { $p11PhaseMarker } else { @{} }
        level_session1 = if ($null -ne $levelPhaseMarker) { $levelPhaseMarker } else { @{} }
        curtain_p11_final = if ($null -ne $p11FinalMarker) { $p11FinalMarker } else { @{} }
        level_final = if ($null -ne $levelFinalMarker) { $levelFinalMarker } else { @{} }
    }
}
$metadata | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $metadataPath -Encoding UTF8

if ($null -ne $cleanupError) { throw $cleanupError }
if ($null -ne $qualificationError) { throw $qualificationError }

Write-Host "QS3D BricsCAD V25 Level Z Undo/save-reopen lifecycle PASS"
Write-Host "Level marker: $levelResultPath"
Write-Host "Curtain P11 marker: $p11ResultPath"
Write-Host "Metadata: $metadataPath"
