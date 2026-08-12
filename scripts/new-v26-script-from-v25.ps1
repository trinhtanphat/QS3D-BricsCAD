[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateSet(
        'install-v25-autoload.ps1',
        'uninstall-v25-autoload.ps1',
        'update-v25.ps1',
        'finalize-v25-signed-package.ps1',
        'new-v25-update-manifest.ps1'
    )]
    [string]$SourceScript,

    [Parameter(Mandatory = $true)]
    [string]$OutputPath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$sourcePath = Join-Path $PSScriptRoot $SourceScript
if (-not (Test-Path -LiteralPath $sourcePath -PathType Leaf)) {
    throw "V25 template script was not found: $sourcePath"
}

$sourceFull = [IO.Path]::GetFullPath($sourcePath)
$outputFull = [IO.Path]::GetFullPath($OutputPath)
if ([string]::Equals($sourceFull, $outputFull, [StringComparison]::OrdinalIgnoreCase)) {
    throw 'V26 generation output must not overwrite its V25 source template.'
}
if (-not [string]::Equals([IO.Path]::GetExtension($outputFull), '.ps1', [StringComparison]::OrdinalIgnoreCase)) {
    throw "V26 generated script must use the .ps1 extension: $outputFull"
}

$text = Get-Content -LiteralPath $sourceFull -Raw
if ([string]::IsNullOrWhiteSpace($text)) { throw "V25 template script is empty: $sourceFull" }
if ($text.IndexOf('V25', [StringComparison]::Ordinal) -lt 0 -and $text.IndexOf('v25', [StringComparison]::Ordinal) -lt 0) {
    throw "V25 template contains no host-major token to transform: $SourceScript"
}

# This transformation is intentionally narrow: only the host-major token changes.
# Every security/transaction/download/signature branch remains byte-for-byte equivalent
# apart from V25/v25 -> V26/v26 identifiers, paths, registry selectors and asset names.
$generated = $text.Replace('V25', 'V26').Replace('v25', 'v26')

if ($generated.IndexOf('V25', [StringComparison]::Ordinal) -ge 0 -or
    $generated.IndexOf('v25', [StringComparison]::Ordinal) -ge 0) {
    throw "Generated V26 script still contains a V25/v25 token: $SourceScript"
}

$requiredTokens = switch ($SourceScript) {
    'install-v25-autoload.ps1' {
        @('QS3D.BricsCAD.V26.dll', 'BricsCAD V26 x64', 'BricsCAD-V26', '^V26', 'QS3D-BricsCAD-V26-Update-')
        break
    }
    'uninstall-v25-autoload.ps1' {
        @('QS3D.BricsCAD.V26.dll', 'BricsCAD V26 x64', 'BricsCAD-V26', '^V26', 'QS3D-BricsCAD-V26-Update-')
        break
    }
    'update-v25.ps1' {
        @('QS3D.BricsCAD.V26.dll', 'BricsCAD V26 x64', 'BricsCAD-V26', 'QS3D-BricsCAD-V26.update.json', 'QS3D-BricsCAD-V26.zip', 'install-v26-autoload.ps1', 'QS3D-BricsCAD-V26-Update-')
        break
    }
    'finalize-v25-signed-package.ps1' {
        @('QS3D.BricsCAD.V26.dll', 'BricsCAD V26 x64', 'QS3D-BricsCAD-V26.zip', 'install-v26-autoload.ps1', 'uninstall-v26-autoload.ps1', 'update-v26.ps1')
        break
    }
    'new-v25-update-manifest.ps1' {
        @('QS3D.BricsCAD.V26.dll', 'BricsCAD V26 x64', 'QS3D-BricsCAD-V26.zip', 'QS3D-BricsCAD-V26.update.json', 'install-v26-autoload.ps1', 'uninstall-v26-autoload.ps1', 'update-v26.ps1')
        break
    }
    default { throw "Unsupported V25 template: $SourceScript" }
}

foreach ($token in $requiredTokens) {
    if ($generated.IndexOf($token, [StringComparison]::Ordinal) -lt 0) {
        throw "Generated V26 script is missing required transformed token '$token' from $SourceScript"
    }
}

$parent = Split-Path -Parent $outputFull
if ([string]::IsNullOrWhiteSpace($parent)) { throw "V26 generated script output requires a parent directory: $outputFull" }
New-Item -ItemType Directory -Path $parent -Force | Out-Null
[IO.File]::WriteAllText($outputFull, $generated, (New-Object Text.UTF8Encoding($false)))

Write-Host "Generated V26 script: $outputFull"
Write-Host "Template: $SourceScript"
Write-Host "Template SHA256: $((Get-FileHash -LiteralPath $sourceFull -Algorithm SHA256).Hash.ToUpperInvariant())"
