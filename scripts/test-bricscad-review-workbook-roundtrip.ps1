param(
    [Parameter(Mandatory = $true)][ValidateSet("V25", "V26")][string]$HostMajor,
    [Parameter(Mandatory = $true)][string]$BricsCadDir,
    [Parameter(Mandatory = $true)][string]$PluginDll,
    [Parameter(Mandatory = $true)][string]$DrawingCopy,
    [Parameter(Mandatory = $true)][string]$Profile,
    [Parameter(Mandatory = $true)][string]$ArtifactDir,
    [Parameter(Mandatory = $true)][ValidatePattern('^[0-9a-fA-F]{40}$')][string]$ExpectedSourceSha,
    [Parameter(Mandatory = $true)][switch]$ConfirmDisposableCopy,
    [ValidateRange(60, 1200)][int]$StartupTimeoutSeconds = 420
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$windowInteropPath = Join-Path $PSScriptRoot "bricscad-runner-window-interop.ps1"
if (-not (Test-Path -LiteralPath $windowInteropPath -PathType Leaf)) {
    throw "QS Review runner window interop helper is missing."
}
. $windowInteropPath

function Read-Qs3dMarker {
    param([Parameter(Mandatory = $true)][string]$Path)
    $marker = @{}
    foreach ($line in Get-Content -LiteralPath $Path) {
        if ([string]::IsNullOrWhiteSpace($line)) { continue }
        $separator = $line.IndexOf('=')
        if ($separator -le 0) { throw "Malformed QS Review runtime marker line." }
        $key = $line.Substring(0, $separator).Trim()
        $value = $line.Substring($separator + 1).Trim()
        if ($marker.ContainsKey($key)) { throw "Duplicate QS Review runtime marker key: $key" }
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
    if (-not $Marker.ContainsKey($Key)) { throw "QS Review marker is missing '$Key'." }
    if (-not [string]::Equals([string]$Marker[$Key], $Expected, [StringComparison]::OrdinalIgnoreCase)) {
        throw "QS Review marker '$Key' expected '$Expected' but was '$($Marker[$Key])'."
    }
}

function Require-Qs3dInt {
    param(
        [Parameter(Mandatory = $true)]$Marker,
        [Parameter(Mandatory = $true)][string]$Key,
        [Parameter(Mandatory = $true)][int]$Expected
    )
    if (-not $Marker.ContainsKey($Key)) { throw "QS Review marker is missing '$Key'." }
    [int]$actual = 0
    if (-not [int]::TryParse(
        [string]$Marker[$Key],
        [Globalization.NumberStyles]::None,
        [Globalization.CultureInfo]::InvariantCulture,
        [ref]$actual)) {
        throw "QS Review marker '$Key' is not an invariant integer."
    }
    if ($actual -ne $Expected) { throw "QS Review marker '$Key' expected $Expected but was $actual." }
}

function Restore-EnvironmentValue {
    param([Parameter(Mandatory = $true)][string]$Name, [AllowNull()][string]$Value)
    if ($null -eq $Value) { Remove-Item -LiteralPath ("Env:" + $Name) -ErrorAction SilentlyContinue }
    else { Set-Item -LiteralPath ("Env:" + $Name) -Value $Value }
}

function Stop-Qs3dLaunchedProcess {
    param([AllowNull()][Diagnostics.Process]$Process)
    if ($null -eq $Process) { return }
    try {
        $Process.Refresh()
        if (-not $Process.HasExited) {
            Stop-Process -Id $Process.Id -Force -ErrorAction Stop
            $Process.WaitForExit(10000) | Out-Null
            $Process.Refresh()
        }
        if (-not $Process.HasExited) { throw "Launched BricsCAD process did not exit." }
    }
    catch [InvalidOperationException] { }
}

function Read-Qs3dWorkbookSheets {
    param([Parameter(Mandatory = $true)][string]$Path)
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $archive = [IO.Compression.ZipFile]::OpenRead($Path)
    try {
        $entry = $archive.GetEntry("xl/workbook.xml")
        if ($null -eq $entry) { throw "QS Review workbook is missing xl/workbook.xml." }
        $reader = [IO.StreamReader]::new($entry.Open(), [Text.Encoding]::UTF8, $true)
        try { [xml]$workbook = $reader.ReadToEnd() }
        finally { $reader.Dispose() }
        return @($workbook.workbook.sheets.sheet | ForEach-Object { [string]$_.name })
    }
    finally { $archive.Dispose() }
}

if ([Environment]::OSVersion.Platform -ne [PlatformID]::Win32NT) {
    throw "QS Review runtime qualification requires Windows."
}
if (-not [Environment]::UserInteractive) {
    throw "QS Review runtime qualification requires an interactive Windows session."
}
if (-not $ConfirmDisposableCopy) {
    throw "Pass -ConfirmDisposableCopy only for a disposable drawing copy."
}
if ([string]::IsNullOrWhiteSpace($Profile)) {
    throw "QS Review runtime qualification requires a BricsCAD profile."
}

$repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot ".."))
$BricsCadDir = [IO.Path]::GetFullPath($BricsCadDir)
$PluginDll = [IO.Path]::GetFullPath($PluginDll)
$DrawingCopy = [IO.Path]::GetFullPath($DrawingCopy)
$ArtifactDir = [IO.Path]::GetFullPath($ArtifactDir)
$ExpectedSourceSha = $ExpectedSourceSha.ToLowerInvariant()
if (-not [IO.Path]::GetFileName($DrawingCopy).EndsWith(".review-probe-copy.dwg", [StringComparison]::OrdinalIgnoreCase)) {
    throw "DrawingCopy must use the guarded '*.review-probe-copy.dwg' suffix."
}

