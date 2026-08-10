[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string[]] $Path,

    [ValidatePattern('^[0-9A-Fa-f]{40}$')]
    [string] $ExpectedThumbprint = ''
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$normalizedExpected = $ExpectedThumbprint.Replace(' ', '').ToUpperInvariant()
$failures = New-Object System.Collections.Generic.List[string]

foreach ($inputPath in $Path) {
    $resolved = Resolve-Path -LiteralPath $inputPath -ErrorAction Stop
    $item = Get-Item -LiteralPath $resolved.Path -ErrorAction Stop
    if ($item.PSIsContainer) {
        $failures.Add("Path is a directory: $($item.FullName)")
        continue
    }

    $signature = Get-AuthenticodeSignature -FilePath $item.FullName
    if ($signature.Status -ne [System.Management.Automation.SignatureStatus]::Valid) {
        $failures.Add("Invalid signature: $($item.FullName) [$($signature.Status)] $($signature.StatusMessage)")
        continue
    }
    if (-not $signature.SignerCertificate) {
        $failures.Add("Missing signer certificate: $($item.FullName)")
        continue
    }

    $thumbprint = $signature.SignerCertificate.Thumbprint.Replace(' ', '').ToUpperInvariant()
    if ($normalizedExpected.Length -gt 0 -and $thumbprint -ne $normalizedExpected) {
        $failures.Add("Unexpected signer: $($item.FullName) [$thumbprint]")
        continue
    }

    if ($signature.TimeStamperCertificate) {
        Write-Host ("VALID {0} signer={1} timestamp={2}" -f $item.FullName, $thumbprint, $signature.TimeStamperCertificate.Subject)
    }
    else {
        $failures.Add("Missing trusted timestamp: $($item.FullName)")
    }
}

if ($failures.Count -gt 0) {
    foreach ($failure in $failures) { Write-Error $failure }
    throw "Authenticode verification failed for $($failures.Count) file(s)."
}

Write-Host ("PASS: verified {0} Authenticode-signed file(s)." -f $Path.Count)
