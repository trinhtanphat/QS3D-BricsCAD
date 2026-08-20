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
if (-not (Test-Path -LiteralPath $windowInteropPath -PathType Leaf)) { throw "LOCAL-004 P01 runner window interop helper is missing." }
. $windowInteropPath

function Read-Qs3dMarker {
    param([Parameter(Mandatory = $true)][string]$Path)
    $marker = @{}
    foreach ($line in Get-Content -LiteralPath $Path) {
        if ([string]::IsNullOrWhiteSpace($line)) { continue }
        $separator = $line.IndexOf('=')
        if ($separator -le 0 -or $separator -eq $line.Length - 1) { throw "Malformed LOCAL-004 P01 marker line." }
        $key = $line.Substring(0, $separator).Trim()
        $value = $line.Substring($separator + 1).Trim()
        if ($marker.ContainsKey($key)) { throw "Duplicate LOCAL-004 P01 marker key." }
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
    if (-not $Marker.ContainsKey($Key)) { throw "LOCAL-004 P01 marker is missing a required field." }
    if (-not [string]::Equals([string]$Marker[$Key], $Expected, [StringComparison]::OrdinalIgnoreCase)) {
        throw "LOCAL-004 P01 marker field did not match its required value."
    }
}

function Require-Qs3dMarkerSchema {
    param(
        [Parameter(Mandatory = $true)]$Marker,
        [Parameter(Mandatory = $true)][string]$Nonce,
        [Parameter(Mandatory = $true)][bool]$ColdReopen,
        [Parameter(Mandatory = $true)][bool]$Qualified
    )
    $expectedKeys = @(
        "status", "command", "nonce", "schema", "qualification_boundary", "production_local004_p01_qualified",
        "native_move_verified", "move_reconcile_verified", "move_rebuild_verified", "native_rotate_verified",
        "rotate_reconcile_verified", "native_stretch_verified", "stretch_reconcile_verified", "final_rebuild_verified",
        "cold_reopen_verified", "source_type", "edit_commands", "final_length_class", "error_code"
    )
    if (@($Marker.Keys).Count -ne $expectedKeys.Count) { throw "LOCAL-004 P01 marker contains an unexpected field." }
    foreach ($key in $expectedKeys) {
        if (-not $Marker.ContainsKey($key)) { throw "LOCAL-004 P01 marker is missing a required field." }
    }
    Require-Qs3dValue $Marker "status" "PASS"
    Require-Qs3dValue $Marker "nonce" $Nonce
    Require-Qs3dValue $Marker "schema" "QS3D_SOURCE_RECONCILE_NATIVE_LINE_RUNTIME_V1"
    Require-Qs3dValue $Marker "qualification_boundary" "LOCAL_004_P01_LINE_ONLY"
    Require-Qs3dValue $Marker "production_local004_p01_qualified" $(if ($Qualified) { "true" } else { "false" })
    foreach ($key in @(
        "native_move_verified", "move_reconcile_verified", "move_rebuild_verified", "native_rotate_verified",
        "rotate_reconcile_verified", "native_stretch_verified", "stretch_reconcile_verified", "final_rebuild_verified"
    )) { Require-Qs3dValue $Marker $key "true" }
    Require-Qs3dValue $Marker "cold_reopen_verified" $(if ($ColdReopen) { "true" } else { "false" })
    Require-Qs3dValue $Marker "source_type" "LINE"
    Require-Qs3dValue $Marker "edit_commands" "MOVE_ROTATE_STRETCH"
    Require-Qs3dValue $Marker "final_length_class" "EIGHT_METERS"
    Require-Qs3dValue $Marker "error_code" "NONE"
}

function Require-Qs3dFailureMarker {
    param([Parameter(Mandatory = $true)]$Marker, [Parameter(Mandatory = $true)][string]$Nonce)
    $expectedKeys = @(
        "status", "command", "nonce", "schema", "qualification_boundary",
        "production_local004_p01_qualified", "error_code", "failure_phase", "failure_code"
    )
    if (@($Marker.Keys).Count -ne $expectedKeys.Count) { throw "LOCAL-004 P01 failure marker contains an unexpected field." }
    foreach ($key in $expectedKeys) {
        if (-not $Marker.ContainsKey($key)) { throw "LOCAL-004 P01 failure marker is missing a required field." }
    }
    Require-Qs3dValue $Marker "status" "FAIL"
    Require-Qs3dValue $Marker "nonce" $Nonce
    Require-Qs3dValue $Marker "schema" "QS3D_SOURCE_RECONCILE_NATIVE_LINE_RUNTIME_V1"
    Require-Qs3dValue $Marker "qualification_boundary" "LOCAL_004_P01_LINE_ONLY"
    Require-Qs3dValue $Marker "production_local004_p01_qualified" "false"
    Require-Qs3dValue $Marker "error_code" "SOURCE_RECONCILE_NATIVE_LINE_RUNTIME_FAILED"
    $allowedPhases = @(
        "prepare", "native_move", "select_source", "check_move_reconcile", "check_move_build",
        "native_rotate", "check_rotate_reconcile", "native_stretch", "check_stretch_reconcile",
        "final_rebuild", "cold_reopen"
    )
    if (-not ($allowedPhases -contains [string]$Marker["failure_phase"])) { throw "LOCAL-004 P01 failure phase is not allowlisted." }
    $allowedCodes = @(
        "ACTIVE_DOCUMENT_MISSING", "PROJECT_MISSING", "SEQUENCE_NOT_INITIALIZED", "SEQUENCE_CONTEXT_CHANGED",
        "SEQUENCE_ORDER_REJECTED", "SOURCE_OWNER_REJECTED", "SOURCE_MISSING", "SOURCE_TYPE_REJECTED",
        "SELECTION_REJECTED", "GENERATED_HANDLE_REJECTED", "GENERATED_OWNERSHIP_REJECTED",
        "GENERATED_INVALIDATION_REJECTED", "GENERATED_REPLACEMENT_REJECTED", "EXPECTED_GEOMETRY_REJECTED",
        "PHASE_EVIDENCE_MISSING", "PHASE_EVIDENCE_REJECTED", "AUTOMATION_CONTEXT_REJECTED",
        "RESULT_PATH_REJECTED", "DOCUMENT_PATH_REJECTED", "STATE_REJECTED"
    )
    if (-not ($allowedCodes -contains [string]$Marker["failure_code"])) { throw "LOCAL-004 P01 failure code is not allowlisted." }
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
    if (-not $Process.HasExited) { throw "Launched LOCAL-004 P01 BricsCAD process did not exit." }
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
    if ($matches.Count -gt 1) { throw "LOCAL-004 P01 launcher produced an ambiguous BricsCAD process handoff." }
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
    throw "BricsCAD launcher exited without an exact child-process handoff or LOCAL-004 P01 marker."
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
            throw "LOCAL-004 P01 launcher retained a late BricsCAD child process."
        }
    }
}

