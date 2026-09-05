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
$maxGeneratedScriptBytes = 1MB
$strictUtf8 = [Text.UTF8Encoding]::new($false, $true)

function Assert-OrdinaryPathItem {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$Label,
        [Parameter(Mandatory = $true)][bool]$Directory
    )

    $item = Get-Item -LiteralPath $Path -Force -ErrorAction Stop
    if ($Directory -and -not $item.PSIsContainer) { throw "$Label must be a directory: $Path" }
    if (-not $Directory -and ($item.PSIsContainer -or -not ($item -is [IO.FileInfo]))) { throw "$Label must be a regular file: $Path" }
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

function Read-HeldStrictUtf8 {
    param(
        [Parameter(Mandatory = $true)][IO.FileStream]$Stream,
        [Parameter(Mandatory = $true)][string]$Label,
        [Parameter(Mandatory = $true)][int64]$MaxBytes
    )

    if (-not $Stream.CanRead) { throw "$Label held stream is not readable." }
    $length = [int64]$Stream.Length
    if ($length -lt 1 -or $length -gt $MaxBytes -or $length -gt [int]::MaxValue) {
        throw "$Label held generation has invalid bounded size: $length bytes."
    }
    $Stream.Position = 0
    $bytes = [byte[]]::new([int]$length)
    try {
        $offset = 0
        while ($offset -lt $bytes.Length) {
            $read = $Stream.Read($bytes, $offset, $bytes.Length - $offset)
            if ($read -le 0) { throw "$Label ended before its held length was read." }
            $offset += $read
        }
        if ($Stream.ReadByte() -ne -1 -or $Stream.Length -ne $length) { throw "$Label changed while its held bytes were being read." }
        try { return $strictUtf8.GetString($bytes) }
        catch [Text.DecoderFallbackException] { throw "$Label is not strict UTF-8." }
    }
    finally {
        [Array]::Clear($bytes, 0, $bytes.Length)
        $Stream.Position = 0
    }
}

function Assert-HeldGeneratedScript {
    param(
        [Parameter(Mandatory = $true)][IO.FileStream]$Stream,
        [Parameter(Mandatory = $true)][IO.FileInfo]$Admitted,
        [Parameter(Mandatory = $true)][string]$ExpectedPath
    )

    $current = Assert-OrdinaryPathItem -Path $ExpectedPath -Label 'Generated V26 update-manifest script' -Directory $false
    if (-not [string]::Equals($current.FullName, $Admitted.FullName, [StringComparison]::OrdinalIgnoreCase) -or
        -not [string]::Equals([IO.Path]::GetFullPath($Stream.Name), $Admitted.FullName, [StringComparison]::OrdinalIgnoreCase) -or
        [int64]$Stream.Length -ne [int64]$Admitted.Length -or
        [int64]$current.Length -ne [int64]$Admitted.Length -or
        [int64]$current.LastWriteTimeUtc.Ticks -ne [int64]$Admitted.LastWriteTimeUtc.Ticks) {
        throw 'Generated V26 update-manifest pathname or metadata no longer matches the held admitted generation.'
    }
}

function Remove-V26ManifestTemporaryWorkspaceStrict {
    param([Parameter(Mandatory = $true)][string]$ScriptPath, [Parameter(Mandatory = $true)][string]$RootPath)

    if (Test-Path -LiteralPath $ScriptPath) {
        Assert-OrdinaryPathItem -Path $ScriptPath -Label 'Generated V26 update-manifest script' -Directory $false | Out-Null
        Remove-Item -LiteralPath $ScriptPath -Force -ErrorAction Stop
        if (Test-Path -LiteralPath $ScriptPath) { throw "Generated V26 update-manifest script still exists after cleanup: $ScriptPath" }
    }
    if (Test-Path -LiteralPath $RootPath) {
        Assert-DirectoryAncestorChain -Path $RootPath -Label 'V26 manifest temporary ancestor'
        Assert-OrdinaryPathItem -Path $RootPath -Label 'V26 manifest temporary workspace' -Directory $true | Out-Null
        $residue = @(Get-ChildItem -LiteralPath $RootPath -Force)
        if ($residue.Count -ne 0) { throw "V26 manifest temporary workspace contains unexpected residue; refusing recursive cleanup: $RootPath" }
        Remove-Item -LiteralPath $RootPath -Force -ErrorAction Stop
        if (Test-Path -LiteralPath $RootPath) { throw "V26 manifest temporary workspace still exists after cleanup: $RootPath" }
    }
}

