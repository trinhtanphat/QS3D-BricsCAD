[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$BricsCadDir,
    [Parameter(Mandatory = $true)][string]$PluginDll,
    [Parameter(Mandatory = $true)][string]$ArtifactDir,
    [Parameter(Mandatory = $true)][ValidatePattern('^[0-9a-fA-F]{40}$')][string]$CandidateSha,
    [string]$Profile = 'Default',
    [string]$CloudflaredPath = '',
    [ValidateRange(30, 900)][int]$StartupTimeoutSeconds = 180,
    [switch]$SkipScreenshot
)

# LOCAL_ONLY holder for the MCP qualification cell.  It deliberately uses a
# fresh V25 profile sandbox, an exact NETLOAD DLL and an explicit stop signal.
# The canonical generic V25 runtime runner must run once before this holder.
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

. (Join-Path $PSScriptRoot 'v25-profile-sandbox.ps1')

function Wait-Qs3dTcpPort {
    param([Parameter(Mandatory = $true)][int]$Port, [Parameter(Mandatory = $true)][datetime]$Deadline)

    while ((Get-Date) -lt $Deadline) {
        $client = $null
        try {
            $client = New-Object System.Net.Sockets.TcpClient
            $task = $client.ConnectAsync('127.0.0.1', $Port)
            if ($task.Wait(500) -and $client.Connected) { return }
        }
        catch { }
        finally { if ($null -ne $client) { $client.Dispose() } }
        Start-Sleep -Milliseconds 300
    }
    throw "Timed out waiting for QS3D MCP loopback port $Port."
}

function Read-Qs3dRuntimeMarker {
    param([Parameter(Mandatory = $true)][string]$Path)

    $marker = @{}
    foreach ($line in Get-Content -LiteralPath $Path) {
        if ([string]::IsNullOrWhiteSpace($line)) { continue }
        $separator = $line.IndexOf('=')
        if ($separator -le 0) { throw 'Malformed runtime marker line.' }
        $key = $line.Substring(0, $separator).Trim()
        if ($marker.ContainsKey($key)) { throw "Duplicate runtime marker key: $key" }
        $marker[$key] = $line.Substring($separator + 1).Trim()
    }
    return $marker
}

