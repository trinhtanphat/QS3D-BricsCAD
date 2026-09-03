[CmdletBinding()]
param(
  [Parameter(Mandatory = $true)][string[]]$Path,
  [Parameter(Mandatory = $true)][string]$ReleaseTag,
  [Parameter(Mandatory = $true)][string]$Repository
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

if ($Path.Count -eq 0) { throw 'At least one V25 release asset is required.' }
if ([string]::IsNullOrWhiteSpace($ReleaseTag)) { throw 'ReleaseTag is required.' }
if ($Repository -notmatch '^[^/\s]+/[^/\s]+$') { throw 'Repository must be owner/name.' }

$held = New-Object System.Collections.Generic.List[object]
$names = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
try {
  foreach ($candidate in $Path) {
    $item = Get-Item -LiteralPath $candidate -Force -ErrorAction Stop
    if ($item.PSIsContainer) { throw "V25 release asset must be a file: $candidate" }
    if (($item.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) { throw "V25 release asset must not be a reparse point: $candidate" }
    $name = [IO.Path]::GetFileName($item.FullName)
    if ([string]::IsNullOrWhiteSpace($name) -or -not $names.Add($name)) { throw "Duplicate or empty V25 release asset name: $name" }

    # FileShare.Read deliberately denies write/delete sharing. On Windows this keeps the
    # admitted pathname generation immutable while gh opens the same file for reading.
    $stream = [IO.File]::Open($item.FullName, [IO.FileMode]::Open, [IO.FileAccess]::Read, [IO.FileShare]::Read)
    try {
      $sha = [Security.Cryptography.SHA256]::Create()
      try {
        $digest = $sha.ComputeHash($stream)
        $hash = ([BitConverter]::ToString($digest)).Replace('-', '').ToLowerInvariant()
      }
      finally { $sha.Dispose() }
      $stream.Position = 0
      $held.Add([PSCustomObject]@{
        Name = $name
        Path = $item.FullName
        Length = [int64]$stream.Length
        Sha256 = $hash
        Stream = $stream
      })
      $stream = $null
    }
    finally {
      if ($null -ne $stream) { $stream.Dispose() }
    }
  }

  foreach ($asset in $held) {
    & gh release upload $ReleaseTag ([string]$asset.Path) --repo $Repository 2>&1 | ForEach-Object { Write-Host $_ }
    if ($LASTEXITCODE -ne 0) { throw "Failed to upload held V25 draft asset: $($asset.Name)" }
  }

  foreach ($asset in $held) {
    $current = Get-Item -LiteralPath ([string]$asset.Path) -Force -ErrorAction Stop
    if ($current.PSIsContainer -or ($current.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
      throw "Held V25 release asset pathname lost ordinary-file continuity: $($asset.Name)"
    }
    if ([int64]$current.Length -ne [int64]$asset.Length) {
      throw "Held V25 release asset pathname length changed during upload: $($asset.Name)"
    }
    [PSCustomObject]@{
      Name = [string]$asset.Name
      Path = [string]$asset.Path
      Length = [int64]$asset.Length
      Sha256 = [string]$asset.Sha256
    }
  }
}
finally {
  foreach ($asset in $held) {
    if ($null -ne $asset.Stream) { $asset.Stream.Dispose() }
  }
}
