[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$BricsCadDir,
    [Parameter(Mandatory = $true)][string]$PluginDll,
    [Parameter(Mandatory = $true)][string]$FixtureDwg,
    [Parameter(Mandatory = $true)][string]$Profile,
    [Parameter(Mandatory = $true)][string]$ArtifactDir,
    [Parameter(Mandatory = $true)][switch]$ConfirmDisposableCopies,
    [ValidateRange(120, 1800)][int]$StartupTimeoutSeconds = 600
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$runner = Join-Path $PSScriptRoot "test-bricscad-v25-source-reconcile-native-polyline-edit.ps1"
if (-not (Test-Path -LiteralPath $runner -PathType Leaf)) {
    throw "The shared native POLYLINE edit runner is missing."
}

& $runner `
    -BricsCadDir $BricsCadDir `
    -PluginDll $PluginDll `
    -FixtureDwg $FixtureDwg `
    -Profile $Profile `
    -ArtifactDir $ArtifactDir `
    -ConfirmDisposableCopies:$ConfirmDisposableCopies `
    -HostMajor 26 `
    -StartupTimeoutSeconds $StartupTimeoutSeconds

if (-not $?) { throw "The BricsCAD V26 native POLYLINE edit runner failed." }