function Wait-Qs3dMarker {
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
            if ($handoffAdopted) { throw "BricsCAD exited before the LOCAL-004 P01 marker was published." }
            $current = Wait-Qs3dHandoffProcess -LauncherId $launcherId -ExpectedExecutable $ExpectedExecutable -Deadline $Deadline
            $Process.Value = $current
            $HandoffCount.Value = [int]$HandoffCount.Value + 1
            $handoffAdopted = $true
        }
        Start-Sleep -Milliseconds 500
    }
    throw "Timed out waiting for the LOCAL-004 P01 marker."
}

function Wait-Qs3dExit {
    param([Parameter(Mandatory = $true)][Diagnostics.Process]$Process, [Parameter(Mandatory = $true)][DateTime]$Deadline)
    while ((Get-Date) -lt $Deadline) {
        $Process.Refresh()
        if ($Process.HasExited) { return }
        [void](Close-Qs3dProxyInformationDialog -Process $Process)
        Start-Sleep -Milliseconds 500
    }
    throw "Timed out waiting for the LOCAL-004 P01 BricsCAD process to exit."
}

function Remove-ExactFile {
    param([Parameter(Mandatory = $true)][string]$Path)
    if (Test-Path -LiteralPath $Path) { Remove-Item -LiteralPath $Path -Force -ErrorAction Stop }
    if (Test-Path -LiteralPath $Path) { throw "LOCAL-004 P01 exact private-file cleanup failed." }
}

