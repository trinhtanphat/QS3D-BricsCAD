[CmdletBinding(SupportsShouldProcess = $true, ConfirmImpact = 'Low')]
param(
    [string]$PackageDirectory = (Join-Path (Split-Path -Parent $PSScriptRoot) 'dist\QS3D-BricsCAD-V25'),
    [string]$PackageZip = (Join-Path (Split-Path -Parent $PSScriptRoot) 'dist\QS3D-BricsCAD-V25.zip'),

    [Parameter(Mandatory = $true)]
    [ValidatePattern('^https://')]
    [string]$PackageUri,

    [Parameter(Mandatory = $true)]
    [ValidatePattern('^[0-9A-Fa-f]{40}$')]
    [string]$ExpectedSignerThumbprint,

    [string]$OutputPath = (Join-Path (Split-Path -Parent $PSScriptRoot) 'dist\QS3D-BricsCAD-V25.update.json')
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.IO.Compression.FileSystem

$script:MaxMetadataBytes = 65536
$SignedPayloadNames = @(
    'QS3D.BricsCAD.V25.dll',
    'QS3D.Core.dll',
    'install-v25-autoload.ps1',
    'uninstall-v25-autoload.ps1',
    'update-v25.ps1'
)

function Assert-NoReparseDirectoryChain {
    param([Parameter(Mandatory = $true)][IO.DirectoryInfo]$Directory, [Parameter(Mandatory = $true)][string]$Label)
    $cursor = $Directory
    while ($null -ne $cursor) {
        if (($cursor.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
            throw "$Label path contains a reparse-point directory: $($cursor.FullName)"
        }
        $cursor = $cursor.Parent
    }
}

function Resolve-OrdinaryNonReparseDirectory {
    param([Parameter(Mandatory = $true)][string]$Path, [Parameter(Mandatory = $true)][string]$Label)
    $item = Get-Item -LiteralPath $Path -Force -ErrorAction Stop
    if (-not $item.PSIsContainer) { throw "$Label must be a directory: $Path" }
    if (($item.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) { throw "$Label must not be a reparse-point directory: $Path" }
    Assert-NoReparseDirectoryChain -Directory $item -Label $Label
    return $item
}

function Resolve-OrdinaryNonReparseFile {
    param([Parameter(Mandatory = $true)][string]$Path, [Parameter(Mandatory = $true)][string]$Label)
    $item = Get-Item -LiteralPath $Path -Force -ErrorAction Stop
    if ($item.PSIsContainer) { throw "$Label must be an ordinary file: $Path" }
    if (($item.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) { throw "$Label must not be a reparse-point file: $Path" }
    Assert-NoReparseDirectoryChain -Directory $item.Directory -Label $Label
    return $item
}

function Get-StreamingSha256 {
    param([Parameter(Mandatory = $true)][IO.FileInfo]$File, [Parameter(Mandatory = $true)][string]$Label)
    $stream = [IO.File]::Open($File.FullName, [IO.FileMode]::Open, [IO.FileAccess]::Read, [IO.FileShare]::Read)
    $sha = [Security.Cryptography.SHA256]::Create()
    try {
        $bytes = $sha.ComputeHash($stream)
        return ([BitConverter]::ToString($bytes)).Replace('-', '').ToUpperInvariant()
    }
    catch { throw "$Label SHA-256 could not be read safely: $($_.Exception.Message)" }
    finally { $sha.Dispose(); $stream.Dispose() }
}

function Get-StableFileState {
    param([Parameter(Mandatory = $true)][string]$Path, [Parameter(Mandatory = $true)][string]$Label)
    $file = Resolve-OrdinaryNonReparseFile -Path $Path -Label $Label
    $firstLength = [long]$file.Length
    $firstLastWriteUtcTicks = [long]$file.LastWriteTimeUtc.Ticks
    $hash = Get-StreamingSha256 -File $file -Label $Label
    $current = Resolve-OrdinaryNonReparseFile -Path $file.FullName -Label $Label
    $currentHash = Get-StreamingSha256 -File $current -Label $Label
    if ($firstLength -ne [long]$current.Length -or $firstLastWriteUtcTicks -ne [long]$current.LastWriteTimeUtc.Ticks -or -not [string]::Equals($hash, $currentHash, [StringComparison]::Ordinal)) {
        throw "$Label changed while its stable input state was being captured."
    }
    return [pscustomobject]@{
        Path = $current.FullName
        Length = [long]$current.Length
        LastWriteUtcTicks = [long]$current.LastWriteTimeUtc.Ticks
        Sha256 = $currentHash
    }
}

function Assert-StableFileState {
    param([Parameter(Mandatory = $true)]$Expected, [Parameter(Mandatory = $true)][string]$Label)
    $current = Resolve-OrdinaryNonReparseFile -Path ([string]$Expected.Path) -Label $Label
    $currentHash = Get-StreamingSha256 -File $current -Label $Label
    if ([long]$Expected.Length -ne [long]$current.Length -or
        [long]$Expected.LastWriteUtcTicks -ne [long]$current.LastWriteTimeUtc.Ticks -or
        -not [string]::Equals([string]$Expected.Sha256, $currentHash, [StringComparison]::Ordinal)) {
        throw "$Label changed after its admitted input generation was captured."
    }
    return $current
}

function Read-BoundedStrictUtf8File {
    param([Parameter(Mandatory = $true)][IO.FileInfo]$File, [Parameter(Mandatory = $true)][string]$Label)
    $stream = [IO.File]::Open($File.FullName, [IO.FileMode]::Open, [IO.FileAccess]::Read, [IO.FileShare]::Read)
    try {
        if ($stream.Length -gt $script:MaxMetadataBytes) { throw "$Label exceeds the $($script:MaxMetadataBytes)-byte safety limit." }
        $bytes = [byte[]]::new([int]$stream.Length)
        $offset = 0
        while ($offset -lt $bytes.Length) {
            $read = $stream.Read($bytes, $offset, $bytes.Length - $offset)
            if ($read -le 0) { throw "$Label ended before its declared length." }
            $offset += $read
        }
        if ($stream.ReadByte() -ne -1) { throw "$Label changed while it was being read." }
        try { return [Text.UTF8Encoding]::new($false, $true).GetString($bytes) }
        catch [Text.DecoderFallbackException] { throw "$Label is not strict UTF-8." }
    }
    finally { $stream.Dispose() }
}

function Normalize-Thumbprint {
    param([string]$Thumbprint)
    return $Thumbprint.Replace(' ', '').ToUpperInvariant()
}

function Convert-ToStrictSemVerText {
    param([string]$Value, [string]$Label)

    if ([string]::IsNullOrWhiteSpace($Value)) { throw "$Label is missing." }
    $text = $Value.Trim()
    $match = [regex]::Match(
        $text,
        '^(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)(?:-([0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*))?(?:\+[0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*)?$',
        [Text.RegularExpressions.RegexOptions]::CultureInvariant)
    if (-not $match.Success) { throw "$Label is not strict SemVer: $text" }

    foreach ($index in 1..3) {
        $parsed = 0
        if (-not [int]::TryParse($match.Groups[$index].Value, [Globalization.NumberStyles]::None, [Globalization.CultureInfo]::InvariantCulture, [ref]$parsed)) {
            throw "$Label numeric component is outside the supported range: $text"
        }
    }

    if ($match.Groups[4].Success) {
        foreach ($identifier in $match.Groups[4].Value.Split('.')) {
            if ($identifier -match '^[0-9]+$' -and $identifier.Length -gt 1 -and $identifier[0] -eq '0') {
                throw "$Label has a numeric prerelease identifier with a leading zero: $text"
            }
        }
    }
    return $text
}

function Assert-AuthenticodeSigner {
    param([string]$Path, [string]$ExpectedSigner, [string]$Label)
    $signature = Get-AuthenticodeSignature -FilePath $Path
    if ($signature.Status -ne [System.Management.Automation.SignatureStatus]::Valid) { throw "$Label signature is not valid: $($signature.Status)" }
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
    catch { throw "$Label assembly version is unreadable: $($_.Exception.Message)" }
}

function Read-ManagedProductVersion {
    param([string]$Path, [string]$Label)
    try {
        $productVersion = [Diagnostics.FileVersionInfo]::GetVersionInfo($Path).ProductVersion
        return Convert-ToStrictSemVerText -Value ([string]$productVersion) -Label ("$Label product version")
    }
    catch { throw "$Label product version is unreadable: $($_.Exception.Message)" }
}

function Get-ZipEntrySha256 {
    param([System.IO.Compression.ZipArchiveEntry]$Entry)
    $input = $Entry.Open()
    $sha = [Security.Cryptography.SHA256]::Create()
    try {
        $bytes = $sha.ComputeHash($input)
        return ([BitConverter]::ToString($bytes)).Replace('-', '').ToUpperInvariant()
    }
    finally { $sha.Dispose(); $input.Dispose() }
}

function Get-SafeStagedFiles {
    param([Parameter(Mandatory = $true)][IO.DirectoryInfo]$Root)
    $rootDirectory = Resolve-OrdinaryNonReparseDirectory -Path $Root.FullName -Label 'Signed staging package root'
    $pending = [Collections.Generic.Stack[string]]::new()
    $files = [Collections.Generic.List[IO.FileInfo]]::new()
    $pending.Push($rootDirectory.FullName)
    while ($pending.Count -gt 0) {
        $directoryPath = $pending.Pop()
        $directory = Resolve-OrdinaryNonReparseDirectory -Path $directoryPath -Label 'Signed staging package directory'
        foreach ($item in @(Get-ChildItem -LiteralPath $directory.FullName -Force -ErrorAction Stop)) {
            if (($item.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
                throw "Signed staging package contains a reparse-backed entry: $($item.FullName)"
            }
            if ($item.PSIsContainer) {
                $pending.Push($item.FullName)
                continue
            }
            if (-not ($item -is [IO.FileInfo])) {
                throw "Signed staging package contains a non-regular filesystem entry: $($item.FullName)"
            }
            $safeFile = Resolve-OrdinaryNonReparseFile -Path $item.FullName -Label 'Signed staging package file'
            $files.Add($safeFile)
        }
    }
    return @($files | Sort-Object FullName)
}

function Assert-ZipPayloadMatchesSignedStaging {
    param([IO.FileInfo]$ZipFile, [IO.DirectoryInfo]$PackageRoot, [string]$ExpectedSigner)
    $tempParent = Resolve-OrdinaryNonReparseDirectory -Path ([IO.Path]::GetTempPath()) -Label 'Manifest verification temp parent'
    $temp = Join-Path $tempParent.FullName ('qs3d-manifest-verify-' + [Guid]::NewGuid().ToString('N'))
    if (Test-Path -LiteralPath $temp) { throw "Manifest verification workspace already exists: $temp" }
    New-Item -ItemType Directory -Path $temp | Out-Null
    $tempDirectory = Resolve-OrdinaryNonReparseDirectory -Path $temp -Label 'Manifest verification workspace'
    $archive = $null
    try {
        $archive = [System.IO.Compression.ZipFile]::OpenRead($ZipFile.FullName)
        $packageRootPath = $PackageRoot.FullName.TrimEnd([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar)
        $packageRootPrefix = $packageRootPath + [IO.Path]::DirectorySeparatorChar
        $stagedFiles = @(Get-SafeStagedFiles -Root $PackageRoot)
        if ($stagedFiles.Count -eq 0) { throw 'Signed staging package contains no regular files.' }

        $stagedByName = [Collections.Generic.Dictionary[string,string]]::new([StringComparer]::OrdinalIgnoreCase)
        $stagedStates = [Collections.Generic.Dictionary[string,object]]::new([StringComparer]::OrdinalIgnoreCase)
        foreach ($stagedFile in $stagedFiles) {
            $safeStagedFile = Resolve-OrdinaryNonReparseFile -Path $stagedFile.FullName -Label 'Signed staging package file'
            $fullPath = $safeStagedFile.FullName
            if (-not $fullPath.StartsWith($packageRootPrefix, [StringComparison]::OrdinalIgnoreCase)) { throw "Staged package file escaped package root: $fullPath" }
            $relative = $fullPath.Substring($packageRootPrefix.Length).Replace([IO.Path]::DirectorySeparatorChar, '/').Replace([IO.Path]::AltDirectorySeparatorChar, '/')
            if ($stagedByName.ContainsKey($relative)) { throw "Duplicate/case-colliding staged package path: $relative" }
            $state = Get-StableFileState -Path $fullPath -Label ("Signed staging package file " + $relative)
            $stagedByName.Add($relative, $state.Path)
            $stagedStates.Add($relative, $state)
        }

        $zipByName = [Collections.Generic.Dictionary[string,System.IO.Compression.ZipArchiveEntry]]::new([StringComparer]::OrdinalIgnoreCase)
        foreach ($entry in $archive.Entries) {
            if ([string]::IsNullOrEmpty([string]$entry.Name)) { continue }
            $name = [string]$entry.FullName
            if ([string]::IsNullOrWhiteSpace($name) -or $name.IndexOf([char]0) -ge 0 -or [IO.Path]::IsPathRooted($name) -or $name.Contains('\') -or $name.Contains(':')) { throw "Unsafe package ZIP entry: $name" }
            $segments = @($name.Split('/'))
            if (@($segments | Where-Object { [string]::IsNullOrWhiteSpace($_) -or $_ -eq '.' -or $_ -eq '..' }).Count -gt 0) { throw "Unsafe package ZIP entry: $name" }
            if ($zipByName.ContainsKey($name)) { throw "Duplicate/case-colliding package ZIP path: $name" }
            if (-not $stagedByName.ContainsKey($name)) { throw "Package ZIP contains file not present in signed staging: $name" }
            $zipByName.Add($name, $entry)
        }

        foreach ($name in $stagedByName.Keys) {
            if (-not $zipByName.ContainsKey($name)) { throw "Package ZIP is missing signed staging file: $name" }
            $stagedState = $stagedStates[$name]
            $null = Assert-StableFileState -Expected $stagedState -Label ("Signed staging package file " + $name)
            $stagedHash = [string]$stagedState.Sha256
            $zippedHash = Get-ZipEntrySha256 -Entry $zipByName[$name]
            $null = Assert-StableFileState -Expected $stagedState -Label ("Signed staging package file " + $name)
            if ($stagedHash -ne $zippedHash) { throw "Package ZIP payload does not match signed staging file: $name" }
        }
        if ($zipByName.Count -ne $stagedByName.Count) { throw "Package ZIP/staging file-count mismatch. ZIP=$($zipByName.Count), staging=$($stagedByName.Count)." }

        foreach ($name in $SignedPayloadNames) {
            if (-not $zipByName.ContainsKey($name)) { throw "Package ZIP is missing signed executable payload: $name" }
            $destination = Join-Path $tempDirectory.FullName $name
            if (Test-Path -LiteralPath $destination) { throw "Manifest verification destination already exists: $destination" }
            $input = $zipByName[$name].Open()
            $output = [IO.File]::CreateNew($destination)
            try { $input.CopyTo($output) }
            finally { $output.Dispose(); $input.Dispose() }
            $verified = Resolve-OrdinaryNonReparseFile -Path $destination -Label 'Extracted manifest verification payload'
            Assert-AuthenticodeSigner -Path $verified.FullName -ExpectedSigner $ExpectedSigner -Label ("Zipped QS3D executable payload " + $name)
        }
    }
    finally {
        if ($archive) { $archive.Dispose() }
        if (Test-Path -LiteralPath $temp) {
            $workspace = Resolve-OrdinaryNonReparseDirectory -Path $temp -Label 'Manifest verification workspace cleanup'
            foreach ($child in @(Get-ChildItem -LiteralPath $workspace.FullName -Force)) {
                if ($child.PSIsContainer) { throw "Unexpected directory in manifest verification workspace: $($child.FullName)" }
                $safeChild = Resolve-OrdinaryNonReparseFile -Path $child.FullName -Label 'Manifest verification cleanup file'
                Remove-Item -LiteralPath $safeChild.FullName -Force
            }
            if (@(Get-ChildItem -LiteralPath $workspace.FullName -Force).Count -ne 0) { throw 'Manifest verification workspace was not empty after bounded cleanup.' }
            Remove-Item -LiteralPath $workspace.FullName -Force
        }
    }
}

$uri = $null
if (-not [Uri]::TryCreate($PackageUri, [UriKind]::Absolute, [ref]$uri) -or $uri.Scheme -ne [Uri]::UriSchemeHttps -or [string]::IsNullOrWhiteSpace($uri.Host)) { throw 'PackageUri must be an absolute HTTPS URI.' }
if ($uri.UserInfo) { throw 'PackageUri must not contain embedded credentials.' }

$package = Resolve-OrdinaryNonReparseDirectory -Path $PackageDirectory -Label 'Signed package directory'
$packagePath = $package.FullName.TrimEnd([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar)
$packageRoot = $packagePath + [IO.Path]::DirectorySeparatorChar
$zip = Resolve-OrdinaryNonReparseFile -Path $PackageZip -Label 'Signed package ZIP'
$zipPath = $zip.FullName
$outputFull = [IO.Path]::GetFullPath($OutputPath)
if (-not [string]::Equals([IO.Path]::GetExtension($outputFull), '.json', [StringComparison]::OrdinalIgnoreCase)) { throw "OutputPath must use the .json extension: $outputFull" }
if ([string]::Equals($outputFull, $packagePath, [StringComparison]::OrdinalIgnoreCase) -or $outputFull.StartsWith($packageRoot, [StringComparison]::OrdinalIgnoreCase)) { throw 'OutputPath must be outside PackageDirectory so manifest generation cannot overwrite signed staging.' }
if ([string]::Equals($outputFull, $zipPath, [StringComparison]::OrdinalIgnoreCase)) { throw 'OutputPath must not alias PackageZip.' }
$outputParentPath = Split-Path -Parent $outputFull
if ([string]::IsNullOrWhiteSpace($outputParentPath)) { throw 'OutputPath must have a parent directory.' }
$outputParent = Resolve-OrdinaryNonReparseDirectory -Path $outputParentPath -Label 'Update manifest output parent'
$hadExistingOutput = $false
if (Test-Path -LiteralPath $outputFull) {
    Resolve-OrdinaryNonReparseFile -Path $outputFull -Label 'Existing update manifest' | Out-Null
    $hadExistingOutput = $true
}

$metadataFile = Resolve-OrdinaryNonReparseFile -Path (Join-Path $package.FullName 'PACKAGE-METADATA.json') -Label 'PACKAGE-METADATA.json'
$metadataState = Get-StableFileState -Path $metadataFile.FullName -Label 'PACKAGE-METADATA.json'
$zipState = Get-StableFileState -Path $zip.FullName -Label 'Signed package ZIP'
$payloadFiles = @{}
$payloadStates = @{}
foreach ($name in $SignedPayloadNames) {
    $payloadFiles[$name] = Resolve-OrdinaryNonReparseFile -Path (Join-Path $package.FullName $name) -Label ("Signed payload " + $name)
    $payloadStates[$name] = Get-StableFileState -Path $payloadFiles[$name].FullName -Label ("Signed payload " + $name)
}

$metadataText = Read-BoundedStrictUtf8File -File $metadataFile -Label 'PACKAGE-METADATA.json'
$metadataFile = Assert-StableFileState -Expected $metadataState -Label 'PACKAGE-METADATA.json'
try { $metadata = $metadataText | ConvertFrom-Json -ErrorAction Stop }
catch { throw "PACKAGE-METADATA.json is invalid JSON: $($_.Exception.Message)" }
if ([string]$metadata.product -ne 'QS3D') { throw 'PACKAGE-METADATA product must be QS3D.' }
if ([string]$metadata.target -ne 'BricsCAD V25 x64') { throw 'PACKAGE-METADATA target must be BricsCAD V25 x64.' }
if (-not $metadata.PSObject.Properties['version']) { throw 'PACKAGE-METADATA is missing version.' }
if (-not $metadata.PSObject.Properties['productVersion']) { throw 'PACKAGE-METADATA is missing productVersion.' }
try { $version = [Version]::Parse([string]$metadata.version) }
catch { throw "PACKAGE-METADATA version is invalid: $($metadata.version)" }
$productVersion = Convert-ToStrictSemVerText -Value ([string]$metadata.productVersion) -Label 'PACKAGE-METADATA productVersion'

$expectedSigner = Normalize-Thumbprint $ExpectedSignerThumbprint
foreach ($name in $SignedPayloadNames) {
    Assert-AuthenticodeSigner -Path $payloadFiles[$name].FullName -ExpectedSigner $expectedSigner -Label ("QS3D executable payload " + $name)
    $payloadFiles[$name] = Assert-StableFileState -Expected $payloadStates[$name] -Label ("Signed payload " + $name)
}
$managedIdentityNames = @('QS3D.BricsCAD.V25.dll', 'QS3D.Core.dll')
$managedIdentities = @{}
foreach ($name in $managedIdentityNames) {
    $path = $payloadFiles[$name].FullName
    $assemblyVersion = Read-ManagedAssemblyVersion -Path $path -Label $name
    $payloadFiles[$name] = Assert-StableFileState -Expected $payloadStates[$name] -Label ("Signed payload " + $name)
    if ($version -ne $assemblyVersion) { throw "PACKAGE-METADATA version $version does not match signed $name assembly version $assemblyVersion." }
    $managedProductVersion = Read-ManagedProductVersion -Path $payloadFiles[$name].FullName -Label $name
    $payloadFiles[$name] = Assert-StableFileState -Expected $payloadStates[$name] -Label ("Signed payload " + $name)
    if (-not [string]::Equals($productVersion, $managedProductVersion, [StringComparison]::Ordinal)) { throw "PACKAGE-METADATA productVersion $productVersion does not match signed $name product version $managedProductVersion." }
    $managedIdentities[$name] = [pscustomobject]@{ AssemblyVersion = $assemblyVersion; ProductVersion = $managedProductVersion }
}
$signedPluginVersion = $managedIdentities['QS3D.BricsCAD.V25.dll'].AssemblyVersion
$signedPluginProductVersion = $managedIdentities['QS3D.BricsCAD.V25.dll'].ProductVersion
Assert-ZipPayloadMatchesSignedStaging -ZipFile $zip -PackageRoot $package -ExpectedSigner $expectedSigner
$zip = Assert-StableFileState -Expected $zipState -Label 'Signed package ZIP'

$zipHash = [string]$zipState.Sha256
$manifest = [ordered]@{
    schemaVersion = 2
    product = 'QS3D'
    target = 'BricsCAD V25 x64'
    productVersion = $signedPluginProductVersion
    version = $signedPluginVersion.ToString()
    packageUri = $uri.AbsoluteUri
    sha256 = $zipHash
    signerThumbprint = $expectedSigner
    generatedUtc = [DateTime]::UtcNow.ToString('o')
}

if ($PSCmdlet.ShouldProcess($outputFull, 'Write QS3D update manifest')) {
    $manifestJson = $manifest | ConvertTo-Json
    $utf8NoBom = [Text.UTF8Encoding]::new($false, $true)
    $nonce = [Guid]::NewGuid().ToString('N')
    $stagePath = Join-Path $outputParent.FullName (([IO.Path]::GetFileName($outputFull)) + ".tmp-$nonce")
    $backupPath = Join-Path $outputParent.FullName (([IO.Path]::GetFileName($outputFull)) + ".bak-$nonce")
    if (Test-Path -LiteralPath $stagePath) { throw "Refusing to reuse update-manifest staging path: $stagePath" }
    if (Test-Path -LiteralPath $backupPath) { throw "Refusing to reuse update-manifest backup path: $backupPath" }
    try {
        [IO.File]::WriteAllText($stagePath, $manifestJson + [Environment]::NewLine, $utf8NoBom)
        $stage = Resolve-OrdinaryNonReparseFile -Path $stagePath -Label 'Update manifest staging file'
        if ($hadExistingOutput) { [IO.File]::Replace($stage.FullName, $outputFull, $backupPath, $true) }
        else { [IO.File]::Move($stage.FullName, $outputFull) }
        $published = Resolve-OrdinaryNonReparseFile -Path $outputFull -Label 'Published update manifest'
        $publishedText = Read-BoundedStrictUtf8File -File $published -Label 'Published update manifest'
        $null = $publishedText | ConvertFrom-Json -ErrorAction Stop
    }
    finally {
        if (Test-Path -LiteralPath $stagePath) {
            $stage = Resolve-OrdinaryNonReparseFile -Path $stagePath -Label 'Update manifest staging cleanup'
            Remove-Item -LiteralPath $stage.FullName -Force
        }
        if (Test-Path -LiteralPath $backupPath) {
            $backup = Resolve-OrdinaryNonReparseFile -Path $backupPath -Label 'Update manifest backup cleanup'
            Remove-Item -LiteralPath $backup.FullName -Force
        }
    }
}

Write-Host "Update manifest: $outputFull"
Write-Host "Product version: $signedPluginProductVersion"
Write-Host "Assembly version: $($signedPluginVersion.ToString())"
Write-Host "Package SHA256: $zipHash"
Write-Host "Signer: $expectedSigner"