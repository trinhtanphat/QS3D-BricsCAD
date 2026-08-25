[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][ValidatePattern('^[0-9a-fA-F]{40}$')][string]$ExpectedGitSha,
    [Parameter(Mandatory = $true)][ValidatePattern('^[0-9a-fA-F]{40}$')][string]$ExpectedSignerThumbprint,
    [Parameter(Mandatory = $true)][ValidatePattern('^https://')][string]$TimestampServer,
    [Parameter(Mandatory = $true)][ValidatePattern('^https://')][string]$PackageUri,
    [string]$ArtifactDirectory = (Join-Path ([IO.Path]::GetTempPath()) 'qs3d-v26-signed-finalization-evidence')
)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent $PSScriptRoot
function Invoke-Git([string[]]$Arguments) {
    $value = (& git -C $repo @Arguments 2>$null | Out-String).Trim()
    if ($LASTEXITCODE -ne 0) { throw 'Git prerequisite failed.' }
    return $value
}
$actualSha = Invoke-Git @('rev-parse','HEAD')
if ($actualSha -ine $ExpectedGitSha) { throw 'Checked-out Git SHA does not match ExpectedGitSha.' }
if ((Invoke-Git @('status','--porcelain')).Length -ne 0) { throw 'Qualification requires a clean checkout.' }
if (-not (Get-Command Get-AuthenticodeSignature -ErrorAction SilentlyContinue)) { throw 'Authenticode verification is unavailable.' }
if (-not (Get-Command Get-PfxCertificate -ErrorAction SilentlyContinue)) { throw 'Windows certificate tooling is unavailable.' }
$cert = Get-ChildItem Cert:\CurrentUser\My,Cert:\LocalMachine\My -ErrorAction SilentlyContinue | Where-Object { $_.Thumbprint -ieq $ExpectedSignerThumbprint } | Select-Object -First 1
if ($null -eq $cert -or -not $cert.HasPrivateKey) { throw 'Expected signing certificate with private key is unavailable.' }
if ($cert.NotAfter -le [DateTime]::UtcNow) { throw 'Expected signing certificate is expired.' }
$packageDir = Join-Path $repo 'dist\QS3D-BricsCAD-V26'
$packageZip = Join-Path $repo 'dist\QS3D-BricsCAD-V26.zip'
$manifest = Join-Path $repo 'dist\QS3D-BricsCAD-V26.update.json'
$payload = @(
    (Join-Path $packageDir 'QS3D.BricsCAD.V26.dll'),
    (Join-Path $packageDir 'QS3D.Core.dll'),
    (Join-Path $packageDir 'install-v26-autoload.ps1'),
    (Join-Path $packageDir 'uninstall-v26-autoload.ps1'),
    (Join-Path $packageDir 'update-v26.ps1')
)
& (Join-Path $PSScriptRoot 'package-v26.ps1')
if (-not $?) { throw 'V26 package preparation failed.' }
foreach ($path in $payload) { if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { throw 'Expected package payload is missing.' } }
& (Join-Path $PSScriptRoot 'sign-v26.ps1') -Path $payload -CertificateThumbprint $ExpectedSignerThumbprint -TimestampServer $TimestampServer -Confirm:$false
if (-not $?) { throw 'V26 signing failed.' }
& (Join-Path $PSScriptRoot 'verify-v26-signatures.ps1') -Path $payload -ExpectedThumbprint $ExpectedSignerThumbprint
if (-not $?) { throw 'V26 signature verification failed.' }
& (Join-Path $PSScriptRoot 'finalize-v26-signed-package.ps1') -PackageDirectory $packageDir -PackageZip $packageZip -ExpectedSignerThumbprint $ExpectedSignerThumbprint -Confirm:$false
if (-not $?) { throw 'V26 signed-package finalization failed.' }
& (Join-Path $PSScriptRoot 'new-v26-update-manifest.ps1') -PackageDirectory $packageDir -PackageZip $packageZip -PackageUri $PackageUri -ExpectedSignerThumbprint $ExpectedSignerThumbprint -OutputPath $manifest -Confirm:$false
if (-not $?) { throw 'V26 update-manifest generation failed.' }
foreach ($path in $payload) {
    $sig = Get-AuthenticodeSignature -LiteralPath $path
    if ($sig.Status -ne 'Valid' -or $null -eq $sig.SignerCertificate -or $sig.SignerCertificate.Thumbprint -ine $ExpectedSignerThumbprint) { throw 'Finalized payload signature identity mismatch.' }
}
if (-not (Test-Path -LiteralPath $packageZip -PathType Leaf) -or -not (Test-Path -LiteralPath $manifest -PathType Leaf)) { throw 'Finalized ZIP or update manifest is missing.' }
$manifestJson = Get-Content -LiteralPath $manifest -Raw | ConvertFrom-Json
$zipHash = (Get-FileHash -LiteralPath $packageZip -Algorithm SHA256).Hash.ToUpperInvariant()
$manifestText = Get-Content -LiteralPath $manifest -Raw
if ($manifestText -notmatch [regex]::Escape($zipHash)) { throw 'Update manifest does not bind the finalized ZIP SHA-256.' }
if ($manifestText -notmatch [regex]::Escape($PackageUri)) { throw 'Update manifest does not bind the requested HTTPS package URI.' }
if ($manifestText -notmatch [regex]::Escape($ExpectedSignerThumbprint.ToUpperInvariant())) { throw 'Update manifest does not bind the expected signer thumbprint.' }
New-Item -ItemType Directory -Path $ArtifactDirectory -Force | Out-Null
$evidence = [ordered]@{
    schema = 1
    result = 'LOCAL_PASS'
    gitSha = $actualSha.ToLowerInvariant()
    signerThumbprintSha256 = ([Convert]::ToHexString([Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes($ExpectedSignerThumbprint.ToUpperInvariant())))).ToLowerInvariant()
    packageSha256 = $zipHash.ToLowerInvariant()
    payloadSignatureCount = $payload.Count
    packageUriHttps = $PackageUri.StartsWith('https://',[StringComparison]::OrdinalIgnoreCase)
    finalized = $true
    manifestBound = $true
}
$evidencePath = Join-Path $ArtifactDirectory 'v26-signed-finalization.json'
$evidence | ConvertTo-Json | Set-Content -LiteralPath $evidencePath -Encoding UTF8
Write-Host 'QS3D_V26_SIGNED_FINALIZATION_LOCAL_PASS'
Write-Host ('evidence=' + $evidencePath)
