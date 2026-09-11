[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$ZipPath,

    [Parameter(Mandatory = $true)]
    [string]$DestinationRoot,

    [Parameter(Mandatory = $true)]
    [ValidateRange(1, [long]::MaxValue)]
    [long]$MaxPackageBytes,

    [Parameter(Mandatory = $true)]
    [ValidateRange(1, [long]::MaxValue)]
    [long]$MaxExpandedBytes,

    [Parameter(Mandatory = $true)]
    [ValidateRange(1, 20000)]
    [int]$MaxEntries
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.IO.Compression.FileSystem

function Assert-ExistingSafePathChain {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$BoundaryRoot
    )

    $pathFull = [IO.Path]::GetFullPath($Path).TrimEnd([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar)
    $boundaryFull = [IO.Path]::GetFullPath($BoundaryRoot).TrimEnd([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar)
    $rootPrefix = $boundaryFull + [IO.Path]::DirectorySeparatorChar
    if (-not [string]::Equals($pathFull, $boundaryFull, [StringComparison]::OrdinalIgnoreCase) -and
        -not $pathFull.StartsWith($rootPrefix, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Commercial archive extraction path escaped the destination root: $pathFull"
    }

    $cursor = Get-Item -LiteralPath $pathFull -Force -ErrorAction Stop
    $reachedBoundary = $false
    while ($null -ne $cursor) {
        $cursorFull = [IO.Path]::GetFullPath([string]$cursor.FullName).TrimEnd([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar)
        $isBoundary = [string]::Equals($cursorFull, $boundaryFull, [StringComparison]::OrdinalIgnoreCase)
        if (-not $isBoundary -and -not $cursorFull.StartsWith($rootPrefix, [StringComparison]::OrdinalIgnoreCase)) {
            throw "Commercial archive extraction path escaped the destination root: $cursorFull"
        }
        if (($cursor.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
            throw "Commercial archive extraction traverses a reparse point: $cursorFull"
        }
        if ($isBoundary) {
            $reachedBoundary = $true
            break
        }
        $cursor = $cursor.Parent
    }
    if (-not $reachedBoundary) {
        throw "Commercial archive extraction path does not resolve through the destination root: $pathFull"
    }
}

function Ensure-SafeDirectory {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$BoundaryRoot
    )

    $boundaryFull = [IO.Path]::GetFullPath($BoundaryRoot).TrimEnd([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar)
    $rootPrefix = $boundaryFull + [IO.Path]::DirectorySeparatorChar
    $pathFull = [IO.Path]::GetFullPath($Path).TrimEnd([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar)
    if (-not [string]::Equals($pathFull, $boundaryFull, [StringComparison]::OrdinalIgnoreCase) -and
        -not $pathFull.StartsWith($rootPrefix, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Commercial archive extraction directory escaped the destination root: $pathFull"
    }

    Assert-ExistingSafePathChain -Path $boundaryFull -BoundaryRoot $boundaryFull
    if ([string]::Equals($pathFull, $boundaryFull, [StringComparison]::OrdinalIgnoreCase)) { return }

    $relative = $pathFull.Substring($rootPrefix.Length)
    $current = $boundaryFull
    foreach ($segment in $relative.Split([IO.Path]::DirectorySeparatorChar)) {
        if ([string]::IsNullOrWhiteSpace($segment)) {
            throw "Commercial archive extraction directory has an empty path segment: $pathFull"
        }
        Assert-ExistingSafePathChain -Path $current -BoundaryRoot $boundaryFull
        $next = [IO.Path]::GetFullPath((Join-Path $current $segment))
        if (Test-Path -LiteralPath $next) {
            if (-not (Test-Path -LiteralPath $next -PathType Container)) {
                throw "Commercial archive extraction directory target exists as a non-directory: $next"
            }
        }
        else {
            [IO.Directory]::CreateDirectory($next) | Out-Null
        }
        Assert-ExistingSafePathChain -Path $next -BoundaryRoot $boundaryFull
        $current = $next
    }
}

$zipFull = (Resolve-Path -LiteralPath $ZipPath -ErrorAction Stop).Path
if (-not (Test-Path -LiteralPath $zipFull -PathType Leaf)) {
    throw "Commercial candidate archive is not a file: $zipFull"
}

$destinationFull = [IO.Path]::GetFullPath($DestinationRoot).TrimEnd([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar)
if (Test-Path -LiteralPath $destinationFull) {
    throw "Commercial candidate extraction destination must not already exist: $destinationFull"
}
$destinationParent = [IO.Path]::GetDirectoryName($destinationFull)
if ([string]::IsNullOrWhiteSpace($destinationParent) -or -not (Test-Path -LiteralPath $destinationParent -PathType Container)) {
    throw "Commercial candidate extraction destination parent does not exist: $destinationParent"
}
$parentItem = Get-Item -LiteralPath $destinationParent -Force -ErrorAction Stop
if (($parentItem.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
    throw "Commercial candidate extraction destination parent is a reparse point: $destinationParent"
}
[IO.Directory]::CreateDirectory($destinationFull) | Out-Null
Assert-ExistingSafePathChain -Path $destinationFull -BoundaryRoot $destinationFull

$zipStream = [IO.File]::Open($zipFull, [IO.FileMode]::Open, [IO.FileAccess]::Read, [IO.FileShare]::Read)
try {
    if ($zipStream.Length -le 0 -or $zipStream.Length -gt $MaxPackageBytes) {
        throw "Commercial candidate archive size $($zipStream.Length) bytes is outside the allowed range (max $MaxPackageBytes)."
    }

    $archive = [IO.Compression.ZipArchive]::new($zipStream, [IO.Compression.ZipArchiveMode]::Read, $true)
    try {
        $rootPrefix = $destinationFull + [IO.Path]::DirectorySeparatorChar
        $seenTargets = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
        $fileRelatives = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
        $records = [Collections.Generic.List[object]]::new()
        $invalidFileNameChars = [IO.Path]::GetInvalidFileNameChars()
        [long]$expandedBytes = 0
        $entryCount = 0

        foreach ($entry in $archive.Entries) {
            $entryCount++
            if ($entryCount -gt $MaxEntries) {
                throw "Commercial candidate archive exceeds the allowed entry count ($MaxEntries)."
            }

            $name = [string]$entry.FullName
            if ([string]::IsNullOrWhiteSpace($name) -or $name.IndexOf([char]0) -ge 0 -or
                [IO.Path]::IsPathRooted($name) -or $name.Contains('\') -or $name.Contains(':')) {
                throw "Unsafe commercial candidate archive entry: $name"
            }

            $isDirectory = $name.EndsWith('/', [StringComparison]::Ordinal)
            $relative = $name.TrimEnd('/')
            if ([string]::IsNullOrWhiteSpace($relative)) {
                throw "Unsafe commercial candidate archive entry: $name"
            }
            $segments = @($relative.Split('/'))
            if ($segments.Count -eq 0) {
                throw "Unsafe commercial candidate archive entry: $name"
            }
            foreach ($segment in $segments) {
                if ([string]::IsNullOrWhiteSpace($segment) -or $segment -eq '.' -or $segment -eq '..' -or
                    $segment.IndexOfAny($invalidFileNameChars) -ge 0 -or
                    $segment.EndsWith('.', [StringComparison]::Ordinal) -or
                    $segment.EndsWith(' ', [StringComparison]::Ordinal) -or
                    $segment -match '^(?i:con|prn|aux|nul|com[1-9]|lpt[1-9])(?:\.|$)') {
                    throw "Unsafe commercial candidate archive entry segment '$segment' in '$name'."
                }
            }

            if (-not $isDirectory) {
                if ($entry.Length -lt 0 -or $entry.Length -gt $MaxExpandedBytes -or $expandedBytes -gt ($MaxExpandedBytes - [long]$entry.Length)) {
                    throw "Commercial candidate archive exceeds the allowed expanded size ($MaxExpandedBytes bytes)."
                }
                $expandedBytes += [int64]$entry.Length
                if ($expandedBytes -gt $MaxExpandedBytes) {
                    throw "Commercial candidate archive exceeds the allowed expanded size ($MaxExpandedBytes bytes)."
                }
            }

            $normalizedRelative = $segments -join '/'
            for ($i = 1; $i -lt $segments.Count; $i++) {
                $parentRelative = ($segments[0..($i - 1)] -join '/')
                if ($fileRelatives.Contains($parentRelative)) {
                    throw "Commercial candidate archive entry traverses an entry already admitted as a file: $name"
                }
            }
            if (-not $isDirectory) {
                $childPrefix = $normalizedRelative + '/'
                foreach ($existing in $records) {
                    if ([string]$existing.Relative -like "$childPrefix*") {
                        throw "Commercial candidate archive file conflicts with an already admitted child path: $name"
                    }
                }
                if (-not $fileRelatives.Add($normalizedRelative)) {
                    throw "Duplicate commercial candidate archive file entry: $name"
                }
            }

            $target = [IO.Path]::GetFullPath((Join-Path $destinationFull ($normalizedRelative.Replace('/', [IO.Path]::DirectorySeparatorChar))))
            if (-not $target.StartsWith($rootPrefix, [StringComparison]::OrdinalIgnoreCase)) {
                throw "Commercial candidate archive entry escaped the destination root: $name"
            }
            if (-not $seenTargets.Add($target)) {
                throw "Duplicate or case-aliased commercial candidate archive target: $name"
            }

            $records.Add([pscustomobject]@{
                Entry = $entry
                Name = $name
                Relative = $normalizedRelative
                Target = $target
                IsDirectory = $isDirectory
            })
        }

        if ($entryCount -eq 0) {
            throw 'Commercial candidate archive contains no entries.'
        }

        foreach ($record in $records) {
            if ($record.IsDirectory) {
                Ensure-SafeDirectory -Path $record.Target -BoundaryRoot $destinationFull
                continue
            }

            $parent = [IO.Path]::GetDirectoryName([string]$record.Target)
            Ensure-SafeDirectory -Path $parent -BoundaryRoot $destinationFull
            Assert-ExistingSafePathChain -Path $parent -BoundaryRoot $destinationFull
            $input = $record.Entry.Open()
            $output = $null
            try {
                $output = [IO.File]::Open([string]$record.Target, [IO.FileMode]::CreateNew, [IO.FileAccess]::Write, [IO.FileShare]::None)
                $input.CopyTo($output)
                $output.Flush()
            }
            finally {
                if ($output) { $output.Dispose() }
                $input.Dispose()
            }
            $written = Get-Item -LiteralPath ([string]$record.Target) -Force -ErrorAction Stop
            if (($written.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0 -or [long]$written.Length -ne [long]$record.Entry.Length) {
                throw "Commercial candidate archive entry did not materialize as the exact admitted ordinary file: $($record.Name)"
            }
            Assert-ExistingSafePathChain -Path $parent -BoundaryRoot $destinationFull
        }
    }
    finally {
        $archive.Dispose()
    }
}
catch {
    if (Test-Path -LiteralPath $destinationFull) {
        Remove-Item -LiteralPath $destinationFull -Recurse -Force -ErrorAction SilentlyContinue
    }
    throw
}
finally {
    $zipStream.Dispose()
}

Write-Host "Safely extracted V25 commercial candidate archive: entries=$entryCount expandedBytes=$expandedBytes destination=$destinationFull"
