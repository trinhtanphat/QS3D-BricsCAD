[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('Hash', 'Copy')]
    [string]$Operation,

    [Parameter(Mandatory = $true)]
    [string]$Path,

    [string]$Destination
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

function Get-CanonicalFullPath {
    param([Parameter(Mandatory = $true)][string]$LiteralPath)
    return [IO.Path]::GetFullPath($LiteralPath)
}

function Assert-NoReparseAncestor {
    param([Parameter(Mandatory = $true)][string]$LiteralPath)

    $cursor = [IO.Directory]::GetParent((Get-CanonicalFullPath -LiteralPath $LiteralPath))
    while ($null -ne $cursor) {
        if (($cursor.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
            throw "Held V25 release input traverses a reparse-point ancestor: $($cursor.FullName)"
        }
        $cursor = $cursor.Parent
    }
}

function Open-HeldGeneration {
    param([Parameter(Mandatory = $true)][string]$LiteralPath)

    $canonical = Get-CanonicalFullPath -LiteralPath $LiteralPath
    Assert-NoReparseAncestor -LiteralPath $canonical
    $admitted = Get-Item -LiteralPath $canonical -Force -ErrorAction Stop
    if ($admitted.PSIsContainer) { throw "Held V25 release input must be a file: $canonical" }
    if (($admitted.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
        throw "Held V25 release input must not be a reparse point: $canonical"
    }
    $admittedPath = Get-CanonicalFullPath -LiteralPath $admitted.FullName
    if (-not [string]::Equals($canonical, $admittedPath, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Held V25 release input canonical identity drifted before open: $canonical"
    }
    $admittedLength = [int64]$admitted.Length
    $admittedWriteTicks = [int64]$admitted.LastWriteTimeUtc.Ticks

    $stream = [IO.File]::Open($canonical, [IO.FileMode]::Open, [IO.FileAccess]::Read, [IO.FileShare]::Read)
    try {
        $rebound = Get-Item -LiteralPath $canonical -Force -ErrorAction Stop
        $reboundPath = Get-CanonicalFullPath -LiteralPath $rebound.FullName
        if ($rebound.PSIsContainer -or (($rebound.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0)) {
            throw "Held V25 release input changed to a non-ordinary file after open: $canonical"
        }
        if (-not [string]::Equals($admittedPath, $reboundPath, [StringComparison]::OrdinalIgnoreCase) -or
            [int64]$rebound.Length -ne $admittedLength -or
            [int64]$rebound.LastWriteTimeUtc.Ticks -ne $admittedWriteTicks -or
            [int64]$stream.Length -ne $admittedLength) {
            throw "Held V25 release input generation changed across admission/open: $canonical"
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

$held = Open-HeldGeneration -LiteralPath $Path
try {
    switch ($Operation) {
        'Hash' {
            $sha = [Security.Cryptography.SHA256]::Create()
            try {
                $digest = $sha.ComputeHash($held.Stream)
                $hex = -join ($digest | ForEach-Object { $_.ToString('x2') })
                Write-Output $hex
            }
            finally {
                $sha.Dispose()
            }
        }
        'Copy' {
            if ([string]::IsNullOrWhiteSpace($Destination)) {
                throw 'Destination is required for Copy.'
            }
            $destinationFull = Get-CanonicalFullPath -LiteralPath $Destination
            $parent = Split-Path -Parent $destinationFull
            if (-not (Test-Path -LiteralPath $parent -PathType Container)) {
                throw "Held V25 release copy destination parent does not exist: $parent"
            }
            if (Test-Path -LiteralPath $destinationFull) {
                throw "Held V25 release copy destination already exists: $destinationFull"
            }
            $output = [IO.File]::Open($destinationFull, [IO.FileMode]::CreateNew, [IO.FileAccess]::Write, [IO.FileShare]::None)
            try {
                $held.Stream.CopyTo($output)
                $output.Flush($true)
            }
            finally {
                $output.Dispose()
            }
            $copied = Get-Item -LiteralPath $destinationFull -Force -ErrorAction Stop
            if ([int64]$copied.Length -ne [int64]$held.Length) {
                Remove-Item -LiteralPath $destinationFull -Force -ErrorAction SilentlyContinue
                throw "Held V25 release copy length mismatch: $destinationFull"
            }
            Write-Output $destinationFull
        }
    }
}
finally {
    $held.Stream.Dispose()
}
