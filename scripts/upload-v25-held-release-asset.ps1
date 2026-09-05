[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$Path,

    [Parameter(Mandatory = $true)]
    [string]$Name,

    [Parameter(Mandatory = $true)]
    [string]$UploadBase,

    [Parameter(Mandatory = $true)]
    [hashtable]$Headers,

    [Parameter(Mandatory = $true)]
    [string]$ContentType,

    [Parameter(Mandatory = $true)]
    [string]$ExpectedSha256,

    [Parameter(Mandatory = $true)]
    [Int64]$ExpectedSize
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Net.Http

if ($ExpectedSha256 -notmatch '^[0-9A-Fa-f]{64}$') {
    throw "ExpectedSha256 must be exactly 64 hexadecimal characters for $Name."
}
if ($ExpectedSize -le 0) {
    throw "ExpectedSize must be positive for $Name."
}
if ([string]::IsNullOrWhiteSpace($UploadBase) -or [string]::IsNullOrWhiteSpace($Name)) {
    throw 'UploadBase and Name are required.'
}

$resolvedPath = (Resolve-Path -LiteralPath $Path).Path
$stream = [System.IO.File]::Open(
    $resolvedPath,
    [System.IO.FileMode]::Open,
    [System.IO.FileAccess]::Read,
    [System.IO.FileShare]::Read)
try {
    if ($stream.Length -ne $ExpectedSize) {
        throw "Held release asset size mismatch for $Name. Expected=$ExpectedSize, actual=$($stream.Length)."
    }

    $sha256 = [System.Security.Cryptography.SHA256]::Create()
    try {
        $actualHash = ([BitConverter]::ToString($sha256.ComputeHash($stream))).Replace('-', '')
    }
    finally {
        $sha256.Dispose()
    }

    if (-not [string]::Equals($actualHash, $ExpectedSha256, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Held release asset SHA-256 mismatch for $Name."
    }

    $stream.Position = 0
    $encodedName = [Uri]::EscapeDataString($Name)
    $uploadUri = $UploadBase + '?name=' + $encodedName

    $client = [System.Net.Http.HttpClient]::new()
    try {
        foreach ($key in $Headers.Keys) {
            $headerValue = [string]$Headers[$key]
            if (-not $client.DefaultRequestHeaders.TryAddWithoutValidation([string]$key, $headerValue)) {
                throw "Could not apply upload request header '$key'."
            }
        }

        $content = [System.Net.Http.StreamContent]::new($stream)
        try {
            $content.Headers.ContentType = [System.Net.Http.Headers.MediaTypeHeaderValue]::Parse($ContentType)
            $response = $client.PostAsync($uploadUri, $content).GetAwaiter().GetResult()
            try {
                if (-not $response.IsSuccessStatusCode) {
                    throw "GitHub release asset upload failed for $Name with HTTP $([int]$response.StatusCode)."
                }
            }
            finally {
                $response.Dispose()
            }
        }
        finally {
            $content.Dispose()
        }
    }
    finally {
        $client.Dispose()
    }
}
finally {
    $stream.Dispose()
}
