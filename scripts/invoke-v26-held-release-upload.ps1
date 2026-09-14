[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$UploadBase,

    [Parameter(Mandatory = $true)]
    [string]$Token,

    [Parameter(Mandatory = $true)]
    [string]$Path,

    [Parameter(Mandatory = $true)]
    [string]$ContentType
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# Windows PowerShell does not guarantee that System.Net.Http is loaded before a script
# resolves its first HttpClient/StreamContent type literal. Load the framework assembly
# explicitly while the admitted-generation stream contract remains unchanged.
Add-Type -AssemblyName System.Net.Http

function Get-CanonicalFullPath {
    param([Parameter(Mandatory = $true)][string]$LiteralPath)
    return [IO.Path]::GetFullPath($LiteralPath)
}

function Assert-NoReparseAncestor {
    param([Parameter(Mandatory = $true)][string]$LiteralPath)

    $cursor = [IO.Directory]::GetParent((Get-CanonicalFullPath -LiteralPath $LiteralPath))
    while ($null -ne $cursor) {
        if (($cursor.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
            throw "Held V26 upload input traverses a reparse-point ancestor: $($cursor.FullName)"
        }
        $cursor = $cursor.Parent
    }
}

function Open-HeldGeneration {
    param([Parameter(Mandatory = $true)][string]$LiteralPath)

    $canonical = Get-CanonicalFullPath -LiteralPath $LiteralPath
    Assert-NoReparseAncestor -LiteralPath $canonical
    $admitted = Get-Item -LiteralPath $canonical -Force -ErrorAction Stop
    if ($admitted.PSIsContainer) { throw "Held V26 upload input must be a file: $canonical" }
    if (($admitted.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
        throw "Held V26 upload input must not be a reparse point: $canonical"
    }

    $admittedPath = Get-CanonicalFullPath -LiteralPath $admitted.FullName
    if (-not [string]::Equals($canonical, $admittedPath, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Held V26 upload input canonical identity drifted before open: $canonical"
    }
    $admittedLength = [int64]$admitted.Length
    $admittedWriteTicks = [int64]$admitted.LastWriteTimeUtc.Ticks

    $stream = [IO.File]::Open($canonical, [IO.FileMode]::Open, [IO.FileAccess]::Read, [IO.FileShare]::Read)
    try {
        $rebound = Get-Item -LiteralPath $canonical -Force -ErrorAction Stop
        $reboundPath = Get-CanonicalFullPath -LiteralPath $rebound.FullName
        if ($rebound.PSIsContainer -or (($rebound.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0)) {
            throw "Held V26 upload input changed to a non-ordinary file after open: $canonical"
        }
        if (-not [string]::Equals($admittedPath, $reboundPath, [StringComparison]::OrdinalIgnoreCase) -or
            [int64]$rebound.Length -ne $admittedLength -or
            [int64]$rebound.LastWriteTimeUtc.Ticks -ne $admittedWriteTicks -or
            [int64]$stream.Length -ne $admittedLength) {
            throw "Held V26 upload input generation changed across admission/open: $canonical"
        }
        return [pscustomobject]@{
            Stream = $stream
            CanonicalPath = $admittedPath
            Length = $admittedLength
            LastWriteTimeUtcTicks = $admittedWriteTicks
        }
    }
    catch {
        $stream.Dispose()
        throw
    }
}

if ([string]::IsNullOrWhiteSpace($Token)) { throw 'V26 held upload token is required.' }
if ([string]::IsNullOrWhiteSpace($UploadBase)) { throw 'V26 held upload base URI is required.' }
$uploadUri = $null
if (-not [Uri]::TryCreate($UploadBase, [UriKind]::Absolute, [ref]$uploadUri) -or
    -not [string]::Equals($uploadUri.Scheme, 'https', [StringComparison]::OrdinalIgnoreCase) -or
    -not [string]::Equals($uploadUri.Host, 'uploads.github.com', [StringComparison]::OrdinalIgnoreCase) -or
    -not $uploadUri.IsDefaultPort -or
    -not [string]::IsNullOrEmpty($uploadUri.UserInfo) -or
    -not [string]::IsNullOrEmpty($uploadUri.Fragment) -or
    -not [string]::IsNullOrEmpty($uploadUri.Query)) {
    throw 'V26 held upload base must be an absolute HTTPS uploads.github.com endpoint with the default port and no credentials, fragment, or query.'
}

$held = Open-HeldGeneration -LiteralPath $Path
try {
    $name = [IO.Path]::GetFileName($held.CanonicalPath)
    if ([string]::IsNullOrWhiteSpace($name)) { throw 'Held V26 upload input has no asset name.' }
    if ($name -match 'V25') { throw "V25 release asset leaked into V26 held upload: $name" }

    $sha = [Security.Cryptography.SHA256]::Create()
    try {
        $digest = $sha.ComputeHash($held.Stream)
        $hashHex = -join ($digest | ForEach-Object { $_.ToString('x2') })
    }
    finally {
        $sha.Dispose()
    }

    $held.Stream.Position = 0
    $handler = [System.Net.Http.HttpClientHandler]::new()
    $handler.AllowAutoRedirect = $false
    $client = [System.Net.Http.HttpClient]::new($handler)
    $request = $null
    $response = $null
    try {
        $client.DefaultRequestHeaders.Authorization = [System.Net.Http.Headers.AuthenticationHeaderValue]::new('Bearer', $Token)
        $client.DefaultRequestHeaders.Accept.ParseAdd('application/vnd.github+json')
        $client.DefaultRequestHeaders.Add('X-GitHub-Api-Version', '2022-11-28')
        $client.DefaultRequestHeaders.UserAgent.ParseAdd('QS3D-V26-Manual-Release')

        $requestUri = $uploadUri.AbsoluteUri + '?name=' + [Uri]::EscapeDataString($name)
        $request = [System.Net.Http.HttpRequestMessage]::new([System.Net.Http.HttpMethod]::Post, $requestUri)
        $request.Content = [System.Net.Http.StreamContent]::new($held.Stream)
        $request.Content.Headers.ContentType = [System.Net.Http.Headers.MediaTypeHeaderValue]::Parse($ContentType)
        $request.Content.Headers.ContentLength = [int64]$held.Length

        $response = $client.SendAsync($request, [System.Net.Http.HttpCompletionOption]::ResponseHeadersRead).GetAwaiter().GetResult()
        if ($response.StatusCode -ne [System.Net.HttpStatusCode]::Created) {
            throw "V26 held asset upload failed for $name with HTTP $([int]$response.StatusCode)."
        }

        $responseBody = $response.Content.ReadAsStringAsync().GetAwaiter().GetResult()
        if ([string]::IsNullOrWhiteSpace($responseBody)) { throw "V26 held asset upload returned an empty response for $name." }
        try {
            $uploaded = $responseBody | ConvertFrom-Json -ErrorAction Stop
        }
        catch {
            throw "V26 held asset upload returned invalid JSON for $name."
        }

        $uploadedAssetId = 0L
        if ($null -eq $uploaded -or -not [Int64]::TryParse(([string]$uploaded.id), [ref]$uploadedAssetId) -or $uploadedAssetId -le 0) {
            throw "V26 held asset upload returned no usable asset id for $name."
        }
        if (-not [string]::Equals([string]$uploaded.name, $name, [StringComparison]::Ordinal)) {
            throw "V26 held asset upload returned mismatched asset name for $name."
        }
        if (-not [string]::Equals([string]$uploaded.state, 'uploaded', [StringComparison]::Ordinal)) {
            throw "V26 held asset upload did not reach uploaded state for $name."
        }

        $uploadedSize = 0L
        if (-not [Int64]::TryParse(([string]$uploaded.size), [ref]$uploadedSize) -or $uploadedSize -ne [int64]$held.Length) {
            throw "V26 held asset upload returned mismatched asset size for $name."
        }

        $expectedDigest = 'sha256:' + $hashHex.ToLowerInvariant()
        $uploadedDigest = ([string]$uploaded.digest).Trim()
        if ($uploadedDigest -notmatch '^sha256:[0-9A-Fa-f]{64}$' -or
            -not [string]::Equals($uploadedDigest, $expectedDigest, [StringComparison]::OrdinalIgnoreCase)) {
            throw "V26 held asset upload returned mismatched SHA-256 digest for $name."
        }

        [pscustomobject]@{
            Name = $name
            CanonicalPath = $held.CanonicalPath
            Length = [int64]$held.Length
            LastWriteTimeUtcTicks = [int64]$held.LastWriteTimeUtcTicks
            Sha256 = $hashHex
            UploadedAssetId = $uploadedAssetId
        }
    }
    finally {
        if ($null -ne $response) { $response.Dispose() }
        if ($null -ne $request) { $request.Dispose() }
        $client.Dispose()
    }
}
finally {
    $held.Stream.Dispose()
}
