param(
    [Parameter(Mandatory = $true)][string]$BricsCadDir,
    [Parameter(Mandatory = $true)][string]$PluginDll,
    [Parameter(Mandatory = $true)][string]$DrawingCopy,
    [Parameter(Mandatory = $true)][string]$Profile,
    [Parameter(Mandatory = $true)][string]$ArtifactDir,
    [Parameter(Mandatory = $true)][ValidatePattern("^[0-9a-fA-F]{40}$")][string]$ExpectedSourceSha,
    [Parameter(Mandatory = $true)][switch]$ConfirmDisposableCopy,
    [ValidateSet("Millimeter", "Meter")][string]$NativeDrawingUnit = "Millimeter",
    [ValidateRange(60, 900)][int]$StartupTimeoutSeconds = 300,
    [ValidateRange(10, 120)][int]$GracefulExitTimeoutSeconds = 20
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$windowInteropPath = Join-Path $PSScriptRoot "bricscad-runner-window-interop.ps1"
if (-not (Test-Path -LiteralPath $windowInteropPath -PathType Leaf)) {
    throw "Complete-family runner window helper is missing."
}
. $windowInteropPath

function Read-Qs3dCompleteFamilyMarker {
    param([Parameter(Mandatory = $true)][string]$Path)
    $marker = @{}
    foreach ($line in Get-Content -LiteralPath $Path) {
        if ([string]::IsNullOrWhiteSpace($line)) { continue }
        $separator = $line.IndexOf('=')
        if ($separator -le 0) { throw "Malformed complete-family marker line." }
        $key = $line.Substring(0, $separator).Trim()
        $value = $line.Substring($separator + 1).Trim()
        if ($marker.ContainsKey($key)) { throw "Duplicate complete-family marker key: $key" }
        $marker[$key] = $value
    }
    return $marker
}

function Require-Qs3dCompleteFamilyValue {
    param(
        [Parameter(Mandatory = $true)]$Marker,
        [Parameter(Mandatory = $true)][string]$Key,
        [Parameter(Mandatory = $true)][string]$Expected
    )
    if (-not $Marker.ContainsKey($Key)) { throw "Complete-family marker is missing '$Key'." }
    if (-not [string]::Equals([string]$Marker[$Key], $Expected, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Complete-family marker '$Key' expected '$Expected' but was '$($Marker[$Key])'."
    }
}

function Read-Qs3dCompleteFamilyInt {
    param([Parameter(Mandatory = $true)]$Marker, [Parameter(Mandatory = $true)][string]$Key)
    if (-not $Marker.ContainsKey($Key)) { throw "Complete-family marker is missing '$Key'." }
    [int]$value = 0
    if (-not [int]::TryParse([string]$Marker[$Key], [Globalization.NumberStyles]::None, [Globalization.CultureInfo]::InvariantCulture, [ref]$value) -or $value -lt 0) {
        throw "Complete-family marker '$Key' is not a non-negative integer."
    }
    return $value
}

function Restore-Qs3dCompleteFamilyEnvironment {
    param([Parameter(Mandatory = $true)][string]$Name, [AllowNull()][string]$Value)
    if ($null -eq $Value) { Remove-Item -LiteralPath ("Env:" + $Name) -ErrorAction SilentlyContinue }
    else { Set-Item -LiteralPath ("Env:" + $Name) -Value $Value }
}

function Stop-Qs3dCompleteFamilyProcess {
    param([AllowNull()][Diagnostics.Process]$Process)
    if ($null -eq $Process) { return }
    try {
        $Process.Refresh()
        if (-not $Process.HasExited) {
            Stop-Process -Id $Process.Id -Force -ErrorAction Stop
            $Process.WaitForExit(15000) | Out-Null
            $Process.Refresh()
        }
    }
    catch { throw }
    if (-not $Process.HasExited) { throw "Complete-family BricsCAD process did not exit." }
}

function Remove-Qs3dCompleteFamilyFile {
    param([Parameter(Mandatory = $true)][string]$Path)
    if (Test-Path -LiteralPath $Path) { Remove-Item -LiteralPath $Path -Force -ErrorAction Stop }
    if (Test-Path -LiteralPath $Path) { throw "Complete-family cleanup failed for a private file." }
}

if ([Environment]::OSVersion.Platform -ne [PlatformID]::Win32NT) { throw "Complete-family qualification requires Windows." }
if (-not [Environment]::UserInteractive) { throw "Complete-family qualification requires an interactive Windows session." }
if (-not $ConfirmDisposableCopy) { throw "Pass -ConfirmDisposableCopy only for a synthetic disposable drawing." }
if ([string]::IsNullOrWhiteSpace($Profile)) { throw "Complete-family qualification requires an initialized profile." }

$repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot ".."))
$BricsCadDir = [IO.Path]::GetFullPath($BricsCadDir)
$PluginDll = [IO.Path]::GetFullPath($PluginDll)
$DrawingCopy = [IO.Path]::GetFullPath($DrawingCopy)
$ArtifactDir = [IO.Path]::GetFullPath($ArtifactDir)
$ExpectedSourceSha = $ExpectedSourceSha.Trim().ToLowerInvariant()
if (-not [IO.Path]::GetFileName($DrawingCopy).EndsWith(".level-z-complete-family-probe-copy.dwg", [StringComparison]::OrdinalIgnoreCase)) {
    throw "DrawingCopy must use the guarded '*.level-z-complete-family-probe-copy.dwg' suffix."
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
    if (-not (Test-Path -LiteralPath $required -PathType Leaf)) { throw "Required complete-family input is missing." }
}

