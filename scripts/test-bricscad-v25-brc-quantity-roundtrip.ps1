param(
    [Parameter(Mandatory = $true)][string]$BricsCadDir,
    [Parameter(Mandatory = $true)][string]$PluginDll,
    [Parameter(Mandatory = $true)][string]$DrawingCopy,
    [Parameter(Mandatory = $true)][string]$Profile,
    [Parameter(Mandatory = $true)][string]$ArtifactDir,
    [Parameter(Mandatory = $true)][switch]$ConfirmReferenceCopy,
    [ValidateRange(30, 900)][int]$StartupTimeoutSeconds = 240
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$windowInteropPath = Join-Path $PSScriptRoot "bricscad-runner-window-interop.ps1"
if (-not (Test-Path -LiteralPath $windowInteropPath -PathType Leaf)) {
    throw "BRC runner window interop helper is missing: $windowInteropPath"
}
. $windowInteropPath

function Read-Qs3dMarker {
    param([Parameter(Mandatory = $true)][string]$Path)
    $marker = @{}
    foreach ($line in Get-Content -LiteralPath $Path) {
        if ([string]::IsNullOrWhiteSpace($line)) { continue }
        $separator = $line.IndexOf('=')
        if ($separator -le 0) { throw "Malformed BRC quantity marker line: $line" }
        $key = $line.Substring(0, $separator).Trim()
        $value = $line.Substring($separator + 1).Trim()
        if ($marker.ContainsKey($key)) { throw "Duplicate BRC quantity marker key: $key" }
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
    if (-not $Marker.ContainsKey($Key)) { throw "BRC quantity marker is missing '$Key'." }
    if (-not [string]::Equals([string]$Marker[$Key], $Expected, [StringComparison]::OrdinalIgnoreCase)) {
        throw "BRC quantity marker '$Key' expected '$Expected' but was '$($Marker[$Key])'."
    }
}

function Read-NonNegativeMarkerInt {
    param(
        [Parameter(Mandatory = $true)]$Marker,
        [Parameter(Mandatory = $true)][string]$Key
    )
    if (-not $Marker.ContainsKey($Key)) { throw "BRC quantity marker is missing '$Key'." }
    [int]$value = 0
    if (-not [int]::TryParse([string]$Marker[$Key], [Globalization.NumberStyles]::None, [Globalization.CultureInfo]::InvariantCulture, [ref]$value) -or $value -lt 0) {
        throw "BRC quantity marker '$Key' is not a non-negative invariant integer."
    }
    return $value
}

function Restore-EnvironmentValue {
    param(
        [Parameter(Mandatory = $true)][string]$Name,
        [AllowNull()][string]$Value
    )
    if ($null -eq $Value) { Remove-Item -LiteralPath ("Env:" + $Name) -ErrorAction SilentlyContinue }
    else { Set-Item -LiteralPath ("Env:" + $Name) -Value $Value }
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
    throw "The BricsCAD V25 BRC quantity round-trip requires Windows."
}
if (-not [Environment]::UserInteractive) {
    throw "The BricsCAD V25 BRC quantity round-trip requires an interactive Windows session."
}
if (-not $ConfirmReferenceCopy) {
    throw "Pass -ConfirmReferenceCopy only for a disposable reference copy, never the owner-supplied original."
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
    if (-not (Test-Path -LiteralPath $required -PathType Leaf)) { throw "Required BRC quantity input is missing: $required" }
}
if (@(Get-Process -Name "bricscad" -ErrorAction SilentlyContinue).Count -gt 0) {
    throw "Close existing BricsCAD processes before starting the isolated BRC quantity round-trip."
}

$projectSidecar = [IO.Path]::ChangeExtension($DrawingCopy, ".qsdb")
if ((Test-Path -LiteralPath $projectSidecar) -or (Test-Path -LiteralPath ($projectSidecar + ".bak"))) {
    throw "The disposable drawing copy must not have a pre-existing QS3D sidecar."
}

New-Item -ItemType Directory -Force -Path $ArtifactDir | Out-Null
$resultPath = Join-Path $ArtifactDir "brc-quantity-roundtrip-result.txt"
$workbookPath = Join-Path $ArtifactDir "brc-quantity-roundtrip.xlsx"
$scriptPath = Join-Path $ArtifactDir "brc-quantity-roundtrip.scr"
$metadataPath = Join-Path $ArtifactDir "brc-quantity-roundtrip-metadata.json"
Remove-Item -LiteralPath $resultPath, $workbookPath, $scriptPath, $metadataPath -Force -ErrorAction SilentlyContinue

$drawingHashBefore = (Get-FileHash -LiteralPath $DrawingCopy -Algorithm SHA256).Hash.ToUpperInvariant()
$pluginHash = (Get-FileHash -LiteralPath $PluginDll -Algorithm SHA256).Hash.ToUpperInvariant()
$nonce = [Guid]::NewGuid().ToString("N")
$oldResult = [Environment]::GetEnvironmentVariable("QS3D_BRC_ROUNDTRIP_RESULT", "Process")
$oldWorkbook = [Environment]::GetEnvironmentVariable("QS3D_BRC_ROUNDTRIP_WORKBOOK", "Process")
$oldNonce = [Environment]::GetEnvironmentVariable("QS3D_BRC_ROUNDTRIP_NONCE", "Process")
$process = $null
$proxyInformationDialogsDismissed = 0
$startedAt = Get-Date

try {
    $env:QS3D_BRC_ROUNDTRIP_RESULT = $resultPath
    $env:QS3D_BRC_ROUNDTRIP_WORKBOOK = $workbookPath
    $env:QS3D_BRC_ROUNDTRIP_NONCE = $nonce
    $script = @(
        "FILEDIA",
        "0",
        "CMDECHO",
        "1",
        "NETLOAD",
        ('"' + $PluginDll + '"'),
        "QS3DB4D",
        "QS3DBRCROUNDTRIPPROBE"
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
        if ($process.HasExited) {
            throw "BricsCAD exited before the BRC quantity round-trip created its marker. ExitCode=$($process.ExitCode)"
        }
        Start-Sleep -Milliseconds 500
    }
    if (-not (Test-Path -LiteralPath $resultPath -PathType Leaf)) {
        throw "Timed out waiting for QS3DB4D + QS3DBRCROUNDTRIPPROBE after $StartupTimeoutSeconds seconds."
    }

    $marker = Read-Qs3dMarker -Path $resultPath
    Require-Qs3dValue -Marker $marker -Key "status" -Expected "PASS"
    Require-Qs3dValue -Marker $marker -Key "command" -Expected "QS3DBRCROUNDTRIPPROBE"
    Require-Qs3dValue -Marker $marker -Key "process" -Expected "bricscad"
    Require-Qs3dValue -Marker $marker -Key "nonce" -Expected $nonce
    Require-Qs3dValue -Marker $marker -Key "schema" -Expected "QS3D_BRC_QUANTITY_ROUNDTRIP_V1"
    Require-Qs3dValue -Marker $marker -Key "is_64bit" -Expected "true"
    Require-Qs3dValue -Marker $marker -Key "modern_ed2_schema" -Expected "true"
    Require-Qs3dValue -Marker $marker -Key "detail_sheet_resolved" -Expected "true"
    Require-Qs3dValue -Marker $marker -Key "drawing_fingerprint_matched" -Expected "true"
    Require-Qs3dValue -Marker $marker -Key "element_handle_provenance_matched" -Expected "true"
    Require-Qs3dValue -Marker $marker -Key "proxy_capture_ready_count" -Expected "0"
    Require-Qs3dValue -Marker $marker -Key "proxy_autoaccepted_count" -Expected "0"
    Require-Qs3dValue -Marker $marker -Key "proxy_captured_owner_count" -Expected "0"

    $projectElementCount = Read-NonNegativeMarkerInt -Marker $marker -Key "project_element_count"
    $detailRowCount = Read-NonNegativeMarkerInt -Marker $marker -Key "detail_row_count"
    $summaryRowCount = Read-NonNegativeMarkerInt -Marker $marker -Key "summary_row_count"
    $locatedHandleCount = Read-NonNegativeMarkerInt -Marker $marker -Key "located_handle_count"
    $selectedObjectCount = Read-NonNegativeMarkerInt -Marker $marker -Key "selected_object_count"
    if ($projectElementCount -le 0 -or $detailRowCount -le 0 -or $summaryRowCount -le 0 -or $locatedHandleCount -le 0) {
        throw "The BRC quantity round-trip produced no usable semantic/export/locate result."
    }
    if ($selectedObjectCount -ne $locatedHandleCount) {
        throw "Excel Locate selected $selectedObjectCount objects but resolved $locatedHandleCount Handles."
    }
    if (-not (Test-Path -LiteralPath $workbookPath -PathType Leaf)) {
        throw "The BRC quantity round-trip did not create its new ED2 workbook."
    }

    Stop-Qs3dLaunchedProcess -Process $process

    $drawingHashAfter = (Get-FileHash -LiteralPath $DrawingCopy -Algorithm SHA256).Hash.ToUpperInvariant()
    if (-not [string]::Equals($drawingHashBefore, $drawingHashAfter, [StringComparison]::Ordinal)) {
        throw "The guarded reference-copy DWG changed during the quantity round-trip. Before=$drawingHashBefore After=$drawingHashAfter"
    }
    if ((Test-Path -LiteralPath $projectSidecar) -or (Test-Path -LiteralPath ($projectSidecar + ".bak"))) {
        throw "The local quantity round-trip unexpectedly persisted a QS3D sidecar beside the disposable DWG."
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
        workbook_sha256 = (Get-FileHash -LiteralPath $workbookPath -Algorithm SHA256).Hash.ToUpperInvariant()
        proxy_information_dialogs_dismissed = $proxyInformationDialogsDismissed
        marker = $marker
    }
    $metadata | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $metadataPath -Encoding UTF8

    Write-Host "QS3D BricsCAD V25 B4D -> ED2 -> Excel Locate round-trip PASS"
    Write-Host "Marker: $resultPath"
    Write-Host "Workbook: $workbookPath"
    Write-Host "Metadata: $metadataPath"
    Write-Host "Proxy Information dialogs dismissed for the launched PID: $proxyInformationDialogsDismissed"
    Write-Host "Reference-copy SHA256 unchanged: $drawingHashAfter"
}
finally {
    Stop-Qs3dLaunchedProcess -Process $process
    Restore-EnvironmentValue -Name "QS3D_BRC_ROUNDTRIP_RESULT" -Value $oldResult
    Restore-EnvironmentValue -Name "QS3D_BRC_ROUNDTRIP_WORKBOOK" -Value $oldWorkbook
    Restore-EnvironmentValue -Name "QS3D_BRC_ROUNDTRIP_NONCE" -Value $oldNonce
}
