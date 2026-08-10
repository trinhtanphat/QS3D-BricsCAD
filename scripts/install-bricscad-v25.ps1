param(
    [Parameter(Mandatory = $true)][string]$MsiPath,
    [string]$InstallDir = ""
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

if ([Environment]::OSVersion.Platform -ne [PlatformID]::Win32NT) {
    throw "BricsCAD V25 installation requires Windows."
}

$identity = [Security.Principal.WindowsIdentity]::GetCurrent()
$principal = New-Object Security.Principal.WindowsPrincipal($identity)
if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    throw "Run this installer helper from an elevated PowerShell session."
}

$MsiPath = [IO.Path]::GetFullPath($MsiPath)
if (-not (Test-Path -LiteralPath $MsiPath -PathType Leaf)) {
    throw "MSI not found: $MsiPath"
}
if ([IO.Path]::GetExtension($MsiPath) -ne ".msi") {
    throw "Expected a BricsCAD V25 .msi installer."
}
if ([IO.Path]::GetFileName($MsiPath) -notmatch '^BricsCAD-V25[.-].*\(x64\)\.msi$') {
    Write-Warning "Installer filename does not look like the usual BricsCAD V25 x64 naming convention: $MsiPath"
}

$arguments = New-Object System.Collections.Generic.List[string]
$arguments.Add('/i')
$arguments.Add('"' + $MsiPath + '"')
$arguments.Add('/qn')
$arguments.Add('/norestart')
$arguments.Add('REBOOT=ReallySuppress')
$arguments.Add('ADDDESKTOPSHORTCUT=""')
$arguments.Add('SHOWRELEASENOTES=""')
if (-not [string]::IsNullOrWhiteSpace($InstallDir)) {
    $InstallDir = [IO.Path]::GetFullPath($InstallDir)
    $arguments.Add('APPLICATIONFOLDER="' + $InstallDir + '"')
}

Write-Host "Installing BricsCAD V25 silently from: $MsiPath"
$process = Start-Process -FilePath "msiexec.exe" -ArgumentList ([string]::Join(' ', $arguments)) -Wait -PassThru
if ($process.ExitCode -ne 0 -and $process.ExitCode -ne 3010) {
    throw "BricsCAD installer failed with msiexec exit code $($process.ExitCode)."
}

Write-Host "BricsCAD V25 installation completed. msiexec exit code: $($process.ExitCode)"
Write-Host "Licensing is intentionally not automated by this repository. Activate a valid BricsCAD V25 license/trial for the dedicated runner account, launch BricsCAD once interactively, choose the desired interface/workspace, then configure BRICSCAD_V25_DIR."