$framework = if ($HostMajor -eq "V25") { "net48" } else { "net8.0-windows" }
$expectedAssembly = "QS3D.BricsCAD." + $HostMajor
$expectedPlugin = [IO.Path]::GetFullPath((Join-Path $repoRoot (
    "src\" + $expectedAssembly + "\bin\x64\Release\" + $framework + "\" + $expectedAssembly + ".dll")))
if (-not [string]::Equals($PluginDll, $expectedPlugin, [StringComparison]::OrdinalIgnoreCase)) {
    throw "PluginDll must be the exact repository x64 Release $HostMajor output."
}

$bricscadExe = Join-Path $BricsCadDir "bricscad.exe"
$coreDll = Join-Path (Split-Path -Parent $PluginDll) "QS3D.Core.dll"
foreach ($required in @($bricscadExe, $PluginDll, $coreDll, $DrawingCopy)) {
    if (-not (Test-Path -LiteralPath $required -PathType Leaf)) {
        throw "Required QS Review runtime input is missing: $required"
    }
}

$git = Get-Command git -CommandType Application -ErrorAction Stop | Select-Object -First 1
$gitHead = (& $git.Source -C $repoRoot rev-parse HEAD).Trim().ToLowerInvariant()
if ($LASTEXITCODE -ne 0 -or $gitHead -notmatch '^[0-9a-f]{40}$') {
    throw "Cannot resolve the exact QS Review Git candidate SHA."
}
if (-not [string]::Equals($gitHead, $ExpectedSourceSha, [StringComparison]::Ordinal)) {
    throw "ExpectedSourceSha does not match HEAD. Expected=$ExpectedSourceSha HEAD=$gitHead"
}
$gitStatus = @(& $git.Source -C $repoRoot status --porcelain --untracked-files=normal)
if ($LASTEXITCODE -ne 0 -or $gitStatus.Count -ne 0) {
    throw "QS Review runtime qualification requires a clean exact-SHA worktree."
}
$upstreamSha = (& $git.Source -C $repoRoot rev-parse '@{u}').Trim().ToLowerInvariant()
if ($LASTEXITCODE -ne 0 -or -not [string]::Equals($upstreamSha, $ExpectedSourceSha, [StringComparison]::Ordinal)) {
    throw "QS Review runtime qualification requires the exact candidate SHA to be pushed to its upstream branch."
}
Assert-Qs3dExactSourceIdentity -RepoRoot $repoRoot -PluginDll $PluginDll -ExpectedSourceSha $ExpectedSourceSha

if (@(Get-Qs3dExactBricsCadProcesses -ExpectedExecutable $bricscadExe).Count -gt 0) {
    throw "Close existing $HostMajor BricsCAD processes before starting the isolated QS Review probe."
}

$projectSidecar = [IO.Path]::ChangeExtension($DrawingCopy, ".qsdb")
$drawingBackup = [IO.Path]::ChangeExtension($DrawingCopy, ".bak")
$drawingLocks = @(
    [IO.Path]::ChangeExtension($DrawingCopy, ".dwl"),
    [IO.Path]::ChangeExtension($DrawingCopy, ".dwl2"))
foreach ($forbidden in @($projectSidecar, ($projectSidecar + ".bak"), $drawingBackup) + $drawingLocks) {
    if (Test-Path -LiteralPath $forbidden) {
        throw "Disposable QS Review drawing has a pre-existing sidecar, backup or lock file."
    }
}
if (Test-Path -LiteralPath $ArtifactDir) {
    if (@(Get-ChildItem -LiteralPath $ArtifactDir -Force).Count -ne 0) {
        throw "QS Review ArtifactDir must be empty."
    }
}
else { New-Item -ItemType Directory -Path $ArtifactDir | Out-Null }

$resultPath = Join-Path $ArtifactDir "review-workbook-roundtrip-result.txt"
$workbookPath = Join-Path $ArtifactDir "review-workbook-roundtrip.xlsx"
$scriptPath = Join-Path $ArtifactDir "review-workbook-roundtrip.scr"
$metadataPath = Join-Path $ArtifactDir "review-workbook-roundtrip-metadata.json"
$drawingHashBefore = (Get-FileHash -LiteralPath $DrawingCopy -Algorithm SHA256).Hash.ToUpperInvariant()
$pluginHash = (Get-FileHash -LiteralPath $PluginDll -Algorithm SHA256).Hash.ToUpperInvariant()
$nonce = [Guid]::NewGuid().ToString("N")
$oldResult = [Environment]::GetEnvironmentVariable("QS3D_REVIEW_ROUNDTRIP_RESULT", "Process")
$oldWorkbook = [Environment]::GetEnvironmentVariable("QS3D_REVIEW_ROUNDTRIP_WORKBOOK", "Process")
$oldNonce = [Environment]::GetEnvironmentVariable("QS3D_REVIEW_ROUNDTRIP_NONCE", "Process")
$process = $null
$proxyDialogsDismissed = 0
$startedAt = Get-Date

try {
    $env:QS3D_REVIEW_ROUNDTRIP_RESULT = $resultPath
    $env:QS3D_REVIEW_ROUNDTRIP_WORKBOOK = $workbookPath
    $env:QS3D_REVIEW_ROUNDTRIP_NONCE = $nonce
    $script = @(
        "FILEDIA", "0",
        "CMDECHO", "1",
        "TILEMODE", "1",
        "UCS", "W",
        "NETLOAD", ('"' + $PluginDll + '"'),
        "QS3DREVIEWROUNDTRIPPROBE"
    )
    Set-Content -LiteralPath $scriptPath -Value $script -Encoding ASCII

    $argumentParts = New-Object System.Collections.Generic.List[string]
    $argumentParts.Add('"' + $DrawingCopy + '"')
    $argumentParts.Add('/P')
    $argumentParts.Add('"' + $Profile + '"')
    $argumentParts.Add('/B')
    $argumentParts.Add('"' + $scriptPath + '"')
    $process = Start-Process -FilePath $bricscadExe -ArgumentList ([string]::Join(' ', $argumentParts)) `
        -PassThru -WindowStyle Hidden -WorkingDirectory $ArtifactDir

    $deadline = (Get-Date).AddSeconds($StartupTimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        if (Test-Path -LiteralPath $resultPath -PathType Leaf) { break }
        $proxyDialogsDismissed += Close-Qs3dProxyInformationDialog -Process $process
        $process.Refresh()
        if ($process.HasExited) {
            throw "BricsCAD $HostMajor exited before QS3DREVIEWROUNDTRIPPROBE created its marker. ExitCode=$($process.ExitCode)"
        }
        Start-Sleep -Milliseconds 500
    }
    if (-not (Test-Path -LiteralPath $resultPath -PathType Leaf)) {
        throw "Timed out waiting for BricsCAD $HostMajor QS Review round-trip."
    }

    $marker = Read-Qs3dMarker -Path $resultPath
    Require-Qs3dValue $marker "status" "PASS"
    Require-Qs3dValue $marker "command" "QS3DREVIEWROUNDTRIPPROBE"
    Require-Qs3dValue $marker "schema" "QS3D_REVIEW_HOST_ROUNDTRIP_V1"
    Require-Qs3dValue $marker "nonce" $nonce
    Require-Qs3dValue $marker "process" "bricscad"
    Require-Qs3dValue $marker "plugin_assembly" $expectedAssembly
    Require-Qs3dValue $marker "is_64bit" "true"
    foreach ($key in @(
        "wrong_fingerprint_refused", "wrong_revision_refused", "stale_handle_refused",
        "partial_resolution_refused", "all_targets_resolved_before_selection",
        "production_export_service", "production_locate_service")) {
        Require-Qs3dValue $marker $key "true"
    }
    Require-Qs3dInt $marker "sheet_count" 6
    Require-Qs3dInt $marker "quantity_detail_count" 3
    Require-Qs3dInt $marker "quantity_summary_count" 3
    Require-Qs3dInt $marker "clash_count" 1
    Require-Qs3dInt $marker "duplicate_count" 1
    Require-Qs3dInt $marker "qto_located_count" 1
    Require-Qs3dInt $marker "clash_located_count" 2
    Require-Qs3dInt $marker "duplicate_located_count" 2
    Require-Qs3dInt $marker "negative_attempt_count" 4
    Require-Qs3dInt $marker "negative_refusal_count" 4
    Require-Qs3dInt $marker "negative_pickfirst_preserved_count" 4
    Require-Qs3dInt $marker "negative_semantic_unchanged_count" 4

    if (-not (Test-Path -LiteralPath $workbookPath -PathType Leaf)) {
        throw "BricsCAD $HostMajor did not create the six-sheet QS Review workbook."
    }
    $expectedSheets = @(
        "01_TONG_HOP", "02_CHI_TIET_QTO", "03_CLASHES",
        "04_DUPLICATES", "05_RULES", "06_MODEL_INFO")
    $actualSheets = @(Read-Qs3dWorkbookSheets -Path $workbookPath)
    if ($actualSheets.Count -ne $expectedSheets.Count -or
        -not [string]::Equals([string]::Join("|", $actualSheets), [string]::Join("|", $expectedSheets), [StringComparison]::Ordinal)) {
        throw "BricsCAD $HostMajor workbook does not have the exact canonical six-sheet order."
    }

    Stop-Qs3dLaunchedProcess -Process $process
    if (-not (Wait-Qs3dNoExactBricsCadProcesses -ExpectedExecutable $bricscadExe -TimeoutSeconds 20)) {
        throw "BricsCAD $HostMajor process residue remains after the QS Review probe."
    }
    $drawingHashAfter = (Get-FileHash -LiteralPath $DrawingCopy -Algorithm SHA256).Hash.ToUpperInvariant()
    if (-not [string]::Equals($drawingHashBefore, $drawingHashAfter, [StringComparison]::Ordinal)) {
        throw "Disposable QS Review DWG changed on disk."
    }
    if ((Test-Path -LiteralPath $projectSidecar) -or (Test-Path -LiteralPath ($projectSidecar + ".bak"))) {
        throw "QS Review probe unexpectedly persisted a QSDB sidecar."
    }

    $metadata = [ordered]@{
        status = "PASS"
        host_major = $HostMajor
        source_sha = $ExpectedSourceSha
        started_at = $startedAt.ToUniversalTime().ToString("O")
        completed_at = (Get-Date).ToUniversalTime().ToString("O")
        bricscad_file_version = (Get-Item -LiteralPath $bricscadExe).VersionInfo.FileVersion
        bricscad_product_version = (Get-Item -LiteralPath $bricscadExe).VersionInfo.ProductVersion
        bricscad_exe_sha256 = (Get-FileHash -LiteralPath $bricscadExe -Algorithm SHA256).Hash.ToUpperInvariant()
        plugin_assembly = $expectedAssembly
        plugin_sha256 = $pluginHash
        drawing_copy_sha256_before = $drawingHashBefore
        drawing_copy_sha256_after = $drawingHashAfter
        workbook_sha256 = (Get-FileHash -LiteralPath $workbookPath -Algorithm SHA256).Hash.ToUpperInvariant()
        workbook_sheets = $actualSheets
        proxy_information_dialogs_dismissed = $proxyDialogsDismissed
        marker = $marker
    }
    $metadata | ConvertTo-Json -Depth 7 | Set-Content -LiteralPath $metadataPath -Encoding UTF8

    Write-Host "QS3D BricsCAD $HostMajor six-sheet Review + Excel-to-Model Locate PASS"
    Write-Host "Exact source SHA: $ExpectedSourceSha"
    Write-Host "Marker: $resultPath"
    Write-Host "Workbook: $workbookPath"
    Write-Host "Metadata: $metadataPath"
}
finally {
    Stop-Qs3dLaunchedProcess -Process $process
    Restore-EnvironmentValue -Name "QS3D_REVIEW_ROUNDTRIP_RESULT" -Value $oldResult
    Restore-EnvironmentValue -Name "QS3D_REVIEW_ROUNDTRIP_WORKBOOK" -Value $oldWorkbook
    Restore-EnvironmentValue -Name "QS3D_REVIEW_ROUNDTRIP_NONCE" -Value $oldNonce
}
