param(
    [Parameter(Mandatory = $true)][string]$BricsCadDir,
    [Parameter(Mandatory = $true)][string]$PluginDll,
    [Parameter(Mandatory = $true)][string]$DrawingCopy,
    [Parameter(Mandatory = $true)][string]$Profile,
    [Parameter(Mandatory = $true)][string]$ArtifactDir,
    [Parameter(Mandatory = $true)][switch]$ConfirmReferenceCopy,
    [ValidateRange(10, 900)][int]$StartupTimeoutSeconds = 180
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$windowInteropPath = Join-Path $PSScriptRoot "bricscad-runner-window-interop.ps1"
if (-not (Test-Path -LiteralPath $windowInteropPath -PathType Leaf)) {
    throw "BRC runner window interop helper is missing: $windowInteropPath"
}
. $windowInteropPath

function Read-Qs3dProbeMarker {
    param([Parameter(Mandatory = $true)][string]$Path)
    $marker = @{}
    foreach ($line in Get-Content -LiteralPath $Path) {
        if ([string]::IsNullOrWhiteSpace($line)) { continue }
        $separator = $line.IndexOf('=')
        if ($separator -le 0) { throw "Malformed BRC probe marker line: $line" }
        $key = $line.Substring(0, $separator).Trim()
        $value = $line.Substring($separator + 1).Trim()
        if ($marker.ContainsKey($key)) { throw "Duplicate BRC probe marker key: $key" }
        $marker[$key] = $value
    }
    return $marker
}

function Require-Qs3dProbeValue {
    param(
        [Parameter(Mandatory = $true)]$Marker,
        [Parameter(Mandatory = $true)][string]$Key,
        [Parameter(Mandatory = $true)][string]$Expected
    )
    if (-not $Marker.ContainsKey($Key)) { throw "BRC probe marker is missing '$Key'." }
    if (-not [string]::Equals([string]$Marker[$Key], $Expected, [StringComparison]::OrdinalIgnoreCase)) {
        throw "BRC probe marker '$Key' expected '$Expected' but was '$($Marker[$Key])'."
    }
}

function Read-NonNegativeMarkerInt {
    param(
        [Parameter(Mandatory = $true)]$Marker,
        [Parameter(Mandatory = $true)][string]$Key
    )
    if (-not $Marker.ContainsKey($Key)) { throw "BRC probe marker is missing '$Key'." }
    [int]$value = 0
    if (-not [int]::TryParse([string]$Marker[$Key], [Globalization.NumberStyles]::None, [Globalization.CultureInfo]::InvariantCulture, [ref]$value) -or $value -lt 0) {
        throw "BRC probe marker '$Key' is not a non-negative invariant integer."
    }
    return $value
}

function Restore-EnvironmentValue {
    param(
        [Parameter(Mandatory = $true)][string]$Name,
        [AllowNull()][string]$Value
    )
    if ($null -eq $Value) {
        Remove-Item -LiteralPath ("Env:" + $Name) -ErrorAction SilentlyContinue
    }
    else {
        Set-Item -LiteralPath ("Env:" + $Name) -Value $Value
    }
}

function Stop-Qs3dLaunchedProcess {
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

if ([Environment]::OSVersion.Platform -ne [PlatformID]::Win32NT) {
    throw "The BricsCAD V25 BRC public probe requires Windows."
}
if (-not [Environment]::UserInteractive) {
    throw "The BricsCAD V25 BRC public probe requires an interactive Windows session."
}
if (-not $ConfirmReferenceCopy) {
    throw "Pass -ConfirmReferenceCopy only after verifying that DrawingCopy is a disposable reference copy, never the owner-supplied original."
}

$BricsCadDir = [IO.Path]::GetFullPath($BricsCadDir)
$PluginDll = [IO.Path]::GetFullPath($PluginDll)
$DrawingCopy = [IO.Path]::GetFullPath($DrawingCopy)
$ArtifactDir = [IO.Path]::GetFullPath($ArtifactDir)
if (-not [IO.Path]::GetFileName($DrawingCopy).EndsWith(".reference-copy.dwg", [StringComparison]::OrdinalIgnoreCase)) {
    throw "DrawingCopy must use the guarded '*.reference-copy.dwg' suffix."
}

$bricscadExe = Join-Path $BricsCadDir "bricscad.exe"
$coreDll = Join-Path (Split-Path -Parent $PluginDll) "QS3D.Core.dll"
foreach ($required in @($bricscadExe, $PluginDll, $coreDll, $DrawingCopy)) {
    if (-not (Test-Path -LiteralPath $required -PathType Leaf)) {
        throw "Required BRC probe input is missing: $required"
    }
}
if (@(Get-Process -Name "bricscad" -ErrorAction SilentlyContinue).Count -gt 0) {
    throw "Close existing BricsCAD processes before starting the isolated BRC public probe."
}

New-Item -ItemType Directory -Force -Path $ArtifactDir | Out-Null
$resultPath = Join-Path $ArtifactDir "brc-public-probe-result.txt"
$scriptPath = Join-Path $ArtifactDir "brc-public-probe.scr"
$metadataPath = Join-Path $ArtifactDir "brc-public-probe-metadata.json"
Remove-Item -LiteralPath $resultPath, $scriptPath, $metadataPath -Force -ErrorAction SilentlyContinue

$drawingHashBefore = (Get-FileHash -LiteralPath $DrawingCopy -Algorithm SHA256).Hash.ToUpperInvariant()
$pluginHash = (Get-FileHash -LiteralPath $PluginDll -Algorithm SHA256).Hash.ToUpperInvariant()
$nonce = [Guid]::NewGuid().ToString("N")
$oldResult = [Environment]::GetEnvironmentVariable("QS3D_BRC_PROBE_RESULT", "Process")
$oldNonce = [Environment]::GetEnvironmentVariable("QS3D_BRC_PROBE_NONCE", "Process")
$process = $null
$proxyInformationDialogsDismissed = 0
$startedAt = Get-Date

try {
    $env:QS3D_BRC_PROBE_RESULT = $resultPath
    $env:QS3D_BRC_PROBE_NONCE = $nonce
    $script = @(
        "FILEDIA",
        "0",
        "CMDECHO",
        "1",
        "NETLOAD",
        ('"' + $PluginDll + '"'),
        "QS3DBRCPROBE"
    )
    Set-Content -LiteralPath $scriptPath -Value $script -Encoding ASCII

    $argumentParts = New-Object System.Collections.Generic.List[string]
    $argumentParts.Add('"' + $DrawingCopy + '"')
    $argumentParts.Add('/P')
    $argumentParts.Add('"' + $Profile + '"')
    $argumentParts.Add('/B')
    $argumentParts.Add('"' + $scriptPath + '"')
    $arguments = [string]::Join(' ', $argumentParts)
    $process = Start-Process -FilePath $bricscadExe -ArgumentList $arguments -PassThru -WindowStyle Hidden -WorkingDirectory $ArtifactDir

    $deadline = (Get-Date).AddSeconds($StartupTimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        if (Test-Path -LiteralPath $resultPath -PathType Leaf) { break }
        $proxyInformationDialogsDismissed += Close-Qs3dProxyInformationDialog -Process $process
        $process.Refresh()
        if ($process.HasExited) {
            throw "BricsCAD exited before QS3DBRCPROBE created its marker. ExitCode=$($process.ExitCode)"
        }
        Start-Sleep -Milliseconds 500
    }
    if (-not (Test-Path -LiteralPath $resultPath -PathType Leaf)) {
        throw "Timed out waiting for NETLOAD + QS3DBRCPROBE after $StartupTimeoutSeconds seconds."
    }

    $marker = Read-Qs3dProbeMarker -Path $resultPath
    Require-Qs3dProbeValue -Marker $marker -Key "status" -Expected "PASS"
    Require-Qs3dProbeValue -Marker $marker -Key "command" -Expected "QS3DBRCPROBE"
    Require-Qs3dProbeValue -Marker $marker -Key "process" -Expected "bricscad"
    Require-Qs3dProbeValue -Marker $marker -Key "nonce" -Expected $nonce
    Require-Qs3dProbeValue -Marker $marker -Key "schema" -Expected "QS3D_BRC_PUBLIC_PROBE_V1"
    Require-Qs3dProbeValue -Marker $marker -Key "is_64bit" -Expected "true"
    $entityCount = Read-NonNegativeMarkerInt -Marker $marker -Key "entity_attempted_count"
    if ($entityCount -le 0) { throw "The reference copy exposed no Current Space entities to the public probe." }
    $proxyEntityCount = Read-NonNegativeMarkerInt -Marker $marker -Key "proxy_entity_count"
    Require-Qs3dProbeValue -Marker $marker -Key "scan_complete" -Expected "true"

    # BricsCAD holds the opened DWG exclusively. Stop only the process launched
    # above before the mandatory after-hash; otherwise Get-FileHash cannot prove
    # that the disposable reference copy remained byte-for-byte unchanged.
    Stop-Qs3dLaunchedProcess -Process $process

    $drawingHashAfter = (Get-FileHash -LiteralPath $DrawingCopy -Algorithm SHA256).Hash.ToUpperInvariant()
    if (-not [string]::Equals($drawingHashBefore, $drawingHashAfter, [StringComparison]::Ordinal)) {
        throw "The guarded reference-copy DWG changed during the read-only public probe. Before=$drawingHashBefore After=$drawingHashAfter"
    }

    $metadata = [ordered]@{
        status = "PASS"
        started_at = $startedAt.ToUniversalTime().ToString("O")
        completed_at = (Get-Date).ToUniversalTime().ToString("O")
        profile = $Profile
        bricscad_file_version = (Get-Item -LiteralPath $bricscadExe).VersionInfo.FileVersion
        plugin_sha256 = $pluginHash
        drawing_copy_sha256_before = $drawingHashBefore
        drawing_copy_sha256_after = $drawingHashAfter
        marker_schema = [string]$marker["schema"]
        entity_attempted_count = $entityCount
        entity_opened_count = Read-NonNegativeMarkerInt -Marker $marker -Key "entity_opened_count"
        proxy_entity_count = $proxyEntityCount
        proxy_information_dialogs_dismissed = $proxyInformationDialogsDismissed
        marker = $marker
    }
    $metadata | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $metadataPath -Encoding UTF8

    Write-Host "QS3D BricsCAD V25 clean-room BRC public probe PASS"
    Write-Host "Marker: $resultPath"
    Write-Host "Metadata: $metadataPath"
    Write-Host "Proxy Information dialogs dismissed for the launched PID: $proxyInformationDialogsDismissed"
    Write-Host "Reference-copy SHA256 unchanged: $drawingHashAfter"
}
finally {
    Stop-Qs3dLaunchedProcess -Process $process
    Restore-EnvironmentValue -Name "QS3D_BRC_PROBE_RESULT" -Value $oldResult
    Restore-EnvironmentValue -Name "QS3D_BRC_PROBE_NONCE" -Value $oldNonce
}
