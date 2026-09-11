$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
Add-Type -AssemblyName System.IO.Compression.FileSystem

$extractor = Join-Path $PSScriptRoot 'expand-v25-commercial-candidate.ps1'
if (-not (Test-Path -LiteralPath $extractor -PathType Leaf)) {
    throw "V25 commercial safe extractor was not found: $extractor"
}

function New-TestArchive {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][object[]]$Entries
    )

    Remove-Item -LiteralPath $Path -Force -ErrorAction SilentlyContinue
    $archive = [IO.Compression.ZipFile]::Open($Path, [IO.Compression.ZipArchiveMode]::Create)
    try {
        foreach ($spec in $Entries) {
            $name = [string]$spec.Name
            $entry = $archive.CreateEntry($name, [IO.Compression.CompressionLevel]::Optimal)
            $stream = $entry.Open()
            try {
                $bytes = if ($spec.ContainsKey('Bytes')) {
                    [byte[]]$spec.Bytes
                }
                else {
                    [Text.Encoding]::UTF8.GetBytes([string]$spec.Text)
                }
                if ($bytes.Length -gt 0) {
                    $stream.Write($bytes, 0, $bytes.Length)
                }
            }
            finally { $stream.Dispose() }
        }
    }
    finally { $archive.Dispose() }
}

