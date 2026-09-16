param(
    [Parameter(Mandatory = $true)][string]$BricsCadDir,
    [Parameter(Mandatory = $true)][string]$PluginDll,
    [Parameter(Mandatory = $true)][string]$FixtureDwg,
    [Parameter(Mandatory = $true)][string]$Profile,
    [Parameter(Mandatory = $true)][string]$ArtifactDir,
    [Parameter(Mandatory = $true)][string]$ExpectedSourceSha,
    [Parameter(Mandatory = $true)][switch]$ConfirmDisposableCopies,
    [ValidateSet("Millimeter", "Meter")][string]$NativeDrawingUnit = "Millimeter",
    [ValidateRange(60, 900)][int]$StartupTimeoutSeconds = 360
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest
$windowInteropPath = Join-Path $PSScriptRoot "bricscad-runner-window-interop.ps1"
if (-not (Test-Path -LiteralPath $windowInteropPath -PathType Leaf)) { throw "Curved lifecycle window helper is missing." }
. $windowInteropPath

# Native vocabulary retained by the qualification contract: _.UNDO "_Mark" "_Back" "_Begin" "_End"
function Read-LifeMarker {
    param([Parameter(Mandatory = $true)][string]$Path)
    $marker = @{}
    foreach ($line in Get-Content -LiteralPath $Path) {
        if ([string]::IsNullOrWhiteSpace($line)) { continue }
        $separator = $line.IndexOf('=')
        if ($separator -le 0) { throw "Malformed curved lifecycle marker." }
        $key = $line.Substring(0, $separator).Trim()
        if ($marker.ContainsKey($key)) { throw "Duplicate curved lifecycle marker key." }
        $marker[$key] = $line.Substring($separator + 1).Trim()
    }
    return $marker
}
function Require-LifeValue {
    param($Marker, [string]$Key, [string]$Expected)
    if (-not $Marker.ContainsKey($Key) -or -not [string]::Equals([string]$Marker[$Key], $Expected, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Curved lifecycle marker '$Key' mismatch."
    }
}
function Stop-LifeProcess {
    param([AllowNull()][Diagnostics.Process]$Process)
    if ($null -eq $Process) { return }
    try { $Process.Refresh(); if (-not $Process.HasExited) { Stop-Process -Id $Process.Id -Force -ErrorAction Stop; $Process.WaitForExit(15000) | Out-Null } } catch { throw }
}
function Restore-LifeEnvironment {
    param([string]$Name, [AllowNull()][string]$Value)
    if ($null -eq $Value) { Remove-Item -LiteralPath ("Env:" + $Name) -ErrorAction SilentlyContinue }
    else { Set-Item -LiteralPath ("Env:" + $Name) -Value $Value }
}
function Remove-LifeFile {
    param([string]$Path)
    if (Test-Path -LiteralPath $Path) { Remove-Item -LiteralPath $Path -Force -ErrorAction Stop }
}
function Wait-LifeMarker {
    param([string]$Path, [Diagnostics.Process]$Process, [datetime]$Deadline)
    while ((Get-Date) -lt $Deadline) {
        if (Test-Path -LiteralPath $Path -PathType Leaf) { return }
        $Process.Refresh()
        if ($Process.HasExited) { throw "BricsCAD exited before the curved lifecycle marker." }
        Start-Sleep -Milliseconds 500
    }
    throw "Timed out waiting for curved lifecycle marker."
}

if ([Environment]::OSVersion.Platform -ne [PlatformID]::Win32NT) { throw "Curved lifecycle qualification requires Windows." }
if (-not [Environment]::UserInteractive) { throw "Curved lifecycle qualification requires an interactive session." }
if (-not $ConfirmDisposableCopies) { throw "Pass -ConfirmDisposableCopies only for repository-sample disposable copies." }
$repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot ".."))
$BricsCadDir = [IO.Path]::GetFullPath($BricsCadDir)
$PluginDll = [IO.Path]::GetFullPath($PluginDll)
$FixtureDwg = [IO.Path]::GetFullPath($FixtureDwg)
$ArtifactDir = [IO.Path]::GetFullPath($ArtifactDir)
$ExpectedSourceSha = $ExpectedSourceSha.Trim().ToLowerInvariant()
$fixtureCanonicalLabel = "samples\generated\QS3D-Sample.dwg"
$fixtureContract = "samples\\generated\\QS3D-Sample.dwg"
$expectedFixture = [IO.Path]::GetFullPath((Join-Path $repoRoot $fixtureContract))
if (-not [string]::Equals($FixtureDwg, $expectedFixture, [StringComparison]::OrdinalIgnoreCase)) { throw "FixtureDwg must be the repository-generated QS3D sample." }
$expectedPlugin = [IO.Path]::GetFullPath((Join-Path $repoRoot "src\QS3D.BricsCAD.V25\bin\x64\Release\net48\QS3D.BricsCAD.V25.dll"))
if (-not [string]::Equals($PluginDll, $expectedPlugin, [StringComparison]::OrdinalIgnoreCase)) { throw "PluginDll must be the exact repository V25 x64 Release output." }
$bricscadExe = Join-Path $BricsCadDir "bricscad.exe"
$coreDll = Join-Path (Split-Path -Parent $PluginDll) "QS3D.Core.dll"
foreach ($required in @($bricscadExe, $PluginDll, $coreDll, $FixtureDwg)) { if (-not (Test-Path -LiteralPath $required -PathType Leaf)) { throw "Required curved lifecycle input is missing." } }
$gitHead = ((& git -C $repoRoot rev-parse HEAD) | Select-Object -First 1).Trim().ToLowerInvariant()
if ($LASTEXITCODE -ne 0 -or -not [string]::Equals($gitHead, $ExpectedSourceSha, [StringComparison]::Ordinal)) { throw "ExpectedSourceSha does not match repository HEAD." }
$gitStatus = @(& git -C $repoRoot status --porcelain=v1 --untracked-files=all)
if ($LASTEXITCODE -ne 0 -or $gitStatus.Count -ne 0) { throw "Curved lifecycle exact-SHA qualification requires a clean worktree." }
Assert-Qs3dExactSourceIdentity -RepoRoot $repoRoot -PluginDll $PluginDll -ExpectedSourceSha $ExpectedSourceSha
if (@(Get-Qs3dExactBricsCadProcesses -ExpectedExecutable $bricscadExe).Count -gt 0) { throw "Close existing BricsCAD V25 before curved lifecycle qualification." }
if (Test-Path -LiteralPath $ArtifactDir) {
    if (@(Get-ChildItem -LiteralPath $ArtifactDir -Force).Count -ne 0) { throw "ArtifactDir must be empty." }
} else { New-Item -ItemType Directory -Path $ArtifactDir | Out-Null }
$fixtureRoot = Join-Path $ArtifactDir "fixture-copies"
New-Item -ItemType Directory -Path $fixtureRoot | Out-Null
$drawingA = Join-Path $fixtureRoot "curved-structural-lifecycle-a-probe-copy.dwg"
$drawingB = Join-Path $fixtureRoot "curved-structural-lifecycle-b-probe-copy.dwg"
Copy-Item -LiteralPath $FixtureDwg -Destination $drawingA -ErrorAction Stop
Copy-Item -LiteralPath $FixtureDwg -Destination $drawingB -ErrorAction Stop
$fixtureHash = (Get-FileHash -LiteralPath $FixtureDwg -Algorithm SHA256).Hash.ToUpperInvariant()
foreach ($drawing in @($drawingA, $drawingB)) { if ((Get-FileHash -LiteralPath $drawing -Algorithm SHA256).Hash.ToUpperInvariant() -ne $fixtureHash) { throw "Curved lifecycle disposable copy hash mismatch." } }
$resultPath = Join-Path $ArtifactDir "curved-structural-lifecycle-result.txt"
$phasePath = Join-Path $ArtifactDir "curved-structural-lifecycle-session1.txt"
$script1 = Join-Path $ArtifactDir "curved-structural-lifecycle-session1.private.scr"
$script2 = Join-Path $ArtifactDir "curved-structural-lifecycle-session2.private.scr"
$metadataPath = Join-Path $ArtifactDir "curved-structural-lifecycle-metadata.json"
$nativeInsunits = if ($NativeDrawingUnit -eq "Meter") { "6" } else { "4" }
$nonce = [Guid]::NewGuid().ToString("N")
$environmentNames = @("QS3D_CURVED_LIFECYCLE_RESULT","QS3D_CURVED_LIFECYCLE_PHASE_RESULT","QS3D_CURVED_LIFECYCLE_NONCE","QS3D_CURVED_LIFECYCLE_SOURCE_SHA","QS3D_CURVED_LIFECYCLE_DWG_A","QS3D_CURVED_LIFECYCLE_DWG_B","QS3D_CURVED_LIFECYCLE_EXPECTED_REOPEN_FINGERPRINT")
$oldEnvironment = @{}
foreach ($name in $environmentNames) { $oldEnvironment[$name] = [Environment]::GetEnvironmentVariable($name, "Process") }
$process1 = $null
$process2 = $null
$process_cleanup_verified = $false
$script_cleanup_verified = $false
$sidecar_cleanup_verified = $false
$drawing_restore_verified = $false
$phaseMarker = $null
$finalMarker = $null
$startedAt = Get-Date
try {
    $env:QS3D_CURVED_LIFECYCLE_RESULT = $resultPath
    $env:QS3D_CURVED_LIFECYCLE_PHASE_RESULT = $phasePath
    $env:QS3D_CURVED_LIFECYCLE_NONCE = $nonce
    $env:QS3D_CURVED_LIFECYCLE_SOURCE_SHA = $ExpectedSourceSha
    $env:QS3D_CURVED_LIFECYCLE_DWG_A = $drawingA
    $env:QS3D_CURVED_LIFECYCLE_DWG_B = $drawingB
    $session1 = @(
        "FILEDIA","0","CMDECHO","1","TILEMODE","1","INSUNITS",$nativeInsunits,"UCS","W",
        "NETLOAD",('"'+$PluginDll+'"'),
        "QS3DCURVEDLIFEPREPARE","_.UNDO","_Mark",
        "QS3DCURVEDLIFESELECTBEAMS","QS3DBUILD3D",
        "QS3DCURVEDLIFESELECTSLAB","QS3DBUILD3D","QS3DCURVEDLIFECAPTUREBASELINE",
        "_.UNDO","_Back","QS3DCURVEDLIFECHECKUNDO",
        "QS3DCURVEDLIFESELECTBEAMS","_.UNDO","_Begin","QS3DBUILD3D",
        "QS3DCURVEDLIFESELECTSLAB","QS3DBUILD3D","QS3DCURVEDLIFECAPTUREBASELINE",
        "_.UNDO","_End","_.U","_.REDO","QS3DCURVEDLIFECAPTUREREDO","QS3DCURVEDLIFECHECKREDO",
        "QS3DSAVE","_.QSAVE","QS3DCURVEDLIFESESSION1","_.CLOSE","_N","_.QUIT","_N"
    )
    [IO.File]::WriteAllLines($script1, $session1, [Text.Encoding]::ASCII)
    $args1 = '"' + $drawingA + '" /P "' + $Profile + '" /B "' + $script1 + '"'
    $process1 = Start-Process -FilePath $bricscadExe -ArgumentList $args1 -PassThru -WindowStyle Hidden -WorkingDirectory $ArtifactDir
    $deadline = (Get-Date).AddSeconds($StartupTimeoutSeconds)
    while ((Get-Date) -lt $deadline -and -not (Test-Path -LiteralPath $phasePath -PathType Leaf)) {
        if (Test-Path -LiteralPath $resultPath -PathType Leaf) { break }
        $null = Close-Qs3dProxyInformationDialog -Process $process1
        $process1.Refresh(); if ($process1.HasExited) { throw "BricsCAD session one exited before its marker." }
        Start-Sleep -Milliseconds 500
    }
    if (-not (Test-Path -LiteralPath $phasePath -PathType Leaf)) { throw "Curved lifecycle session one did not publish PASS marker." }
    $phaseMarker = Read-LifeMarker -Path $phasePath
    foreach ($pair in @(@("status","PASS"),@("command","QS3DCURVEDLIFESESSION1"),@("undo_coherent","true"),@("redo_coherent","true"),@("undo_generated_absent","true"),@("saved_checkpoint_matches","true"),@("generated_count","4"))) { Require-LifeValue -Marker $phaseMarker -Key $pair[0] -Expected $pair[1] }
    $expectedReopenFingerprint = [string]$phaseMarker["reopen_fingerprint"]
    if ($expectedReopenFingerprint -notmatch '^[0-9A-F]{64}$') { throw "Curved lifecycle reopen fingerprint is invalid." }
    $env:QS3D_CURVED_LIFECYCLE_EXPECTED_REOPEN_FINGERPRINT = $expectedReopenFingerprint
    if (-not $process1.WaitForExit(30000)) { throw "Curved lifecycle session one did not exit gracefully." }

    $session2 = @(
        "FILEDIA","0","CMDECHO","1","TILEMODE","1","INSUNITS",$nativeInsunits,"UCS","W",
        "NETLOAD",('"'+$PluginDll+'"'),"QS3DCURVEDLIFEREOPEN",
        "QS3DCURVEDLIFESELECTBEAMS","QS3DBUILD3D","QS3DCURVEDLIFESELECTSLAB","QS3DBUILD3D","QS3DCURVEDLIFEAFTERREBUILD",
        "QS3DSAVE","_.QSAVE","_.OPEN",('"'+$drawingB+'"'),"INSUNITS",$nativeInsunits,"UCS","W",
        "QS3DCURVEDLIFEPREPAREB","QS3DCURVEDLIFESELECTBEAMS","QS3DBUILD3D","QS3DCURVEDLIFESELECTSLAB","QS3DBUILD3D","QS3DCURVEDLIFECAPTUREB",
        "QS3DCURVEDLIFEACTIVATEA","QS3DCURVEDLIFECHECKA","QS3DCURVEDLIFEACTIVATEB","QS3DCURVEDLIFECOMPLETE","_.QUIT","_N"
    )
    [IO.File]::WriteAllLines($script2, $session2, [Text.Encoding]::ASCII)
    $args2 = '"' + $drawingA + '" /P "' + $Profile + '" /B "' + $script2 + '"'
    $process2 = Start-Process -FilePath $bricscadExe -ArgumentList $args2 -PassThru -WindowStyle Hidden -WorkingDirectory $ArtifactDir
    Wait-LifeMarker -Path $resultPath -Process $process2 -Deadline ((Get-Date).AddSeconds($StartupTimeoutSeconds))
    $finalMarker = Read-LifeMarker -Path $resultPath
    foreach ($pair in @(@("status","PASS"),@("command","QS3DCURVEDLIFECOMPLETE"),@("reopen_coherent","true"),@("rebuild_coherent","true"),@("old_generated_removed","true"),@("new_generated_disjoint","true"),@("rebuild_counts_stable","true"),@("multi_dwg_isolated","true"),@("drawing_a_unchanged","true"),@("drawing_b_unchanged","true"),@("generated_count","4"),@("error_code","NONE"))) { Require-LifeValue -Marker $finalMarker -Key $pair[0] -Expected $pair[1] }
    if (-not $process2.WaitForExit(30000)) { throw "Curved lifecycle session two did not exit gracefully." }
}
finally {
    Stop-LifeProcess -Process $process1
    Stop-LifeProcess -Process $process2
    $process_cleanup_verified = Wait-Qs3dNoExactBricsCadProcesses -ExpectedExecutable $bricscadExe -TimeoutSeconds 30
    foreach ($script in @($script1,$script2)) { Remove-LifeFile -Path $script }
    $script_cleanup_verified = -not (Test-Path -LiteralPath $script1) -and -not (Test-Path -LiteralPath $script2)
    foreach ($drawing in @($drawingA,$drawingB)) {
        $sidecar = [IO.Path]::ChangeExtension($drawing, ".qsdb")
        foreach ($private in @($sidecar,($sidecar+".bak"),($sidecar+".lock"),[IO.Path]::ChangeExtension($drawing,".dwl"),[IO.Path]::ChangeExtension($drawing,".dwl2"),[IO.Path]::ChangeExtension($drawing,".bak"))) { Remove-LifeFile -Path $private }
        Copy-Item -LiteralPath $FixtureDwg -Destination $drawing -Force -ErrorAction Stop
    }
    $sidecar_cleanup_verified = $true
    foreach ($drawing in @($drawingA,$drawingB)) {
        $sidecar = [IO.Path]::ChangeExtension($drawing, ".qsdb")
        $remainingPrivate = @($sidecar,($sidecar+".bak"),($sidecar+".lock"),[IO.Path]::ChangeExtension($drawing,".dwl"),[IO.Path]::ChangeExtension($drawing,".dwl2"),[IO.Path]::ChangeExtension($drawing,".bak"))
        if (@($remainingPrivate | Where-Object { Test-Path -LiteralPath $_ }).Count -gt 0) { $sidecar_cleanup_verified = $false }
    }
    $drawing_restore_verified = ((Get-FileHash -LiteralPath $drawingA -Algorithm SHA256).Hash.ToUpperInvariant() -eq $fixtureHash) -and ((Get-FileHash -LiteralPath $drawingB -Algorithm SHA256).Hash.ToUpperInvariant() -eq $fixtureHash)
    foreach ($name in $environmentNames) { Restore-LifeEnvironment -Name $name -Value $oldEnvironment[$name] }
}
if (-not $process_cleanup_verified) { throw "Curved lifecycle process cleanup failed." }
if (-not $script_cleanup_verified) { throw "Curved lifecycle script cleanup failed." }
if (-not $sidecar_cleanup_verified) { throw "Curved lifecycle sidecar cleanup failed." }
if (-not $drawing_restore_verified) { throw "Curved lifecycle drawing restoration failed." }
$metadata = [ordered]@{
    status = "PASS"; source_sha = $ExpectedSourceSha; native_drawing_unit = $NativeDrawingUnit
    bricscad_file_version = (Get-Item -LiteralPath $bricscadExe).VersionInfo.FileVersion
    plugin_sha256 = (Get-FileHash -LiteralPath $PluginDll -Algorithm SHA256).Hash.ToUpperInvariant()
    core_sha256 = (Get-FileHash -LiteralPath $coreDll -Algorithm SHA256).Hash.ToUpperInvariant()
    drawing_sha256 = $fixtureHash; process_cleanup_verified = $process_cleanup_verified
    script_cleanup_verified = $script_cleanup_verified; sidecar_cleanup_verified = $sidecar_cleanup_verified
    drawing_restore_verified = $drawing_restore_verified; marker = $finalMarker
    started_at = $startedAt.ToUniversalTime().ToString("O"); completed_at = (Get-Date).ToUniversalTime().ToString("O")
}
$metadata | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $metadataPath -Encoding UTF8
Write-Host "QS3D BricsCAD V25 curved lifecycle + multi-DWG runtime PASS"
Write-Host ("Native drawing unit: " + $NativeDrawingUnit + " (INSUNITS=" + $nativeInsunits + ")")
Write-Host ("Marker: " + $resultPath)
Write-Host ("Metadata: " + $metadataPath)
