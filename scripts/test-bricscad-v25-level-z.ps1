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
    throw "Level-Z runner window interop helper is missing: $windowInteropPath"
}
. $windowInteropPath

function Read-LevelZMarker {
    param([Parameter(Mandatory = $true)][string]$Path)
    $marker = @{}
    foreach ($line in Get-Content -LiteralPath $Path) {
        if ([string]::IsNullOrWhiteSpace($line)) { continue }
        $separator = $line.IndexOf('=')
        if ($separator -le 0) { throw "Malformed Level-Z marker line." }
        $key = $line.Substring(0, $separator).Trim()
        $value = $line.Substring($separator + 1).Trim()
        if ($marker.ContainsKey($key)) { throw "Duplicate Level-Z marker key: $key" }
        $marker[$key] = $value
    }
    return $marker
}

function Require-LevelZValue {
    param(
        [Parameter(Mandatory = $true)]$Marker,
        [Parameter(Mandatory = $true)][string]$Key,
        [Parameter(Mandatory = $true)][string]$Expected
    )
    if (-not $Marker.ContainsKey($Key)) { throw "Level-Z marker is missing '$Key'." }
    if (-not [string]::Equals([string]$Marker[$Key], $Expected, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Level-Z marker '$Key' expected '$Expected' but was '$($Marker[$Key])'."
    }
}

function Require-LevelZNumber {
    param(
        [Parameter(Mandatory = $true)]$Marker,
        [Parameter(Mandatory = $true)][string]$Key,
        [Parameter(Mandatory = $true)][double]$Expected
    )
    if (-not $Marker.ContainsKey($Key)) { throw "Level-Z marker is missing '$Key'." }
    [double]$value = 0
    if (-not [double]::TryParse([string]$Marker[$Key], [Globalization.NumberStyles]::Float, [Globalization.CultureInfo]::InvariantCulture, [ref]$value)) {
        throw "Level-Z marker '$Key' is not an invariant number."
    }
    if ([Math]::Abs($value - $Expected) -gt 0.000001) {
        throw "Level-Z marker '$Key' expected '$Expected' but was '$value'."
    }
}

function Restore-LevelZEnvironment {
    param([Parameter(Mandatory = $true)][string]$Name, [AllowNull()][string]$Value)
    if ($null -eq $Value) { Remove-Item -LiteralPath ("Env:" + $Name) -ErrorAction SilentlyContinue }
    else { Set-Item -LiteralPath ("Env:" + $Name) -Value $Value }
}

function Stop-LevelZProcess {
    param([AllowNull()][Diagnostics.Process]$Process)
    if ($null -eq $Process) { return }
    $Process.Refresh()
    if (-not $Process.HasExited) {
        Stop-Process -Id $Process.Id -Force -ErrorAction SilentlyContinue
        $Process.WaitForExit(10000) | Out-Null
        $Process.Refresh()
    }
    if (-not $Process.HasExited) { throw "Launched BricsCAD Level-Z process did not exit." }
}

if ([Environment]::OSVersion.Platform -ne [PlatformID]::Win32NT) { throw "Level-Z runtime qualification requires Windows." }
if (-not [Environment]::UserInteractive) { throw "Level-Z runtime qualification requires an interactive Windows session." }
if (-not $ConfirmDisposableCopy) { throw "Pass -ConfirmDisposableCopy only for a disposable synthetic drawing copy." }
if ([string]::IsNullOrWhiteSpace($Profile)) { throw "Level-Z runtime qualification requires an initialized BricsCAD profile." }

$BricsCadDir = [IO.Path]::GetFullPath($BricsCadDir)
$PluginDll = [IO.Path]::GetFullPath($PluginDll)
$DrawingCopy = [IO.Path]::GetFullPath($DrawingCopy)
$ArtifactDir = [IO.Path]::GetFullPath($ArtifactDir)
if (-not [IO.Path]::GetFileName($DrawingCopy).EndsWith(".level-z-probe-copy.dwg", [StringComparison]::OrdinalIgnoreCase)) {
    throw "DrawingCopy must use the guarded '*.level-z-probe-copy.dwg' suffix."
}

$bricscadExe = Join-Path $BricsCadDir "bricscad.exe"
$coreDll = Join-Path (Split-Path -Parent $PluginDll) "QS3D.Core.dll"
foreach ($required in @($bricscadExe, $PluginDll, $coreDll, $DrawingCopy)) {
    if (-not (Test-Path -LiteralPath $required -PathType Leaf)) { throw "Required Level-Z runtime input is missing." }
}
if (@(Get-Process -Name "bricscad" -ErrorAction SilentlyContinue).Count -gt 0) {
    throw "Close existing BricsCAD processes before starting the isolated Level-Z runtime probe."
}

$projectSidecar = [IO.Path]::ChangeExtension($DrawingCopy, ".qsdb")
if ((Test-Path -LiteralPath $projectSidecar) -or (Test-Path -LiteralPath ($projectSidecar + ".bak"))) {
    throw "The disposable Level-Z drawing copy must not have a pre-existing QS3D sidecar."
}

if (Test-Path -LiteralPath $ArtifactDir) {
    if (@(Get-ChildItem -LiteralPath $ArtifactDir -Force).Count -ne 0) { throw "ArtifactDir must be empty." }
}
else { New-Item -ItemType Directory -Path $ArtifactDir | Out-Null }

$resultPath = Join-Path $ArtifactDir "level-z-runtime-result.txt"
$scriptPath = Join-Path $ArtifactDir "level-z-runtime.scr"
$metadataPath = Join-Path $ArtifactDir "level-z-runtime-metadata.json"
$drawingHashBefore = (Get-FileHash -LiteralPath $DrawingCopy -Algorithm SHA256).Hash.ToUpperInvariant()
$pluginHash = (Get-FileHash -LiteralPath $PluginDll -Algorithm SHA256).Hash.ToUpperInvariant()
$nonce = [Guid]::NewGuid().ToString("N")
$oldResult = [Environment]::GetEnvironmentVariable("QS3D_LEVEL_Z_RESULT", "Process")
$oldNonce = [Environment]::GetEnvironmentVariable("QS3D_LEVEL_Z_NONCE", "Process")
$process = $null
$proxyInformationDialogsDismissed = 0
$startedAt = Get-Date
$passed = $false

try {
    $env:QS3D_LEVEL_Z_RESULT = $resultPath
    $env:QS3D_LEVEL_Z_NONCE = $nonce
    $script = @(
        "FILEDIA", "0",
        "CMDECHO", "1",
        "TILEMODE", "1",
        "INSUNITS", "4",
        "UCS", "W",
        "NETLOAD", ('"' + $PluginDll + '"'),
        "QS3DLEVELZPROBE"
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
        if ($process.HasExited) { throw "BricsCAD exited before the Level-Z marker. ExitCode=$($process.ExitCode)" }
        Start-Sleep -Milliseconds 500
    }
    if (-not (Test-Path -LiteralPath $resultPath -PathType Leaf)) {
        throw "Timed out waiting for QS3DLEVELZPROBE after $StartupTimeoutSeconds seconds."
    }

    $marker = Read-LevelZMarker -Path $resultPath
    Require-LevelZValue -Marker $marker -Key "status" -Expected "PASS"
    Require-LevelZValue -Marker $marker -Key "command" -Expected "QS3DLEVELZPROBE"
    Require-LevelZValue -Marker $marker -Key "process" -Expected "bricscad"
    Require-LevelZValue -Marker $marker -Key "nonce" -Expected $nonce
    Require-LevelZValue -Marker $marker -Key "schema" -Expected "QS3D_LEVEL_Z_RUNTIME_V1"
    Require-LevelZValue -Marker $marker -Key "is_64bit" -Expected "true"
    Require-LevelZValue -Marker $marker -Key "legacy_solid_count" -Expected "1"
    Require-LevelZNumber -Marker $marker -Key "legacy_min_z_m" -Expected 0.2
    Require-LevelZNumber -Marker $marker -Key "legacy_max_z_m" -Expected 3.2
    Require-LevelZValue -Marker $marker -Key "level_rebuild_blocked" -Expected "true"
    Require-LevelZValue -Marker $marker -Key "retained_solid_count" -Expected "1"
    Require-LevelZValue -Marker $marker -Key "ownership_unchanged" -Expected "true"
    Require-LevelZValue -Marker $marker -Key "pending_health_count" -Expected "1"
    Require-LevelZValue -Marker $marker -Key "production_level_qualified" -Expected "false"

    Stop-LevelZProcess -Process $process
    $drawingHashAfter = (Get-FileHash -LiteralPath $DrawingCopy -Algorithm SHA256).Hash.ToUpperInvariant()
    if (-not [string]::Equals($drawingHashBefore, $drawingHashAfter, [StringComparison]::Ordinal)) {
        throw "The disposable Level-Z drawing was written unexpectedly."
    }
    if ((Test-Path -LiteralPath $projectSidecar) -or (Test-Path -LiteralPath ($projectSidecar + ".bak"))) {
        throw "Level-Z runtime probe unexpectedly persisted a QS3D sidecar."
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
        proxy_information_dialogs_dismissed = $proxyInformationDialogsDismissed
        marker = $marker
        qualification_boundary = "LEGACY_Z_AND_LEVEL_FAIL_CLOSED_ONLY"
    }
    $metadata | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $metadataPath -Encoding UTF8
    $passed = $true

    Write-Host "QS3D BricsCAD V25 Level-Z boundary runtime PASS"
    Write-Host "Marker: $resultPath"
    Write-Host "Metadata: $metadataPath"
}
finally {
    Stop-LevelZProcess -Process $process
    Restore-LevelZEnvironment -Name "QS3D_LEVEL_Z_RESULT" -Value $oldResult
    Restore-LevelZEnvironment -Name "QS3D_LEVEL_Z_NONCE" -Value $oldNonce
    Remove-Item -LiteralPath $scriptPath -Force -ErrorAction SilentlyContinue
    if (Test-Path -LiteralPath $scriptPath) { throw "Level-Z runner could not remove its private NETLOAD script." }
    if (-not $passed) {
        Remove-Item -LiteralPath $metadataPath -Force -ErrorAction SilentlyContinue
    }
}
