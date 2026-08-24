[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$BricsCadDir,
    [Parameter(Mandatory = $true)][string]$PluginDll,
    [Parameter(Mandatory = $true)][string]$FixtureDwg,
    [Parameter(Mandatory = $true)][string]$Profile,
    [Parameter(Mandatory = $true)][string]$ArtifactDir,
    [Parameter(Mandatory = $true)][ValidateSet(25, 26)][int]$ExpectedHostMajor,
    [Parameter(Mandatory = $true)][switch]$ConfirmSyntheticFixture,
    [ValidateRange(30, 900)][int]$TimeoutSeconds = 240
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$helper = Join-Path $PSScriptRoot 'bricscad-runner-window-interop.ps1'
if (-not (Test-Path -LiteralPath $helper -PathType Leaf)) { throw 'BricsCAD runner helper is missing.' }
. $helper

if ([Environment]::OSVersion.Platform -ne [PlatformID]::Win32NT) { throw 'SAVEAS lifecycle qualification requires Windows.' }
if (-not [Environment]::UserInteractive) { throw 'SAVEAS lifecycle qualification requires an interactive Windows session.' }
if (-not $ConfirmSyntheticFixture) { throw 'Pass -ConfirmSyntheticFixture only for the repository-generated QS3D sample.' }

$repoRoot = [IO.Path]::GetFullPath((Split-Path -Parent $PSScriptRoot))
$artifactsRoot = [IO.Path]::GetFullPath((Join-Path $repoRoot 'artifacts'))
$BricsCadDir = [IO.Path]::GetFullPath($BricsCadDir)
$PluginDll = [IO.Path]::GetFullPath($PluginDll)
$FixtureDwg = [IO.Path]::GetFullPath($FixtureDwg)
$ArtifactDir = [IO.Path]::GetFullPath($ArtifactDir)
$artifactPrefix = $artifactsRoot.TrimEnd([IO.Path]::DirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar
if (-not $ArtifactDir.StartsWith($artifactPrefix, [StringComparison]::OrdinalIgnoreCase)) { throw 'ArtifactDir must stay inside repository artifacts/.' }
if (-not [string]::Equals([IO.Path]::GetFileName($FixtureDwg), 'QS3D-Sample.dwg', [StringComparison]::OrdinalIgnoreCase) -or
    -not [string]::Equals([IO.Path]::GetFileName((Split-Path -Parent $FixtureDwg)), 'generated', [StringComparison]::OrdinalIgnoreCase)) {
    throw 'FixtureDwg must be samples/generated/QS3D-Sample.dwg.'
}
if (Test-Path -LiteralPath $ArtifactDir) {
    if (@(Get-ChildItem -LiteralPath $ArtifactDir -Force).Count -gt 0) { throw 'ArtifactDir must be new or empty.' }
} else { New-Item -ItemType Directory -Path $ArtifactDir | Out-Null }

$bricscadExe = Join-Path $BricsCadDir 'bricscad.exe'
foreach ($required in @($bricscadExe, $PluginDll, $FixtureDwg)) {
    if (-not (Test-Path -LiteralPath $required -PathType Leaf)) { throw 'Required SAVEAS lifecycle input is missing.' }
}
$hostVersion = [Diagnostics.FileVersionInfo]::GetVersionInfo($bricscadExe)
if ($hostVersion.ProductMajorPart -ne $ExpectedHostMajor -and $hostVersion.FileMajorPart -ne $ExpectedHostMajor) {
    throw "Configured BricsCAD host does not match expected major $ExpectedHostMajor."
}
$expectedAssembly = "QS3D.BricsCAD.V$ExpectedHostMajor"
if (-not [string]::Equals([Reflection.AssemblyName]::GetAssemblyName($PluginDll).Name, $expectedAssembly, [StringComparison]::Ordinal)) {
    throw "Plugin assembly does not match expected host major $ExpectedHostMajor."
}
if (@(Get-Qs3dExactBricsCadProcesses -ExpectedExecutable $bricscadExe).Count -gt 0) { throw 'Close existing matching BricsCAD processes first.' }

$gitStatus = @(& git -C $repoRoot status --porcelain)
if ($LASTEXITCODE -ne 0 -or $gitStatus.Count -ne 0) { throw 'SAVEAS lifecycle qualification requires a clean exact Git SHA.' }
$exactSha = (& git -C $repoRoot rev-parse HEAD).Trim()
if ($LASTEXITCODE -ne 0 -or $exactSha -notmatch '^[0-9a-f]{40}$') { throw 'Unable to resolve exact Git SHA.' }
Assert-Qs3dExactSourceIdentity -RepoRoot $repoRoot -PluginDll $PluginDll -ExpectedSourceSha $exactSha

$sourceDrawing = Join-Path $ArtifactDir 'saveas-source.reference-copy.dwg'
$targetDrawing = Join-Path $ArtifactDir 'saveas-target.reference-copy.dwg'
$statePath = Join-Path $ArtifactDir 'saveas-lifecycle-state.txt'
$resultPath = Join-Path $ArtifactDir 'saveas-lifecycle-result.txt'
$scriptPath = Join-Path $ArtifactDir 'saveas-lifecycle.scr'
$metadataPath = Join-Path $ArtifactDir 'saveas-lifecycle-metadata.json'
Copy-Item -LiteralPath $FixtureDwg -Destination $sourceDrawing
$fixtureHashBefore = (Get-FileHash -LiteralPath $FixtureDwg -Algorithm SHA256).Hash.ToUpperInvariant()
$sourceHashBefore = (Get-FileHash -LiteralPath $sourceDrawing -Algorithm SHA256).Hash.ToUpperInvariant()
$pluginHash = (Get-FileHash -LiteralPath $PluginDll -Algorithm SHA256).Hash.ToUpperInvariant()
$nonce = [Guid]::NewGuid().ToString('N')

$environmentNames = @('QS3D_SAVEAS_RESULT','QS3D_SAVEAS_STATE','QS3D_SAVEAS_NONCE','QS3D_SAVEAS_ORIGINAL_DWG','QS3D_SAVEAS_TARGET_DWG')
$oldEnvironment = @{}
foreach ($name in $environmentNames) { $oldEnvironment[$name] = [Environment]::GetEnvironmentVariable($name, 'Process') }
$process = $null
$startedUtc = [DateTime]::UtcNow
try {
    $env:QS3D_SAVEAS_RESULT = $resultPath
    $env:QS3D_SAVEAS_STATE = $statePath
    $env:QS3D_SAVEAS_NONCE = $nonce
    $env:QS3D_SAVEAS_ORIGINAL_DWG = $sourceDrawing
    $env:QS3D_SAVEAS_TARGET_DWG = $targetDrawing

    $lines = @(
        'FILEDIA', '0', 'CMDECHO', '1',
        'NETLOAD', ('"' + $PluginDll + '"'),
        'QS3DSAVEASLIFECYCLEPREP',
        '_.SAVEAS', '2018', ('"' + $targetDrawing + '"'),
        'QS3DSAVEASLIFECYCLEVERIFY'
    )
    Set-Content -LiteralPath $scriptPath -Value $lines -Encoding ASCII

    $args = '"' + $sourceDrawing + '" /P "' + $Profile + '" /B "' + $scriptPath + '"'
    $process = Start-Process -FilePath $bricscadExe -ArgumentList $args -PassThru -WindowStyle Hidden -WorkingDirectory $ArtifactDir
    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        if (Test-Path -LiteralPath $resultPath -PathType Leaf) { break }
        $process.Refresh()
        if ($process.HasExited) { throw "BricsCAD exited before producing SAVEAS lifecycle evidence. ExitCode=$($process.ExitCode)" }
        Start-Sleep -Milliseconds 500
    }
    if (-not (Test-Path -LiteralPath $resultPath -PathType Leaf)) { throw "Timed out waiting for SAVEAS lifecycle evidence after $TimeoutSeconds seconds." }

    $marker = @{}
    foreach ($line in Get-Content -LiteralPath $resultPath) {
        if ([string]::IsNullOrWhiteSpace($line)) { continue }
        $separator = $line.IndexOf('=')
        if ($separator -le 0) { throw 'Malformed SAVEAS lifecycle marker.' }
        $key = $line.Substring(0,$separator).Trim(); $value = $line.Substring($separator+1).Trim()
        if ($marker.ContainsKey($key)) { throw 'Duplicate SAVEAS lifecycle marker key.' }
        $marker[$key] = $value
    }
    foreach ($key in @('native_saveas_path_transition','canonical_project_identity_preserved','target_sidecar_persisted','original_sidecar_unchanged','pending_state_cleared','cold_cache_reload_matched')) {
        if (-not $marker.ContainsKey($key) -or -not [string]::Equals([string]$marker[$key], 'true', [StringComparison]::OrdinalIgnoreCase)) {
            throw "SAVEAS lifecycle marker '$key' did not pass."
        }
    }
    if (-not $marker.ContainsKey('status') -or -not [string]::Equals([string]$marker['status'],'PASS',[StringComparison]::OrdinalIgnoreCase)) { throw 'SAVEAS lifecycle probe did not report PASS.' }

    if (-not (Test-Path -LiteralPath $targetDrawing -PathType Leaf)) { throw 'Native SAVEAS target drawing is missing.' }
    if (-not (Test-Path -LiteralPath ([IO.Path]::ChangeExtension($targetDrawing,'.qsdb')) -PathType Leaf)) { throw 'SAVEAS target sidecar is missing.' }
    if (-not (Test-Path -LiteralPath ([IO.Path]::ChangeExtension($sourceDrawing,'.qsdb')) -PathType Leaf)) { throw 'SAVEAS original sidecar is missing.' }
    $fixtureHashAfter = (Get-FileHash -LiteralPath $FixtureDwg -Algorithm SHA256).Hash.ToUpperInvariant()
    if (-not [string]::Equals($fixtureHashBefore,$fixtureHashAfter,[StringComparison]::Ordinal)) { throw 'Repository-generated fixture changed during SAVEAS qualification.' }

    [ordered]@{
        schema = 1
        status = 'PASS'
        exactSha = $exactSha
        expectedHostMajor = $ExpectedHostMajor
        bricscadVersion = $hostVersion.FileVersion
        pluginSha256 = $pluginHash
        fixtureSha256Before = $fixtureHashBefore
        fixtureSha256After = $fixtureHashAfter
        sourceCopySha256Before = $sourceHashBefore
        nativeSaveAsPathTransition = $true
        canonicalProjectIdentityPreserved = $true
        targetSidecarPersisted = $true
        originalSidecarUnchanged = $true
        pendingStateCleared = $true
        coldCacheReloadMatched = $true
        startedUtc = $startedUtc.ToString('O')
        completedUtc = [DateTime]::UtcNow.ToString('O')
    } | ConvertTo-Json -Depth 3 | Set-Content -LiteralPath $metadataPath -Encoding UTF8

    Write-Host "QS3D BricsCAD V$ExpectedHostMajor SAVEAS lifecycle probe PASS"
    Write-Host "Evidence: $metadataPath"
}
finally {
    if ($null -ne $process) {
        try { $process.Refresh(); if (-not $process.HasExited) { Stop-Process -Id $process.Id -Force -ErrorAction SilentlyContinue; $process.WaitForExit(10000) | Out-Null } } catch { }
    }
    foreach ($name in $environmentNames) {
        $old = $oldEnvironment[$name]
        if ($null -eq $old) { Remove-Item -LiteralPath ('Env:' + $name) -ErrorAction SilentlyContinue }
        else { Set-Item -LiteralPath ('Env:' + $name) -Value $old }
    }
    if (-not (Wait-Qs3dNoExactBricsCadProcesses -ExpectedExecutable $bricscadExe -TimeoutSeconds 30)) { throw 'SAVEAS lifecycle cleanup left a matching BricsCAD process.' }
}
