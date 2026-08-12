param(
    [Parameter(Mandatory = $true)][string]$BricsCadDir,
    [Parameter(Mandatory = $true)][string]$PluginDll,
    [Parameter(Mandatory = $true)][string]$DrawingCopy,
    [Parameter(Mandatory = $true)][string]$Profile,
    [Parameter(Mandatory = $true)][string]$ArtifactDir,
    [Parameter(Mandatory = $true)][switch]$ConfirmDisposableCopy,
    [ValidateRange(30, 900)][int]$StartupTimeoutSeconds = 300
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$windowInteropPath = Join-Path $PSScriptRoot "bricscad-runner-window-interop.ps1"
if (-not (Test-Path -LiteralPath $windowInteropPath -PathType Leaf)) {
    throw "Curtain-opening runner window interop helper is missing: $windowInteropPath"
}
. $windowInteropPath

function Read-Qs3dMarker {
    param([Parameter(Mandatory = $true)][string]$Path)
    $marker = @{}
    foreach ($line in Get-Content -LiteralPath $Path) {
        if ([string]::IsNullOrWhiteSpace($line)) { continue }
        $separator = $line.IndexOf('=')
        if ($separator -le 0) { throw "Malformed Curtain-opening marker line: $line" }
        $key = $line.Substring(0, $separator).Trim()
        $value = $line.Substring($separator + 1).Trim()
        if ($marker.ContainsKey($key)) { throw "Duplicate Curtain-opening marker key: $key" }
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
    if (-not $Marker.ContainsKey($Key)) { throw "Curtain-opening marker is missing '$Key'." }
    if (-not [string]::Equals([string]$Marker[$Key], $Expected, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Curtain-opening marker '$Key' expected '$Expected' but was '$($Marker[$Key])'."
    }
}

function Read-NonNegativeMarkerInt {
    param([Parameter(Mandatory = $true)]$Marker, [Parameter(Mandatory = $true)][string]$Key)
    if (-not $Marker.ContainsKey($Key)) { throw "Curtain-opening marker is missing '$Key'." }
    [int]$value = 0
    if (-not [int]::TryParse([string]$Marker[$Key], [Globalization.NumberStyles]::None, [Globalization.CultureInfo]::InvariantCulture, [ref]$value) -or $value -lt 0) {
        throw "Curtain-opening marker '$Key' is not a non-negative invariant integer."
    }
    return $value
}

function Read-PositiveMarkerInt {
    param([Parameter(Mandatory = $true)]$Marker, [Parameter(Mandatory = $true)][string]$Key)
    $value = Read-NonNegativeMarkerInt -Marker $Marker -Key $Key
    if ($value -le 0) { throw "Curtain-opening marker '$Key' must be positive." }
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
    if (-not $Process.HasExited) { throw "Launched BricsCAD Curtain-opening process did not exit." }
}

if ([Environment]::OSVersion.Platform -ne [PlatformID]::Win32NT) { throw "Curtain-opening runtime qualification requires Windows." }
if (-not [Environment]::UserInteractive) { throw "Curtain-opening runtime qualification requires an interactive Windows session." }
if (-not $ConfirmDisposableCopy) { throw "Pass -ConfirmDisposableCopy only for a disposable synthetic drawing copy." }
if ([string]::IsNullOrWhiteSpace($Profile)) { throw "Curtain-opening runtime qualification requires an initialized BricsCAD profile." }

$repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot ".."))
$BricsCadDir = [IO.Path]::GetFullPath($BricsCadDir)
$PluginDll = [IO.Path]::GetFullPath($PluginDll)
$DrawingCopy = [IO.Path]::GetFullPath($DrawingCopy)
$ArtifactDir = [IO.Path]::GetFullPath($ArtifactDir)
if (-not [IO.Path]::GetFileName($DrawingCopy).EndsWith(".curtain-opening-probe-copy.dwg", [StringComparison]::OrdinalIgnoreCase)) {
    throw "DrawingCopy must use the guarded '*.curtain-opening-probe-copy.dwg' suffix."
}

$bricscadExe = Join-Path $BricsCadDir "bricscad.exe"
$coreDll = Join-Path (Split-Path -Parent $PluginDll) "QS3D.Core.dll"
foreach ($required in @($bricscadExe, $PluginDll, $coreDll, $DrawingCopy)) {
    if (-not (Test-Path -LiteralPath $required -PathType Leaf)) { throw "Required Curtain-opening runtime input is missing: $required" }
}
$expectedPlugin = [IO.Path]::GetFullPath((Join-Path $repoRoot "src\QS3D.BricsCAD.V25\bin\x64\Release\net48\QS3D.BricsCAD.V25.dll"))
if (-not [string]::Equals($PluginDll, $expectedPlugin, [StringComparison]::OrdinalIgnoreCase)) {
    throw "PluginDll must be the exact repository x64 Release V25 build output."
}
if ($ArtifactDir.StartsWith($repoRoot + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
    throw "ArtifactDir must stay outside the repository because the runtime script contains a private local plugin path."
}

$git = Get-Command git -CommandType Application -ErrorAction Stop | Select-Object -First 1
if ($null -eq $git -or [string]::IsNullOrWhiteSpace($git.Source)) { throw "Git executable is unavailable." }
$gitHead = (& $git.Source -C $repoRoot rev-parse HEAD).Trim()
if ($LASTEXITCODE -ne 0 -or $gitHead -notmatch '^[0-9a-f]{40}$') { throw "Cannot resolve the exact Git candidate SHA." }
$gitStatus = @(& $git.Source -C $repoRoot status --porcelain --untracked-files=normal)
if ($LASTEXITCODE -ne 0) { throw "Cannot inspect the Git candidate worktree." }
if ($gitStatus.Count -ne 0) { throw "Curtain-opening runtime qualification requires a clean exact-SHA worktree." }
if (@(Get-Process -Name "bricscad" -ErrorAction SilentlyContinue).Count -gt 0) {
    throw "Close existing BricsCAD processes before starting the isolated Curtain-opening runtime probe."
}

$projectSidecar = [IO.Path]::ChangeExtension($DrawingCopy, ".qsdb")
if ((Test-Path -LiteralPath $projectSidecar) -or (Test-Path -LiteralPath ($projectSidecar + ".bak"))) {
    throw "The disposable Curtain-opening drawing copy must not have a pre-existing QS3D sidecar."
}

if (Test-Path -LiteralPath $ArtifactDir) {
    if (@(Get-ChildItem -LiteralPath $ArtifactDir -Force).Count -ne 0) { throw "ArtifactDir must be empty." }
}
else { New-Item -ItemType Directory -Path $ArtifactDir | Out-Null }
$resultPath = Join-Path $ArtifactDir "curtain-panel-opening-runtime-result.txt"
$scriptPath = Join-Path $ArtifactDir "curtain-panel-opening-runtime.scr"
$metadataPath = Join-Path $ArtifactDir "curtain-panel-opening-runtime-metadata.json"
foreach ($output in @($resultPath, $scriptPath, $metadataPath)) {
    if (Test-Path -LiteralPath $output) { throw "Curtain-opening runtime output must not already exist: $output" }
}

$drawingHashBefore = (Get-FileHash -LiteralPath $DrawingCopy -Algorithm SHA256).Hash.ToUpperInvariant()
$pluginHash = (Get-FileHash -LiteralPath $PluginDll -Algorithm SHA256).Hash.ToUpperInvariant()
$nonce = [Guid]::NewGuid().ToString("N")
$oldResult = [Environment]::GetEnvironmentVariable("QS3D_CURTAIN_PANEL_OPENING_RESULT", "Process")
$oldNonce = [Environment]::GetEnvironmentVariable("QS3D_CURTAIN_PANEL_OPENING_NONCE", "Process")
$process = $null
$proxyInformationDialogsDismissed = 0
$startedAt = Get-Date

try {
    $env:QS3D_CURTAIN_PANEL_OPENING_RESULT = $resultPath
    $env:QS3D_CURTAIN_PANEL_OPENING_NONCE = $nonce
    $script = @(
        "FILEDIA", "0",
        "CMDECHO", "1",
        "TILEMODE", "1",
        "INSUNITS", "4",
        "UCS", "W",
        "NETLOAD", ('"' + $PluginDll + '"'),
        "QS3DDRAWGLASSWALL", "0,0", "5000,0", "",
        "QS3DDRAWGLASSWALL", "0,10000", "5000,10000", "",
        "QS3DDRAWDOOR", "800,0", "2200,0",
        "QS3DDRAWOPENINGADV", "50,10000", "4950,10000", "3.5", "0.05", "0.01",
        "QS3DCURTAINOPENINGPREPARE",
        "QS3DCURTAIN3D",
        "QS3DCURTAINOPENINGPROBE"
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
        if ($process.HasExited) { throw "BricsCAD exited before the Curtain-opening marker. ExitCode=$($process.ExitCode)" }
        Start-Sleep -Milliseconds 500
    }
    if (-not (Test-Path -LiteralPath $resultPath -PathType Leaf)) {
        throw "Timed out waiting for synthetic Direct Draw + QS3DCURTAIN3D + Curtain-opening probe after $StartupTimeoutSeconds seconds."
    }

    $marker = Read-Qs3dMarker -Path $resultPath
    Require-Qs3dValue -Marker $marker -Key "status" -Expected "PASS"
    Require-Qs3dValue -Marker $marker -Key "command" -Expected "QS3DCURTAINOPENINGPROBE"
    Require-Qs3dValue -Marker $marker -Key "process" -Expected "bricscad"
    Require-Qs3dValue -Marker $marker -Key "nonce" -Expected $nonce
    Require-Qs3dValue -Marker $marker -Key "schema" -Expected "QS3D_CURTAIN_PANEL_OPENING_RUNTIME_V1"
    Require-Qs3dValue -Marker $marker -Key "qualification_boundary" -Expected "LOCAL_002_P02_ONLY"
    Require-Qs3dValue -Marker $marker -Key "production_local002_qualified" -Expected "false"
    Require-Qs3dValue -Marker $marker -Key "is_64bit" -Expected "true"
    Require-Qs3dValue -Marker $marker -Key "legacy_no_level" -Expected "true"
    Require-Qs3dValue -Marker $marker -Key "complete_empty_build_state" -Expected "true"
    Require-Qs3dValue -Marker $marker -Key "opening_aware_metadata" -Expected "true"
    Require-Qs3dValue -Marker $marker -Key "source_geometry_preserved" -Expected "true"
    Require-Qs3dValue -Marker $marker -Key "ownership_sets_disjoint" -Expected "true"
    Require-Qs3dValue -Marker $marker -Key "health_issue_count" -Expected "0"
    Require-Qs3dValue -Marker $marker -Key "located_panel_count" -Expected "1"
    Require-Qs3dValue -Marker $marker -Key "canonical_owner_count" -Expected "1"
    Require-Qs3dValue -Marker $marker -Key "partial_native_opening_intersection_count" -Expected "0"
    Require-Qs3dValue -Marker $marker -Key "complete_empty_output_piece_count" -Expected "0"
    Require-Qs3dValue -Marker $marker -Key "complete_empty_handle_count" -Expected "0"

    $partialSourceCount = Read-PositiveMarkerInt -Marker $marker -Key "partial_source_panel_count"
    $partialOutputCount = Read-PositiveMarkerInt -Marker $marker -Key "partial_output_piece_count"
    $partialFullyRemovedCount = Read-PositiveMarkerInt -Marker $marker -Key "partial_fully_removed_panel_count"
    $partialClippedCount = Read-PositiveMarkerInt -Marker $marker -Key "partial_clipped_panel_count"
    $partialNativeMatchCount = Read-PositiveMarkerInt -Marker $marker -Key "partial_native_plan_match_count"
    $emptySourceCount = Read-PositiveMarkerInt -Marker $marker -Key "complete_empty_source_panel_count"
    $emptyFullyRemovedCount = Read-PositiveMarkerInt -Marker $marker -Key "complete_empty_fully_removed_panel_count"
    if ($partialNativeMatchCount -ne $partialOutputCount) { throw "Partial native/plan piece counts differ." }
    if ($partialFullyRemovedCount -ge $partialSourceCount) { throw "Partial case unexpectedly removed every source panel." }
    if ($partialClippedCount -gt $partialSourceCount) { throw "Partial clipped count exceeds source panels." }
    if ($emptyFullyRemovedCount -ne $emptySourceCount) { throw "Complete-empty case did not remove every source panel." }

    Stop-Qs3dLaunchedProcess -Process $process
    if (Test-Path -LiteralPath $scriptPath) {
        Remove-Item -LiteralPath $scriptPath -Force -ErrorAction Stop
    }
    if (Test-Path -LiteralPath $scriptPath) { throw "Curtain-opening runtime script cleanup failed." }
    $drawingHashAfter = (Get-FileHash -LiteralPath $DrawingCopy -Algorithm SHA256).Hash.ToUpperInvariant()
    if (-not [string]::Equals($drawingHashBefore, $drawingHashAfter, [StringComparison]::Ordinal)) {
        throw "The disposable Curtain-opening drawing was written unexpectedly."
    }
    if ((Test-Path -LiteralPath $projectSidecar) -or (Test-Path -LiteralPath ($projectSidecar + ".bak"))) {
        throw "Curtain-opening runtime probe unexpectedly persisted a QS3D sidecar."
    }

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
        sidecar_absent_verified = $true
        proxy_information_dialogs_dismissed = $proxyInformationDialogsDismissed
        marker = $marker
    }
    $metadata | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $metadataPath -Encoding UTF8

    Write-Host "QS3D BricsCAD V25 Curtain-panel P02 opening-clipping runtime PASS"
    Write-Host "Partial source/output: $partialSourceCount/$partialOutputCount; complete-empty source: $emptySourceCount"
    Write-Host "Marker: $resultPath"
    Write-Host "Metadata: $metadataPath"
}
finally {
    try {
        Stop-Qs3dLaunchedProcess -Process $process
        if (Test-Path -LiteralPath $scriptPath) {
            Remove-Item -LiteralPath $scriptPath -Force -ErrorAction Stop
        }
        if (Test-Path -LiteralPath $scriptPath) { throw "Curtain-opening runtime script cleanup failed." }
    }
    finally {
        Restore-EnvironmentValue -Name "QS3D_CURTAIN_PANEL_OPENING_RESULT" -Value $oldResult
        Restore-EnvironmentValue -Name "QS3D_CURTAIN_PANEL_OPENING_NONCE" -Value $oldNonce
    }
}