if ([Environment]::OSVersion.Platform -ne [PlatformID]::Win32NT) { throw "LOCAL-004 P01 qualification requires Windows." }
if (-not [Environment]::UserInteractive) { throw "LOCAL-004 P01 qualification requires an interactive Windows session." }
if (-not $ConfirmDisposableCopies) { throw "Pass -ConfirmDisposableCopies only for repository-sample disposable copies." }
if ([string]::IsNullOrWhiteSpace($Profile)) { throw "LOCAL-004 P01 qualification requires an initialized BricsCAD profile." }

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
    if (-not (Test-Path -LiteralPath $required -PathType Leaf)) { throw "Required LOCAL-004 P01 input is missing." }
}

$git = Get-Command git -CommandType Application -ErrorAction Stop | Select-Object -First 1
$gitOutput = @(& $git.Source -C $repoRoot rev-parse HEAD 2>$null)
if ($LASTEXITCODE -ne 0 -or $gitOutput.Count -ne 1) { throw "Cannot resolve the exact LOCAL-004 P01 Git candidate SHA." }
$gitHead = ([string]$gitOutput[0]).Trim().ToLowerInvariant()
if ($gitHead -notmatch '^[0-9a-f]{40}$') { throw "LOCAL-004 P01 Git candidate SHA is invalid." }
$gitStatus = @(& $git.Source -C $repoRoot status --porcelain=v1 --untracked-files=all 2>$null)
if ($LASTEXITCODE -ne 0) { throw "Cannot inspect the LOCAL-004 P01 candidate worktree." }
if ($gitStatus.Count -ne 0) { throw "LOCAL-004 P01 qualification requires a clean exact-SHA worktree." }
$expectedAssemblyRevision = "+" + $gitHead
foreach ($assemblyPath in @($PluginDll, $coreDll)) {
    $productVersion = [string](Get-Item -LiteralPath $assemblyPath).VersionInfo.ProductVersion
    if (-not $productVersion.EndsWith($expectedAssemblyRevision, [StringComparison]::OrdinalIgnoreCase)) {
        throw "LOCAL-004 P01 assembly was not built from the exact Git candidate SHA."
    }
}
if (@(Get-Process -Name "bricscad" -ErrorAction SilentlyContinue).Count -gt 0) {
    throw "Close existing BricsCAD processes before isolated LOCAL-004 P01 qualification."
}

if (Test-Path -LiteralPath $ArtifactDir) {
    if (@(Get-ChildItem -LiteralPath $ArtifactDir -Force).Count -ne 0) { throw "ArtifactDir must be empty." }
}
else { New-Item -ItemType Directory -Path $ArtifactDir | Out-Null }
$fixtureRoot = Join-Path $ArtifactDir "fixture-copy"
New-Item -ItemType Directory -Path $fixtureRoot | Out-Null
$drawing = Join-Path $fixtureRoot "source-native-line-edit-probe-copy.dwg"
Copy-Item -LiteralPath $FixtureDwg -Destination $drawing -ErrorAction Stop
$fixtureHash = (Get-FileHash -LiteralPath $FixtureDwg -Algorithm SHA256).Hash.ToUpperInvariant()
if (-not [string]::Equals((Get-FileHash -LiteralPath $drawing -Algorithm SHA256).Hash, $fixtureHash, [StringComparison]::OrdinalIgnoreCase)) {
    throw "LOCAL-004 P01 disposable copy hash mismatch."
}

