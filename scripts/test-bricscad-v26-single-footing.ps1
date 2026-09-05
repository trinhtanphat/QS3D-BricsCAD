param(
    [Parameter(Mandatory = $true)][string]$ProductDir,
    [Parameter(Mandatory = $true)][string]$PackageZip,
    [Parameter(Mandatory = $true)][ValidatePattern('^[0-9a-fA-F]{64}$')][string]$PackageSha256,
    [Parameter(Mandatory = $true)][string]$ProvenancePath,
    [Parameter(Mandatory = $true)][ValidatePattern('^[0-9a-f]{40}$')][string]$ProductSourceSha,
    [Parameter(Mandatory = $true)][string]$ProbeDll,
    [Parameter(Mandatory = $true)][string]$ArtifactDir,
    [string]$BricsCadDir = 'C:\Program Files\Bricsys\BricsCAD V26 en_US',
    [string]$Profile = 'QS3D-V26-TEST',
    [ValidateRange(60, 600)][int]$PhaseTimeoutSeconds = 240,
    [switch]$InteractiveUi,
    [Parameter(Mandatory = $true)][switch]$ConfirmDisposableCopy
)

# LOCAL-022 bounded native qualification; -InteractiveUi adds real mouse/key
# authoring with independent host-side assertions. Neither mode tests MCP or
# claims aggregate V25/V26, private-DWG or full-DPI qualification.
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'bricscad-runner-window-interop.ps1')
. (Join-Path $PSScriptRoot 'local022-ui-input.ps1')

$script:Qs3dV26ProfilesRegistryPath = 'Software\Bricsys\BricsCAD\V26x64\en_US\Profiles'
$script:Qs3dV26NoncePrefix = 'QS3D-AUTO-'

function Assert-Qs3dNoBricsCadProcess {
    $existing = @(Get-Process -Name 'bricscad' -ErrorAction SilentlyContinue)
    if ($existing.Count -gt 0) {
        throw 'Close existing BricsCAD processes before capturing or restoring the V26 profile sandbox.'
    }
}

function Open-Qs3dV26ProfilesRegistryKey {
    param([switch]$Writable)

    $key = [Microsoft.Win32.Registry]::CurrentUser.OpenSubKey(
        $script:Qs3dV26ProfilesRegistryPath,
        [bool]$Writable)
    if ($null -eq $key) {
        throw "BricsCAD V26 profile registry root is missing: HKCU:\$($script:Qs3dV26ProfilesRegistryPath)"
    }
    return $key
}

function Get-Qs3dSortedProfileNames {
    param([Parameter(Mandatory = $true)][Microsoft.Win32.RegistryKey]$ProfilesKey)

    [string[]]$names = @($ProfilesKey.GetSubKeyNames())
    [Array]::Sort($names, [StringComparer]::Ordinal)
    return $names
}

function Get-Qs3dStringArrayHash {
    param([Parameter(Mandatory = $true)][string[]]$Values)

    $payload = [Text.Encoding]::UTF8.GetBytes([string]::Join([char]0, $Values))
    $sha = [Security.Cryptography.SHA256]::Create()
    try {
        return ([BitConverter]::ToString($sha.ComputeHash($payload))).Replace('-', '').ToLowerInvariant()
    }
    finally {
        $sha.Dispose()
    }
}

function Copy-Qs3dRegistryTree {
    param(
        [Parameter(Mandatory = $true)][Microsoft.Win32.RegistryKey]$Source,
        [Parameter(Mandatory = $true)][Microsoft.Win32.RegistryKey]$Destination
    )

    foreach ($valueName in $Source.GetValueNames()) {
        $value = $Source.GetValue(
            $valueName,
            $null,
            [Microsoft.Win32.RegistryValueOptions]::DoNotExpandEnvironmentNames)
        $kind = $Source.GetValueKind($valueName)
        $Destination.SetValue($valueName, $value, $kind)
    }

    foreach ($subKeyName in $Source.GetSubKeyNames()) {
        $sourceChild = $null
        $destinationChild = $null
        try {
            $sourceChild = $Source.OpenSubKey($subKeyName, $false)
            if ($null -eq $sourceChild) {
                throw "Could not open source profile subkey '$subKeyName'."
            }
            $destinationChild = $Destination.CreateSubKey($subKeyName)
            if ($null -eq $destinationChild) {
                throw "Could not create runner-owned profile subkey '$subKeyName'."
            }
            Copy-Qs3dRegistryTree -Source $sourceChild -Destination $destinationChild
        }
        finally {
            if ($null -ne $destinationChild) { $destinationChild.Dispose() }
            if ($null -ne $sourceChild) { $sourceChild.Dispose() }
        }
    }
}

