[CmdletBinding(SupportsShouldProcess = $true, ConfirmImpact = 'High')]
param(
    [string]$InstallDirectory = (Join-Path $env:LOCALAPPDATA 'QS3D\BricsCAD-V25'),
    [string[]]$VersionKeys,
    [string[]]$LanguageKeys,
    [switch]$KeepFiles,
    [switch]$Force
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if (Get-Process -Name bricscad -ErrorAction SilentlyContinue) {
    throw 'Close all BricsCAD processes before uninstalling QS3D.'
}

$root = 'HKCU:\Software\Bricsys\BricsCAD'
if (Test-Path -LiteralPath $root) {
    $versions = @(Get-ChildItem -LiteralPath $root | Where-Object { $_.PSChildName -match '^V25' })
    if ($VersionKeys -and $VersionKeys.Count -gt 0) { $versions = @($versions | Where-Object { $VersionKeys -contains $_.PSChildName }) }
    foreach ($version in $versions) {
        $languages = @(Get-ChildItem -LiteralPath $version.PSPath | Where-Object { $_.PSChildName -match '^[A-Za-z]{2}_[A-Za-z]{2}$' })
        if ($LanguageKeys -and $LanguageKeys.Count -gt 0) { $languages = @($languages | Where-Object { $LanguageKeys -contains $_.PSChildName }) }
        foreach ($language in $languages) {
            $appKey = Join-Path $language.PSPath 'Applications\QS3D'
            if ((Test-Path -LiteralPath $appKey) -and $PSCmdlet.ShouldProcess("$($version.PSChildName)/$($language.PSChildName)", 'Remove QS3D DemandLoad registration')) {
                Remove-Item -LiteralPath $appKey -Recurse -Force
            }
        }
    }
}

if (-not $KeepFiles) {
    $installFull = [IO.Path]::GetFullPath($InstallDirectory)
    $localRoot = [IO.Path]::GetFullPath($env:LOCALAPPDATA).TrimEnd('\') + '\'
    $isDefaultScope = $installFull.StartsWith($localRoot, [StringComparison]::OrdinalIgnoreCase) -and $installFull.IndexOf('\QS3D\', [StringComparison]::OrdinalIgnoreCase) -ge 0
    if (-not $isDefaultScope -and -not $Force) {
        throw 'Refusing to remove a custom install directory outside the QS3D LocalAppData scope. Use -Force only after verifying the path.'
    }
    if ((Test-Path -LiteralPath $installFull) -and $PSCmdlet.ShouldProcess($installFull, 'Remove QS3D installed files')) {
        Remove-Item -LiteralPath $installFull -Recurse -Force
    }
}

Write-Host 'QS3D DemandLoad registration removed for the selected BricsCAD V25 targets.'
