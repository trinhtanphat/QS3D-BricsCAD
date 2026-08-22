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
$generator = Join-Path $PSScriptRoot 'new-v26-script-from-v25.ps1'
if (-not (Test-Path -LiteralPath $generator -PathType Leaf)) { throw "V26 script transformer was not found: $generator" }

$tempRoot = Join-Path ([IO.Path]::GetTempPath()) ('qs3d-v26-manifest-' + [Guid]::NewGuid().ToString('N'))
$tempScript = Join-Path $tempRoot 'new-v26-update-manifest.generated.ps1'
New-Item -ItemType Directory -Path $tempRoot -Force | Out-Null
try {
    & $generator -SourceScript 'new-v25-update-manifest.ps1' -OutputPath $tempScript
    if (-not $?) { throw 'Could not generate the V26 update-manifest implementation.' }
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
    if (Test-Path -LiteralPath $tempRoot) { Remove-Item -LiteralPath $tempRoot -Recurse -Force -ErrorAction SilentlyContinue }
}
