[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string] $PfxBase64,

    [Parameter(Mandatory = $true)]
    [ValidateNotNull()]
    [Security.SecureString] $Password,

    [Parameter(Mandatory = $true)]
    [ValidatePattern('^[0-9A-Fa-f]{40}$')]
    [string] $ExpectedThumbprint
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$maxPfxDecodedBytes = 1048576
$maxPfxBase64Chars = 1398104

function Normalize-Thumbprint {
    param([Parameter(Mandatory = $true)][string] $Thumbprint)
    return $Thumbprint.Replace(' ', '').ToUpperInvariant()
}

function Test-CodeSigningEku {
    param([Parameter(Mandatory = $true)][Security.Cryptography.X509Certificates.X509Certificate2] $Certificate)

    $codeSigningOid = '1.3.6.1.5.5.7.3.3'
    $eku = $Certificate.Extensions | Where-Object { $_.Oid.Value -eq '2.5.29.37' } | Select-Object -First 1
    if (-not $eku) { return $false }
    $enhancedEku = New-Object Security.Cryptography.X509Certificates.X509EnhancedKeyUsageExtension
    $enhancedEku.CopyFrom($eku)
    return [bool]@($enhancedEku.EnhancedKeyUsages | Where-Object { $_.Value -eq $codeSigningOid })
}

function Remove-ImportedCertificates {
    param([string[]] $Thumbprints)

    foreach ($thumbprint in @($Thumbprints | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | Sort-Object -Unique)) {
        $normalized = Normalize-Thumbprint $thumbprint
        if ($normalized -notmatch '^[0-9A-F]{40}$') { continue }
        $path = "Cert:\CurrentUser\My\$normalized"
        if (Test-Path -LiteralPath $path) {
            Remove-Item -Path $path -DeleteKey -Force -ErrorAction Stop
        }
    }
}

$expected = Normalize-Thumbprint $ExpectedThumbprint
$existing = @(Get-ChildItem -Path Cert:\CurrentUser\My | ForEach-Object {
    if ($_.Thumbprint) { Normalize-Thumbprint $_.Thumbprint }
})
if ($existing -contains $expected) {
    throw "Expected commercial signing certificate $expected already exists in Cert:\CurrentUser\My. Refusing a non-ephemeral signing key."
}

$encodedPfx = $PfxBase64.Trim()
if ($encodedPfx.Length -gt $maxPfxBase64Chars) {
    throw "QS3D signing PFX base64 input exceeds the allowed encoded size: $($encodedPfx.Length) characters."
}
try {
    $bytes = [Convert]::FromBase64String($encodedPfx)
}
catch {
    throw 'QS3D signing PFX secret is not valid base64.'
}
if ($bytes.Length -lt 256 -or $bytes.Length -gt $maxPfxDecodedBytes) {
    throw "QS3D signing PFX decoded size is outside the allowed range: $($bytes.Length) bytes."
}

$importedNewThumbprints = @()
$operationError = $null
$certificate = $null
$store = $null

try {
    # First validate the bounded bytes without PersistKeySet so a malformed or
    # unexpected PFX is rejected before a durable private key is created.
    $probeCertificate = New-Object Security.Cryptography.X509Certificates.X509Certificate2
    try {
        $probeCertificate.Import(
            $bytes,
            $Password,
            [Security.Cryptography.X509Certificates.X509KeyStorageFlags]::UserKeySet)
        $probeThumbprint = if ($probeCertificate.Thumbprint) { Normalize-Thumbprint $probeCertificate.Thumbprint } else { [string]::Empty }
        if (-not [string]::Equals($probeThumbprint, $expected, [StringComparison]::Ordinal)) {
            throw "PFX certificate thumbprint does not match expected commercial signing certificate $expected."
        }
        if (-not $probeCertificate.HasPrivateKey -or -not (Test-CodeSigningEku $probeCertificate)) {
            throw "PFX must contain the expected private-key Code Signing certificate $expected."
        }
        $probeNow = Get-Date
        if ($probeCertificate.NotBefore -gt $probeNow -or $probeCertificate.NotAfter -le $probeNow) {
            throw "PFX code-signing certificate $expected is outside its validity period."
        }
    }
    finally {
        $probeCertificate.Dispose()
    }

    $certificate = [Security.Cryptography.X509Certificates.X509Certificate2]::new()
    $keyStorageFlags = [Security.Cryptography.X509Certificates.X509KeyStorageFlags]::UserKeySet -bor
        [Security.Cryptography.X509Certificates.X509KeyStorageFlags]::PersistKeySet
    $certificate.Import($bytes, $Password, $keyStorageFlags)

    $store = [Security.Cryptography.X509Certificates.X509Store]::new(
        [Security.Cryptography.X509Certificates.StoreName]::My,
        [Security.Cryptography.X509Certificates.StoreLocation]::CurrentUser)
    $store.Open([Security.Cryptography.X509Certificates.OpenFlags]::ReadWrite)

    # The expected thumbprint was proven absent before import and the exact same
    # bytes were validated in the non-persistent pass above. Arm cleanup before
    # Store.Add so any partial add is still bounded to this attempt.
    $importedNewThumbprints = @($expected)
    $store.Add($certificate)

    $candidates = @($store.Certificates | Where-Object {
        $_.Thumbprint -and
        (Normalize-Thumbprint $_.Thumbprint) -eq $expected -and
        $_.HasPrivateKey -and
        (Test-CodeSigningEku $_)
    })
    if ($candidates.Count -ne 1) {
        throw "PFX must import exactly one expected private-key Code Signing certificate $expected; found $($candidates.Count)."
    }

    $candidate = $candidates[0]
    $now = Get-Date
    if ($candidate.NotBefore -gt $now -or $candidate.NotAfter -le $now) {
        throw "Imported code-signing certificate $expected is outside its validity period."
    }
    if ($importedNewThumbprints -notcontains $expected) {
        throw "Expected code-signing certificate $expected was not newly imported from this PFX."
    }

    Write-Output ("SIGNING_THUMBPRINT=" + $expected)
    Write-Output ("IMPORTED_THUMBPRINTS=" + ($importedNewThumbprints -join ','))
}
catch {
    $operationError = $_.Exception
    $certificateCleanupError = $null
    if ($importedNewThumbprints.Count -gt 0) {
        try {
            Remove-ImportedCertificates -Thumbprints $importedNewThumbprints
        }
        catch {
            $certificateCleanupError = $_.Exception
        }
    }
    if ($null -ne $certificateCleanupError) {
        $operationError = [AggregateException]::new(
            'Signing operation failed and imported certificate cleanup also failed.',
            [Exception[]]@($operationError, $certificateCleanupError))
        throw $operationError
    }
    throw
}
finally {
    if ($null -ne $store) { $store.Close() }
    if ($null -ne $certificate) { $certificate.Dispose() }
    [Array]::Clear($bytes, 0, $bytes.Length)
}
