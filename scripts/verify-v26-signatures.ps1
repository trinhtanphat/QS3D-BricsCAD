[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string[]]$Path,

    [ValidatePattern('^[0-9A-Fa-f]{40}$')]
    [string]$ExpectedThumbprint = ''
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$template = Join-Path $PSScriptRoot 'verify-v25-signatures.ps1'
if (-not (Test-Path -LiteralPath $template -PathType Leaf)) { throw "Signature verification implementation was not found: $template" }

& $template @PSBoundParameters
if (-not $?) { throw 'V26 Authenticode signature verification failed.' }