$expectedPlugin = [IO.Path]::GetFullPath((Join-Path $repoRoot "src\QS3D.BricsCAD.V25\bin\x64\Release\net48\QS3D.BricsCAD.V25.dll"))
if (-not [string]::Equals($PluginDll, $expectedPlugin, [StringComparison]::OrdinalIgnoreCase)) {
    throw "PluginDll must be the exact repository V25 x64 Release output."
}

$gitHeadOutput = @(& git -C $repoRoot rev-parse HEAD 2>$null)
$gitHeadExit = $LASTEXITCODE
if ($gitHeadExit -ne 0 -or $gitHeadOutput.Count -ne 1) { throw "Cannot resolve complete-family repository HEAD." }
$gitHead = ([string]$gitHeadOutput[0]).Trim().ToLowerInvariant()
if (-not [string]::Equals($gitHead, $ExpectedSourceSha, [StringComparison]::Ordinal)) {
    throw "ExpectedSourceSha does not match the complete-family worktree HEAD."
}
$gitStatus = @(& git -C $repoRoot status --porcelain=v1 --untracked-files=all 2>$null)
$gitStatusExit = $LASTEXITCODE
if ($gitStatusExit -ne 0) { throw "Cannot inspect the complete-family worktree." }
if ($gitStatus.Count -ne 0) { throw "Complete-family exact-SHA qualification requires a clean committed worktree." }

Assert-Qs3dExactSourceIdentity -RepoRoot $repoRoot -PluginDll $PluginDll -ExpectedSourceSha $ExpectedSourceSha
if (@(Get-Qs3dExactBricsCadProcesses -ExpectedExecutable $bricscadExe).Count -gt 0) {
    throw "Close existing BricsCAD V25 processes before complete-family qualification."
}

$nativeInsunits = switch ($NativeDrawingUnit) {
    "Millimeter" { "4" }
    "Meter" { "6" }
    default { throw "Unsupported native drawing unit." }
}

$projectSidecar = [IO.Path]::ChangeExtension($DrawingCopy, ".qsdb")
$sidecarBackup = $projectSidecar + ".bak"
$sidecarLock = $projectSidecar + ".lock"
$drawingLock = [IO.Path]::ChangeExtension($DrawingCopy, ".dwl")
$drawingLock2 = [IO.Path]::ChangeExtension($DrawingCopy, ".dwl2")
$drawingBackup = [IO.Path]::ChangeExtension($DrawingCopy, ".bak")
foreach ($privateInput in @($projectSidecar, $sidecarBackup, $sidecarLock, $drawingLock, $drawingLock2, $drawingBackup)) {
    if (Test-Path -LiteralPath $privateInput) { throw "Complete-family disposable copy has pre-existing private state." }
}

if (Test-Path -LiteralPath $ArtifactDir) {
    if (@(Get-ChildItem -LiteralPath $ArtifactDir -Force).Count -ne 0) { throw "Complete-family ArtifactDir must be empty." }
}
else { New-Item -ItemType Directory -Path $ArtifactDir | Out-Null }

