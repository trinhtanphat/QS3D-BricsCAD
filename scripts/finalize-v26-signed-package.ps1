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
$generator = Join-Path $PSScriptRoot 'new-v26-script-from-v25.ps1'
if (-not (Test-Path -LiteralPath $generator -PathType Leaf)) { throw "V26 script transformer was not found: $generator" }

# The generated finalizer inherits the V25 containment contract, which derives the
# repository root from the generated script's PSScriptRoot. Keep the transient
# generated script in this canonical scripts directory so its parent remains the
# real repository root; generating under the process temp root would rebase the
# containment boundary to %TEMP% and reject legitimate repo-local dist outputs.
$tempScript = Join-Path $PSScriptRoot ('.finalize-v26-signed-package.generated.' + [Guid]::NewGuid().ToString('N') + '.ps1')
try {
    & $generator -SourceScript 'finalize-v25-signed-package.ps1' -OutputPath $tempScript
    if (-not $?) { throw 'Could not generate the V26 signed-package finalizer.' }
    $generated = Get-Content -LiteralPath $tempScript -Raw
    if ($generated -match '(?i)v25') { throw 'Generated V26 finalizer contains a V25 token.' }

    $forward = @{
        PackageDirectory = $PackageDirectory
        PackageZip = $PackageZip
        ExpectedSignerThumbprint = $ExpectedSignerThumbprint
    }
    if ($PSBoundParameters.ContainsKey('WhatIf')) { $forward['WhatIf'] = [bool]$PSBoundParameters['WhatIf'] }
    if ($PSBoundParameters.ContainsKey('Confirm')) { $forward['Confirm'] = [bool]$PSBoundParameters['Confirm'] }
    & $tempScript @forward
    if (-not $?) { throw 'V26 signed-package finalization failed.' }
}
finally {
    if (Test-Path -LiteralPath $tempScript) { Remove-Item -LiteralPath $tempScript -Force -ErrorAction SilentlyContinue }
}
