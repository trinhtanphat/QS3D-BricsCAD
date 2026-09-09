param(
    [Parameter(Mandatory = $true)][string]$ProductDir,
    [Parameter(Mandatory = $true)][ValidatePattern('^[0-9a-f]{40}$')][string]$ProductSourceSha,
    [Parameter(Mandatory = $true)][string]$ArtifactDir,
    [string]$BricsCadDir = 'C:\Program Files\Bricsys\BricsCAD V25 en_US',
    [string]$Profile = 'QS3D-V25-TEST',
    [ValidateRange(60, 1200)][int]$PhaseTimeoutSeconds = 300,
    [Parameter(Mandatory = $true)][switch]$ConfirmDisposableCopy
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

# LOCAL-005 first bounded native row only:
# straight disjoint Slab regions + one rectangular hole. This runner does not
# qualify bulges, topology add/remove, corrupt ownership, cap failure or the
# Foundation command; those remain PENDING_LOCAL until separately evidenced.
if (-not $ConfirmDisposableCopy) { throw 'Disposable fixture authorization is required.' }
if ([Environment]::OSVersion.Platform -ne [PlatformID]::Win32NT -or -not [Environment]::UserInteractive) {
    throw 'An interactive licensed Windows BricsCAD V25 host is required.'
}

$repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
. (Join-Path $repoRoot 'scripts\v25-profile-sandbox.ps1')

function Get-Hash([string]$Path) { (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash.ToLowerInvariant() }
function Assert-ChildPath([string]$Root, [string]$Path) {
    $rootFull = [IO.Path]::GetFullPath($Root).TrimEnd('\') + '\'
    $full = [IO.Path]::GetFullPath($Path)
    if (-not $full.StartsWith($rootFull, [StringComparison]::OrdinalIgnoreCase)) { throw 'Owned path escaped its allocation root.' }
    $full
}
function Get-RepoRelativePath([string]$Root, [string]$Path) {
    $rootFull = [IO.Path]::GetFullPath($Root).TrimEnd('\') + '\'
    $full = [IO.Path]::GetFullPath($Path)
    if (-not $full.StartsWith($rootFull, [StringComparison]::OrdinalIgnoreCase)) { throw 'Runner path escaped repository.' }
    $full.Substring($rootFull.Length).Replace('\','/')
}
function Get-TunnelProcessCount {
    $count = 0
    foreach ($record in @(Get-CimInstance -ClassName Win32_Process)) {
        $name = [string]$record.Name
        if ($name.StartsWith('tunnel-client', [StringComparison]::OrdinalIgnoreCase) -or
            $name.StartsWith('cloudflared', [StringComparison]::OrdinalIgnoreCase)) { $count++ }
    }
    $count
}
function Assert-NoBricsCad {
    if (@(Get-Process -Name bricscad -ErrorAction SilentlyContinue).Count -ne 0) { throw 'BricsCAD must be closed before LOCAL-005 allocation.' }
}
function Set-Qs3dDemandLoadControls {
    param([string]$RegistryPath,[int]$ExpectedCurrent,[int]$NewValue)
    $current = [int](Get-ItemPropertyValue -LiteralPath $RegistryPath -Name 'LoadCtrls' -ErrorAction Stop)
    if ($current -ne $ExpectedCurrent) { throw 'QS3D DemandLoad controls changed concurrently; refusing to overwrite them.' }
    Set-ItemProperty -LiteralPath $RegistryPath -Name 'LoadCtrls' -Value $NewValue -ErrorAction Stop
    $readback = [int](Get-ItemPropertyValue -LiteralPath $RegistryPath -Name 'LoadCtrls' -ErrorAction Stop)
    if ($readback -ne $NewValue) { throw 'QS3D DemandLoad control readback did not match the guarded value.' }
}
function Restore-Qs3dDemandLoadControls {
    param([string]$RegistryPath,[int]$OriginalValue,[int]$IsolatedValue)
    $current = [int](Get-ItemPropertyValue -LiteralPath $RegistryPath -Name 'LoadCtrls' -ErrorAction Stop)
    if ($current -eq $OriginalValue) { return }
    Set-Qs3dDemandLoadControls -RegistryPath $RegistryPath -ExpectedCurrent $IsolatedValue -NewValue $OriginalValue
}
function Assert-Qs3dDemandLoadIdentity {
    param([string]$RegistryPath,[string]$ExpectedLoader,[string]$ExpectedLoaderHash)
    if (-not (Test-Path -LiteralPath $RegistryPath -PathType Container)) { throw 'QS3D DemandLoad registration disappeared during LOCAL-005 qualification.' }
    $loader = [string](Get-ItemPropertyValue -LiteralPath $RegistryPath -Name 'Loader' -ErrorAction Stop)
    if ([string]::IsNullOrWhiteSpace($loader) -or -not (Test-Path -LiteralPath $loader -PathType Leaf)) { throw 'QS3D DemandLoad loader identity is unavailable.' }
    if (-not [string]::Equals([IO.Path]::GetFullPath($loader),[IO.Path]::GetFullPath($ExpectedLoader),[StringComparison]::OrdinalIgnoreCase)) { throw 'QS3D DemandLoad loader path changed concurrently.' }
    if ((Get-Hash $loader) -cne $ExpectedLoaderHash) { throw 'QS3D DemandLoad loader bytes changed concurrently.' }
}
function Write-Json([string]$Path, $Value) {
    if (Test-Path -LiteralPath $Path) { throw 'Evidence path already exists.' }
    [IO.File]::WriteAllText($Path, ($Value | ConvertTo-Json -Depth 10), [Text.UTF8Encoding]::new($false))
}
function Read-Marker([string]$Phase, [string[]]$RequiredChecks) {
    $path = Join-Path $ArtifactDir ('phase-' + $Phase + '.json')
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { throw ('Missing native marker: ' + $Phase) }
    $marker = Get-Content -LiteralPath $path -Raw | ConvertFrom-Json
    $keys = @($marker.PSObject.Properties.Name | Sort-Object)
    $expectedKeys = @('checks','error_code','phase','run_id','schema','stage','status')
    if ([string]::Join([char]0, $keys) -cne [string]::Join([char]0, $expectedKeys)) { throw 'Native marker schema changed.' }
    if ($marker.schema -cne 'QS3D_LOCAL005_NATIVE_V1' -or $marker.run_id -cne $runId -or
        $marker.phase -cne $Phase -or $marker.stage -cne $Phase -or $marker.status -cne 'PASS' -or $marker.error_code -cne 'NONE') {
        throw ('Native marker failed: ' + $Phase + '/' + [string]$marker.error_code)
    }
    $actualChecks = @($marker.checks.PSObject.Properties | Sort-Object Name)
    $actualNames = @($actualChecks.Name)
    $required = @($RequiredChecks | Sort-Object)
    if ($actualNames.Count -ne $required.Count -or [string]::Join([char]0, $actualNames) -cne [string]::Join([char]0, $required)) {
        throw ('Native assertion coverage mismatch: ' + $Phase)
    }
    foreach ($check in $actualChecks) {
        if ($check.Value -isnot [bool] -or -not $check.Value -or $check.Name -cnotmatch '^[a-z0-9_]{1,80}$') {
            throw ('Native assertion failed: ' + $Phase + '/' + $check.Name)
        }
    }
    $marker
}

$setupChecks = @('active_disposable_drawing','host_major_25','implied_selection_seeded','meter_units','product_location_exact','single_semantic_slab_owner','synthetic_one_hole','synthetic_two_disjoint_regions')
$runChecks = @('active_disposable_drawing','exact_generated_aggregate_count','generated_manifest_present','hole_interior_excluded','native_rebar_ownership','native_region_ownership','real_product_output_present','runtime_health_no_errors','source_manifest_present','topology_fingerprint_present','two_regions_materialized')
$savedChecks = @($runChecks + @('native_database_still_open','sidecar_exists_after_qs3dsave'))
$reopenChecks = @($runChecks + @('cold_reopen_project_bind','reopened_generated_handles_live','reopened_ownership_and_topology_stable','reopened_source_handles_live'))

$ProductDir = [IO.Path]::GetFullPath($ProductDir)
$BricsCadDir = [IO.Path]::GetFullPath($BricsCadDir)
$bricscadExe = Join-Path $BricsCadDir 'bricscad.exe'
$productDll = Join-Path $ProductDir 'QS3D.BricsCAD.V25.dll'
$coreDll = Join-Path $ProductDir 'QS3D.Core.dll'
$metadataPath = Join-Path $ProductDir 'PACKAGE-METADATA.json'
foreach ($path in @($ProductDir,$BricsCadDir,$ArtifactDir,$Profile)) { if ($path -match '["\r\n]') { throw 'Unsafe LOCAL-005 input.' } }
foreach ($path in @($bricscadExe,$productDll,$coreDll,$metadataPath)) { if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { throw 'Required licensed input is missing.' } }
if ((Get-Item -LiteralPath $bricscadExe).VersionInfo.FileMajorPart -ne 25) { throw 'Wrong BricsCAD host major.' }
$metadata = Get-Content -LiteralPath $metadataPath -Raw | ConvertFrom-Json
if ($metadata.gitCommit -cne $ProductSourceSha -or $metadata.target -cne 'BricsCAD V25 x64') { throw 'Product payload identity does not match allocated source.' }

$harnessSha = (& git -C $repoRoot rev-parse HEAD).Trim()
if ($LASTEXITCODE -ne 0 -or $harnessSha -notmatch '^[0-9a-f]{40}$') { throw 'Cannot freeze harness Git SHA.' }
& git -C $repoRoot merge-base --is-ancestor $ProductSourceSha $harnessSha
if ($LASTEXITCODE -ne 0) { throw 'Product source is not an ancestor of the committed LOCAL-005 harness.' }
$dirty = @(& git -C $repoRoot status --porcelain=v1 --untracked-files=all)
if ($LASTEXITCODE -ne 0 -or $dirty.Count -ne 0) { throw 'Commit/push the complete LOCAL-005 harness before licensed execution.' }
$relativeRunner = Get-RepoRelativePath $repoRoot $PSCommandPath
& git -C $repoRoot ls-files --error-unmatch -- $relativeRunner *> $null
if ($LASTEXITCODE -ne 0) { throw 'LOCAL-005 runner is not a committed repository file.' }

$project = Join-Path $PSScriptRoot 'QS3D.LocalQualification.MultiRegion.csproj'
$source = Join-Path $PSScriptRoot 'Local005NativeMultiRegionProbeCommands.cs'
$probeDll = Join-Path $PSScriptRoot 'bin\Release\net48\QS3D.LocalQualification.MultiRegion.dll'
$dotnet = Get-Command dotnet -CommandType Application -ErrorAction Stop | Select-Object -First 1
& $dotnet.Source build $project -c Release -t:Rebuild ("-p:ProductDir=" + $ProductDir) ("-p:BricsCadDir=" + $BricsCadDir)
if ($LASTEXITCODE -ne 0) { throw 'Committed LOCAL-005 probe build failed.' }
if (-not (Test-Path -LiteralPath $probeDll -PathType Leaf)) { throw 'LOCAL-005 probe DLL missing after build.' }
if (@(& git -C $repoRoot status --porcelain=v1 --untracked-files=all).Count -ne 0) { throw 'Probe build changed tracked harness state.' }

$artifactBase = Join-Path $repoRoot 'artifacts\issue-6286-local005'
$ArtifactDir = Assert-ChildPath $artifactBase $ArtifactDir
if (Test-Path -LiteralPath $ArtifactDir) { throw 'LOCAL-005 allocation root already exists.' }
New-Item -ItemType Directory -Path $ArtifactDir | Out-Null
$privateRoot = Join-Path $ArtifactDir 'private'
New-Item -ItemType Directory -Path $privateRoot | Out-Null
$fixture = Join-Path $repoRoot 'samples\generated\QS3D-Sample.dwg'
if (-not (Test-Path -LiteralPath $fixture -PathType Leaf)) { throw 'Disposable seed DWG missing.' }
$fixtureHash = Get-Hash $fixture
$drawing = Join-Path $privateRoot 'local005-multiregion.dwg'
Copy-Item -LiteralPath $fixture -Destination $drawing
if ((Get-Hash $drawing) -cne $fixtureHash) { throw 'Disposable DWG copy differs from frozen seed.' }

Assert-NoBricsCad
if ((Get-TunnelProcessCount) -ne 0) { throw 'MCP/tunnel processes must remain stopped during LOCAL-005 native qualification.' }
$demandLoadRegistryPath = 'Registry::HKEY_CURRENT_USER\Software\Bricsys\BricsCAD\V25x64\en_US\Applications\QS3D'
$isolateDemandLoad = $false
$script:demandLoadChanged = $false
$script:demandLoadRestored = $true
$demandLoadOriginalControls = 0
$demandLoadIsolatedControls = 0
$demandLoadLoader = ''
$demandLoadLoaderHash = ''
if (Test-Path -LiteralPath $demandLoadRegistryPath -PathType Container) {
    $demandLoadOriginalControls = [int](Get-ItemPropertyValue -LiteralPath $demandLoadRegistryPath -Name 'LoadCtrls' -ErrorAction Stop)
    if ($demandLoadOriginalControls -ne 2 -and $demandLoadOriginalControls -ne 4) { throw 'LOCAL-005 only supports canonical QS3D DemandLoad controls 2 or 4.' }
    $demandLoadLoader = [string](Get-ItemPropertyValue -LiteralPath $demandLoadRegistryPath -Name 'Loader' -ErrorAction Stop)
    if ([string]::IsNullOrWhiteSpace($demandLoadLoader) -or -not (Test-Path -LiteralPath $demandLoadLoader -PathType Leaf)) { throw 'Installed QS3D DemandLoad loader is missing.' }
    $demandLoadLoader = [IO.Path]::GetFullPath($demandLoadLoader)
    $demandLoadLoaderHash = Get-Hash $demandLoadLoader
    if (($demandLoadOriginalControls -band 2) -ne 0) {
        $demandLoadIsolatedControls = [int](($demandLoadOriginalControls -band (-bnot 2)) -bor 4)
        $isolateDemandLoad = $true
    } else {
        $demandLoadIsolatedControls = $demandLoadOriginalControls
    }
}
$productHash = Get-Hash $productDll
$coreHash = Get-Hash $coreDll
$probeHash = Get-Hash $probeDll
$runnerHash = Get-Hash $PSCommandPath
$sourceHash = Get-Hash $source
$projectHash = Get-Hash $project
$runId = [Guid]::NewGuid().ToString('N')
$started = [DateTime]::UtcNow
$envNames = @('QS3D_LOCAL005_RUN_ID','QS3D_LOCAL005_ROOT','QS3D_LOCAL005_DRAWING','QS3D_LOCAL005_PRODUCT_DLL','QS3D_LOCAL005_PROBE_DLL')
$envBefore = @{}
foreach ($name in $envNames) { $envBefore[$name] = [Environment]::GetEnvironmentVariable($name, 'Process') }
$env:QS3D_LOCAL005_RUN_ID = $runId
$env:QS3D_LOCAL005_ROOT = $ArtifactDir
$env:QS3D_LOCAL005_DRAWING = $drawing
$env:QS3D_LOCAL005_PRODUCT_DLL = $productDll
$env:QS3D_LOCAL005_PROBE_DLL = $probeDll
$sandbox = $null
$owned = [Collections.Generic.List[Diagnostics.Process]]::new()
$failure = $null
$cleanupFailure = $null
$profileReceipt = $null
$cleanupOk = $false

function Invoke-HostPhase([string]$Phase, [string[]]$Commands, [string[]]$ExpectedMarkers) {
    Assert-NoBricsCad
    if ($isolateDemandLoad) {
        Assert-Qs3dDemandLoadIdentity -RegistryPath $demandLoadRegistryPath -ExpectedLoader $demandLoadLoader -ExpectedLoaderHash $demandLoadLoaderHash
        Set-Qs3dDemandLoadControls -RegistryPath $demandLoadRegistryPath -ExpectedCurrent $demandLoadOriginalControls -NewValue $demandLoadIsolatedControls
        $script:demandLoadChanged = $true
        $script:demandLoadRestored = $false
    }
    if ((Get-Hash $productDll) -cne $productHash -or (Get-Hash $coreDll) -cne $coreHash -or
        (Get-Hash $probeDll) -cne $probeHash -or (Get-Hash $PSCommandPath) -cne $runnerHash -or
        (Get-Hash $source) -cne $sourceHash -or (Get-Hash $project) -cne $projectHash) { throw 'Frozen LOCAL-005 inputs changed before process boundary.' }
    $scriptPath = Join-Path $privateRoot ($Phase + '.scr')
    $lines = @('FILEDIA','0','CMDECHO','1','TILEMODE','1','INSUNITS','6','_.UCS','_W',
        'NETLOAD',('"' + $productDll + '"'),'NETLOAD',('"' + $probeDll + '"')) + $Commands
    [IO.File]::WriteAllLines($scriptPath, $lines, [Text.Encoding]::ASCII)
    $arguments = '"' + $drawing + '" /P "' + $sandbox.NonceProfile + '" /B "' + $scriptPath + '"'
    $process = Start-Process -FilePath $bricscadExe -ArgumentList $arguments -WorkingDirectory $privateRoot -PassThru -WindowStyle Hidden
    $owned.Add($process)
    $launcherId = $process.Id
    $handoff = $false
    $deadline = [DateTime]::UtcNow.AddSeconds($PhaseTimeoutSeconds)
    while ([DateTime]::UtcNow -lt $deadline) {
        $process.Refresh()
        $complete = $true
        foreach ($marker in $ExpectedMarkers) { if (-not (Test-Path -LiteralPath (Join-Path $ArtifactDir ('phase-' + $marker + '.json')))) { $complete = $false } }
        if ($process.HasExited) {
            if ($complete) { break }
            $children = @(Get-CimInstance Win32_Process -Filter ("Name='bricscad.exe' AND ParentProcessId=" + $launcherId) |
                Where-Object { $_.ExecutablePath -and [IO.Path]::GetFullPath($_.ExecutablePath) -ieq $bricscadExe })
            if (-not $handoff -and $children.Count -eq 1) {
                $process = Get-Process -Id $children[0].ProcessId -ErrorAction Stop
                $owned.Add($process)
                $handoff = $true
                continue
            }
            if ($children.Count -gt 1) { throw ('Ambiguous BricsCAD launcher handoff: ' + $Phase) }
            throw ('BricsCAD exited before expected LOCAL-005 marker(s): ' + $Phase)
        }
        Start-Sleep -Milliseconds 400
    }
    $process.Refresh()
    if (-not $process.HasExited) { throw ('BricsCAD phase timed out: ' + $Phase) }
    $zeroDeadline = [DateTime]::UtcNow.AddSeconds(15)
    while ([DateTime]::UtcNow -lt $zeroDeadline -and @(Get-Process -Name bricscad -ErrorAction SilentlyContinue).Count -gt 0) { Start-Sleep -Milliseconds 250 }
    Assert-NoBricsCad
    if ($script:demandLoadChanged) {
        Restore-Qs3dDemandLoadControls -RegistryPath $demandLoadRegistryPath -OriginalValue $demandLoadOriginalControls -IsolatedValue $demandLoadIsolatedControls
        Assert-Qs3dDemandLoadIdentity -RegistryPath $demandLoadRegistryPath -ExpectedLoader $demandLoadLoader -ExpectedLoaderHash $demandLoadLoaderHash
        $script:demandLoadChanged = $false
        $script:demandLoadRestored = $true
    }
}

try {
    $sandbox = New-Qs3dV25ProfileSandbox -SourceProfile $Profile
    $allocation = [ordered]@{
        schema='QS3D_LOCAL005_ALLOCATION_V1'; run_id=$runId; product_source_sha=$ProductSourceSha; harness_git_sha=$harnessSha
        product_sha256=$productHash; core_sha256=$coreHash; probe_sha256=$probeHash; runner_sha256=$runnerHash
        synthetic_fixture='two_disjoint_rectangles_plus_one_rectangular_hole'; fixture_seed_sha256=$fixtureHash
        host_major=25; profile_sandbox='NONCE_COPY'; mcp_test_executed=$false; tunnel_processes=0; startup_demandload_isolated=$isolateDemandLoad
    }
    Write-Json (Join-Path $ArtifactDir 'allocation.json') $allocation

    # Real public production mutation command sits between test-only setup and verification.
    Invoke-HostPhase 'run' @(
        'QL005SETUP',
        'PICKFIRST','1',
        '_.SELECT','_W','-1,-1','23,9','',
        'QS3DSLABREBAR3DMULTI',
        'QL005DUMPEX',
        'QL005VERIFY',
        'QS3DMULTIREBARHEALTH',
        'QS3DSAVE',
        '_.QSAVE',
        'QL005SAVED',
        '_.QUIT','_Y'
    ) @('setup','run','saved')
    $null = Read-Marker 'setup' $setupChecks
    $null = Read-Marker 'run' $runChecks
    $null = Read-Marker 'saved' $savedChecks
    if (-not (Test-Path -LiteralPath ([IO.Path]::ChangeExtension($drawing, '.qsdb')) -PathType Leaf)) { throw 'Product sidecar missing after QS3DSAVE.' }

    # New BricsCAD process + same disposable DWG/sidecar = cold reopen boundary.
    Invoke-HostPhase 'reopen' @('QL005REOPEN','_.QUIT','_N') @('reopen')
    $null = Read-Marker 'reopen' $reopenChecks
} catch {
    $failure = $_.Exception.Message
} finally {
    foreach ($process in $owned) {
        try {
            $process.Refresh()
            if (-not $process.HasExited) {
                [void]$process.CloseMainWindow()
                if (-not $process.WaitForExit(10000)) {
                    Stop-Process -Id $process.Id -Force
                    if (-not $process.WaitForExit(10000)) { throw 'Owned BricsCAD process did not exit.' }
                }
            }
        } catch { if ($null -eq $cleanupFailure) { $cleanupFailure = 'owned_process_cleanup_failed' } }
    }
    $zeroHosts = $false
    try {
        $deadline = [DateTime]::UtcNow.AddSeconds(15)
        while ([DateTime]::UtcNow -lt $deadline -and @(Get-Process -Name bricscad -ErrorAction SilentlyContinue).Count -gt 0) { Start-Sleep -Milliseconds 250 }
        Assert-NoBricsCad
        $zeroHosts = $true
    } catch { if ($null -eq $cleanupFailure) { $cleanupFailure = 'host_zero_cleanup_failed' } }
    if ($script:demandLoadChanged) {
        if ($zeroHosts) {
            try {
                Restore-Qs3dDemandLoadControls -RegistryPath $demandLoadRegistryPath -OriginalValue $demandLoadOriginalControls -IsolatedValue $demandLoadIsolatedControls
                Assert-Qs3dDemandLoadIdentity -RegistryPath $demandLoadRegistryPath -ExpectedLoader $demandLoadLoader -ExpectedLoaderHash $demandLoadLoaderHash
                $script:demandLoadChanged = $false
                $script:demandLoadRestored = $true
            } catch { if ($null -eq $cleanupFailure) { $cleanupFailure = 'demandload_restore_failed' } }
        } elseif ($null -eq $cleanupFailure) {
            $cleanupFailure = 'demandload_restore_skipped_host_active'
        }
    } elseif ($zeroHosts -and -not [string]::IsNullOrWhiteSpace($demandLoadLoader)) {
        try {
            Assert-Qs3dDemandLoadIdentity -RegistryPath $demandLoadRegistryPath -ExpectedLoader $demandLoadLoader -ExpectedLoaderHash $demandLoadLoaderHash
            $controls = [int](Get-ItemPropertyValue -LiteralPath $demandLoadRegistryPath -Name 'LoadCtrls' -ErrorAction Stop)
            if ($controls -ne $demandLoadOriginalControls) { throw 'QS3D DemandLoad controls were not restored.' }
        } catch { if ($null -eq $cleanupFailure) { $cleanupFailure = 'demandload_final_state_changed' } }
    }
    if ($null -ne $sandbox) {
        if ($zeroHosts) {
            try { $profileReceipt = Restore-Qs3dV25ProfileSandbox -Sandbox $sandbox }
            catch { if ($null -eq $cleanupFailure) { $cleanupFailure = 'profile_restore_failed' } }
        } elseif ($null -eq $cleanupFailure) {
            $cleanupFailure = 'profile_restore_skipped_host_active'
        }
    }
    foreach ($name in $envNames) {
        try { [Environment]::SetEnvironmentVariable($name, $envBefore[$name], 'Process') }
        catch { if ($null -eq $cleanupFailure) { $cleanupFailure = 'environment_restore_failed' } }
    }
    if ($zeroHosts) {
        try {
            if ((Get-Hash $fixture) -cne $fixtureHash -or (Get-Hash $productDll) -cne $productHash -or (Get-Hash $coreDll) -cne $coreHash) { throw 'Frozen input changed.' }
            if (Test-Path -LiteralPath $privateRoot) { Remove-Item -LiteralPath $privateRoot -Recurse -Force }
            $cleanupOk = -not (Test-Path -LiteralPath $privateRoot)
            if (-not $cleanupOk) { throw 'Private root still exists.' }
        } catch { if ($null -eq $cleanupFailure) { $cleanupFailure = 'private_cleanup_failed' } }
    } elseif ($null -eq $cleanupFailure) {
        $cleanupFailure = 'private_cleanup_skipped_host_active'
    }
}

$status = if ($null -eq $failure -and $null -eq $cleanupFailure -and $cleanupOk) { 'LOCAL_PASS_BOUNDED' } else { 'FAIL_OR_NO_RESULT' }
$receipt = [ordered]@{
    schema='QS3D_LOCAL005_RECEIPT_V1'; run_id=$runId; status=$status; product_source_sha=$ProductSourceSha; harness_git_sha=$harnessSha
    scenario='SLAB_STRAIGHT_DISJOINT_PLUS_HOLE'; phases_verified=4
    production_commands=@('QS3DSLABREBAR3DMULTI','QS3DMULTIREBARHEALTH','QS3DSAVE')
    geometry_verified=($status -ceq 'LOCAL_PASS_BOUNDED'); ownership_verified=($status -ceq 'LOCAL_PASS_BOUNDED')
    save_reopen_verified=($status -ceq 'LOCAL_PASS_BOUNDED'); private_cleanup_verified=$cleanupOk
    zero_bricscad_processes=(@(Get-Process -Name bricscad -ErrorAction SilentlyContinue).Count -eq 0)
    profile_restored=($null -ne $profileReceipt); demandload_isolated=$isolateDemandLoad; demandload_restored=$script:demandLoadRestored; mcp_test_executed=$false
    pending_rows=@('SLAB_BULGE','SLAB_ADD_REMOVE_REGION','SLAB_CORRUPT_OWNERSHIP','SLAB_CAP_FAIL_CLOSED','FOUNDATION_STRAIGHT_DISJOINT_PLUS_HOLE')
    started_utc=$started.ToString('o'); ended_utc=[DateTime]::UtcNow.ToString('o')
}
Write-Json (Join-Path $ArtifactDir 'receipt.json') $receipt
if ($status -cne 'LOCAL_PASS_BOUNDED') {
    Write-Json (Join-Path $ArtifactDir 'diagnostics.private.json') @{ failure=$failure; cleanup_failure=$cleanupFailure }
    throw 'LOCAL-005 native qualification did not pass; inspect private diagnostics and do not claim licensed evidence.'
}
$receipt | ConvertTo-Json -Depth 8
