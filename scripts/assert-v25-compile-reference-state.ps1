[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$StatePath,
    [Parameter(Mandatory = $true)][string]$BricsCadDir
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$requiredNames = @('BrxMgd.dll', 'TD_Mgd.dll', 'TD_MgdBrep.dll')
$maxStateBytes = 32768

function Get-CanonicalAbsolutePath {
    param([Parameter(Mandatory = $true)][string]$Path)

    $full = [IO.Path]::GetFullPath($Path)
    $root = [IO.Path]::GetPathRoot($full)
    if ([string]::IsNullOrWhiteSpace($root)) { throw "Path has no filesystem root: $Path" }
    if ($full.Length -gt $root.Length) {
        return $full.TrimEnd([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar)
    }
    return $full
}

function Assert-NoExistingReparseComponent {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$Label
    )

    $canonical = Get-CanonicalAbsolutePath -Path $Path
    $root = [IO.Path]::GetPathRoot($canonical)
    $relative = $canonical.Substring($root.Length)
    $current = $root
    foreach ($segment in @($relative -split '[\\/]' | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })) {
        $current = Join-Path $current $segment
        if (-not (Test-Path -LiteralPath $current)) { break }
        $item = Get-Item -LiteralPath $current -Force
        if (($item.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
            throw "$Label must not traverse a filesystem reparse point: $current"
        }
    }
}

function Assert-OrdinaryFile {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$Label
    )

    Assert-NoExistingReparseComponent -Path $Path -Label $Label
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        throw "$Label is missing: $Path"
    }
    $item = Get-Item -LiteralPath $Path -Force
    if (($item.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
        throw "$Label must be an ordinary non-reparse file: $Path"
    }
    return $item
}

function Get-StreamingSha256 {
    param([Parameter(Mandatory = $true)][string]$Path)

    $stream = [IO.File]::Open($Path, [IO.FileMode]::Open, [IO.FileAccess]::Read, [IO.FileShare]::Read)
    try {
        $sha = [Security.Cryptography.SHA256]::Create()
        try {
            $bytes = $sha.ComputeHash($stream)
            return ([BitConverter]::ToString($bytes)).Replace('-', '').ToUpperInvariant()
        }
        finally { $sha.Dispose() }
    }
    finally { $stream.Dispose() }
}

function Get-ByteArraySha256 {
    param([Parameter(Mandatory = $true)][byte[]]$Bytes)

    $sha = [Security.Cryptography.SHA256]::Create()
    try {
        $hashBytes = $sha.ComputeHash($Bytes)
        return ([BitConverter]::ToString($hashBytes)).Replace('-', '').ToUpperInvariant()
    }
    finally { $sha.Dispose() }
}

function Get-CurrentStableState {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [string]$Label = 'V25 compile reference'
    )

    $first = Assert-OrdinaryFile -Path $Path -Label $Label
    $firstPath = Get-CanonicalAbsolutePath -Path $first.FullName
    $length = [int64]$first.Length
    $ticks = [int64]$first.LastWriteTimeUtc.Ticks
    $hash = Get-StreamingSha256 -Path $firstPath

    $second = Assert-OrdinaryFile -Path $Path -Label $Label
    $secondPath = Get-CanonicalAbsolutePath -Path $second.FullName
    $secondHash = Get-StreamingSha256 -Path $secondPath
    if (-not [string]::Equals($firstPath, $secondPath, [StringComparison]::OrdinalIgnoreCase) -or
        $length -ne [int64]$second.Length -or
        $ticks -ne [int64]$second.LastWriteTimeUtc.Ticks -or
        -not [string]::Equals($hash, $secondHash, [StringComparison]::Ordinal)) {
        throw "$Label changed while its current generation was being verified: $Path"
    }

    return [pscustomobject]@{
        path = $secondPath
        length = $length
        lastWriteUtcTicks = $ticks
        sha256 = $hash
    }
}

