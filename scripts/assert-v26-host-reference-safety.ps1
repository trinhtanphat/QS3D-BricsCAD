[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$BricsCadDir
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

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
$required = @{}
foreach ($name in $requiredNames) {
    $path = Join-Path $canonicalDir $name
    Assert-NoExistingReparseComponent -Path $path -Label $name
    $required[$name] = Get-RequiredOrdinaryFile -Path $path -Label $name
}

$version = $required['bricscad.exe'].VersionInfo
if ($version.FileMajorPart -ne 26) {
    throw "BricsCadDir is not BricsCAD V26. Detected $($version.FileVersion)."
}

Write-Output $canonicalDir