$resultPath = Join-Path $ArtifactDir "level-z-complete-family-result.txt"
$scriptPath = Join-Path $ArtifactDir "level-z-complete-family.scr"
$metadataPath = Join-Path $ArtifactDir "level-z-complete-family-metadata.json"
$restoreCopyPath = Join-Path $ArtifactDir "level-z-complete-family-original.private.dwg"
foreach ($output in @($resultPath, $scriptPath, $metadataPath, $restoreCopyPath)) {
    if (Test-Path -LiteralPath $output) { throw "Complete-family output already exists." }
}

$originalAttributes = [IO.File]::GetAttributes($DrawingCopy)
$drawingHashBefore = (Get-FileHash -LiteralPath $DrawingCopy -Algorithm SHA256).Hash.ToUpperInvariant()
Copy-Item -LiteralPath $DrawingCopy -Destination $restoreCopyPath -ErrorAction Stop
if (-not [string]::Equals((Get-FileHash -LiteralPath $restoreCopyPath -Algorithm SHA256).Hash, $drawingHashBefore, [StringComparison]::OrdinalIgnoreCase)) {
    throw "Complete-family private restore copy hash mismatch."
}

$pluginHash = (Get-FileHash -LiteralPath $PluginDll -Algorithm SHA256).Hash.ToUpperInvariant()
$coreHash = (Get-FileHash -LiteralPath $coreDll -Algorithm SHA256).Hash.ToUpperInvariant()
$nonce = [Guid]::NewGuid().ToString("N")
$environmentNames = @(
    "QS3D_LEVEL_Z_COMPLETE_FAMILY_RESULT",
    "QS3D_LEVEL_Z_COMPLETE_FAMILY_NONCE",
    "QS3D_LEVEL_Z_COMPLETE_FAMILY_SOURCE_SHA"
)
$oldEnvironment = @{}
foreach ($name in $environmentNames) { $oldEnvironment[$name] = [Environment]::GetEnvironmentVariable($name, "Process") }

$process = $null
$marker = $null
$qualificationError = $null
$cleanupError = $null
$processCleanupVerified = $false
$scriptCleanupVerified = $false
$privateStateCleanupVerified = $false
$drawingRestoreVerified = $false
$drawingAttributesRestored = $false
$drawingReadOnlyBeforeLaunchVerified = $false
$drawingReadOnlyThroughHostExitVerified = $false
$drawingUnwrittenVerified = $false
$proxyInformationDialogsDismissed = 0
$startedAt = Get-Date

