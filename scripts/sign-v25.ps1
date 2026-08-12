[CmdletBinding(SupportsShouldProcess = $true, ConfirmImpact = 'High')]
param(
    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string[]] $Path,

    [Parameter(Mandatory = $true)]
    [ValidatePattern('^[0-9A-Fa-f]{40}$')]
    [string] $CertificateThumbprint,

    [Parameter(Mandatory = $true)]
    [ValidatePattern('^https://')]
    [string] $TimestampServer
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Get-CodeSigningCertificate {
    param([string] $Thumbprint)

    $normalized = $Thumbprint.Replace(' ', '').ToUpperInvariant()
    $certificate = Get-ChildItem -Path Cert:\CurrentUser\My | Where-Object {
        $_.Thumbprint -and $_.Thumbprint.Replace(' ', '').ToUpperInvariant() -eq $normalized
    } | Select-Object -First 1

    if (-not $certificate) {
        throw "Code-signing certificate not found in Cert:\CurrentUser\My: $normalized"
    }
    if (-not $certificate.HasPrivateKey) {
        throw "Certificate $normalized does not have an accessible private key."
    }
    $now = Get-Date
    if ($certificate.NotBefore -gt $now -or $certificate.NotAfter -le $now) {
        throw "Certificate $normalized is outside its validity period."
    }

    $codeSigningOid = '1.3.6.1.5.5.7.3.3'
    $eku = $certificate.Extensions | Where-Object { $_.Oid.Value -eq '2.5.29.37' } | Select-Object -First 1
    if (-not $eku) {
        throw "Certificate $normalized does not expose an Enhanced Key Usage extension."
    }
    $enhancedEku = New-Object Security.Cryptography.X509Certificates.X509EnhancedKeyUsageExtension
    $enhancedEku.CopyFrom($eku)
    $hasCodeSigningEku = $false
    foreach ($usage in $enhancedEku.EnhancedKeyUsages) {
        if ([string]::Equals([string]$usage.Value, $codeSigningOid, [StringComparison]::Ordinal)) {
            $hasCodeSigningEku = $true
            break
        }
    }
    if (-not $hasCodeSigningEku) {
        throw "Certificate $normalized is not valid for Code Signing ($codeSigningOid)."
    }
    return $certificate
}

function Resolve-SignableFile {
    param([string] $InputPath)

    $resolved = Resolve-Path -LiteralPath $InputPath -ErrorAction Stop
    $item = Get-Item -LiteralPath $resolved.Path -ErrorAction Stop
    if ($item.PSIsContainer) {
        throw "Signing path must be a file, not a directory: $($item.FullName)"
    }
    $extension = [System.IO.Path]::GetExtension($item.FullName).ToLowerInvariant()
    if ($extension -notin @('.dll', '.exe', '.ps1', '.psm1')) {
        throw "Unsupported Authenticode file type '$extension': $($item.FullName)"
    }
    return $item
}

$certificate = Get-CodeSigningCertificate -Thumbprint $CertificateThumbprint
$files = @($Path | ForEach-Object { Resolve-SignableFile -InputPath $_ })
if ($files.Count -eq 0) {
    throw 'No files were supplied for signing.'
}

foreach ($file in $files) {
    if (-not $PSCmdlet.ShouldProcess($file.FullName, "Authenticode sign with $($certificate.Thumbprint)")) {
        continue
    }

    $signature = Set-AuthenticodeSignature `
        -FilePath $file.FullName `
        -Certificate $certificate `
        -HashAlgorithm SHA256 `
        -TimestampServer $TimestampServer

    if ($signature.Status -ne [System.Management.Automation.SignatureStatus]::Valid) {
        throw "Authenticode signing failed for $($file.FullName): $($signature.Status) $($signature.StatusMessage)"
    }

    $verified = Get-AuthenticodeSignature -FilePath $file.FullName
    if ($verified.Status -ne [System.Management.Automation.SignatureStatus]::Valid) {
        throw "Post-sign verification failed for $($file.FullName): $($verified.Status) $($verified.StatusMessage)"
    }
    if (-not $verified.SignerCertificate -or $verified.SignerCertificate.Thumbprint -ne $certificate.Thumbprint) {
        throw "Post-sign certificate mismatch for $($file.FullName)."
    }

    Write-Host ("SIGNED {0} [{1}]" -f $file.FullName, $certificate.Thumbprint)
}
