[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string[]] $Path,

    [Parameter(Mandatory = $true)]
    [ValidatePattern('^[0-9A-Fa-f]{40}$')]
    [string] $ExpectedThumbprint
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Normalize-Thumbprint {
    param([Parameter(Mandatory = $true)][string] $Thumbprint)
    return $Thumbprint.Replace(' ', '').ToUpperInvariant()
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

$normalizedExpected = Normalize-Thumbprint $ExpectedThumbprint
$files = @($Path)
if ($files.Count -eq 0) {
    throw 'No files were supplied for Authenticode verification.'
}

$failures = New-Object System.Collections.Generic.List[string]
$signTool = $null

foreach ($inputPath in $files) {
    try {
        $resolved = Resolve-Path -LiteralPath $inputPath -ErrorAction Stop
        $item = Get-Item -LiteralPath $resolved.Path -ErrorAction Stop
        if ($item.PSIsContainer) {
            throw "Path is a directory: $($item.FullName)"
        }

        $extension = [System.IO.Path]::GetExtension($item.FullName).ToLowerInvariant()
        if ($extension -notin @('.dll', '.exe', '.ps1', '.psm1')) {
            throw "Unsupported Authenticode file type '$extension': $($item.FullName)"
        }

        $signature = Get-AuthenticodeSignature -FilePath $item.FullName
        if ($signature.Status -ne [System.Management.Automation.SignatureStatus]::Valid) {
            throw "Invalid signature [$($signature.Status)] $($signature.StatusMessage)"
        }
        if (-not $signature.SignerCertificate) {
            throw 'Missing signer certificate.'
        }
        $thumbprint = Normalize-Thumbprint $signature.SignerCertificate.Thumbprint
        if ($thumbprint -ne $normalizedExpected) {
            throw "Unexpected signer [$thumbprint]. Expected $normalizedExpected."
        }
        if (-not $signature.TimeStamperCertificate) {
            throw 'Missing trusted timestamp.'
        }

        if ($extension -in @('.dll', '.exe')) {
            if (-not $signTool) { $signTool = Get-SignTool }
            & $signTool verify /pa /all /v $item.FullName
            if ($LASTEXITCODE -ne 0) {
                throw "signtool Windows trust verification failed with exit code $LASTEXITCODE."
            }
        }

        Write-Host ("VALID {0} signer={1} timestamp={2}" -f $item.FullName, $thumbprint, $signature.TimeStamperCertificate.Subject)
    }
    catch {
        $failures.Add("$inputPath :: $($_.Exception.Message)")
    }
}

if ($failures.Count -gt 0) {
    foreach ($failure in $failures) { Write-Error $failure }
    throw "Authenticode verification failed for $($failures.Count) file(s)."
}

Write-Host ("PASS: verified {0} Authenticode-signed file(s) against signer {1}." -f $files.Count, $normalizedExpected)
