$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$verifier = Join-Path $PSScriptRoot 'verify-v25-package.ps1'
if (-not (Test-Path -LiteralPath $verifier -PathType Leaf)) { throw "Package verifier was not found: $verifier" }

Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem

function New-SyntheticPackage {
    param(
        [Parameter(Mandatory = $true)][string]$Directory,
        [Parameter(Mandatory = $true)][string]$ZipPath,
        [switch]$TamperPayload
    )

    Remove-Item -LiteralPath $Directory -Recurse -Force -ErrorAction SilentlyContinue
    Remove-Item -LiteralPath $ZipPath -Force -ErrorAction SilentlyContinue
    New-Item -ItemType Directory -Path (Join-Path $Directory 'nested') -Force | Out-Null
    Set-Content -LiteralPath (Join-Path $Directory 'payload.txt') -Value 'verified payload' -Encoding ASCII
    [IO.File]::WriteAllBytes((Join-Path $Directory 'nested/data.bin'), [byte[]](0, 1, 2, 3, 254, 255))

    $directoryFull = [IO.Path]::GetFullPath($Directory).TrimEnd([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar)
    $hashLines = Get-ChildItem -LiteralPath $Directory -Recurse -File | Sort-Object FullName | ForEach-Object {
        $hash = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash
        $relative = $_.FullName.Substring($directoryFull.Length + 1).Replace([IO.Path]::DirectorySeparatorChar, '/')
        "$hash  $relative"
    }
    $hashLines | Set-Content -LiteralPath (Join-Path $Directory 'SHA256SUMS.txt') -Encoding ASCII

    if ($TamperPayload) {
        Set-Content -LiteralPath (Join-Path $Directory 'payload.txt') -Value 'tampered after manifest' -Encoding ASCII
    }

    Compress-Archive -Path (Join-Path $Directory '*') -DestinationPath $ZipPath -CompressionLevel Optimal
    $checksumPath = $ZipPath + '.sha256'
    $zipHash = (Get-FileHash -LiteralPath $ZipPath -Algorithm SHA256).Hash.ToLowerInvariant()
    "$zipHash  $([IO.Path]::GetFileName($ZipPath))" | Set-Content -LiteralPath $checksumPath -Encoding ASCII
    return $checksumPath
}

function Add-ZipTextEntry {
    param(
        [Parameter(Mandatory = $true)][System.IO.Compression.ZipArchive]$Archive,
        [Parameter(Mandatory = $true)][string]$Name,
        [Parameter(Mandatory = $true)][string]$Value
    )

    $entry = $Archive.CreateEntry($Name)
    $stream = $entry.Open()
    try {
        $writer = [IO.StreamWriter]::new($stream)
        try { $writer.Write($Value) }
        finally { $writer.Dispose() }
    }
    finally { $stream.Dispose() }
}

function Assert-VerifierFails {
    param(
        [Parameter(Mandatory = $true)][scriptblock]$Action,
        [Parameter(Mandatory = $true)][string]$Label
    )

    $failed = $false
    try {
        & $Action | Out-Null
    }
    catch {
        $failed = $true
        Write-Host "Expected rejection [$Label]: $($_.Exception.Message)"
    }
    if (-not $failed) {
        throw "Package verifier accepted an invalid fixture: $Label"
    }
}

$tempRoot = Join-Path ([IO.Path]::GetTempPath()) ('qs3d-v25-package-verifier-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $tempRoot -Force | Out-Null
try {
    $goodDir = Join-Path $tempRoot 'good'
    $goodZip = Join-Path $tempRoot 'good.zip'
    $goodChecksum = New-SyntheticPackage -Directory $goodDir -ZipPath $goodZip
    & $verifier -ZipPath $goodZip -ChecksumPath $goodChecksum -RequiredEntries @('payload.txt', 'nested/data.bin', 'SHA256SUMS.txt')

    $badExternalChecksum = Join-Path $tempRoot 'bad-external.sha256'
    (('0' * 64) + '  ' + [IO.Path]::GetFileName($goodZip)) | Set-Content -LiteralPath $badExternalChecksum -Encoding ASCII
    Assert-VerifierFails -Label 'external ZIP checksum tamper' -Action {
        & $verifier -ZipPath $goodZip -ChecksumPath $badExternalChecksum
    }

    $tamperedDir = Join-Path $tempRoot 'tampered-payload'
    $tamperedZip = Join-Path $tempRoot 'tampered-payload.zip'
    $tamperedChecksum = New-SyntheticPackage -Directory $tamperedDir -ZipPath $tamperedZip -TamperPayload
    Assert-VerifierFails -Label 'internal payload hash tamper' -Action {
        & $verifier -ZipPath $tamperedZip -ChecksumPath $tamperedChecksum
    }

    $traversalZip = Join-Path $tempRoot 'traversal.zip'
    $traversalArchive = [IO.Compression.ZipFile]::Open($traversalZip, [IO.Compression.ZipArchiveMode]::Create)
    try {
        Add-ZipTextEntry -Archive $traversalArchive -Name '../escape.txt' -Value 'escape'
    }
    finally { $traversalArchive.Dispose() }
    Assert-VerifierFails -Label 'archive path traversal' -Action {
        & $verifier -ZipPath $traversalZip
    }

    $collisionZip = Join-Path $tempRoot 'case-collision.zip'
    $collisionArchive = [IO.Compression.ZipFile]::Open($collisionZip, [IO.Compression.ZipArchiveMode]::Create)
    try {
        Add-ZipTextEntry -Archive $collisionArchive -Name 'payload.txt' -Value 'one'
        Add-ZipTextEntry -Archive $collisionArchive -Name 'PAYLOAD.txt' -Value 'two'
    }
    finally { $collisionArchive.Dispose() }
    Assert-VerifierFails -Label 'case-colliding archive paths' -Action {
        & $verifier -ZipPath $collisionZip
    }

    Write-Host 'V25 package verifier contract tests passed.'
}
finally {
    Remove-Item -LiteralPath $tempRoot -Recurse -Force -ErrorAction SilentlyContinue
}
