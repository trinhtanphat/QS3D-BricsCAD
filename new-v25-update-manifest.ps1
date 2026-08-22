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

function Assert-ZipPayloadMatchesSignedStaging {
    param([string]$ZipPath, [string]$PackageRoot, [string]$ExpectedSigner)
    $temp = Join-Path ([IO.Path]::GetTempPath()) ('qs3d-manifest-verify-' + [Guid]::NewGuid().ToString('N'))
    New-Item -ItemType Directory -Path $temp -Force | Out-Null
    $archive = $null
    try {
        $archive = [System.IO.Compression.ZipFile]::OpenRead($ZipPath)
        foreach ($name in @('PACKAGE-METADATA.json') + $SignedPayloadNames) {
            $matches = @($archive.Entries | Where-Object { [string]::Equals($_.FullName, $name, [StringComparison]::Ordinal) })
            if ($matches.Count -ne 1) { throw "Package ZIP must contain exactly one root entry named $name." }
            $destination = Join-Path $temp $name
            $input = $matches[0].Open()
            $output = [IO.File]::Create($destination)
            try { $input.CopyTo($output) }
            finally { $output.Dispose(); $input.Dispose() }

            $staged = Join-Path $PackageRoot $name
            $stagedHash = (Get-FileHash -LiteralPath $staged -Algorithm SHA256).Hash.ToUpperInvariant()
            $zippedHash = (Get-FileHash -LiteralPath $destination -Algorithm SHA256).Hash.ToUpperInvariant()
            if ($stagedHash -ne $zippedHash) { throw "Package ZIP payload does not match signed staging file: $name" }
        }
        foreach ($name in $SignedPayloadNames) {
            Assert-AuthenticodeSigner -Path (Join-Path $temp $name) -ExpectedSigner $ExpectedSigner -Label ("Zipped QS3D executable payload " + $name)
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
try { $version = [Version]::Parse([string]$metadata.version) }
catch { throw "PACKAGE-METADATA version is invalid: $($metadata.version)" }

$expectedSigner = Normalize-Thumbprint $ExpectedSignerThumbprint
foreach ($name in $SignedPayloadNames) {
    Assert-AuthenticodeSigner -Path (Join-Path $package $name) -ExpectedSigner $expectedSigner -Label ("QS3D executable payload " + $name)
}
$signedPluginVersion = Read-PluginAssemblyVersion -Path (Join-Path $package 'QS3D.BricsCAD.V25.dll')
if ($version -ne $signedPluginVersion) {
    throw "PACKAGE-METADATA version $version does not match signed QS3D plugin assembly version $signedPluginVersion."
}
Assert-ZipPayloadMatchesSignedStaging -ZipPath $zip -PackageRoot $package -ExpectedSigner $expectedSigner

$zipHash = (Get-FileHash -LiteralPath $zip -Algorithm SHA256).Hash.ToUpperInvariant()
$manifest = [ordered]@{
    schemaVersion = 1
    product = 'QS3D'
    target = 'BricsCAD V25 x64'
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
Write-Host "Version: $($signedPluginVersion.ToString())"
Write-Host "Package SHA256: $zipHash"
Write-Host "Signer: $expectedSigner"
