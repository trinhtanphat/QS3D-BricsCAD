$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
Add-Type -AssemblyName System.IO.Compression.FileSystem

$extractor = Join-Path $PSScriptRoot 'expand-v25-commercial-candidate.ps1'
$heldVerifier = Join-Path $PSScriptRoot 'verify-v25-held-file.ps1'
foreach ($required in @($extractor, $heldVerifier)) {
    if (-not (Test-Path -LiteralPath $required -PathType Leaf)) { throw "Required V25 commercial archive test helper was not found: $required" }
}

function New-TestArchive {
    param([Parameter(Mandatory = $true)][string]$Path,[Parameter(Mandatory = $true)][object[]]$Entries)
    Remove-Item -LiteralPath $Path -Force -ErrorAction SilentlyContinue
    $archive = [IO.Compression.ZipFile]::Open($Path, [IO.Compression.ZipArchiveMode]::Create)
    try {
        foreach ($spec in $Entries) {
            $entry = $archive.CreateEntry([string]$spec.Name, [IO.Compression.CompressionLevel]::Optimal)
            $stream = $entry.Open()
            try {
                [byte[]]$bytes = if ($spec.ContainsKey('Bytes')) { [byte[]]$spec.Bytes } else { [Text.Encoding]::UTF8.GetBytes([string]$spec.Text) }
                if ($bytes.Length -gt 0) { $stream.Write($bytes, 0, $bytes.Length) }
            }
            finally { $stream.Dispose() }
        }
    }
    finally { $archive.Dispose() }
}

function Admit-TestZip {
    param([Parameter(Mandatory = $true)][string]$ZipPath)
    $digest = (& $heldVerifier -Operation Hash -Path $ZipPath).Trim().ToLowerInvariant()
    if ($digest -notmatch '^[0-9a-f]{64}$') { throw "Held verifier returned malformed test ZIP digest: $digest" }
    if ([string]::Equals([IO.Path]::GetFileName($ZipPath), 'QS3D-BricsCAD-V25.zip', [StringComparison]::Ordinal)) {
        if (-not [string]::Equals($env:QS3D_V25_COMMERCIAL_ZIP_SHA256, $digest, [StringComparison]::OrdinalIgnoreCase)) {
            throw 'Held verifier did not publish the exact production-named ZIP digest for extraction.'
        }
    }
    else {
        # Adversarial fixtures intentionally use descriptive filenames. Bind each exact fixture digest
        # directly so path-validation failures are tested after generation admission, without broadening
        # the production helper's exact QS3D-BricsCAD-V25.zip publication scope.
        $env:QS3D_V25_COMMERCIAL_ZIP_SHA256 = $digest
    }
    return $digest
}

function Invoke-SafeExtract {
    param(
        [Parameter(Mandatory = $true)][string]$ZipPath,
        [Parameter(Mandatory = $true)][string]$Destination,
        [long]$MaxPackageBytes = 1048576,
        [long]$MaxExpandedBytes = 1048576,
        [int]$MaxEntries = 32,
        [switch]$SkipAdmission
    )
    if (-not $SkipAdmission) { Admit-TestZip -ZipPath $ZipPath | Out-Null }
    & $extractor -ZipPath $ZipPath -DestinationRoot $Destination -MaxPackageBytes $MaxPackageBytes -MaxExpandedBytes $MaxExpandedBytes -MaxEntries $MaxEntries | Out-Null
}

function Assert-Rejected {
    param([Parameter(Mandatory = $true)][string]$Label,[Parameter(Mandatory = $true)][scriptblock]$Action,[string]$ExpectedMessage)
    $rejected = $false
    try { & $Action }
    catch {
        $rejected = $true
        $message = $_.Exception.Message
        Write-Host "Expected safe-extraction rejection [$Label]: $message"
        if (-not [string]::IsNullOrWhiteSpace($ExpectedMessage) -and $message.IndexOf($ExpectedMessage, [StringComparison]::OrdinalIgnoreCase) -lt 0) {
            throw "Safe extractor rejected [$Label] for the wrong reason. Expected '$ExpectedMessage', got '$message'."
        }
    }
    if (-not $rejected) { throw "Safe extractor accepted an invalid archive fixture: $Label" }
}

