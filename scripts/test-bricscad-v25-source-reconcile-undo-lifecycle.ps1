param(
    [Parameter(Mandatory = $true)][string]$BricsCadDir,
    [Parameter(Mandatory = $true)][string]$PluginDll,
    [Parameter(Mandatory = $true)][string]$FixtureDwg,
    [Parameter(Mandatory = $true)][string]$Profile,
    [Parameter(Mandatory = $true)][string]$ArtifactDir,
    [Parameter(Mandatory = $true)][switch]$ConfirmDisposableCopies,
    [ValidateRange(120, 1200)][int]$TimeoutSeconds = 480
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$windowInteropPath = Join-Path $PSScriptRoot "bricscad-runner-window-interop.ps1"
if (-not (Test-Path -LiteralPath $windowInteropPath -PathType Leaf)) {
    throw "LOCAL-004 Undo lifecycle window helper is missing."
}
. $windowInteropPath

function Read-Qs3dMarker {
    param([Parameter(Mandatory = $true)][string]$Path)
    $marker = @{}
    foreach ($line in Get-Content -LiteralPath $Path) {
        if ([string]::IsNullOrWhiteSpace($line)) { continue }
        $separator = $line.IndexOf('=')
        if ($separator -le 0) { throw "Malformed LOCAL-004 Undo lifecycle marker line." }
        $key = $line.Substring(0, $separator).Trim()
        $value = $line.Substring($separator + 1).Trim()
        if ($marker.ContainsKey($key)) { throw "Duplicate LOCAL-004 Undo lifecycle marker key." }
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
    if (-not $Marker.ContainsKey($Key)) { throw "LOCAL-004 Undo lifecycle marker is missing a required field." }
    if (-not [string]::Equals([string]$Marker[$Key], $Expected, [StringComparison]::OrdinalIgnoreCase)) {
        throw "LOCAL-004 Undo lifecycle marker field did not match its required value."
    }
}

function Require-Qs3dAllowedValue {
    param(
        [Parameter(Mandatory = $true)]$Marker,
        [Parameter(Mandatory = $true)][string]$Key,
        [Parameter(Mandatory = $true)][string[]]$Allowed
    )
    if (-not $Marker.ContainsKey($Key)) { throw "LOCAL-004 Undo lifecycle marker is missing a classification." }
    if (-not ($Allowed -contains [string]$Marker[$Key])) {
        throw "LOCAL-004 Undo lifecycle marker contains an invalid classification."
    }
}

function Require-Qs3dPassMarker {
    param(
        [Parameter(Mandatory = $true)]$Marker,
        [Parameter(Mandatory = $true)][string]$Nonce,
        [Parameter(Mandatory = $true)][string]$Variant
    )
    $keys = @(
        "status", "schema", "qualification_boundary", "production_local004_qualified",
        "nonce", "variant", "db_recording_entry", "db_recording_after_enable",
        "db_recording_after_start", "existing_after_undo", "topology_after_undo"
    )
    if (@($Marker.Keys).Count -ne $keys.Count) { throw "LOCAL-004 Undo lifecycle PASS marker has unexpected fields." }
    foreach ($key in $keys) {
        if (-not $Marker.ContainsKey($key)) { throw "LOCAL-004 Undo lifecycle PASS marker is incomplete." }
    }
    Require-Qs3dValue $Marker "status" "PASS"
    Require-Qs3dValue $Marker "schema" "QS3D_SOURCE_UNDO_LIFECYCLE_V1"
    Require-Qs3dValue $Marker "qualification_boundary" "LOCAL_004_DIAGNOSTIC_ONLY"
    Require-Qs3dValue $Marker "production_local004_qualified" "false"
    Require-Qs3dValue $Marker "nonce" $Nonce
    Require-Qs3dValue $Marker "variant" $Variant
    Require-Qs3dAllowedValue $Marker "db_recording_entry" @("ON", "OFF")
    Require-Qs3dAllowedValue $Marker "db_recording_after_enable" @("ON", "OFF", "NOT_RUN")
    Require-Qs3dAllowedValue $Marker "db_recording_after_start" @("ON", "OFF", "NOT_RUN")
    Require-Qs3dAllowedValue $Marker "existing_after_undo" @("BEFORE", "AFTER", "OTHER_OR_INVALID")
    Require-Qs3dAllowedValue $Marker "topology_after_undo" @("UNDONE", "PRESENT", "OTHER_OR_INVALID")
}

function Restore-Qs3dEnvironmentValue {
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
    if (-not $Process.HasExited) { throw "LOCAL-004 Undo lifecycle process did not exit." }
}

function Find-Qs3dHandoffProcess {
    param(
        [Parameter(Mandatory = $true)][int]$LauncherId,
        [Parameter(Mandatory = $true)][string]$ExpectedExecutable
    )
    $records = @(Get-CimInstance -ClassName Win32_Process -Filter ("Name = 'bricscad.exe' AND ParentProcessId = " + $LauncherId))
    $matches = New-Object System.Collections.Generic.List[Diagnostics.Process]
    foreach ($record in $records) {
        $candidatePath = [string]$record.ExecutablePath
        if ([string]::IsNullOrWhiteSpace($candidatePath)) { continue }
        $candidatePath = [IO.Path]::GetFullPath($candidatePath)
        if (-not [string]::Equals($candidatePath, $ExpectedExecutable, [StringComparison]::OrdinalIgnoreCase)) { continue }
        $candidate = Get-Process -Id ([int]$record.ProcessId) -ErrorAction SilentlyContinue
        if ($null -ne $candidate) { $matches.Add($candidate) }
    }
    if ($matches.Count -gt 1) { throw "LOCAL-004 Undo lifecycle launcher handoff is ambiguous." }
    if ($matches.Count -eq 1) { return $matches[0] }
    return $null
}

function Wait-Qs3dHandoffProcess {
    param(
        [Parameter(Mandatory = $true)][int]$LauncherId,
        [Parameter(Mandatory = $true)][string]$ExpectedExecutable,
        [Parameter(Mandatory = $true)][DateTime]$Deadline
    )
    $handoffDeadline = (Get-Date).AddSeconds(30)
    if ($handoffDeadline -gt $Deadline) { $handoffDeadline = $Deadline }
    while ((Get-Date) -lt $handoffDeadline) {
        $candidate = Find-Qs3dHandoffProcess -LauncherId $LauncherId -ExpectedExecutable $ExpectedExecutable
        if ($null -ne $candidate) { return $candidate }
        Start-Sleep -Milliseconds 250
    }
    throw "LOCAL-004 Undo lifecycle launcher exited without an exact handoff."
}

function Wait-Qs3dMarker {
    param(
        [Parameter(Mandatory = $true)][ref]$Process,
        [Parameter(Mandatory = $true)][ref]$HandoffObserved,
        [Parameter(Mandatory = $true)][string]$ExpectedExecutable,
        [Parameter(Mandatory = $true)][string]$MarkerPath,
        [Parameter(Mandatory = $true)][DateTime]$Deadline
    )
    [Diagnostics.Process]$current = $Process.Value
    $launcherId = $current.Id
    $adopted = $false
    while ((Get-Date) -lt $Deadline) {
        if (Test-Path -LiteralPath $MarkerPath -PathType Leaf) { return }
        [void](Close-Qs3dProxyInformationDialog -Process $current)
        $current.Refresh()
        if ($current.HasExited) {
            if ($adopted) { throw "LOCAL-004 Undo lifecycle host exited before publishing a marker." }
            $current = Wait-Qs3dHandoffProcess -LauncherId $launcherId -ExpectedExecutable $ExpectedExecutable -Deadline $Deadline
            $Process.Value = $current
            $HandoffObserved.Value = $true
            $adopted = $true
        }
        Start-Sleep -Milliseconds 400
    }
    throw "Timed out waiting for the LOCAL-004 Undo lifecycle marker."
}

function Wait-Qs3dExit {
    param([Parameter(Mandatory = $true)][Diagnostics.Process]$Process, [Parameter(Mandatory = $true)][DateTime]$Deadline)
    while ((Get-Date) -lt $Deadline) {
        $Process.Refresh()
        if ($Process.HasExited) { return }
        [void](Close-Qs3dProxyInformationDialog -Process $Process)
        Start-Sleep -Milliseconds 400
    }
    throw "Timed out waiting for the LOCAL-004 Undo lifecycle host to exit."
}

if ([Environment]::OSVersion.Platform -ne [PlatformID]::Win32NT) { throw "LOCAL-004 Undo lifecycle matrix requires Windows." }
if (-not [Environment]::UserInteractive) { throw "LOCAL-004 Undo lifecycle matrix requires an interactive Windows session." }
if (-not $ConfirmDisposableCopies) { throw "Confirm repository-sample disposable copies explicitly." }
if ([string]::IsNullOrWhiteSpace($Profile)) { throw "LOCAL-004 Undo lifecycle matrix requires an initialized profile." }

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
    throw "PluginDll must be the exact repository V25 Release output."
}

$bricscadExe = Join-Path $BricsCadDir "bricscad.exe"
$coreDll = Join-Path (Split-Path -Parent $PluginDll) "QS3D.Core.dll"
foreach ($required in @($bricscadExe, $PluginDll, $coreDll, $FixtureDwg)) {
    if (-not (Test-Path -LiteralPath $required -PathType Leaf)) { throw "Required LOCAL-004 Undo lifecycle input is missing." }
}

$git = Get-Command git -CommandType Application -ErrorAction Stop | Select-Object -First 1
$gitHeadOutput = @(& $git.Source -C $repoRoot rev-parse HEAD 2>$null)
if ($LASTEXITCODE -ne 0 -or $gitHeadOutput.Count -ne 1) { throw "Cannot resolve the exact LOCAL-004 Undo lifecycle SHA." }
$gitHead = ([string]$gitHeadOutput[0]).Trim().ToLowerInvariant()
if ($gitHead -notmatch '^[0-9a-f]{40}$') { throw "LOCAL-004 Undo lifecycle SHA is invalid." }
$gitStatus = @(& $git.Source -C $repoRoot status --porcelain=v1 --untracked-files=all 2>$null)
if ($LASTEXITCODE -ne 0 -or $gitStatus.Count -ne 0) { throw "LOCAL-004 Undo lifecycle matrix requires a clean exact-SHA worktree." }
$expectedRevision = "+" + $gitHead
foreach ($assemblyPath in @($PluginDll, $coreDll)) {
    $productVersion = [string](Get-Item -LiteralPath $assemblyPath).VersionInfo.ProductVersion
    if (-not $productVersion.EndsWith($expectedRevision, [StringComparison]::OrdinalIgnoreCase)) {
        throw "LOCAL-004 Undo lifecycle assembly does not match the exact SHA."
    }
}
if (@(Get-Process -Name "bricscad" -ErrorAction SilentlyContinue).Count -gt 0) {
    throw "Close existing BricsCAD processes before isolated LOCAL-004 Undo lifecycle qualification."
}

if (Test-Path -LiteralPath $ArtifactDir) {
    if (@(Get-ChildItem -LiteralPath $ArtifactDir -Force).Count -ne 0) { throw "ArtifactDir must be empty." }
}
else { New-Item -ItemType Directory -Path $ArtifactDir | Out-Null }

$variants = @("OBJECT_ONLY", "DB_ENABLE_OBJECT", "DB_START_OBJECT", "DB_ENABLE_DB_START_OBJECT")
$fixtureHash = (Get-FileHash -LiteralPath $FixtureDwg -Algorithm SHA256).Hash.ToUpperInvariant()
$pluginHash = (Get-FileHash -LiteralPath $PluginDll -Algorithm SHA256).Hash.ToUpperInvariant()
$environmentNames = @(
    "QS3D_SOURCE_UNDO_MATRIX_RESULT", "QS3D_SOURCE_UNDO_MATRIX_NONCE",
    "QS3D_SOURCE_UNDO_MATRIX_VARIANT", "QS3D_SOURCE_UNDO_MATRIX_DWG"
)
$oldEnvironment = @{}
foreach ($name in $environmentNames) { $oldEnvironment[$name] = [Environment]::GetEnvironmentVariable($name, "Process") }

$results = New-Object System.Collections.Generic.List[object]
$launchedProcesses = New-Object System.Collections.Generic.List[Diagnostics.Process]
$privateScripts = New-Object System.Collections.Generic.List[string]
$disposableDrawings = New-Object System.Collections.Generic.List[string]
$privateStatePaths = New-Object System.Collections.Generic.List[string]
$handoffObserved = $false
$processCleanupVerified = $false
$scriptCleanupVerified = $false
$privateStateCleanupVerified = $false
$drawingCleanupVerified = $false
$qualificationError = $null
$cleanupError = $null

try {
    foreach ($variant in $variants) {
        if (@(Get-Process -Name "bricscad" -ErrorAction SilentlyContinue).Count -gt 0) {
            throw "A BricsCAD process appeared before a LOCAL-004 Undo lifecycle variant."
        }

        $variantRoot = Join-Path $ArtifactDir $variant.ToLowerInvariant()
        New-Item -ItemType Directory -Path $variantRoot | Out-Null
        $drawing = Join-Path $variantRoot "source-undo-lifecycle-probe-copy.dwg"
        $resultPath = Join-Path $variantRoot "source-undo-lifecycle-result.txt"
        $scriptPath = Join-Path $variantRoot "source-undo-lifecycle.private.scr"
        Copy-Item -LiteralPath $FixtureDwg -Destination $drawing -ErrorAction Stop
        if (-not [string]::Equals((Get-FileHash -LiteralPath $drawing -Algorithm SHA256).Hash, $fixtureHash, [StringComparison]::OrdinalIgnoreCase)) {
            throw "LOCAL-004 Undo lifecycle disposable copy hash mismatch."
        }
        $disposableDrawings.Add($drawing)
        $privateScripts.Add($scriptPath)

        $sidecar = [IO.Path]::ChangeExtension($drawing, ".qsdb")
        foreach ($path in @(
            $sidecar, ($sidecar + ".bak"), ($sidecar + ".lock"),
            [IO.Path]::ChangeExtension($drawing, ".dwl"),
            [IO.Path]::ChangeExtension($drawing, ".dwl2"),
            [IO.Path]::ChangeExtension($drawing, ".bak")
        )) {
            $full = [IO.Path]::GetFullPath($path)
            if (-not $full.StartsWith($variantRoot + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
                throw "LOCAL-004 Undo lifecycle private-state path escaped its variant root."
            }
            if (Test-Path -LiteralPath $full) { throw "LOCAL-004 Undo lifecycle copy has pre-existing private state." }
            $privateStatePaths.Add($full)
        }

        $nonce = [Guid]::NewGuid().ToString("N")
        $env:QS3D_SOURCE_UNDO_MATRIX_RESULT = $resultPath
        $env:QS3D_SOURCE_UNDO_MATRIX_NONCE = $nonce
        $env:QS3D_SOURCE_UNDO_MATRIX_VARIANT = $variant
        $env:QS3D_SOURCE_UNDO_MATRIX_DWG = $drawing

        $script = @(
            "FILEDIA", "0", "CMDECHO", "1", "TILEMODE", "1",
            "NETLOAD", ('"' + $PluginDll + '"'),
            "QS3DSRULPREPARE", "QS3DSRULMUTATE",
            "_.UNDO", "1", "QS3DSRULCHECKUNDO",
            "_.CLOSE", "_N",
            "_.QUIT", "_N"
        )
        [IO.File]::WriteAllLines($scriptPath, $script, [Text.Encoding]::ASCII)

        $arguments = '"' + $drawing + '" /P "' + $Profile + '" /B "' + $scriptPath + '"'
        $process = Start-Process -FilePath $bricscadExe -ArgumentList $arguments -PassThru -WindowStyle Hidden -WorkingDirectory $variantRoot
        $launchedProcesses.Add($process)
        $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
        Wait-Qs3dMarker -Process ([ref]$process) -HandoffObserved ([ref]$handoffObserved) -ExpectedExecutable $bricscadExe -MarkerPath $resultPath -Deadline $deadline
        if (-not ($launchedProcesses | Where-Object { $_.Id -eq $process.Id })) { $launchedProcesses.Add($process) }
        Wait-Qs3dExit -Process $process -Deadline $deadline

        $marker = Read-Qs3dMarker -Path $resultPath
        if ($marker.ContainsKey("status") -and [string]::Equals([string]$marker["status"], "FAIL", [StringComparison]::OrdinalIgnoreCase)) {
            Require-Qs3dValue $marker "schema" "QS3D_SOURCE_UNDO_LIFECYCLE_V1"
            Require-Qs3dValue $marker "qualification_boundary" "LOCAL_004_DIAGNOSTIC_ONLY"
            Require-Qs3dValue $marker "nonce" $nonce
            Require-Qs3dValue $marker "variant" $variant
            throw "LOCAL-004 Undo lifecycle probe returned a sanitized FAIL marker."
        }
        Require-Qs3dPassMarker -Marker $marker -Nonce $nonce -Variant $variant
        $results.Add([ordered]@{
            variant = [string]$marker["variant"]
            db_recording_entry = [string]$marker["db_recording_entry"]
            db_recording_after_enable = [string]$marker["db_recording_after_enable"]
            db_recording_after_start = [string]$marker["db_recording_after_start"]
            existing_after_undo = [string]$marker["existing_after_undo"]
            topology_after_undo = [string]$marker["topology_after_undo"]
        })

        if (-not [string]::Equals((Get-FileHash -LiteralPath $drawing -Algorithm SHA256).Hash, $fixtureHash, [StringComparison]::OrdinalIgnoreCase)) {
            throw "LOCAL-004 Undo lifecycle host changed the disposable DWG despite close-without-save."
        }
        foreach ($path in $privateStatePaths | Where-Object { $_.StartsWith($variantRoot + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase) }) {
            if (Test-Path -LiteralPath $path) { throw "LOCAL-004 Undo lifecycle retained private drawing state." }
        }
    }
}
catch {
    $qualificationError = $_
}
finally {
    foreach ($process in $launchedProcesses) {
        try { Stop-Qs3dLaunchedProcess -Process $process }
        catch { if ($null -eq $cleanupError) { $cleanupError = $_ } }
    }
    $processCleanupVerified = @(Get-Process -Name "bricscad" -ErrorAction SilentlyContinue).Count -eq 0

    foreach ($path in $privateScripts) {
        try { if (Test-Path -LiteralPath $path) { Remove-Item -LiteralPath $path -Force -ErrorAction Stop } }
        catch { if ($null -eq $cleanupError) { $cleanupError = $_ } }
    }
    $scriptCleanupVerified = @($privateScripts | Where-Object { Test-Path -LiteralPath $_ }).Count -eq 0

    foreach ($path in $privateStatePaths) {
        try { if (Test-Path -LiteralPath $path) { Remove-Item -LiteralPath $path -Force -ErrorAction Stop } }
        catch { if ($null -eq $cleanupError) { $cleanupError = $_ } }
    }
    $privateStateCleanupVerified = @($privateStatePaths | Where-Object { Test-Path -LiteralPath $_ }).Count -eq 0

    $drawingHashesMatch = $true
    foreach ($path in $disposableDrawings) {
        try {
            if (Test-Path -LiteralPath $path) {
                if (-not [string]::Equals((Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash, $fixtureHash, [StringComparison]::OrdinalIgnoreCase)) {
                    $drawingHashesMatch = $false
                }
                Remove-Item -LiteralPath $path -Force -ErrorAction Stop
            }
        }
        catch { if ($null -eq $cleanupError) { $cleanupError = $_ } }
    }
    $drawingCleanupVerified = $drawingHashesMatch -and @($disposableDrawings | Where-Object { Test-Path -LiteralPath $_ }).Count -eq 0

    foreach ($name in $environmentNames) {
        try { Restore-Qs3dEnvironmentValue -Name $name -Value $oldEnvironment[$name] }
        catch { if ($null -eq $cleanupError) { $cleanupError = $_ } }
    }
}

$metadataError = $null
$metadataPath = Join-Path $ArtifactDir "source-undo-lifecycle-metadata.json"
try {
    $metadataVariants = @($results | ForEach-Object { $_ })
    $metadata = [ordered]@{
        status = if ($null -eq $qualificationError -and $null -eq $cleanupError -and $processCleanupVerified -and $scriptCleanupVerified -and $privateStateCleanupVerified -and $drawingCleanupVerified) { "PASS" } else { "FAIL" }
        schema = "QS3D_SOURCE_UNDO_LIFECYCLE_RUNNER_V1"
        qualification_boundary = "LOCAL_004_DIAGNOSTIC_ONLY"
        production_local004_qualified = $false
        exact_sha = $gitHead
        plugin_sha256 = $pluginHash
        bricscad_major = 25
        launcher_handoff_observed = [bool]$handoffObserved
        process_cleanup_verified = [bool]$processCleanupVerified
        script_cleanup_verified = [bool]$scriptCleanupVerified
        private_state_cleanup_verified = [bool]$privateStateCleanupVerified
        drawing_cleanup_verified = [bool]$drawingCleanupVerified
        variants = $metadataVariants
    }
    $metadata | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $metadataPath -Encoding UTF8
}
catch {
    $metadataError = $_
}

if ($null -ne $cleanupError) { throw "LOCAL-004 Undo lifecycle cleanup failed." }
if ($null -ne $qualificationError) { throw $qualificationError }
if ($null -ne $metadataError) { throw "LOCAL-004 Undo lifecycle metadata publication failed." }
if (-not $processCleanupVerified -or -not $scriptCleanupVerified -or -not $privateStateCleanupVerified -or -not $drawingCleanupVerified) {
    throw "LOCAL-004 Undo lifecycle cleanup verification failed."
}
if ($results.Count -ne $variants.Count) { throw "LOCAL-004 Undo lifecycle matrix is incomplete." }

Write-Output ("LOCAL-004 Source Reconcile Undo lifecycle diagnostic PASS at " + $gitHead)
Write-Output ("Sanitized metadata: " + $metadataPath)
