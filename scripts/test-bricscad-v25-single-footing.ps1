param(
    [Parameter(Mandatory = $true)][string]$ProductDir,
    [Parameter(Mandatory = $true)][string]$PackageZip,
    [Parameter(Mandatory = $true)][ValidatePattern('^[0-9a-fA-F]{64}$')][string]$PackageSha256,
    [Parameter(Mandatory = $true)][ValidatePattern('^[0-9a-f]{40}$')][string]$ProductSourceSha,
    [Parameter(Mandatory = $true)][string]$ProbeDll,
    [Parameter(Mandatory = $true)][string]$ArtifactDir,
    [string]$BricsCadDir = 'C:\Program Files\Bricsys\BricsCAD V25 en_US',
    [string]$Profile = 'QS3D-V25-TEST',
    [ValidateRange(60, 600)][int]$PhaseTimeoutSeconds = 240,
    [switch]$InteractiveUi,
    [Parameter(Mandatory = $true)][switch]$ConfirmDisposableCopy
)

# LOCAL-022 bounded native qualification; -InteractiveUi adds real mouse/key
# authoring with independent host-side assertions. Neither mode tests MCP or
# claims aggregate V25/V26, private-DWG or full-DPI qualification.
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'v25-profile-sandbox.ps1')
. (Join-Path $PSScriptRoot 'bricscad-runner-window-interop.ps1')
. (Join-Path $PSScriptRoot 'local022-ui-input.ps1')

$candidates = @{
    '0fc1ced48a089267246e78fe4ceeadc36cd5a2e7' = @{
        PackageSha256 = '0d2032d4be962ab3b321abf1292bd9fd67e59ae09172cef674421bed430c2f05'
        ProductVersion = '0.1.0-preview.10307'; Kind = 'LOCAL_PR_CANDIDATE'
    }
    '0db6e659510809a6781221204a32409605c851ba' = @{
        PackageSha256 = '6b6d00de4d391e772b58780be96afab9e4b31c0d8e0246dee3d7b79a8c1c5f70'
        ProductVersion = '0.1.0-preview.10307'; Kind = 'LOCAL_PR_CANDIDATE'
    }
    '988998bd26c9d0da5915670d9b5adca14b93ecca' = @{
        PackageSha256 = '8618feb76d523337d9a9ff5900520683a5807050dcd158e27f9b8b3c4bef3771'
        ProductVersion = '0.1.0-preview.10308'; Kind = 'PUBLISHED_RELEASE'
    }
    '43130a49f49676299b865f094a9a6ded482f67ad' = @{
        PackageSha256 = '4d9869e38682674772196a3e238f115624ff357a276bb0b976000b63c9a833b5'
        ProductVersion = '0.1.0-preview.10307'; Kind = 'LOCAL_PR_CANDIDATE'
    }
}
if (-not $candidates.ContainsKey($ProductSourceSha)) { throw 'Unallocated LOCAL-022 product source.' }
$candidate = $candidates[$ProductSourceSha]
$expectedProductSourceSha = $ProductSourceSha
$expectedPackageSha256 = $candidate.PackageSha256
$expectedProductVersion = $candidate.ProductVersion

function Get-Hash([string]$Path) {
    return (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash.ToLowerInvariant()
}

function Get-StringHash([string]$Value) {
    $hasher = [Security.Cryptography.SHA256]::Create()
    try {
        return ([BitConverter]::ToString($hasher.ComputeHash([Text.Encoding]::UTF8.GetBytes($Value)))).Replace('-', '').ToLowerInvariant()
    } finally { $hasher.Dispose() }
}

function Get-Qs3dActiveTunnelProcessCount {
    $count = 0
    foreach ($record in @(Get-CimInstance -ClassName Win32_Process)) {
        $name = [string]$record.Name
        if ($name.StartsWith('tunnel-client', [StringComparison]::OrdinalIgnoreCase) -or
            $name.StartsWith('cloudflared', [StringComparison]::OrdinalIgnoreCase)) { $count++ }
    }
    return $count
}

function Write-Json([string]$Path, $Value) {
    # Refuse to overwrite any receipt from a consumed allocation.
    if (Test-Path -LiteralPath $Path) { throw 'Receipt already exists.' }
    [IO.File]::WriteAllText($Path, ($Value | ConvertTo-Json -Depth 12), [Text.UTF8Encoding]::new($false))
}

function Write-DurableJson([string]$Path, $Value, [switch]$ReplaceExisting) {
    $directory = [IO.Path]::GetDirectoryName([IO.Path]::GetFullPath($Path))
    $tempPath = Join-Path $directory ('.' + [IO.Path]::GetFileName($Path) + '.' + [Guid]::NewGuid().ToString('N') + '.tmp')
    $backupPath = $Path + '.replace-backup'
    if ($ReplaceExisting) {
        if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) { throw 'Durable receipt replacement target is missing.' }
        if (Test-Path -LiteralPath $backupPath) { throw 'Durable receipt replacement backup already exists.' }
    } elseif (Test-Path -LiteralPath $Path) {
        throw 'Durable receipt already exists.'
    }
    $bytes = [Text.UTF8Encoding]::new($false).GetBytes(($Value | ConvertTo-Json -Depth 12))
    $stream = $null
    try {
        $stream = [IO.FileStream]::new(
            $tempPath,
            [IO.FileMode]::CreateNew,
            [IO.FileAccess]::Write,
            [IO.FileShare]::None,
            4096,
            [IO.FileOptions]::WriteThrough)
        $stream.Write($bytes, 0, $bytes.Length)
        $stream.Flush($true)
        $stream.Dispose()
        $stream = $null
        if ($ReplaceExisting) {
            # The prepared receipt remains present until this same-volume atomic
            # replacement commits the exact allocated receipt.
            [IO.File]::Replace($tempPath, $Path, $backupPath, $true)
            if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) { throw 'Atomic receipt replacement did not publish a file.' }
            [IO.File]::Delete($backupPath)
        } else {
            [IO.File]::Move($tempPath, $Path)
        }
    }
    finally {
        if ($null -ne $stream) { $stream.Dispose() }
        if (Test-Path -LiteralPath $tempPath -PathType Leaf) { [IO.File]::Delete($tempPath) }
    }
}

