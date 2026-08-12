param(
    [Parameter(Mandatory = $true)][string]$BricsCadDir,
    [Parameter(Mandatory = $true)][string]$PluginDll,
    [Parameter(Mandatory = $true)][string]$DrawingCopy,
    [Parameter(Mandatory = $true)][string]$Profile,
    [Parameter(Mandatory = $true)][string]$ArtifactDir,
    [Parameter(Mandatory = $true)][switch]$ConfirmDisposableCopy,
    [ValidateRange(30, 900)][int]$StartupTimeoutSeconds = 240
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$windowInteropPath = Join-Path $PSScriptRoot "bricscad-runner-window-interop.ps1"
if (-not (Test-Path -LiteralPath $windowInteropPath -PathType Leaf)) {
    throw "Plan-to-3D runner window interop helper is missing: $windowInteropPath"
}
. $windowInteropPath

function Read-Qs3dMarker {
    param([Parameter(Mandatory = $true)][string]$Path)
    $marker = @{}
    foreach ($line in Get-Content -LiteralPath $Path) {
        if ([string]::IsNullOrWhiteSpace($line)) { continue }
        $separator = $line.IndexOf('=')
        if ($separator -le 0) { throw "Malformed Plan-to-3D marker line: $line" }
        $key = $line.Substring(0, $separator).Trim()
        $value = $line.Substring($separator + 1).Trim()
        if ($marker.ContainsKey($key)) { throw "Duplicate Plan-to-3D marker key: $key" }
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
    if (-not $Marker.ContainsKey($Key)) { throw "Plan-to-3D marker is missing '$Key'." }
    if (-not [string]::Equals([string]$Marker[$Key], $Expected, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Plan-to-3D marker '$Key' expected '$Expected' but was '$($Marker[$Key])'."
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
        $Process.WaitForExit(10000) | Out-Null
        $Process.Refresh()
    }
    if (-not $Process.HasExited) { throw "Launched BricsCAD Plan-to-3D process did not exit." }
}

if ([Environment]::OSVersion.Platform -ne [PlatformID]::Win32NT) { throw "Plan-to-3D runtime qualification requires Windows." }
if (-not [Environment]::UserInteractive) { throw "Plan-to-3D runtime qualification requires an interactive Windows session." }
if (-not $ConfirmDisposableCopy) { throw "Pass -ConfirmDisposableCopy only for a disposable synthetic drawing copy." }
if ([string]::IsNullOrWhiteSpace($Profile)) { throw "Plan-to-3D runtime qualification requires an initialized BricsCAD profile." }

$repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot ".."))
$BricsCadDir = [IO.Path]::GetFullPath($BricsCadDir)
$PluginDll = [IO.Path]::GetFullPath($PluginDll)
$DrawingCopy = [IO.Path]::GetFullPath($DrawingCopy)
$ArtifactDir = [IO.Path]::GetFullPath($ArtifactDir)
if (-not [IO.Path]::GetFileName($DrawingCopy).EndsWith(".plan-to-3d-probe-copy.dwg", [StringComparison]::OrdinalIgnoreCase)) {
    throw "DrawingCopy must use the guarded '*.plan-to-3d-probe-copy.dwg' suffix."
}

$bricscadExe = Join-Path $BricsCadDir "bricscad.exe"
$coreDll = Join-Path (Split-Path -Parent $PluginDll) "QS3D.Core.dll"
foreach ($required in @($bricscadExe, $PluginDll, $coreDll, $DrawingCopy)) {
    if (-not (Test-Path -LiteralPath $required -PathType Leaf)) { throw "Required Plan-to-3D runtime input is missing: $required" }
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
if ($gitStatus.Count -ne 0) { throw "Plan-to-3D runtime qualification requires a clean exact-SHA worktree." }
if (@(Get-Process -Name "bricscad" -ErrorAction SilentlyContinue).Count -gt 0) {
    throw "Close existing BricsCAD processes before starting the isolated Plan-to-3D runtime probe."
}

$projectSidecar = [IO.Path]::ChangeExtension($DrawingCopy, ".qsdb")
if ((Test-Path -LiteralPath $projectSidecar) -or (Test-Path -LiteralPath ($projectSidecar + ".bak"))) {
    throw "The disposable Plan-to-3D drawing copy must not have a pre-existing QS3D sidecar."
}

if (Test-Path -LiteralPath $ArtifactDir) {
    if (@(Get-ChildItem -LiteralPath $ArtifactDir -Force).Count -ne 0) { throw "ArtifactDir must be empty." }
}
else { New-Item -ItemType Directory -Path $ArtifactDir | Out-Null }
$resultPath = Join-Path $ArtifactDir "plan-to-3d-runtime-result.txt"
$scriptPath = Join-Path $ArtifactDir "plan-to-3d-runtime.scr"
$metadataPath = Join-Path $ArtifactDir "plan-to-3d-runtime-metadata.json"
foreach ($output in @($resultPath, $scriptPath, $metadataPath)) {
    if (Test-Path -LiteralPath $output) { throw "Plan-to-3D runtime output must not already exist: $output" }
}

$drawingHashBefore = (Get-FileHash -LiteralPath $DrawingCopy -Algorithm SHA256).Hash.ToUpperInvariant()
$pluginHash = (Get-FileHash -LiteralPath $PluginDll -Algorithm SHA256).Hash.ToUpperInvariant()
$nonce = [Guid]::NewGuid().ToString("N")
$oldResult = [Environment]::GetEnvironmentVariable("QS3D_PLAN_TO_3D_RESULT", "Process")
$oldNonce = [Environment]::GetEnvironmentVariable("QS3D_PLAN_TO_3D_NONCE", "Process")
$process = $null
$proxyInformationDialogsDismissed = 0
$startedAt = Get-Date

try {
    $env:QS3D_PLAN_TO_3D_RESULT = $resultPath
    $env:QS3D_PLAN_TO_3D_NONCE = $nonce
    $script = @(
        "FILEDIA", "0",
        "CMDECHO", "1",
        "TILEMODE", "1",
        "INSUNITS", "4",
        "UCS", "W",
        "NETLOAD", ('"' + $PluginDll + '"'),
        "QS3DPLAN2DPROBE"
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
        if ($process.HasExited) { throw "BricsCAD exited before the Plan-to-3D marker. ExitCode=$($process.ExitCode)" }
        Start-Sleep -Milliseconds 500
    }
    if (-not (Test-Path -LiteralPath $resultPath -PathType Leaf)) {
        throw "Timed out waiting for QS3DPLAN2DPROBE after $StartupTimeoutSeconds seconds."
    }

    $marker = Read-Qs3dMarker -Path $resultPath
    Require-Qs3dValue -Marker $marker -Key "status" -Expected "PASS"
    Require-Qs3dValue -Marker $marker -Key "command" -Expected "QS3DPLAN2DPROBE"
    Require-Qs3dValue -Marker $marker -Key "process" -Expected "bricscad"
    Require-Qs3dValue -Marker $marker -Key "nonce" -Expected $nonce
    Require-Qs3dValue -Marker $marker -Key "schema" -Expected "QS3D_PLAN_TO_3D_RUNTIME_V1"
    Require-Qs3dValue -Marker $marker -Key "is_64bit" -Expected "true"
    Require-Qs3dValue -Marker $marker -Key "native_unit" -Expected "Millimeter"
    Require-Qs3dValue -Marker $marker -Key "source_line_count" -Expected "2"
    Require-Qs3dValue -Marker $marker -Key "semantic_wall_count" -Expected "2"
    Require-Qs3dValue -Marker $marker -Key "generated_solid_count" -Expected "2"
    Require-Qs3dValue -Marker $marker -Key "fallback_thickness_m" -Expected "0.2"
    Require-Qs3dValue -Marker $marker -Key "fallback_height_m" -Expected "3"
    Require-Qs3dValue -Marker $marker -Key "fallback_bottom_offset_m" -Expected "0"
    Require-Qs3dValue -Marker $marker -Key "source_geometry_retained" -Expected "true"
    Require-Qs3dValue -Marker $marker -Key "ownership_sets_disjoint" -Expected "true"
    Require-Qs3dValue -Marker $marker -Key "native_bounds_verified" -Expected "true"
    Require-Qs3dValue -Marker $marker -Key "core_health_error_count" -Expected "0"
    Require-Qs3dValue -Marker $marker -Key "runtime_health_error_count" -Expected "0"
    Require-Qs3dValue -Marker $marker -Key "qualification_boundary" -Expected "P01_QUICK_POSITIVE_ONLY"
    Require-Qs3dValue -Marker $marker -Key "production_local014_qualified" -Expected "false"

    Stop-Qs3dLaunchedProcess -Process $process
    if (Test-Path -LiteralPath $scriptPath) {
        Remove-Item -LiteralPath $scriptPath -Force -ErrorAction Stop
    }
    if (Test-Path -LiteralPath $scriptPath) { throw "Plan-to-3D runtime script cleanup failed." }
    $drawingHashAfter = (Get-FileHash -LiteralPath $DrawingCopy -Algorithm SHA256).Hash.ToUpperInvariant()
    if (-not [string]::Equals($drawingHashBefore, $drawingHashAfter, [StringComparison]::Ordinal)) {
        throw "The disposable Plan-to-3D drawing was written unexpectedly."
    }
    if ((Test-Path -LiteralPath $projectSidecar) -or (Test-Path -LiteralPath ($projectSidecar + ".bak"))) {
        throw "Plan-to-3D runtime probe unexpectedly persisted a QS3D sidecar."
    }

    $metadata = [ordered]@{
        status = "PASS"
        git_sha = $gitHead
        started_at = $startedAt.ToUniversalTime().ToString("O")
        completed_at = (Get-Date).ToUniversalTime().ToString("O")
        profile = $Profile
        bricscad_file_version = (Get-Item -LiteralPath $bricscadExe).VersionInfo.FileVersion
        plugin_sha256 = $pluginHash
        drawing_copy_sha256_before = $drawingHashBefore
        drawing_copy_sha256_after = $drawingHashAfter
        process_cleanup_verified = $true
        script_cleanup_verified = $true
        proxy_information_dialogs_dismissed = $proxyInformationDialogsDismissed
        marker = $marker
    }
    $metadata | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $metadataPath -Encoding UTF8

    Write-Host "QS3D BricsCAD V25 Plan-to-3D P01 runtime PASS"
    Write-Host "Sources: 2; semantic walls: 2; generated solids: 2"
    Write-Host "Marker: $resultPath"
    Write-Host "Metadata: $metadataPath"
}
finally {
    try {
        Stop-Qs3dLaunchedProcess -Process $process
        if (Test-Path -LiteralPath $scriptPath) {
            Remove-Item -LiteralPath $scriptPath -Force -ErrorAction Stop
        }
        if (Test-Path -LiteralPath $scriptPath) { throw "Plan-to-3D runtime script cleanup failed." }
    }
    finally {
        Restore-EnvironmentValue -Name "QS3D_PLAN_TO_3D_RESULT" -Value $oldResult
        Restore-EnvironmentValue -Name "QS3D_PLAN_TO_3D_NONCE" -Value $oldNonce
    }
}
