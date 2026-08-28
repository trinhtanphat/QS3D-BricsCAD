[CmdletBinding(SupportsShouldProcess = $true, ConfirmImpact = 'Low')]
param(
    [string]$PackageDirectory = (Join-Path (Split-Path -Parent $PSScriptRoot) 'dist\QS3D-BricsCAD-V26'),
    [string]$PackageZip = (Join-Path (Split-Path -Parent $PSScriptRoot) 'dist\QS3D-BricsCAD-V26.zip'),

    [Parameter(Mandatory = $true)]
    [ValidatePattern('^https://')]
    [string]$PackageUri,

    [Parameter(Mandatory = $true)]
    [ValidatePattern('^[0-9A-Fa-f]{40}$')]
    [string]$ExpectedSignerThumbprint,

    [string]$OutputPath = (Join-Path (Split-Path -Parent $PSScriptRoot) 'dist\QS3D-BricsCAD-V26.update.json')
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Assert-OrdinaryPathItem {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$Label,
        [Parameter(Mandatory = $true)][bool]$Directory
    )

    $item = Get-Item -LiteralPath $Path -Force -ErrorAction Stop
    if ($Directory -and -not $item.PSIsContainer) { throw "$Label must be a directory: $Path" }
    if (-not $Directory -and $item.PSIsContainer) { throw "$Label must be a regular file: $Path" }
    if (($item.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
        throw "$Label must not be reparse-backed: $Path"
    }
    return $item
}

function Assert-DirectoryAncestorChain {
    param([Parameter(Mandatory = $true)][string]$Path, [Parameter(Mandatory = $true)][string]$Label)

    $cursor = [IO.Path]::GetFullPath($Path)
    while ($true) {
        if (Test-Path -LiteralPath $cursor) {
            Assert-OrdinaryPathItem -Path $cursor -Label $Label -Directory $true | Out-Null
        }
        $parent = [IO.Directory]::GetParent($cursor)
        if (-not $parent) { break }
        $cursor = $parent.FullName
    }
}

$generator = Join-Path $PSScriptRoot 'new-v26-script-from-v25.ps1'
if (-not (Test-Path -LiteralPath $generator -PathType Leaf)) { throw "V26 script transformer was not found: $generator" }
Assert-DirectoryAncestorChain -Path (Split-Path -Parent $generator) -Label 'V26 transformer ancestor'
Assert-OrdinaryPathItem -Path $generator -Label 'V26 script transformer' -Directory $false | Out-Null

$tempParent = [IO.Path]::GetFullPath([IO.Path]::GetTempPath())
Assert-DirectoryAncestorChain -Path $tempParent -Label 'V26 manifest temporary ancestor'
Assert-OrdinaryPathItem -Path $tempParent -Label 'V26 manifest temporary parent' -Directory $true | Out-Null

$tempRoot = Join-Path $tempParent ('qs3d-v26-manifest-' + [Guid]::NewGuid().ToString('N'))
$tempScript = Join-Path $tempRoot 'new-v26-update-manifest.generated.ps1'
if (Test-Path -LiteralPath $tempRoot) { throw "V26 manifest temporary workspace already exists: $tempRoot" }
New-Item -ItemType Directory -Path $tempRoot | Out-Null
Assert-DirectoryAncestorChain -Path $tempRoot -Label 'V26 manifest temporary ancestor'
Assert-OrdinaryPathItem -Path $tempRoot -Label 'V26 manifest temporary workspace' -Directory $true | Out-Null
try {
    & $generator -SourceScript 'new-v25-update-manifest.ps1' -OutputPath $tempScript
    if (-not $?) { throw 'Could not generate the V26 update-manifest implementation.' }
    Assert-OrdinaryPathItem -Path $tempScript -Label 'Generated V26 update-manifest script' -Directory $false | Out-Null
    $generated = Get-Content -LiteralPath $tempScript -Raw
    if ($generated -match '(?i)v25') { throw 'Generated V26 update-manifest implementation contains a V25 token.' }

    $forward = @{
        PackageDirectory = $PackageDirectory
        PackageZip = $PackageZip
        PackageUri = $PackageUri
        ExpectedSignerThumbprint = $ExpectedSignerThumbprint
        OutputPath = $OutputPath
    }
    if ($PSBoundParameters.ContainsKey('WhatIf')) { $forward['WhatIf'] = [bool]$PSBoundParameters['WhatIf'] }
    if ($PSBoundParameters.ContainsKey('Confirm')) { $forward['Confirm'] = [bool]$PSBoundParameters['Confirm'] }
    & $tempScript @forward
    if (-not $?) { throw 'V26 update-manifest generation failed.' }
}
finally {
    if (Test-Path -LiteralPath $tempScript) {
        Assert-OrdinaryPathItem -Path $tempScript -Label 'Generated V26 update-manifest script' -Directory $false | Out-Null
        Remove-Item -LiteralPath $tempScript -Force
    }
    if (Test-Path -LiteralPath $tempRoot) {
        Assert-DirectoryAncestorChain -Path $tempRoot -Label 'V26 manifest temporary ancestor'
        Assert-OrdinaryPathItem -Path $tempRoot -Label 'V26 manifest temporary workspace' -Directory $true | Out-Null
        $residue = @(Get-ChildItem -LiteralPath $tempRoot -Force)
        if ($residue.Count -ne 0) { throw "V26 manifest temporary workspace contains unexpected residue; refusing recursive cleanup: $tempRoot" }
        Remove-Item -LiteralPath $tempRoot -Force
    }
}
