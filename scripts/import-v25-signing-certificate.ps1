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

function Get-CanonicalFullPath {
    param([Parameter(Mandatory = $true)][string] $Path, [Parameter(Mandatory = $true)][string] $Label)

    if ([string]::IsNullOrWhiteSpace($Path)) { throw "$Label path is required." }
    try { return [IO.Path]::GetFullPath($Path) }
    catch { throw "$Label path is invalid: $($_.Exception.Message)" }
}

function Assert-SafeTempDirectory {
    param([Parameter(Mandatory = $true)][string] $Path)

    $fullPath = (Get-CanonicalFullPath -Path $Path -Label 'temporary directory').TrimEnd([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar)
    $pathRoot = [IO.Path]::GetPathRoot($fullPath)
    if ([string]::IsNullOrWhiteSpace($fullPath) -or
        [string]::Equals($fullPath, $pathRoot.TrimEnd([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar), [StringComparison]::OrdinalIgnoreCase)) {
        throw "Temporary directory must not be a filesystem root: $fullPath"
    }
    if (-not (Test-Path -LiteralPath $fullPath -PathType Container)) {
        throw "Temporary directory was not found: $fullPath"
    }
    $item = Get-Item -LiteralPath $fullPath -Force -ErrorAction Stop
    if (-not $item.PSIsContainer -or ($item.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
        throw "Temporary directory must be an ordinary non-reparse directory: $fullPath"
    }
    return $fullPath
}

function Assert-SafeTempFile {
    param(
        [Parameter(Mandatory = $true)][string] $Path,
        [Parameter(Mandatory = $true)][string] $TempRoot
    )

    $fullPath = Get-CanonicalFullPath -Path $Path -Label 'temporary PFX'
    $parent = [IO.Path]::GetDirectoryName($fullPath)
    if (-not [string]::Equals($parent, $TempRoot, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Temporary PFX escaped the validated temporary directory: $fullPath"
    }
    if (-not (Test-Path -LiteralPath $fullPath -PathType Leaf)) {
        throw "Temporary PFX file was not found after write: $fullPath"
    }
    $item = Get-Item -LiteralPath $fullPath -Force -ErrorAction Stop
    if ($item.PSIsContainer -or ($item.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0 -or -not ($item -is [IO.FileInfo])) {
        throw "Temporary PFX must be an ordinary non-reparse file: $fullPath"
    }
    return $fullPath
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

$tempRootCandidate = if ([string]::IsNullOrWhiteSpace($env:RUNNER_TEMP)) { [IO.Path]::GetTempPath() } else { $env:RUNNER_TEMP }
$tempRoot = Assert-SafeTempDirectory -Path $tempRootCandidate
$pfxPath = Join-Path $tempRoot ('qs3d-signing-' + [Guid]::NewGuid().ToString('N') + '.pfx')
$importedNewThumbprints = @()

try {
    [IO.File]::WriteAllBytes($pfxPath, $bytes)
    $pfxPath = Assert-SafeTempFile -Path $pfxPath -TempRoot $tempRoot
    $imported = @(Import-PfxCertificate `
        -FilePath $pfxPath `
        -CertStoreLocation Cert:\CurrentUser\My `
        -Password $Password `
        -Exportable:$false `
        -ErrorAction Stop)

    $importedThumbprints = @($imported | ForEach-Object {
        if ($_.Thumbprint) { Normalize-Thumbprint $_.Thumbprint }
    } | Where-Object { $_ } | Sort-Object -Unique)
    $importedNewThumbprints = @($importedThumbprints | Where-Object { $existing -notcontains $_ })
    if ($importedNewThumbprints.Count -eq 0) {
        throw 'PFX import did not add any new certificate to Cert:\CurrentUser\My.'
    }

    $candidates = @(Get-ChildItem -Path Cert:\CurrentUser\My | Where-Object {
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
    if ($importedNewThumbprints.Count -gt 0) {
        Remove-ImportedCertificates -Thumbprints $importedNewThumbprints
    }
    throw
}
finally {
    if (Test-Path -LiteralPath $pfxPath) {
        Remove-Item -LiteralPath $pfxPath -Force -ErrorAction SilentlyContinue
    }
    [Array]::Clear($bytes, 0, $bytes.Length)
}
