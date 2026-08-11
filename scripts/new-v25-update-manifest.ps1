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

$SignedPayloadNames = @(
    'QS3D.BricsCAD.V25.dll',
    'QS3D.Core.dll',
    'install-v25-autoload.ps1',
    'uninstall-v25-autoload.ps1',
    'update-v25.ps1'
)

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
    if ($signature.Status -ne [System.Management.Automation.SignatureStatus]::Valid) {
        throw "$Label signature is not valid: $($signature.Status)"
    }
    if (-not $signature.SignerCertificate) { throw "$Label signature has no signer certificate." }
    $actualSigner = Normalize-Thumbprint $signature.SignerCertificate.Thumbprint
    if ($actualSigner -ne $ExpectedSigner) { throw "$Label signer mismatch. Expected $ExpectedSigner, got $actualSigner." }
}

function Read-PluginAssemblyVersion {
    param([string]$Path)
    try {
        $version = [Reflection.AssemblyName]::GetAssemblyName($Path).Version
        if (-not $version) { throw 'assembly version is missing' }
        return $version
    }
    catch {
        throw "QS3D plugin assembly version is unreadable: $($_.Exception.Message)"
    }
}

function Read-PluginProductVersion {
    param([string]$Path)
    try {
        $productVersion = [Diagnostics.FileVersionInfo]::GetVersionInfo($Path).ProductVersion
        return Convert-ToStrictSemVerText -Value ([string]$productVersion) -Label 'Signed QS3D plugin product version'
    }
    catch {
        throw "QS3D plugin product version is unreadable: $($_.Exception.Message)"
    }
}

function Get-ZipEntrySha256 {
    param([System.IO.Compression.ZipArchiveEntry]$Entry)

    $input = $Entry.Open()
    $sha = [Security.Cryptography.SHA256]::Create()
    try {
        $bytes = $sha.ComputeHash($input)
        return ([BitConverter]::ToString($bytes)).Replace('-', '').ToUpperInvariant()
    }
    finally {
        $sha.Dispose()
        $input.Dispose()
    }
}

