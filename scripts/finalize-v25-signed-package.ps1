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

$package = (Resolve-Path -LiteralPath $PackageDirectory).Path
$zip = [IO.Path]::GetFullPath($PackageZip)
$expectedSigner = Normalize-Thumbprint $ExpectedSignerThumbprint
$metadataPath = Join-Path $package 'PACKAGE-METADATA.json'
if (-not (Test-Path -LiteralPath $metadataPath -PathType Leaf)) { throw "Missing signed-package artifact: $metadataPath" }
foreach ($name in $SignedPayloadNames) {
    $path = Join-Path $package $name
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { throw "Missing signed-package artifact: $path" }
    Assert-AuthenticodeSigner -Path $path -ExpectedSigner $expectedSigner -Label ("QS3D executable payload " + $name)
}

$metadata = Get-Content -LiteralPath $metadataPath -Raw | ConvertFrom-Json
if (-not $metadata.PSObject.Properties['version']) { throw 'PACKAGE-METADATA is missing version.' }
try { $metadataVersion = [Version]::Parse([string]$metadata.version) }
catch { throw "PACKAGE-METADATA version is invalid: $($metadata.version)" }
$signedPluginVersion = Read-PluginAssemblyVersion -Path (Join-Path $package 'QS3D.BricsCAD.V25.dll')
if ($metadataVersion -ne $signedPluginVersion) {
    throw "PACKAGE-METADATA version $metadataVersion does not match signed QS3D plugin assembly version $signedPluginVersion."
}

if (-not $PSCmdlet.ShouldProcess($zip, 'Finalize signed QS3D V25 package and rebuild ZIP')) { return }

$metadata | Add-Member -NotePropertyName pluginSignatureStatus -NotePropertyValue 'Valid' -Force
$metadata | Add-Member -NotePropertyName pluginSignerThumbprint -NotePropertyValue $expectedSigner -Force
$metadata | Add-Member -NotePropertyName signedExecutablePayload -NotePropertyValue @($SignedPayloadNames) -Force
$metadata | Add-Member -NotePropertyName signedPayloadSignerThumbprint -NotePropertyValue $expectedSigner -Force
$metadata | Add-Member -NotePropertyName signedPluginAssemblyVersion -NotePropertyValue $signedPluginVersion.ToString() -Force
$metadata | Add-Member -NotePropertyName signedPackageFinalizedUtc -NotePropertyValue ([DateTime]::UtcNow.ToString('o')) -Force
$metadata | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $metadataPath -Encoding UTF8

$hashManifest = Join-Path $package 'SHA256SUMS.txt'
if (Test-Path -LiteralPath $hashManifest) { Remove-Item -LiteralPath $hashManifest -Force }
$hashLines = foreach ($file in Get-ChildItem -LiteralPath $package -Recurse -File | Sort-Object FullName) {
    if ($file.FullName -eq $hashManifest) { continue }
    $relative = $file.FullName.Substring($package.Length).TrimStart('\', '/').Replace([IO.Path]::DirectorySeparatorChar, '/')
    if ([string]::IsNullOrWhiteSpace($relative) -or [IO.Path]::IsPathRooted($relative) -or $relative.Contains(':') -or $relative.Contains('\')) {
        throw "Unsafe package-relative path while hashing: $relative"
    }
    $segments = @($relative.Split('/'))
    if (@($segments | Where-Object { [string]::IsNullOrWhiteSpace($_) -or $_ -eq '.' -or $_ -eq '..' }).Count -gt 0) {
        throw "Unsafe package-relative path while hashing: $relative"
    }
    $hash = (Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash.ToUpperInvariant()
    "$hash  $relative"
}
if (@($hashLines).Count -eq 0) { throw 'Signed package contains no payload files to hash.' }
$hashLines | Set-Content -LiteralPath $hashManifest -Encoding ASCII

$zipParent = Split-Path -Parent $zip
if (-not [string]::IsNullOrWhiteSpace($zipParent)) { New-Item -ItemType Directory -Path $zipParent -Force | Out-Null }
if (Test-Path -LiteralPath $zip) { Remove-Item -LiteralPath $zip -Force }
Compress-Archive -Path (Join-Path $package '*') -DestinationPath $zip -CompressionLevel Optimal

Write-Host "FINALIZED: $zip"
Write-Host "Signer: $expectedSigner"
Write-Host "Signed plugin version: $($signedPluginVersion.ToString())"
Write-Host "Signed executable payloads: $($SignedPayloadNames.Count)"
Write-Host "ZIP SHA256: $((Get-FileHash -LiteralPath $zip -Algorithm SHA256).Hash.ToUpperInvariant())"
