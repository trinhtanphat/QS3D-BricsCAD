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

$uploadBaseUri = $null
if (-not [Uri]::TryCreate($UploadBase, [UriKind]::Absolute, [ref]$uploadBaseUri) -or
    -not [string]::Equals($uploadBaseUri.Scheme, 'https', [StringComparison]::OrdinalIgnoreCase) -or
    -not [string]::Equals($uploadBaseUri.Host, 'uploads.github.com', [StringComparison]::OrdinalIgnoreCase) -or
    -not $uploadBaseUri.IsDefaultPort -or
    -not [string]::IsNullOrEmpty($uploadBaseUri.UserInfo) -or
    -not [string]::IsNullOrEmpty($uploadBaseUri.Fragment) -or
    -not [string]::IsNullOrEmpty($uploadBaseUri.Query)) {
    throw 'UploadBase must be an absolute HTTPS uploads.github.com endpoint with the default port and no credentials, fragment, or query.'
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
    $uploadUri = $uploadBaseUri.AbsoluteUri + '?name=' + $encodedName

    $handler = [System.Net.Http.HttpClientHandler]::new()
    $handler.AllowAutoRedirect = $false
    $client = [System.Net.Http.HttpClient]::new($handler)
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
                if ($response.StatusCode -ne [System.Net.HttpStatusCode]::Created) {
                    throw "GitHub release asset upload failed for $Name with HTTP $([int]$response.StatusCode); expected HTTP 201 Created."
                }

                $responseBody = $response.Content.ReadAsStringAsync().GetAwaiter().GetResult()
                if ([string]::IsNullOrWhiteSpace($responseBody)) {
                    throw "GitHub release asset upload returned an empty response for $Name."
                }

                try {
                    $uploadedAsset = $responseBody | ConvertFrom-Json -ErrorAction Stop
                }
                catch {
                    throw "GitHub release asset upload returned invalid JSON for $Name."
                }

                $uploadedAssetId = 0L
                if (-not [Int64]::TryParse(([string]$uploadedAsset.id), [ref]$uploadedAssetId) -or $uploadedAssetId -le 0) {
                    throw "GitHub release asset upload returned an invalid asset id for $Name."
                }
                if (-not [string]::Equals(([string]$uploadedAsset.name), $Name, [StringComparison]::Ordinal)) {
                    throw "GitHub release asset upload returned a mismatched asset name for $Name."
                }
                if (-not [string]::Equals(([string]$uploadedAsset.state), 'uploaded', [StringComparison]::Ordinal)) {
                    throw "GitHub release asset upload did not reach uploaded state for $Name."
                }
                if ([Int64]$uploadedAsset.size -ne $ExpectedSize) {
                    throw "GitHub release asset upload returned a mismatched asset size for $Name. Expected=$ExpectedSize, API=$($uploadedAsset.size)."
                }

                $expectedDigest = 'sha256:' + $ExpectedSha256.ToLowerInvariant()
                $uploadedDigest = ([string]$uploadedAsset.digest).Trim()
                if ($uploadedDigest -notmatch '^sha256:[0-9A-Fa-f]{64}$' -or
                    -not [string]::Equals($uploadedDigest, $expectedDigest, [StringComparison]::OrdinalIgnoreCase)) {
                    throw "GitHub release asset upload returned a mismatched SHA-256 digest for $Name. Expected=$expectedDigest, API=$uploadedDigest."
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
