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

function Normalize-Thumbprint {
    param([string]$Thumbprint)
    return $Thumbprint.Replace(' ', '').ToUpperInvariant()
}

$uri = $null
if (-not [Uri]::TryCreate($PackageUri, [UriKind]::Absolute, [ref]$uri) -or $uri.Scheme -ne [Uri]::UriSchemeHttps -or [string]::IsNullOrWhiteSpace($uri.Host)) {
    throw 'PackageUri must be an absolute HTTPS URI.'
}
if ($uri.UserInfo) { throw 'PackageUri must not contain embedded credentials.' }

$package = (Resolve-Path -LiteralPath $PackageDirectory).Path
$zip = (Resolve-Path -LiteralPath $PackageZip).Path
$metadataPath = Join-Path $package 'PACKAGE-METADATA.json'
$pluginPath = Join-Path $package 'QS3D.BricsCAD.V25.dll'
foreach ($path in @($metadataPath, $pluginPath)) {
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { throw "Missing package artifact: $path" }
}

$metadata = Get-Content -LiteralPath $metadataPath -Raw | ConvertFrom-Json
if ([string]$metadata.product -ne 'QS3D') { throw 'PACKAGE-METADATA product must be QS3D.' }
if ([string]$metadata.target -ne 'BricsCAD V25 x64') { throw 'PACKAGE-METADATA target must be BricsCAD V25 x64.' }
if (-not $metadata.PSObject.Properties['version']) { throw 'PACKAGE-METADATA is missing version.' }
try { $version = [Version]::Parse([string]$metadata.version) }
catch { throw "PACKAGE-METADATA version is invalid: $($metadata.version)" }

$signature = Get-AuthenticodeSignature -FilePath $pluginPath
if ($signature.Status -ne [System.Management.Automation.SignatureStatus]::Valid) {
    throw "QS3D plugin signature is not valid: $($signature.Status)"
}
if (-not $signature.SignerCertificate) { throw 'QS3D plugin signature has no signer certificate.' }
$expectedSigner = Normalize-Thumbprint $ExpectedSignerThumbprint
$actualSigner = Normalize-Thumbprint $signature.SignerCertificate.Thumbprint
if ($actualSigner -ne $expectedSigner) { throw "QS3D signer mismatch. Expected $expectedSigner, got $actualSigner." }

$zipHash = (Get-FileHash -LiteralPath $zip -Algorithm SHA256).Hash.ToUpperInvariant()
$manifest = [ordered]@{
    schemaVersion = 1
    product = 'QS3D'
    target = 'BricsCAD V25 x64'
    version = $version.ToString()
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
Write-Host "Version: $($version.ToString())"
Write-Host "Package SHA256: $zipHash"
Write-Host "Signer: $expectedSigner"
