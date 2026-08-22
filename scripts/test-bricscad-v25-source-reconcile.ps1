param(
    [Parameter(Mandatory = $true)][string]$BricsCadDir,
    [Parameter(Mandatory = $true)][string]$PluginDll,
    [Parameter(Mandatory = $true)][string]$FixtureDwg,
    [Parameter(Mandatory = $true)][string]$Profile,
    [Parameter(Mandatory = $true)][string]$ArtifactDir,
    [Parameter(Mandatory = $true)][switch]$ConfirmDisposableCopies,
    [ValidateRange(120, 1800)][int]$StartupTimeoutSeconds = 600
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$windowInteropPath = Join-Path $PSScriptRoot "bricscad-runner-window-interop.ps1"
if (-not (Test-Path -LiteralPath $windowInteropPath -PathType Leaf)) { throw "LOCAL-004 runner window interop helper is missing." }
. $windowInteropPath

function Read-Qs3dMarker {
    param([Parameter(Mandatory = $true)][string]$Path)
    $marker = @{}
    foreach ($line in Get-Content -LiteralPath $Path) {
        if ([string]::IsNullOrWhiteSpace($line)) { continue }
        $separator = $line.IndexOf('=')
        if ($separator -le 0) { throw "Malformed LOCAL-004 marker line." }
        $key = $line.Substring(0, $separator).Trim()
        $value = $line.Substring($separator + 1).Trim()
        if ($marker.ContainsKey($key)) { throw "Duplicate LOCAL-004 marker key." }
        $marker[$key] = $value
    }
    return $marker
}

function Require-Qs3dValue {
    param([Parameter(Mandatory = $true)]$Marker, [Parameter(Mandatory = $true)][string]$Key, [Parameter(Mandatory = $true)][string]$Expected)
    if (-not $Marker.ContainsKey($Key)) { throw "LOCAL-004 marker is missing a required field." }
    if (-not [string]::Equals([string]$Marker[$Key], $Expected, [StringComparison]::OrdinalIgnoreCase)) {
        throw "LOCAL-004 marker field did not match its required value."
    }
}

function Require-Qs3dAllowedValue {
    param(
        [Parameter(Mandatory = $true)]$Marker,
        [Parameter(Mandatory = $true)][string]$Key,
        [Parameter(Mandatory = $true)][string[]]$Allowed
    )
    if (-not $Marker.ContainsKey($Key)) { throw "LOCAL-004 marker is missing a sanitized diagnostic field." }
    $actual = [string]$Marker[$Key]
    if (-not ($Allowed -contains $actual)) { throw "LOCAL-004 marker contains an invalid sanitized diagnostic classification." }
}

function Require-FinalReconcileDiagnostics {
    param([Parameter(Mandatory = $true)]$Marker)
    Require-Qs3dAllowedValue $Marker "final_selection_class" @("BOTH_SOURCES", "LINE_ONLY", "POLY_ONLY", "OTHER_OR_MISSING")
    Require-Qs3dAllowedValue $Marker "final_owner_match_class" @("BOTH", "LINE_ONLY", "POLY_ONLY", "NONE")
    Require-Qs3dAllowedValue $Marker "final_generated_state" @("REMOVED_ALL", "RETAINED_ALL", "PARTIAL")
    Require-Qs3dAllowedValue $Marker "final_project_state" @("CHANGED", "UNCHANGED")
    Require-Qs3dAllowedValue $Marker "final_revision_state" @("ADVANCED", "UNCHANGED", "REGRESSED_OR_RESET")
    Require-Qs3dAllowedValue $Marker "final_native_marker_state" @("ADVANCED", "UNCHANGED", "MISSING_OR_INVALID")
    Require-Qs3dAllowedValue $Marker "final_history_before_state" @("NONE", "SYNCED", "MARKER_MISMATCH", "DESYNCHRONIZED")
    Require-Qs3dAllowedValue $Marker "final_history_after_state" @("NONE", "SYNCED", "MARKER_MISMATCH", "DESYNCHRONIZED")
    Require-Qs3dAllowedValue $Marker "final_history_entry_before_class" @("ONE", "MULTIPLE")
    Require-Qs3dAllowedValue $Marker "final_history_entry_after_class" @("ONE", "MULTIPLE")
}

function Require-PostUndoMarkerDiagnostics {
    param(
        [Parameter(Mandatory = $true)]$Marker,
        [Parameter(Mandatory = $true)][string]$Nonce,
        [switch]$RequireRedo
    )
    $expectedKeys = @(
        "status", "schema", "qualification_boundary", "nonce",
        "post_undo_marker_class"
    )
    $hasRedo = $Marker.ContainsKey("post_redo_marker_class")
    if ($RequireRedo -and -not $hasRedo) {
        throw "LOCAL-004 post-Redo marker diagnostic is missing."
    }
    if ($hasRedo) { $expectedKeys += "post_redo_marker_class" }
    if (@($Marker.Keys).Count -ne $expectedKeys.Count) {
        throw "LOCAL-004 post-Undo marker diagnostic contains an unexpected field."
    }
    foreach ($key in $expectedKeys) {
        if (-not $Marker.ContainsKey($key)) {
            throw "LOCAL-004 post-Undo marker diagnostic is missing a required field."
        }
    }
    Require-Qs3dValue $Marker "status" "PASS"
    Require-Qs3dValue $Marker "schema" "QS3D_SOURCE_RECONCILE_POST_UNDO_MARKER_V1"
    Require-Qs3dValue $Marker "qualification_boundary" "LOCAL_004_ONLY"
    Require-Qs3dValue $Marker "nonce" $Nonce
    $allowed = @("BEFORE", "AFTER", "OTHER_OR_INVALID")
    Require-Qs3dAllowedValue $Marker "post_undo_marker_class" $allowed
    if ($hasRedo) { Require-Qs3dAllowedValue $Marker "post_redo_marker_class" $allowed }
}

function Read-PositiveMarkerInt {
    param([Parameter(Mandatory = $true)]$Marker, [Parameter(Mandatory = $true)][string]$Key)
    if (-not $Marker.ContainsKey($Key)) { throw "LOCAL-004 marker is missing a required count." }
    [int]$value = 0
    if (-not [int]::TryParse([string]$Marker[$Key], [Globalization.NumberStyles]::None, [Globalization.CultureInfo]::InvariantCulture, [ref]$value) -or $value -le 0) {
        throw "LOCAL-004 marker count is invalid."
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
    if (-not $Process.HasExited) { throw "Launched LOCAL-004 BricsCAD process did not exit." }
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
    if ($matches.Count -gt 1) { throw "LOCAL-004 launcher produced an ambiguous BricsCAD process handoff." }
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
    throw "BricsCAD launcher exited without an exact child-process handoff or LOCAL-004 marker."
}

function Stop-Qs3dLateHandoffProcesses {
    param(
        [Parameter(Mandatory = $true)][int[]]$LauncherIds,
        [Parameter(Mandatory = $true)][string]$ExpectedExecutable
    )
    $deadline = (Get-Date).AddSeconds(10)
    do {
        foreach ($launcherId in $LauncherIds) {
            if ($launcherId -le 0) { continue }
            $candidate = Find-Qs3dHandoffProcess -LauncherId $launcherId -ExpectedExecutable $ExpectedExecutable
            if ($null -ne $candidate) { Stop-Qs3dLaunchedProcess -Process $candidate }
        }
        Start-Sleep -Milliseconds 250
    } while ((Get-Date) -lt $deadline)
    foreach ($launcherId in $LauncherIds) {
        if ($launcherId -le 0) { continue }
        if ($null -ne (Find-Qs3dHandoffProcess -LauncherId $launcherId -ExpectedExecutable $ExpectedExecutable)) {
            throw "LOCAL-004 launcher retained a late BricsCAD child process."
        }
    }
}

function Wait-Qs3dMarkerOrFailure {
    param(
        [Parameter(Mandatory = $true)][ref]$Process,
        [Parameter(Mandatory = $true)][ref]$HandoffCount,
        [Parameter(Mandatory = $true)][string]$ExpectedExecutable,
        [Parameter(Mandatory = $true)][string]$ExpectedPath,
        [Parameter(Mandatory = $true)][string]$FailurePath,
        [Parameter(Mandatory = $true)][DateTime]$Deadline
    )
    $dismissed = 0
    [Diagnostics.Process]$current = $Process.Value
    $launcherId = $current.Id
    $handoffAdopted = $false
    while ((Get-Date) -lt $Deadline) {
        if (Test-Path -LiteralPath $ExpectedPath -PathType Leaf) { return $dismissed }
        if (Test-Path -LiteralPath $FailurePath -PathType Leaf) { return $dismissed }
        $dismissed += Close-Qs3dProxyInformationDialog -Process $current
        $current.Refresh()
        if ($current.HasExited) {
            if ($handoffAdopted) { throw "BricsCAD exited before the LOCAL-004 marker was published." }
            $current = Wait-Qs3dHandoffProcess -LauncherId $launcherId -ExpectedExecutable $ExpectedExecutable -Deadline $Deadline
            $Process.Value = $current
            $HandoffCount.Value = [int]$HandoffCount.Value + 1
            $handoffAdopted = $true
        }
        Start-Sleep -Milliseconds 500
    }
    throw "Timed out waiting for the LOCAL-004 marker."
}

function Wait-Qs3dExit {
    param([Parameter(Mandatory = $true)][Diagnostics.Process]$Process, [Parameter(Mandatory = $true)][DateTime]$Deadline)
    while ((Get-Date) -lt $Deadline) {
        $Process.Refresh()
        if ($Process.HasExited) { return }
        [void](Close-Qs3dProxyInformationDialog -Process $Process)
        Start-Sleep -Milliseconds 500
    }
    throw "Timed out waiting for the LOCAL-004 BricsCAD process to exit."
}

function Remove-ExactFile {
    param([Parameter(Mandatory = $true)][string]$Path)
    if (Test-Path -LiteralPath $Path) { Remove-Item -LiteralPath $Path -Force -ErrorAction Stop }
    if (Test-Path -LiteralPath $Path) { throw "LOCAL-004 exact private-file cleanup failed." }
}

if ([Environment]::OSVersion.Platform -ne [PlatformID]::Win32NT) { throw "LOCAL-004 qualification requires Windows." }
if (-not [Environment]::UserInteractive) { throw "LOCAL-004 qualification requires an interactive Windows session." }
if (-not $ConfirmDisposableCopies) { throw "Pass -ConfirmDisposableCopies only for repository-sample disposable copies." }
if ([string]::IsNullOrWhiteSpace($Profile)) { throw "LOCAL-004 qualification requires an initialized BricsCAD profile." }

$repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot ".."))
$BricsCadDir = [IO.Path]::GetFullPath($BricsCadDir)
$PluginDll = [IO.Path]::GetFullPath($PluginDll)
$FixtureDwg = [IO.Path]::GetFullPath($FixtureDwg)
$ArtifactDir = [IO.Path]::GetFullPath($ArtifactDir)
if ($ArtifactDir.StartsWith($repoRoot + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
    throw "ArtifactDir must stay outside the repository."
}
$expectedFixture = [IO.Path]::GetFullPath((Join-Path $repoRoot "samples\generated\QS3D-Sample.dwg"))
if (-not [string]::Equals($FixtureDwg, $expectedFixture, [StringComparison]::OrdinalIgnoreCase)) {
    throw "FixtureDwg must be the repository-generated QS3D sample."
}
$expectedPlugin = [IO.Path]::GetFullPath((Join-Path $repoRoot "src\QS3D.BricsCAD.V25\bin\x64\Release\net48\QS3D.BricsCAD.V25.dll"))
if (-not [string]::Equals($PluginDll, $expectedPlugin, [StringComparison]::OrdinalIgnoreCase)) {
    throw "PluginDll must be the exact repository x64 Release V25 build output."
}

$bricscadExe = Join-Path $BricsCadDir "bricscad.exe"
$coreDll = Join-Path (Split-Path -Parent $PluginDll) "QS3D.Core.dll"
foreach ($required in @($bricscadExe, $PluginDll, $coreDll, $FixtureDwg)) {
    if (-not (Test-Path -LiteralPath $required -PathType Leaf)) { throw "Required LOCAL-004 input is missing." }
}

$git = Get-Command git -CommandType Application -ErrorAction Stop | Select-Object -First 1
$gitOutput = @(& $git.Source -C $repoRoot rev-parse HEAD 2>$null)
if ($LASTEXITCODE -ne 0 -or $gitOutput.Count -ne 1) { throw "Cannot resolve the exact LOCAL-004 Git candidate SHA." }
$gitHead = ([string]$gitOutput[0]).Trim().ToLowerInvariant()
if ($gitHead -notmatch '^[0-9a-f]{40}$') { throw "LOCAL-004 Git candidate SHA is invalid." }
$gitStatus = @(& $git.Source -C $repoRoot status --porcelain=v1 --untracked-files=all 2>$null)
if ($LASTEXITCODE -ne 0) { throw "Cannot inspect the LOCAL-004 candidate worktree." }
if ($gitStatus.Count -ne 0) { throw "LOCAL-004 qualification requires a clean exact-SHA worktree." }
$expectedAssemblyRevision = "+" + $gitHead
foreach ($assemblyPath in @($PluginDll, $coreDll)) {
    $productVersion = [string](Get-Item -LiteralPath $assemblyPath).VersionInfo.ProductVersion
    if (-not $productVersion.EndsWith($expectedAssemblyRevision, [StringComparison]::OrdinalIgnoreCase)) {
        throw "LOCAL-004 assembly was not built from the exact Git candidate SHA."
    }
}
if (@(Get-Process -Name "bricscad" -ErrorAction SilentlyContinue).Count -gt 0) {
    throw "Close existing BricsCAD processes before isolated LOCAL-004 qualification."
}

if (Test-Path -LiteralPath $ArtifactDir) {
    if (@(Get-ChildItem -LiteralPath $ArtifactDir -Force).Count -ne 0) { throw "ArtifactDir must be empty." }
}
else { New-Item -ItemType Directory -Path $ArtifactDir | Out-Null }
$fixtureRoot = Join-Path $ArtifactDir "fixture-copies"
New-Item -ItemType Directory -Path $fixtureRoot | Out-Null
$drawingA = Join-Path $fixtureRoot "source-a.source-reconcile-probe-copy.dwg"
$drawingB = Join-Path $fixtureRoot "source-b.source-reconcile-probe-copy.dwg"
Copy-Item -LiteralPath $FixtureDwg -Destination $drawingA -ErrorAction Stop
Copy-Item -LiteralPath $FixtureDwg -Destination $drawingB -ErrorAction Stop
$fixtureHash = (Get-FileHash -LiteralPath $FixtureDwg -Algorithm SHA256).Hash.ToUpperInvariant()
foreach ($drawing in @($drawingA, $drawingB)) {
    if (-not [string]::Equals((Get-FileHash -LiteralPath $drawing -Algorithm SHA256).Hash, $fixtureHash, [StringComparison]::OrdinalIgnoreCase)) {
        throw "LOCAL-004 disposable copy hash mismatch."
    }
}

$privateFiles = New-Object System.Collections.Generic.List[string]
foreach ($drawing in @($drawingA, $drawingB)) {
    $sidecar = [IO.Path]::ChangeExtension($drawing, ".qsdb")
    foreach ($path in @($sidecar, ($sidecar + ".bak"), ($sidecar + ".lock"), [IO.Path]::ChangeExtension($drawing, ".dwl"), [IO.Path]::ChangeExtension($drawing, ".dwl2"), [IO.Path]::ChangeExtension($drawing, ".bak"))) {
        $full = [IO.Path]::GetFullPath($path)
        if (-not $full.StartsWith($fixtureRoot + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
            throw "LOCAL-004 private-state path escaped the fixture root."
        }
        if (Test-Path -LiteralPath $full) { throw "LOCAL-004 disposable copy has pre-existing private state." }
        $privateFiles.Add($full)
    }
}
if ($privateFiles.Count -ne 12) { throw "LOCAL-004 private-state cleanup set is incomplete." }

$resultPath = Join-Path $ArtifactDir "source-reconcile-result.txt"
$phasePath = Join-Path $ArtifactDir "source-reconcile-session1.txt"
$markerDiagnosticPath = Join-Path $ArtifactDir "source-reconcile-post-undo-marker.txt"
$scriptOnePath = Join-Path $ArtifactDir "source-reconcile-session1.private.scr"
$scriptTwoPath = Join-Path $ArtifactDir "source-reconcile-session2.private.scr"
$metadataPath = Join-Path $ArtifactDir "source-reconcile-metadata.json"
$nonce = [Guid]::NewGuid().ToString("N")
$pluginHash = (Get-FileHash -LiteralPath $PluginDll -Algorithm SHA256).Hash.ToUpperInvariant()
$environmentNames = @(
    "QS3D_SOURCE_RECONCILE_RESULT", "QS3D_SOURCE_RECONCILE_PHASE_RESULT", "QS3D_SOURCE_RECONCILE_NONCE",
    "QS3D_SOURCE_RECONCILE_MARKER_RESULT",
    "QS3D_SOURCE_RECONCILE_DWG_A", "QS3D_SOURCE_RECONCILE_DWG_B",
    "QS3D_SOURCE_RECONCILE_UNDO_COHERENT", "QS3D_SOURCE_RECONCILE_REDO_COHERENT"
)
$oldEnvironment = @{}
foreach ($name in $environmentNames) { $oldEnvironment[$name] = [Environment]::GetEnvironmentVariable($name, "Process") }

$processOne = $null
$processTwo = $null
$launcherOneId = 0
$launcherTwoId = 0
$launcherHandoffs = 0
$phaseMarker = $null
$markerDiagnostic = $null
$finalMarker = $null
$qualificationError = $null
$cleanupError = $null
$processCleanupVerified = $false
$scriptCleanupVerified = $false
$privateStateCleanupVerified = $false
$drawingRestoreVerified = $false
$proxyDialogsDismissed = 0
$startedAt = Get-Date

try {
    $env:QS3D_SOURCE_RECONCILE_RESULT = $resultPath
    $env:QS3D_SOURCE_RECONCILE_PHASE_RESULT = $phasePath
    $env:QS3D_SOURCE_RECONCILE_MARKER_RESULT = $markerDiagnosticPath
    $env:QS3D_SOURCE_RECONCILE_NONCE = $nonce
    $env:QS3D_SOURCE_RECONCILE_DWG_A = $drawingA
    $env:QS3D_SOURCE_RECONCILE_DWG_B = $drawingB

    $scriptOne = @(
        "FILEDIA", "0", "CMDECHO", "1", "TILEMODE", "1", "INSUNITS", "4", "UCS", "W",
        "NETLOAD", ('"' + $PluginDll + '"'),
        "QS3DDRAWWALL", "0,0", "5000,0",
        "QS3DDRAWWALLADV", "0,10000", "4000,10000", "4000,13000", "", "", "", "",
        "QS3DSRTPREPARE", "QS3DSYNCSOURCE", "QS3DSRTAFTERSYNC1",
        "QS3DSRTSELECTLINE", "QS3DBUILD3D", "QS3DSRTSELECTPOLY", "QS3DBUILD3D", "QS3DSRTAFTERREBUILD1",
        "QS3DSRTPREPAREROLLBACK", "INSUNITS", "6", "QS3DSYNCSOURCE", "INSUNITS", "4", "QS3DSRTCHECKROLLBACK",
        "QS3DSRTPREPGENERATED", "QS3DSYNCSOURCE", "QS3DSRTCHECKGENERATED",
        "QS3DSRTPREPAMBIGUOUS", "QS3DSYNCSOURCE", "QS3DSRTCHECKAMBIGUOUS",
        "_.OPEN", ('"' + $drawingB + '"'), "QS3DSRTSEEDB", "QS3DSYNCSOURCE", "QS3DSRTCHECKB",
        "QS3DSRTMARKERBEFOREFINAL", "QS3DSRTSELECTSOURCES", "QS3DSYNCSOURCE", "QS3DSRTAFTERFINALSYNC", "QS3DSRTMARKERAFTERFINAL",
        "_.UNDO", "1", "QS3DSRTMARKERAFTERUNDO", "QS3DSRTCHECKUNDO", "_.REDO", "QS3DSRTMARKERAFTERREDO", "QS3DSRTCHECKREDO",
        "QS3DSRTSELECTLINE", "QS3DBUILD3D", "QS3DSRTSELECTPOLY", "QS3DBUILD3D", "QS3DSRTFINALREBUILD",
        "QS3DSRTSESSION1", "QS3DSRTMARKERPUBLISH", "QS3DSAVE", "_.QSAVE", "_.QUIT", "_Y"
    )
    [IO.File]::WriteAllLines($scriptOnePath, $scriptOne, [Text.Encoding]::ASCII)
    $argumentsOne = '"' + $drawingA + '" /P "' + $Profile + '" /B "' + $scriptOnePath + '"'
    $deadlineOne = (Get-Date).AddSeconds($StartupTimeoutSeconds)
    $processOne = Start-Process -FilePath $bricscadExe -ArgumentList $argumentsOne -PassThru -WindowStyle Hidden -WorkingDirectory $ArtifactDir
    $launcherOneId = $processOne.Id
    $proxyDialogsDismissed += Wait-Qs3dMarkerOrFailure -Process ([ref]$processOne) -HandoffCount ([ref]$launcherHandoffs) -ExpectedExecutable $bricscadExe -ExpectedPath $phasePath -FailurePath $resultPath -Deadline $deadlineOne
    if (Test-Path -LiteralPath $resultPath -PathType Leaf) {
        $finalMarker = Read-Qs3dMarker -Path $resultPath
        if (Test-Path -LiteralPath $markerDiagnosticPath -PathType Leaf) {
            $markerDiagnostic = Read-Qs3dMarker -Path $markerDiagnosticPath
            Require-PostUndoMarkerDiagnostics -Marker $markerDiagnostic -Nonce $nonce
        }
        Require-Qs3dValue -Marker $finalMarker -Key "status" -Expected "FAIL"
        Require-Qs3dValue -Marker $finalMarker -Key "schema" -Expected "QS3D_SOURCE_RECONCILE_RUNTIME_V1"
        Require-Qs3dValue -Marker $finalMarker -Key "qualification_boundary" -Expected "LOCAL_004_ONLY"
        Require-Qs3dValue -Marker $finalMarker -Key "nonce" -Expected $nonce
        if ([string]::Equals([string]$finalMarker["failure_phase"], "verify_final_reconcile", [StringComparison]::Ordinal)) {
            Require-FinalReconcileDiagnostics -Marker $finalMarker
        }
        throw "LOCAL-004 session one published a sanitized failure marker."
    }
    Wait-Qs3dExit -Process $processOne -Deadline $deadlineOne
    Stop-Qs3dLaunchedProcess -Process $processOne
    $phaseMarker = Read-Qs3dMarker -Path $phasePath
    Require-Qs3dValue -Marker $phaseMarker -Key "status" -Expected "PASS"
    Require-Qs3dValue -Marker $phaseMarker -Key "schema" -Expected "QS3D_SOURCE_RECONCILE_RUNTIME_V1"
    Require-Qs3dValue -Marker $phaseMarker -Key "qualification_boundary" -Expected "LOCAL_004_ONLY"
    Require-Qs3dValue -Marker $phaseMarker -Key "nonce" -Expected $nonce
    $markerDiagnostic = Read-Qs3dMarker -Path $markerDiagnosticPath
    Require-PostUndoMarkerDiagnostics -Marker $markerDiagnostic -Nonce $nonce -RequireRedo
    Require-Qs3dValue -Marker $phaseMarker -Key "source_count" -Expected "2"
    Require-Qs3dValue -Marker $phaseMarker -Key "success_reconcile_count" -Expected "2"
    foreach ($key in @("generated_refusal_verified", "ambiguous_refusal_verified", "forced_rollback_verified", "multi_document_refusal_verified", "source_geometry_preserved", "generated_replacement_verified")) {
        Require-Qs3dValue -Marker $phaseMarker -Key $key -Expected "true"
    }
    [void](Read-PositiveMarkerInt -Marker $phaseMarker -Key "generated_solid_count")
    Require-FinalReconcileDiagnostics -Marker $phaseMarker
    $env:QS3D_SOURCE_RECONCILE_UNDO_COHERENT = [string]$phaseMarker["undo_coherent"]
    $env:QS3D_SOURCE_RECONCILE_REDO_COHERENT = [string]$phaseMarker["redo_coherent"]
    Remove-ExactFile -Path $scriptOnePath

    $scriptTwo = @(
        "FILEDIA", "0", "CMDECHO", "1", "TILEMODE", "1", "INSUNITS", "4", "UCS", "W",
        "NETLOAD", ('"' + $PluginDll + '"'), "QS3DSRTREOPEN", "_.QUIT", "_Y"
    )
    [IO.File]::WriteAllLines($scriptTwoPath, $scriptTwo, [Text.Encoding]::ASCII)
    $argumentsTwo = '"' + $drawingA + '" /P "' + $Profile + '" /B "' + $scriptTwoPath + '"'
    $deadlineTwo = (Get-Date).AddSeconds($StartupTimeoutSeconds)
    $processTwo = Start-Process -FilePath $bricscadExe -ArgumentList $argumentsTwo -PassThru -WindowStyle Hidden -WorkingDirectory $ArtifactDir
    $launcherTwoId = $processTwo.Id
    $proxyDialogsDismissed += Wait-Qs3dMarkerOrFailure -Process ([ref]$processTwo) -HandoffCount ([ref]$launcherHandoffs) -ExpectedExecutable $bricscadExe -ExpectedPath $resultPath -FailurePath $resultPath -Deadline $deadlineTwo
    Wait-Qs3dExit -Process $processTwo -Deadline $deadlineTwo
    Stop-Qs3dLaunchedProcess -Process $processTwo
    $finalMarker = Read-Qs3dMarker -Path $resultPath
    Require-Qs3dValue -Marker $finalMarker -Key "schema" -Expected "QS3D_SOURCE_RECONCILE_RUNTIME_V1"
    Require-Qs3dValue -Marker $finalMarker -Key "qualification_boundary" -Expected "LOCAL_004_ONLY"
    Require-Qs3dValue -Marker $finalMarker -Key "nonce" -Expected $nonce
    Require-Qs3dValue -Marker $finalMarker -Key "cold_reopen_verified" -Expected "true"
    [void](Read-PositiveMarkerInt -Marker $finalMarker -Key "generated_solid_count")
    if ([string]::Equals([string]$finalMarker["status"], "PASS", [StringComparison]::OrdinalIgnoreCase)) {
        Require-Qs3dValue -Marker $finalMarker -Key "production_local004_qualified" -Expected "true"
        Require-Qs3dValue -Marker $finalMarker -Key "undo_coherent" -Expected "true"
        Require-Qs3dValue -Marker $finalMarker -Key "redo_coherent" -Expected "true"
        Require-Qs3dValue -Marker $finalMarker -Key "error_code" -Expected "NONE"
    }
    else {
        Require-Qs3dValue -Marker $finalMarker -Key "status" -Expected "FAIL"
        Require-Qs3dValue -Marker $finalMarker -Key "production_local004_qualified" -Expected "false"
        Require-Qs3dValue -Marker $finalMarker -Key "error_code" -Expected "NATIVE_UNDO_SEMANTIC_DIVERGENCE"
        Require-Qs3dValue -Marker $finalMarker -Key "failure_phase" -Expected "native_undo"
        throw "LOCAL-004 reproduced sanitized production failure NATIVE_UNDO_SEMANTIC_DIVERGENCE."
    }
}
catch { $qualificationError = $_.Exception }
finally {
    try {
        Stop-Qs3dLaunchedProcess -Process $processOne
        Stop-Qs3dLaunchedProcess -Process $processTwo
        Stop-Qs3dLateHandoffProcesses -LauncherIds @($launcherOneId, $launcherTwoId) -ExpectedExecutable $bricscadExe
        if (@(Get-Process -Name "bricscad" -ErrorAction SilentlyContinue).Count -ne 0) { throw "LOCAL-004 process cleanup is incomplete." }
        $processCleanupVerified = $true
        foreach ($scriptPath in @($scriptOnePath, $scriptTwoPath)) { Remove-ExactFile -Path $scriptPath }
        $scriptCleanupVerified = $true
        foreach ($privatePath in $privateFiles) { Remove-ExactFile -Path $privatePath }
        $privateStateCleanupVerified = $true
        Copy-Item -LiteralPath $FixtureDwg -Destination $drawingA -Force -ErrorAction Stop
        Copy-Item -LiteralPath $FixtureDwg -Destination $drawingB -Force -ErrorAction Stop
        $drawingRestoreVerified = [string]::Equals((Get-FileHash -LiteralPath $drawingA -Algorithm SHA256).Hash, $fixtureHash, [StringComparison]::OrdinalIgnoreCase) -and
                                  [string]::Equals((Get-FileHash -LiteralPath $drawingB -Algorithm SHA256).Hash, $fixtureHash, [StringComparison]::OrdinalIgnoreCase)
        if (-not $drawingRestoreVerified) { throw "LOCAL-004 drawing restore verification failed." }
        Remove-ExactFile -Path $drawingA
        Remove-ExactFile -Path $drawingB
        if (@(Get-ChildItem -LiteralPath $fixtureRoot -Force).Count -ne 0) { throw "LOCAL-004 fixture root retained private files." }
        Remove-Item -LiteralPath $fixtureRoot -Force -ErrorAction Stop
        if (Test-Path -LiteralPath $fixtureRoot) { throw "LOCAL-004 fixture-root cleanup failed." }
    }
    catch { $cleanupError = $_.Exception }
    finally {
        foreach ($name in $environmentNames) { Restore-EnvironmentValue -Name $name -Value $oldEnvironment[$name] }
    }
}

$status = if ($null -eq $qualificationError -and $null -eq $cleanupError) { "PASS" } else { "FAIL" }
$metadata = [ordered]@{
    status = $status
    git_sha = $gitHead
    started_at = $startedAt.ToUniversalTime().ToString("O")
    completed_at = (Get-Date).ToUniversalTime().ToString("O")
    bricscad_file_version = (Get-Item -LiteralPath $bricscadExe).VersionInfo.FileVersion
    plugin_sha256 = $pluginHash
    repository_fixture_sha256 = $fixtureHash
    process_cleanup_verified = $processCleanupVerified
    script_cleanup_verified = $scriptCleanupVerified
    private_state_cleanup_verified = $privateStateCleanupVerified
    drawing_restore_verified = $drawingRestoreVerified
    proxy_information_dialogs_dismissed = $proxyDialogsDismissed
    launcher_handoffs = $launcherHandoffs
    phase_marker = $phaseMarker
    post_undo_marker_diagnostic = $markerDiagnostic
    marker = $finalMarker
}
$metadata | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $metadataPath -Encoding UTF8

if ($null -ne $cleanupError) { throw "LOCAL-004 cleanup failed; inspect local console state only." }
if ($null -ne $qualificationError) { throw $qualificationError }
Write-Host "QS3D BricsCAD V25 LOCAL-004 Source Reconcile runtime PASS"
Write-Host "Sanitized marker and metadata written to the requested artifact directory."
