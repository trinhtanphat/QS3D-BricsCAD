param(
    [Parameter(Mandatory = $true)]
    [string]$ZipPath,

    [string]$ChecksumPath,

    [string[]]$RequiredEntries = @()
)

$ErrorActionPreference = 'Stop'

function Assert-NoReparseAncestors {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$Label
    )

    $cursor = Get-Item -LiteralPath $Path -Force -ErrorAction Stop
    while ($null -ne $cursor) {
        if (($cursor.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
            throw "$Label is reparse-backed: $($cursor.FullName)"
        }
        $parent = $cursor.Parent
        if ($null -eq $parent) { break }
        $cursor = $parent
    }
}

function Resolve-OrdinaryNonReparseFile {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$Label
    )

    $full = [IO.Path]::GetFullPath($Path)
    $item = Get-Item -LiteralPath $full -Force -ErrorAction Stop
    if ($item.PSIsContainer -or -not ($item -is [IO.FileInfo])) {
        throw "$Label is not an ordinary file: $full"
    }
    Assert-NoReparseAncestors -Path $item.FullName -Label $Label
    return $item
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

function Get-FileStreamSha256 {
    param(
        [Parameter(Mandatory = $true)][IO.FileInfo]$File,
        [Parameter(Mandatory = $true)][string]$Label
    )

    $stream = [IO.File]::Open($File.FullName, [IO.FileMode]::Open, [IO.FileAccess]::Read, [IO.FileShare]::Read)
    try {
        return Get-StreamSha256 -Stream $stream
    }
    catch {
        throw "$Label SHA-256 could not be read safely: $($_.Exception.Message)"
    }
    finally {
        $stream.Dispose()
    }
}

function Get-StableFileState {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$Label
    )

    $first = Resolve-OrdinaryNonReparseFile -Path $Path -Label $Label
    $firstLength = [long]$first.Length
    $firstTicks = [long]$first.LastWriteTimeUtc.Ticks
    $firstHash = Get-FileStreamSha256 -File $first -Label $Label

    $second = Resolve-OrdinaryNonReparseFile -Path $first.FullName -Label $Label
    $secondHash = Get-FileStreamSha256 -File $second -Label $Label
    if ($firstLength -ne [long]$second.Length -or
        $firstTicks -ne [long]$second.LastWriteTimeUtc.Ticks -or
        -not [string]::Equals($firstHash, $secondHash, [StringComparison]::Ordinal)) {
        throw "$Label changed while its stable input state was being captured."
    }

    return [pscustomobject]@{
        Path = $second.FullName
        Length = [long]$second.Length
        LastWriteUtcTicks = [long]$second.LastWriteTimeUtc.Ticks
        Sha256 = $secondHash
    }
}

function Assert-StableFileState {
    param(
        [Parameter(Mandatory = $true)]$Expected,
        [Parameter(Mandatory = $true)][string]$Label
    )

    $current = Resolve-OrdinaryNonReparseFile -Path ([string]$Expected.Path) -Label $Label
    $currentHash = Get-FileStreamSha256 -File $current -Label $Label
    if ([long]$Expected.Length -ne [long]$current.Length -or
        [long]$Expected.LastWriteUtcTicks -ne [long]$current.LastWriteTimeUtc.Ticks -or
        -not [string]::Equals([string]$Expected.Sha256, $currentHash, [StringComparison]::Ordinal)) {
        throw "$Label changed after its admitted input generation was captured."
    }
    return $current
}

function Open-StableReadStream {
    param(
        [Parameter(Mandatory = $true)]$State,
        [Parameter(Mandatory = $true)][string]$Label
    )

    $file = Assert-StableFileState -Expected $State -Label $Label
    $stream = [IO.File]::Open($file.FullName, [IO.FileMode]::Open, [IO.FileAccess]::Read, [IO.FileShare]::Read)
    try {
        if ([long]$stream.Length -ne [long]$State.Length) {
            throw "$Label changed before its admitted generation could be opened."
        }
        $streamHash = Get-StreamSha256 -Stream $stream
        if (-not [string]::Equals([string]$State.Sha256, $streamHash, [StringComparison]::Ordinal)) {
            throw "$Label changed before its admitted generation could be opened."
        }
        $stream.Position = 0
        return $stream
    }
    catch {
        $stream.Dispose()
        throw
    }
}