$sidecar = [IO.Path]::ChangeExtension($drawing, ".qsdb")
$privateFiles = @(
    $sidecar, ($sidecar + ".bak"), ($sidecar + ".lock"),
    [IO.Path]::ChangeExtension($drawing, ".dwl"), [IO.Path]::ChangeExtension($drawing, ".dwl2"),
    [IO.Path]::ChangeExtension($drawing, ".bak")
) | ForEach-Object { [IO.Path]::GetFullPath($_) }
foreach ($path in $privateFiles) {
    if (-not $path.StartsWith($fixtureRoot + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
        throw "LOCAL-004 P01 private-state path escaped the fixture root."
    }
    if (Test-Path -LiteralPath $path) { throw "LOCAL-004 P01 disposable copy has pre-existing private state." }
}

$resultPath = Join-Path $ArtifactDir "source-reconcile-native-line-result.txt"
$phasePath = Join-Path $ArtifactDir "source-reconcile-native-line-session1.txt"
$scriptOnePath = Join-Path $ArtifactDir "source-reconcile-native-line-session1.private.scr"
$scriptTwoPath = Join-Path $ArtifactDir "source-reconcile-native-line-session2.private.scr"
$metadataPath = Join-Path $ArtifactDir "source-reconcile-native-line-metadata.json"
$nonce = [Guid]::NewGuid().ToString("N")
$pluginHash = (Get-FileHash -LiteralPath $PluginDll -Algorithm SHA256).Hash.ToUpperInvariant()
$environmentNames = @(
    "QS3D_SOURCE_RECONCILE_NATIVE_LINE_RESULT", "QS3D_SOURCE_RECONCILE_NATIVE_LINE_PHASE_RESULT",
    "QS3D_SOURCE_RECONCILE_NATIVE_LINE_NONCE", "QS3D_SOURCE_RECONCILE_NATIVE_LINE_DWG"
)
$oldEnvironment = @{}
foreach ($name in $environmentNames) { $oldEnvironment[$name] = [Environment]::GetEnvironmentVariable($name, "Process") }

$processOne = $null
$processTwo = $null
$launcherOneId = 0
$launcherTwoId = 0
$launcherHandoffs = 0
$phaseMarker = $null
$finalMarker = $null
$qualificationError = $null
$cleanupError = $null
$processCleanupVerified = $false
$scriptCleanupVerified = $false
$privateStateCleanupVerified = $false
$drawingRestoreVerified = $false
$drawingPersistedChanged = $false
$sidecarPersisted = $false
$proxyDialogsDismissed = 0
$startedAt = Get-Date

try {
    $env:QS3D_SOURCE_RECONCILE_NATIVE_LINE_RESULT = $resultPath
    $env:QS3D_SOURCE_RECONCILE_NATIVE_LINE_PHASE_RESULT = $phasePath
    $env:QS3D_SOURCE_RECONCILE_NATIVE_LINE_NONCE = $nonce
    $env:QS3D_SOURCE_RECONCILE_NATIVE_LINE_DWG = $drawing

    $scriptOne = @(
        "FILEDIA", "0", "CMDECHO", "1", "TILEMODE", "1", "INSUNITS", "4", "UCS", "W", "ANGBASE", "0", "ANGDIR", "0",
        "NETLOAD", ('"' + $PluginDll + '"'),
        "QS3DDRAWWALL", "0,0", "5000,0",
        "QS3DSRNATIVEPREPARE",
        "QS3DSRNATIVEMOVE", "QS3DSRNATIVESELECT", "QS3DSYNCSOURCE", "QS3DSRNATIVECHECKMOVE",
        "QS3DSRNATIVESELECT", "QS3DBUILD3D", "QS3DSRNATIVECHECKMOVEBUILD",
        "QS3DSRNATIVEROTATE", "QS3DSRNATIVESELECT", "QS3DSYNCSOURCE", "QS3DSRNATIVECHECKROTATE",
        "QS3DSRNATIVESTRETCH", "QS3DSRNATIVESELECT", "QS3DSYNCSOURCE", "QS3DSRNATIVECHECKSTRETCH",
        "QS3DSRNATIVESELECT", "QS3DBUILD3D", "QS3DSRNATIVEFINAL",
        "QS3DSAVE", "_.QSAVE", "_.QUIT", "_Y"
    )
    [IO.File]::WriteAllLines($scriptOnePath, $scriptOne, [Text.Encoding]::ASCII)
    $argumentsOne = '"' + $drawing + '" /P "' + $Profile + '" /B "' + $scriptOnePath + '"'
    $deadlineOne = (Get-Date).AddSeconds($StartupTimeoutSeconds)
    $processOne = Start-Process -FilePath $bricscadExe -ArgumentList $argumentsOne -PassThru -WindowStyle Hidden -WorkingDirectory $ArtifactDir
    $launcherOneId = $processOne.Id
    $proxyDialogsDismissed += Wait-Qs3dMarker -Process ([ref]$processOne) -HandoffCount ([ref]$launcherHandoffs) -ExpectedExecutable $bricscadExe -ExpectedPath $phasePath -FailurePath $resultPath -Deadline $deadlineOne
    if (Test-Path -LiteralPath $resultPath -PathType Leaf) {
        $finalMarker = Read-Qs3dMarker -Path $resultPath
        Require-Qs3dFailureMarker -Marker $finalMarker -Nonce $nonce
        throw "LOCAL-004 P01 session one published a sanitized failure marker."
    }
    Wait-Qs3dExit -Process $processOne -Deadline $deadlineOne
    Stop-Qs3dLaunchedProcess -Process $processOne
    $phaseMarker = Read-Qs3dMarker -Path $phasePath
    Require-Qs3dMarkerSchema -Marker $phaseMarker -Nonce $nonce -ColdReopen $false -Qualified $false
    $drawingPersistedChanged = -not [string]::Equals((Get-FileHash -LiteralPath $drawing -Algorithm SHA256).Hash, $fixtureHash, [StringComparison]::OrdinalIgnoreCase)
    if (-not $drawingPersistedChanged) { throw "LOCAL-004 P01 session one did not persist its disposable drawing changes." }
    $sidecarPersisted = Test-Path -LiteralPath $sidecar -PathType Leaf
    if (-not $sidecarPersisted) { throw "LOCAL-004 P01 session one did not persist its disposable semantic sidecar." }
    Remove-ExactFile -Path $scriptOnePath

    $scriptTwo = @(
        "FILEDIA", "0", "CMDECHO", "1", "TILEMODE", "1", "INSUNITS", "4", "UCS", "W",
        "NETLOAD", ('"' + $PluginDll + '"'), "QS3DSRNATIVEREOPEN", "_.QUIT", "_Y"
    )
    [IO.File]::WriteAllLines($scriptTwoPath, $scriptTwo, [Text.Encoding]::ASCII)
    $argumentsTwo = '"' + $drawing + '" /P "' + $Profile + '" /B "' + $scriptTwoPath + '"'
    $deadlineTwo = (Get-Date).AddSeconds($StartupTimeoutSeconds)
    $processTwo = Start-Process -FilePath $bricscadExe -ArgumentList $argumentsTwo -PassThru -WindowStyle Hidden -WorkingDirectory $ArtifactDir
    $launcherTwoId = $processTwo.Id
    $proxyDialogsDismissed += Wait-Qs3dMarker -Process ([ref]$processTwo) -HandoffCount ([ref]$launcherHandoffs) -ExpectedExecutable $bricscadExe -ExpectedPath $resultPath -FailurePath $resultPath -Deadline $deadlineTwo
    Wait-Qs3dExit -Process $processTwo -Deadline $deadlineTwo
    Stop-Qs3dLaunchedProcess -Process $processTwo
    $finalMarker = Read-Qs3dMarker -Path $resultPath
    if ([string]::Equals([string]$finalMarker["status"], "PASS", [StringComparison]::OrdinalIgnoreCase)) {
        Require-Qs3dMarkerSchema -Marker $finalMarker -Nonce $nonce -ColdReopen $true -Qualified $true
    }
    else {
        Require-Qs3dFailureMarker -Marker $finalMarker -Nonce $nonce
        throw "LOCAL-004 P01 cold reopen published a sanitized failure marker."
    }
}
catch { $qualificationError = $_.Exception }
finally {
    try {
        Stop-Qs3dLaunchedProcess -Process $processOne
        Stop-Qs3dLaunchedProcess -Process $processTwo
        Stop-Qs3dLateHandoffProcesses -LauncherIds @($launcherOneId, $launcherTwoId) -ExpectedExecutable $bricscadExe
        if (@(Get-Process -Name "bricscad" -ErrorAction SilentlyContinue).Count -ne 0) { throw "LOCAL-004 P01 process cleanup is incomplete." }
        $processCleanupVerified = $true
        foreach ($scriptPath in @($scriptOnePath, $scriptTwoPath)) { Remove-ExactFile -Path $scriptPath }
        $scriptCleanupVerified = $true
        foreach ($privatePath in $privateFiles) { Remove-ExactFile -Path $privatePath }
        $privateStateCleanupVerified = $true
        Copy-Item -LiteralPath $FixtureDwg -Destination $drawing -Force -ErrorAction Stop
        $drawingRestoreVerified = [string]::Equals((Get-FileHash -LiteralPath $drawing -Algorithm SHA256).Hash, $fixtureHash, [StringComparison]::OrdinalIgnoreCase)
        if (-not $drawingRestoreVerified) { throw "LOCAL-004 P01 drawing restore verification failed." }
        Remove-ExactFile -Path $drawing
        if (@(Get-ChildItem -LiteralPath $fixtureRoot -Force).Count -ne 0) { throw "LOCAL-004 P01 fixture root retained private files." }
        Remove-Item -LiteralPath $fixtureRoot -Force -ErrorAction Stop
        if (Test-Path -LiteralPath $fixtureRoot) { throw "LOCAL-004 P01 fixture-root cleanup failed." }
    }
    catch { $cleanupError = $_.Exception }
    finally {
        foreach ($name in $environmentNames) { Restore-EnvironmentValue -Name $name -Value $oldEnvironment[$name] }
    }
}

$status = if ($null -eq $qualificationError -and $null -eq $cleanupError) { "PASS" } else { "FAIL" }
$metadata = [ordered]@{
    status = $status
    qualification_boundary = "LOCAL_004_P01_LINE_ONLY"
    git_sha = $gitHead
    started_at = $startedAt.ToUniversalTime().ToString("O")
    completed_at = (Get-Date).ToUniversalTime().ToString("O")
    bricscad_file_version = (Get-Item -LiteralPath $bricscadExe).VersionInfo.FileVersion
    plugin_sha256 = $pluginHash
    repository_fixture_sha256 = $fixtureHash
    drawing_persisted_changed = $drawingPersistedChanged
    sidecar_persisted = $sidecarPersisted
    process_cleanup_verified = $processCleanupVerified
    script_cleanup_verified = $scriptCleanupVerified
    private_state_cleanup_verified = $privateStateCleanupVerified
    drawing_restore_verified = $drawingRestoreVerified
    proxy_information_dialogs_dismissed = $proxyDialogsDismissed
    launcher_handoffs = $launcherHandoffs
    phase_marker = $phaseMarker
    marker = $finalMarker
}
$metadata | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $metadataPath -Encoding UTF8

if ($null -ne $cleanupError) { throw "LOCAL-004 P01 cleanup failed; inspect local console state only." }
if ($null -ne $qualificationError) { throw $qualificationError }
Write-Host "QS3D BricsCAD V25 LOCAL-004 P01 native LINE edit runtime PASS"
Write-Host "Sanitized marker and metadata written to the requested artifact directory."
