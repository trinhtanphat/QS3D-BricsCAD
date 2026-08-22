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

function Normalize-Thumbprint {
    param([Parameter(Mandatory = $true)][string] $Thumbprint)
    return $Thumbprint.Replace(' ', '').ToUpperInvariant()
}

function Get-CodeSigningCertificate {
    param([string] $Thumbprint)

    $normalized = Normalize-Thumbprint $Thumbprint
    $matches = @(Get-ChildItem -Path Cert:\CurrentUser\My | Where-Object {
        $_.Thumbprint -and (Normalize-Thumbprint $_.Thumbprint) -eq $normalized
    })
    if ($matches.Count -ne 1) {
        throw "Expected exactly one code-signing certificate in Cert:\CurrentUser\My for $normalized; found $($matches.Count)."
    }

    $certificate = $matches[0]
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
    if (-not @($enhancedEku.EnhancedKeyUsages | Where-Object { $_.Value -eq $codeSigningOid })) {
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

function Get-SignTool {
    $command = Get-Command signtool.exe -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($command) { return $command.Source }

    $programFilesX86 = ${env:ProgramFiles(x86)}
    if ([string]::IsNullOrWhiteSpace($programFilesX86)) {
        throw 'signtool.exe is not on PATH and ProgramFiles(x86) is unavailable.'
    }
    $kitsBin = Join-Path $programFilesX86 'Windows Kits\10\bin'
    if (-not (Test-Path -LiteralPath $kitsBin -PathType Container)) {
        throw "signtool.exe is not on PATH and Windows Kits bin was not found: $kitsBin"
    }
    $candidates = @(Get-ChildItem -LiteralPath $kitsBin -Recurse -Filter signtool.exe -File -ErrorAction SilentlyContinue |
        Where-Object { $_.FullName -match '\\x64\\signtool\.exe$' } |
        Sort-Object FullName -Descending)
    if ($candidates.Count -eq 0) {
        throw 'No x64 signtool.exe was found under Windows Kits.'
    }
    return $candidates[0].FullName
}

function Assert-PostSignSignature {
    param(
        [Parameter(Mandatory = $true)][string] $FilePath,
        [Parameter(Mandatory = $true)][string] $ExpectedThumbprint
    )

    $signature = Get-AuthenticodeSignature -FilePath $FilePath
    if ($signature.Status -ne [System.Management.Automation.SignatureStatus]::Valid) {
        throw "Post-sign verification failed for ${FilePath}: $($signature.Status) $($signature.StatusMessage)"
    }
    if (-not $signature.SignerCertificate) {
        throw "Post-sign verification returned no signer certificate for $FilePath."
    }
    $actual = Normalize-Thumbprint $signature.SignerCertificate.Thumbprint
    if ($actual -ne $ExpectedThumbprint) {
        throw "Post-sign certificate mismatch for $FilePath. Expected $ExpectedThumbprint, got $actual."
    }
    if (-not $signature.TimeStamperCertificate) {
        throw "Post-sign verification found no trusted timestamp for $FilePath."
    }
}

$certificate = Get-CodeSigningCertificate -Thumbprint $CertificateThumbprint
$expectedThumbprint = Normalize-Thumbprint $certificate.Thumbprint
$timestampUri = [Uri]$TimestampServer
if ($timestampUri.Scheme -ne 'https') {
    throw 'TimestampServer must use HTTPS.'
}

$files = @($Path | ForEach-Object { Resolve-SignableFile -InputPath $_ })
if ($files.Count -eq 0) {
    throw 'No files were supplied for signing.'
}
$signTool = $null

foreach ($file in $files) {
    if (-not $PSCmdlet.ShouldProcess($file.FullName, "Authenticode sign with $expectedThumbprint")) {
        continue
    }

    $extension = [System.IO.Path]::GetExtension($file.FullName).ToLowerInvariant()
    if ($extension -in @('.dll', '.exe')) {
        if (-not $signTool) { $signTool = Get-SignTool }
        & $signTool sign /sha1 $expectedThumbprint /s My /fd SHA256 /tr $TimestampServer /td SHA256 /v $file.FullName
        if ($LASTEXITCODE -ne 0) {
            throw "signtool RFC3161 signing failed for $($file.FullName) with exit code $LASTEXITCODE."
        }
    }
    else {
        $signature = Set-AuthenticodeSignature `
            -FilePath $file.FullName `
            -Certificate $certificate `
            -HashAlgorithm SHA256 `
            -TimestampServer $TimestampServer
        if ($signature.Status -ne [System.Management.Automation.SignatureStatus]::Valid) {
            throw "Authenticode script signing failed for $($file.FullName): $($signature.Status) $($signature.StatusMessage)"
        }
    }

    Assert-PostSignSignature -FilePath $file.FullName -ExpectedThumbprint $expectedThumbprint
    if ($extension -in @('.dll', '.exe')) {
        & $signTool verify /pa /all /v $file.FullName
        if ($LASTEXITCODE -ne 0) {
            throw "signtool trust verification failed for $($file.FullName) with exit code $LASTEXITCODE."
        }
    }

    Write-Host ("SIGNED {0} [{1}]" -f $file.FullName, $expectedThumbprint)
}