function Remove-V26ManifestTemporaryWorkspaceBestEffort {
    param([Parameter(Mandatory = $true)][string]$ScriptPath, [Parameter(Mandatory = $true)][string]$RootPath)

    try {
        if (Test-Path -LiteralPath $ScriptPath) {
            $scriptItem = Get-Item -LiteralPath $ScriptPath -Force -ErrorAction Stop
            if (-not $scriptItem.PSIsContainer -and ($scriptItem -is [IO.FileInfo]) -and (($scriptItem.Attributes -band [IO.FileAttributes]::ReparsePoint) -eq 0)) {
                Remove-Item -LiteralPath $ScriptPath -Force -ErrorAction Stop
            }
        }
    }
    catch { Write-Verbose "Secondary V26 manifest script cleanup failed while preserving the primary failure: $($_.Exception.Message)" }

    try {
        if (Test-Path -LiteralPath $RootPath) {
            $rootItem = Get-Item -LiteralPath $RootPath -Force -ErrorAction Stop
            if ($rootItem.PSIsContainer -and (($rootItem.Attributes -band [IO.FileAttributes]::ReparsePoint) -eq 0)) {
                $residue = @(Get-ChildItem -LiteralPath $RootPath -Force -ErrorAction Stop)
                if ($residue.Count -eq 0) { Remove-Item -LiteralPath $RootPath -Force -ErrorAction Stop }
            }
        }
    }
    catch { Write-Verbose "Secondary V26 manifest workspace cleanup failed while preserving the primary failure: $($_.Exception.Message)" }
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
$generatedStream = $null
$primaryFailure = $null
try {
    & $generator -SourceScript 'new-v25-update-manifest.ps1' -OutputPath $tempScript
    if (-not $?) { throw 'Could not generate the V26 update-manifest implementation.' }
    $generatedItem = Assert-OrdinaryPathItem -Path $tempScript -Label 'Generated V26 update-manifest script' -Directory $false
    $generatedStream = [IO.File]::Open($generatedItem.FullName, [IO.FileMode]::Open, [IO.FileAccess]::Read, [IO.FileShare]::Read)
    Assert-HeldGeneratedScript -Stream $generatedStream -Admitted $generatedItem -ExpectedPath $tempScript
    $generated = Read-HeldStrictUtf8 -Stream $generatedStream -Label 'Generated V26 update-manifest script' -MaxBytes $maxGeneratedScriptBytes
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

    Assert-HeldGeneratedScript -Stream $generatedStream -Admitted $generatedItem -ExpectedPath $tempScript
    & $tempScript @forward
    if (-not $?) { throw 'V26 update-manifest generation failed.' }
    Assert-HeldGeneratedScript -Stream $generatedStream -Admitted $generatedItem -ExpectedPath $tempScript
}
catch {
    $primaryFailure = $_
    throw
}
finally {
    if ($null -ne $generatedStream) {
        $generatedStream.Dispose()
        $generatedStream = $null
    }
    if ($null -eq $primaryFailure) {
        Remove-V26ManifestTemporaryWorkspaceStrict -ScriptPath $tempScript -RootPath $tempRoot
    }
    else {
        Remove-V26ManifestTemporaryWorkspaceBestEffort -ScriptPath $tempScript -RootPath $tempRoot
    }
}