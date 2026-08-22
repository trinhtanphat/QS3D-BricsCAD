[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$BricsCadDir,
    [Parameter(Mandatory = $true)][string]$PluginDll,
    [Parameter(Mandatory = $true)][string]$FixtureDwg,
    [Parameter(Mandatory = $true)][string]$Profile,
    [Parameter(Mandatory = $true)][string]$ArtifactDir,
    [Parameter(Mandatory = $true)][switch]$ConfirmSyntheticFixture,
    [ValidateRange(30, 900)][int]$StartupTimeoutSeconds = 240
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

function Read-Qs3dMarker {
    param([Parameter(Mandatory = $true)][string]$Path)
    $marker = @{}
    foreach ($line in Get-Content -LiteralPath $Path) {
        if ([string]::IsNullOrWhiteSpace($line)) { continue }
        $separator = $line.IndexOf('=')
        if ($separator -le 0) { throw "Malformed project-lifecycle marker line." }
        $key = $line.Substring(0, $separator).Trim()
        $value = $line.Substring($separator + 1).Trim()
        if ($marker.ContainsKey($key)) { throw "Duplicate project-lifecycle marker key: $key" }
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
    if (-not $Marker.ContainsKey($Key)) { throw "Project-lifecycle marker is missing '$Key'." }
    if (-not [string]::Equals([string]$Marker[$Key], $Expected, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Project-lifecycle marker '$Key' did not match the expected value."
    }
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

function Invoke-Qs3dScript {
    param(
        [Parameter(Mandatory = $true)][string]$Drawing,
        [Parameter(Mandatory = $true)][string]$ScriptPath,
        [Parameter(Mandatory = $true)][string]$ResultPath,
        [Parameter(Mandatory = $true)][string[]]$Lines,
        [AllowEmptyString()][string]$Role = ""
    )

    if (Test-Path -LiteralPath $ResultPath) { throw "Project-lifecycle result already exists." }
    Set-Content -LiteralPath $ScriptPath -Value $Lines -Encoding ASCII
    $env:QS3D_LIFECYCLE_RESULT = $ResultPath
    if ([string]::IsNullOrWhiteSpace($Role)) { Remove-Item Env:QS3D_LIFECYCLE_ROLE -ErrorAction SilentlyContinue }
    else { $env:QS3D_LIFECYCLE_ROLE = $Role }

    $argumentParts = New-Object System.Collections.Generic.List[string]
    $argumentParts.Add('"' + $Drawing + '"')
    $argumentParts.Add('/P')
    $argumentParts.Add('"' + $Profile + '"')
    $argumentParts.Add('/B')
    $argumentParts.Add('"' + $ScriptPath + '"')
    $process = $null
    try {
        $process = Start-Process -FilePath $script:bricscadExe -ArgumentList ([string]::Join(' ', $argumentParts)) -PassThru -WindowStyle Hidden -WorkingDirectory $script:ArtifactDir
        $deadline = (Get-Date).AddSeconds($StartupTimeoutSeconds)
        while ((Get-Date) -lt $deadline) {
            if (Test-Path -LiteralPath $ResultPath -PathType Leaf) { break }
            $process.Refresh()
            if ($process.HasExited) { throw "BricsCAD exited before producing the project-lifecycle marker. ExitCode=$($process.ExitCode)" }
            Start-Sleep -Milliseconds 500
        }
        if (-not (Test-Path -LiteralPath $ResultPath -PathType Leaf)) {
            throw "Timed out waiting for the project-lifecycle marker after $StartupTimeoutSeconds seconds."
        }
        $marker = Read-Qs3dMarker -Path $ResultPath
        Require-Qs3dValue -Marker $marker -Key "status" -Expected "PASS"
        return $marker
    }
    finally {
        Stop-Qs3dLaunchedProcess -Process $process
    }
}

if ([Environment]::OSVersion.Platform -ne [PlatformID]::Win32NT) {
    throw "The BricsCAD V25 project-lifecycle probe requires Windows."
}
if (-not [Environment]::UserInteractive) {
    throw "The BricsCAD V25 project-lifecycle probe requires an interactive Windows session."
}
if (-not $ConfirmSyntheticFixture) {
    throw "Pass -ConfirmSyntheticFixture only for the repository's generated QS3D sample."
}

$repoRoot = [IO.Path]::GetFullPath((Split-Path -Parent $PSScriptRoot))
$artifactsRoot = [IO.Path]::GetFullPath((Join-Path $repoRoot "artifacts"))
$BricsCadDir = [IO.Path]::GetFullPath($BricsCadDir)
$PluginDll = [IO.Path]::GetFullPath($PluginDll)
$FixtureDwg = [IO.Path]::GetFullPath($FixtureDwg)
$ArtifactDir = [IO.Path]::GetFullPath($ArtifactDir)
$artifactPrefix = $artifactsRoot.TrimEnd([IO.Path]::DirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar
if (-not $ArtifactDir.StartsWith($artifactPrefix, [StringComparison]::OrdinalIgnoreCase)) {
    throw "ArtifactDir must stay inside the repository artifacts directory."
}
if (-not [string]::Equals([IO.Path]::GetFileName($FixtureDwg), "QS3D-Sample.dwg", [StringComparison]::OrdinalIgnoreCase) -or
    -not [string]::Equals([IO.Path]::GetFileName((Split-Path -Parent $FixtureDwg)), "generated", [StringComparison]::OrdinalIgnoreCase)) {
    throw "FixtureDwg must be the repository-generated QS3D-Sample.dwg, never a customer/reference drawing."
}
if (Test-Path -LiteralPath $ArtifactDir) {
    if (@(Get-ChildItem -LiteralPath $ArtifactDir -Force).Count -gt 0) {
        throw "ArtifactDir must be new or empty so lifecycle evidence cannot be overwritten."
    }
}
else { New-Item -ItemType Directory -Path $ArtifactDir | Out-Null }

$bricscadExe = Join-Path $BricsCadDir "bricscad.exe"
$coreDll = Join-Path (Split-Path -Parent $PluginDll) "QS3D.Core.dll"
foreach ($required in @($bricscadExe, $PluginDll, $coreDll, $FixtureDwg)) {
    if (-not (Test-Path -LiteralPath $required -PathType Leaf)) { throw "Required lifecycle input is missing." }
}
if (@(Get-Process -Name "bricscad" -ErrorAction SilentlyContinue).Count -gt 0) {
    throw "Close existing BricsCAD processes before starting the isolated project-lifecycle probe."
}

$gitStatus = @(& git -C $repoRoot status --porcelain)
if ($LASTEXITCODE -ne 0 -or $gitStatus.Count -ne 0) { throw "The project-lifecycle probe requires a clean exact Git SHA." }
$exactSha = (& git -C $repoRoot rev-parse HEAD).Trim()
if ($LASTEXITCODE -ne 0 -or $exactSha -notmatch '^[0-9a-f]{40}$') { throw "Unable to resolve the exact Git SHA." }

$copyDir = Join-Path $ArtifactDir "fixture-copies"
New-Item -ItemType Directory -Path $copyDir | Out-Null
$drawingA = Join-Path $copyDir "project-lifecycle-a.reference-copy.dwg"
$drawingB = Join-Path $copyDir "project-lifecycle-b.reference-copy.dwg"
$drawingC = Join-Path $copyDir "project-lifecycle-c.reference-copy.dwg"
$drawingD = Join-Path $copyDir "project-lifecycle-d.reference-copy.dwg"
foreach ($copy in @($drawingA, $drawingB, $drawingC, $drawingD)) { Copy-Item -LiteralPath $FixtureDwg -Destination $copy }
foreach ($sidecar in @(
    [IO.Path]::ChangeExtension($drawingA, ".qsdb"),
    [IO.Path]::ChangeExtension($drawingB, ".qsdb"),
    [IO.Path]::ChangeExtension($drawingC, ".qsdb"),
    [IO.Path]::ChangeExtension($drawingD, ".qsdb")
)) {
    if ((Test-Path -LiteralPath $sidecar) -or (Test-Path -LiteralPath ($sidecar + ".bak"))) {
        throw "A lifecycle drawing copy unexpectedly has a pre-existing sidecar."
    }
}
$corruptSidecar = [IO.Path]::ChangeExtension($drawingD, ".qsdb")
[IO.File]::WriteAllText($corruptSidecar, "<not-a-qs3d-project />", (New-Object Text.UTF8Encoding($false)))
$corruptHashBefore = (Get-FileHash -LiteralPath $corruptSidecar -Algorithm SHA256).Hash.ToUpperInvariant()

$fixtureHashBefore = (Get-FileHash -LiteralPath $FixtureDwg -Algorithm SHA256).Hash.ToUpperInvariant()
$pluginHash = (Get-FileHash -LiteralPath $PluginDll -Algorithm SHA256).Hash.ToUpperInvariant()
$nonce = [Guid]::NewGuid().ToString("N")
$statePath = Join-Path $ArtifactDir "project-lifecycle-state.txt"
Set-Content -LiteralPath $statePath -Value ("nonce=" + $nonce) -Encoding UTF8
$seedAResult = Join-Path $ArtifactDir "project-lifecycle-seed-a.txt"
$seedBResult = Join-Path $ArtifactDir "project-lifecycle-seed-b.txt"
$finalResult = Join-Path $ArtifactDir "project-lifecycle-result.txt"
$metadataPath = Join-Path $ArtifactDir "project-lifecycle-metadata.json"

$environmentNames = @(
    "QS3D_LIFECYCLE_RESULT", "QS3D_LIFECYCLE_STATE", "QS3D_LIFECYCLE_NONCE", "QS3D_LIFECYCLE_ROLE",
    "QS3D_LIFECYCLE_DWG_A", "QS3D_LIFECYCLE_DWG_B", "QS3D_LIFECYCLE_DWG_C", "QS3D_LIFECYCLE_DWG_D"
)
$oldEnvironment = @{}
foreach ($name in $environmentNames) { $oldEnvironment[$name] = [Environment]::GetEnvironmentVariable($name, "Process") }
$startedAt = [DateTime]::UtcNow

try {
    $env:QS3D_LIFECYCLE_STATE = $statePath
    $env:QS3D_LIFECYCLE_NONCE = $nonce
    $env:QS3D_LIFECYCLE_DWG_A = $drawingA
    $env:QS3D_LIFECYCLE_DWG_B = $drawingB
    $env:QS3D_LIFECYCLE_DWG_C = $drawingC
    $env:QS3D_LIFECYCLE_DWG_D = $drawingD

    $seedA = Invoke-Qs3dScript -Drawing $drawingA -ScriptPath (Join-Path $ArtifactDir "project-lifecycle-seed-a.scr") -ResultPath $seedAResult -Role "A" -Lines @(
        "FILEDIA", "0", "CMDECHO", "1", "NETLOAD", ('"' + $PluginDll + '"'),
        "QS3DLIFECYCLESEED", "_.QSAVE", "QS3DLIFECYCLEAFTERSAVE"
    )
    $seedB = Invoke-Qs3dScript -Drawing $drawingB -ScriptPath (Join-Path $ArtifactDir "project-lifecycle-seed-b.scr") -ResultPath $seedBResult -Role "B" -Lines @(
        "FILEDIA", "0", "CMDECHO", "1", "NETLOAD", ('"' + $PluginDll + '"'),
        "QS3DLIFECYCLESEED", "_.QSAVE", "QS3DLIFECYCLEAFTERSAVE"
    )
    foreach ($seed in @($seedA, $seedB)) {
        Require-Qs3dValue -Marker $seed -Key "dwg_savecomplete_sidecar" -Expected "true"
        Require-Qs3dValue -Marker $seed -Key "pending_changes_cleared" -Expected "true"
        Require-Qs3dValue -Marker $seed -Key "saved_project_readable" -Expected "true"
    }

    $multi = Invoke-Qs3dScript -Drawing $drawingA -ScriptPath (Join-Path $ArtifactDir "project-lifecycle-multi.scr") -ResultPath $finalResult -Lines @(
        "FILEDIA", "0", "CMDECHO", "1", "NETLOAD", ('"' + $PluginDll + '"'),
        "_.OPEN", ('"' + $drawingB + '"'),
        "_.OPEN", ('"' + $drawingC + '"'),
        "_.OPEN", ('"' + $drawingD + '"'),
        "QS3DLIFECYCLEPROBE"
    )
    foreach ($key in @(
        "dwg_savecomplete_sidecar", "cold_reopen_project_identity_matched", "canonical_bind_matched",
        "detached_snapshot_not_mutated", "distinct_project_identity", "multi_dwg_mutation_isolated",
        "second_cold_reload_persisted", "absent_sidecar_noncreating", "corrupt_sidecar_fail_closed"
    )) { Require-Qs3dValue -Marker $multi -Key $key -Expected "true" }
    [int]$documentCount = 0
    if (-not $multi.ContainsKey("document_count") -or
        -not [int]::TryParse([string]$multi["document_count"], [Globalization.NumberStyles]::None, [Globalization.CultureInfo]::InvariantCulture, [ref]$documentCount) -or
        $documentCount -lt 4) { throw "Project-lifecycle probe did not observe all four documents." }

    $sidecarA = [IO.Path]::ChangeExtension($drawingA, ".qsdb")
    $sidecarB = [IO.Path]::ChangeExtension($drawingB, ".qsdb")
    $sidecarC = [IO.Path]::ChangeExtension($drawingC, ".qsdb")
    if (-not (Test-Path -LiteralPath $sidecarA) -or -not (Test-Path -LiteralPath $sidecarB)) {
        throw "Lifecycle A/B sidecars are missing after save/reopen."
    }
    if ((Test-Path -LiteralPath $sidecarC) -or (Test-Path -LiteralPath ($sidecarC + ".bak"))) {
        throw "The absent-sidecar drawing acquired a QS3D project file."
    }
    $corruptHashAfter = (Get-FileHash -LiteralPath $corruptSidecar -Algorithm SHA256).Hash.ToUpperInvariant()
    if (-not [string]::Equals($corruptHashBefore, $corruptHashAfter, [StringComparison]::Ordinal)) {
        throw "The corrupt sidecar changed while the loader was expected to fail closed."
    }
    $fixtureHashAfter = (Get-FileHash -LiteralPath $FixtureDwg -Algorithm SHA256).Hash.ToUpperInvariant()
    if (-not [string]::Equals($fixtureHashBefore, $fixtureHashAfter, [StringComparison]::Ordinal)) {
        throw "The repository-generated fixture changed during lifecycle qualification."
    }

    $metadata = [ordered]@{
        schema = 1
        status = "PASS"
        exactSha = $exactSha
        bricscadVersion = [Diagnostics.FileVersionInfo]::GetVersionInfo($bricscadExe).FileVersion
        pluginSha256 = $pluginHash
        fixtureSha256Before = $fixtureHashBefore
        fixtureSha256After = $fixtureHashAfter
        documentCount = $documentCount
        dwgSaveCompleteSidecar = $true
        coldReopenProjectIdentityMatched = $true
        canonicalBindMatched = $true
        detachedSnapshotNotMutated = $true
        distinctProjectIdentity = $true
        multiDwgMutationIsolated = $true
        secondColdReloadPersisted = $true
        absentSidecarNoncreating = $true
        corruptSidecarFailClosed = $true
        corruptSidecarUnchanged = $true
        startedUtc = $startedAt.ToString("O")
        completedUtc = [DateTime]::UtcNow.ToString("O")
    }
    $metadata | ConvertTo-Json -Depth 3 | Set-Content -LiteralPath $metadataPath -Encoding UTF8
    Remove-Item -LiteralPath $statePath -Force
    Write-Host "QS3D BricsCAD V25 project save/reopen/multi-DWG lifecycle probe PASS"
    Write-Host "Result: $finalResult"
}
finally {
    foreach ($name in $environmentNames) { Restore-EnvironmentValue -Name $name -Value $oldEnvironment[$name] }
    foreach ($process in @(Get-Process -Name "bricscad" -ErrorAction SilentlyContinue)) { Stop-Qs3dLaunchedProcess -Process $process }
}
