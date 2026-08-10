[CmdletBinding(SupportsShouldProcess = $true, ConfirmImpact = 'High')]
param(
    [Parameter(Mandatory = $true)]
    [ValidatePattern('^https://')]
    [string]$ManifestUri,

    [Parameter(Mandatory = $true)]
    [ValidatePattern('^[0-9A-Fa-f]{40}$')]
    [string]$ExpectedSignerThumbprint,

    [string]$InstallDirectory = (Join-Path $env:LOCALAPPDATA 'QS3D\BricsCAD-V25'),

    [ValidateSet('OnCommand', 'OnStartup')]
    [string]$LoadMode = 'OnCommand',

    [string[]]$VersionKeys,
    [string[]]$LanguageKeys,
    [string[]]$AllowedPackageHost,

    [ValidateRange(1, 512)]
    [int]$MaxPackageSizeMB = 256,

    [ValidateRange(1, 2048)]
    [int]$MaxExpandedPackageSizeMB = 512,

    [ValidateRange(1, 20000)]
    [int]$MaxArchiveEntries = 4096,

    [switch]$AllowSameVersion
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.IO.Compression.FileSystem

$SignedPayloadNames = @(
    'QS3D.BricsCAD.V25.dll',
    'QS3D.Core.dll',
    'install-v25-autoload.ps1',
    'uninstall-v25-autoload.ps1',
    'update-v25.ps1'
)

function Normalize-Thumbprint {
    param([string]$Thumbprint)
    return ($Thumbprint.Replace(' ', '').ToUpperInvariant())
}

function Convert-ToSafeHttpsUri {
    param([string]$Value, [string]$Label)
    $uri = $null
    if (-not [Uri]::TryCreate($Value, [UriKind]::Absolute, [ref]$uri)) { throw "$Label is not an absolute URI." }
    if ($uri.Scheme -ne [Uri]::UriSchemeHttps) { throw "$Label must use HTTPS." }
    if ([string]::IsNullOrWhiteSpace($uri.Host)) { throw "$Label must include a host." }
    if ($uri.UserInfo) { throw "$Label must not contain embedded credentials." }
    return $uri
}

function Require-ManifestProperty {
    param($Manifest, [string]$Name)
    $property = $Manifest.PSObject.Properties[$Name]
    if (-not $property) { throw "Update manifest is missing '$Name'." }
    return $property.Value
}

function Read-InstalledVersion {
    param([string]$Directory)

    $metadataPath = Join-Path $Directory 'PACKAGE-METADATA.json'
    $pluginPath = Join-Path $Directory 'QS3D.BricsCAD.V25.dll'
    $hasMetadata = Test-Path -LiteralPath $metadataPath -PathType Leaf
    $hasPlugin = Test-Path -LiteralPath $pluginPath -PathType Leaf
    if (-not $hasMetadata -and -not $hasPlugin) { return [Version]'0.0.0.0' }

    $metadataVersion = $null
    if ($hasMetadata) {
        try {
            $metadata = Get-Content -LiteralPath $metadataPath -Raw | ConvertFrom-Json
            if (-not $metadata.PSObject.Properties['version']) {
                throw 'version property is missing'
            }
            $metadataVersion = [Version]::Parse([string]$metadata.version)
        }
        catch {
            throw "Installed PACKAGE-METADATA.json has an invalid version: $($_.Exception.Message)"
        }
    }

    $pluginVersion = $null
    if ($hasPlugin) {
        try {
            $pluginVersion = [Reflection.AssemblyName]::GetAssemblyName($pluginPath).Version
            if (-not $pluginVersion) { throw 'assembly version is missing' }
        }
        catch {
            throw "Installed QS3D plugin assembly version is unreadable: $($_.Exception.Message)"
        }
    }

    if ($metadataVersion -and $pluginVersion -and $metadataVersion -ne $pluginVersion) {
        throw "Installed package metadata version $metadataVersion does not match installed plugin assembly version $pluginVersion. Refusing update until installed state is repaired."
    }
    if ($pluginVersion) { return $pluginVersion }
    if ($metadataVersion) { return $metadataVersion }
    throw 'Installed QS3D state does not expose a readable version.'
}

function Read-SignedPluginVersion {
    param([string]$Path)
    try {
        $version = [Reflection.AssemblyName]::GetAssemblyName($Path).Version
        if (-not $version) { throw 'assembly version is missing' }
        return $version
    }
    catch {
        throw "Signed QS3D plugin assembly version is unreadable: $($_.Exception.Message)"
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
    if ($actualSigner -ne $ExpectedSigner) {
        throw "$Label signer mismatch. Expected $ExpectedSigner, got $actualSigner."
    }
}

function Assert-SafeArchive {
    param(
        [string]$ZipPath,
        [string]$DestinationRoot,
        [int64]$MaxExpandedBytes,
        [int]$MaxEntries
    )

    $archive = [System.IO.Compression.ZipFile]::OpenRead($ZipPath)
    try {
        $root = [IO.Path]::GetFullPath($DestinationRoot).TrimEnd([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar
        [int64]$expandedBytes = 0
        $entryCount = 0
        foreach ($entry in $archive.Entries) {
            $entryCount++
            if ($entryCount -gt $MaxEntries) { throw "Package archive exceeds the allowed entry count ($MaxEntries)." }
            $name = [string]$entry.FullName
            if ([string]::IsNullOrWhiteSpace($name) -or $name.IndexOf([char]0) -ge 0 -or [IO.Path]::IsPathRooted($name) -or $name.Contains('\') -or $name.Contains(':')) {
                throw "Unsafe package archive entry: $name"
            }
            $relative = $name.TrimEnd('/')
            if ([string]::IsNullOrWhiteSpace($relative)) { throw "Unsafe package archive entry: $name" }
            $segments = @($relative.Split('/'))
            if (@($segments | Where-Object { [string]::IsNullOrWhiteSpace($_) -or $_ -eq '.' -or $_ -eq '..' }).Count -gt 0) {
                throw "Unsafe package archive entry: $name"
            }
            $target = [IO.Path]::GetFullPath((Join-Path $DestinationRoot ($relative.Replace('/', [IO.Path]::DirectorySeparatorChar))))
            if (-not $target.StartsWith($root, [StringComparison]::OrdinalIgnoreCase)) { throw "Unsafe package archive entry: $name" }
            $entryLength = [int64]$entry.Length
            if ($entryLength -lt 0 -or $expandedBytes -gt ($MaxExpandedBytes - $entryLength)) {
                throw "Package expanded size exceeds the allowed maximum ($MaxExpandedBytes bytes)."
            }
            $expandedBytes += $entryLength
        }
        if ($entryCount -eq 0) { throw 'Downloaded package archive contains no entries.' }
    }
    finally {
        $archive.Dispose()
    }
}

function Assert-PackageRoot {
    param([string]$Directory, [string]$ExpectedSigner)

    foreach ($name in @('COMMANDS.txt', 'PACKAGE-METADATA.json', 'SHA256SUMS.txt') + $SignedPayloadNames) {
        if (-not (Test-Path -LiteralPath (Join-Path $Directory $name) -PathType Leaf)) {
            throw "Downloaded package is missing required payload: $name"
        }
    }

    foreach ($name in $SignedPayloadNames) {
        Assert-AuthenticodeSigner -Path (Join-Path $Directory $name) -ExpectedSigner $ExpectedSigner -Label ("Downloaded QS3D executable payload " + $name)
    }

    $hashManifest = Join-Path $Directory 'SHA256SUMS.txt'
    $verified = 0
    foreach ($line in Get-Content -LiteralPath $hashManifest) {
        if ([string]::IsNullOrWhiteSpace($line)) { continue }
        if ($line -notmatch '^([0-9A-Fa-f]{64})\s{2}(.+)$') { throw "Invalid SHA256SUMS entry: $line" }
        $expected = $Matches[1].ToUpperInvariant()
        $name = $Matches[2].Trim()
        if ($name -eq 'SHA256SUMS.txt' -or [IO.Path]::IsPathRooted($name) -or $name.Contains('\') -or $name.Contains(':')) {
            throw "Unsafe SHA256SUMS entry: $name"
        }
        $segments = @($name.Split('/'))
        if ($segments.Count -eq 0 -or @($segments | Where-Object { [string]::IsNullOrWhiteSpace($_) -or $_ -eq '.' -or $_ -eq '..' }).Count -gt 0) {
            throw "Unsafe SHA256SUMS entry: $name"
        }
        $packageRoot = [IO.Path]::GetFullPath($Directory).TrimEnd([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar
        $path = [IO.Path]::GetFullPath((Join-Path $Directory ($name.Replace('/', [IO.Path]::DirectorySeparatorChar))))
        if (-not $path.StartsWith($packageRoot, [StringComparison]::OrdinalIgnoreCase)) { throw "Unsafe SHA256SUMS entry: $name" }
        if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { throw "Missing hashed payload: $name" }
        $actual = (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToUpperInvariant()
        if ($actual -ne $expected) { throw "SHA-256 mismatch for downloaded payload: $name" }
        $verified++
    }
    if ($verified -eq 0) { throw 'Downloaded SHA256SUMS.txt contains no payload entries.' }
}

if (Get-Process -Name bricscad -ErrorAction SilentlyContinue) {
    throw 'Close all BricsCAD processes before updating QS3D.'
}

$manifestAddress = Convert-ToSafeHttpsUri -Value $ManifestUri -Label 'ManifestUri'
$expectedSigner = Normalize-Thumbprint $ExpectedSignerThumbprint
$allowedHosts = @($manifestAddress.Host)
if ($AllowedPackageHost) { $allowedHosts += $AllowedPackageHost }
$allowedHosts = @($allowedHosts | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | ForEach-Object { $_.Trim().ToLowerInvariant() } | Sort-Object -Unique)

$tempRoot = Join-Path ([IO.Path]::GetTempPath()) ('qs3d-update-' + [Guid]::NewGuid().ToString('N'))
$manifestPath = Join-Path $tempRoot 'manifest.json'
$zipPath = Join-Path $tempRoot 'package.zip'
$extractRoot = Join-Path $tempRoot 'package'

try {
    New-Item -ItemType Directory -Path $tempRoot -Force | Out-Null
    Invoke-WebRequest -Uri $manifestAddress.AbsoluteUri -OutFile $manifestPath -UseBasicParsing
    $manifestFile = Get-Item -LiteralPath $manifestPath
    if ($manifestFile.Length -le 0 -or $manifestFile.Length -gt 65536) { throw 'Update manifest must be between 1 byte and 64 KiB.' }

    $manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
    $schemaVersion = [int](Require-ManifestProperty -Manifest $manifest -Name 'schemaVersion')
    if ($schemaVersion -ne 1) { throw "Unsupported update manifest schemaVersion: $schemaVersion" }
    if ([string](Require-ManifestProperty -Manifest $manifest -Name 'product') -ne 'QS3D') { throw 'Update manifest product must be QS3D.' }
    if ([string](Require-ManifestProperty -Manifest $manifest -Name 'target') -ne 'BricsCAD V25 x64') { throw 'Update manifest target must be BricsCAD V25 x64.' }

    $versionText = [string](Require-ManifestProperty -Manifest $manifest -Name 'version')
    try { $targetVersion = [Version]::Parse($versionText) }
    catch { throw "Update manifest version is invalid: $versionText" }

    $packageAddress = Convert-ToSafeHttpsUri -Value ([string](Require-ManifestProperty -Manifest $manifest -Name 'packageUri')) -Label 'packageUri'
    if ($allowedHosts -notcontains $packageAddress.Host.ToLowerInvariant()) {
        throw "Package host '$($packageAddress.Host)' is not approved. Allowed hosts: $($allowedHosts -join ', ')"
    }

    $expectedZipHash = ([string](Require-ManifestProperty -Manifest $manifest -Name 'sha256')).Trim().ToUpperInvariant()
    if ($expectedZipHash -notmatch '^[0-9A-F]{64}$') { throw 'Update manifest sha256 must be 64 hexadecimal characters.' }
    $manifestSigner = Normalize-Thumbprint ([string](Require-ManifestProperty -Manifest $manifest -Name 'signerThumbprint'))
    if ($manifestSigner -ne $expectedSigner) { throw 'Update manifest signerThumbprint does not match ExpectedSignerThumbprint.' }

    $installedVersion = Read-InstalledVersion -Directory $InstallDirectory
    if ($targetVersion -lt $installedVersion) { throw "Refusing downgrade from $installedVersion to $targetVersion." }
    if ($targetVersion -eq $installedVersion -and -not $AllowSameVersion) { throw "QS3D $targetVersion is already installed. Use -AllowSameVersion only for an intentional repair." }

    if (-not $PSCmdlet.ShouldProcess($InstallDirectory, "Update QS3D from $installedVersion to $targetVersion")) { return }

    Invoke-WebRequest -Uri $packageAddress.AbsoluteUri -OutFile $zipPath -UseBasicParsing
    $zipFile = Get-Item -LiteralPath $zipPath
    $maxBytes = [int64]$MaxPackageSizeMB * 1MB
    if ($zipFile.Length -le 0 -or $zipFile.Length -gt $maxBytes) {
        throw "Downloaded package size $($zipFile.Length) bytes is outside the allowed range (max $maxBytes)."
    }
    $actualZipHash = (Get-FileHash -LiteralPath $zipPath -Algorithm SHA256).Hash.ToUpperInvariant()
    if ($actualZipHash -ne $expectedZipHash) { throw 'Downloaded package SHA-256 does not match the update manifest.' }

    $maxExpandedBytes = [int64]$MaxExpandedPackageSizeMB * 1MB
    Assert-SafeArchive -ZipPath $zipPath -DestinationRoot $extractRoot -MaxExpandedBytes $maxExpandedBytes -MaxEntries $MaxArchiveEntries
    New-Item -ItemType Directory -Path $extractRoot -Force | Out-Null
    Expand-Archive -LiteralPath $zipPath -DestinationPath $extractRoot -Force
    Assert-PackageRoot -Directory $extractRoot -ExpectedSigner $expectedSigner

    $signedPluginVersion = Read-SignedPluginVersion -Path (Join-Path $extractRoot 'QS3D.BricsCAD.V25.dll')
    if ($signedPluginVersion -ne $targetVersion) {
        throw "Signed QS3D plugin assembly version $signedPluginVersion does not match manifest version $targetVersion. Refusing replay/downgrade metadata substitution."
    }

    $downloadedMetadata = Get-Content -LiteralPath (Join-Path $extractRoot 'PACKAGE-METADATA.json') -Raw | ConvertFrom-Json
    if (-not $downloadedMetadata.PSObject.Properties['version']) { throw 'Downloaded PACKAGE-METADATA.json is missing version.' }
    $packageVersion = [Version]::Parse([string]$downloadedMetadata.version)
    if ($packageVersion -ne $signedPluginVersion) {
        throw "Downloaded package metadata version $packageVersion does not match signed plugin assembly version $signedPluginVersion."
    }
    if ($packageVersion -ne $targetVersion) { throw "Downloaded package version $packageVersion does not match manifest version $targetVersion." }

    $installer = Join-Path $extractRoot 'install-v25-autoload.ps1'
    $arguments = @{
        PackageDirectory = $extractRoot
        InstallDirectory = $InstallDirectory
        LoadMode = $LoadMode
        Force = $true
        RequireSigned = $true
        ExpectedSignerThumbprint = $expectedSigner
    }
    if ($VersionKeys) { $arguments.VersionKeys = $VersionKeys }
    if ($LanguageKeys) { $arguments.LanguageKeys = $LanguageKeys }
    & $installer @arguments

    Write-Host "QS3D updated securely to $targetVersion."
}
finally {
    if (Test-Path -LiteralPath $tempRoot) { Remove-Item -LiteralPath $tempRoot -Recurse -Force -ErrorAction SilentlyContinue }
}
