param(
    [Parameter(Mandatory = $true)][string]$BricsCadDir,
    [Parameter(Mandatory = $true)][string]$PluginDll,
    [Parameter(Mandatory = $true)][string]$DrawingCopy,
    [Parameter(Mandatory = $true)][string]$Profile,
    [Parameter(Mandatory = $true)][string]$ArtifactDir,
    [Parameter(Mandatory = $true)][ValidatePattern("^[0-9a-fA-F]{40}$")][string]$ExpectedSourceSha,
    [Parameter(Mandatory = $true)][switch]$ConfirmDisposableCopy,
    [ValidateSet("Millimeter", "Meter")][string]$NativeDrawingUnit = "Millimeter",
    [ValidateRange(30, 900)][int]$StartupTimeoutSeconds = 240,
    [ValidateRange(15, 120)][int]$GracefulExitTimeoutSeconds = 15
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$windowInteropPath = Join-Path $PSScriptRoot "bricscad-runner-window-interop.ps1"
if (-not (Test-Path -LiteralPath $windowInteropPath -PathType Leaf)) {
    throw "Curved structural runner window interop helper is missing: $windowInteropPath"
}
. $windowInteropPath

function Read-Qs3dCurvedMarker {
    param([Parameter(Mandatory = $true)][string]$Path)
    $marker = @{}
    foreach ($line in Get-Content -LiteralPath $Path) {
        if ([string]::IsNullOrWhiteSpace($line)) { continue }
        $separator = $line.IndexOf('=')
        if ($separator -le 0) { throw "Malformed curved structural marker line." }
        $key = $line.Substring(0, $separator).Trim()
        $value = $line.Substring($separator + 1).Trim()
        if ($marker.ContainsKey($key)) { throw "Duplicate curved structural marker key: $key" }
        $marker[$key] = $value
    }
    return $marker
}

function Require-Qs3dCurvedValue {
    param(
        [Parameter(Mandatory = $true)]$Marker,
        [Parameter(Mandatory = $true)][string]$Key,
        [Parameter(Mandatory = $true)][string]$Expected
    )
    if (-not $Marker.ContainsKey($Key)) { throw "Curved structural marker is missing '$Key'." }
    if (-not [string]::Equals([string]$Marker[$Key], $Expected, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Curved structural marker '$Key' did not match the expected value."
    }
}

function Read-Qs3dCurvedNumber {
    param([Parameter(Mandatory = $true)]$Marker, [Parameter(Mandatory = $true)][string]$Key)
    if (-not $Marker.ContainsKey($Key)) { throw "Curved structural marker is missing '$Key'." }
    [double]$value = 0
    if (-not [double]::TryParse([string]$Marker[$Key], [Globalization.NumberStyles]::Float, [Globalization.CultureInfo]::InvariantCulture, [ref]$value) -or
        [double]::IsNaN($value) -or [double]::IsInfinity($value)) {
        throw "Curved structural marker '$Key' is not a finite invariant number."
    }
    return $value
}

function Require-Qs3dCurvedNumber {
    param(
        [Parameter(Mandatory = $true)]$Marker,
        [Parameter(Mandatory = $true)][string]$Key,
        [Parameter(Mandatory = $true)][double]$Expected
    )
    $actual = Read-Qs3dCurvedNumber -Marker $Marker -Key $Key
    $tolerance = [Math]::Max(0.0000001, [Math]::Max([Math]::Abs($Expected), [Math]::Abs($actual)) * 0.000001)
    if ([Math]::Abs($Expected - $actual) -gt $tolerance) {
        throw "Curved structural marker '$Key' is outside tolerance."
    }
}

function Require-Qs3dCurvedPositiveNumber {
    param([Parameter(Mandatory = $true)]$Marker, [Parameter(Mandatory = $true)][string]$Key)
    $value = Read-Qs3dCurvedNumber -Marker $Marker -Key $Key
    if ($value -le 0) { throw "Curved structural marker '$Key' must be positive." }
    return $value
}

function Require-Qs3dCurvedFailure {
    param([Parameter(Mandatory = $true)]$Marker)
    Require-Qs3dCurvedValue -Marker $Marker -Key "command" -Expected "QS3DCURVEDSTRUCTURALPROBE"
    $allowedStages = @("context", "source_creation", "positive_cases", "fail_closed", "marker")
    $allowedCases = @(
        "none", "beam_line", "beam_arc", "beam_circle", "beam_polyline_straight",
        "beam_polyline_curved", "slab_circle", "column_circle", "closed_beam_polyline",
        "non_wcs_beam_circle"
    )
    if (-not $Marker.ContainsKey("error_stage") -or -not ($allowedStages -contains [string]$Marker["error_stage"])) {
        throw "Curved structural marker has an invalid sanitized failure stage."
    }
    if (-not $Marker.ContainsKey("error_case") -or -not ($allowedCases -contains [string]$Marker["error_case"])) {
        throw "Curved structural marker has an invalid sanitized failure case."
    }
    if (-not $Marker.ContainsKey("exception_type") -or [string]$Marker["exception_type"] -notmatch '^[A-Za-z0-9_.+`]+$') {
        throw "Curved structural marker has an invalid sanitized exception type."
    }
    if (-not $Marker.ContainsKey("exception_hresult") -or [string]$Marker["exception_hresult"] -notmatch '^0x[0-9A-F]{8}$') {
        throw "Curved structural marker has an invalid sanitized HRESULT."
    }
    throw "Curved structural runtime probe reported sanitized failure at stage '$([string]$Marker["error_stage"])' / case '$([string]$Marker["error_case"])'."
}

function Restore-EnvironmentValue {
    param([Parameter(Mandatory = $true)][string]$Name, [AllowNull()][string]$Value)
    if ($null -eq $Value) { Remove-Item -LiteralPath ("Env:" + $Name) -ErrorAction SilentlyContinue }
    else { Set-Item -LiteralPath ("Env:" + $Name) -Value $Value }
}

function Stop-Qs3dCurvedProcess {
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

function Restore-Qs3dCurvedDrawingAndPrivateState {
    param(
        [Parameter(Mandatory = $true)][string]$ScriptPath,
        [Parameter(Mandatory = $true)][string]$ProjectSidecar,
        [Parameter(Mandatory = $true)][string]$DrawingCopy,
        [Parameter(Mandatory = $true)][string]$DrawingBackupPath,
        [Parameter(Mandatory = $true)][IO.FileAttributes]$OriginalDrawingAttributes
    )
    if (Test-Path -LiteralPath $ScriptPath -PathType Leaf) {
        Remove-Item -LiteralPath $ScriptPath -Force -ErrorAction SilentlyContinue
    }
    foreach ($privatePath in @(
        $ProjectSidecar,
        ($ProjectSidecar + ".bak"),
        [IO.Path]::ChangeExtension($DrawingCopy, ".bak"),
        [IO.Path]::ChangeExtension($DrawingCopy, ".dwl"),
        [IO.Path]::ChangeExtension($DrawingCopy, ".dwl2")
    )) {
        if (Test-Path -LiteralPath $privatePath -PathType Leaf) {
            Remove-Item -LiteralPath $privatePath -Force -ErrorAction SilentlyContinue
        }
    }
    try {
        if (Test-Path -LiteralPath $DrawingBackupPath -PathType Leaf) {
            if (Test-Path -LiteralPath $DrawingCopy -PathType Leaf) {
                $current = [IO.File]::GetAttributes($DrawingCopy)
                $writable = [IO.FileAttributes](([int]$current) -band (-bnot [int][IO.FileAttributes]::ReadOnly))
                [IO.File]::SetAttributes($DrawingCopy, $writable)
            }
            Copy-Item -LiteralPath $DrawingBackupPath -Destination $DrawingCopy -Force -ErrorAction Stop
            Remove-Item -LiteralPath $DrawingBackupPath -Force -ErrorAction SilentlyContinue
        }
    }
    finally {
        if (Test-Path -LiteralPath $DrawingCopy -PathType Leaf) {
            [IO.File]::SetAttributes($DrawingCopy, $OriginalDrawingAttributes)
        }
    }
}

if ([Environment]::OSVersion.Platform -ne [PlatformID]::Win32NT) { throw "Curved structural runtime qualification requires Windows." }
if (-not [Environment]::UserInteractive) { throw "Curved structural runtime qualification requires an interactive Windows session." }
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
if ($sourceShaExitCode -ne 0 -or $sourceShaOutput.Count -ne 1 -or
    $resolvedSourceSha -notmatch "^[0-9a-fA-F]{40}$" -or
    -not [string]::Equals($resolvedSourceSha, $ExpectedSourceSha, [StringComparison]::OrdinalIgnoreCase)) {
    throw "ExpectedSourceSha does not match the current repository HEAD."
}
$worktreeStatus = @(& git -C $repoRoot status --porcelain=v1 --untracked-files=all 2>$null)
if ($LASTEXITCODE -ne 0) { throw "Unable to verify the repository worktree for exact-SHA curved structural qualification." }
if ($worktreeStatus.Count -gt 0) { throw "Exact-SHA curved structural qualification requires a clean repository worktree." }
$ExpectedSourceSha = $resolvedSourceSha.ToLowerInvariant()

if (-not [IO.Path]::GetFileName($DrawingCopy).EndsWith(".curved-structural-probe-copy.dwg", [StringComparison]::OrdinalIgnoreCase)) {
    throw "DrawingCopy must use the guarded '*.curved-structural-probe-copy.dwg' suffix."
}

$bricscadExe = Join-Path $BricsCadDir "bricscad.exe"
$coreDll = Join-Path (Split-Path -Parent $PluginDll) "QS3D.Core.dll"
foreach ($required in @($bricscadExe, $PluginDll, $coreDll, $DrawingCopy)) {
    if (-not (Test-Path -LiteralPath $required -PathType Leaf)) { throw "Required curved structural runtime input is missing." }
}
$expectedAssemblyRevision = "+" + $ExpectedSourceSha
foreach ($assemblyPath in @($PluginDll, $coreDll)) {
    $productVersion = [string](Get-Item -LiteralPath $assemblyPath).VersionInfo.ProductVersion
    if (-not $productVersion.EndsWith($expectedAssemblyRevision, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Assembly was not built from ExpectedSourceSha."
    }
}
if (@(Get-Process -Name "bricscad" -ErrorAction SilentlyContinue).Count -gt 0) {
    throw "Close existing BricsCAD processes before starting the isolated curved structural runtime probe."
}

$projectSidecar = [IO.Path]::ChangeExtension($DrawingCopy, ".qsdb")
if ((Test-Path -LiteralPath $projectSidecar) -or (Test-Path -LiteralPath ($projectSidecar + ".bak"))) {
    throw "The disposable curved structural drawing copy must not have a pre-existing QS3D sidecar."
}

New-Item -ItemType Directory -Force -Path $ArtifactDir | Out-Null
$resultPath = Join-Path $ArtifactDir "curved-structural-runtime-result.txt"
$scriptPath = Join-Path $ArtifactDir "curved-structural-runtime.scr"
$metadataPath = Join-Path $ArtifactDir "curved-structural-runtime-metadata.json"
$drawingBackupPath = Join-Path $ArtifactDir "curved-structural-original.dwg"
foreach ($output in @($resultPath, $scriptPath, $metadataPath, $drawingBackupPath)) {
    if (Test-Path -LiteralPath $output) { throw "Curved structural runtime output must not already exist." }
}

$originalDrawingAttributes = [IO.File]::GetAttributes($DrawingCopy)
$drawingHashBefore = (Get-FileHash -LiteralPath $DrawingCopy -Algorithm SHA256).Hash.ToUpperInvariant()
Copy-Item -LiteralPath $DrawingCopy -Destination $drawingBackupPath -ErrorAction Stop
$pluginHash = (Get-FileHash -LiteralPath $PluginDll -Algorithm SHA256).Hash.ToUpperInvariant()
$nonce = [Guid]::NewGuid().ToString("N")
$oldResult = [Environment]::GetEnvironmentVariable("QS3D_CURVED_STRUCTURAL_RESULT", "Process")
$oldNonce = [Environment]::GetEnvironmentVariable("QS3D_CURVED_STRUCTURAL_NONCE", "Process")
$oldSourceSha = [Environment]::GetEnvironmentVariable("QS3D_CURVED_STRUCTURAL_SOURCE_SHA", "Process")
$process = $null
$proxyInformationDialogsDismissed = 0
$gracefulExit = $false
$processCleanupVerified = $false
$drawingReadOnlyBeforeLaunchVerified = $false
$drawingReadOnlyThroughHostExitVerified = $false
$drawingUnwrittenVerified = $false
$drawingRestoreVerified = $false
$drawingAttributesRestored = $false
$privateStateCleanupVerified = $false
$scriptCleanupVerified = $false
$drawingHashAfter = $null
$marker = $null
$startedAt = Get-Date
$nativeInsunits = switch ($NativeDrawingUnit) {
    "Millimeter" { "4" }
    "Meter" { "6" }
    default { throw "Unsupported native drawing unit." }
}

try {
    $guardedDrawingAttributes = [IO.FileAttributes](([int]$originalDrawingAttributes) -bor [int][IO.FileAttributes]::ReadOnly)
    [IO.File]::SetAttributes($DrawingCopy, $guardedDrawingAttributes)
    $observedDrawingAttributes = [IO.File]::GetAttributes($DrawingCopy)
    $drawingReadOnlyBeforeLaunchVerified = (([int]$observedDrawingAttributes -band [int][IO.FileAttributes]::ReadOnly) -ne 0)
    if (-not $drawingReadOnlyBeforeLaunchVerified) { throw "The disposable curved structural drawing could not be guarded read-only before launch." }

    $env:QS3D_CURVED_STRUCTURAL_RESULT = $resultPath
    $env:QS3D_CURVED_STRUCTURAL_NONCE = $nonce
    $env:QS3D_CURVED_STRUCTURAL_SOURCE_SHA = $ExpectedSourceSha
    $script = @(
        "FILEDIA", "0",
        "_.OPEN", ('"' + $DrawingCopy + '"'),
        "CMDECHO", "1",
        "TILEMODE", "1",
        "INSUNITS", $nativeInsunits,
        "UCS", "W",
        "NETLOAD", ('"' + $PluginDll + '"'),
        "QS3DCURVEDSTRUCTURALPROBE",
        "_.CLOSE", "_N",
        "_.QUIT", "_N"
    )
    Set-Content -LiteralPath $scriptPath -Value $script -Encoding ASCII

    $argumentParts = New-Object System.Collections.Generic.List[string]
    $argumentParts.Add('/Automation')
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
        if ($process.HasExited) { throw "BricsCAD exited before the curved structural marker. ExitCode=$($process.ExitCode)" }
        Start-Sleep -Milliseconds 500
    }
    if (-not (Test-Path -LiteralPath $resultPath -PathType Leaf)) {
        throw "Timed out waiting for QS3DCURVEDSTRUCTURALPROBE after $StartupTimeoutSeconds seconds."
    }

    $process.Refresh()
    if (-not $process.HasExited) { $gracefulExit = $process.WaitForExit($GracefulExitTimeoutSeconds * 1000) }
    else { $gracefulExit = $true }

    $marker = Read-Qs3dCurvedMarker -Path $resultPath
    if ([string]::Equals([string]$marker["status"], "FAIL", [StringComparison]::OrdinalIgnoreCase)) {
        Require-Qs3dCurvedFailure -Marker $marker
    }
    Require-Qs3dCurvedValue -Marker $marker -Key "status" -Expected "PASS"
    Require-Qs3dCurvedValue -Marker $marker -Key "command" -Expected "QS3DCURVEDSTRUCTURALPROBE"
    Require-Qs3dCurvedValue -Marker $marker -Key "process" -Expected "bricscad"
    Require-Qs3dCurvedValue -Marker $marker -Key "nonce" -Expected $nonce
    Require-Qs3dCurvedValue -Marker $marker -Key "source_sha" -Expected $ExpectedSourceSha
    Require-Qs3dCurvedValue -Marker $marker -Key "schema" -Expected "QS3D_CURVED_STRUCTURAL_RUNTIME_V1"
    Require-Qs3dCurvedValue -Marker $marker -Key "is_64bit" -Expected "true"
    Require-Qs3dCurvedValue -Marker $marker -Key "native_drawing_unit" -Expected $NativeDrawingUnit
    Require-Qs3dCurvedValue -Marker $marker -Key "positive_case_count" -Expected "7"
    Require-Qs3dCurvedValue -Marker $marker -Key "rebuild_count" -Expected "7"
    Require-Qs3dCurvedValue -Marker $marker -Key "closed_beam_polyline_fail_closed" -Expected "true"
    Require-Qs3dCurvedValue -Marker $marker -Key "non_wcs_beam_circle_fail_closed" -Expected "true"

    Require-Qs3dCurvedNumber -Marker $marker -Key "beam_line_length_m" -Expected 4.0
    Require-Qs3dCurvedNumber -Marker $marker -Key "beam_arc_length_m" -Expected (3.0 * [Math]::PI / 2.0)
    Require-Qs3dCurvedNumber -Marker $marker -Key "beam_circle_length_m" -Expected (4.0 * [Math]::PI)
    Require-Qs3dCurvedNumber -Marker $marker -Key "beam_polyline_straight_length_m" -Expected (4.0 + [Math]::Sqrt(8.0))
    Require-Qs3dCurvedNumber -Marker $marker -Key "beam_polyline_curved_length_m" -Expected ([Math]::PI * [Math]::Sqrt(2.0) + [Math]::Sqrt(8.0))
    Require-Qs3dCurvedNumber -Marker $marker -Key "slab_circle_area_m2" -Expected (4.0 * [Math]::PI)
    Require-Qs3dCurvedNumber -Marker $marker -Key "column_circle_area_m2" -Expected (0.16 * [Math]::PI)

    foreach ($case in @("beam_line", "beam_arc", "beam_circle", "beam_polyline_straight", "beam_polyline_curved")) {
        Require-Qs3dCurvedNumber -Marker $marker -Key ($case + "_min_z_m") -Expected 0.0
        Require-Qs3dCurvedNumber -Marker $marker -Key ($case + "_max_z_m") -Expected 0.5
        $null = Require-Qs3dCurvedPositiveNumber -Marker $marker -Key ($case + "_volume_m3")
    }
    Require-Qs3dCurvedNumber -Marker $marker -Key "slab_circle_min_z_m" -Expected 0.0
    Require-Qs3dCurvedNumber -Marker $marker -Key "slab_circle_max_z_m" -Expected 0.2
    Require-Qs3dCurvedNumber -Marker $marker -Key "column_circle_min_z_m" -Expected 0.0
    Require-Qs3dCurvedNumber -Marker $marker -Key "column_circle_max_z_m" -Expected 3.0
    $null = Require-Qs3dCurvedPositiveNumber -Marker $marker -Key "slab_circle_volume_m3"
    $null = Require-Qs3dCurvedPositiveNumber -Marker $marker -Key "column_circle_volume_m3"
    if (-not $gracefulExit) { throw "BricsCAD did not exit gracefully after the curved structural marker." }
}
finally {
    Stop-Qs3dCurvedProcess -Process $process
    try {
        $processCleanupVerified = @(Get-Process -Name "bricscad" -ErrorAction SilentlyContinue).Count -eq 0
        $observedDrawingAttributes = [IO.File]::GetAttributes($DrawingCopy)
        $drawingReadOnlyThroughHostExitVerified = (([int]$observedDrawingAttributes -band [int][IO.FileAttributes]::ReadOnly) -ne 0)
        $drawingHashAfter = (Get-FileHash -LiteralPath $DrawingCopy -Algorithm SHA256).Hash.ToUpperInvariant()
        $drawingUnwrittenVerified = [string]::Equals($drawingHashBefore, $drawingHashAfter, [StringComparison]::Ordinal)
        if (-not $processCleanupVerified) { throw "Curved structural runtime left a BricsCAD process after cleanup." }
        if (-not $drawingReadOnlyThroughHostExitVerified) { throw "The disposable curved structural drawing lost its read-only guard." }
        if (-not $drawingUnwrittenVerified) { throw "The disposable curved structural drawing was written despite its read-only guard." }
    }
    finally {
        try {
            Restore-Qs3dCurvedDrawingAndPrivateState -ScriptPath $scriptPath -ProjectSidecar $projectSidecar -DrawingCopy $DrawingCopy -DrawingBackupPath $drawingBackupPath -OriginalDrawingAttributes $originalDrawingAttributes
            $scriptCleanupVerified = -not (Test-Path -LiteralPath $scriptPath)
            $privateStateCleanupVerified = -not (
                (Test-Path -LiteralPath $projectSidecar) -or
                (Test-Path -LiteralPath ($projectSidecar + ".bak")) -or
                (Test-Path -LiteralPath ([IO.Path]::ChangeExtension($DrawingCopy, ".bak"))) -or
                (Test-Path -LiteralPath ([IO.Path]::ChangeExtension($DrawingCopy, ".dwl"))) -or
                (Test-Path -LiteralPath ([IO.Path]::ChangeExtension($DrawingCopy, ".dwl2")))
            )
            if (-not $scriptCleanupVerified) { throw "Curved structural runtime left its private script after cleanup." }
            if (-not $privateStateCleanupVerified) { throw "Curved structural runtime left private drawing state after cleanup." }
            $drawingRestoreVerified = -not (Test-Path -LiteralPath $drawingBackupPath) -and [string]::Equals(
                $drawingHashBefore,
                (Get-FileHash -LiteralPath $DrawingCopy -Algorithm SHA256).Hash.ToUpperInvariant(),
                [StringComparison]::Ordinal)
            if (-not $drawingRestoreVerified) { throw "The disposable curved structural drawing backup restoration failed." }
            $drawingAttributesRestored = [IO.File]::GetAttributes($DrawingCopy) -eq $originalDrawingAttributes
            if (-not $drawingAttributesRestored) { throw "The disposable curved structural drawing attributes were not restored." }
        }
        finally {
            Restore-EnvironmentValue -Name "QS3D_CURVED_STRUCTURAL_RESULT" -Value $oldResult
            Restore-EnvironmentValue -Name "QS3D_CURVED_STRUCTURAL_NONCE" -Value $oldNonce
            Restore-EnvironmentValue -Name "QS3D_CURVED_STRUCTURAL_SOURCE_SHA" -Value $oldSourceSha
        }
    }
}

$metadata = [ordered]@{
    status = "PASS"
    source_sha = $ExpectedSourceSha
    started_at = $startedAt.ToUniversalTime().ToString("O")
    completed_at = (Get-Date).ToUniversalTime().ToString("O")
    profile = $Profile
    bricscad_file_version = (Get-Item -LiteralPath $bricscadExe).VersionInfo.FileVersion
    native_drawing_unit = $NativeDrawingUnit
    native_insunits = $nativeInsunits
    plugin_sha256 = $pluginHash
    drawing_copy_sha256_before = $drawingHashBefore
    drawing_copy_sha256_after = $drawingHashAfter
    proxy_information_dialogs_dismissed = $proxyInformationDialogsDismissed
    graceful_exit = $gracefulExit
    process_cleanup_verified = $processCleanupVerified
    script_cleanup_verified = $scriptCleanupVerified
    private_state_cleanup_verified = $privateStateCleanupVerified
    drawing_read_only_before_launch_verified = $drawingReadOnlyBeforeLaunchVerified
    drawing_read_only_through_host_exit_verified = $drawingReadOnlyThroughHostExitVerified
    drawing_unwritten_verified = $drawingUnwrittenVerified
    drawing_restore_verified = $drawingRestoreVerified
    drawing_attributes_restored = $drawingAttributesRestored
    marker = $marker
}
$metadata | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $metadataPath -Encoding UTF8

Write-Host "QS3D BricsCAD V25 curved structural runtime PASS"
Write-Host "Native drawing unit: $NativeDrawingUnit (INSUNITS=$nativeInsunits)"
Write-Host "Plugin SHA256: $pluginHash"
Write-Host "Marker: $resultPath"
Write-Host "Metadata: $metadataPath"
