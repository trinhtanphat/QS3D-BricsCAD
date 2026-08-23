param(
    [Parameter(Mandatory = $true)][string]$BricsCadDir,
    [Parameter(Mandatory = $true)][string]$PluginDll,
    [Parameter(Mandatory = $true)][string]$FixtureDwg,
    [Parameter(Mandatory = $true)][string]$Profile,
    [Parameter(Mandatory = $true)][string]$ArtifactDir,
    [Parameter(Mandatory = $true)][switch]$ConfirmDisposableCopies,
    [ValidateRange(120, 1800)][int]$StartupTimeoutSeconds = 600
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$windowInteropPath = Join-Path $PSScriptRoot "bricscad-runner-window-interop.ps1"
if (-not (Test-Path -LiteralPath $windowInteropPath -PathType Leaf)) { throw "LOCAL-004 P04 runner window interop helper is missing." }
. $windowInteropPath

function Read-Qs3dMarker {
    param([Parameter(Mandatory = $true)][string]$Path)
    $marker = @{}
    foreach ($line in Get-Content -LiteralPath $Path) {
        if ([string]::IsNullOrWhiteSpace($line)) { continue }
        $i = $line.IndexOf('=')
        if ($i -le 0 -or $i -eq $line.Length - 1) { throw "Malformed LOCAL-004 P04 marker line." }
        $key = $line.Substring(0, $i).Trim(); $value = $line.Substring($i + 1).Trim()
        if ($marker.ContainsKey($key)) { throw "Duplicate LOCAL-004 P04 marker key." }
        $marker[$key] = $value
    }
    return $marker
}

function Require-Value {
    param($Marker, [string]$Key, [string]$Expected)
    if (-not $Marker.ContainsKey($Key) -or -not [string]::Equals([string]$Marker[$Key], $Expected, [StringComparison]::OrdinalIgnoreCase)) {
        throw "LOCAL-004 P04 marker field mismatch: $Key"
    }
}

function Require-PassMarker {
    param($Marker, [string]$Nonce, [bool]$ColdReopen, [bool]$Qualified)
    $keys = @(
        "status","command","nonce","schema","qualification_boundary","production_local004_p04_qualified",
        "baseline_verified","native_stretch_verified","pre_sync_output_isolation_verified","source_reconcile_verified",
        "dependent_invalidation_verified","dependent_rebuild_verified","stirrup_redistribution_verified","longitudinal_extent_verified",
        "cold_reopen_verified","source_type","edit_command","final_length_class","stirrup_count_class","output_families","error_code"
    )
    if (@($Marker.Keys).Count -ne $keys.Count) { throw "LOCAL-004 P04 marker contains an unexpected field." }
    foreach ($key in $keys) { if (-not $Marker.ContainsKey($key)) { throw "LOCAL-004 P04 marker missing $key." } }
    Require-Value $Marker "status" "PASS"
    Require-Value $Marker "nonce" $Nonce
    Require-Value $Marker "schema" "QS3D_SOURCE_RECONCILE_NATIVE_BEAM_STRETCH_DEPENDENT_RUNTIME_V1"
    Require-Value $Marker "qualification_boundary" "LOCAL_004_P04_BEAM_DEPENDENT_STRETCH"
    Require-Value $Marker "production_local004_p04_qualified" $(if ($Qualified) { "true" } else { "false" })
    foreach ($key in @("baseline_verified","native_stretch_verified","pre_sync_output_isolation_verified","source_reconcile_verified","dependent_invalidation_verified","dependent_rebuild_verified","stirrup_redistribution_verified","longitudinal_extent_verified")) { Require-Value $Marker $key "true" }
    Require-Value $Marker "cold_reopen_verified" $(if ($ColdReopen) { "true" } else { "false" })
    Require-Value $Marker "source_type" "LINE_BEAM"
    Require-Value $Marker "edit_command" "STRETCH"
    Require-Value $Marker "final_length_class" "EIGHT_METERS"
    Require-Value $Marker "stirrup_count_class" "NINE_AT_D8_1000"
    Require-Value $Marker "output_families" "HOST_LONGITUDINAL_STIRRUP"
    Require-Value $Marker "error_code" "NONE"
}

function Restore-EnvironmentValue {
    param([string]$Name, [AllowNull()][string]$Value)
    if ($null -eq $Value) { Remove-Item -LiteralPath ("Env:" + $Name) -ErrorAction SilentlyContinue }
    else { Set-Item -LiteralPath ("Env:" + $Name) -Value $Value }
}

function Read-SingleProjectValue {
    param([Parameter(Mandatory = $true)][string]$ProjectPath, [Parameter(Mandatory = $true)][string]$Element)
    try { [xml]$project = Get-Content -LiteralPath $ProjectPath -Raw }
    catch { throw "Could not read P04 project identity from $ProjectPath." }
    $values = @($project.Project.PropertyGroup | ForEach-Object {
        $property = $_.PSObject.Properties[$Element]
        if ($null -ne $property -and -not [string]::IsNullOrWhiteSpace([string]$property.Value)) { ([string]$property.Value).Trim() }
    } | Select-Object -Unique)
    if ($values.Count -ne 1) { throw "P04 project must declare exactly one $Element identity: $ProjectPath" }
    return [string]$values[0]
}

function Require-PdbSourceLink {
    param([Parameter(Mandatory = $true)][string]$PdbPath, [Parameter(Mandatory = $true)][string]$ExpectedUrl)
    $pdbText = [Text.Encoding]::UTF8.GetString([IO.File]::ReadAllBytes($PdbPath))
    if ($pdbText.IndexOf($ExpectedUrl, [StringComparison]::OrdinalIgnoreCase) -lt 0) {
        throw "P04 PDB SourceLink does not bind the binary to the exact clean Git SHA: $PdbPath"
    }
}

function Stop-OwnedProcess {
    param([AllowNull()][Diagnostics.Process]$Process)
    if ($null -eq $Process) { return }
    $Process.Refresh()
    if (-not $Process.HasExited) { Stop-Process -Id $Process.Id -Force -ErrorAction Stop; [void]$Process.WaitForExit(15000); $Process.Refresh() }
    if (-not $Process.HasExited) { throw "LOCAL-004 P04 BricsCAD process did not exit." }
}

function Find-Handoff {
    param([int]$ParentId, [string]$ExpectedExe)
    $records = @(Get-CimInstance Win32_Process -Filter ("Name = 'bricscad.exe' AND ParentProcessId = " + $ParentId))
    $matches = @()
    foreach ($record in $records) {
        if ([string]::IsNullOrWhiteSpace([string]$record.ExecutablePath)) { continue }
        if (-not [string]::Equals([IO.Path]::GetFullPath([string]$record.ExecutablePath), $ExpectedExe, [StringComparison]::OrdinalIgnoreCase)) { continue }
        $p = Get-Process -Id ([int]$record.ProcessId) -ErrorAction SilentlyContinue
        if ($null -ne $p) { $matches += $p }
    }
    if ($matches.Count -gt 1) { throw "LOCAL-004 P04 launcher produced ambiguous BricsCAD handoff." }
    if ($matches.Count -eq 1) { return $matches[0] }
    return $null
}

function Wait-Marker {
    param([ref]$Process, [string]$ExpectedExe, [string]$SuccessPath, [string]$FailurePath, [DateTime]$Deadline)
    [Diagnostics.Process]$current = $Process.Value
    $launcher = $current.Id; $adopted = $false
    while ((Get-Date) -lt $Deadline) {
        if (Test-Path -LiteralPath $SuccessPath -PathType Leaf) { return }
        if (Test-Path -LiteralPath $FailurePath -PathType Leaf) { return }
        [void](Close-Qs3dProxyInformationDialog -Process $current)
        $current.Refresh()
        if ($current.HasExited) {
            if ($adopted) { throw "BricsCAD exited before LOCAL-004 P04 marker publication." }
            $handoffDeadline = (Get-Date).AddSeconds(30)
            do { $next = Find-Handoff -ParentId $launcher -ExpectedExe $ExpectedExe; if ($null -ne $next) { break }; Start-Sleep -Milliseconds 250 } while ((Get-Date) -lt $handoffDeadline)
            if ($null -eq $next) { throw "LOCAL-004 P04 launcher exited without exact child handoff." }
            $current = $next; $Process.Value = $current; $adopted = $true
        }
        Start-Sleep -Milliseconds 500
    }
    throw "Timed out waiting for LOCAL-004 P04 marker."
}

function Wait-Exit {
    param([Diagnostics.Process]$Process, [DateTime]$Deadline)
    while ((Get-Date) -lt $Deadline) {
        $Process.Refresh(); if ($Process.HasExited) { return }
        [void](Close-Qs3dProxyInformationDialog -Process $Process); Start-Sleep -Milliseconds 500
    }
    throw "Timed out waiting for LOCAL-004 P04 BricsCAD exit."
}

function Get-ExactBricsCadProcesses {
    param([Parameter(Mandatory = $true)][string]$ExpectedExe)
    $matches = @()
    foreach ($record in @(Get-CimInstance Win32_Process -Filter "Name = 'bricscad.exe'")) {
        if ([string]::IsNullOrWhiteSpace([string]$record.ExecutablePath)) { continue }
        if (-not [string]::Equals([IO.Path]::GetFullPath([string]$record.ExecutablePath), $ExpectedExe, [StringComparison]::OrdinalIgnoreCase)) { continue }
        $process = Get-Process -Id ([int]$record.ProcessId) -ErrorAction SilentlyContinue
        if ($null -ne $process) { $matches += $process }
    }
    return $matches
}

function Wait-NoExactBricsCadProcesses {
    param([Parameter(Mandatory = $true)][string]$ExpectedExe, [DateTime]$Deadline)
    while ((Get-Date) -lt $Deadline) {
        if (@(Get-ExactBricsCadProcesses -ExpectedExe $ExpectedExe).Count -eq 0) { return }
        Start-Sleep -Milliseconds 500
    }
    throw "P04 process cleanup incomplete."
}

function Remove-ExactFile {
    param([string]$Path)
    if (Test-Path -LiteralPath $Path) { Remove-Item -LiteralPath $Path -Force -ErrorAction Stop }
    if (Test-Path -LiteralPath $Path) { throw "LOCAL-004 P04 private-file cleanup failed." }
}

if ([Environment]::OSVersion.Platform -ne [PlatformID]::Win32NT -or -not [Environment]::UserInteractive) { throw "LOCAL-004 P04 requires interactive Windows." }
if (-not $ConfirmDisposableCopies) { throw "Pass -ConfirmDisposableCopies only for repository sample copies." }
if ([string]::IsNullOrWhiteSpace($Profile)) { throw "LOCAL-004 P04 requires an initialized BricsCAD profile." }

$repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot ".."))
$BricsCadDir = [IO.Path]::GetFullPath($BricsCadDir); $PluginDll = [IO.Path]::GetFullPath($PluginDll)
$FixtureDwg = [IO.Path]::GetFullPath($FixtureDwg); $ArtifactDir = [IO.Path]::GetFullPath($ArtifactDir)
if ($ArtifactDir.StartsWith($repoRoot + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) { throw "ArtifactDir must stay outside repository." }
$expectedFixture = [IO.Path]::GetFullPath((Join-Path $repoRoot "samples\generated\QS3D-Sample.dwg"))
$pluginProject = [IO.Path]::GetFullPath((Join-Path $repoRoot "src\QS3D.BricsCAD.V25\QS3D.BricsCAD.V25.csproj"))
$coreProject = [IO.Path]::GetFullPath((Join-Path $repoRoot "src\QS3D.Core\QS3D.Core.csproj"))
$expectedPlugin = [IO.Path]::GetFullPath((Join-Path $repoRoot "src\QS3D.BricsCAD.V25\bin\x64\Release\net48\QS3D.BricsCAD.V25.dll"))
if (-not [string]::Equals($FixtureDwg, $expectedFixture, [StringComparison]::OrdinalIgnoreCase)) { throw "FixtureDwg must be repository QS3D sample." }
if (-not [string]::Equals($PluginDll, $expectedPlugin, [StringComparison]::OrdinalIgnoreCase)) { throw "PluginDll must be exact repository x64 Release output." }
$bricscadExe = Join-Path $BricsCadDir "bricscad.exe"
foreach ($required in @($bricscadExe,$FixtureDwg,$pluginProject,$coreProject)) { if (-not (Test-Path -LiteralPath $required -PathType Leaf)) { throw "Required LOCAL-004 P04 input missing." } }

$git = Get-Command git -CommandType Application -ErrorAction Stop | Select-Object -First 1
$headLines = @(& $git.Source -C $repoRoot rev-parse HEAD 2>$null); if ($LASTEXITCODE -ne 0 -or $headLines.Count -ne 1) { throw "Cannot resolve P04 Git SHA." }
$gitHead = ([string]$headLines[0]).Trim().ToLowerInvariant(); if ($gitHead -notmatch '^[0-9a-f]{40}$') { throw "P04 Git SHA invalid." }
if (@(& $git.Source -C $repoRoot status --porcelain=v1 --untracked-files=all 2>$null).Count -ne 0) { throw "P04 qualification requires clean worktree." }
$dotnet = Get-Command dotnet -CommandType Application -ErrorAction Stop | Select-Object -First 1
& $dotnet.Source build $pluginProject -c Release '-p:Platform=x64' ("-p:BRICSCAD_V25_DIR=" + $BricsCadDir)
if ($LASTEXITCODE -ne 0) { throw "P04 exact-source V25 Release|x64 build failed with exit code $LASTEXITCODE." }
if (@(& $git.Source -C $repoRoot status --porcelain=v1 --untracked-files=all 2>$null).Count -ne 0) { throw "P04 build changed tracked source state." }

$coreDll = Join-Path (Split-Path -Parent $PluginDll) "QS3D.Core.dll"
$pluginPdb = [IO.Path]::ChangeExtension($PluginDll, ".pdb")
$corePdb = [IO.Path]::ChangeExtension($coreDll, ".pdb")
foreach ($required in @($PluginDll,$coreDll,$pluginPdb,$corePdb)) { if (-not (Test-Path -LiteralPath $required -PathType Leaf)) { throw "P04 exact-source build output missing: $required" } }
$publicProductVersion = Read-SingleProjectValue -ProjectPath $pluginProject -Element "InformationalVersion"
$coreProductVersion = Read-SingleProjectValue -ProjectPath $coreProject -Element "InformationalVersion"
if (-not [string]::Equals($publicProductVersion, $coreProductVersion, [StringComparison]::Ordinal)) { throw "P04 plugin/Core public ProductVersion declarations disagree." }
foreach ($assembly in @($PluginDll,$coreDll)) {
    $actualProductVersion = ([string](Get-Item -LiteralPath $assembly).VersionInfo.ProductVersion).Trim()
    if (-not [string]::Equals($actualProductVersion, $publicProductVersion, [StringComparison]::Ordinal)) { throw "P04 assembly public ProductVersion does not match the exact project identity: $assembly" }
}
$sourceLinkPrefix = "https://raw.githubusercontent.com/trinhtanphat/QS3D-BricsCAD/" + $gitHead + "/"
Require-PdbSourceLink -PdbPath $pluginPdb -ExpectedUrl $sourceLinkPrefix
Require-PdbSourceLink -PdbPath $corePdb -ExpectedUrl $sourceLinkPrefix
if (@(Get-ExactBricsCadProcesses -ExpectedExe $bricscadExe).Count -gt 0) { throw "Close BricsCAD V25 before isolated P04 run." }

if (Test-Path $ArtifactDir) { if (@(Get-ChildItem $ArtifactDir -Force).Count -ne 0) { throw "ArtifactDir must be empty." } } else { New-Item -ItemType Directory -Path $ArtifactDir | Out-Null }
$fixtureRoot = Join-Path $ArtifactDir "fixture-copy"; New-Item -ItemType Directory -Path $fixtureRoot | Out-Null
$drawing = Join-Path $fixtureRoot "source-native-beam-stretch-dependent-copy.dwg"; Copy-Item $FixtureDwg $drawing
$fixtureHash = (Get-FileHash $FixtureDwg -Algorithm SHA256).Hash.ToUpperInvariant(); if ((Get-FileHash $drawing -Algorithm SHA256).Hash.ToUpperInvariant() -ne $fixtureHash) { throw "P04 fixture copy mismatch." }
$sidecar = [IO.Path]::ChangeExtension($drawing, ".qsdb")
$privateFiles = @($sidecar,($sidecar + ".bak"),($sidecar + ".lock"),[IO.Path]::ChangeExtension($drawing,".dwl"),[IO.Path]::ChangeExtension($drawing,".dwl2"),[IO.Path]::ChangeExtension($drawing,".bak"))
$resultPath = Join-Path $ArtifactDir "source-reconcile-native-beam-stretch-dependent-result.txt"
$phasePath = Join-Path $ArtifactDir "source-reconcile-native-beam-stretch-dependent-session1.txt"
$script1 = Join-Path $ArtifactDir "source-reconcile-native-beam-stretch-dependent-session1.private.scr"
$script2 = Join-Path $ArtifactDir "source-reconcile-native-beam-stretch-dependent-session2.private.scr"
$metadataPath = Join-Path $ArtifactDir "source-reconcile-native-beam-stretch-dependent-metadata.json"
$nonce = [Guid]::NewGuid().ToString("N")
$envNames = @("QS3D_SOURCE_RECONCILE_NATIVE_BEAM_STRETCH_DEPENDENT_RESULT","QS3D_SOURCE_RECONCILE_NATIVE_BEAM_STRETCH_DEPENDENT_PHASE_RESULT","QS3D_SOURCE_RECONCILE_NATIVE_BEAM_STRETCH_DEPENDENT_NONCE","QS3D_SOURCE_RECONCILE_NATIVE_BEAM_STRETCH_DEPENDENT_DWG")
$oldEnv = @{}; foreach ($name in $envNames) { $oldEnv[$name] = [Environment]::GetEnvironmentVariable($name,"Process") }
$p1=$null; $p2=$null; $phaseMarker=$null; $finalMarker=$null; $qualificationError=$null; $cleanupError=$null
$processCleanup=$false; $scriptCleanup=$false; $privateCleanup=$false; $drawingRestore=$false; $persisted=$false; $sidecarPersisted=$false

try {
    $env:QS3D_SOURCE_RECONCILE_NATIVE_BEAM_STRETCH_DEPENDENT_RESULT=$resultPath
    $env:QS3D_SOURCE_RECONCILE_NATIVE_BEAM_STRETCH_DEPENDENT_PHASE_RESULT=$phasePath
    $env:QS3D_SOURCE_RECONCILE_NATIVE_BEAM_STRETCH_DEPENDENT_NONCE=$nonce
    $env:QS3D_SOURCE_RECONCILE_NATIVE_BEAM_STRETCH_DEPENDENT_DWG=$drawing
    $commands1 = @(
        "LOGFILEPATH",$ArtifactDir,"LOGFILEMODE","1","FILEDIA","0","CMDECHO","1","TILEMODE","1","INSUNITS","4","UCS","W","ANGBASE","0","ANGDIR","0",
        "PICKFIRST","0","PICKADD","1","PICKAUTO","1",
        "NETLOAD",('"'+$PluginDll+'"'),"QS3DDRAWBEAM","0,0","5000,0",
        "QS3DSRBEAMP04PREPARE","QS3DBEAMREBAR3D","QS3DSRBEAMP04SELECT","QS3DBEAMSTIRRUP3D","QS3DSRBEAMP04BASELINE",
        "QS3DSRBEAMP04SELECT","_.STRETCH","_C","_non","4900,-100","_non","5100,100","","_non","0,0","_non","3000,0","PICKFIRST","1","QS3DSRBEAMP04STRETCHCHECK",
        "QS3DSRBEAMP04SELECT","QS3DSYNCSOURCE","QS3DSRBEAMP04SYNCCHECK",
        "QS3DSRBEAMP04SELECT","QS3DBUILD3D","QS3DSRBEAMP04SELECT","QS3DBEAMREBAR3D","QS3DSRBEAMP04SELECT","QS3DBEAMSTIRRUP3D","QS3DSRBEAMP04FINAL",
        "QS3DSAVE","_.QSAVE","LOGFILEMODE","0","_.QUIT","_Y")
    [IO.File]::WriteAllLines($script1,$commands1,[Text.Encoding]::ASCII)
    $deadline=(Get-Date).AddSeconds($StartupTimeoutSeconds); $p1=Start-Process $bricscadExe -ArgumentList ('"'+$drawing+'" /P "'+$Profile+'" /B "'+$script1+'"') -PassThru -WindowStyle Hidden -WorkingDirectory $ArtifactDir
    Wait-Marker ([ref]$p1) $bricscadExe $phasePath $resultPath $deadline
    if (Test-Path $resultPath) { $finalMarker=Read-Qs3dMarker $resultPath; throw "P04 session one published sanitized failure marker." }
    Wait-Exit $p1 $deadline; Stop-OwnedProcess $p1; $phaseMarker=Read-Qs3dMarker $phasePath; Require-PassMarker $phaseMarker $nonce $false $false
    $persisted=((Get-FileHash $drawing -Algorithm SHA256).Hash.ToUpperInvariant() -ne $fixtureHash); if (-not $persisted) { throw "P04 drawing was not persisted." }
    $sidecarPersisted=Test-Path $sidecar -PathType Leaf; if (-not $sidecarPersisted) { throw "P04 sidecar was not persisted." }
    Remove-ExactFile $script1

    $commands2=@("LOGFILEPATH",$ArtifactDir,"LOGFILEMODE","1","FILEDIA","0","CMDECHO","1","TILEMODE","1","INSUNITS","4","UCS","W","NETLOAD",('"'+$PluginDll+'"'),"QS3DSRBEAMP04REOPEN","LOGFILEMODE","0","_.QUIT","_Y")
    [IO.File]::WriteAllLines($script2,$commands2,[Text.Encoding]::ASCII)
    $deadline=(Get-Date).AddSeconds($StartupTimeoutSeconds); $p2=Start-Process $bricscadExe -ArgumentList ('"'+$drawing+'" /P "'+$Profile+'" /B "'+$script2+'"') -PassThru -WindowStyle Hidden -WorkingDirectory $ArtifactDir
    Wait-Marker ([ref]$p2) $bricscadExe $resultPath $resultPath $deadline; Wait-Exit $p2 $deadline; Stop-OwnedProcess $p2
    $finalMarker=Read-Qs3dMarker $resultPath; Require-PassMarker $finalMarker $nonce $true $true
}
catch { $qualificationError=$_.Exception }
finally {
    try {
        Stop-OwnedProcess $p1; Stop-OwnedProcess $p2
        Wait-NoExactBricsCadProcesses -ExpectedExe $bricscadExe -Deadline (Get-Date).AddSeconds(15); $processCleanup=$true
        foreach ($path in @($script1,$script2)) { Remove-ExactFile $path }; $scriptCleanup=$true
        foreach ($path in $privateFiles) { Remove-ExactFile $path }; $privateCleanup=$true
        Copy-Item $FixtureDwg $drawing -Force; $drawingRestore=((Get-FileHash $drawing -Algorithm SHA256).Hash.ToUpperInvariant() -eq $fixtureHash); if (-not $drawingRestore) { throw "P04 drawing restore failed." }
        Remove-ExactFile $drawing; Remove-Item $fixtureRoot -Force
    } catch { $cleanupError=$_.Exception } finally { foreach ($name in $envNames) { Restore-EnvironmentValue $name $oldEnv[$name] } }
}

$metadata=[ordered]@{status=$(if($null -eq $qualificationError -and $null -eq $cleanupError){"PASS"}else{"FAIL"});qualification_boundary="LOCAL_004_P04_BEAM_DEPENDENT_STRETCH";git_sha=$gitHead;public_product_version=$publicProductVersion;exact_source_link_verified=$true;bricscad_file_version=(Get-Item $bricscadExe).VersionInfo.FileVersion;plugin_sha256=(Get-FileHash $PluginDll -Algorithm SHA256).Hash.ToUpperInvariant();core_sha256=(Get-FileHash $coreDll -Algorithm SHA256).Hash.ToUpperInvariant();plugin_pdb_sha256=(Get-FileHash $pluginPdb -Algorithm SHA256).Hash.ToUpperInvariant();core_pdb_sha256=(Get-FileHash $corePdb -Algorithm SHA256).Hash.ToUpperInvariant();repository_fixture_sha256=$fixtureHash;drawing_persisted_changed=$persisted;sidecar_persisted=$sidecarPersisted;process_cleanup_verified=$processCleanup;script_cleanup_verified=$scriptCleanup;private_state_cleanup_verified=$privateCleanup;drawing_restore_verified=$drawingRestore;phase_marker=$phaseMarker;marker=$finalMarker}
$metadata | ConvertTo-Json -Depth 5 | Set-Content $metadataPath -Encoding UTF8
if ($null -ne $cleanupError) { throw "LOCAL-004 P04 cleanup failed." }
if ($null -ne $qualificationError) { throw $qualificationError }
Write-Host "QS3D BricsCAD V25 LOCAL-004 P04 Beam STRETCH dependent redistribution PASS"
