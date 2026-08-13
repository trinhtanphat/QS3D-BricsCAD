param(
    [Parameter(Mandatory = $true)][string]$BricsCadDir,
    [Parameter(Mandatory = $true)][string]$PluginDll,
    [Parameter(Mandatory = $true)][string]$FixtureDwg,
    [Parameter(Mandatory = $true)][string]$Profile,
    [Parameter(Mandatory = $true)][string]$ArtifactDir,
    [Parameter(Mandatory = $true)][switch]$ConfirmDisposableCopies,
    [ValidateRange(60, 1200)][int]$StartupTimeoutSeconds = 360
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$windowInteropPath = Join-Path $PSScriptRoot "bricscad-runner-window-interop.ps1"
if (-not (Test-Path -LiteralPath $windowInteropPath -PathType Leaf)) {
    throw "Curtain P12 runner window interop helper is missing."
}
. $windowInteropPath

function Read-Qs3dMarker {
    param([Parameter(Mandatory = $true)][string]$Path)
    $marker = @{}
    foreach ($line in Get-Content -LiteralPath $Path) {
        if ([string]::IsNullOrWhiteSpace($line)) { continue }
        $separator = $line.IndexOf('=')
        if ($separator -le 0) { throw "Malformed Curtain P12 marker line." }
        $key = $line.Substring(0, $separator).Trim()
        $value = $line.Substring($separator + 1).Trim()
        if ($marker.ContainsKey($key)) { throw "Duplicate Curtain P12 marker key: $key" }
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
    if (-not $Marker.ContainsKey($Key)) { throw "Curtain P12 marker is missing '$Key'." }
    if (-not [string]::Equals([string]$Marker[$Key], $Expected, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Curtain P12 marker '$Key' did not match its expected value."
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
    if (-not $Process.HasExited) { throw "Launched Curtain P12 BricsCAD process did not exit." }
}

function Remove-ExactFile {
    param([Parameter(Mandatory = $true)][string]$Path)
    if (Test-Path -LiteralPath $Path) { Remove-Item -LiteralPath $Path -Force -ErrorAction Stop }
    if (Test-Path -LiteralPath $Path) { throw "Curtain P12 exact private-file cleanup failed." }
}

if ([Environment]::OSVersion.Platform -ne [PlatformID]::Win32NT) { throw "Curtain P12 qualification requires Windows." }
if (-not [Environment]::UserInteractive) { throw "Curtain P12 qualification requires an interactive Windows session." }
if (-not $ConfirmDisposableCopies) { throw "Pass -ConfirmDisposableCopies only for repository-sample disposable copies." }
if ([string]::IsNullOrWhiteSpace($Profile)) { throw "Curtain P12 qualification requires an initialized BricsCAD profile." }

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
    if (-not (Test-Path -LiteralPath $required -PathType Leaf)) { throw "Required Curtain P12 input is missing." }
}

$git = Get-Command git -CommandType Application -ErrorAction Stop | Select-Object -First 1
$gitOutput = @(& $git.Source -C $repoRoot rev-parse HEAD 2>$null)
$gitExitCode = $LASTEXITCODE
if ($gitExitCode -ne 0 -or $gitOutput.Count -ne 1) { throw "Cannot resolve the exact Curtain P12 Git candidate SHA." }
$gitHead = ([string]$gitOutput[0]).Trim().ToLowerInvariant()
if ($gitHead -notmatch '^[0-9a-f]{40}$') { throw "Curtain P12 Git candidate SHA is invalid." }
$gitStatus = @(& $git.Source -C $repoRoot status --porcelain=v1 --untracked-files=all 2>$null)
$gitStatusExitCode = $LASTEXITCODE
if ($gitStatusExitCode -ne 0) { throw "Cannot inspect the Curtain P12 candidate worktree." }
if ($gitStatus.Count -ne 0) { throw "Curtain P12 qualification requires a clean exact-SHA worktree." }
$expectedAssemblyRevision = "+" + $gitHead
foreach ($assemblyPath in @($PluginDll, $coreDll)) {
    $productVersion = [string](Get-Item -LiteralPath $assemblyPath).VersionInfo.ProductVersion
    if (-not $productVersion.EndsWith($expectedAssemblyRevision, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Curtain P12 assembly was not built from the exact Git candidate SHA."
    }
}
if (@(Get-Process -Name "bricscad" -ErrorAction SilentlyContinue).Count -gt 0) {
    throw "Close existing BricsCAD processes before isolated Curtain P12 qualification."
}

if (Test-Path -LiteralPath $ArtifactDir) {
    if (@(Get-ChildItem -LiteralPath $ArtifactDir -Force).Count -ne 0) { throw "ArtifactDir must be empty." }
}
else { New-Item -ItemType Directory -Path $ArtifactDir | Out-Null }
$fixtureRoot = Join-Path $ArtifactDir "fixture-copies"
New-Item -ItemType Directory -Path $fixtureRoot | Out-Null
$drawingA = Join-Path $fixtureRoot "curtain-a.curtain-multidwg-probe-copy.dwg"
$drawingB = Join-Path $fixtureRoot "curtain-b.curtain-multidwg-probe-copy.dwg"
Copy-Item -LiteralPath $FixtureDwg -Destination $drawingA -ErrorAction Stop
Copy-Item -LiteralPath $FixtureDwg -Destination $drawingB -ErrorAction Stop

$fixtureHash = (Get-FileHash -LiteralPath $FixtureDwg -Algorithm SHA256).Hash.ToUpperInvariant()
foreach ($drawing in @($drawingA, $drawingB)) {
    if (-not [string]::Equals((Get-FileHash -LiteralPath $drawing -Algorithm SHA256).Hash, $fixtureHash, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Curtain P12 disposable copy hash mismatch."
    }
}

$privateFiles = New-Object System.Collections.Generic.List[string]
foreach ($drawing in @($drawingA, $drawingB)) {
    $sidecar = [IO.Path]::ChangeExtension($drawing, ".qsdb")
    foreach ($path in @($sidecar, $sidecar + ".bak", $sidecar + ".lock", [IO.Path]::ChangeExtension($drawing, ".dwl"), [IO.Path]::ChangeExtension($drawing, ".dwl2"), [IO.Path]::ChangeExtension($drawing, ".bak"))) {
        if (Test-Path -LiteralPath $path) { throw "Curtain P12 disposable copy has pre-existing private state." }
        $privateFiles.Add($path)
    }
}

$resultPath = Join-Path $ArtifactDir "curtain-panel-multidwg-result.txt"
$scriptPath = Join-Path $ArtifactDir "curtain-panel-multidwg.private.scr"
$metadataPath = Join-Path $ArtifactDir "curtain-panel-multidwg-metadata.json"
$pluginHash = (Get-FileHash -LiteralPath $PluginDll -Algorithm SHA256).Hash.ToUpperInvariant()
$nonce = [Guid]::NewGuid().ToString("N")
$environmentNames = @("QS3D_CURTAIN_P12_RESULT", "QS3D_CURTAIN_P12_NONCE", "QS3D_CURTAIN_P12_DWG_A", "QS3D_CURTAIN_P12_DWG_B")
$oldEnvironment = @{}
foreach ($name in $environmentNames) { $oldEnvironment[$name] = [Environment]::GetEnvironmentVariable($name, "Process") }

$process = $null
$marker = $null
$qualificationError = $null
$cleanupError = $null
$startedAt = Get-Date
$proxyDialogsDismissed = 0
$drawingAAfterRun = ""
$drawingBAfterRun = ""
$processCleanupVerified = $false

try {
    $env:QS3D_CURTAIN_P12_RESULT = $resultPath
    $env:QS3D_CURTAIN_P12_NONCE = $nonce
    $env:QS3D_CURTAIN_P12_DWG_A = $drawingA
    $env:QS3D_CURTAIN_P12_DWG_B = $drawingB

    $script = @(
        "FILEDIA", "0", "CMDECHO", "1", "TILEMODE", "1", "INSUNITS", "4", "UCS", "W",
        "NETLOAD", ('"' + $PluginDll + '"'),
        "QS3DCURTAINP12SEEDA", "QS3DCURTAIN", "QS3DCURTAINP12CAPTURE",
        "_.OPEN", ('"' + $drawingB + '"'),
        "QS3DCURTAINP12SEEDB", "QS3DCURTAINP12CHECKB",
        "QS3DCURTAINP12ACTIVATEA", "QS3DCURTAINP12CHECKA",
        "QS3DCURTAINP12ACTIVATEB", "QS3DCURTAINP12CLOSEA", "QS3DCURTAINP12FINAL",
        "_.QUIT", "_Y"
    )
    [IO.File]::WriteAllLines($scriptPath, $script, [Text.Encoding]::ASCII)
    $arguments = '"' + $drawingA + '" /P "' + $Profile + '" /B "' + $scriptPath + '"'
    $process = Start-Process -FilePath $bricscadExe -ArgumentList $arguments -PassThru -WindowStyle Hidden -WorkingDirectory $ArtifactDir
    $deadline = (Get-Date).AddSeconds($StartupTimeoutSeconds)
    while ((Get-Date) -lt $deadline -and -not (Test-Path -LiteralPath $resultPath -PathType Leaf)) {
        $proxyDialogsDismissed += Close-Qs3dProxyInformationDialog -Process $process
        $process.Refresh()
        if ($process.HasExited) { throw "BricsCAD exited before the Curtain P12 marker." }
        Start-Sleep -Milliseconds 500
    }
    if (-not (Test-Path -LiteralPath $resultPath -PathType Leaf)) { throw "Timed out waiting for the Curtain P12 marker." }
    $marker = Read-Qs3dMarker -Path $resultPath
    Require-Qs3dValue -Marker $marker -Key "command" -Expected "QS3DCURTAINP12FINAL"
    Require-Qs3dValue -Marker $marker -Key "nonce" -Expected $nonce
    Require-Qs3dValue -Marker $marker -Key "schema" -Expected "QS3D_CURTAIN_PANEL_MULTIDWG_RUNTIME_V1"
    Require-Qs3dValue -Marker $marker -Key "production_local002_qualified" -Expected "false"

    if (-not $process.WaitForExit(30000)) { throw "Curtain P12 BricsCAD process did not exit after publishing its marker." }
    $process.Refresh()
    if (-not $process.HasExited) { throw "Curtain P12 launched process remained live." }
    $drawingAAfterRun = (Get-FileHash -LiteralPath $drawingA -Algorithm SHA256).Hash.ToUpperInvariant()
    $drawingBAfterRun = (Get-FileHash -LiteralPath $drawingB -Algorithm SHA256).Hash.ToUpperInvariant()
    if (-not [string]::Equals($drawingAAfterRun, $fixtureHash, [StringComparison]::Ordinal) -or
        -not [string]::Equals($drawingBAfterRun, $fixtureHash, [StringComparison]::Ordinal)) {
        throw "Curtain P12 disposable drawing bytes changed."
    }

    if ([string]::Equals([string]$marker["status"], "PASS", [StringComparison]::OrdinalIgnoreCase)) {
        foreach ($key in @(
            "p12_qualified", "two_documents_observed", "curtain_window_bound_to_a", "b_refresh_refused",
            "b_command_refused", "projects_unchanged_while_b_active", "reactivated_a_refresh_succeeded",
            "a_close_closed_bound_window", "window_closed_event_observed", "b_remained_active",
            "b_project_unchanged_after_a_close"
        )) { Require-Qs3dValue -Marker $marker -Key $key -Expected "true" }
        [int]$documentCount = 0
        if (-not $marker.ContainsKey("document_count_after_close") -or
            -not [int]::TryParse([string]$marker["document_count_after_close"], [Globalization.NumberStyles]::None, [Globalization.CultureInfo]::InvariantCulture, [ref]$documentCount) -or
            $documentCount -lt 1) { throw "Curtain P12 marker has an invalid final document count." }
    }
    else {
        $phase = if ($marker.ContainsKey("failure_phase")) { [string]$marker["failure_phase"] } else { "unknown" }
        $code = if ($marker.ContainsKey("failure_code")) { [string]$marker["failure_code"] } else { "STATE_REJECTED" }
        throw "Curtain P12 licensed result failed: $phase/$code"
    }
}
catch { $qualificationError = $_ }
finally {
    try { Stop-Qs3dLaunchedProcess -Process $process }
    catch { if ($null -eq $cleanupError) { $cleanupError = $_ } }
    if ($null -eq $cleanupError) {
        try { Remove-ExactFile -Path $scriptPath }
        catch { if ($null -eq $cleanupError) { $cleanupError = $_ } }
        foreach ($privatePath in $privateFiles) {
            try { Remove-ExactFile -Path $privatePath }
            catch { if ($null -eq $cleanupError) { $cleanupError = $_ } }
        }
        foreach ($drawing in @($drawingA, $drawingB)) {
            try {
                if (-not [string]::Equals((Get-FileHash -LiteralPath $drawing -Algorithm SHA256).Hash, $fixtureHash, [StringComparison]::OrdinalIgnoreCase)) {
                    Copy-Item -LiteralPath $FixtureDwg -Destination $drawing -Force -ErrorAction Stop
                }
                if (-not [string]::Equals((Get-FileHash -LiteralPath $drawing -Algorithm SHA256).Hash, $fixtureHash, [StringComparison]::OrdinalIgnoreCase)) {
                    throw "Curtain P12 drawing restore failed."
                }
            }
            catch { if ($null -eq $cleanupError) { $cleanupError = $_ } }
        }
        try {
            if (@(Get-Process -Name "bricscad" -ErrorAction SilentlyContinue).Count -gt 0) { throw "Curtain P12 cleanup left a BricsCAD process." }
            $processCleanupVerified = $true
        }
        catch { if ($null -eq $cleanupError) { $cleanupError = $_ } }
    }
    foreach ($name in $environmentNames) { Restore-EnvironmentValue -Name $name -Value $oldEnvironment[$name] }
}

$markerForMetadata = if ($null -ne $marker) { $marker } else { @{} }
$metadata = [ordered]@{
    status = if ($null -eq $qualificationError -and $null -eq $cleanupError) { "PASS" } else { "FAIL" }
    git_sha = $gitHead
    started_at = $startedAt.ToUniversalTime().ToString("O")
    completed_at = (Get-Date).ToUniversalTime().ToString("O")
    bricscad_file_version = (Get-Item -LiteralPath $bricscadExe).VersionInfo.FileVersion
    plugin_sha256 = $pluginHash
    fixture_sha256 = $fixtureHash
    drawing_a_sha256_after_run = $drawingAAfterRun
    drawing_b_sha256_after_run = $drawingBAfterRun
    drawing_a_restore_verified = [string]::Equals((Get-FileHash -LiteralPath $drawingA -Algorithm SHA256).Hash, $fixtureHash, [StringComparison]::OrdinalIgnoreCase)
    drawing_b_restore_verified = [string]::Equals((Get-FileHash -LiteralPath $drawingB -Algorithm SHA256).Hash, $fixtureHash, [StringComparison]::OrdinalIgnoreCase)
    process_cleanup_verified = $processCleanupVerified
    script_cleanup_verified = (-not (Test-Path -LiteralPath $scriptPath))
    private_state_cleanup_verified = (@($privateFiles | Where-Object { Test-Path -LiteralPath $_ }).Count -eq 0)
    proxy_information_dialogs_dismissed = $proxyDialogsDismissed
    marker = $markerForMetadata
}
$metadata | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $metadataPath -Encoding UTF8

if ($null -ne $cleanupError) { throw $cleanupError }
if ($null -ne $qualificationError) { throw $qualificationError }

Write-Host "QS3D BricsCAD V25 Curtain P12 multi-DWG/modeless PASS"
Write-Host "Marker: $resultPath"
Write-Host "Metadata: $metadataPath"