try {
    $guardedAttributes = [IO.FileAttributes](([int]$originalAttributes) -bor [int][IO.FileAttributes]::ReadOnly)
    [IO.File]::SetAttributes($DrawingCopy, $guardedAttributes)
    $drawingReadOnlyBeforeLaunchVerified = (([int][IO.File]::GetAttributes($DrawingCopy) -band [int][IO.FileAttributes]::ReadOnly) -ne 0)
    if (-not $drawingReadOnlyBeforeLaunchVerified) { throw "Complete-family drawing read-only guard failed." }

    $env:QS3D_LEVEL_Z_COMPLETE_FAMILY_RESULT = $resultPath
    $env:QS3D_LEVEL_Z_COMPLETE_FAMILY_NONCE = $nonce
    $env:QS3D_LEVEL_Z_COMPLETE_FAMILY_SOURCE_SHA = $ExpectedSourceSha

    $script = @(
        "FILEDIA", "0",
        "_.OPEN", ('"' + $DrawingCopy + '"'),
        "CMDECHO", "1",
        "TILEMODE", "1",
        "INSUNITS", $nativeInsunits,
        "UCS", "W",
        "NETLOAD", ('"' + $PluginDll + '"'),
        "QS3DLEVELZCOMPLETEFAMILY",
        "_.CLOSE", "_N",
        "_.QUIT", "_N"
    )
    Set-Content -LiteralPath $scriptPath -Value $script -Encoding ASCII

    $arguments = '/Automation /P "' + $Profile + '" /B "' + $scriptPath + '"'
    $process = Start-Process -FilePath $bricscadExe -ArgumentList $arguments -PassThru -WindowStyle Hidden -WorkingDirectory $ArtifactDir
    $deadline = (Get-Date).AddSeconds($StartupTimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        if (Test-Path -LiteralPath $resultPath -PathType Leaf) { break }
        $proxyInformationDialogsDismissed += Close-Qs3dProxyInformationDialog -Process $process
        $process.Refresh()
        if ($process.HasExited) { throw "BricsCAD exited before complete-family marker. ExitCode=$($process.ExitCode)" }
        Start-Sleep -Milliseconds 500
    }
    if (-not (Test-Path -LiteralPath $resultPath -PathType Leaf)) {
        throw "Timed out waiting for QS3DLEVELZCOMPLETEFAMILY."
    }

    $gracefulExit = $false
    $process.Refresh()
    if ($process.HasExited) { $gracefulExit = $true }
    else {
        $gracefulDeadline = (Get-Date).AddSeconds($GracefulExitTimeoutSeconds)
        while ((Get-Date) -lt $gracefulDeadline) {
            if ($process.WaitForExit(250)) { $gracefulExit = $true; break }
        }
    }
    if (-not $gracefulExit) { throw "BricsCAD did not exit gracefully after complete-family marker." }

    $marker = Read-Qs3dCompleteFamilyMarker -Path $resultPath
    if ($marker.ContainsKey("status") -and [string]::Equals([string]$marker["status"], "FAIL", [StringComparison]::OrdinalIgnoreCase)) {
        if (-not $marker.ContainsKey("error_code") -or [string]$marker["error_code"] -ne "LOCAL_003_COMPLETE_FAMILY_RUNTIME_FAILED") {
            throw "Complete-family failure marker is not sanitized."
        }
        throw "Complete-family runtime command reported sanitized failure."
    }
    Require-Qs3dCompleteFamilyValue -Marker $marker -Key "status" -Expected "PASS"
    Require-Qs3dCompleteFamilyValue -Marker $marker -Key "command" -Expected "QS3DLEVELZCOMPLETEFAMILY"
    Require-Qs3dCompleteFamilyValue -Marker $marker -Key "process" -Expected "bricscad"
    Require-Qs3dCompleteFamilyValue -Marker $marker -Key "nonce" -Expected $nonce
    Require-Qs3dCompleteFamilyValue -Marker $marker -Key "source_sha" -Expected $ExpectedSourceSha
    Require-Qs3dCompleteFamilyValue -Marker $marker -Key "schema" -Expected "LOCAL_003_COMPLETE_FAMILY_RUNTIME_V1"
    Require-Qs3dCompleteFamilyValue -Marker $marker -Key "qualification_boundary" -Expected "LOCAL_003_COMPLETE_FAMILY_HOSTS_ONLY"
    Require-Qs3dCompleteFamilyValue -Marker $marker -Key "production_local003_qualified" -Expected "false"
    Require-Qs3dCompleteFamilyValue -Marker $marker -Key "native_drawing_unit" -Expected $NativeDrawingUnit

    foreach ($pair in @(
        @{ Key = "host_family_count"; Expected = 10 },
        @{ Key = "family_case_count"; Expected = 30 },
        @{ Key = "legacy_family_count"; Expected = 10 },
        @{ Key = "bottom_only_family_count"; Expected = 10 },
        @{ Key = "bottom_top_family_count"; Expected = 10 },
        @{ Key = "hosted_opening_count"; Expected = 2 }
    )) {
        $actual = Read-Qs3dCompleteFamilyInt -Marker $marker -Key $pair.Key
        if ($actual -ne $pair.Expected) { throw "Complete-family marker count '$($pair.Key)' changed." }
    }
    foreach ($key in @(
        "door_straight_cut", "wallopening_straight_cut", "top_only_fail_closed",
        "missing_level_fail_closed", "ambiguous_level_fail_closed", "non_finite_offset_fail_closed",
        "invalid_vertical_range_fail_closed"
    )) {
        Require-Qs3dCompleteFamilyValue -Marker $marker -Key $key -Expected "true"
    }
    Require-Qs3dCompleteFamilyValue -Marker $marker -Key "level_health_issue_count" -Expected "0"
}
catch {
    $qualificationError = $_
}
finally {
    try {
        if ($null -ne $process) { Stop-Qs3dCompleteFamilyProcess -Process $process }
        if (-not (Wait-Qs3dNoExactBricsCadProcesses -ExpectedExecutable $bricscadExe -TimeoutSeconds 30)) {
            throw "Complete-family cleanup left a BricsCAD V25 process."
        }
        $processCleanupVerified = $true
        $drawingReadOnlyThroughHostExitVerified = (([int][IO.File]::GetAttributes($DrawingCopy) -band [int][IO.FileAttributes]::ReadOnly) -ne 0)
        $drawingHashAfterHost = (Get-FileHash -LiteralPath $DrawingCopy -Algorithm SHA256).Hash.ToUpperInvariant()
        $drawingUnwrittenVerified = [string]::Equals($drawingHashBefore, $drawingHashAfterHost, [StringComparison]::Ordinal)
    }
    catch { $cleanupError = $_ }

    try {
        foreach ($privatePath in @($scriptPath, $projectSidecar, $sidecarBackup, $sidecarLock, $drawingLock, $drawingLock2, $drawingBackup)) {
            Remove-Qs3dCompleteFamilyFile -Path $privatePath
        }
        $scriptCleanupVerified = -not (Test-Path -LiteralPath $scriptPath)
        $privateStateCleanupVerified = -not (
            (Test-Path -LiteralPath $projectSidecar) -or (Test-Path -LiteralPath $sidecarBackup) -or
            (Test-Path -LiteralPath $sidecarLock) -or (Test-Path -LiteralPath $drawingLock) -or
            (Test-Path -LiteralPath $drawingLock2) -or (Test-Path -LiteralPath $drawingBackup)
        )
        if (Test-Path -LiteralPath $restoreCopyPath -PathType Leaf) {
            $currentAttributes = [IO.File]::GetAttributes($DrawingCopy)
            $writableAttributes = [IO.FileAttributes](([int]$currentAttributes) -band (-bnot [int][IO.FileAttributes]::ReadOnly))
            [IO.File]::SetAttributes($DrawingCopy, $writableAttributes)
            Copy-Item -LiteralPath $restoreCopyPath -Destination $DrawingCopy -Force -ErrorAction Stop
            Remove-Qs3dCompleteFamilyFile -Path $restoreCopyPath
        }
        [IO.File]::SetAttributes($DrawingCopy, $originalAttributes)
        $drawingRestoreVerified = [string]::Equals(
            (Get-FileHash -LiteralPath $DrawingCopy -Algorithm SHA256).Hash,
            $drawingHashBefore,
            [StringComparison]::OrdinalIgnoreCase)
        $drawingAttributesRestored = [IO.File]::GetAttributes($DrawingCopy) -eq $originalAttributes
    }
    catch { if ($null -eq $cleanupError) { $cleanupError = $_ } }
    foreach ($name in $environmentNames) { Restore-Qs3dCompleteFamilyEnvironment -Name $name -Value $oldEnvironment[$name] }
}