function Test-ProfileSnapshotExact($Left, $Right) {
    if ($Left.ProfileInventorySha256 -cne $Right.ProfileInventorySha256 -or
        $Left.CurProfileExists -ne $Right.CurProfileExists) { return $false }
    [string[]]$leftNames = @($Left.ProfileNames)
    [string[]]$rightNames = @($Right.ProfileNames)
    if ($leftNames.Length -ne $rightNames.Length) { return $false }
    for ($i = 0; $i -lt $leftNames.Length; $i++) {
        if ($leftNames[$i] -cne $rightNames[$i]) { return $false }
    }
    if (-not $Left.CurProfileExists) { return $true }
    return $Left.CurProfileKind -eq $Right.CurProfileKind -and
        (Test-Qs3dRegistryValueEqual -Left $Left.CurProfileValue -Right $Right.CurProfileValue -Kind $Left.CurProfileKind)
}

function Assert-ProfileRecoveryReceipt([string]$Path, [string]$ExpectedHash, $Expected) {
    if ([string]::IsNullOrWhiteSpace($ExpectedHash) -or $null -eq $Expected) {
        throw 'Committed profile recovery identity is unavailable.'
    }
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf) -or (Get-Hash $Path) -cne $ExpectedHash) {
        throw 'Profile recovery receipt is missing or changed.'
    }
    $actual = Get-Content -LiteralPath $Path -Raw | ConvertFrom-Json
    $actualKeys = @($actual.PSObject.Properties.Name)
    $expectedKeys = @($Expected.Keys)
    if ($actualKeys.Count -ne $expectedKeys.Count) { throw 'Profile recovery receipt field count changed.' }
    for ($i = 0; $i -lt $expectedKeys.Count; $i++) {
        if ($actualKeys[$i] -cne $expectedKeys[$i]) { throw 'Profile recovery receipt schema changed.' }
    }
    if ($actual.schema -cne 'QS3D_V25_PROFILE_RECOVERY_V1' -or $actual.state -cne 'ALLOCATED' -or
        $actual.run_id -cne $runId -or $actual.source_profile -cne $Expected.source_profile -or
        $actual.nonce_prefix -cne 'QS3D-AUTO-' -or $actual.nonce_profile -cne $Expected.nonce_profile -or
        $actual.profile_inventory_before_sha256 -cne $Expected.profile_inventory_before_sha256) {
        throw 'Profile recovery receipt identity changed.'
    }
    [string[]]$actualNames = @($actual.profile_names_before)
    [string[]]$expectedNames = @($Expected.profile_names_before)
    if ($actualNames.Length -ne $expectedNames.Length) { throw 'Profile recovery inventory changed.' }
    for ($i = 0; $i -lt $actualNames.Length; $i++) {
        if ($actualNames[$i] -cne $expectedNames[$i]) { throw 'Profile recovery inventory changed.' }
    }
}