function Test-Qs3dRegistryValueEqual {
    param(
        [Parameter(Mandatory = $true)]$Left,
        [Parameter(Mandatory = $true)]$Right,
        [Parameter(Mandatory = $true)][Microsoft.Win32.RegistryValueKind]$Kind
    )

    if ($Kind -eq [Microsoft.Win32.RegistryValueKind]::Binary) {
        [byte[]]$leftBytes = @($Left)
        [byte[]]$rightBytes = @($Right)
        if ($leftBytes.Length -ne $rightBytes.Length) { return $false }
        for ($i = 0; $i -lt $leftBytes.Length; $i++) {
            if ($leftBytes[$i] -ne $rightBytes[$i]) { return $false }
        }
        return $true
    }
    if ($Kind -eq [Microsoft.Win32.RegistryValueKind]::MultiString) {
        [string[]]$leftStrings = @($Left)
        [string[]]$rightStrings = @($Right)
        if ($leftStrings.Length -ne $rightStrings.Length) { return $false }
        for ($i = 0; $i -lt $leftStrings.Length; $i++) {
            if (-not [string]::Equals($leftStrings[$i], $rightStrings[$i], [StringComparison]::Ordinal)) {
                return $false
            }
        }
        return $true
    }
    return [object]::Equals($Left, $Right)
}

function Get-Qs3dV26ProfileSnapshot {
    Assert-Qs3dNoBricsCadProcess

    $profiles = Open-Qs3dV26ProfilesRegistryKey
    try {
        [string[]]$profileNames = Get-Qs3dSortedProfileNames -ProfilesKey $profiles
        $valueNames = @($profiles.GetValueNames())
        $curProfileExists = $valueNames -contains 'CurProfile'
        $curProfileKind = $null
        $curProfileValue = $null
        if ($curProfileExists) {
            $curProfileKind = $profiles.GetValueKind('CurProfile')
            $curProfileValue = $profiles.GetValue(
                'CurProfile',
                $null,
                [Microsoft.Win32.RegistryValueOptions]::DoNotExpandEnvironmentNames)
        }
        return [pscustomobject]@{
            ProfileNames = $profileNames
            ProfileInventorySha256 = Get-Qs3dStringArrayHash -Values $profileNames
            CurProfileExists = $curProfileExists
            CurProfileKind = $curProfileKind
            CurProfileValue = $curProfileValue
        }
    }
    finally {
        $profiles.Dispose()
    }
}

function Test-Qs3dProfileName {
    param([Parameter(Mandatory = $true)][string]$Name)

    if ([string]::IsNullOrWhiteSpace($Name) -or
        -not [string]::Equals($Name, $Name.Trim(), [StringComparison]::Ordinal) -or
        $Name.Length -gt 128 -or
        $Name -match '[\\/\x00-\x1f\x7f]') {
        throw 'V26 profile names must be canonical nonblank names without path separators, controls, or surrounding whitespace.'
    }
}

function New-Qs3dV26ProfileSandbox {
    param([Parameter(Mandatory = $true)][string]$SourceProfile)

    if ([Environment]::OSVersion.Platform -ne [PlatformID]::Win32NT) {
        throw 'The V26 profile sandbox requires Windows.'
    }
    Test-Qs3dProfileName -Name $SourceProfile
    $snapshot = Get-Qs3dV26ProfileSnapshot

    $profiles = Open-Qs3dV26ProfilesRegistryKey -Writable
    $source = $null
    $destination = $null
    $nonceName = $null
    try {
        $source = $profiles.OpenSubKey($SourceProfile, $false)
        if ($null -eq $source) {
            throw "Requested BricsCAD V26 profile does not exist: $SourceProfile"
        }

        for ($attempt = 0; $attempt -lt 16; $attempt++) {
            $candidate = $script:Qs3dV26NoncePrefix + ([Guid]::NewGuid().ToString('N'))
            if ($snapshot.ProfileNames -contains $candidate) { continue }
            $collision = $profiles.OpenSubKey($candidate, $false)
            if ($null -ne $collision) {
                $collision.Dispose()
                continue
            }
            $destination = $profiles.CreateSubKey($candidate)
            if ($null -eq $destination) { continue }
            $nonceName = $candidate
            break
        }
        if ([string]::IsNullOrWhiteSpace($nonceName)) {
            throw 'Could not allocate a unique runner-owned BricsCAD V26 profile name.'
        }

        try {
            Copy-Qs3dRegistryTree -Source $source -Destination $destination
        }
        catch {
            $destination.Dispose()
            $destination = $null
            $profiles.DeleteSubKeyTree($nonceName, $false)
            throw
        }

        return [pscustomobject]@{
            SourceProfile = $SourceProfile
            NonceProfile = $nonceName
            Snapshot = $snapshot
        }
    }
    finally {
        if ($null -ne $destination) { $destination.Dispose() }
        if ($null -ne $source) { $source.Dispose() }
        $profiles.Dispose()
    }
}

