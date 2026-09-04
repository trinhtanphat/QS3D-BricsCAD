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
        [Parameter(Mandatory = $true)][string]$Label,
        [string]$ExpectedMessage
    )

    $failed = $false
    try {
        & $Action | Out-Null
    }
    catch {
        $failed = $true
        $message = $_.Exception.Message
        Write-Host "Expected rejection [$Label]: $message"
        if (-not [string]::IsNullOrWhiteSpace($ExpectedMessage) -and $message -notlike "*$ExpectedMessage*") {
            throw "Package verifier rejected [$Label] for the wrong reason. Expected message containing '$ExpectedMessage', got '$message'."
        }
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

    $goodArchive = [IO.Compression.ZipFile]::OpenRead($goodZip)
    try {
        $goodEntryCount = [int]$goodArchive.Entries.Count
        $goodFileLengths = @($goodArchive.Entries | Where-Object { -not [string]::IsNullOrEmpty($_.Name) } | ForEach-Object { [long]$_.Length })
        $goodMaxEntryBytes = [long](($goodFileLengths | Measure-Object -Maximum).Maximum)
        $goodTotalBytes = [long](($goodFileLengths | Measure-Object -Sum).Sum)
    }
    finally { $goodArchive.Dispose() }
    & $verifier `
        -ZipPath $goodZip `
        -ChecksumPath $goodChecksum `
        -RequiredEntries @('payload.txt', 'nested/data.bin', 'SHA256SUMS.txt') `
        -MaxArchiveEntries $goodEntryCount `
        -MaxEntryUncompressedBytes $goodMaxEntryBytes `
        -MaxTotalUncompressedBytes $goodTotalBytes

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

    $entryCountZip = Join-Path $tempRoot 'entry-count-bound.zip'
    $entryCountArchive = [IO.Compression.ZipFile]::Open($entryCountZip, [IO.Compression.ZipArchiveMode]::Create)
    try {
        1..4 | ForEach-Object { Add-ZipTextEntry -Archive $entryCountArchive -Name "entry-$_.txt" -Value 'x' }
    }
    finally { $entryCountArchive.Dispose() }
    Assert-VerifierFails -Label 'archive entry-count expansion bound' -ExpectedMessage 'archive entry count exceeds the maximum' -Action {
        & $verifier -ZipPath $entryCountZip -MaxArchiveEntries 3
    }

    $singleEntryZip = Join-Path $tempRoot 'single-entry-bound.zip'
    $singleEntryArchive = [IO.Compression.ZipFile]::Open($singleEntryZip, [IO.Compression.ZipArchiveMode]::Create)
    try {
        Add-ZipTextEntry -Archive $singleEntryArchive -Name 'oversized.txt' -Value '12345678901'
    }
    finally { $singleEntryArchive.Dispose() }
    Assert-VerifierFails -Label 'single-entry uncompressed-size bound' -ExpectedMessage 'entry exceeds the maximum uncompressed size' -Action {
        & $verifier -ZipPath $singleEntryZip -MaxEntryUncompressedBytes 10 -MaxTotalUncompressedBytes 100
    }

    $aggregateZip = Join-Path $tempRoot 'aggregate-bound.zip'
    $aggregateArchive = [IO.Compression.ZipFile]::Open($aggregateZip, [IO.Compression.ZipArchiveMode]::Create)
    try {
        Add-ZipTextEntry -Archive $aggregateArchive -Name 'one.txt' -Value '12345678'
        Add-ZipTextEntry -Archive $aggregateArchive -Name 'two.txt' -Value '12345678'
    }
    finally { $aggregateArchive.Dispose() }
    Assert-VerifierFails -Label 'aggregate uncompressed-size bound' -ExpectedMessage 'total uncompressed size exceeds the maximum' -Action {
        & $verifier -ZipPath $aggregateZip -MaxEntryUncompressedBytes 16 -MaxTotalUncompressedBytes 12
    }

    Write-Host 'V25 package verifier contract tests passed.'
}
finally {
    Remove-Item -LiteralPath $tempRoot -Recurse -Force -ErrorAction SilentlyContinue
}