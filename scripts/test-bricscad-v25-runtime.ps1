param(
    [Parameter(Mandatory = $true)][string]$BricsCadDir,
    [Parameter(Mandatory = $true)][string]$PluginDll,
    [string]$Profile = "",
    [string]$ArtifactDir = "",
    [ValidateRange(10, 900)][int]$StartupTimeoutSeconds = 120,
    [switch]$DemandLoadOnly,
    [switch]$SkipScreenshot
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

function Read-Qs3dRuntimeMarker {
    param([Parameter(Mandatory = $true)][string]$Path)
    $marker = @{}
    foreach ($line in Get-Content -LiteralPath $Path) {
        if ([string]::IsNullOrWhiteSpace($line)) { continue }
        $separator = $line.IndexOf('=')
        if ($separator -le 0) { throw "Malformed runtime marker line: $line" }
        $key = $line.Substring(0, $separator).Trim()
        $value = $line.Substring($separator + 1).Trim()
        if ($marker.ContainsKey($key)) { throw "Duplicate runtime marker key: $key" }
        $marker[$key] = $value
    }
    return $marker
}

function Require-Qs3dMarkerValue {
    param(
        [Parameter(Mandatory = $true)]$Marker,
        [Parameter(Mandatory = $true)][string]$Key,
        [Parameter(Mandatory = $true)][string]$Expected
    )
    if (-not $Marker.ContainsKey($Key)) { throw "Runtime marker is missing '$Key'." }
    if (-not [string]::Equals([string]$Marker[$Key], $Expected, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Runtime marker '$Key' expected '$Expected' but was '$($Marker[$Key])'."
    }
}

if ([Environment]::OSVersion.Platform -ne [PlatformID]::Win32NT) {
    throw "BricsCAD V25 runtime test requires Windows."
}
if (-not [Environment]::UserInteractive) {
    throw "The V25 runtime/screenshot gate requires an interactive Windows session. Run the GitHub self-hosted runner interactively, not as a Windows service."
}

$repoRoot = Split-Path -Parent $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($ArtifactDir)) {
    $ArtifactDir = Join-Path $repoRoot "artifacts\bricscad-v25-runtime"
}
$ArtifactDir = [IO.Path]::GetFullPath($ArtifactDir)
$BricsCadDir = [IO.Path]::GetFullPath($BricsCadDir)
$PluginDll = [IO.Path]::GetFullPath($PluginDll)

$bricscadExe = Join-Path $BricsCadDir "bricscad.exe"
$brxMgd = Join-Path $BricsCadDir "BrxMgd.dll"
$tdMgd = Join-Path $BricsCadDir "TD_Mgd.dll"
$coreDll = Join-Path (Split-Path -Parent $PluginDll) "QS3D.Core.dll"

foreach ($required in @($bricscadExe, $brxMgd, $tdMgd, $PluginDll, $coreDll)) {
    if (-not (Test-Path -LiteralPath $required -PathType Leaf)) {
        throw "Required runtime file is missing: $required"
    }
}

$existing = @(Get-Process -Name "bricscad" -ErrorAction SilentlyContinue)
if ($existing.Count -gt 0) {
    throw "Close existing BricsCAD processes on the dedicated test runner before starting the runtime gate."
}

New-Item -ItemType Directory -Force -Path $ArtifactDir | Out-Null
$resultPath = Join-Path $ArtifactDir "runtime-result.txt"
$scriptPath = Join-Path $ArtifactDir "runtime.scr"
$screenshotPath = Join-Path $ArtifactDir "bricscad-v25-qs3d.png"
$metadataPath = Join-Path $ArtifactDir "runtime-metadata.json"
Remove-Item -LiteralPath $resultPath, $screenshotPath, $metadataPath -Force -ErrorAction SilentlyContinue

$env:QS3D_RUNTIME_RESULT = $resultPath
$script = @(
    "FILEDIA",
    "0",
    "CMDECHO",
    "1"
)
if (-not $DemandLoadOnly) {
    $script += @("NETLOAD", ('"' + $PluginDll + '"'))
}
$script += "QS3DRUNTIMEPROBE"
Set-Content -LiteralPath $scriptPath -Value $script -Encoding ASCII

$argumentParts = New-Object System.Collections.Generic.List[string]
$argumentParts.Add('/L')
if (-not [string]::IsNullOrWhiteSpace($Profile)) {
    $argumentParts.Add('/P')
    $argumentParts.Add('"' + $Profile + '"')
}
$argumentParts.Add('/B')
$argumentParts.Add('"' + $scriptPath + '"')
$arguments = [string]::Join(' ', $argumentParts)

$startedAt = Get-Date
$process = $null

try {
    $process = Start-Process -FilePath $bricscadExe -ArgumentList $arguments -WorkingDirectory $ArtifactDir -PassThru
    $deadline = (Get-Date).AddSeconds($StartupTimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        if (Test-Path -LiteralPath $resultPath -PathType Leaf) { break }
        if ($process.HasExited) {
            throw "BricsCAD exited before QS3DRUNTIMEPROBE created the runtime marker. ExitCode=$($process.ExitCode)"
        }
        Start-Sleep -Milliseconds 500
        $process.Refresh()
    }

    if (-not (Test-Path -LiteralPath $resultPath -PathType Leaf)) {
        throw "Timed out waiting for NETLOAD + QS3DRUNTIMEPROBE after $StartupTimeoutSeconds seconds."
    }

    $marker = Read-Qs3dRuntimeMarker -Path $resultPath
    Require-Qs3dMarkerValue -Marker $marker -Key "status" -Expected "PASS"
    Require-Qs3dMarkerValue -Marker $marker -Key "command" -Expected "QS3DRUNTIMEPROBE"
    Require-Qs3dMarkerValue -Marker $marker -Key "process" -Expected "bricscad"
    Require-Qs3dMarkerValue -Marker $marker -Key "is_64bit" -Expected "true"
    Require-Qs3dMarkerValue -Marker $marker -Key "ribbon_ready" -Expected "true"
    Require-Qs3dMarkerValue -Marker $marker -Key "palette_visible" -Expected "true"
    Require-Qs3dMarkerValue -Marker $marker -Key "right_palette_visible" -Expected "false"

    if (-not $marker.ContainsKey("assembly")) { throw "Runtime marker is missing 'assembly'." }
    $loadedAssembly = [IO.Path]::GetFullPath([string]$marker["assembly"])
    if (-not [string]::Equals($loadedAssembly, $PluginDll, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Runtime marker came from a different plugin DLL. Expected '$PluginDll', loaded '$loadedAssembly'."
    }

    $loadMode = if ($DemandLoadOnly) { "DemandLoad" } else { "NETLOAD" }

    if (-not $SkipScreenshot) {
        $windowDeadline = (Get-Date).AddSeconds(30)
        while ((Get-Date) -lt $windowDeadline) {
            $process.Refresh()
            if ($process.MainWindowHandle -ne [IntPtr]::Zero) { break }
            Start-Sleep -Milliseconds 500
        }
        if ($process.MainWindowHandle -eq [IntPtr]::Zero) {
            throw "BricsCAD runtime passed, but no interactive main window was available for the requested screenshot."
        }

        Add-Type -AssemblyName System.Drawing
        Add-Type @"
using System;
using System.Runtime.InteropServices;
public static class QS3DWin32Capture {
    [StructLayout(LayoutKind.Sequential)]
    public struct RECT { public int Left; public int Top; public int Right; public int Bottom; }
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr hWnd, out RECT rect);
    [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    [DllImport("user32.dll")] public static extern bool PrintWindow(IntPtr hWnd, IntPtr hdcBlt, uint nFlags);
}
"@

        [QS3DWin32Capture]::ShowWindow($process.MainWindowHandle, 9) | Out-Null
        Start-Sleep -Seconds 3

        $rect = New-Object QS3DWin32Capture+RECT
        if (-not [QS3DWin32Capture]::GetWindowRect($process.MainWindowHandle, [ref]$rect)) {
            throw "Unable to read BricsCAD window bounds for screenshot capture."
        }
        $width = $rect.Right - $rect.Left
        $height = $rect.Bottom - $rect.Top
        if ($width -lt 400 -or $height -lt 300) {
            throw "BricsCAD window bounds are unexpectedly small: ${width}x${height}."
        }

        $bitmap = New-Object System.Drawing.Bitmap $width, $height
        $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
        try {
            # Capture only the target BricsCAD HWND. A desktop-region capture can
            # include unrelated windows that overlap the host and leak private UI.
            $hdc = $graphics.GetHdc()
            try {
                $captured = [QS3DWin32Capture]::PrintWindow($process.MainWindowHandle, $hdc, 2)
                if (-not $captured) {
                    $captured = [QS3DWin32Capture]::PrintWindow($process.MainWindowHandle, $hdc, 0)
                }
                if (-not $captured) {
                    throw "PrintWindow could not capture the BricsCAD window without exposing the desktop."
                }
            }
            finally {
                $graphics.ReleaseHdc($hdc)
            }
            $bitmap.Save($screenshotPath, [System.Drawing.Imaging.ImageFormat]::Png)
        }
        finally {
            $graphics.Dispose()
            $bitmap.Dispose()
        }

        if (-not (Test-Path -LiteralPath $screenshotPath -PathType Leaf)) {
            throw "Screenshot file was not created."
        }
    }

    $metadata = [ordered]@{
        status = "PASS"
        started_at = $startedAt.ToUniversalTime().ToString("O")
        completed_at = (Get-Date).ToUniversalTime().ToString("O")
        bricscad_exe = $bricscadExe
        bricscad_file_version = (Get-Item -LiteralPath $bricscadExe).VersionInfo.FileVersion
        plugin_dll = $PluginDll
        plugin_sha256 = (Get-FileHash -Algorithm SHA256 -LiteralPath $PluginDll).Hash
        runtime_marker = $resultPath
        screenshot = if ($SkipScreenshot) { $null } else { $screenshotPath }
        screenshot_capture = if ($SkipScreenshot) { $null } else { "PrintWindow(hwnd)" }
        load_mode = $loadMode
        process_id = $process.Id
        profile = $Profile
        runner_user = [Environment]::UserName
        interactive = [Environment]::UserInteractive
        ribbon_ready = $true
        palette_visible = $true
        right_palette_visible = $false
    }
    $metadata | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath $metadataPath -Encoding UTF8

    Write-Host "QS3D BricsCAD V25 $loadMode/runtime gate PASS"
    Write-Host "Marker: $resultPath"
    if (-not $SkipScreenshot) { Write-Host "Screenshot: $screenshotPath" }
}
finally {
    if ($null -ne $process) {
        try {
            $process.Refresh()
            if (-not $process.HasExited) {
                Stop-Process -Id $process.Id -Force -ErrorAction SilentlyContinue
                $process.WaitForExit(10000) | Out-Null
            }
        }
        catch { }
    }
    Remove-Item Env:QS3D_RUNTIME_RESULT -ErrorAction SilentlyContinue
}
