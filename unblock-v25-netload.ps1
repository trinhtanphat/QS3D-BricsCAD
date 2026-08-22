[CmdletBinding()]
param(
    [string]$PackageDirectory = $PSScriptRoot
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Resolve-Qs3dPackageRoot {
    param([string]$Directory)

    if ([string]::IsNullOrWhiteSpace($Directory)) {
        throw 'PackageDirectory is required.'
    }
    $resolved = (Resolve-Path -LiteralPath $Directory -ErrorAction Stop).Path
    if (-not (Test-Path -LiteralPath $resolved -PathType Container)) {
        throw "QS3D package directory was not found: $resolved"
    }
    return [IO.Path]::GetFullPath($resolved).TrimEnd([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar)
}

function Get-SafePackagePath {
    param(
        [string]$Root,
        [string]$RelativePath
    )

    if ([string]::IsNullOrWhiteSpace($RelativePath) -or [IO.Path]::IsPathRooted($RelativePath) -or $RelativePath.Contains('\') -or $RelativePath.Contains(':')) {
        throw "Unsafe SHA256SUMS entry: $RelativePath"
    }

    $segments = @($RelativePath.Split('/'))
    if ($segments.Count -eq 0 -or @($segments | Where-Object { [string]::IsNullOrWhiteSpace($_) -or $_ -eq '.' -or $_ -eq '..' }).Count -gt 0) {
        throw "Unsafe SHA256SUMS entry: $RelativePath"
    }

    $rootPrefix = $Root + [IO.Path]::DirectorySeparatorChar
    $candidate = [IO.Path]::GetFullPath((Join-Path $Root ($RelativePath.Replace('/', [IO.Path]::DirectorySeparatorChar))))
    if (-not $candidate.StartsWith($rootPrefix, [StringComparison]::OrdinalIgnoreCase)) {
        throw "SHA256SUMS entry escapes the package root: $RelativePath"
    }
    return $candidate
}

function Assert-Qs3dPackageIntegrity {
    param([string]$Root)

    $manifest = Join-Path $Root 'SHA256SUMS.txt'
    if (-not (Test-Path -LiteralPath $manifest -PathType Leaf)) {
        throw "Missing hash manifest: $manifest"
    }

    $manifestEntries = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    $verified = 0
    foreach ($line in Get-Content -LiteralPath $manifest) {
        if ([string]::IsNullOrWhiteSpace($line)) { continue }
        if ($line -notmatch '^([0-9A-Fa-f]{64})\s{2}(.+)$') {
            throw "Invalid SHA256SUMS entry: $line"
        }

        $expected = $Matches[1].ToUpperInvariant()
        $relative = $Matches[2].Trim()
        if ($relative -eq 'SHA256SUMS.txt') {
            throw 'SHA256SUMS.txt must not hash itself.'
        }
        if (-not $manifestEntries.Add($relative)) {
            throw "Duplicate SHA256SUMS payload entry: $relative"
        }

        $path = Get-SafePackagePath -Root $Root -RelativePath $relative
        if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
            throw "Missing package payload: $relative"
        }
        $actual = (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToUpperInvariant()
        if ($actual -ne $expected) {
            throw "SHA-256 mismatch for $relative"
        }
        $verified++
    }
    if ($verified -eq 0) {
        throw 'SHA256SUMS.txt contains no payload entries.'
    }

    $rootPrefix = $Root + [IO.Path]::DirectorySeparatorChar
    $actualEntries = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    foreach ($packageFile in Get-ChildItem -LiteralPath $Root -File -Recurse) {
        $fullPath = [IO.Path]::GetFullPath($packageFile.FullName)
        if ([string]::Equals($fullPath, [IO.Path]::GetFullPath($manifest), [StringComparison]::OrdinalIgnoreCase)) { continue }
        if (-not $fullPath.StartsWith($rootPrefix, [StringComparison]::OrdinalIgnoreCase)) {
            throw "Package payload escaped package root: $($packageFile.FullName)"
        }
        $relative = $fullPath.Substring($rootPrefix.Length).Replace([IO.Path]::DirectorySeparatorChar, '/').Replace([IO.Path]::AltDirectorySeparatorChar, '/')
        if (-not $actualEntries.Add($relative)) {
            throw "Duplicate/case-colliding package payload path: $relative"
        }
        if (-not $manifestEntries.Contains($relative)) {
            throw "Unhashed package payload: $relative"
        }
    }

    foreach ($relative in $manifestEntries) {
        if (-not $actualEntries.Contains($relative)) {
            throw "SHA256SUMS entry does not map to a regular package file: $relative"
        }
    }
    if ($actualEntries.Count -ne $manifestEntries.Count) {
        throw "SHA256SUMS coverage mismatch. Manifest entries=$($manifestEntries.Count), package files=$($actualEntries.Count)."
    }

    foreach ($required in @(
        'QS3D.BricsCAD.V25.dll',
        'QS3D.Core.dll',
        'PACKAGE-METADATA.json',
        'COMMANDS.txt',
        'INSTALL-QS3D.cmd',
        'UNBLOCK-QS3D.cmd',
        'unblock-v25-netload.ps1'
    )) {
        if (-not $manifestEntries.Contains($required)) {
            throw "Required V25 recovery payload is not covered by SHA256SUMS.txt: $required"
        }
    }

    $metadataPath = Join-Path $Root 'PACKAGE-METADATA.json'
    try { $metadata = Get-Content -LiteralPath $metadataPath -Raw | ConvertFrom-Json }
    catch { throw "PACKAGE-METADATA.json is unreadable: $($_.Exception.Message)" }
    if ([string]$metadata.product -ne 'QS3D') {
        throw 'PACKAGE-METADATA product must be QS3D.'
    }
    if ([string]$metadata.target -ne 'BricsCAD V25 x64') {
        throw 'PACKAGE-METADATA target must be BricsCAD V25 x64.'
    }

    $commandsPath = Join-Path $Root 'COMMANDS.txt'
    $commands = @(Get-Content -LiteralPath $commandsPath | ForEach-Object { $_.Trim().ToUpperInvariant() } | Where-Object { $_ })
    if (-not ($commands -contains 'QS3D')) {
        throw 'COMMANDS.txt does not contain the QS3D entry command.'
    }
}

$package = Resolve-Qs3dPackageRoot -Directory $PackageDirectory
Assert-Qs3dPackageIntegrity -Root $package

$unblocked = 0
foreach ($packageFile in Get-ChildItem -LiteralPath $package -File -Recurse) {
    Unblock-File -LiteralPath $packageFile.FullName -ErrorAction Stop
    $unblocked++
}

Write-Host "QS3D package integrity verified. Mark-of-the-Web was removed from $unblocked package file(s)."
Write-Host ('Manual NETLOAD target: ' + (Join-Path $package 'QS3D.BricsCAD.V25.dll'))
Write-Host 'No BricsCAD security, trusted-path or PowerShell execution-policy setting was weakened.'