function Assert-ChildPath([string]$Root, [string]$Path) {
    $full = [IO.Path]::GetFullPath($Path)
    if (-not $full.StartsWith($Root.TrimEnd('\') + '\', [StringComparison]::OrdinalIgnoreCase)) {
        throw 'Owned path escaped its exact allocation root.'
    }
    return $full
}

function Get-ProtectedState {
    # Capture hashes only: never emit machine registration paths or credentials.
    $registry = Get-ItemProperty -LiteralPath 'HKCU:\Software\Bricsys\BricsCAD\V25x64\en_US\Applications\QS3D' -ErrorAction Stop
    $loader = [string]$registry.LOADER
    $flags = @()
    foreach ($provider in @('OpenAiSecureTunnel', 'CloudflareAccount')) {
        $flag = Join-Path ([Environment]::GetFolderPath('ApplicationData')) ('QS3D\MCP\' + $provider + '\autostart.txt')
        if (Test-Path -LiteralPath $flag) {
            if ((Get-Content -LiteralPath $flag -Raw).Trim() -ne '0') { throw 'Tunnel autostart is not paused. No launch performed.' }
            $flags += Get-Hash $flag
        } else { $flags += 'ABSENT' }
    }
    return [ordered]@{
        loader_value_sha256 = Get-StringHash ([IO.Path]::GetFullPath($loader).ToLowerInvariant())
        loader_exists = Test-Path -LiteralPath $loader -PathType Leaf
        loader_sha256 = if (Test-Path -LiteralPath $loader -PathType Leaf) { Get-Hash $loader } else { 'ABSENT' }
        load_controls = [int]$registry.LOADCTRLS
        tunnel_flags = $flags
        active_tunnel_process_count = Get-Qs3dActiveTunnelProcessCount
    }
}

function Read-Phase([string]$Phase) {
    $path = Join-Path $ArtifactDir ('phase-' + $Phase + '.json')
    $marker = Get-Content -LiteralPath $path -Raw | ConvertFrom-Json
    if ($Phase -in @('ui','uisaved','uireopen')) { return Assert-Local022UiPhase $marker $runId $Phase }
    $keys = @($marker.PSObject.Properties.Name)
    foreach ($key in @('schema', 'run_id', 'phase', 'status', 'stage', 'error_code', 'checks')) {
        if ($keys -notcontains $key) { throw 'Native marker lacks a required field.' }
    }
    if ($keys.Count -ne 7) { throw 'Native marker contains an unapproved field.' }
    if ($marker.schema -cne 'QS3D_LOCAL022_NATIVE_V1' -or $marker.run_id -cne $runId -or $marker.phase -cne $Phase) {
        throw 'Native marker identity mismatch.'
    }
    if ($marker.stage -cnotmatch '^[a-z0-9_]{1,80}$' -or $marker.error_code -cnotmatch '^[A-Z0-9_]{1,80}$') {
        throw 'Native marker diagnostic is not sanitized.'
    }
    if ($marker.status -cne 'PASS') { throw ('Native phase failed: ' + $Phase + '/' + $marker.stage + '/' + $marker.error_code) }
    if ($marker.stage -cne $Phase) { throw 'PASS marker stage mismatch.' }
    if ($marker.error_code -cne 'NONE') { throw 'PASS marker contains an error.' }
    $checks = @($marker.checks.PSObject.Properties)
    $requiredByPhase = @{
        run = @('active_disposable_drawing', 'host_major_25', 'product_location_exact', 'mcp_mutation_boundary_paused', 'meter_units',
            'box_placement', 'tapered_repeated_placement', 'solid_mass_volume_extents',
            'generated_ownership', 'family_regeneration', 'former_generated_handle_erased',
            'generic_foundation_rejected_before_mutation', 'exact_native_semantic_cardinality')
        saved = @('active_disposable_drawing', 'mcp_mutation_boundary_paused', 'sidecar_exists_after_qs3dsave',
            'native_database_still_open', 'saved_semantic_native_state', 'saved_exact_cardinality')
        reopen = @('active_disposable_drawing', 'mcp_mutation_boundary_paused', 'cold_project_bind', 'reopened_semantic_identity',
            'reopened_generated_solids_live', 'reopened_dimensions_volume_extents', 'reopened_exact_cardinality')
    }
    $required = @($requiredByPhase[$Phase] | Sort-Object)
    $actual = @($checks.Name | Sort-Object)
    if ($actual.Count -ne $required.Count -or [string]::Join([char]0, $actual) -cne [string]::Join([char]0, $required)) {
        throw 'Native marker assertion coverage mismatch.'
    }
    foreach ($check in $checks) {
        if ($check.Name -cnotmatch '^[a-z0-9_]{1,80}$' -or $check.Value -isnot [bool] -or -not $check.Value) {
            throw 'Native assertion failed or was not a Boolean.'
        }
    }
    return $marker
}

function Invoke-NativePhase([string]$Phase, [string[]]$Commands) {
    Assert-Qs3dNoBricsCadProcess
    # Recheck frozen inputs at every process boundary, including cold reopen.
    if ((Get-Hash $PackageZip) -ine $expectedPackageSha256) { throw 'Frozen candidate archive changed before launch.' }
    foreach ($entry in $packageFiles.GetEnumerator()) {
        if ((Get-Hash (Join-Path $ProductDir $entry.Key)) -cne $entry.Value) { throw 'Frozen product payload changed before launch.' }
    }
    if ((Get-Hash $ProbeDll) -cne $probeHash -or (Get-Hash $probePdb) -cne $probePdbHash) {
        throw 'Frozen probe payload changed before launch.'
    }
    if (($protectedBefore | ConvertTo-Json -Compress) -cne ((Get-ProtectedState) | ConvertTo-Json -Compress)) {
        throw 'Protected machine state changed before launch.'
    }
    $env:QS3D_LOCAL022_PHASE = $Phase
    $scriptPath = Join-Path $privateRoot ($Phase + '.scr')
    $lines = @('FILEDIA', '0', 'CMDECHO', '1', 'TILEMODE', '1', 'INSUNITS', '6', '_.UCS', '_W',
        'NETLOAD', ('"' + $pluginDll + '"'), 'NETLOAD', ('"' + $ProbeDll + '"')) + $Commands
    [IO.File]::WriteAllLines($scriptPath, $lines, [Text.Encoding]::ASCII)
    $arguments = '"' + $drawing + '" /P "' + $sandbox.NonceProfile + '" /B "' + $scriptPath + '"'
    $windowStyle = if ($InteractiveUi) { 'Maximized' } else { 'Hidden' }
    $process = Start-Process -FilePath $bricscadExe -ArgumentList $arguments -WorkingDirectory $privateRoot -PassThru -WindowStyle $windowStyle
    $ownedProcesses.Add($process)
    $launcherId = $process.Id
    $deadline = [DateTime]::UtcNow.AddSeconds($PhaseTimeoutSeconds)
    $handoff = $false
    $uiSequence = 1
    $markerPath = Join-Path $ArtifactDir ('phase-' + $Phase + '.json')
    Write-Host ('LOCAL-022 native phase started: ' + $Phase)
    while ([DateTime]::UtcNow -lt $deadline) {
        [void](Close-Qs3dProxyInformationDialog -Process $process)
        $process.Refresh()
        if ($InteractiveUi -and -not $process.HasExited -and $Phase -ceq 'ui') {
            if (Invoke-Local022UiPendingAction $ArtifactDir $runId $uiSequence $process $bricscadExe) { $uiSequence++ }
        }
        if ($process.HasExited) {
            if (Test-Path -LiteralPath $markerPath) { break }
            # BricsCAD may hand off from its launcher. Adopt only an exact child
            # with the matching host path, never an unrelated user's process.
            $children = @(Get-CimInstance Win32_Process -Filter ("Name='bricscad.exe' AND ParentProcessId=" + $launcherId) |
                Where-Object { $_.ExecutablePath -and [IO.Path]::GetFullPath($_.ExecutablePath) -ieq $bricscadExe })
            if (-not $handoff -and $children.Count -eq 1) {
                $process = Get-Process -Id $children[0].ProcessId -ErrorAction Stop
                $ownedProcesses.Add($process)
                $handoff = $true
            } elseif ($children.Count -gt 1) { throw 'Ambiguous native host handoff.' }
        }
        Start-Sleep -Milliseconds 500
    }
    $process.Refresh()
    if (-not $process.HasExited) { throw ('Native host did not exit cleanly: ' + $Phase) }
    if (-not (Wait-Qs3dNoExactBricsCadProcesses -ExpectedExecutable $bricscadExe -TimeoutSeconds 15)) {
        throw ('Exact native host remained after phase exit: ' + $Phase)
    }
    $allHostDeadline = [DateTime]::UtcNow.AddSeconds(15)
    while ([DateTime]::UtcNow -lt $allHostDeadline -and @(Get-Process -Name bricscad -ErrorAction SilentlyContinue).Count -gt 0) {
        Start-Sleep -Milliseconds 250
    }
    Assert-Qs3dNoBricsCadProcess
    $phaseMarker = Read-Phase $Phase
    Write-Host ('LOCAL-022 native phase verified: ' + $Phase)
    return $phaseMarker
}

if (-not $ConfirmDisposableCopy) { throw 'Disposable fixture authorization is required.' }
if ([Environment]::OSVersion.Platform -ne [PlatformID]::Win32NT -or -not [Environment]::UserInteractive) {
    throw 'An interactive licensed Windows V25 host is required.'
}
$repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
if ($ProductSourceSha -cne $expectedProductSourceSha -or $PackageSha256.ToLowerInvariant() -cne $expectedPackageSha256) {
    throw 'This LOCAL-022 allocation requires the exact pinned candidate ZIP.'
}
$artifactBase = Join-Path $repoRoot 'artifacts\issue-5718-local022'
$ArtifactDir = Assert-ChildPath $artifactBase $ArtifactDir
if (Test-Path -LiteralPath $ArtifactDir) { throw 'Allocation root already exists; create a fresh run identity.' }
foreach ($path in @($ArtifactDir, $ProductDir, $ProbeDll, $BricsCadDir, $Profile)) {
    if ($path -match '["\r\n]') { throw 'Unsafe native script input path.' }
}
$ProductDir = [IO.Path]::GetFullPath($ProductDir)
$PackageZip = [IO.Path]::GetFullPath($PackageZip)
$ProbeDll = [IO.Path]::GetFullPath($ProbeDll)
$BricsCadDir = [IO.Path]::GetFullPath($BricsCadDir)
$bricscadExe = Join-Path $BricsCadDir 'bricscad.exe'
$pluginDll = Join-Path $ProductDir 'QS3D.BricsCAD.V25.dll'
$coreDll = Join-Path $ProductDir 'QS3D.Core.dll'
$expectedProbe = [IO.Path]::GetFullPath((Join-Path $repoRoot 'tests\QS3D.LocalQualification.V25\bin\Release\net48\QS3D.LocalQualification.V25.dll'))
if (-not [string]::Equals($ProbeDll, $expectedProbe, [StringComparison]::OrdinalIgnoreCase)) {
    throw 'ProbeDll must be the exact repository Release build output.'
}
$fixture = Join-Path $repoRoot 'samples\generated\QS3D-Sample.dwg'
$fixtureHash = Get-Hash $fixture
if ($fixtureHash -cne 'cec1350fb2207542aeecd96a790a198a6c9cc9e99a9f875871f367554b3d967e') { throw 'Reference fixture changed.' }
if ((Get-Hash $PackageZip) -ine $PackageSha256) { throw 'Published package hash mismatch.' }
$metadata = Get-Content -LiteralPath (Join-Path $ProductDir 'PACKAGE-METADATA.json') -Raw | ConvertFrom-Json
if ($metadata.gitCommit -cne $expectedProductSourceSha -or $metadata.productVersion -cne $expectedProductVersion -or
    $metadata.target -cne 'BricsCAD V25 x64') { throw 'Published product identity mismatch.' }
& git -C $repoRoot merge-base --is-ancestor 80f609057bb95b58f08f3ea88ea22411b88cb558 $ProductSourceSha
if ($LASTEXITCODE -ne 0) { throw 'Candidate does not contain the required startup fix.' }
$packageFiles = [ordered]@{}
foreach ($line in Get-Content -LiteralPath (Join-Path $ProductDir 'SHA256SUMS.txt')) {
    if ([string]::IsNullOrWhiteSpace($line)) { continue }
    if ($line -cnotmatch '^([0-9A-Fa-f]{64})  (.+)$') { throw 'Invalid package manifest.' }
    $expectedHash = $Matches[1]
    $relative = $Matches[2]
    if ($packageFiles.Contains($relative)) { throw 'Duplicate package manifest entry.' }
    $file = Assert-ChildPath $ProductDir (Join-Path $ProductDir $relative)
    if ((Get-Hash $file) -ine $expectedHash) { throw 'Package payload hash mismatch.' }
    $packageFiles[$relative] = $expectedHash.ToLowerInvariant()
}
if (-not $packageFiles.Contains('QS3D.BricsCAD.V25.dll') -or -not $packageFiles.Contains('QS3D.Core.dll')) { throw 'Package lacks required binaries.' }
# Bind the extracted payload to the supplied immutable archive, not only to a
# self-consistent checksum file beside the DLLs.
Add-Type -AssemblyName System.IO.Compression.FileSystem
$zip = [IO.Compression.ZipFile]::OpenRead([IO.Path]::GetFullPath($PackageZip))
try {
    $archiveNames = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    foreach ($entry in $zip.Entries) {
        if ([string]::IsNullOrEmpty($entry.Name)) { continue }
        if (-not $archiveNames.Add($entry.FullName)) { throw 'Duplicate ZIP member.' }
        $extracted = Assert-ChildPath $ProductDir (Join-Path $ProductDir $entry.FullName)
        $stream = $entry.Open()
        $hasher = [Security.Cryptography.SHA256]::Create()
        try { $archiveHash = ([BitConverter]::ToString($hasher.ComputeHash($stream))).Replace('-', '').ToLowerInvariant() }
        finally { $hasher.Dispose(); $stream.Dispose() }
        if ((Get-Hash $extracted) -cne $archiveHash) { throw 'Extracted payload differs from pinned ZIP.' }
    }
    if ($archiveNames.Count -ne $packageFiles.Count + 1 -or -not $archiveNames.Contains('SHA256SUMS.txt')) { throw 'ZIP manifest coverage mismatch.' }
    if (@(Get-ChildItem -LiteralPath $ProductDir -Recurse -File).Count -ne $archiveNames.Count) { throw 'Extracted payload contains unexpected files.' }
} finally { $zip.Dispose() }
foreach ($assembly in @($pluginDll, $coreDll)) {
    if ((Get-Item -LiteralPath $assembly).VersionInfo.ProductVersion -cne $metadata.productVersion) { throw 'Product/Core version mismatch.' }
}
if ((Get-Item -LiteralPath $bricscadExe).VersionInfo.FileMajorPart -ne 25) { throw 'Wrong host major.' }
Assert-Qs3dNoBricsCadProcess
if ((Get-Qs3dActiveTunnelProcessCount) -ne 0) { throw 'Tunnels must remain stopped.' }
$protectedBefore = Get-ProtectedState
if ($protectedBefore.load_controls -ne 4) { throw 'An OnCommand loader is required for exact NETLOAD qualification.' }
$probeSource = Join-Path $repoRoot 'tests\QS3D.LocalQualification.V25\Local022NativeFootingProbeCommands.cs'
$probeProject = Join-Path $repoRoot 'tests\QS3D.LocalQualification.V25\QS3D.LocalQualification.V25.csproj'
$probePdb = [IO.Path]::ChangeExtension($ProbeDll, '.pdb')
$probeSourceHash = Get-Hash $probeSource
$probeProjectHash = Get-Hash $probeProject
$runnerHash = Get-Hash $PSCommandPath
$supplementalInputs = [ordered]@{}
foreach ($inputPath in @((Join-Path $PSScriptRoot 'local022-ui-input.ps1')) + @(Get-ChildItem (Split-Path $probeSource) -Filter '*.cs' -File | Select-Object -ExpandProperty FullName)) {
    $supplementalInputs[$inputPath] = Get-Hash $inputPath
}
$harnessSha = (& git -C $repoRoot rev-parse HEAD).Trim()
if ($LASTEXITCODE -ne 0) { throw 'Cannot read harness Git SHA.' }
$dirty = @(& git -C $repoRoot status --porcelain=v1 --untracked-files=all)
if ($LASTEXITCODE -ne 0 -or $dirty.Count -ne 0) { throw 'Freeze and commit the complete harness before native execution.' }
$dotnet = Get-Command dotnet -CommandType Application -ErrorAction Stop | Select-Object -First 1
& $dotnet.Source build $probeProject -c Release -t:Rebuild ("-p:ProductDir=" + $ProductDir) ("-p:BricsCadDir=" + $BricsCadDir)
if ($LASTEXITCODE -ne 0) { throw 'Exact committed qualification probe build failed.' }
$dirtyAfterBuild = @(& git -C $repoRoot status --porcelain=v1 --untracked-files=all)
if ($LASTEXITCODE -ne 0 -or $dirtyAfterBuild.Count -ne 0) { throw 'Probe build changed tracked harness inputs.' }
if (-not (Test-Path -LiteralPath $ProbeDll -PathType Leaf) -or -not (Test-Path -LiteralPath $probePdb -PathType Leaf)) {
    throw 'Probe build did not produce the exact Release DLL/PDB pair.'
}
$probeOutputNames = @(Get-ChildItem -LiteralPath (Split-Path -Parent $ProbeDll) -File | Select-Object -ExpandProperty Name | Sort-Object)
if ([string]::Join([char]0, $probeOutputNames) -cne [string]::Join([char]0, @('QS3D.LocalQualification.V25.dll', 'QS3D.LocalQualification.V25.pdb'))) {
    throw 'Probe output contains an unexpected payload.'
}
$probeHash = Get-Hash $ProbeDll
$probePdbHash = Get-Hash $probePdb
$dotnetVersion = [string]::Join('', @(& $dotnet.Source --version)).Trim()
if ($LASTEXITCODE -ne 0 -or $dotnetVersion -notmatch '^\d+\.\d+\.\d+') { throw 'Cannot freeze the probe compiler identity.' }
$runId = [Guid]::NewGuid().ToString('N')
$privateRoot = Join-Path $ArtifactDir 'private'
New-Item -ItemType Directory -Path $privateRoot | Out-Null
$drawing = Join-Path $privateRoot 'single-footing-copy.dwg'
$started = [DateTime]::UtcNow
$freeze = [ordered]@{
    schema = 'QS3D_LOCAL022_ALLOCATION_V1'; run_id = $runId; started_utc = $started.ToString('o')
    product_source_sha = $ProductSourceSha; product_version = $metadata.productVersion
    candidate_kind = $candidate.Kind; published_release = ($candidate.Kind -ceq 'PUBLISHED_RELEASE')
    package_sha256 = $PackageSha256.ToLowerInvariant(); package_files = $packageFiles
    probe_sha256 = $probeHash; probe_pdb_sha256 = $probePdbHash
    probe_source_sha256 = $probeSourceHash; probe_project_sha256 = $probeProjectHash; dotnet_sdk = $dotnetVersion
    runner_sha256 = $runnerHash; harness_git_sha = $harnessSha
    interactive_ui = [bool]$InteractiveUi; supplemental_input_hashes = @($supplementalInputs.Values)
    fixture_sha256 = $fixtureHash; host_version = (Get-Item -LiteralPath $bricscadExe).VersionInfo.FileVersion
    host_sha256 = Get-Hash $bricscadExe; pre_existing_host_count = 0
    mcp_test_executed = $false; mcp_requests_issued_by_runner = $false
}
$ownedProcesses = [Collections.Generic.List[Diagnostics.Process]]::new()
$envNames = @('QS3D_LOCAL022_RUN_ID', 'QS3D_LOCAL022_ROOT', 'QS3D_LOCAL022_DRAWING', 'QS3D_LOCAL022_PRODUCT_DLL',
    'QS3D_LOCAL022_PROBE_DLL', 'QS3D_LOCAL022_PHASE')
$envBefore = @{}
foreach ($name in $envNames) { $envBefore[$name] = [Environment]::GetEnvironmentVariable($name, 'Process') }
$sandbox = $null
$failure = $null
$cleanupFailure = $null
$profileReceipt = $null
$profileRecoveryPath = Join-Path $ArtifactDir 'profile-recovery.private.json'
$profileRecoveryExpected = $null
$profileRecoveryHash = $null
$profileRecoveryValidated = $false
$markers = @()
$cleanupOk = $false
$protectedStateOk = $false
$cleanupErrors = [Collections.Generic.List[string]]::new()
try {
    Copy-Item -LiteralPath $fixture -Destination $drawing
    if ((Get-Hash $drawing) -cne $fixtureHash) { throw 'Disposable copy is not exact.' }
    $profileSnapshotBefore = Get-Qs3dV25ProfileSnapshot
    $profileRecoveryPrepared = [ordered]@{
        schema = 'QS3D_V25_PROFILE_RECOVERY_V1'
        state = 'PREPARED'
        run_id = $runId
        source_profile = $Profile
        nonce_prefix = 'QS3D-AUTO-'
        nonce_profile = $null
        profile_names_before = $profileSnapshotBefore.ProfileNames
        profile_inventory_before_sha256 = $profileSnapshotBefore.ProfileInventorySha256
        cur_profile_exists = $profileSnapshotBefore.CurProfileExists
        cur_profile_kind = if ($profileSnapshotBefore.CurProfileExists) { [int]$profileSnapshotBefore.CurProfileKind } else { $null }
        cur_profile_value = $profileSnapshotBefore.CurProfileValue
    }
    # Publish a durable recovery snapshot before the first profile mutation. If
    # the process stops before allocation commits, one new nonce can still be
    # derived safely from the exact pre-allocation inventory and prefix.
    Write-DurableJson -Path $profileRecoveryPath -Value $profileRecoveryPrepared
    $sandbox = New-Qs3dV25ProfileSandbox -SourceProfile $Profile
    if (-not (Test-ProfileSnapshotExact -Left $profileSnapshotBefore -Right $sandbox.Snapshot)) {
        throw 'Profile snapshot changed across sandbox allocation.'
    }
    $profileRecoveryExpected = [ordered]@{
        schema = 'QS3D_V25_PROFILE_RECOVERY_V1'
        state = 'ALLOCATED'
        run_id = $runId
        source_profile = $sandbox.SourceProfile
        nonce_prefix = 'QS3D-AUTO-'
        nonce_profile = $sandbox.NonceProfile
        profile_names_before = $sandbox.Snapshot.ProfileNames
        profile_inventory_before_sha256 = $sandbox.Snapshot.ProfileInventorySha256
        cur_profile_exists = $sandbox.Snapshot.CurProfileExists
        cur_profile_kind = if ($sandbox.Snapshot.CurProfileExists) { [int]$sandbox.Snapshot.CurProfileKind } else { $null }
        cur_profile_value = $sandbox.Snapshot.CurProfileValue
    }
    Write-DurableJson -Path $profileRecoveryPath -Value $profileRecoveryExpected -ReplaceExisting
    $profileRecoveryHash = Get-Hash $profileRecoveryPath
    Assert-ProfileRecoveryReceipt -Path $profileRecoveryPath -ExpectedHash $profileRecoveryHash -Expected $profileRecoveryExpected
    $freeze.profile_recovery_sha256 = $profileRecoveryHash
    Write-Json (Join-Path $ArtifactDir 'allocation.json') $freeze
    $env:QS3D_LOCAL022_RUN_ID = $runId
    $env:QS3D_LOCAL022_ROOT = $ArtifactDir
    $env:QS3D_LOCAL022_DRAWING = $drawing
    $env:QS3D_LOCAL022_PRODUCT_DLL = $pluginDll
    $env:QS3D_LOCAL022_PROBE_DLL = $ProbeDll
    if ($InteractiveUi) {
        $markers += Invoke-NativePhase 'ui' @('OSMODE','0','SNAPMODE','0','DYNMODE','0','QS3D','QL22UI')
        $markers += Read-Phase 'uisaved'
    } else {
        $markers += Invoke-NativePhase 'run' @('QL22RUN', 'QS3DSAVE', '_.QSAVE', 'QL22SAVED', '_.QUIT', '_Y')
        $markers += Read-Phase 'saved'
    }
    if (-not (Test-Path -LiteralPath ([IO.Path]::ChangeExtension($drawing, '.qsdb')))) { throw 'No persisted product sidecar.' }
    if ($InteractiveUi) { $markers += Invoke-NativePhase 'uireopen' @('QL22UIREOPEN') }
    else { $markers += Invoke-NativePhase 'reopen' @('QL22REOPEN', '_.QUIT', '_Y') }
} catch {
    $failure = $_.Exception.Message
} finally {
    foreach ($process in $ownedProcesses) {
        try {
            $process.Refresh()
            if (-not $process.HasExited) {
                [void]$process.CloseMainWindow()
                if (-not $process.WaitForExit(10000)) {
                    Stop-Process -Id $process.Id -Force
                    if (-not $process.WaitForExit(10000)) { throw 'Owned native host did not exit.' }
                }
            }
        } catch { $cleanupErrors.Add('PROCESS:' + $_.Exception.Message) }
    }
    $zeroHosts = $false
    try {
        $globalHostDeadline = [DateTime]::UtcNow.AddSeconds(15)
        while ([DateTime]::UtcNow -lt $globalHostDeadline -and @(Get-Process -Name bricscad -ErrorAction SilentlyContinue).Count -gt 0) {
            Start-Sleep -Milliseconds 250
        }
        Assert-Qs3dNoBricsCadProcess
        $zeroHosts = $true
    } catch { $cleanupErrors.Add('HOST_ZERO:' + $_.Exception.Message) }
    if ($null -ne $sandbox) {
        if ($zeroHosts) {
            try {
                Assert-ProfileRecoveryReceipt -Path $profileRecoveryPath -ExpectedHash $profileRecoveryHash -Expected $profileRecoveryExpected
                $profileRecoveryValidated = $true
            } catch { $cleanupErrors.Add('PROFILE_RECOVERY_VALIDATE:' + $_.Exception.Message) }
            # Restore protected machine state even when recovery evidence was
            # altered, but fail the qualification and retain that evidence.
            try { $profileReceipt = Restore-Qs3dV25ProfileSandbox -Sandbox $sandbox }
            catch { $cleanupErrors.Add('PROFILE:' + $_.Exception.Message) }
        } else { $cleanupErrors.Add('PROFILE:SKIPPED_WHILE_HOST_ACTIVE') }
    }
    # Environment restoration is independent and must run even if host/profile
    # cleanup failed. Restore each value separately so one bad name cannot skip
    # the rest.
    foreach ($name in $envNames) {
        try { [Environment]::SetEnvironmentVariable($name, $envBefore[$name], 'Process') }
        catch { $cleanupErrors.Add('ENVIRONMENT:' + $name) }
    }
    try {
        if ((Get-Hash $fixture) -cne $fixtureHash) { throw 'Protected reference fixture changed.' }
    } catch { $cleanupErrors.Add('FIXTURE:' + $_.Exception.Message) }
    try {
        if (($protectedBefore | ConvertTo-Json -Compress) -cne ((Get-ProtectedState) | ConvertTo-Json -Compress)) {
            throw 'Protected machine state changed.'
        }
        $protectedStateOk = $true
    } catch { $cleanupErrors.Add('PROTECTED_STATE:' + $_.Exception.Message) }
    try {
        foreach ($entry in $supplementalInputs.GetEnumerator()) {
            if ((Get-Hash $entry.Key) -cne $entry.Value) { throw 'Frozen supplemental harness changed.' }
        }
        if ((Get-Hash $ProbeDll) -cne $probeHash -or (Get-Hash $PSCommandPath) -cne $runnerHash -or
            (Get-Hash $probePdb) -cne $probePdbHash -or (Get-Hash $probeSource) -cne $probeSourceHash -or
            (Get-Hash $probeProject) -cne $probeProjectHash) {
            throw 'Frozen harness changed during allocation.'
        }
    } catch { $cleanupErrors.Add('HARNESS:' + $_.Exception.Message) }
    try {
        foreach ($entry in $packageFiles.GetEnumerator()) {
            if ((Get-Hash (Join-Path $ProductDir $entry.Key)) -cne $entry.Value) { throw 'Frozen payload changed during allocation.' }
        }
    } catch { $cleanupErrors.Add('PAYLOAD:' + $_.Exception.Message) }
    if ($zeroHosts) {
        try {
            # Only this newly-created allocation's private files may be deleted.
            # Reject links; validate every absolute target before removing it.
            $entries = @(Get-ChildItem -LiteralPath $privateRoot -Recurse -Force)
            foreach ($entry in $entries) {
                [void](Assert-ChildPath $privateRoot $entry.FullName)
                if (($entry.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) { throw 'Private cleanup contains a redirected path.' }
            }
            foreach ($entry in @($entries | Where-Object { -not $_.PSIsContainer })) { Remove-Item -LiteralPath $entry.FullName -Force }
            foreach ($entry in @($entries | Where-Object PSIsContainer | Sort-Object { $_.FullName.Length } -Descending)) { Remove-Item -LiteralPath $entry.FullName }
            [void](Assert-ChildPath $ArtifactDir $privateRoot)
            Remove-Item -LiteralPath $privateRoot
            $cleanupOk = -not (Test-Path -LiteralPath $privateRoot)
        } catch { $cleanupErrors.Add('PRIVATE_ROOT:' + $_.Exception.Message) }
    } else {
        $cleanupErrors.Add('PRIVATE_ROOT:SKIPPED_WHILE_HOST_ACTIVE')
    }
    if ($null -ne $profileReceipt -and $profileRecoveryValidated) {
        try {
            Remove-Item -LiteralPath $profileRecoveryPath -Force
            if (Test-Path -LiteralPath $profileRecoveryPath) { throw 'Profile recovery receipt cleanup failed.' }
        } catch { $cleanupErrors.Add('PROFILE_RECOVERY:' + $_.Exception.Message) }
    }
    if ($cleanupErrors.Count -gt 0) {
        $cleanupFailure = [string]::Join(' | ', $cleanupErrors)
    }
}
$status = if ($null -eq $failure -and $null -eq $cleanupFailure -and $cleanupOk -and $markers.Count -eq 3) { 'LOCAL_PASS_BOUNDED' } else { 'FAIL_OR_NO_RESULT' }
$receipt = [ordered]@{
    schema = 'QS3D_LOCAL022_RECEIPT_V1'; run_id = $runId; status = $status
    product_source_sha = $ProductSourceSha; product_version = $metadata.productVersion
    candidate_kind = $candidate.Kind; published_release = ($candidate.Kind -ceq 'PUBLISHED_RELEASE')
    started_utc = $started.ToString('o'); ended_utc = [DateTime]::UtcNow.ToString('o')
    phases_verified = $markers.Count; private_cleanup_verified = $cleanupOk
    protected_state_unchanged = $protectedStateOk
    profile_cleanup = $profileReceipt; mcp_test_executed = $false; mcp_requests_issued_by_runner = $false
    aggregate_local022_qualified = $false
    interactive_ui_executed = [bool]$InteractiveUi
}
Write-Json (Join-Path $ArtifactDir 'receipt.json') $receipt
if ($status -cne 'LOCAL_PASS_BOUNDED' -or $failure -or $cleanupFailure) {
    # Diagnostics stay in ignored local artifacts, never in a public receipt.
    Write-Json (Join-Path $ArtifactDir 'diagnostics.private.json') @{ failure = $failure; cleanup_failure = $cleanupFailure }
    throw 'LOCAL-022 did not qualify. Inspect local receipts; do not retry the consumed allocation.'
}
$receipt | ConvertTo-Json -Depth 8