function Read-BoundedStrictUtf8State {
    param(
        [Parameter(Mandatory = $true)]$State,
        [Parameter(Mandatory = $true)][string]$Label,
        [int]$MaxBytes = 65536
    )

    if ([long]$State.Length -gt $MaxBytes) {
        throw "$Label exceeds the bounded UTF-8 read limit of $MaxBytes bytes."
    }

    $stream = Open-StableReadStream -State $State -Label $Label
    $utf8 = [Text.UTF8Encoding]::new($false, $true)
    $reader = [IO.StreamReader]::new($stream, $utf8, $true)
    try {
        return $reader.ReadToEnd()
    }
    catch {
        throw "$Label is not strict UTF-8: $($_.Exception.Message)"
    }
    finally {
        $reader.Dispose()
        $stream.Dispose()
        $null = Assert-StableFileState -Expected $State -Label $Label
    }
}

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

$zipState = Get-StableFileState -Path $ZipPath -Label 'V25 package ZIP'
if ([long]$zipState.Length -le 0) {
    throw "V25 package ZIP is empty: $($zipState.Path)"
}

$checksumState = $null
if (-not [string]::IsNullOrWhiteSpace($ChecksumPath)) {
    $checksumState = Get-StableFileState -Path $ChecksumPath -Label 'V25 package checksum'
    $checksumText = (Read-BoundedStrictUtf8State -State $checksumState -Label 'V25 package checksum').Trim()
    $checksumMatch = [regex]::Match(
        $checksumText,
        '^([0-9A-Fa-f]{64})[ \t]+\*?([^\r\n]+)$',
        [Text.RegularExpressions.RegexOptions]::CultureInvariant)
    if (-not $checksumMatch.Success) {
        throw "V25 package checksum must contain exactly one '<sha256>  <filename>' record: $($checksumState.Path)"
    }

    $expectedZipName = [IO.Path]::GetFileName([string]$zipState.Path)
    $declaredZipName = $checksumMatch.Groups[2].Value.Trim()
    if (-not [string]::Equals($declaredZipName, $expectedZipName, [StringComparison]::Ordinal)) {
        throw "V25 package checksum targets '$declaredZipName' instead of '$expectedZipName'."
    }

    $expectedZipHash = $checksumMatch.Groups[1].Value.ToLowerInvariant()
    if (-not [string]::Equals([string]$zipState.Sha256, $expectedZipHash, [StringComparison]::Ordinal)) {
        throw "V25 package ZIP SHA-256 mismatch. Expected $expectedZipHash, got $($zipState.Sha256)."
    }
    $null = Assert-StableFileState -Expected $checksumState -Label 'V25 package checksum'
}

Add-Type -AssemblyName System.IO.Compression

$zipStream = Open-StableReadStream -State $zipState -Label 'V25 package ZIP'
$archive = [IO.Compression.ZipArchive]::new($zipStream, [IO.Compression.ZipArchiveMode]::Read, $false)
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
        $reader = [IO.StreamReader]::new($manifestStream, [Text.UTF8Encoding]::new($false, $true), $true)
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
    $zipStream.Dispose()
}

$null = Assert-StableFileState -Expected $zipState -Label 'V25 package ZIP'
if ($null -ne $checksumState) {
    $null = Assert-StableFileState -Expected $checksumState -Label 'V25 package checksum'
}

Write-Host "Verified V25 package integrity: $($zipState.Path)"
Write-Host "Archive bytes: $($zipState.Length)"
Write-Host "Manifest-covered files: $($manifestRecords.Count)"