$metadataStatus = if ($null -eq $qualificationError -and $null -eq $cleanupError) { "PASS" } else { "FAIL" }
$metadata = [ordered]@{
    status = $metadataStatus
    git_sha = $gitHead
    started_at = $startedAt.ToUniversalTime().ToString("O")
    completed_at = (Get-Date).ToUniversalTime().ToString("O")
    bricscad_file_version = (Get-Item -LiteralPath $bricscadExe).VersionInfo.FileVersion
    native_drawing_unit = $NativeDrawingUnit
    native_insunits = $nativeInsunits
    plugin_sha256 = $pluginHash
    core_sha256 = $coreHash
    drawing_copy_sha256_before = $drawingHashBefore
    drawing_copy_sha256_restored = (Get-FileHash -LiteralPath $DrawingCopy -Algorithm SHA256).Hash.ToUpperInvariant()
    process_cleanup_verified = $processCleanupVerified
    script_cleanup_verified = $scriptCleanupVerified
    private_state_cleanup_verified = $privateStateCleanupVerified
    drawing_read_only_before_launch_verified = $drawingReadOnlyBeforeLaunchVerified
    drawing_read_only_through_host_exit_verified = $drawingReadOnlyThroughHostExitVerified
    drawing_unwritten_verified = $drawingUnwrittenVerified
    drawing_restore_verified = $drawingRestoreVerified
    drawing_attributes_restored = $drawingAttributesRestored
    proxy_information_dialogs_dismissed = $proxyInformationDialogsDismissed
    marker = if ($null -ne $marker) { $marker } else { @{} }
}
$metadata | ConvertTo-Json -Depth 7 | Set-Content -LiteralPath $metadataPath -Encoding UTF8

if ($null -ne $cleanupError) { throw $cleanupError }
if ($null -ne $qualificationError) { throw $qualificationError }
Write-Host "QS3D BricsCAD V25 LOCAL-003 complete-family host matrix PASS"
Write-Host "Native drawing unit: $NativeDrawingUnit (INSUNITS=$nativeInsunits)"
Write-Host "Marker: $resultPath"
Write-Host "Metadata: $metadataPath"