function Restore-Qs3dV26ProfileSandbox {
    param([Parameter(Mandatory = $true)]$Sandbox)

    Assert-Qs3dNoBricsCadProcess

    $snapshot = $Sandbox.Snapshot
    $nonceName = [string]$Sandbox.NonceProfile
    if ([string]::IsNullOrWhiteSpace($nonceName) -or
        -not $nonceName.StartsWith($script:Qs3dV26NoncePrefix, [StringComparison]::Ordinal) -or
        ($snapshot.ProfileNames -contains $nonceName)) {
        throw 'Refusing to delete a V26 profile that is not proven runner-owned.'
    }

    $profiles = Open-Qs3dV26ProfilesRegistryKey -Writable
    try {
        # Restore the protected pointer first. If this fails, the nonce profile remains
        # available rather than leaving CurProfile pointing at a deleted profile.
        if ($snapshot.CurProfileExists) {
            $profiles.SetValue('CurProfile', $snapshot.CurProfileValue, $snapshot.CurProfileKind)
        }
        elseif (@($profiles.GetValueNames()) -contains 'CurProfile') {
            $profiles.DeleteValue('CurProfile', $false)
        }

        # Delete only the runner-owned nonce after the original pointer is safe.
        $nonceKey = $profiles.OpenSubKey($nonceName, $false)
        if ($null -ne $nonceKey) {
            $nonceKey.Dispose()
            $profiles.DeleteSubKeyTree($nonceName, $false)
        }
    }
    finally {
        $profiles.Dispose()
    }

    $after = Get-Qs3dV26ProfileSnapshot
    $beforeNames = [string[]]$snapshot.ProfileNames
    $afterNames = [string[]]$after.ProfileNames
    $inventoryMatches = $beforeNames.Length -eq $afterNames.Length
    if ($inventoryMatches) {
        for ($i = 0; $i -lt $beforeNames.Length; $i++) {
            if (-not [string]::Equals($beforeNames[$i], $afterNames[$i], [StringComparison]::Ordinal)) {
                $inventoryMatches = $false
                break
            }
        }
    }
    $curProfileMatches = $snapshot.CurProfileExists -eq $after.CurProfileExists
    if ($curProfileMatches -and $snapshot.CurProfileExists) {
        $curProfileMatches = ($snapshot.CurProfileKind -eq $after.CurProfileKind) -and
            (Test-Qs3dRegistryValueEqual -Left $snapshot.CurProfileValue -Right $after.CurProfileValue -Kind $snapshot.CurProfileKind)
    }

    if (-not $inventoryMatches -or -not $curProfileMatches) {
        throw 'BricsCAD V26 profile sandbox cleanup could not restore the exact protected profile boundary.'
    }

    return [pscustomobject]@{
        zero_bricscad_processes = $true
        cur_profile_restored = $true
        profile_inventory_restored = $true
        nonce_profile_removed = $true
        profile_inventory_before_sha256 = $snapshot.ProfileInventorySha256
        profile_inventory_after_sha256 = $after.ProfileInventorySha256
    }
}

$expectedProductSourceSha = '43130a49f49676299b865f094a9a6ded482f67ad'
$expectedPackageSha256 = '7dbf9216e873f2e20c2fae5011785148e9feded944a7b43233b4710b331fd2c5'

