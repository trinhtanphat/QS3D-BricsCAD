Set-StrictMode -Version Latest

$script:Qs3dV25ProfilesRegistryPath = 'Software\Bricsys\BricsCAD\V25x64\en_US\Profiles'
$script:Qs3dV25NoncePrefix = 'QS3D-AUTO-'

function Assert-Qs3dNoBricsCadProcess {
    $existing = @(Get-Process -Name 'bricscad' -ErrorAction SilentlyContinue)
    if ($existing.Count -gt 0) {
        throw 'Close existing BricsCAD processes before capturing or restoring the V25 profile sandbox.'
    }
}

function Open-Qs3dV25ProfilesRegistryKey {
    param([switch]$Writable)

    $key = [Microsoft.Win32.Registry]::CurrentUser.OpenSubKey(
        $script:Qs3dV25ProfilesRegistryPath,
        [bool]$Writable)
    if ($null -eq $key) {
        throw "BricsCAD V25 profile registry root is missing: HKCU:\$($script:Qs3dV25ProfilesRegistryPath)"
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

function Get-Qs3dV25ProfileSnapshot {
    Assert-Qs3dNoBricsCadProcess

    $profiles = Open-Qs3dV25ProfilesRegistryKey
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
        throw 'V25 profile names must be canonical nonblank names without path separators, controls, or surrounding whitespace.'
    }
}

function New-Qs3dV25ProfileSandbox {
    param([Parameter(Mandatory = $true)][string]$SourceProfile)

    if ([Environment]::OSVersion.Platform -ne [PlatformID]::Win32NT) {
        throw 'The V25 profile sandbox requires Windows.'
    }
    Test-Qs3dProfileName -Name $SourceProfile
    $snapshot = Get-Qs3dV25ProfileSnapshot

    $profiles = Open-Qs3dV25ProfilesRegistryKey -Writable
    $source = $null
    $destination = $null
    $nonceName = $null
    try {
        $source = $profiles.OpenSubKey($SourceProfile, $false)
        if ($null -eq $source) {
            throw "Requested BricsCAD V25 profile does not exist: $SourceProfile"
        }

        for ($attempt = 0; $attempt -lt 16; $attempt++) {
            $candidate = $script:Qs3dV25NoncePrefix + ([Guid]::NewGuid().ToString('N'))
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
            throw 'Could not allocate a unique runner-owned BricsCAD V25 profile name.'
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

function Restore-Qs3dV25ProfileSandbox {
    param([Parameter(Mandatory = $true)]$Sandbox)

    Assert-Qs3dNoBricsCadProcess

    $snapshot = $Sandbox.Snapshot
    $nonceName = [string]$Sandbox.NonceProfile
    if ([string]::IsNullOrWhiteSpace($nonceName) -or
        -not $nonceName.StartsWith($script:Qs3dV25NoncePrefix, [StringComparison]::Ordinal) -or
        ($snapshot.ProfileNames -contains $nonceName)) {
        throw 'Refusing to delete a V25 profile that is not proven runner-owned.'
    }

    $profiles = Open-Qs3dV25ProfilesRegistryKey -Writable
    try {
        $nonceKey = $profiles.OpenSubKey($nonceName, $false)
        if ($null -ne $nonceKey) {
            $nonceKey.Dispose()
            $profiles.DeleteSubKeyTree($nonceName, $false)
        }

        if ($snapshot.CurProfileExists) {
            $profiles.SetValue('CurProfile', $snapshot.CurProfileValue, $snapshot.CurProfileKind)
        }
        elseif (@($profiles.GetValueNames()) -contains 'CurProfile') {
            $profiles.DeleteValue('CurProfile', $false)
        }
    }
    finally {
        $profiles.Dispose()
    }

    $after = Get-Qs3dV25ProfileSnapshot
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
        throw 'BricsCAD V25 profile sandbox cleanup could not restore the exact protected profile boundary.'
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