function Assert-ZipPayloadMatchesSignedStaging {
    param([string]$ZipPath, [string]$PackageRoot, [string]$ExpectedSigner)
    $temp = Join-Path ([IO.Path]::GetTempPath()) ('qs3d-manifest-verify-' + [Guid]::NewGuid().ToString('N'))
    New-Item -ItemType Directory -Path $temp -Force | Out-Null
    $archive = $null
    try {
        $archive = [System.IO.Compression.ZipFile]::OpenRead($ZipPath)
        $packageRootPath = [IO.Path]::GetFullPath($PackageRoot).TrimEnd([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar)
        $packageRootPrefix = $packageRootPath + [IO.Path]::DirectorySeparatorChar
        $stagedFiles = @(Get-ChildItem -LiteralPath $PackageRoot -File -Recurse)
        if ($stagedFiles.Count -eq 0) { throw 'Signed staging package contains no regular files.' }

        $stagedByName = [Collections.Generic.Dictionary[string,string]]::new([StringComparer]::OrdinalIgnoreCase)
        foreach ($stagedFile in $stagedFiles) {
            $fullPath = [IO.Path]::GetFullPath($stagedFile.FullName)
            if (-not $fullPath.StartsWith($packageRootPrefix, [StringComparison]::OrdinalIgnoreCase)) {
                throw "Staged package file escaped package root: $($stagedFile.FullName)"
            }
            $relative = $fullPath.Substring($packageRootPrefix.Length).Replace([IO.Path]::DirectorySeparatorChar, '/').Replace([IO.Path]::AltDirectorySeparatorChar, '/')
            if ($stagedByName.ContainsKey($relative)) { throw "Duplicate/case-colliding staged package path: $relative" }
            $stagedByName.Add($relative, $fullPath)
        }

        $zipByName = [Collections.Generic.Dictionary[string,System.IO.Compression.ZipArchiveEntry]]::new([StringComparer]::OrdinalIgnoreCase)
        foreach ($entry in $archive.Entries) {
            if ([string]::IsNullOrEmpty([string]$entry.Name)) { continue }
            $name = [string]$entry.FullName
            if ([string]::IsNullOrWhiteSpace($name) -or $name.IndexOf([char]0) -ge 0 -or [IO.Path]::IsPathRooted($name) -or $name.Contains('\') -or $name.Contains(':')) {
                throw "Unsafe package ZIP entry: $name"
            }
            $segments = @($name.Split('/'))
            if (@($segments | Where-Object { [string]::IsNullOrWhiteSpace($_) -or $_ -eq '.' -or $_ -eq '..' }).Count -gt 0) {
                throw "Unsafe package ZIP entry: $name"
            }
            if ($zipByName.ContainsKey($name)) { throw "Duplicate/case-colliding package ZIP path: $name" }
            if (-not $stagedByName.ContainsKey($name)) { throw "Package ZIP contains file not present in signed staging: $name" }
            $zipByName.Add($name, $entry)
        }

        foreach ($name in $stagedByName.Keys) {
            if (-not $zipByName.ContainsKey($name)) { throw "Package ZIP is missing signed staging file: $name" }
            $stagedHash = (Get-FileHash -LiteralPath $stagedByName[$name] -Algorithm SHA256).Hash.ToUpperInvariant()
            $zippedHash = Get-ZipEntrySha256 -Entry $zipByName[$name]
            if ($stagedHash -ne $zippedHash) { throw "Package ZIP payload does not match signed staging file: $name" }
        }
        if ($zipByName.Count -ne $stagedByName.Count) {
            throw "Package ZIP/staging file-count mismatch. ZIP=$($zipByName.Count), staging=$($stagedByName.Count)."
        }

        foreach ($name in $SignedPayloadNames) {
            if (-not $zipByName.ContainsKey($name)) { throw "Package ZIP is missing signed executable payload: $name" }
            $destination = Join-Path $temp $name
            $input = $zipByName[$name].Open()
            $output = [IO.File]::Create($destination)
            try { $input.CopyTo($output) }
            finally { $output.Dispose(); $input.Dispose() }
            Assert-AuthenticodeSigner -Path $destination -ExpectedSigner $ExpectedSigner -Label ("Zipped QS3D executable payload " + $name)
        }
    }
    finally {
        if ($archive) { $archive.Dispose() }
        if (Test-Path -LiteralPath $temp) { Remove-Item -LiteralPath $temp -Recurse -Force -ErrorAction SilentlyContinue }
    }
}

$uri = $null
if (-not [Uri]::TryCreate($PackageUri, [UriKind]::Absolute, [ref]$uri) -or $uri.Scheme -ne [Uri]::UriSchemeHttps -or [string]::IsNullOrWhiteSpace($uri.Host)) {
    throw 'PackageUri must be an absolute HTTPS URI.'
}
if ($uri.UserInfo) { throw 'PackageUri must not contain embedded credentials.' }

$package = (Resolve-Path -LiteralPath $PackageDirectory).Path
$zip = (Resolve-Path -LiteralPath $PackageZip).Path
$metadataPath = Join-Path $package 'PACKAGE-METADATA.json'
if (-not (Test-Path -LiteralPath $metadataPath -PathType Leaf)) { throw "Missing package artifact: $metadataPath" }
foreach ($name in $SignedPayloadNames) {
    $path = Join-Path $package $name
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { throw "Missing package artifact: $path" }
}

$metadata = Get-Content -LiteralPath $metadataPath -Raw | ConvertFrom-Json
if ([string]$metadata.product -ne 'QS3D') { throw 'PACKAGE-METADATA product must be QS3D.' }
if ([string]$metadata.target -ne 'BricsCAD V25 x64') { throw 'PACKAGE-METADATA target must be BricsCAD V25 x64.' }
if (-not $metadata.PSObject.Properties['version']) { throw 'PACKAGE-METADATA is missing version.' }
if (-not $metadata.PSObject.Properties['productVersion']) { throw 'PACKAGE-METADATA is missing productVersion.' }
try { $version = [Version]::Parse([string]$metadata.version) }
catch { throw "PACKAGE-METADATA version is invalid: $($metadata.version)" }
$productVersion = Convert-ToStrictSemVerText -Value ([string]$metadata.productVersion) -Label 'PACKAGE-METADATA productVersion'

$expectedSigner = Normalize-Thumbprint $ExpectedSignerThumbprint
foreach ($name in $SignedPayloadNames) {
    Assert-AuthenticodeSigner -Path (Join-Path $package $name) -ExpectedSigner $expectedSigner -Label ("QS3D executable payload " + $name)
}
$pluginPath = Join-Path $package 'QS3D.BricsCAD.V25.dll'
$signedPluginVersion = Read-PluginAssemblyVersion -Path $pluginPath
if ($version -ne $signedPluginVersion) {
    throw "PACKAGE-METADATA version $version does not match signed QS3D plugin assembly version $signedPluginVersion."
}
$signedPluginProductVersion = Read-PluginProductVersion -Path $pluginPath
if (-not [string]::Equals($productVersion, $signedPluginProductVersion, [StringComparison]::Ordinal)) {
    throw "PACKAGE-METADATA productVersion $productVersion does not match signed QS3D plugin product version $signedPluginProductVersion."
}
Assert-ZipPayloadMatchesSignedStaging -ZipPath $zip -PackageRoot $package -ExpectedSigner $expectedSigner

$zipHash = (Get-FileHash -LiteralPath $zip -Algorithm SHA256).Hash.ToUpperInvariant()
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

$outputFull = [IO.Path]::GetFullPath($OutputPath)
$outputParent = Split-Path -Parent $outputFull
if ([string]::IsNullOrWhiteSpace($outputParent)) { throw 'OutputPath must have a parent directory.' }
if ($PSCmdlet.ShouldProcess($outputFull, 'Write QS3D update manifest')) {
    New-Item -ItemType Directory -Path $outputParent -Force | Out-Null
    $manifest | ConvertTo-Json | Set-Content -LiteralPath $outputFull -Encoding UTF8
}

Write-Host "Update manifest: $outputFull"
Write-Host "Product version: $signedPluginProductVersion"
Write-Host "Assembly version: $($signedPluginVersion.ToString())"
Write-Host "Package SHA256: $zipHash"
Write-Host "Signer: $expectedSigner"