function Assert-Qs3dV26InstalledDesktopRuntime {
    param([AllowNull()][string]$ExpectedRuntimeVersion)

    $programFiles = [Environment]::GetFolderPath([Environment+SpecialFolder]::ProgramFiles)
    if ([string]::IsNullOrWhiteSpace($programFiles)) {
        throw "BricsCAD V26 managed bridge requires the system-installed x64 .NET 8 Windows Desktop Runtime; DOTNET_ROOT alone is insufficient."
    }

    $installedRoot = Join-Path $programFiles "dotnet"
    $coreRoot = Join-Path $installedRoot "shared\Microsoft.NETCore.App"
    $desktopRoot = Join-Path $installedRoot "shared\Microsoft.WindowsDesktop.App"
    $installedCore8 = @()
    $installedDesktop8 = @()
    if (Test-Path -LiteralPath $coreRoot -PathType Container) {
        $installedCore8 = @(Get-ChildItem -LiteralPath $coreRoot -Directory -ErrorAction Stop | Where-Object {
            $_.Name -match '^8\.' -and (Test-Path -LiteralPath (Join-Path $_.FullName "coreclr.dll") -PathType Leaf)
        })
    }
    if (Test-Path -LiteralPath $desktopRoot -PathType Container) {
        $installedDesktop8 = @(Get-ChildItem -LiteralPath $desktopRoot -Directory -ErrorAction Stop | Where-Object {
            $_.Name -match '^8\.' -and
            (Test-Path -LiteralPath (Join-Path $_.FullName "WindowsBase.dll") -PathType Leaf) -and
            (Test-Path -LiteralPath (Join-Path $_.FullName "System.Windows.Forms.dll") -PathType Leaf)
        })
    }

    $matchingVersions = @($installedCore8 | Where-Object {
        $coreVersion = $_.Name
        $installedDesktop8.Name -contains $coreVersion
    })
    if (-not [string]::IsNullOrWhiteSpace($ExpectedRuntimeVersion)) {
        $matchingVersions = @($matchingVersions | Where-Object { $_.Name -eq $ExpectedRuntimeVersion })
    }
    if ($matchingVersions.Count -eq 0) {
        $expectedLabel = if ([string]::IsNullOrWhiteSpace($ExpectedRuntimeVersion)) { "an x64 8.x patch" } else { "x64 $ExpectedRuntimeVersion" }
        throw "BricsCAD V26 managed bridge requires the system-installed .NET 8 Windows Desktop Runtime ($expectedLabel) under '$installedRoot'; DOTNET_ROOT alone is insufficient."
    }
}