function Require-Qs3dMarkerValue {
    param([Parameter(Mandatory = $true)]$Marker, [Parameter(Mandatory = $true)][string]$Key, [Parameter(Mandatory = $true)][string]$Expected)

    if (-not $Marker.ContainsKey($Key) -or -not [string]::Equals([string]$Marker[$Key], $Expected, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Runtime marker '$Key' did not match the expected V25 value."
    }
}

if ([Environment]::OSVersion.Platform -ne [PlatformID]::Win32NT -or -not [Environment]::UserInteractive) {
    throw 'MCP V25 local cell requires an interactive Windows desktop.'
}

$BricsCadDir = [IO.Path]::GetFullPath($BricsCadDir)
$PluginDll = [IO.Path]::GetFullPath($PluginDll)
$ArtifactDir = [IO.Path]::GetFullPath($ArtifactDir)
$bricscadExe = Join-Path $BricsCadDir 'bricscad.exe'
$coreDll = Join-Path (Split-Path -Parent $PluginDll) 'QS3D.Core.dll'
foreach ($required in @($bricscadExe, (Join-Path $BricsCadDir 'BrxMgd.dll'), (Join-Path $BricsCadDir 'TD_Mgd.dll'), $PluginDll, $coreDll)) {
    if (-not (Test-Path -LiteralPath $required -PathType Leaf)) { throw "Required V25 runtime file is missing: $required" }
}
if (-not [string]::IsNullOrWhiteSpace($CloudflaredPath)) {
    $CloudflaredPath = [IO.Path]::GetFullPath($CloudflaredPath)
    if (-not (Test-Path -LiteralPath $CloudflaredPath -PathType Leaf)) { throw "CloudflaredPath is missing: $CloudflaredPath" }
}

Assert-Qs3dNoBricsCadProcess
if (@(Get-Process -Name 'cloudflared' -ErrorAction SilentlyContinue).Count -gt 0) {
    throw 'A cloudflared process is already active; this cell refuses to take ownership.'
}

New-Item -ItemType Directory -Force -Path $ArtifactDir | Out-Null
$resultPath = Join-Path $ArtifactDir 'runtime-result.txt'
$scriptPath = Join-Path $ArtifactDir 'mcp-runtime.scr'
$readyPath = Join-Path $ArtifactDir 'mcp-ready.json'
$ownerPidPath = Join-Path $ArtifactDir 'owned-bricscad-pid.txt'
$stopPath = Join-Path $ArtifactDir 'stop.signal'
$cleanupPath = Join-Path $ArtifactDir 'cleanup.json'
$screenshotPath = Join-Path $ArtifactDir 'bricscad-v25-mcp.png'
Remove-Item -LiteralPath $resultPath, $scriptPath, $readyPath, $ownerPidPath, $stopPath, $cleanupPath, $screenshotPath -Force -ErrorAction SilentlyContinue

$process = $null
$sandbox = $null
$runtimeError = $null
$cleanupError = $null
$startedAt = [DateTime]::UtcNow
$priorCloudflaredPath = $env:QS3D_CLOUDFLARED_PATH
$gracefulCloseAttempted = $false
$gracefulCloseSucceeded = $false
$forceCloseFallbackUsed = $false

try {
    if (-not [string]::IsNullOrWhiteSpace($CloudflaredPath)) { $env:QS3D_CLOUDFLARED_PATH = $CloudflaredPath }
    $sandbox = New-Qs3dV25ProfileSandbox -SourceProfile $Profile
    $env:QS3D_RUNTIME_RESULT = $resultPath
    Set-Content -LiteralPath $scriptPath -Encoding ASCII -Value @(
        'FILEDIA', '0', 'CMDECHO', '1', 'NETLOAD', ('"' + $PluginDll + '"'), 'QS3DRUNTIMEPROBE', 'QS3DMCPSTART'
    )

    $arguments = '/L /P "' + $sandbox.NonceProfile + '" /B "' + $scriptPath + '"'
    $process = Start-Process -FilePath $bricscadExe -ArgumentList $arguments -WorkingDirectory $ArtifactDir -PassThru
    Set-Content -LiteralPath $ownerPidPath -Value ([string]$process.Id) -Encoding ASCII

    $deadline = (Get-Date).AddSeconds($StartupTimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        if (Test-Path -LiteralPath $resultPath -PathType Leaf) { break }
        $process.Refresh()
        if ($process.HasExited) { throw "BricsCAD exited before QS3DRUNTIMEPROBE wrote a marker. ExitCode=$($process.ExitCode)" }
        Start-Sleep -Milliseconds 500
    }
    if (-not (Test-Path -LiteralPath $resultPath -PathType Leaf)) { throw "Timed out waiting for the V25 runtime marker after $StartupTimeoutSeconds seconds." }

    $marker = Read-Qs3dRuntimeMarker -Path $resultPath
    foreach ($pair in @{ status='PASS'; command='QS3DRUNTIMEPROBE'; process='bricscad'; is_64bit='true'; native_runtime_major='25'; native_runtime_label='V25'; native_runtime_matches='true'; ribbon_ready='true'; palette_visible='true'; workspace_palette_visible='true'; right_palette_visible='true'; quantity_palette_visible='false' }.GetEnumerator()) {
        Require-Qs3dMarkerValue -Marker $marker -Key $pair.Key -Expected $pair.Value
    }
    $loadedAssembly = [IO.Path]::GetFullPath([string]$marker['assembly'])
    if (-not [string]::Equals($loadedAssembly, $PluginDll, [StringComparison]::OrdinalIgnoreCase)) { throw 'Runtime marker came from a different plugin DLL.' }
    Wait-Qs3dTcpPort -Port 8765 -Deadline ((Get-Date).AddSeconds(30))

    if (-not $SkipScreenshot) {
        $windowDeadline = (Get-Date).AddSeconds(30)
        while ((Get-Date) -lt $windowDeadline) {
            $process.Refresh()
            if ($process.MainWindowHandle -ne [IntPtr]::Zero) { break }
            Start-Sleep -Milliseconds 500
        }
        if ($process.MainWindowHandle -eq [IntPtr]::Zero) { throw 'No interactive BricsCAD window is available for target-only capture.' }
        Add-Type -AssemblyName System.Drawing
        Add-Type @"
using System; using System.Runtime.InteropServices;
public static class Qs3dMcpCapture { [DllImport("user32.dll")] public static extern bool PrintWindow(IntPtr hWnd, IntPtr hdcBlt, uint flags); }
"@
        $bitmap = New-Object System.Drawing.Bitmap 1600, 1000
        $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
        try {
            $hdc = $graphics.GetHdc()
            try {
                $captured = [Qs3dMcpCapture]::PrintWindow($process.MainWindowHandle, $hdc, 2)
                if (-not $captured) { $captured = [Qs3dMcpCapture]::PrintWindow($process.MainWindowHandle, $hdc, 0) }
                if (-not $captured) { throw 'Target-only PrintWindow capture failed.' }
            }
            finally { $graphics.ReleaseHdc($hdc) }
            $bitmap.Save($screenshotPath, [System.Drawing.Imaging.ImageFormat]::Png)
        }
        finally { $graphics.Dispose(); $bitmap.Dispose() }
    }

    [ordered]@{
        status = 'READY'
        candidate_sha = $CandidateSha.ToLowerInvariant()
        plugin_sha256 = (Get-FileHash -Algorithm SHA256 -LiteralPath $PluginDll).Hash
        process_id = $process.Id
        runtime_marker = 'runtime-result.txt'
        loopback_port = 8765
        screenshot = if ($SkipScreenshot) { $null } else { 'bricscad-v25-mcp.png' }
        started_at = $startedAt.ToString('O')
    } | ConvertTo-Json -Depth 3 | Set-Content -LiteralPath $readyPath -Encoding UTF8
    Write-Host "MCP V25 local cell READY (owned PID $($process.Id)); create stop.signal after sanitized probes finish."

    while (-not (Test-Path -LiteralPath $stopPath -PathType Leaf)) {
        $process.Refresh()
        if ($process.HasExited) { throw "Owned BricsCAD exited while the MCP cell was active. ExitCode=$($process.ExitCode)" }
        Start-Sleep -Seconds 1
    }
}
catch { $runtimeError = $_ }
finally {
    Remove-Item Env:QS3D_RUNTIME_RESULT -ErrorAction SilentlyContinue
    if ($null -ne $process) {
        try {
            $process.Refresh()
            if (-not $process.HasExited) {
                $gracefulCloseAttempted = $true
                try { $gracefulCloseSucceeded = $process.CloseMainWindow() -and $process.WaitForExit(5000) } catch { $gracefulCloseSucceeded = $false }
            }
            $process.Refresh()
            if (-not $process.HasExited) {
                $forceCloseFallbackUsed = $true
                Microsoft.PowerShell.Management\Stop-Process -Id $process.Id -Force -ErrorAction SilentlyContinue
                $process.WaitForExit(10000) | Out-Null
            }
        }
        catch { $cleanupError = $_ }
        finally { try { $process.Dispose() } catch { } }
    }
    try {
        Assert-Qs3dNoBricsCadProcess
        if (@(Get-Process -Name 'cloudflared' -ErrorAction SilentlyContinue).Count -gt 0) { throw 'cloudflared residue is present; cleanup is not proven.' }
        $profileEvidence = if ($null -ne $sandbox) { Restore-Qs3dV25ProfileSandbox -Sandbox $sandbox } else { $null }
        [ordered]@{
            status = if ($null -eq $runtimeError -and $null -eq $cleanupError) { 'CLEANUP_PASS' } else { 'CLEANUP_FAIL' }
            zero_bricscad_processes = $true
            zero_cloudflared_processes = $true
            profile_sandbox_restored = ($null -eq $sandbox -or [bool]$profileEvidence.cur_profile_restored)
            graceful_close_attempted = $gracefulCloseAttempted
            graceful_close_succeeded = $gracefulCloseSucceeded
            force_close_fallback_used = $forceCloseFallbackUsed
            completed_at = [DateTime]::UtcNow.ToString('O')
        } | ConvertTo-Json -Depth 3 | Set-Content -LiteralPath $cleanupPath -Encoding UTF8
    }
    catch { if ($null -eq $cleanupError) { $cleanupError = $_ } }
    $env:QS3D_CLOUDFLARED_PATH = $priorCloudflaredPath
}

if ($null -ne $cleanupError) {
    if ($null -ne $runtimeError) { throw "MCP V25 cell failed ('$($runtimeError.Exception.Message)') and cleanup failed ('$($cleanupError.Exception.Message)')." }
    throw $cleanupError
}
if ($null -ne $runtimeError) { throw $runtimeError }