function Get-JsonPropertyOccurrenceCount {
    param(
        [Parameter(Mandatory = $true)][string]$JsonText,
        [Parameter(Mandatory = $true)][string]$PropertyName
    )

    $firstNonWhitespace = 0
    while ($firstNonWhitespace -lt $JsonText.Length -and [char]::IsWhiteSpace($JsonText[$firstNonWhitespace])) { $firstNonWhitespace++ }
    if ($firstNonWhitespace -ge $JsonText.Length -or $JsonText[$firstNonWhitespace] -ne '{') {
        throw 'V25 compile-reference state identity object must have a top-level object.'
    }

    $count = 0
    $objectDepth = 0
    $arrayDepth = 0
    $i = 0
    while ($i -lt $JsonText.Length) {
        $ch = $JsonText[$i]
        if ($ch -eq '"') {
            $tokenStart = $i
            $i++
            $closed = $false
            while ($i -lt $JsonText.Length) {
                if ($JsonText[$i] -eq '\') { $i += 2; continue }
                if ($JsonText[$i] -eq '"') { $closed = $true; break }
                $i++
            }
            if (-not $closed) { throw 'V25 compile-reference state contains an unterminated string token.' }
            $tokenEnd = $i
            $lookahead = $tokenEnd + 1
            while ($lookahead -lt $JsonText.Length -and [char]::IsWhiteSpace($JsonText[$lookahead])) { $lookahead++ }
            if ($objectDepth -eq 1 -and $arrayDepth -eq 0 -and $lookahead -lt $JsonText.Length -and $JsonText[$lookahead] -eq ':') {
                $rawPropertyToken = $JsonText.Substring($tokenStart, $tokenEnd - $tokenStart + 1)
                try { $decodedPropertyName = [string]($rawPropertyToken | ConvertFrom-Json -ErrorAction Stop) }
                catch { throw 'V25 compile-reference state contains malformed property-name encoding.' }
                if ([string]::Equals($decodedPropertyName, $PropertyName, [StringComparison]::OrdinalIgnoreCase)) { $count++ }
            }
            $i = $tokenEnd + 1
            continue
        }

        switch ($ch) {
            '{' { $objectDepth++ }
            '}' { $objectDepth--; if ($objectDepth -lt 0) { throw 'V25 compile-reference state has invalid object nesting.' } }
            '[' { $arrayDepth++ }
            ']' { $arrayDepth--; if ($arrayDepth -lt 0) { throw 'V25 compile-reference state has invalid array nesting.' } }
        }
        $i++
    }
    if ($objectDepth -ne 0 -or $arrayDepth -ne 0) { throw 'V25 compile-reference state has unbalanced container nesting.' }
    return $count
}

function Assert-JsonPropertyOccursExactlyOnce {
    param(
        [Parameter(Mandatory = $true)][string]$JsonText,
        [Parameter(Mandatory = $true)][string]$PropertyName,
        [Parameter(Mandatory = $true)][string]$Label
    )

    $count = Get-JsonPropertyOccurrenceCount -JsonText $JsonText -PropertyName $PropertyName
    if ($count -ne 1) { throw "$Label must contain exactly one top-level '$PropertyName' property; found $count." }
}

function Get-JsonTopLevelArrayObjectTexts {
    param(
        [Parameter(Mandatory = $true)][string]$JsonText,
        [Parameter(Mandatory = $true)][string]$ArrayPropertyName,
        [Parameter(Mandatory = $true)][string]$Label
    )

    $firstNonWhitespace = 0
    while ($firstNonWhitespace -lt $JsonText.Length -and [char]::IsWhiteSpace($JsonText[$firstNonWhitespace])) { $firstNonWhitespace++ }
    if ($firstNonWhitespace -ge $JsonText.Length -or $JsonText[$firstNonWhitespace] -ne '{') { throw "$Label must have a top-level object." }

    $objectDepth = 0
    $arrayDepth = 0
    $arrayStart = -1
    $i = 0
    while ($i -lt $JsonText.Length -and $arrayStart -lt 0) {
        $ch = $JsonText[$i]
        if ($ch -eq '"') {
            $tokenStart = $i
            $i++
            $closed = $false
            while ($i -lt $JsonText.Length) {
                if ($JsonText[$i] -eq '\') { $i += 2; continue }
                if ($JsonText[$i] -eq '"') { $closed = $true; break }
                $i++
            }
            if (-not $closed) { throw "$Label contains an unterminated string token." }
            $tokenEnd = $i
            $lookahead = $tokenEnd + 1
            while ($lookahead -lt $JsonText.Length -and [char]::IsWhiteSpace($JsonText[$lookahead])) { $lookahead++ }
            if ($objectDepth -eq 1 -and $arrayDepth -eq 0 -and $lookahead -lt $JsonText.Length -and $JsonText[$lookahead] -eq ':') {
                $rawPropertyToken = $JsonText.Substring($tokenStart, $tokenEnd - $tokenStart + 1)
                try { $decodedPropertyName = [string]($rawPropertyToken | ConvertFrom-Json -ErrorAction Stop) }
                catch { throw "$Label contains malformed property-name encoding." }
                if ([string]::Equals($decodedPropertyName, $ArrayPropertyName, [StringComparison]::OrdinalIgnoreCase)) {
                    $valueStart = $lookahead + 1
                    while ($valueStart -lt $JsonText.Length -and [char]::IsWhiteSpace($JsonText[$valueStart])) { $valueStart++ }
                    if ($valueStart -ge $JsonText.Length -or $JsonText[$valueStart] -ne '[') { throw "$Label property '$ArrayPropertyName' must be an array." }
                    $arrayStart = $valueStart
                    break
                }
            }
            $i = $tokenEnd + 1
            continue
        }
        switch ($ch) {
            '{' { $objectDepth++ }
            '}' { $objectDepth-- }
            '[' { $arrayDepth++ }
            ']' { $arrayDepth-- }
        }
        $i++
    }
    if ($arrayStart -lt 0) { throw "$Label is missing top-level array property '$ArrayPropertyName'." }

    $items = [Collections.Generic.List[string]]::new()
    $arrayDepth = 0
    $objectDepth = 0
    $objectStart = -1
    $i = $arrayStart
    while ($i -lt $JsonText.Length) {
        $ch = $JsonText[$i]
        if ($ch -eq '"') {
            $i++
            $closed = $false
            while ($i -lt $JsonText.Length) {
                if ($JsonText[$i] -eq '\') { $i += 2; continue }
                if ($JsonText[$i] -eq '"') { $closed = $true; break }
                $i++
            }
            if (-not $closed) { throw "$Label contains an unterminated string token in '$ArrayPropertyName'." }
            $i++
            continue
        }
        switch ($ch) {
            '[' { $arrayDepth++ }
            ']' {
                if ($arrayDepth -eq 1 -and $objectDepth -eq 0) { return @($items) }
                $arrayDepth--
                if ($arrayDepth -lt 0) { throw "$Label has invalid array nesting in '$ArrayPropertyName'." }
            }
            '{' {
                if ($arrayDepth -eq 1 -and $objectDepth -eq 0) { $objectStart = $i }
                $objectDepth++
            }
            '}' {
                $objectDepth--
                if ($objectDepth -lt 0) { throw "$Label has invalid object nesting in '$ArrayPropertyName'." }
                if ($arrayDepth -eq 1 -and $objectDepth -eq 0 -and $objectStart -ge 0) {
                    $items.Add($JsonText.Substring($objectStart, $i - $objectStart + 1))
                    $objectStart = -1
                }
            }
        }
        $i++
    }
    throw "$Label array property '$ArrayPropertyName' is unterminated."
}

Assert-NoExistingReparseComponent -Path $StatePath -Label 'V25 compile-reference state path'
Assert-NoExistingReparseComponent -Path $BricsCadDir -Label 'V25 compile-reference snapshot directory'
$stateBefore = Get-CurrentStableState -Path $StatePath -Label 'V25 compile-reference state'
if ($stateBefore.length -le 0 -or $stateBefore.length -gt $maxStateBytes) {
    throw "V25 compile-reference state size is outside the accepted bound: $($stateBefore.length) bytes."
}
$rawBytes = [IO.File]::ReadAllBytes($stateBefore.path)
if ($rawBytes.Length -ne $stateBefore.length -or $rawBytes.Length -gt $maxStateBytes) {
    throw 'V25 compile-reference state changed size while it was being materialized.'
}
$materializedHash = Get-ByteArraySha256 -Bytes $rawBytes
$stateAfter = Get-CurrentStableState -Path $StatePath -Label 'V25 compile-reference state'
if (-not [string]::Equals($stateBefore.path, $stateAfter.path, [StringComparison]::OrdinalIgnoreCase) -or
    $stateBefore.length -ne $stateAfter.length -or
    $stateBefore.lastWriteUtcTicks -ne $stateAfter.lastWriteUtcTicks -or
    -not [string]::Equals($stateBefore.sha256, $stateAfter.sha256, [StringComparison]::Ordinal) -or
    -not [string]::Equals($materializedHash, $stateBefore.sha256, [StringComparison]::Ordinal)) {
    throw 'V25 compile-reference state changed while it was being materialized.'
}

$utf8 = New-Object Text.UTF8Encoding($false, $true)
try { $raw = $utf8.GetString($rawBytes) }
catch { throw "V25 compile-reference state is not strict UTF-8: $($_.Exception.Message)" }

foreach ($propertyName in @('schemaVersion', 'bricsCadDir', 'references')) {
    Assert-JsonPropertyOccursExactlyOnce -JsonText $raw -PropertyName $propertyName -Label 'V25 compile-reference state'
}
$rawReferenceRecords = @(Get-JsonTopLevelArrayObjectTexts -JsonText $raw -ArrayPropertyName 'references' -Label 'V25 compile-reference state')
if ($rawReferenceRecords.Count -ne $requiredNames.Count) {
    throw "V25 compile-reference state must contain exactly $($requiredNames.Count) reference objects."
}
for ($recordIndex = 0; $recordIndex -lt $rawReferenceRecords.Count; $recordIndex++) {
    foreach ($propertyName in @('name', 'path', 'length', 'lastWriteUtcTicks', 'sha256')) {
        Assert-JsonPropertyOccursExactlyOnce -JsonText $rawReferenceRecords[$recordIndex] -PropertyName $propertyName -Label "V25 compile-reference state references[$recordIndex]"
    }
}

try { $state = $raw | ConvertFrom-Json }
catch { throw "V25 compile-reference state is invalid JSON: $($_.Exception.Message)" }

if ([int]$state.schemaVersion -ne 1) {
    throw "Unsupported V25 compile-reference state schemaVersion: $($state.schemaVersion)"
}
$expectedDir = Get-CanonicalAbsolutePath -Path $BricsCadDir
if (-not [string]::Equals(([string]$state.bricsCadDir), $expectedDir, [StringComparison]::OrdinalIgnoreCase)) {
    throw "V25 compile-reference state directory mismatch. Expected $expectedDir, got $($state.bricsCadDir)."
}

$entries = @($state.references)
if ($entries.Count -ne $requiredNames.Count) {
    throw "V25 compile-reference state must contain exactly $($requiredNames.Count) references."
}
foreach ($name in $requiredNames) {
    $matches = @($entries | Where-Object { [string]::Equals(([string]$_.name), $name, [StringComparison]::Ordinal) })
    if ($matches.Count -ne 1) { throw "V25 compile-reference state must contain exactly one entry for $name." }
    $expected = $matches[0]
    $path = Join-Path $expectedDir $name
    $current = Get-CurrentStableState -Path $path
    if (-not [string]::Equals(([string]$expected.path), $current.path, [StringComparison]::OrdinalIgnoreCase) -or
        [int64]$expected.length -ne $current.length -or
        [int64]$expected.lastWriteUtcTicks -ne $current.lastWriteUtcTicks -or
        -not [string]::Equals(([string]$expected.sha256).ToUpperInvariant(), $current.sha256, [StringComparison]::Ordinal)) {
        throw "V25 compile reference no longer matches its admitted generation: $name"
    }
}

Write-Host 'PASS: V25 compile references still match the exact admitted generations.'