function Assert-Qs3dV26DotNetRoot {
    $configured = [Environment]::GetEnvironmentVariable("DOTNET_ROOT", "Process")
    $configuredX64 = [Environment]::GetEnvironmentVariable("DOTNET_ROOT_X64", "Process")
    if (-not [string]::IsNullOrWhiteSpace($configuredX64)) {
        if (-not [string]::IsNullOrWhiteSpace($configured) -and
            -not [string]::Equals([IO.Path]::GetFullPath($configured.Trim()).TrimEnd('\'),
                [IO.Path]::GetFullPath($configuredX64.Trim()).TrimEnd('\'), [StringComparison]::OrdinalIgnoreCase)) {
            throw 'DOTNET_ROOT and DOTNET_ROOT_X64 must identify the same validated x64 runtime root.'
        }
        $configured = $configuredX64
    }
    if ([string]::IsNullOrWhiteSpace($configured)) {
        Assert-Qs3dV26InstalledDesktopRuntime
        return
    }
    try { $root = [IO.Path]::GetFullPath($configured.Trim()) }
    catch { throw "DOTNET_ROOT is set but is not a valid absolute directory." }

    $dotnet = Join-Path $root "dotnet.exe"
    $fxrRoot = Join-Path $root "host\fxr"
    $runtimeRoot = Join-Path $root "shared\Microsoft.NETCore.App"
    $desktopRoot = Join-Path $root "shared\Microsoft.WindowsDesktop.App"
    if (-not (Test-Path -LiteralPath $root -PathType Container) -or
        -not (Test-Path -LiteralPath $dotnet -PathType Leaf) -or
        -not (Test-Path -LiteralPath $fxrRoot -PathType Container) -or
        -not (Test-Path -LiteralPath $runtimeRoot -PathType Container) -or
        -not (Test-Path -LiteralPath $desktopRoot -PathType Container)) {
        throw "DOTNET_ROOT is set but does not contain a complete .NET 8 WindowsDesktop host/runtime."
    }

    $fxr8 = @(Get-ChildItem -LiteralPath $fxrRoot -Directory -ErrorAction Stop | Where-Object {
        $_.Name -match '^8\.' -and (Test-Path -LiteralPath (Join-Path $_.FullName "hostfxr.dll") -PathType Leaf)
    })
    $runtime8 = @(Get-ChildItem -LiteralPath $runtimeRoot -Directory -ErrorAction Stop | Where-Object {
        $_.Name -match '^8\.' -and (Test-Path -LiteralPath (Join-Path $_.FullName "coreclr.dll") -PathType Leaf)
    })
    $desktop8 = @(Get-ChildItem -LiteralPath $desktopRoot -Directory -ErrorAction Stop | Where-Object {
        $_.Name -match '^8\.' -and
        (Test-Path -LiteralPath (Join-Path $_.FullName "WindowsBase.dll") -PathType Leaf) -and
        (Test-Path -LiteralPath (Join-Path $_.FullName "System.Windows.Forms.dll") -PathType Leaf)
    })
    if ($fxr8.Count -eq 0 -or $runtime8.Count -eq 0 -or $desktop8.Count -eq 0) {
        throw "DOTNET_ROOT is set but does not contain a complete .NET 8 WindowsDesktop host/runtime."
    }

    $selectedRuntime = $runtime8 | Sort-Object { [Version]$_.Name } -Descending | Select-Object -First 1
    if ($desktop8.Name -notcontains $selectedRuntime.Name) {
        throw "DOTNET_ROOT is set but its latest .NETCore and WindowsDesktop 8.x patch versions do not match."
    }
    Assert-Qs3dV26InstalledDesktopRuntime -ExpectedRuntimeVersion $selectedRuntime.Name
}
$expectedProductVersion = '0.1.0-preview.10307'
$candidateKind = 'LOCAL_PR_CANDIDATE'

function Get-Hash([string]$Path) {
    return (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash.ToLowerInvariant()
}

function Assert-OrdinaryFile([string]$Path, [string]$Label) {
    $item = Get-Item -LiteralPath $Path -Force -ErrorAction Stop
    if ($item.PSIsContainer -or -not ($item -is [IO.FileInfo]) -or
        ($item.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
        throw ($Label + ' must be an ordinary non-reparse file.')
    }
    return $item.FullName
}

function Assert-JsonPropertySet($Value, [string[]]$Expected, [string]$Label) {
    [string[]]$actual = @($Value.PSObject.Properties.Name | Sort-Object)
    [string[]]$required = @($Expected | Sort-Object)
    if ($actual.Length -ne $required.Length -or
        [string]::Join([char]0, $actual) -cne [string]::Join([char]0, $required)) {
        throw ($Label + ' schema changed.')
    }
}

function Assert-Net8RuntimeConfig([string]$Path, [string]$Label, [switch]$RequireWindowsDesktop) {
    $config = Get-Content -LiteralPath $Path -Raw | ConvertFrom-Json -ErrorAction Stop
    if ($null -eq $config.runtimeOptions -or [string]$config.runtimeOptions.tfm -cne 'net8.0') {
        throw ($Label + ' must declare tfm net8.0.')
    }
    $frameworks = @()
    if ($config.runtimeOptions.PSObject.Properties.Name -contains 'framework') { $frameworks += $config.runtimeOptions.framework }
    if ($config.runtimeOptions.PSObject.Properties.Name -contains 'frameworks') { $frameworks += @($config.runtimeOptions.frameworks) }
    $names = @($frameworks | ForEach-Object { [string]$_.name })
    if ($names -notcontains 'Microsoft.NETCore.App' -or
        @($frameworks | Where-Object { [string]$_.version -notmatch '^8\.0\.\d+$' }).Count -ne 0) {
        throw ($Label + ' must declare only compatible .NET 8 framework versions.')
    }
    if ($RequireWindowsDesktop -and $names -notcontains 'Microsoft.WindowsDesktop.App') {
        throw ($Label + ' must declare Microsoft.WindowsDesktop.App 8.x.')
    }
    return $config
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
    if ($actual.schema -cne 'QS3D_V26_PROFILE_RECOVERY_V1' -or $actual.state -cne 'ALLOCATED' -or
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
    $registrationPath = 'HKCU:\Software\Bricsys\BricsCAD\V26x64\en_US\Applications\QS3D'
    $registry = Get-ItemProperty -LiteralPath $registrationPath -ErrorAction SilentlyContinue
    $registrationExists = $null -ne $registry
    $loader = if ($registrationExists) { [string]$registry.LOADER } else { $null }
    $registrationValueHash = 'ABSENT'
    if ($registrationExists) {
        $key = [Microsoft.Win32.Registry]::CurrentUser.OpenSubKey('Software\Bricsys\BricsCAD\V26x64\en_US\Applications\QS3D', $false)
        if ($null -eq $key) { throw 'V26 demand-load registration changed while being captured.' }
        try {
            [string[]]$names = @($key.GetValueNames())
            [Array]::Sort($names, [StringComparer]::Ordinal)
            $fingerprints = foreach ($name in $names) {
                $kind = $key.GetValueKind($name)
                $value = $key.GetValue($name, $null, [Microsoft.Win32.RegistryValueOptions]::DoNotExpandEnvironmentNames)
                $serialized = if ($kind -eq [Microsoft.Win32.RegistryValueKind]::Binary) {
                    [Convert]::ToBase64String([byte[]]$value)
                } elseif ($kind -eq [Microsoft.Win32.RegistryValueKind]::MultiString) {
                    [string]::Join([char]0, [string[]]$value)
                } else { [Convert]::ToString($value, [Globalization.CultureInfo]::InvariantCulture) }
                $name + [char]0 + ([int]$kind) + [char]0 + $serialized
            }
            $registrationValueHash = Get-StringHash ([string]::Join([char]1, $fingerprints))
        } finally { $key.Dispose() }
    }
    $flags = @()
    foreach ($provider in @('OpenAiSecureTunnel', 'CloudflareAccount')) {
        $flag = Join-Path ([Environment]::GetFolderPath('ApplicationData')) ('QS3D\MCP\' + $provider + '\autostart.txt')
        if (Test-Path -LiteralPath $flag) {
            if ((Get-Content -LiteralPath $flag -Raw).Trim() -ne '0') { throw 'Tunnel autostart is not paused. No launch performed.' }
            $flags += Get-Hash $flag
        } else { $flags += 'ABSENT' }
    }
    return [ordered]@{
        registration_exists = $registrationExists
        registration_values_sha256 = $registrationValueHash
        loader_value_sha256 = if ($registrationExists -and -not [string]::IsNullOrWhiteSpace($loader)) { Get-StringHash ([IO.Path]::GetFullPath($loader).ToLowerInvariant()) } else { 'ABSENT' }
        loader_exists = $registrationExists -and -not [string]::IsNullOrWhiteSpace($loader) -and (Test-Path -LiteralPath $loader -PathType Leaf)
        loader_sha256 = if ($registrationExists -and -not [string]::IsNullOrWhiteSpace($loader) -and (Test-Path -LiteralPath $loader -PathType Leaf)) { Get-Hash $loader } else { 'ABSENT' }
        load_controls = if ($registrationExists) { [int]$registry.LOADCTRLS } else { $null }
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
    if ($marker.schema -cne 'QS3D_LOCAL022_V26_NATIVE_V1' -or $marker.run_id -cne $runId -or $marker.phase -cne $Phase) {
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
        run = @('active_disposable_drawing', 'host_major_26', 'product_location_exact', 'mcp_mutation_boundary_paused', 'meter_units',
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
    if ((Get-Hash $PackageZip) -ine $expectedPackageSha256 -or
        (Get-Hash $ProvenancePath) -cne $provenanceHash) { throw 'Frozen candidate archive/provenance changed before launch.' }
    foreach ($entry in $packageFiles.GetEnumerator()) {
        if ((Get-Hash (Join-Path $ProductDir $entry.Key)) -cne $entry.Value) { throw 'Frozen product payload changed before launch.' }
    }
    foreach ($entry in $probeExtraHashes.GetEnumerator()) {
        if ((Get-Hash (Join-Path (Split-Path -Parent $ProbeDll) $entry.Key)) -cne $entry.Value) { throw 'Frozen probe payload changed before launch.' }
    }
    Assert-Qs3dV26DotNetRoot
    if (($protectedBefore | ConvertTo-Json -Compress) -cne ((Get-ProtectedState) | ConvertTo-Json -Compress)) {
        throw 'Protected machine state changed before launch.'
    }
    $env:QS3D_LOCAL022_V26_PHASE = $Phase
    $scriptPath = Join-Path $privateRoot ($Phase + '.scr')
    $lines = @('FILEDIA', '0', 'CMDECHO', '1', 'TILEMODE', '1', 'INSUNITS', '6', '_.UCS', '_W',
        'NETLOAD', ('"' + $pluginDll + '"'), 'NETLOAD', ('"' + $ProbeDll + '"')) + $Commands
    [IO.File]::WriteAllLines($scriptPath, $lines, [Text.Encoding]::ASCII)
    $arguments = '"' + $drawing + '" /L /P "' + $sandbox.NonceProfile + '" /B "' + $scriptPath + '"'
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
    throw 'An interactive licensed Windows V26 host is required.'
}
$repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
if ($ProductSourceSha -cne $expectedProductSourceSha -or $PackageSha256.ToLowerInvariant() -cne $expectedPackageSha256) {
    throw 'This LOCAL-022 allocation requires the exact pinned candidate ZIP.'
}
$artifactBase = Join-Path $repoRoot 'artifacts\issue-5718-local022'
$ArtifactDir = Assert-ChildPath $artifactBase $ArtifactDir
if (Test-Path -LiteralPath $ArtifactDir) { throw 'Allocation root already exists; create a fresh run identity.' }
foreach ($path in @($ArtifactDir, $ProductDir, $PackageZip, $ProvenancePath, $ProbeDll, $BricsCadDir, $Profile)) {
    if ($path -match '["\r\n]') { throw 'Unsafe native script input path.' }
}
$ProductDir = [IO.Path]::GetFullPath($ProductDir)
$PackageZip = [IO.Path]::GetFullPath($PackageZip)
$ProbeDll = [IO.Path]::GetFullPath($ProbeDll)
$BricsCadDir = [IO.Path]::GetFullPath($BricsCadDir)
$bricscadExe = Join-Path $BricsCadDir 'bricscad.exe'
$pluginDll = Join-Path $ProductDir 'QS3D.BricsCAD.V26.dll'
$coreDll = Join-Path $ProductDir 'QS3D.Core.dll'
$expectedProbe = [IO.Path]::GetFullPath((Join-Path $repoRoot 'tests\QS3D.LocalQualification.V26\bin\Release\net8.0-windows\QS3D.LocalQualification.V26.dll'))
if (-not [string]::Equals($ProbeDll, $expectedProbe, [StringComparison]::OrdinalIgnoreCase)) {
    throw 'ProbeDll must be the exact repository Release build output.'
}
$fixture = Join-Path $repoRoot 'samples\generated\QS3D-Sample.dwg'
$fixtureHash = Get-Hash $fixture
if ($fixtureHash -cne 'cec1350fb2207542aeecd96a790a198a6c9cc9e99a9f875871f367554b3d967e') { throw 'Reference fixture changed.' }
if ((Get-Hash $PackageZip) -ine $PackageSha256) { throw 'Candidate package hash mismatch.' }
$ProvenancePath = Assert-OrdinaryFile $ProvenancePath 'Candidate provenance'
$provenanceHash = Get-Hash $ProvenancePath
$provenance = Get-Content -LiteralPath $ProvenancePath -Raw | ConvertFrom-Json
Assert-JsonPropertySet $provenance @('product', 'target', 'releaseTag', 'productVersion', 'sourceCommit', 'packageSha256') 'Candidate provenance'
if ($provenance.product -cne 'QS3D' -or $provenance.target -cne 'BricsCAD V26 x64' -or
    $provenance.sourceCommit -cne $expectedProductSourceSha -or
    $provenance.productVersion -cne $expectedProductVersion -or
    $provenance.releaseTag -cne ('v' + $expectedProductVersion) -or
    $provenance.packageSha256 -ine $expectedPackageSha256) { throw 'Candidate provenance identity mismatch.' }
$metadata = Get-Content -LiteralPath (Join-Path $ProductDir 'PACKAGE-METADATA.json') -Raw | ConvertFrom-Json
if ($metadata.product -cne 'QS3D' -or $metadata.framework -cne 'net8.0-windows' -or
    $metadata.productVersion -cne $expectedProductVersion -or
    $metadata.target -cne 'BricsCAD V26 x64') { throw 'Candidate product identity mismatch.' }
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
if (-not $packageFiles.Contains('QS3D.BricsCAD.V26.dll') -or -not $packageFiles.Contains('QS3D.Core.dll')) { throw 'Package lacks required binaries.' }
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
if ((Get-Item -LiteralPath $bricscadExe).VersionInfo.FileMajorPart -ne 26) { throw 'Wrong host major.' }
Assert-Qs3dV26DotNetRoot
[void](Assert-Net8RuntimeConfig (Join-Path $ProductDir 'QS3D.BricsCAD.V26.runtimeconfig.json') 'Product runtime' -RequireWindowsDesktop)
Assert-Qs3dNoBricsCadProcess
if ((Get-Qs3dActiveTunnelProcessCount) -ne 0) { throw 'Tunnels must remain stopped.' }
$protectedBefore = Get-ProtectedState
if ($protectedBefore.registration_exists -and $protectedBefore.load_controls -ne 4) { throw 'An existing loader must use OnCommand for exact NETLOAD qualification.' }
$probeSource = Join-Path $repoRoot 'tests\QS3D.LocalQualification.V26\Local022NativeFootingProbeCommands.cs'
$probeProject = Join-Path $repoRoot 'tests\QS3D.LocalQualification.V26\QS3D.LocalQualification.V26.csproj'
$probePdb = [IO.Path]::ChangeExtension($ProbeDll, '.pdb')
$probeSourceHash = Get-Hash $probeSource
$probeProjectHash = Get-Hash $probeProject
$runnerHash = Get-Hash $PSCommandPath
$supplementalInputs = [ordered]@{}
foreach ($inputPath in @((Join-Path $PSScriptRoot 'local022-ui-input.ps1')) + @(Get-ChildItem (Split-Path $probeSource) -Filter '*.cs' -File | Select-Object -ExpandProperty FullName) + @(Get-ChildItem (Join-Path $repoRoot 'tests\QS3D.LocalQualification.V25') -Filter '*Ui*.cs' -File | Select-Object -ExpandProperty FullName)) {
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
$expectedProbeFiles = @('QS3D.LocalQualification.V26.deps.json', 'QS3D.LocalQualification.V26.dll', 'QS3D.LocalQualification.V26.pdb', 'QS3D.LocalQualification.V26.runtimeconfig.json')
if ([string]::Join([char]0, $probeOutputNames) -cne [string]::Join([char]0, $expectedProbeFiles)) {
    throw 'Probe output contains an unexpected payload.'
}
[void](Assert-Net8RuntimeConfig ([IO.Path]::ChangeExtension($ProbeDll, '.runtimeconfig.json')) 'Probe runtime')
$probeExtraHashes = [ordered]@{}
foreach ($name in $expectedProbeFiles) { $probeExtraHashes[$name] = Get-Hash (Join-Path (Split-Path -Parent $ProbeDll) $name) }
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
    schema = 'QS3D_LOCAL022_V26_ALLOCATION_V1'; run_id = $runId; started_utc = $started.ToString('o')
    product_source_sha = $ProductSourceSha; product_version = $metadata.productVersion
    candidate_kind = $candidateKind; published_release = $false; provenance_sha256 = $provenanceHash
    package_sha256 = $PackageSha256.ToLowerInvariant(); package_files = $packageFiles
    probe_sha256 = $probeHash; probe_pdb_sha256 = $probePdbHash
    probe_files = $probeExtraHashes
    probe_source_sha256 = $probeSourceHash; probe_project_sha256 = $probeProjectHash; dotnet_sdk = $dotnetVersion
    runner_sha256 = $runnerHash; harness_git_sha = $harnessSha
    interactive_ui = [bool]$InteractiveUi; supplemental_input_hashes = @($supplementalInputs.Values)
    fixture_sha256 = $fixtureHash; host_version = (Get-Item -LiteralPath $bricscadExe).VersionInfo.FileVersion
    host_sha256 = Get-Hash $bricscadExe; pre_existing_host_count = 0
    mcp_test_executed = $false; mcp_requests_issued_by_runner = $false
}
$ownedProcesses = [Collections.Generic.List[Diagnostics.Process]]::new()
$envNames = @('QS3D_LOCAL022_V26_RUN_ID', 'QS3D_LOCAL022_V26_ROOT', 'QS3D_LOCAL022_V26_DRAWING', 'QS3D_LOCAL022_V26_PRODUCT_DLL',
    'QS3D_LOCAL022_V26_PROBE_DLL', 'QS3D_LOCAL022_V26_PHASE')
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
    $profileSnapshotBefore = Get-Qs3dV26ProfileSnapshot
    $profileRecoveryPrepared = [ordered]@{
        schema = 'QS3D_V26_PROFILE_RECOVERY_V1'
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
    $sandbox = New-Qs3dV26ProfileSandbox -SourceProfile $Profile
    if (-not (Test-ProfileSnapshotExact -Left $profileSnapshotBefore -Right $sandbox.Snapshot)) {
        throw 'Profile snapshot changed across sandbox allocation.'
    }
    $profileRecoveryExpected = [ordered]@{
        schema = 'QS3D_V26_PROFILE_RECOVERY_V1'
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
    $env:QS3D_LOCAL022_V26_RUN_ID = $runId
    $env:QS3D_LOCAL022_V26_ROOT = $ArtifactDir
    $env:QS3D_LOCAL022_V26_DRAWING = $drawing
    $env:QS3D_LOCAL022_V26_PRODUCT_DLL = $pluginDll
    $env:QS3D_LOCAL022_V26_PROBE_DLL = $ProbeDll
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
            try { $profileReceipt = Restore-Qs3dV26ProfileSandbox -Sandbox $sandbox }
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
        if ((Get-Hash $ProvenancePath) -cne $provenanceHash) { throw 'Frozen provenance changed during allocation.' }
        foreach ($entry in $supplementalInputs.GetEnumerator()) {
            if ((Get-Hash $entry.Key) -cne $entry.Value) { throw 'Frozen supplemental harness changed.' }
        }
        foreach ($entry in $probeExtraHashes.GetEnumerator()) {
            if ((Get-Hash (Join-Path (Split-Path -Parent $ProbeDll) $entry.Key)) -cne $entry.Value) { throw 'Frozen probe payload changed.' }
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
    schema = 'QS3D_LOCAL022_V26_RECEIPT_V1'; run_id = $runId; status = $status
    product_source_sha = $ProductSourceSha; product_version = $metadata.productVersion
    candidate_kind = $candidateKind; published_release = $false
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
