param(
    [Parameter(Mandatory = $true)][string]$BricsCadDir,
    [Parameter(Mandatory = $true)][string]$PluginDll,
    [string]$Profile = "",
    [string]$ArtifactDir = "",
    [ValidateRange(10, 900)][int]$StartupTimeoutSeconds = 120,
    [switch]$DemandLoadOnly
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
    throw "BricsCAD V26 runtime qualification requires Windows."
}
if (-not [Environment]::UserInteractive) {
    throw "BricsCAD V26 runtime qualification requires an interactive Windows session."
}

$repoRoot = Split-Path -Parent $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($ArtifactDir)) {
    $ArtifactDir = Join-Path $repoRoot "artifacts\bricscad-v26-runtime"
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
        throw "Required V26 runtime file is missing: $required"
    }
}

$hostVersion = (Get-Item -LiteralPath $bricscadExe).VersionInfo
if ($hostVersion.FileMajorPart -ne 26) {
    throw "BRICSCAD_V26_DIR must point to BricsCAD V26. Detected file version '$($hostVersion.FileVersion)'."
}
if ([IO.Path]::GetFileName($PluginDll) -ne "QS3D.BricsCAD.V26.dll") {
    throw "V26 runtime gate requires QS3D.BricsCAD.V26.dll, not a V25 adapter binary."
}

$existing = @(Get-Process -Name "bricscad" -ErrorAction SilentlyContinue)
if ($existing.Count -gt 0) {
    throw "Close existing BricsCAD processes on the dedicated V26 qualification runner first."
}

New-Item -ItemType Directory -Force -Path $ArtifactDir | Out-Null
$resultPath = Join-Path $ArtifactDir "runtime-result.txt"
$scriptPath = Join-Path $ArtifactDir "runtime.scr"
$metadataPath = Join-Path $ArtifactDir "runtime-metadata.json"
Remove-Item -LiteralPath $resultPath, $metadataPath -Force -ErrorAction SilentlyContinue

$env:QS3D_RUNTIME_RESULT = $resultPath
$script = @("FILEDIA", "0", "CMDECHO", "1")
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
$process = $null
$startedAt = [DateTime]::UtcNow

try {
    $process = Start-Process -FilePath $bricscadExe -ArgumentList $arguments -WorkingDirectory $ArtifactDir -PassThru
    $deadline = (Get-Date).AddSeconds($StartupTimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        if (Test-Path -LiteralPath $resultPath -PathType Leaf) { break }
        $process.Refresh()
        if ($process.HasExited) {
            throw "BricsCAD V26 exited before QS3DRUNTIMEPROBE created the runtime marker. ExitCode=$($process.ExitCode)"
        }
        Start-Sleep -Milliseconds 500
    }

    if (-not (Test-Path -LiteralPath $resultPath -PathType Leaf)) {
        throw "Timed out waiting for BricsCAD V26 runtime marker after $StartupTimeoutSeconds seconds."
    }

    $marker = Read-Qs3dRuntimeMarker -Path $resultPath
    Require-Qs3dMarkerValue -Marker $marker -Key "status" -Expected "PASS"
    Require-Qs3dMarkerValue -Marker $marker -Key "command" -Expected "QS3DRUNTIMEPROBE"
    Require-Qs3dMarkerValue -Marker $marker -Key "process" -Expected "bricscad"
    Require-Qs3dMarkerValue -Marker $marker -Key "is_64bit" -Expected "true"
    Require-Qs3dMarkerValue -Marker $marker -Key "ribbon_ready" -Expected "true"
    Require-Qs3dMarkerValue -Marker $marker -Key "palette_visible" -Expected "true"
    Require-Qs3dMarkerValue -Marker $marker -Key "workspace_palette_visible" -Expected "true"
    Require-Qs3dMarkerValue -Marker $marker -Key "right_palette_visible" -Expected "false"
    Require-Qs3dMarkerValue -Marker $marker -Key "quantity_palette_visible" -Expected "false"

    if (-not $marker.ContainsKey("assembly")) { throw "Runtime marker is missing 'assembly'." }
    $loadedAssembly = [IO.Path]::GetFullPath([string]$marker["assembly"])
    if (-not [string]::Equals($loadedAssembly, $PluginDll, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Runtime marker came from a different plugin DLL. Expected '$PluginDll', loaded '$loadedAssembly'."
    }

    $metadata = [ordered]@{
        status = "PASS"
        host_major = 26
        bricscad_file_version = $hostVersion.FileVersion
        started_at = $startedAt.ToString("O")
        completed_at = [DateTime]::UtcNow.ToString("O")
        plugin_sha256 = (Get-FileHash -Algorithm SHA256 -LiteralPath $PluginDll).Hash
        load_mode = if ($DemandLoadOnly) { "DemandLoad" } else { "NETLOAD" }
        ribbon_ready = $true
        palette_visible = $true
        workspace_palette_visible = $true
        right_palette_visible = $false
        quantity_palette_visible = $false
        interactive = [Environment]::UserInteractive
    }
    $metadata | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath $metadataPath -Encoding UTF8

    Write-Host "QS3D BricsCAD V26 runtime gate PASS"
    Write-Host "Marker: $resultPath"
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
