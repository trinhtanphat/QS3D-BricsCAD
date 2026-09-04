[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$BricsCadDir,

    [string]$StatePath,

    [string]$VerifyStatePath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if (-not [string]::IsNullOrWhiteSpace($StatePath) -and -not [string]::IsNullOrWhiteSpace($VerifyStatePath)) {
    throw 'Specify either StatePath or VerifyStatePath, not both.'
}

function Get-CanonicalAbsolutePath {
    param([Parameter(Mandatory = $true)][string]$Path)

    if ([string]::IsNullOrWhiteSpace($Path)) {
        throw 'BricsCadDir must not be empty.'
    }

    $trimmed = $Path.Trim()
    if (-not [IO.Path]::IsPathRooted($trimmed)) {
        throw "BricsCadDir must be an absolute filesystem path: $Path"
    }

    try {
        return [IO.Path]::GetFullPath($trimmed)
    }
    catch {
        throw "BricsCadDir is not a valid absolute filesystem path: $Path"
    }
}

function Assert-NoExistingReparseComponent {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$Label
    )

    $canonical = Get-CanonicalAbsolutePath -Path $Path
    $root = [IO.Path]::GetPathRoot($canonical)
    if ([string]::IsNullOrWhiteSpace($root)) {
        throw "$Label must have a filesystem root."
    }

    $relative = $canonical.Substring($root.Length)
    $current = $root
    foreach ($segment in @($relative -split '[\\/]' | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })) {
        $current = Join-Path $current $segment
        if (-not (Test-Path -LiteralPath $current)) {
            break
        }

        $item = Get-Item -LiteralPath $current -Force
        if (($item.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
            throw "$Label must not traverse a filesystem reparse point: $current"
        }
    }
}

function Get-RequiredOrdinaryFile {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$Label
    )

    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        throw "Required BricsCAD V26 host file is missing: $Label"
    }

    $item = Get-Item -LiteralPath $Path -Force
    if ($item.PSIsContainer -or ($item.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
        throw "Required BricsCAD V26 host file must be an ordinary non-reparse file: $Label"
    }

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
    finally {
        $stream.Dispose()
    }
}

function Get-StableHostFileState {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$Label
    )

    Assert-NoExistingReparseComponent -Path $Path -Label $Label
    $first = Get-RequiredOrdinaryFile -Path $Path -Label $Label
    $firstLength = [long]$first.Length
    $firstTicks = [long]$first.LastWriteTimeUtc.Ticks
    $firstHash = Get-FileStreamSha256 -File $first -Label $Label

    Assert-NoExistingReparseComponent -Path $first.FullName -Label $Label
    $second = Get-RequiredOrdinaryFile -Path $first.FullName -Label $Label
    $secondHash = Get-FileStreamSha256 -File $second -Label $Label
    if ($firstLength -ne [long]$second.Length -or
        $firstTicks -ne [long]$second.LastWriteTimeUtc.Ticks -or
        -not [string]::Equals($firstHash, $secondHash, [StringComparison]::Ordinal)) {
        throw "$Label changed while stable V26 host reference state was being captured."
    }

    return [pscustomobject]@{
        Path = $second.FullName
        Length = [long]$second.Length
        LastWriteUtcTicks = [long]$second.LastWriteTimeUtc.Ticks
        Sha256 = $secondHash
    }
}

function Assert-StableHostFileState {
    param(
        [Parameter(Mandatory = $true)]$Expected,
        [Parameter(Mandatory = $true)][string]$Label
    )

    $expectedPath = [string]$Expected.Path
    Assert-NoExistingReparseComponent -Path $expectedPath -Label $Label
    $current = Get-RequiredOrdinaryFile -Path $expectedPath -Label $Label
    $currentHash = Get-FileStreamSha256 -File $current -Label $Label
    if ([long]$Expected.Length -ne [long]$current.Length -or
        [long]$Expected.LastWriteUtcTicks -ne [long]$current.LastWriteTimeUtc.Ticks -or
        -not [string]::Equals([string]$Expected.Sha256, $currentHash, [StringComparison]::Ordinal)) {
        throw "$Label changed after its admitted V26 host reference generation was captured."
    }
    return $current
}

function Read-BoundedStrictUtf8 {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$Label,
        [int]$MaxBytes = 65536
    )

    Assert-NoExistingReparseComponent -Path $Path -Label $Label
    $file = Get-RequiredOrdinaryFile -Path $Path -Label $Label
    $stream = [IO.File]::Open($file.FullName, [IO.FileMode]::Open, [IO.FileAccess]::Read, [IO.FileShare]::Read)
    if ([long]$stream.Length -gt $MaxBytes) {
        $stream.Dispose()
        throw "$Label exceeds the bounded read limit of $MaxBytes bytes."
    }
    $reader = [IO.StreamReader]::new($stream, [Text.UTF8Encoding]::new($false, $true), $true)
    try {
        return $reader.ReadToEnd()
    }
    finally {
        $reader.Dispose()
        $stream.Dispose()
    }
}

