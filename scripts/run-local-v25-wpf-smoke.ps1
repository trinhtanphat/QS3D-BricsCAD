[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$BricsCadDir,
    [string]$PluginPath = ""
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$BricsCadDir = [IO.Path]::GetFullPath($BricsCadDir)
if ([string]::IsNullOrWhiteSpace($PluginPath)) {
    $PluginPath = Join-Path $repoRoot "src\QS3D.BricsCAD.V25\bin\x64\Release\net48\QS3D.BricsCAD.V25.dll"
}
$PluginPath = [IO.Path]::GetFullPath($PluginPath)

foreach ($name in @("BrxMgd.dll", "TD_Mgd.dll")) {
    $path = Join-Path $BricsCadDir $name
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Required BricsCAD V25 assembly is missing: $path"
    }
}
if (-not (Test-Path -LiteralPath $PluginPath -PathType Leaf)) {
    throw "Built QS3D V25 plugin is missing: $PluginPath"
}

Write-Host "=== WPF theme resource smoke ==="
& (Join-Path $PSScriptRoot "test-wpf-theme-runtime.ps1")

Write-Host ""
Write-Host "=== WPF Workspace / RightPanel layout smoke ==="
& (Join-Path $PSScriptRoot "test-wpf-palettes-runtime.ps1") `
    -PluginPath $PluginPath `
    -BricscadDirectory $BricsCadDir

Write-Host ""
Write-Host "PASS: offline WPF theme + Workspace/RightPanel smoke completed."
Write-Host "This is an early local failure detector only; it does not replace licensed BricsCAD V25 NETLOAD, host-theme, HiDPI or private-DWG qualification."
