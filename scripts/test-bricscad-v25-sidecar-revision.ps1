[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$BricsCadDir,
    [Parameter(Mandatory = $true)][string]$PluginDll,
    [Parameter(Mandatory = $true)][string]$FixtureDwg,
    [Parameter(Mandatory = $true)][string]$Profile,
    [Parameter(Mandatory = $true)][string]$ArtifactDir,
    [Parameter(Mandatory = $true)][switch]$ConfirmSyntheticFixture,
    [ValidateRange(30, 600)][int]$StartupTimeoutSeconds = 240
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

function Restore-EnvironmentValue {
    param([string]$Name, [AllowNull()][string]$Value)
    if ($null -eq $Value) { Remove-Item -LiteralPath ("Env:" + $Name) -ErrorAction SilentlyContinue }
    else { Set-Item -LiteralPath ("Env:" + $Name) -Value $Value }
}

function Stop-LaunchedProcess {
    param([AllowNull()][Diagnostics.Process]$Process)
    if ($null -eq $Process) { return }
    $Process.Refresh()
    if (-not $Process.HasExited) {
        Stop-Process -Id $Process.Id -Force -ErrorAction Stop
        if (-not $Process.WaitForExit(10000)) { throw "Launched BricsCAD process did not exit during probe cleanup." }
    }
    $Process.WaitForExit()
    $Process.Refresh()
    if (-not $Process.HasExited) { throw "Launched BricsCAD process remains active after probe cleanup." }
}

function Remove-PrivateProbeFile {
    param([string]$Path)
    if ([string]::IsNullOrWhiteSpace($Path)) { throw "Private sidecar revision cleanup path is missing." }
    if (Test-Path -LiteralPath $Path -PathType Container) { throw "Private sidecar revision cleanup refuses directory targets." }
    if (Test-Path -LiteralPath $Path) { Remove-Item -LiteralPath $Path -Force -ErrorAction Stop }
    if (Test-Path -LiteralPath $Path) { throw "Private sidecar revision probe artifact was not removed." }
}

function Remove-PrivateProbeArtifacts {
    param([string]$ArtifactDir, [string]$ScriptPath, [string]$DrawingCopy, [string]$Nonce)
    $sidecarPath = [IO.Path]::ChangeExtension($DrawingCopy, ".qsdb")
    $drawingLockPath = [IO.Path]::ChangeExtension($DrawingCopy, ".dwl")
    $drawingLock2Path = [IO.Path]::ChangeExtension($DrawingCopy, ".dwl2")
    $privatePaths = @(
        $ScriptPath
        $sidecarPath
        ($sidecarPath + ".bak")
        ($sidecarPath + ".lock")
        ($sidecarPath + "." + $Nonce + ".original")
        ($sidecarPath + "." + $Nonce + ".replacement")
        ($sidecarPath + "." + $Nonce + ".removed")
        $drawingLockPath
        $drawingLock2Path
    )
    if ($privatePaths.Count -ne 9) { throw "Private sidecar revision cleanup path inventory is invalid." }
    foreach ($privatePath in $privatePaths) {
        Remove-PrivateProbeFile -Path $privatePath
    }

    $scratchPrefix = "sr-" + $Nonce.Substring(0, 8) + "-"
    foreach ($item in @(Get-ChildItem -LiteralPath $ArtifactDir -File -Force -ErrorAction Stop)) {
        if (-not $item.Name.StartsWith($scratchPrefix, [StringComparison]::Ordinal)) { continue }
        if (-not ($item.Name.EndsWith(".qsdb", [StringComparison]::OrdinalIgnoreCase) -or
                  $item.Name.EndsWith(".qsdb.bak", [StringComparison]::OrdinalIgnoreCase) -or
                  $item.Name.EndsWith(".qsdb.lock", [StringComparison]::OrdinalIgnoreCase))) { continue }
        Remove-PrivateProbeFile -Path $item.FullName
    }
}

function Read-Marker {
    param([string]$Path)
    $result = @{}
    foreach ($line in Get-Content -LiteralPath $Path) {
        if ([string]::IsNullOrWhiteSpace($line)) { continue }
        $separator = $line.IndexOf('=')
        if ($separator -le 0) { throw "Malformed sidecar revision marker." }
        $key = $line.Substring(0, $separator).Trim()
        $value = $line.Substring($separator + 1).Trim()
        if ($result.ContainsKey($key)) { throw "Duplicate sidecar revision marker key." }
        $result[$key] = $value
    }
    return $result
}

function Require-Value {
    param($Marker, [string]$Key, [string]$Expected)
    if (-not $Marker.ContainsKey($Key) -or
        -not [string]::Equals([string]$Marker[$Key], $Expected, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Sidecar revision marker '$Key' did not match the expected value."
    }
}

if ([Environment]::OSVersion.Platform -ne [PlatformID]::Win32NT -or -not [Environment]::UserInteractive) {
    throw "The sidecar revision probe requires an interactive Windows session."
}
if (-not $ConfirmSyntheticFixture) { throw "Confirm the repository-generated synthetic fixture explicitly." }

$repoRoot = [IO.Path]::GetFullPath((Split-Path -Parent $PSScriptRoot))
$artifactsRoot = [IO.Path]::GetFullPath((Join-Path $repoRoot "artifacts"))
$BricsCadDir = [IO.Path]::GetFullPath($BricsCadDir)
$PluginDll = [IO.Path]::GetFullPath($PluginDll)
$FixtureDwg = [IO.Path]::GetFullPath($FixtureDwg)
$ArtifactDir = [IO.Path]::GetFullPath($ArtifactDir)
$artifactPrefix = $artifactsRoot.TrimEnd([IO.Path]::DirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar
if (-not $ArtifactDir.StartsWith($artifactPrefix, [StringComparison]::OrdinalIgnoreCase)) {
    throw "ArtifactDir must stay inside the repository artifacts directory."
}
if (-not [string]::Equals([IO.Path]::GetFileName($FixtureDwg), "QS3D-Sample.dwg", [StringComparison]::OrdinalIgnoreCase) -or
    -not [string]::Equals([IO.Path]::GetFileName((Split-Path -Parent $FixtureDwg)), "generated", [StringComparison]::OrdinalIgnoreCase)) {
    throw "FixtureDwg must be the repository-generated QS3D-Sample.dwg."
}
if (Test-Path -LiteralPath $ArtifactDir) {
    if (@(Get-ChildItem -LiteralPath $ArtifactDir -Force).Count -gt 0) { throw "ArtifactDir must be new or empty." }
}
else { New-Item -ItemType Directory -Path $ArtifactDir | Out-Null }

$bricscadExe = Join-Path $BricsCadDir "bricscad.exe"
$coreDll = Join-Path (Split-Path -Parent $PluginDll) "QS3D.Core.dll"
foreach ($required in @($bricscadExe, $PluginDll, $coreDll, $FixtureDwg)) {
    if (-not (Test-Path -LiteralPath $required -PathType Leaf)) { throw "Required sidecar revision input is missing." }
}
if (@(Get-Process -Name "bricscad" -ErrorAction SilentlyContinue).Count -gt 0) {
    throw "Close existing BricsCAD processes before the isolated sidecar revision probe."
}

$gitStatus = @(& git -C $repoRoot status --porcelain)
if ($LASTEXITCODE -ne 0 -or $gitStatus.Count -ne 0) { throw "The sidecar revision probe requires a clean exact Git SHA." }
$exactSha = (& git -C $repoRoot rev-parse HEAD).Trim()
if ($LASTEXITCODE -ne 0 -or $exactSha -notmatch '^[0-9a-f]{40}$') { throw "Unable to resolve the exact Git SHA." }

$copyDir = Join-Path $ArtifactDir "fixture-copies"
New-Item -ItemType Directory -Path $copyDir | Out-Null
$drawingCopy = Join-Path $copyDir "sidecar-revision.reference-copy.dwg"
Copy-Item -LiteralPath $FixtureDwg -Destination $drawingCopy
$drawingHashBefore = (Get-FileHash -LiteralPath $drawingCopy -Algorithm SHA256).Hash.ToUpperInvariant()
$fixtureHashBefore = (Get-FileHash -LiteralPath $FixtureDwg -Algorithm SHA256).Hash.ToUpperInvariant()
$pluginHash = (Get-FileHash -LiteralPath $PluginDll -Algorithm SHA256).Hash.ToUpperInvariant()
$resultPath = Join-Path $ArtifactDir "sidecar-revision-result.txt"
$scriptPath = Join-Path $ArtifactDir "sidecar-revision.scr"
$metadataPath = Join-Path $ArtifactDir "sidecar-revision-metadata.json"
$nonce = [Guid]::NewGuid().ToString("N")
$environmentNames = @("QS3D_SIDECAR_REVISION_RESULT", "QS3D_SIDECAR_REVISION_NONCE", "QS3D_SIDECAR_REVISION_DWG")
$oldEnvironment = @{}
foreach ($name in $environmentNames) { $oldEnvironment[$name] = [Environment]::GetEnvironmentVariable($name, "Process") }
$process = $null
$startedAt = [DateTime]::UtcNow

try {
    $env:QS3D_SIDECAR_REVISION_RESULT = $resultPath
    $env:QS3D_SIDECAR_REVISION_NONCE = $nonce
    $env:QS3D_SIDECAR_REVISION_DWG = $drawingCopy
    Set-Content -LiteralPath $scriptPath -Encoding ASCII -Value @(
        "FILEDIA", "0", "CMDECHO", "1", "NETLOAD", ('"' + $PluginDll + '"'),
        "QS3DSIDECARREVISIONPROBE"
    )

    $arguments = '"' + $drawingCopy + '" /P "' + $Profile + '" /B "' + $scriptPath + '"'
    $process = Start-Process -FilePath $bricscadExe -ArgumentList $arguments -PassThru -WindowStyle Hidden -WorkingDirectory $ArtifactDir
    $deadline = (Get-Date).AddSeconds($StartupTimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        if (Test-Path -LiteralPath $resultPath -PathType Leaf) { break }
        $process.Refresh()
        if ($process.HasExited) { throw "BricsCAD exited before producing the sidecar revision marker." }
        Start-Sleep -Milliseconds 500
    }
    if (-not (Test-Path -LiteralPath $resultPath -PathType Leaf)) { throw "Timed out waiting for the sidecar revision marker." }

    $marker = Read-Marker -Path $resultPath
    if ($marker.ContainsKey("status") -and
        [string]::Equals([string]$marker["status"], "FAIL", [StringComparison]::OrdinalIgnoreCase)) {
        $allowedFailureStages = @(
            "scope_validation", "baseline_bind", "baseline_save", "baseline_snapshot",
            "baseline_snapshot_detach", "baseline_snapshot_save", "baseline_snapshot_load",
            "baseline_snapshot_normalize", "baseline_snapshot_digest",
            "backup_appearance_prepare", "backup_appearance_read_only", "backup_appearance_canonical_bind",
            "backup_appearance_existing_mutation", "backup_appearance_interchange_confirmation",
            "backup_appearance_save", "backup_appearance_semantic_integrity", "backup_appearance_restore",
            "backup_appearance_recovery", "primary_replacement_prepare", "primary_replacement_read_only",
            "primary_replacement_canonical_bind", "primary_replacement_existing_mutation",
            "primary_replacement_interchange_confirmation", "primary_replacement_save",
            "primary_replacement_semantic_integrity", "primary_replacement_restore",
            "primary_replacement_recovery", "primary_removal_prepare", "primary_removal_read_only",
            "primary_removal_canonical_bind", "primary_removal_existing_mutation",
            "primary_removal_interchange_confirmation", "primary_removal_save",
            "primary_removal_semantic_integrity", "primary_removal_restore",
            "primary_removal_recovery", "final_recovery", "marker_write", "unknown"
        )
        $failureStage = if ($marker.ContainsKey("stage")) { [string]$marker["stage"] } else { "unknown" }
        if ($allowedFailureStages -notcontains $failureStage) { $failureStage = "unknown" }
        $allowedFailureKinds = @("invalid_data", "unauthorized", "io", "xml", "argument", "invalid_operation", "other")
        $failureKind = if ($marker.ContainsKey("failure_kind")) { [string]$marker["failure_kind"] } else { "other" }
        if ($allowedFailureKinds -notcontains $failureKind) { $failureKind = "other" }
        throw "BricsCAD sidecar revision probe returned sanitized failure stage '$failureStage' and kind '$failureKind'."
    }
    foreach ($key in @(
        "backup_appearance_refused", "primary_replacement_refused", "primary_removal_refused",
        "read_only_boundary_refused", "canonical_bind_refused", "existing_mutation_refused",
        "interchange_confirmation_refused", "sidecar_overwrite_refused", "project_state_unchanged",
        "restored_session_recovered", "dwg_write_not_requested"
    )) { Require-Value -Marker $marker -Key $key -Expected "true" }
    Require-Value -Marker $marker -Key "status" -Expected "PASS"
    Require-Value -Marker $marker -Key "nonce" -Expected $nonce

    Stop-LaunchedProcess -Process $process
    $process = $null
    if (@(Get-Process -Name "bricscad" -ErrorAction SilentlyContinue).Count -gt 0) {
        throw "BricsCAD process cleanup could not be verified."
    }

    $drawingHashAfter = (Get-FileHash -LiteralPath $drawingCopy -Algorithm SHA256).Hash.ToUpperInvariant()
    $fixtureHashAfter = (Get-FileHash -LiteralPath $FixtureDwg -Algorithm SHA256).Hash.ToUpperInvariant()
    if (-not [string]::Equals($drawingHashBefore, $drawingHashAfter, [StringComparison]::Ordinal) -or
        -not [string]::Equals($fixtureHashBefore, $fixtureHashAfter, [StringComparison]::Ordinal)) {
        throw "The disposable/reference DWG changed during the sidecar revision probe."
    }

    Remove-PrivateProbeArtifacts -ArtifactDir $ArtifactDir -ScriptPath $scriptPath -DrawingCopy $drawingCopy -Nonce $nonce

    [ordered]@{
        schema = 1
        status = "PASS"
        exactSha = $exactSha
        bricscadVersion = [Diagnostics.FileVersionInfo]::GetVersionInfo($bricscadExe).FileVersion
        pluginSha256 = $pluginHash
        fixtureSha256Before = $fixtureHashBefore
        fixtureSha256After = $fixtureHashAfter
        drawingCopySha256Before = $drawingHashBefore
        drawingCopySha256After = $drawingHashAfter
        warmCacheRevisionMatrix = $true
        cleanupVerified = $true
        startedUtc = $startedAt.ToString("O")
        completedUtc = [DateTime]::UtcNow.ToString("O")
    } | ConvertTo-Json -Depth 3 | Set-Content -LiteralPath $metadataPath -Encoding UTF8
    Write-Host "QS3D BricsCAD V25 sidecar revision probe PASS"
    Write-Host "Result: $resultPath"
}
finally {
    try {
        Stop-LaunchedProcess -Process $process
        Remove-PrivateProbeArtifacts -ArtifactDir $ArtifactDir -ScriptPath $scriptPath -DrawingCopy $drawingCopy -Nonce $nonce
    }
    finally {
        foreach ($name in $environmentNames) { Restore-EnvironmentValue -Name $name -Value $oldEnvironment[$name] }
    }
}