function Write-StateFile {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)]$State
    )

    $full = [IO.Path]::GetFullPath($Path)
    $parent = Split-Path -Parent $full
    if ([string]::IsNullOrWhiteSpace($parent) -or -not (Test-Path -LiteralPath $parent -PathType Container)) {
        throw "V26 host reference state parent does not exist: $parent"
    }
    Assert-NoExistingReparseComponent -Path $parent -Label 'V26 host reference state parent'
    if (Test-Path -LiteralPath $full) {
        $existing = Get-Item -LiteralPath $full -Force
        if ($existing.PSIsContainer -or ($existing.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
            throw 'V26 host reference state path must not be a directory or reparse point.'
        }
    }

    $json = $State | ConvertTo-Json -Depth 5 -Compress
    $bytes = [Text.UTF8Encoding]::new($false, $true).GetBytes($json)
    if ($bytes.Length -gt 65536) {
        throw 'V26 host reference state exceeds 65536 bytes.'
    }
    $temp = Join-Path $parent ('.' + [IO.Path]::GetFileName($full) + '.' + [Guid]::NewGuid().ToString('N') + '.tmp')
    try {
        [IO.File]::WriteAllBytes($temp, $bytes)
        Move-Item -LiteralPath $temp -Destination $full -Force
    }
    finally {
        if (Test-Path -LiteralPath $temp) { Remove-Item -LiteralPath $temp -Force }
    }
}

$canonicalDir = Get-CanonicalAbsolutePath -Path $BricsCadDir
Assert-NoExistingReparseComponent -Path $canonicalDir -Label 'BricsCadDir'
if (-not (Test-Path -LiteralPath $canonicalDir -PathType Container)) {
    throw "BricsCadDir does not exist as a directory: $canonicalDir"
}

$directoryItem = Get-Item -LiteralPath $canonicalDir -Force
if (($directoryItem.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
    throw 'BricsCadDir must be an ordinary non-reparse directory.'
}

$requiredNames = @('bricscad.exe', 'BrxMgd.dll', 'TD_Mgd.dll', 'TD_MgdBrep.dll')
$states = @{}
foreach ($name in $requiredNames) {
    $path = Join-Path $canonicalDir $name
    $states[$name] = Get-StableHostFileState -Path $path -Label $name
}

$versionFile = Assert-StableHostFileState -Expected $states['bricscad.exe'] -Label 'bricscad.exe'
$version = $versionFile.VersionInfo
$null = Assert-StableHostFileState -Expected $states['bricscad.exe'] -Label 'bricscad.exe'
if ($version.FileMajorPart -ne 26) {
    throw "BricsCadDir is not BricsCAD V26. Detected $($version.FileVersion)."
}

if (-not [string]::IsNullOrWhiteSpace($VerifyStatePath)) {
    $stateText = Read-BoundedStrictUtf8 -Path $VerifyStatePath -Label 'V26 host reference state'
    try {
        $expectedState = $stateText | ConvertFrom-Json
    }
    catch {
        throw "V26 host reference state is not valid JSON: $($_.Exception.Message)"
    }
    if ([int]$expectedState.Version -ne 1 -or
        -not [string]::Equals([string]$expectedState.BricsCadDir, $canonicalDir, [StringComparison]::OrdinalIgnoreCase)) {
        throw 'V26 host reference state identity does not match the configured BricsCadDir.'
    }
    $expectedFiles = @($expectedState.Files)
    if ($expectedFiles.Count -ne $requiredNames.Count) {
        throw 'V26 host reference state does not contain exactly the required host files.'
    }
    foreach ($name in $requiredNames) {
        $matches = @($expectedFiles | Where-Object { [string]::Equals([string]$_.Name, $name, [StringComparison]::Ordinal) })
        if ($matches.Count -ne 1) {
            throw "V26 host reference state must contain exactly one record for $name."
        }
        $record = $matches[0]
        $currentState = $states[$name]
        if (-not [string]::Equals([string]$record.Path, [string]$currentState.Path, [StringComparison]::OrdinalIgnoreCase) -or
            [long]$record.Length -ne [long]$currentState.Length -or
            [long]$record.LastWriteUtcTicks -ne [long]$currentState.LastWriteUtcTicks -or
            -not [string]::Equals([string]$record.Sha256, [string]$currentState.Sha256, [StringComparison]::Ordinal)) {
            throw "$name no longer matches the admitted V26 host reference generation."
        }
        $null = Assert-StableHostFileState -Expected $currentState -Label $name
    }
}
elseif (-not [string]::IsNullOrWhiteSpace($StatePath)) {
    $fileRecords = foreach ($name in $requiredNames) {
        [pscustomobject]@{
            Name = $name
            Path = [string]$states[$name].Path
            Length = [long]$states[$name].Length
            LastWriteUtcTicks = [long]$states[$name].LastWriteTimeUtc.Ticks
            Sha256 = [string]$states[$name].Sha256
        }
    }
    Write-StateFile -Path $StatePath -State ([pscustomobject]@{
        Version = 1
        BricsCadDir = $canonicalDir
        Files = @($fileRecords)
    })
}

Write-Output $canonicalDir