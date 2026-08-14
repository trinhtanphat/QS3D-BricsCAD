param(
    [Parameter(Mandatory = $true)]
    [string]$ZipPath,

    [string]$ChecksumPath,

    [string[]]$RequiredEntries = @()
)

$ErrorActionPreference = 'Stop'

function Convert-ToSafeArchivePath {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,
        [string]$Label = 'Archive entry'
    )

    if ([string]::IsNullOrWhiteSpace($Path)) {
        throw "$Label path is empty."
    }

    $normalized = $Path.Replace('\', '/').TrimEnd('/')
    if ([string]::IsNullOrWhiteSpace($normalized)) {
        throw "$Label path is empty after normalization: '$Path'."
    }
    if ($normalized.StartsWith('/') -or $normalized -match '^[A-Za-z]:' -or $normalized.Contains(':')) {
        throw "$Label path is rooted or contains a Windows drive/ADS separator: '$Path'."
    }

    $segments = $normalized.Split('/')
    foreach ($segment in $segments) {
        if ([string]::IsNullOrEmpty($segment) -or $segment -eq '.' -or $segment -eq '..') {
            throw "$Label path contains an unsafe segment: '$Path'."
        }
    }

    return $normalized
}

function Get-StreamSha256 {
    param([Parameter(Mandatory = $true)][System.IO.Stream]$Stream)

    $sha256 = [Security.Cryptography.SHA256]::Create()
    try {
        $bytes = $sha256.ComputeHash($Stream)
        return ([BitConverter]::ToString($bytes)).Replace('-', '').ToLowerInvariant()
    }
    finally {
        $sha256.Dispose()
    }
}

$resolvedZip = (Resolve-Path -LiteralPath $ZipPath).Path
if (-not (Test-Path -LiteralPath $resolvedZip -PathType Leaf)) {
    throw "V25 package ZIP was not found: $ZipPath"
}
$zipInfo = Get-Item -LiteralPath $resolvedZip
if ($zipInfo.Length -le 0) {
    throw "V25 package ZIP is empty: $resolvedZip"
}

if (-not [string]::IsNullOrWhiteSpace($ChecksumPath)) {
    $resolvedChecksum = (Resolve-Path -LiteralPath $ChecksumPath).Path
    if (-not (Test-Path -LiteralPath $resolvedChecksum -PathType Leaf)) {
        throw "V25 package checksum file was not found: $ChecksumPath"
    }

    $checksumText = (Get-Content -LiteralPath $resolvedChecksum -Raw).Trim()
    $checksumMatch = [regex]::Match(
        $checksumText,
        '^([0-9A-Fa-f]{64})[ \t]+\*?([^\r\n]+)$',
        [Text.RegularExpressions.RegexOptions]::CultureInvariant)
    if (-not $checksumMatch.Success) {
        throw "V25 package checksum must contain exactly one '<sha256>  <filename>' record: $resolvedChecksum"
    }

    $expectedZipName = [IO.Path]::GetFileName($resolvedZip)
    $declaredZipName = $checksumMatch.Groups[2].Value.Trim()
    if (-not [string]::Equals($declaredZipName, $expectedZipName, [StringComparison]::Ordinal)) {
        throw "V25 package checksum targets '$declaredZipName' instead of '$expectedZipName'."
    }

    $expectedZipHash = $checksumMatch.Groups[1].Value.ToLowerInvariant()
    $actualZipHash = (Get-FileHash -LiteralPath $resolvedZip -Algorithm SHA256).Hash.ToLowerInvariant()
    if (-not [string]::Equals($actualZipHash, $expectedZipHash, [StringComparison]::Ordinal)) {
        throw "V25 package ZIP SHA-256 mismatch. Expected $expectedZipHash, got $actualZipHash."
    }
}

Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem

$archive = [IO.Compression.ZipFile]::OpenRead($resolvedZip)
try {
    $caseInsensitivePaths = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    $entriesByPath = [System.Collections.Generic.Dictionary[string,System.IO.Compression.ZipArchiveEntry]]::new([StringComparer]::Ordinal)
    $filePaths = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    $manifestEntry = $null

    foreach ($entry in $archive.Entries) {
        $safePath = Convert-ToSafeArchivePath -Path $entry.FullName
        if (-not $caseInsensitivePaths.Add($safePath)) {
            throw "V25 package contains a duplicate or case-colliding archive path: '$($entry.FullName)'."
        }
        $entriesByPath.Add($safePath, $entry)

        $isDirectory = [string]::IsNullOrEmpty($entry.Name)
        if (-not $isDirectory) {
            [void]$filePaths.Add($safePath)
            if ([string]::Equals($safePath, 'SHA256SUMS.txt', [StringComparison]::Ordinal)) {
                if ($null -ne $manifestEntry) {
                    throw 'V25 package contains more than one SHA256SUMS.txt manifest.'
                }
                $manifestEntry = $entry
            }
        }
    }

    if ($null -eq $manifestEntry) {
        throw 'V25 package is missing the root SHA256SUMS.txt manifest.'
    }

    foreach ($required in $RequiredEntries) {
        $safeRequired = Convert-ToSafeArchivePath -Path $required -Label 'Required entry'
        if (-not $entriesByPath.ContainsKey($safeRequired)) {
            throw "V25 package is missing required entry: $safeRequired"
        }
        if ([string]::IsNullOrEmpty($entriesByPath[$safeRequired].Name)) {
            throw "V25 package required entry is a directory instead of a file: $safeRequired"
        }
    }

    $manifestStream = $manifestEntry.Open()
    try {
        $reader = [IO.StreamReader]::new($manifestStream)
        try {
            $manifestText = $reader.ReadToEnd()
        }
        finally {
            $reader.Dispose()
        }
    }
    finally {
        $manifestStream.Dispose()
    }

    $manifestPaths = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    $manifestRecords = @()
    foreach ($line in ($manifestText -split '\r?\n')) {
        if ([string]::IsNullOrWhiteSpace($line)) { continue }
        $recordMatch = [regex]::Match(
            $line,
            '^([0-9A-Fa-f]{64})[ \t]+(.+)$',
            [Text.RegularExpressions.RegexOptions]::CultureInvariant)
        if (-not $recordMatch.Success) {
            throw "Invalid SHA256SUMS.txt record: '$line'."
        }

        $expectedHash = $recordMatch.Groups[1].Value.ToLowerInvariant()
        $declaredPath = Convert-ToSafeArchivePath -Path $recordMatch.Groups[2].Value.Trim() -Label 'SHA256SUMS.txt entry'
        if ([string]::Equals($declaredPath, 'SHA256SUMS.txt', [StringComparison]::OrdinalIgnoreCase)) {
            throw 'SHA256SUMS.txt must not hash itself.'
        }
        if (-not $manifestPaths.Add($declaredPath)) {
            throw "SHA256SUMS.txt contains a duplicate or case-colliding path: $declaredPath"
        }
        if (-not $entriesByPath.ContainsKey($declaredPath)) {
            throw "SHA256SUMS.txt references a missing archive entry: $declaredPath"
        }

        $targetEntry = $entriesByPath[$declaredPath]
        if ([string]::IsNullOrEmpty($targetEntry.Name)) {
            throw "SHA256SUMS.txt references a directory instead of a file: $declaredPath"
        }

        $manifestRecords += [pscustomobject]@{
            Path = $declaredPath
            ExpectedHash = $expectedHash
            Entry = $targetEntry
        }
    }

    if ($manifestRecords.Count -eq 0) {
        throw 'SHA256SUMS.txt contains no package file records.'
    }

    foreach ($filePath in $filePaths) {
        if ([string]::Equals($filePath, 'SHA256SUMS.txt', [StringComparison]::Ordinal)) { continue }
        if (-not $manifestPaths.Contains($filePath)) {
            throw "Archive file is not covered by SHA256SUMS.txt: $filePath"
        }
    }
    if ($manifestPaths.Count -ne ($filePaths.Count - 1)) {
        throw "SHA256SUMS.txt coverage count does not match archive file count. Manifest=$($manifestPaths.Count), archive=$($filePaths.Count - 1)."
    }

    foreach ($record in $manifestRecords) {
        $entryStream = $record.Entry.Open()
        try {
            $actualHash = Get-StreamSha256 -Stream $entryStream
        }
        finally {
            $entryStream.Dispose()
        }
        if (-not [string]::Equals($actualHash, $record.ExpectedHash, [StringComparison]::Ordinal)) {
            throw "Archive entry SHA-256 mismatch for '$($record.Path)'. Expected $($record.ExpectedHash), got $actualHash."
        }
    }
}
finally {
    $archive.Dispose()
}

Write-Host "Verified V25 package integrity: $resolvedZip"
Write-Host "Archive bytes: $($zipInfo.Length)"
Write-Host "Manifest-covered files: $($manifestRecords.Count)"
