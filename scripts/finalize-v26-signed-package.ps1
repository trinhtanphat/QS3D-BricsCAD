[CmdletBinding(SupportsShouldProcess = $true, ConfirmImpact = 'High')]
param(
    [string]$PackageDirectory = (Join-Path (Split-Path -Parent $PSScriptRoot) 'dist\QS3D-BricsCAD-V26'),
    [string]$PackageZip = (Join-Path (Split-Path -Parent $PSScriptRoot) 'dist\QS3D-BricsCAD-V26.zip'),

    [Parameter(Mandatory = $true)]
    [ValidatePattern('^[0-9A-Fa-f]{40}$')]
    [string]$ExpectedSignerThumbprint
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$maxGeneratedScriptBytes = 1MB
$strictUtf8 = [Text.UTF8Encoding]::new($false, $true)

function Assert-NoReparseDirectoryChain {
    param([Parameter(Mandatory = $true)][string]$Path, [Parameter(Mandatory = $true)][string]$Label)

    $cursor = [IO.Path]::GetFullPath($Path)
    while ($true) {
        if (Test-Path -LiteralPath $cursor) {
            $item = Get-Item -LiteralPath $cursor -Force -ErrorAction Stop
            if (-not $item.PSIsContainer) { throw "$Label ancestor must be a directory: $cursor" }
            if (($item.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
                throw "$Label path contains a reparse-point directory: $cursor"
            }
        }
        $parent = [IO.Directory]::GetParent($cursor)
        if ($null -eq $parent) { break }
        $cursor = $parent.FullName
    }
}

function Resolve-OrdinaryNonReparseFile {
    param([Parameter(Mandatory = $true)][string]$Path, [Parameter(Mandatory = $true)][string]$Label)

    $full = [IO.Path]::GetFullPath($Path)
    $parent = [IO.Path]::GetDirectoryName($full)
    if ([string]::IsNullOrWhiteSpace($parent)) { throw "$Label requires an ordinary parent directory: $full" }
    Assert-NoReparseDirectoryChain -Path $parent -Label $Label
    $item = Get-Item -LiteralPath $full -Force -ErrorAction Stop
    if ($item.PSIsContainer -or -not ($item -is [IO.FileInfo])) { throw "$Label must be an ordinary file: $full" }
    if (($item.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) { throw "$Label must not be reparse-backed: $full" }
    return $item
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

    $current = Resolve-OrdinaryNonReparseFile -Path $ExpectedPath -Label 'Generated V26 finalizer script'
    if (-not [string]::Equals($current.FullName, $Admitted.FullName, [StringComparison]::OrdinalIgnoreCase) -or
        -not [string]::Equals([IO.Path]::GetFullPath($Stream.Name), $Admitted.FullName, [StringComparison]::OrdinalIgnoreCase) -or
        [int64]$Stream.Length -ne [int64]$Admitted.Length -or
        [int64]$current.Length -ne [int64]$Admitted.Length -or
        [int64]$current.LastWriteTimeUtc.Ticks -ne [int64]$Admitted.LastWriteTimeUtc.Ticks) {
        throw 'Generated V26 finalizer pathname or metadata no longer matches the held admitted generation.'
    }
}

$generator = Join-Path $PSScriptRoot 'new-v26-script-from-v25.ps1'
if (-not (Test-Path -LiteralPath $generator -PathType Leaf)) { throw "V26 script transformer was not found: $generator" }

# The generated finalizer inherits the V25 containment contract, which derives the
# repository root from the generated script's PSScriptRoot. Keep the transient
# generated script in this canonical scripts directory so its parent remains the
# real repository root; generating under the process temp root would rebase the
# containment boundary to %TEMP% and reject legitimate repo-local dist outputs.
$tempScript = Join-Path $PSScriptRoot ('.finalize-v26-signed-package.generated.' + [Guid]::NewGuid().ToString('N') + '.ps1')
$generatedStream = $null
$primaryFailure = $null
try {
    & $generator -SourceScript 'finalize-v25-signed-package.ps1' -OutputPath $tempScript
    if (-not $?) { throw 'Could not generate the V26 signed-package finalizer.' }

    $generatedItem = Resolve-OrdinaryNonReparseFile -Path $tempScript -Label 'Generated V26 finalizer script'
    $generatedStream = [IO.File]::Open($generatedItem.FullName, [IO.FileMode]::Open, [IO.FileAccess]::Read, [IO.FileShare]::Read)
    Assert-HeldGeneratedScript -Stream $generatedStream -Admitted $generatedItem -ExpectedPath $tempScript
    $generated = Read-HeldStrictUtf8 -Stream $generatedStream -Label 'Generated V26 finalizer script' -MaxBytes $maxGeneratedScriptBytes
    if ($generated -match '(?i)v25') { throw 'Generated V26 finalizer contains a V25 token.' }

    $forward = @{
        PackageDirectory = $PackageDirectory
        PackageZip = $PackageZip
        ExpectedSignerThumbprint = $ExpectedSignerThumbprint
    }
    if ($PSBoundParameters.ContainsKey('WhatIf')) { $forward['WhatIf'] = [bool]$PSBoundParameters['WhatIf'] }
    if ($PSBoundParameters.ContainsKey('Confirm')) { $forward['Confirm'] = [bool]$PSBoundParameters['Confirm'] }

    # FileShare.Read deliberately keeps write/delete sharing closed while the
    # canonical path is invoked, preserving $PSScriptRoot without allowing a
    # second generated-script generation to replace the one validated above.
    Assert-HeldGeneratedScript -Stream $generatedStream -Admitted $generatedItem -ExpectedPath $tempScript
    & $tempScript @forward
    if (-not $?) { throw 'V26 signed-package finalization failed.' }
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

    # Successful finalization has a strict cleanup contract: re-admit the leaf,
    # unlink it, and prove the canonical temp pathname is gone. The repository-
    # root compatibility token uses SilentlyContinue only for the Remove-Item
    # call; the postcondition below converts any hidden unlink failure back into
    # a release-safety failure. If a primary failure is already propagating,
    # cleanup is best-effort and cannot replace that primary evidence.
    if ($null -eq $primaryFailure) {
        if (Test-Path -LiteralPath $tempScript) {
            [void](Resolve-OrdinaryNonReparseFile -Path $tempScript -Label 'Generated V26 finalizer cleanup script')
            if (Test-Path -LiteralPath $tempScript) { Remove-Item -LiteralPath $tempScript -Force -ErrorAction SilentlyContinue }
            if (Test-Path -LiteralPath $tempScript) { throw 'Generated V26 finalizer cleanup did not remove the admitted transient script.' }
        }
    }
    else {
        try {
            if (Test-Path -LiteralPath $tempScript) { Remove-Item -LiteralPath $tempScript -Force -ErrorAction SilentlyContinue }
        }
        catch {
            # Preserve the primary transformer/finalizer failure; this branch is
            # already fail-closed and cleanup is only secondary evidence.
        }
    }
}
