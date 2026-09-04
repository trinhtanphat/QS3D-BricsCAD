[CmdletBinding(SupportsShouldProcess = $true, ConfirmImpact = 'High')]
param(
    [string]$PackageDirectory = (Join-Path (Split-Path -Parent $PSScriptRoot) 'dist\QS3D-BricsCAD-V25'),
    [string]$PackageZip = (Join-Path (Split-Path -Parent $PSScriptRoot) 'dist\QS3D-BricsCAD-V25.zip'),

    [Parameter(Mandatory = $true)]
    [ValidatePattern('^[0-9A-Fa-f]{40}$')]
    [string]$ExpectedSignerThumbprint
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$MaxMetadataBytes = 1MB
$SignedPayloadNames = @(
    'QS3D.BricsCAD.V25.dll',
    'QS3D.Core.dll',
    'install-v25-autoload.ps1',
    'uninstall-v25-autoload.ps1',
    'update-v25.ps1',
    'unblock-v25-netload.ps1'
)

function Normalize-Thumbprint {
    param([string]$Thumbprint)
    return $Thumbprint.Replace(' ', '').ToUpperInvariant()
}

function Get-CanonicalFullPath {
    param([string]$Path, [string]$Label)
    if ([string]::IsNullOrWhiteSpace($Path)) { throw "$Label path is required." }
    try { return [IO.Path]::GetFullPath($Path) }
    catch { throw "$Label path is invalid: $($_.Exception.Message)" }
}

function Test-PathEqualOrContained {
    param([string]$Path, [string]$Container)

    $pathFull = (Get-CanonicalFullPath -Path $Path -Label 'candidate').TrimEnd([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar)
    $containerFull = (Get-CanonicalFullPath -Path $Container -Label 'container').TrimEnd([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar)
    if ([string]::Equals($pathFull, $containerFull, [StringComparison]::OrdinalIgnoreCase)) { return $true }
    return $pathFull.StartsWith($containerFull + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)
}

function Assert-NoReparseDirectoryChain {
    param([string]$Path, [string]$Label)

    $current = Get-CanonicalFullPath -Path $Path -Label $Label
    while (-not [string]::IsNullOrWhiteSpace($current)) {
        if (Test-Path -LiteralPath $current) {
            $item = Get-Item -LiteralPath $current -Force -ErrorAction Stop
            if (($item.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
                throw "$Label traverses a reparse-backed filesystem entry: $current"
            }
            if (-not $item.PSIsContainer) {
                throw "$Label requires a directory path, but an ancestor is not a directory: $current"
            }
        }

        $parent = [IO.Path]::GetDirectoryName($current)
        if ([string]::IsNullOrWhiteSpace($parent) -or [string]::Equals($parent, $current, [StringComparison]::OrdinalIgnoreCase)) { break }
        $current = $parent
    }
}

function Assert-SafeDirectory {
    param([string]$Path, [string]$Label)

    $fullPath = Get-CanonicalFullPath -Path $Path -Label $Label
    $pathRoot = [IO.Path]::GetPathRoot($fullPath)
    if (-not [string]::IsNullOrWhiteSpace($pathRoot) -and
        [string]::Equals($fullPath, $pathRoot, [StringComparison]::OrdinalIgnoreCase)) {
        throw "$Label must not be a filesystem root: $fullPath"
    }
    $trimmedFullPath = $fullPath.TrimEnd([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar)
    Assert-NoReparseDirectoryChain -Path $fullPath -Label $Label
    if (-not (Test-Path -LiteralPath $fullPath -PathType Container)) {
        throw "$Label directory was not found: $fullPath"
    }
    $item = Get-Item -LiteralPath $fullPath -Force -ErrorAction Stop
    if (($item.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0 -or -not $item.PSIsContainer) {
        throw "$Label must be an ordinary non-reparse directory: $fullPath"
    }
    return $trimmedFullPath
}

function Assert-SafeContainedDirectory {
    param([string]$Path, [string]$RepositoryRoot, [string]$Label)

    $repository = Assert-SafeDirectory -Path $RepositoryRoot -Label 'repository root'
    $directory = Assert-SafeDirectory -Path $Path -Label $Label
    if (-not (Test-PathEqualOrContained -Path $directory -Container $repository) -or
        [string]::Equals($directory, $repository, [StringComparison]::OrdinalIgnoreCase)) {
        throw "$Label must stay below the repository root: $directory"
    }
    return $directory
}

function Assert-SafeFile {
    param([string]$Path, [string]$Label)

    $fullPath = Get-CanonicalFullPath -Path $Path -Label $Label
    $parent = [IO.Path]::GetDirectoryName($fullPath)
    Assert-NoReparseDirectoryChain -Path $parent -Label ("$Label parent")
    if (-not (Test-Path -LiteralPath $fullPath -PathType Leaf)) {
        throw "$Label file was not found: $fullPath"
    }
    $item = Get-Item -LiteralPath $fullPath -Force -ErrorAction Stop
    if ($item.PSIsContainer -or ($item.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
        throw "$Label must be an ordinary non-reparse file: $fullPath"
    }
    return $fullPath
}

function Assert-SafeOptionalFileTarget {
    param([string]$Path, [string]$Label)

    $fullPath = Get-CanonicalFullPath -Path $Path -Label $Label
    $parent = [IO.Path]::GetDirectoryName($fullPath)
    Assert-NoReparseDirectoryChain -Path $parent -Label ("$Label parent")
    if (Test-Path -LiteralPath $fullPath) {
        $item = Get-Item -LiteralPath $fullPath -Force -ErrorAction Stop
        if ($item.PSIsContainer -or ($item.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
            throw "$Label must be an ordinary non-reparse file target: $fullPath"
        }
    }
    return $fullPath
}

function Assert-SafeContainedOptionalFileTarget {
    param([string]$Path, [string]$RepositoryRoot, [string]$Label)

    $repository = Assert-SafeDirectory -Path $RepositoryRoot -Label 'repository root'
    $target = Get-CanonicalFullPath -Path $Path -Label $Label
    if (-not (Test-PathEqualOrContained -Path $target -Container $repository) -or
        [string]::Equals($target, $repository, [StringComparison]::OrdinalIgnoreCase)) {
        throw "$Label must stay below the repository root: $target"
    }
    return Assert-SafeOptionalFileTarget -Path $target -Label $Label
}

function Get-SafePackageFiles {
    param([string]$PackageRoot)

    $pending = New-Object 'System.Collections.Generic.Stack[string]'
    $files = New-Object 'System.Collections.Generic.List[System.IO.FileInfo]'
    $pending.Push($PackageRoot)

    while ($pending.Count -gt 0) {
        $directory = $pending.Pop()
        foreach ($item in Get-ChildItem -LiteralPath $directory -Force -ErrorAction Stop) {
            if (($item.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
                throw "Signed package contains a reparse-backed entry: $($item.FullName)"
            }
            if ($item.PSIsContainer) {
                $pending.Push($item.FullName)
                continue
            }
            if (-not ($item -is [IO.FileInfo])) {
                throw "Signed package contains a non-regular filesystem entry: $($item.FullName)"
            }
            $files.Add($item)
        }
    }

    return @($files | Sort-Object FullName)
}

function Read-BoundedUtf8Text {
    param(
        [string]$Path,
        [int64]$MaxBytes,
        [string]$Label
    )

    $safePath = Assert-SafeFile -Path $Path -Label $Label
    $stream = $null
    try {
        $stream = [IO.FileStream]::new(
            $safePath,
            [IO.FileMode]::Open,
            [IO.FileAccess]::Read,
            [IO.FileShare]::Read
        )
        $length = $stream.Length
        if ($length -gt $MaxBytes) {
            throw "$Label exceeds the $MaxBytes-byte input limit: $length bytes."
        }
        if ($length -gt [int]::MaxValue) {
            throw "$Label is too large to materialize safely: $length bytes."
        }

        $bytes = New-Object byte[] ([int]$length)
        $offset = 0
        while ($offset -lt $bytes.Length) {
            $read = $stream.Read($bytes, $offset, $bytes.Length - $offset)
            if ($read -le 0) { throw "$Label changed or ended during bounded read." }
            $offset += $read
        }
        if ($stream.Length -ne $length) {
            throw "$Label changed size during bounded read."
        }

        try {
            $utf8 = [Text.UTF8Encoding]::new($false, $true)
            return $utf8.GetString($bytes)
        }
        catch {
            throw "$Label is not valid UTF-8: $($_.Exception.Message)"
        }
        finally {
            if ($bytes) { [Array]::Clear($bytes, 0, $bytes.Length) }
        }
    }
    finally {
        if ($stream) { $stream.Dispose() }
    }
}

function New-SiblingTempPath {
    param(
        [string]$TargetPath,
        [string]$Suffix
    )

    $target = Get-CanonicalFullPath -Path $TargetPath -Label 'temporary target'
    $parent = [IO.Path]::GetDirectoryName($target)
    Assert-NoReparseDirectoryChain -Path $parent -Label 'temporary target parent'
    $leaf = [IO.Path]::GetFileName($target)
    return Join-Path $parent (".$leaf.$([Guid]::NewGuid().ToString('N'))$Suffix")
}

function Write-Utf8NoBomText {
    param([string]$Path, [string]$Text)
    $encoding = [Text.UTF8Encoding]::new($false, $true)
    [IO.File]::WriteAllText($Path, $Text, $encoding)
}

function Get-PackageRelativePath {
    param([IO.FileInfo]$File, [string]$PackageRoot)

    $relative = $File.FullName.Substring($PackageRoot.Length).TrimStart('\', '/').Replace([IO.Path]::DirectorySeparatorChar, '/')
    if ([string]::IsNullOrWhiteSpace($relative) -or [IO.Path]::IsPathRooted($relative) -or $relative.Contains(':') -or $relative.Contains('\')) {
        throw "Unsafe package-relative path while hashing: $relative"
    }
    $segments = @($relative.Split('/'))
    if (@($segments | Where-Object { [string]::IsNullOrWhiteSpace($_) -or $_ -eq '.' -or $_ -eq '..' }).Count -gt 0) {
        throw "Unsafe package-relative path while hashing: $relative"
    }
    return $relative
}

function Assert-ZipMatchesPackage {
    param([string]$ZipPath, [string]$PackageRoot)

    Add-Type -AssemblyName System.IO.Compression.FileSystem -ErrorAction Stop
    $expected = @(
        Get-SafePackageFiles -PackageRoot $PackageRoot |
            ForEach-Object { Get-PackageRelativePath -File $_ -PackageRoot $PackageRoot } |
            Sort-Object
    )
    $archive = $null
    try {
        $archive = [IO.Compression.ZipFile]::OpenRead($ZipPath)
        $actual = New-Object 'System.Collections.Generic.List[string]'
        $seen = @{}
        foreach ($entry in $archive.Entries) {
            if ([string]::IsNullOrEmpty($entry.Name)) { continue }
            $name = $entry.FullName.Replace('\', '/')
            if ($name.StartsWith('/') -or $name.Contains(':')) {
                throw "Staged ZIP contains an unsafe entry: $name"
            }
            $segments = @($name.Split('/'))
            if (@($segments | Where-Object { [string]::IsNullOrWhiteSpace($_) -or $_ -eq '.' -or $_ -eq '..' }).Count -gt 0) {
                throw "Staged ZIP contains an unsafe entry: $name"
            }
            $key = $name.ToUpperInvariant()
            if ($seen.ContainsKey($key)) {
                throw "Staged ZIP contains a case-insensitive duplicate entry: $name"
            }
            $seen[$key] = $true
            $actual.Add($name)
        }
        $actualSorted = @($actual | Sort-Object)
        if ($expected.Count -ne $actualSorted.Count) {
            throw "Staged ZIP file-count mismatch. Expected $($expected.Count), got $($actualSorted.Count)."
        }
        for ($i = 0; $i -lt $expected.Count; $i++) {
            if (-not [string]::Equals($expected[$i], $actualSorted[$i], [StringComparison]::Ordinal)) {
                throw "Staged ZIP entry mismatch at index $i. Expected '$($expected[$i])', got '$($actualSorted[$i])'."
            }
        }
    }
    finally {
        if ($archive) { $archive.Dispose() }
    }
}

function Assert-ZipManifestIntegrity {
    param([string]$ZipPath)

    Add-Type -AssemblyName System.IO.Compression.FileSystem -ErrorAction Stop
    $safeZipPath = Assert-SafeFile -Path $ZipPath -Label 'staged PackageZip manifest input'
    $fileStream = $null
    $archive = $null
    $outerHash = $null
    try {
        $fileStream = [IO.FileStream]::new(
            $safeZipPath,
            [IO.FileMode]::Open,
            [IO.FileAccess]::Read,
            [IO.FileShare]::Read
        )
        $archive = [IO.Compression.ZipArchive]::new($fileStream, [IO.Compression.ZipArchiveMode]::Read, $true)
        $seenArchivePaths = [Collections.Generic.Dictionary[string,object]]::new([StringComparer]::OrdinalIgnoreCase)
        $archivePayloadPaths = [Collections.Generic.List[string]]::new()
        $manifestEntries = [Collections.Generic.List[object]]::new()

        foreach ($entry in $archive.Entries) {
            $rawName = $entry.FullName.Replace('\', '/')
            $isDirectory = [string]::IsNullOrEmpty($entry.Name)
            $name = if ($isDirectory) { $rawName.TrimEnd('/') } else { $rawName }
            if ([string]::IsNullOrWhiteSpace($name) -or $name.StartsWith('/') -or $name.Contains(':') -or $name.Contains('\')) {
                throw "Staged ZIP contains an unsafe entry while validating checksum manifest: $rawName"
            }
            $segments = @($name.Split('/'))
            if (@($segments | Where-Object { [string]::IsNullOrWhiteSpace($_) -or $_ -eq '.' -or $_ -eq '..' }).Count -gt 0) {
                throw "Staged ZIP contains an unsafe entry while validating checksum manifest: $rawName"
            }
            if ($isDirectory) { continue }
            if ($seenArchivePaths.ContainsKey($name)) {
                throw "Staged ZIP contains a case-insensitive duplicate entry while validating checksum manifest: $name"
            }
            $seenArchivePaths.Add($name, $entry)
            if ([string]::Equals($name, 'SHA256SUMS.txt', [StringComparison]::Ordinal)) {
                $manifestEntries.Add($entry)
            }
            else {
                $archivePayloadPaths.Add($name)
            }
        }

        if ($manifestEntries.Count -ne 1) {
            throw "Staged ZIP must contain exactly one canonical SHA256SUMS.txt entry; found $($manifestEntries.Count)."
        }
        $manifestEntry = $manifestEntries[0]
        if ($manifestEntry.Length -lt 1 -or $manifestEntry.Length -gt 4MB -or $manifestEntry.Length -gt [int]::MaxValue) {
            throw "Staged ZIP checksum manifest has an invalid bounded size: $($manifestEntry.Length) bytes."
        }

        $manifestStream = $null
        $manifestBytes = $null
        try {
            $manifestStream = $manifestEntry.Open()
            $manifestBytes = New-Object byte[] ([int]$manifestEntry.Length)
            $offset = 0
            while ($offset -lt $manifestBytes.Length) {
                $read = $manifestStream.Read($manifestBytes, $offset, $manifestBytes.Length - $offset)
                if ($read -le 0) { throw 'Staged ZIP checksum manifest ended before its declared length.' }
                $offset += $read
            }
            if ($manifestStream.ReadByte() -ne -1) { throw 'Staged ZIP checksum manifest exceeds its declared length.' }
            if (@($manifestBytes | Where-Object { $_ -gt 0x7F }).Count -gt 0) {
                throw 'Staged ZIP checksum manifest must contain ASCII only.'
            }
            $manifestText = [Text.Encoding]::ASCII.GetString($manifestBytes)
        }
        finally {
            if ($manifestStream) { $manifestStream.Dispose() }
            if ($manifestBytes) { [Array]::Clear($manifestBytes, 0, $manifestBytes.Length) }
        }

        $lines = @([Text.RegularExpressions.Regex]::Split($manifestText, "\r?\n"))
        while ($lines.Count -gt 0 -and $lines[$lines.Count - 1] -eq '') {
            if ($lines.Count -eq 1) { $lines = @(); break }
            $lines = @($lines[0..($lines.Count - 2)])
        }
        if ($lines.Count -eq 0) { throw 'Staged ZIP checksum manifest contains no payload records.' }

        $seenManifestPaths = [Collections.Generic.Dictionary[string,string]]::new([StringComparer]::OrdinalIgnoreCase)
        $manifestPayloadPaths = [Collections.Generic.List[string]]::new()
        foreach ($line in $lines) {
            if ($line -notmatch '^([0-9A-F]{64})  (.+)$') {
                throw "Staged ZIP checksum manifest contains a malformed record: $line"
            }
            $expectedHash = $Matches[1]
            $name = $Matches[2]
            if ($name.StartsWith('/') -or $name.Contains(':') -or $name.Contains('\')) {
                throw "Staged ZIP checksum manifest contains an unsafe path: $name"
            }
            $segments = @($name.Split('/'))
            if (@($segments | Where-Object { [string]::IsNullOrWhiteSpace($_) -or $_ -eq '.' -or $_ -eq '..' }).Count -gt 0) {
                throw "Staged ZIP checksum manifest contains an unsafe path: $name"
            }
            if ([string]::Equals($name, 'SHA256SUMS.txt', [StringComparison]::OrdinalIgnoreCase)) {
                throw 'Staged ZIP checksum manifest must not hash itself.'
            }
            if ($seenManifestPaths.ContainsKey($name)) {
                throw "Staged ZIP checksum manifest contains a case-insensitive duplicate path: $name"
            }
            $seenManifestPaths.Add($name, $expectedHash)
            $manifestPayloadPaths.Add($name)
        }

        if ($archivePayloadPaths.Count -ne $manifestPayloadPaths.Count) {
            throw "Staged ZIP checksum manifest coverage mismatch. Archive payload count=$($archivePayloadPaths.Count), manifest count=$($manifestPayloadPaths.Count)."
        }

        foreach ($name in $archivePayloadPaths) {
            if (-not $seenManifestPaths.ContainsKey($name)) {
                throw "Staged ZIP checksum manifest coverage mismatch; missing payload: $name"
            }
            $entry = $seenArchivePaths[$name]
            $stream = $null
            $hash = $null
            try {
                $stream = $entry.Open()
                $hash = [Security.Cryptography.SHA256]::Create()
                $digest = $hash.ComputeHash($stream)
                $actualHash = -join ($digest | ForEach-Object { $_.ToString('X2') })
                if (-not [string]::Equals($actualHash, $seenManifestPaths[$name], [StringComparison]::Ordinal)) {
                    throw "Staged ZIP checksum mismatch for payload: $name"
                }
            }
            finally {
                if ($hash) { $hash.Dispose() }
                if ($stream) { $stream.Dispose() }
            }
        }

        $archive.Dispose()
        $archive = $null
        $fileStream.Position = 0
        $outerHash = [Security.Cryptography.SHA256]::Create()
        $outerDigest = $outerHash.ComputeHash($fileStream)
        return (-join ($outerDigest | ForEach-Object { $_.ToString('X2') }))
    }
    finally {
        if ($outerHash) { $outerHash.Dispose() }
        if ($archive) { $archive.Dispose() }
        if ($fileStream) { $fileStream.Dispose() }
    }
}

function Assert-AuthenticodeSigner {
    param([string]$Path, [string]$ExpectedSigner, [string]$Label)
    $signature = Get-AuthenticodeSignature -FilePath $Path
    if ($signature.Status -ne [System.Management.Automation.SignatureStatus]::Valid) {
        throw "$Label signature is not valid: $($signature.Status)"
    }
    if (-not $signature.SignerCertificate) { throw "$Label signature has no signer certificate." }
    $actualSigner = Normalize-Thumbprint $signature.SignerCertificate.Thumbprint
    if ($actualSigner -ne $ExpectedSigner) { throw "$Label signer mismatch. Expected $ExpectedSigner, got $actualSigner." }
}

function Read-ManagedAssemblyVersion {
    param([string]$Path, [string]$Label)
    try {
        $version = [Reflection.AssemblyName]::GetAssemblyName($Path).Version
        if (-not $version) { throw 'assembly version is missing' }
        return $version
    }
    catch {
        throw "$Label assembly version is unreadable: $($_.Exception.Message)"
    }
}

function Read-ManagedProductVersion {
    param([string]$Path, [string]$Label)
    try {
        $version = ([string][Diagnostics.FileVersionInfo]::GetVersionInfo($Path).ProductVersion).Trim()
        if ([string]::IsNullOrWhiteSpace($version)) { throw 'product version is missing' }
        return $version
    }
    catch {
        throw "$Label product version is unreadable: $($_.Exception.Message)"
    }
}

$repositoryRoot = Assert-SafeDirectory -Path (Split-Path -Parent $PSScriptRoot) -Label 'repository root'
$package = Assert-SafeContainedDirectory -Path $PackageDirectory -RepositoryRoot $repositoryRoot -Label 'PackageDirectory'
$packageRoot = $package + [IO.Path]::DirectorySeparatorChar
$zip = Get-CanonicalFullPath -Path $PackageZip -Label 'PackageZip'
if (-not [string]::Equals([IO.Path]::GetExtension($zip), '.zip', [StringComparison]::OrdinalIgnoreCase)) {
    throw "PackageZip must use the .zip extension: $zip"
}
if ([string]::Equals($zip, $package, [StringComparison]::OrdinalIgnoreCase) -or
    $zip.StartsWith($packageRoot, [StringComparison]::OrdinalIgnoreCase)) {
    throw 'PackageZip must be outside PackageDirectory so finalization cannot delete or overwrite package payload.'
}
$zip = Assert-SafeContainedOptionalFileTarget -Path $zip -RepositoryRoot $repositoryRoot -Label 'PackageZip'
$null = @(Get-SafePackageFiles -PackageRoot $package)
$expectedSigner = Normalize-Thumbprint $ExpectedSignerThumbprint
$metadataPath = Assert-SafeFile -Path (Join-Path $package 'PACKAGE-METADATA.json') -Label 'PACKAGE-METADATA.json'

foreach ($name in $SignedPayloadNames) {
    $path = Assert-SafeFile -Path (Join-Path $package $name) -Label ("signed-package artifact " + $name)
    Assert-AuthenticodeSigner -Path $path -ExpectedSigner $expectedSigner -Label ("QS3D executable payload " + $name)
}

$metadataText = Read-BoundedUtf8Text -Path $metadataPath -MaxBytes $MaxMetadataBytes -Label 'PACKAGE-METADATA.json'
try { $metadata = $metadataText | ConvertFrom-Json -ErrorAction Stop }
catch { throw "PACKAGE-METADATA.json is invalid JSON: $($_.Exception.Message)" }
if ([string]$metadata.product -ne 'QS3D') { throw 'PACKAGE-METADATA product must be QS3D.' }
if ([string]$metadata.target -ne 'BricsCAD V25 x64') { throw 'PACKAGE-METADATA target must be BricsCAD V25 x64.' }
if (-not $metadata.PSObject.Properties['version']) { throw 'PACKAGE-METADATA is missing version.' }
if (-not $metadata.PSObject.Properties['productVersion']) { throw 'PACKAGE-METADATA is missing productVersion.' }
try { $metadataVersion = [Version]::Parse([string]$metadata.version) }
catch { throw "PACKAGE-METADATA version is invalid: $($metadata.version)" }
$metadataProductVersion = ([string]$metadata.productVersion).Trim()
if ([string]::IsNullOrWhiteSpace($metadataProductVersion)) { throw 'PACKAGE-METADATA productVersion is empty.' }

$managedIdentityNames = @('QS3D.BricsCAD.V25.dll', 'QS3D.Core.dll')
$managedIdentities = @{}
foreach ($name in $managedIdentityNames) {
    $path = Assert-SafeFile -Path (Join-Path $package $name) -Label ("managed signed-package artifact " + $name)
    $assemblyVersion = Read-ManagedAssemblyVersion -Path $path -Label $name
    if ($metadataVersion -ne $assemblyVersion) {
        throw "PACKAGE-METADATA version $metadataVersion does not match signed $name assembly version $assemblyVersion."
    }
    $productVersion = Read-ManagedProductVersion -Path $path -Label $name
    if (-not [string]::Equals($metadataProductVersion, $productVersion, [StringComparison]::Ordinal)) {
        throw "PACKAGE-METADATA productVersion $metadataProductVersion does not match signed $name product version $productVersion."
    }
    $managedIdentities[$name] = [pscustomobject]@{
        AssemblyVersion = $assemblyVersion
        ProductVersion = $productVersion
    }
}
$signedPluginVersion = $managedIdentities['QS3D.BricsCAD.V25.dll'].AssemblyVersion

if (-not $PSCmdlet.ShouldProcess($zip, 'Finalize signed QS3D V25 package and rebuild ZIP')) { return }

$package = Assert-SafeContainedDirectory -Path $package -RepositoryRoot $repositoryRoot -Label 'PackageDirectory'
$zipParent = [IO.Path]::GetDirectoryName($zip)
Assert-NoReparseDirectoryChain -Path $zipParent -Label 'PackageZip parent'
if (-not (Test-Path -LiteralPath $zipParent -PathType Container)) {
    New-Item -ItemType Directory -Path $zipParent -Force | Out-Null
}
Assert-NoReparseDirectoryChain -Path $zipParent -Label 'PackageZip parent'
$zip = Assert-SafeContainedOptionalFileTarget -Path $zip -RepositoryRoot $repositoryRoot -Label 'PackageZip'
$metadataPath = Assert-SafeFile -Path (Join-Path $package 'PACKAGE-METADATA.json') -Label 'PACKAGE-METADATA.json'
$hashManifest = Assert-SafeOptionalFileTarget -Path (Join-Path $package 'SHA256SUMS.txt') -Label 'SHA256SUMS.txt'

$metadata | Add-Member -NotePropertyName pluginSignatureStatus -NotePropertyValue 'Valid' -Force
$metadata | Add-Member -NotePropertyName pluginSignerThumbprint -NotePropertyValue $expectedSigner -Force
$metadata | Add-Member -NotePropertyName signedExecutablePayload -NotePropertyValue @($SignedPayloadNames) -Force
$metadata | Add-Member -NotePropertyName signedPayloadSignerThumbprint -NotePropertyValue $expectedSigner -Force
$metadata | Add-Member -NotePropertyName signedPluginAssemblyVersion -NotePropertyValue $signedPluginVersion.ToString() -Force
$metadata | Add-Member -NotePropertyName signedPackageFinalizedUtc -NotePropertyValue ([DateTime]::UtcNow.ToString('o')) -Force

$metadataStage = New-SiblingTempPath -TargetPath $metadataPath -Suffix '.stage.json'
$metadataBackup = New-SiblingTempPath -TargetPath $zip -Suffix '.metadata.backup.json'
$metadataRollbackDiscard = New-SiblingTempPath -TargetPath $zip -Suffix '.metadata.rollback-discard'
$manifestStage = New-SiblingTempPath -TargetPath $hashManifest -Suffix '.stage.txt'
$manifestBackup = New-SiblingTempPath -TargetPath $zip -Suffix '.manifest.backup.txt'
$tempZip = New-SiblingTempPath -TargetPath $zip -Suffix '.stage.zip'
$zipBackup = New-SiblingTempPath -TargetPath $zip -Suffix '.backup.zip'
$zipRollbackDiscard = New-SiblingTempPath -TargetPath $zip -Suffix '.zip.rollback-discard'

foreach ($transactionBackup in @($metadataBackup, $manifestBackup, $metadataRollbackDiscard, $zipBackup, $zipRollbackDiscard)) {
    if (Test-PathEqualOrContained -Path $transactionBackup -Container $package) {
        throw "Signed-package transaction backup must stay outside PackageDirectory: $transactionBackup"
    }
}

$metadataPublished = $false
$manifestDetached = $false
$manifestPublished = $false
$zipPublished = $false
$zipExistedBeforePublish = $false
$transactionCommitted = $false

try {
    $metadataJson = $metadata | ConvertTo-Json -Depth 8
    Write-Utf8NoBomText -Path $metadataStage -Text ($metadataJson + [Environment]::NewLine)
    $null = Assert-SafeFile -Path $metadataStage -Label 'staged PACKAGE-METADATA.json'

    [IO.File]::Replace($metadataStage, $metadataPath, $metadataBackup, $true)
    $metadataPublished = $true

    if (Test-Path -LiteralPath $hashManifest) {
        $hashManifest = Assert-SafeFile -Path $hashManifest -Label 'SHA256SUMS.txt'
        [IO.File]::Move($hashManifest, $manifestBackup)
        $manifestDetached = $true
    }

    $hashLines = foreach ($file in Get-SafePackageFiles -PackageRoot $package) {
        if ([string]::Equals($file.FullName, $hashManifest, [StringComparison]::OrdinalIgnoreCase)) { continue }
        $relative = Get-PackageRelativePath -File $file -PackageRoot $package
        $hash = (Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash.ToUpperInvariant()
        "$hash  $relative"
    }
    if (@($hashLines).Count -eq 0) { throw 'Signed package contains no payload files to hash.' }

    [IO.File]::WriteAllLines($manifestStage, [string[]]$hashLines, [Text.Encoding]::ASCII)
    $null = Assert-SafeFile -Path $manifestStage -Label 'staged SHA256SUMS.txt'
    [IO.File]::Move($manifestStage, $hashManifest)
    $manifestPublished = $true

    $package = Assert-SafeContainedDirectory -Path $package -RepositoryRoot $repositoryRoot -Label 'PackageDirectory'
    $zip = Assert-SafeContainedOptionalFileTarget -Path $zip -RepositoryRoot $repositoryRoot -Label 'PackageZip'
    if (Test-Path -LiteralPath $tempZip) { throw "Staged ZIP path unexpectedly exists: $tempZip" }
    Compress-Archive -Path (Join-Path $package '*') -DestinationPath $tempZip -CompressionLevel Optimal
    $tempZip = Assert-SafeOptionalFileTarget -Path $tempZip -Label 'staged PackageZip'
    if (-not (Test-Path -LiteralPath $tempZip -PathType Leaf)) {
        throw "Staged ZIP was not created: $tempZip"
    }
    if ((Get-Item -LiteralPath $tempZip -Force).Length -le 0) {
        throw 'Staged ZIP is empty.'
    }
    Assert-ZipMatchesPackage -ZipPath $tempZip -PackageRoot $package
    $stagedZipHash = Assert-ZipManifestIntegrity -ZipPath $tempZip

    if (Test-Path -LiteralPath $zip) {
        $zip = Assert-SafeOptionalFileTarget -Path $zip -Label 'PackageZip'
        $zipExistedBeforePublish = $true
        [IO.File]::Replace($tempZip, $zip, $zipBackup, $true)
    }
    else {
        [IO.File]::Move($tempZip, $zip)
    }
    $zipPublished = $true

    $zip = Assert-SafeFile -Path $zip -Label 'finalized PackageZip'
    $installedZipHash = (Get-FileHash -LiteralPath $zip -Algorithm SHA256).Hash.ToUpperInvariant()
    if (-not [string]::Equals($installedZipHash, $stagedZipHash, [StringComparison]::Ordinal)) {
        throw "Finalized ZIP generation mismatch. Expected $stagedZipHash, got $installedZipHash."
    }
    $transactionCommitted = $true

    Write-Host "FINALIZED: $zip"
    Write-Host "Signer: $expectedSigner"
    Write-Host "Signed plugin version: $($signedPluginVersion.ToString())"
    Write-Host "Signed executable payloads: $($SignedPayloadNames.Count)"
    Write-Host "ZIP SHA256: $stagedZipHash"
}
catch {
    $originalError = $_
    $rollbackErrors = New-Object 'System.Collections.Generic.List[string]'

    if ($zipPublished) {
        try {
            $zip = Assert-SafeOptionalFileTarget -Path $zip -Label 'PackageZip rollback target'
            if ($zipExistedBeforePublish) {
                if (-not (Test-Path -LiteralPath $zipBackup -PathType Leaf)) {
                    throw "original PackageZip backup is unavailable: $zipBackup"
                }
                if (Test-Path -LiteralPath $zip -PathType Leaf) {
                    [IO.File]::Replace($zipBackup, $zip, $zipRollbackDiscard, $true)
                    if (Test-Path -LiteralPath $zipRollbackDiscard) {
                        Remove-Item -LiteralPath $zipRollbackDiscard -Force -ErrorAction Stop
                    }
                }
                else {
                    [IO.File]::Move($zipBackup, $zip)
                }
            }
            elseif (Test-Path -LiteralPath $zip) {
                Remove-Item -LiteralPath $zip -Force -ErrorAction Stop
            }
        }
        catch { $rollbackErrors.Add("restore original finalized ZIP: $($_.Exception.Message)") }
    }

    if ($manifestPublished -and (Test-Path -LiteralPath $hashManifest)) {
        try { Remove-Item -LiteralPath $hashManifest -Force -ErrorAction Stop }
        catch { $rollbackErrors.Add("remove staged manifest: $($_.Exception.Message)") }
    }
    if ($manifestDetached -and (Test-Path -LiteralPath $manifestBackup)) {
        try { [IO.File]::Move($manifestBackup, $hashManifest) }
        catch { $rollbackErrors.Add("restore original manifest: $($_.Exception.Message)") }
    }
    if ($metadataPublished -and (Test-Path -LiteralPath $metadataBackup)) {
        try {
            [IO.File]::Replace($metadataBackup, $metadataPath, $metadataRollbackDiscard, $true)
            if (Test-Path -LiteralPath $metadataRollbackDiscard) {
                Remove-Item -LiteralPath $metadataRollbackDiscard -Force -ErrorAction Stop
            }
        }
        catch { $rollbackErrors.Add("restore original metadata: $($_.Exception.Message)") }
    }

    if ($rollbackErrors.Count -gt 0) {
        throw "Signed-package finalization failed: $($originalError.Exception.Message). Rollback also failed: $($rollbackErrors -join '; ')"
    }
    throw $originalError
}
finally {
    foreach ($temporary in @($metadataStage, $manifestStage, $tempZip, $metadataRollbackDiscard, $zipRollbackDiscard)) {
        if (Test-Path -LiteralPath $temporary) {
            Remove-Item -LiteralPath $temporary -Force -ErrorAction SilentlyContinue
        }
    }

    if ($transactionCommitted) {
        foreach ($backup in @($metadataBackup, $manifestBackup, $zipBackup)) {
            if (Test-Path -LiteralPath $backup) {
                Remove-Item -LiteralPath $backup -Force -ErrorAction SilentlyContinue
            }
        }
    }
}