[CmdletBinding(SupportsShouldProcess = $true, ConfirmImpact = 'High')]
param(
    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string[]]$Path,

    [Parameter(Mandatory = $true)]
    [ValidatePattern('^[0-9A-Fa-f]{40}$')]
    [string]$CertificateThumbprint,

    [Parameter(Mandatory = $true)]
    [ValidatePattern('^https://')]
    [string]$TimestampServer
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$template = Join-Path $PSScriptRoot 'sign-v25.ps1'
if (-not (Test-Path -LiteralPath $template -PathType Leaf)) { throw "Signing implementation was not found: $template" }

& $template @PSBoundParameters
if (-not $?) { throw 'V26 Authenticode signing failed.' }