$tempRoot = Join-Path ([IO.Path]::GetTempPath()) ('qs3d-v25-commercial-safe-extract-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $tempRoot -Force | Out-Null
try {
    $validZip = Join-Path $tempRoot 'QS3D-BricsCAD-V25.zip'
    New-TestArchive -Path $validZip -Entries @(@{ Name='payload/file.txt'; Text='verified commercial payload' },@{ Name='payload/data.bin'; Bytes=[byte[]](0,1,2,3,254,255) })

    $admitted = Admit-TestZip -ZipPath $validZip
    $unrelated = Join-Path $tempRoot 'QS3D-BricsCAD-V25.update.json'
    Set-Content -LiteralPath $unrelated -Value '{"schemaVersion":1}' -Encoding ASCII
    & $heldVerifier -Operation Hash -Path $unrelated | Out-Null
    if (-not [string]::Equals($env:QS3D_V25_COMMERCIAL_ZIP_SHA256, $admitted, [StringComparison]::OrdinalIgnoreCase)) {
        throw 'Hashing an unrelated release asset overwrote the admitted commercial ZIP digest.'
    }

    $env:QS3D_V25_COMMERCIAL_ZIP_SHA256 = ('0' * 64)
    $digestMismatchDestination = Join-Path $tempRoot 'digest-mismatch-output'
    Assert-Rejected -Label 'exact parsed generation digest mismatch' -ExpectedMessage 'generation changed between admission and exact-stream extraction' -Action {
        Invoke-SafeExtract -ZipPath $validZip -Destination $digestMismatchDestination -SkipAdmission
    }
    if (Test-Path -LiteralPath $digestMismatchDestination) { throw 'Digest mismatch left a partial extraction destination.' }

    $validDestination = Join-Path $tempRoot 'valid-output'
    Invoke-SafeExtract -ZipPath $validZip -Destination $validDestination
    if ((Get-Content -LiteralPath (Join-Path $validDestination 'payload\file.txt') -Raw) -ne 'verified commercial payload') { throw 'Safe extractor changed valid text payload bytes.' }
    $actual = [IO.File]::ReadAllBytes((Join-Path $validDestination 'payload\data.bin'))
    if (($actual -join ',') -cne '0,1,2,3,254,255') { throw 'Safe extractor changed valid binary payload bytes.' }

    $traversalZip = Join-Path $tempRoot 'traversal.zip'; New-TestArchive $traversalZip @(@{Name='../escape.txt';Text='escape'})
    Assert-Rejected 'parent traversal' { Invoke-SafeExtract $traversalZip (Join-Path $tempRoot 'traversal-output') } 'Unsafe commercial candidate archive entry segment'
    if (Test-Path -LiteralPath (Join-Path $tempRoot 'escape.txt')) { throw 'Traversal fixture wrote outside extraction root.' }

    $caseZip = Join-Path $tempRoot 'case-alias.zip'; New-TestArchive $caseZip @(@{Name='Payload.txt';Text='one'},@{Name='payload.TXT';Text='two'})
    Assert-Rejected 'case alias' { Invoke-SafeExtract $caseZip (Join-Path $tempRoot 'case-output') } 'Duplicate commercial candidate archive file entry'

    $deviceZip = Join-Path $tempRoot 'device-name.zip'; New-TestArchive $deviceZip @(@{Name='COM¹.txt';Text='device'})
    Assert-Rejected 'superscript Windows device name' { Invoke-SafeExtract $deviceZip (Join-Path $tempRoot 'device-output') } 'Unsafe commercial candidate archive entry segment'

    $backslashZip = Join-Path $tempRoot 'backslash.zip'; New-TestArchive $backslashZip @(@{Name='nested\escape.txt';Text='backslash'})
    Assert-Rejected 'backslash path ambiguity' { Invoke-SafeExtract $backslashZip (Join-Path $tempRoot 'backslash-output') } 'Unsafe commercial candidate archive entry'

    $adsZip = Join-Path $tempRoot 'ads.zip'; New-TestArchive $adsZip @(@{Name='payload.txt:stream';Text='ads'})
    Assert-Rejected 'alternate data stream separator' { Invoke-SafeExtract $adsZip (Join-Path $tempRoot 'ads-output') } 'Unsafe commercial candidate archive entry'

    $trailingDotZip = Join-Path $tempRoot 'trailing-dot.zip'; New-TestArchive $trailingDotZip @(@{Name='payload./file.txt';Text='dot'})
    Assert-Rejected 'trailing-dot directory segment' { Invoke-SafeExtract $trailingDotZip (Join-Path $tempRoot 'trailing-dot-output') } 'Unsafe commercial candidate archive entry segment'

    $fileChildZip = Join-Path $tempRoot 'file-child.zip'; New-TestArchive $fileChildZip @(@{Name='node';Text='file'},@{Name='node/child.txt';Text='child'})
    Assert-Rejected 'file-child collision' { Invoke-SafeExtract $fileChildZip (Join-Path $tempRoot 'file-child-output') } 'traverses an entry already admitted as a file'

    $entryCountZip = Join-Path $tempRoot 'entry-count.zip'; New-TestArchive $entryCountZip @(@{Name='one.txt';Text='1'},@{Name='two.txt';Text='2'})
    Assert-Rejected 'entry-count budget' { Invoke-SafeExtract -ZipPath $entryCountZip -Destination (Join-Path $tempRoot 'entry-count-output') -MaxEntries 1 } 'entry count'

    $expandedZip = Join-Path $tempRoot 'expanded.zip'; New-TestArchive $expandedZip @(@{Name='large.txt';Text=('x' * 64)})
    Assert-Rejected 'expanded-byte budget' { Invoke-SafeExtract -ZipPath $expandedZip -Destination (Join-Path $tempRoot 'expanded-output') -MaxExpandedBytes 32 } 'expanded size'
    Assert-Rejected 'compressed-byte budget' { Invoke-SafeExtract -ZipPath $validZip -Destination (Join-Path $tempRoot 'compressed-output') -MaxPackageBytes 1 } 'outside the allowed range'

    $existingDestination = Join-Path $tempRoot 'existing-output'; New-Item -ItemType Directory -Path $existingDestination -Force | Out-Null
    Set-Content -LiteralPath (Join-Path $existingDestination 'sentinel.txt') -Value 'keep' -Encoding ASCII
    Assert-Rejected 'pre-existing destination' { Invoke-SafeExtract $validZip $existingDestination } 'must not already exist'
    if (-not (Test-Path -LiteralPath (Join-Path $existingDestination 'sentinel.txt') -PathType Leaf)) { throw 'Safe extractor modified a pre-existing destination.' }

    Write-Host 'V25 commercial safe archive extraction runtime tests passed.'
}
finally {
    Remove-Item Env:QS3D_V25_COMMERCIAL_ZIP_SHA256 -ErrorAction SilentlyContinue
    Remove-Item -LiteralPath $tempRoot -Recurse -Force -ErrorAction SilentlyContinue
}