function Invoke-SafeExtract {
    param(
        [Parameter(Mandatory = $true)][string]$ZipPath,
        [Parameter(Mandatory = $true)][string]$Destination,
        [long]$MaxPackageBytes = 1048576,
        [long]$MaxExpandedBytes = 1048576,
        [int]$MaxEntries = 32
    )

    & $extractor `
        -ZipPath $ZipPath `
        -DestinationRoot $Destination `
        -MaxPackageBytes $MaxPackageBytes `
        -MaxExpandedBytes $MaxExpandedBytes `
        -MaxEntries $MaxEntries | Out-Null
}

function Assert-Rejected {
    param(
        [Parameter(Mandatory = $true)][string]$Label,
        [Parameter(Mandatory = $true)][scriptblock]$Action,
        [string]$ExpectedMessage
    )

    $rejected = $false
    try {
        & $Action
    }
    catch {
        $rejected = $true
        $message = $_.Exception.Message
        Write-Host "Expected safe-extraction rejection [$Label]: $message"
        if (-not [string]::IsNullOrWhiteSpace($ExpectedMessage) -and $message.IndexOf($ExpectedMessage, [StringComparison]::OrdinalIgnoreCase) -lt 0) {
            throw "Safe extractor rejected [$Label] for the wrong reason. Expected '$ExpectedMessage', got '$message'."
        }
    }
    if (-not $rejected) {
        throw "Safe extractor accepted an invalid archive fixture: $Label"
    }
}

$tempRoot = Join-Path ([IO.Path]::GetTempPath()) ('qs3d-v25-commercial-safe-extract-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $tempRoot -Force | Out-Null
try {
    $validZip = Join-Path $tempRoot 'valid.zip'
    New-TestArchive -Path $validZip -Entries @(
        @{ Name = 'payload/file.txt'; Text = 'verified commercial payload' },
        @{ Name = 'payload/data.bin'; Bytes = [byte[]](0, 1, 2, 3, 254, 255) }
    )
    $validDestination = Join-Path $tempRoot 'valid-output'
    Invoke-SafeExtract -ZipPath $validZip -Destination $validDestination
    $textPath = Join-Path $validDestination 'payload\file.txt'
    $dataPath = Join-Path $validDestination 'payload\data.bin'
    if ((Get-Content -LiteralPath $textPath -Raw) -ne 'verified commercial payload') {
        throw 'Safe extractor changed valid text payload bytes.'
    }
    if (-not ([IO.File]::ReadAllBytes($dataPath) -ceq [byte[]](0, 1, 2, 3, 254, 255))) {
        $actual = [IO.File]::ReadAllBytes($dataPath)
        if (($actual -join ',') -cne '0,1,2,3,254,255') {
            throw 'Safe extractor changed valid binary payload bytes.'
        }
    }

    $traversalZip = Join-Path $tempRoot 'traversal.zip'
    New-TestArchive -Path $traversalZip -Entries @(@{ Name = '../escape.txt'; Text = 'escape' })
    $traversalDestination = Join-Path $tempRoot 'traversal-output'
    Assert-Rejected -Label 'parent traversal' -ExpectedMessage 'Unsafe commercial candidate archive entry segment' -Action {
        Invoke-SafeExtract -ZipPath $traversalZip -Destination $traversalDestination
    }
    if (Test-Path -LiteralPath (Join-Path $tempRoot 'escape.txt')) {
        throw 'Traversal fixture wrote outside the extraction root.'
    }

    $caseZip = Join-Path $tempRoot 'case-alias.zip'
    New-TestArchive -Path $caseZip -Entries @(
        @{ Name = 'Payload.txt'; Text = 'one' },
        @{ Name = 'payload.TXT'; Text = 'two' }
    )
    Assert-Rejected -Label 'case alias' -ExpectedMessage 'Duplicate commercial candidate archive file entry' -Action {
        Invoke-SafeExtract -ZipPath $caseZip -Destination (Join-Path $tempRoot 'case-output')
    }

    $deviceZip = Join-Path $tempRoot 'device-name.zip'
    New-TestArchive -Path $deviceZip -Entries @(@{ Name = 'COM¹.txt'; Text = 'device' })
    Assert-Rejected -Label 'superscript Windows device name' -ExpectedMessage 'Unsafe commercial candidate archive entry segment' -Action {
        Invoke-SafeExtract -ZipPath $deviceZip -Destination (Join-Path $tempRoot 'device-output')
    }

    $backslashZip = Join-Path $tempRoot 'backslash.zip'
    New-TestArchive -Path $backslashZip -Entries @(@{ Name = 'nested\escape.txt'; Text = 'backslash' })
    Assert-Rejected -Label 'backslash path ambiguity' -ExpectedMessage 'Unsafe commercial candidate archive entry' -Action {
        Invoke-SafeExtract -ZipPath $backslashZip -Destination (Join-Path $tempRoot 'backslash-output')
    }

    $adsZip = Join-Path $tempRoot 'ads.zip'
    New-TestArchive -Path $adsZip -Entries @(@{ Name = 'payload.txt:stream'; Text = 'ads' })
    Assert-Rejected -Label 'alternate data stream separator' -ExpectedMessage 'Unsafe commercial candidate archive entry' -Action {
        Invoke-SafeExtract -ZipPath $adsZip -Destination (Join-Path $tempRoot 'ads-output')
    }

    $trailingDotZip = Join-Path $tempRoot 'trailing-dot.zip'
    New-TestArchive -Path $trailingDotZip -Entries @(@{ Name = 'payload./file.txt'; Text = 'dot' })
    Assert-Rejected -Label 'trailing-dot directory segment' -ExpectedMessage 'Unsafe commercial candidate archive entry segment' -Action {
        Invoke-SafeExtract -ZipPath $trailingDotZip -Destination (Join-Path $tempRoot 'trailing-dot-output')
    }

    $fileChildZip = Join-Path $tempRoot 'file-child.zip'
    New-TestArchive -Path $fileChildZip -Entries @(
        @{ Name = 'node'; Text = 'file' },
        @{ Name = 'node/child.txt'; Text = 'child' }
    )
    Assert-Rejected -Label 'file-child collision' -ExpectedMessage 'traverses an entry already admitted as a file' -Action {
        Invoke-SafeExtract -ZipPath $fileChildZip -Destination (Join-Path $tempRoot 'file-child-output')
    }

    $entryCountZip = Join-Path $tempRoot 'entry-count.zip'
    New-TestArchive -Path $entryCountZip -Entries @(
        @{ Name = 'one.txt'; Text = '1' },
        @{ Name = 'two.txt'; Text = '2' }
    )
    Assert-Rejected -Label 'entry-count budget' -ExpectedMessage 'entry count' -Action {
        Invoke-SafeExtract -ZipPath $entryCountZip -Destination (Join-Path $tempRoot 'entry-count-output') -MaxEntries 1
    }

    $expandedZip = Join-Path $tempRoot 'expanded.zip'
    New-TestArchive -Path $expandedZip -Entries @(@{ Name = 'large.txt'; Text = ('x' * 64) })
    Assert-Rejected -Label 'expanded-byte budget' -ExpectedMessage 'expanded size' -Action {
        Invoke-SafeExtract -ZipPath $expandedZip -Destination (Join-Path $tempRoot 'expanded-output') -MaxExpandedBytes 32
    }

    Assert-Rejected -Label 'compressed-byte budget' -ExpectedMessage 'outside the allowed range' -Action {
        Invoke-SafeExtract -ZipPath $validZip -Destination (Join-Path $tempRoot 'compressed-output') -MaxPackageBytes 1
    }

    $existingDestination = Join-Path $tempRoot 'existing-output'
    New-Item -ItemType Directory -Path $existingDestination -Force | Out-Null
    Set-Content -LiteralPath (Join-Path $existingDestination 'sentinel.txt') -Value 'keep' -Encoding ASCII
    Assert-Rejected -Label 'pre-existing destination' -ExpectedMessage 'must not already exist' -Action {
        Invoke-SafeExtract -ZipPath $validZip -Destination $existingDestination
    }
    if (-not (Test-Path -LiteralPath (Join-Path $existingDestination 'sentinel.txt') -PathType Leaf)) {
        throw 'Safe extractor modified a destination that existed before invocation.'
    }

    Write-Host 'V25 commercial safe archive extraction runtime tests passed.'
}
finally {
    Remove-Item -LiteralPath $tempRoot -Recurse -Force -ErrorAction SilentlyContinue
}
